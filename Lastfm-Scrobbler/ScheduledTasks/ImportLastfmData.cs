namespace LastfmScrobbler.ScheduledTasks
{
    using Api;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Common.ScheduledTasks;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Model.Entities;
    using MediaBrowser.Model.Serialization;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Utils;

    class ImportLastfmData : IScheduledTask
    {
        private readonly IUserManager     _userManager;
        private readonly LastfmApiClient  _apiClient;
        private readonly IUserDataManager _userDataManager;

        public ImportLastfmData(IHttpClient httpClient, IJsonSerializer jsonSerializer, IUserManager userManager, IUserDataManager userDataManager)
        {
            _userManager     = userManager;
            _userDataManager = userDataManager;
            
            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);
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
            get { return "Import play counts and favourite tracks for each user with Last.fm accounted configured"; }
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
                var user = UserHelpers.GetUser(u);
                
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

        
        private async Task SyncDataforUserByArtistBulk(User user, IProgress<double> progress, CancellationToken cancellationToken, double progressStage)
        {
            var artists = user.RootFolder.GetRecursiveChildren().OfType<MusicArtist>().ToList();

            var lastFmUser        = UserHelpers.GetUser(user);
            var totalArtists      = artists.Count;
            var progressedArtists = 0;

            //Get loved tracks
            var lovedTracksReponse = await _apiClient.GetLovedTracks(lastFmUser).ConfigureAwait(false);
            var hasLovedTracks     = lovedTracksReponse.HasLovedTracks();

            //Get entire library
            var usersTracks = await GetUsersLibrary(lastFmUser, progress, cancellationToken, (progressStage - (progressStage / 4)));

            if (usersTracks.Count == 0)
            {
                Plugin.Logger.Debug("User {0} has no tracks in last.fm", user.Name);
                return;
            }

            //Group the library by artist
            var userLibrary = usersTracks.GroupBy(t => t.Artist.MusicBrainzId).ToList();

            //Loop through each artist
            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //Report progress
                //These progressStage division are here because download the data is probably 3/4 of the time taken to sync per user
                var currentProgress = ((double)++progressedArtists / totalArtists) * (progressStage / 4) + (progressStage - (progressStage / 4));
                progress.Report(currentProgress * 100);

                Plugin.Logger.Debug(("Progress Sync: " + currentProgress * 100));

                //Get all the tracks by the current artist
                var artistMBid = Helpers.GetMusicBrainzArtistId(artist);
                if (artistMBid == null)
                    continue;

                //Get the tracks from lastfm for the current artist
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
                    //Find the song in the lastFm library
                    var matchedSong = artistTracks.FirstOrDefault(t => StringHelper.IsLike(t.Name, song.Name));

                    if (matchedSong == null)
                        continue; //No match found

                    //We have found a match
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

        private async Task<List<LastfmTrack>> GetUsersLibrary(LastfmUser lastfmUser, IProgress<double> progress, CancellationToken cancellationToken, double progressStage)
        {
            var tracks     = new List<LastfmTrack>();
            var page       = 1; //Page 0 = 1
            bool moreTracks;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await _apiClient.GetTracks(lastfmUser, cancellationToken, page++).ConfigureAwait(false);

                if (response == null || !response.HasTracks())
                    break;

                tracks.AddRange(response.Tracks.Tracks);

                moreTracks = !response.Tracks.Metadata.IsLastPage();

                //Report progress
                var currentProgress = ((double)response.Tracks.Metadata.Page / response.Tracks.Metadata.TotalPages) * progressStage;
                Plugin.Logger.Debug("Progress Downloading: " + currentProgress * 100);
                progress.Report(currentProgress * 100);
            } while (moreTracks);

            return tracks;
        }
    }
}
