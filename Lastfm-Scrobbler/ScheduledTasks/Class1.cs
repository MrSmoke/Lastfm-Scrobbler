namespace LastfmScrobbler.ScheduledTasks
{
    using Api;
    using MediaBrowser.Common.Net;
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
    using MediaBrowser.Model.Extensions;
    using MediaBrowser.Model.Tasks;
    using Utils;
    using StringHelper = Utils.StringHelper;

    class ImportLastfmData : IScheduledTask
    {
        private readonly IUserManager _userManager;
        private readonly LastfmApiClient _apiClient;
        private readonly IUserDataManager _userDataManager;

        public ImportLastfmData(IHttpClient httpClient, IJsonSerializer jsonSerializer, IUserManager userManager, IUserDataManager userDataManager)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;

            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new List<TaskTriggerInfo>
            {
                new TaskTriggerInfo {TimeOfDayTicks = 0}
            };
        }

        public string Name => "Import Last.fm Data";

        public string Key { get; }

        public string Category => "Last.fm";

        public string Description => "Import play counts and favourite tracks for each user with Last.fm accounted configured";

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
            var totalUsers = users.Count;

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var progressOffset = (double)usersProcessed++ / totalUsers;
                var maxProgressForStage = (double)usersProcessed / totalUsers;

                await SyncDataforUserByArtistBulk(user, progress, cancellationToken, maxProgressForStage, progressOffset);
            }

            Plugin.Syncing = false;
        }


        private async Task SyncDataforUserByArtistBulk(User user, IProgress<double> progress, CancellationToken cancellationToken, double maxProgress, double progressOffset)
        {
            var lastFmUser = UserHelpers.GetUser(user);

            if (!lastFmUser.Options.SyncFavourites)
            {
                Plugin.Logger.Info("User {0} does not have favourite sync enabled", user.Name);
                return;
            }

            var artists = user.RootFolder.GetRecursiveChildren().OfType<MusicArtist>().ToList();

            var totalSongs = 0;
            var matchedSongs = 0;

            //Get loved tracks
            var lovedTracksReponse = await _apiClient.GetLovedTracks(lastFmUser).ConfigureAwait(false);
            var hasLovedTracks = lovedTracksReponse.HasLovedTracks();

            if (!hasLovedTracks)
            {
                Plugin.Logger.Info("User {0} has no loved tracks in last.fm", user.Name);
                return;
            }

            //Loop through each artist
            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var artistMbId = artist.GetProviderId(MetadataProviders.MusicBrainzArtist);

                //Loop through each song
                foreach (var song in artist.GetRecursiveChildren().OfType<Audio>())
                {
                    totalSongs++;

                    var songMbTrackId = song.GetProviderId(MetadataProviders.MusicBrainzTrack);

                    if (!string.IsNullOrWhiteSpace(songMbTrackId))
                    {
                        var userData = _userDataManager.GetUserData(user.Id, song);

                        var isLoved = lovedTracksReponse.LovedTracks.Tracks.Any(t => songMbTrackId.Equals(t.MusicBrainzId, StringComparison.OrdinalIgnoreCase));

                        Plugin.Logger.Debug("{0} Favourite: {1}", song.Name, isLoved);

                        if (userData.IsFavorite != isLoved)
                        {
                            userData.IsFavorite = isLoved;
                            await _userDataManager.SaveUserData(user.Id, song, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);
                        }

                        continue;
                    }

                    //try match using artist and song name
                    if (!string.IsNullOrWhiteSpace(artistMbId))
                    {
                        var lovedArtistTracks = lovedTracksReponse.LovedTracks.Tracks.Where(t => artistMbId.Equals(t.Artist.MusicBrainzId, StringComparison.OrdinalIgnoreCase));

                        var tracks = lovedArtistTracks.Where(t => StringHelper.IsLike(t.Name, song.Name));


                    }
                }
            }

            //The percentage might not actually be correct but I'm pretty tired and don't want to think about it
            //Plugin.Logger.Info("Finished import Last.fm library for {0}. Local Songs: {1} | Last.fm Songs: {2} | Matched Songs: {3} | {4}% match rate", user.Name, totalSongs, usersTracks.Count, matchedSongs, Math.Round(((double)matchedSongs / Math.Min(usersTracks.Count, totalSongs)) * 100));
        }
    }
}
