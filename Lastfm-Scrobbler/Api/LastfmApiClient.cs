namespace LastfmScrobbler.Api
{
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Model.Serialization;
    using Models;
    using Models.Requests;
    using Models.Responses;
    using Resources;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Utils;

    public class LastfmApiClient : BaseLastfmApiClient
    {
        public LastfmApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer) : base(httpClient, jsonSerializer) { }

        public async Task<MobileSessionResponse> RequestSession(string username, string password)
        {
            Plugin.Logger.Debug("Requesting session key");

            //Build request object
            var request = new MobileSessionRequest() 
            {
                Username = username,
                Password = password,

                ApiKey   = Strings.Keys.LastfmApiKey,
                Method   = Strings.Methods.GetMobileSession,
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
            var request = new ScrobbleRequest() 
            {
                Artist     = item.Artists.First(),
                Track      = item.Name,
                Timestamp  = Helpers.CurrentTimestamp(),

                ApiKey     = Strings.Keys.LastfmApiKey,
                Method     = Strings.Methods.Scrobble,
                SessionKey = user.SessionKey
            };

            var response = await Post<ScrobbleRequest, ScrobbleResponse>(request);

            if (response != null)
            {
                Plugin.Logger.Info("{0} played '{1}'", user.Username, item.Name);
                return;
            }

            Plugin.Logger.Error("Failed to Scrobble track: {0}", item.Name);
        }

        public async Task NowPlaying(Audio item, LastfmUser user)
        {
            var request = new NowPlayingRequest()
            {
                Artist   = item.Artists.First(),
                Track    = item.Name,

                ApiKey = Strings.Keys.LastfmApiKey,
                Method = Strings.Methods.NowPlaying,
                SessionKey = user.SessionKey
            };

            //Add duration
            if (item.RunTimeTicks != null)
            {
                request.Duration = Convert.ToInt32(TimeSpan.FromTicks((long)item.RunTimeTicks).TotalSeconds);
            }

            Plugin.Logger.Debug("Now playing track {0} with duration {1}", item.Name, request.Duration);

            var response = await Post<NowPlayingRequest, ScrobbleResponse>(request);

            if (response != null)
            {
                Plugin.Logger.Info("{0} is now playing '{1}'", user.Username, item.Name);
                return;
            }

            Plugin.Logger.Error("Failed to send now playing for track: {0}", item.Name);
        }      
    }
}
