namespace LastfmScrobbler.ScheduledTasks
{
    using LastfmScrobbler.Api;
    using LastfmScrobbler.Models;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Common.ScheduledTasks;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Model.Entities;
    using MediaBrowser.Model.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    class ImportLastfmData : IScheduledTask
    {
        private readonly IUserManager     _userManager;
        private readonly LastfmApiClient  _apiClient;
        private readonly IUserDataManager _userDataManager;
        private readonly IJsonSerializer _json;

        public ImportLastfmData(IHttpClient httpClient, IJsonSerializer jsonSerializer, IUserManager userManager, IUserDataManager userDataManager)
        {
            _userManager     = userManager;
            _userDataManager = userDataManager;
            
            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);

            //DEBUG
            _json = jsonSerializer;
        }

        public string Name
        {
            get { return "Import Last.fm Data"; }
        }

        public string Category
        {
            get { return "Last.fm"; }
        }

        public string Description
        {
            get { return "Import play counts and favourite tracks"; }
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
            {
                new WeeklyTrigger { DayOfWeek = DayOfWeek.Sunday, TimeOfDay = TimeSpan.FromHours(3) }
            };
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
             //Get all users
            var users = _userManager.Users.Where(u => {
                var user = Utils.UserHelpers.GetUser(u);
                
                return user != null && !String.IsNullOrWhiteSpace(user.SessionKey);
            }).ToList();

            if (users.Count == 0)
            {
                Plugin.Logger.Info("No users found");
                return;
            }

            Plugin.Syncing = true;

            var usersProcessed = 0;
            var totalUsers     = users.Count;

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await SyncDataforUserByArtistBulk(user, progress, cancellationToken, ((double)++usersProcessed / totalUsers));
            }

            Plugin.Syncing = false;
        }

        //To start with im going to sync the tracks by artist
        //If theres problems with doing it this way ill look into another method
        private async Task SyncDataforUserByArtist(User user, IProgress<double> progress, CancellationToken cancellationToken, double progressStage)
        {
            var artists = user.RootFolder.GetRecursiveChildren().OfType<MusicArtist>();
            if (artists == null)
            {
                Plugin.Logger.Info("No artists");
                return;
            }

            var totalArtists = artists.Count();
            var progressedArtists = 0;

            var lastFmUser = Utils.UserHelpers.GetUser(user);

            //Get loved tracks
            var lovedTracksReponse = await _apiClient.GetLovedTracks(lastFmUser).ConfigureAwait(false);
            var hasLovedTracks     = lovedTracksReponse.HasLovedTracks();

            //Loop through each artist
            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //Report progress
                double currentProgress = ((double)++progressedArtists / totalArtists) * progressStage;
                progress.Report(currentProgress * 100);

                //Get tracks from the Api
                var response = await _apiClient.GetTracks(lastFmUser, artist, cancellationToken).ConfigureAwait(false);
                if (response == null || !response.HasTracks())
                {
                    Plugin.Logger.Debug("{0} has no '{1}' tracks in Last.fm", user.Name, artist.Name);
                    continue;
                }

                //Ensure its the same artist
                var lastfmTracks = response.Tracks.Tracks.Where(t => t.Artist.MusicBrainzId.Equals(GetMusicBrainzArtistId(artist)));

                //Loop through each song
                foreach (var song in artist.GetRecursiveChildren().OfType<Audio>())
                {
                    var matchedSong = lastfmTracks.FirstOrDefault(t => Utils.StringHelper.IsLike(t.Name, song.Name));
                    if (matchedSong == null)
                        continue;

                    Plugin.Logger.Debug("Found match for {0}", song.Name);

                    var userData = _userDataManager.GetUserData(user.Id, song.GetUserDataKey());

                    //Check if its a favourite track
                    if (hasLovedTracks && lastFmUser.Options.SyncFavourites)
                    {
                        var favourited = lovedTracksReponse.LovedTracks.Tracks.Any(t => t.MusicBrainzId.Equals(matchedSong.MusicBrainzId));

                        userData.IsFavorite = favourited;

                        Plugin.Logger.Debug("{0} Favourite: {1}", song.Name, favourited);
                    }

                    //Update the play count
                    if (matchedSong.PlayCount > 0)
                    {
                        userData.Played    = true;
                        userData.PlayCount = Math.Max(userData.PlayCount, matchedSong.PlayCount);
                    }
                    else
                    {
                        userData.Played         = false;
                        userData.PlayCount      = 0;
                        userData.LastPlayedDate = null;
                    }
                    
                    await _userDataManager.SaveUserData(user.Id, song, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);
                }
            }
        }

        //New method which downloads the entire library first
        private async Task SyncDataforUserByArtistBulk(User user, IProgress<double> progress, CancellationToken cancellationToken, double progressStage)
        {
            var artists = user.RootFolder.GetRecursiveChildren().OfType<MusicArtist>();
            if (artists == null)
            {
                Plugin.Logger.Info("No artists");
                return;
            }

            var totalArtists = artists.Count();
            var progressedArtists = 0;

            var lastFmUser = Utils.UserHelpers.GetUser(user);

            //Get loved tracks
            var lovedTracksReponse = await _apiClient.GetLovedTracks(lastFmUser).ConfigureAwait(false);
            var hasLovedTracks = lovedTracksReponse.HasLovedTracks();

            //Get entire library
            var usersTracks = await GetUsersLibrary(lastFmUser, cancellationToken);

            if (usersTracks.Count == 0)
            {
                Plugin.Logger.Debug("User {0} has no tracks in last.fm", user.Name);
                return;
            }

            //Group the library y artist
            var userLibrary = usersTracks.GroupBy(t => t.Artist.MusicBrainzId);

            //Loop through each artist
            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //Report progress
                double currentProgress = ((double)++progressedArtists / totalArtists) * progressStage;
                progress.Report(currentProgress * 100);

                //Get all the tracks by the current artist
                var artistMBid = GetMusicBrainzArtistId(artist);
                if (artistMBid == null)
                    continue;

                var artistTracks = userLibrary.FirstOrDefault(t => t.Key.Equals(artistMBid));
                if (artistTracks == null || !artistTracks.Any())
                {
                    Plugin.Logger.Debug("{0} has no tracks in last.fm library for {1}", user.Name, artist.Name);
                    continue;
                }

                Plugin.Logger.Debug("Found {0} tracks in last.fm library for {1}", artistTracks.Count(), artist.Name);

                //Loop through each song
                foreach (var song in artist.GetRecursiveChildren().OfType<Audio>())
                {
                    var matchedSong = artistTracks.FirstOrDefault(t => Utils.StringHelper.IsLike(t.Name, song.Name));
                    if (matchedSong == null)
                        continue;

                    Plugin.Logger.Debug("Found match for {0}", song.Name);

                    var userData = _userDataManager.GetUserData(user.Id, song.GetUserDataKey());

                    //Check if its a favourite track
                    if (hasLovedTracks && lastFmUser.Options.SyncFavourites)
                    {
                        var favourited = lovedTracksReponse.LovedTracks.Tracks.Any(t => t.MusicBrainzId.Equals(matchedSong.MusicBrainzId));

                        userData.IsFavorite = favourited;

                        Plugin.Logger.Debug("{0} Favourite: {1}", song.Name, favourited);
                    }

                    //Update the play count
                    if (matchedSong.PlayCount > 0)
                    {
                        userData.Played = true;
                        userData.PlayCount = Math.Max(userData.PlayCount, matchedSong.PlayCount);
                    }
                    else
                    {
                        userData.Played = false;
                        userData.PlayCount = 0;
                        userData.LastPlayedDate = null;
                    }

                    await _userDataManager.SaveUserData(user.Id, song, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);
                }
            }
        }

        private async Task<List<LastfmTrack>> GetUsersLibrary(LastfmUser lastfmUser, CancellationToken cancellationToken)
        {
            var tracks     = new List<LastfmTrack>();
            var moreTracks = true;
            var page       = 1; //Page 0 = 1

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await _apiClient.GetTracks(lastfmUser, cancellationToken, page++).ConfigureAwait(false);

                if (response == null || !response.HasTracks())
                    break;

                tracks.AddRange(response.Tracks.Tracks);

                moreTracks = !response.Tracks.Metadata.IsLastPage();
            } while (moreTracks == true);

            return tracks;
        }

        //The nuget doesn't seem to have GetProviderId
        private string GetMusicBrainzArtistId(MusicArtist artist)
        {
            if (artist.ProviderIds == null)
            {
                Plugin.Logger.Debug("No provider id: {0}", artist.Name);
                return null;
            }

            string mbArtistId;

            if (!artist.ProviderIds.TryGetValue("MusicBrainzArtist", out mbArtistId))
            {
                Plugin.Logger.Debug("No MBID: {0}", artist.Name);
                return null;
            }

            return mbArtistId;
        }
    }
}
