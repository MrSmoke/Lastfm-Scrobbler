namespace LastfmScrobbler.Api
{
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Model.Serialization;
    using Models;
    using Models.Requests;
    using Models.Responses;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Utils;

    public class LastfmApiClient : BaseLastfmApiClient
    {
        public LastfmApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer) : base(httpClient, jsonSerializer) { }

        public async Task<MobileSessionResponse> RequestSession(string username, string password)
        {
            //Build request object
            var request = new MobileSessionRequest
            {
                Username = username,
                Password = password,
                Secure   = true
            };

            var response = await Post<MobileSessionRequest, MobileSessionResponse>(request);

            //Log the key for debugging
            if (response != null)
                Plugin.Logger.Info("{0} successfully logged into Last.fm", username);

            return response;
        }

        public async Task Scrobble(Audio item, LastfmUser user)
        {
            var request = new ScrobbleRequest
            {
                Track      = item.Name,
                Album      = item.Album,
                Artist     = item.Artists.First(),
                Timestamp  = Helpers.CurrentTimestamp(),
                SessionKey = user.SessionKey
            };

            var response = await Post<ScrobbleRequest, ScrobbleResponse>(request).ConfigureAwait(false);

            if (response != null && !response.IsError())
            {
                Plugin.Logger.Info("{0} played '{1}' - {2} - {3}", user.Username, request.Track, request.Album, request.Artist);
                return;
            }

            Plugin.Logger.Error("Failed to Scrobble track: {0}", item.Name);
        }

        public async Task NowPlaying(Audio item, LastfmUser user)
        {
            var request = new NowPlayingRequest
            {
                Track  = item.Name,
                Album  = item.Album,
                Artist = item.Artists.First(),
                SessionKey = user.SessionKey
            };

            //Add duration
            if (item.RunTimeTicks != null)
                request.Duration = Convert.ToInt32(TimeSpan.FromTicks((long)item.RunTimeTicks).TotalSeconds);

            var response = await Post<NowPlayingRequest, ScrobbleResponse>(request).ConfigureAwait(false);

            if (response != null && !response.IsError())
            {
                Plugin.Logger.Info("{0} is now playing '{1}' - {2} - {3}", user.Username, request.Track, request.Album, request.Artist);
                return;
            }

            Plugin.Logger.Error("Failed to send now playing for track: {0}", item.Name);
        }

        /// <summary>
        /// Loves or unloves a track
        /// </summary>
        /// <param name="item">The track</param>
        /// <param name="user">The Lastfm User</param>
        /// <param name="love">If the track is loved or not</param>
        /// <returns></returns>
        public async Task<bool> LoveTrack(Audio item, LastfmUser user, bool love = true)
        {
            var request = new TrackLoveRequest(love)
            {
                Artist = item.Artists.First(),
                Track  = item.Name,
                SessionKey = user.SessionKey,
            };

            //Send the request
            var response = await Post<TrackLoveRequest, BaseResponse>(request).ConfigureAwait(false);

            if (response == null || response.IsError())
            {
                Plugin.Logger.Error("{0} Failed to love = {3} track '{1}' - {2}", user.Username, item.Name, response?.Message ?? "empty response", love);
                return false;
            }

            Plugin.Logger.Info("{0} {2}loved track '{1}'", user.Username, item.Name, love ? "" : "un");
            return true;
        }

        /// <summary>
        /// Unlove a track. This is the same as LoveTrack with love as false
        /// </summary>
        /// <param name="item">The track</param>
        /// <param name="user">The Lastfm User</param>
        /// <returns></returns>
        public Task<bool> UnloveTrack(Audio item, LastfmUser user)
        {
            return LoveTrack(item, user, false);
        }
    }
}
