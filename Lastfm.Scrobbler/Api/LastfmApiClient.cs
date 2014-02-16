namespace Lastfm.Scrobbler.Api
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
    using System.Threading;
    using System.Threading.Tasks;
    using Utils;

    public class LastfmApiClient
    {
        private const string API_VERSION = "2.0";
        
        private readonly IHttpClient     _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
                
        public LastfmApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient     = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<MobileSessionResponse> RequestSession(string username, string password)
        {
            Plugin.Logger.Debug("Requesting session key");

            //Build request object
            var data = new MobileSessionRequest() 
            {
                Username = username,
                Password = password,
                ApiKey   = Strings.Keys.LastfmApiKey,
                Method   = Strings.Methods.GetMobileSession
            }.ToDictionary();

            //Append the signature
            Helpers.AppendSignature(ref data);

            //Do stuff
            using (var stream = await _httpClient.Post(new HttpRequestOptions()
            {
                EnableHttpCompression = false,
                ResourcePool = Plugin.LastfmResourcePool,
                CancellationToken = CancellationToken.None,
                Url = BuildPostUrl(true)
            }, data))
            {
                try
                {
                    var result = _jsonSerializer.DeserializeFromStream<MobileSessionResponse>(stream);

                    if (!result.isError())
                        Plugin.Logger.Debug("Got session key: {0}", result.Session.Key);
                    else
                        Plugin.Logger.Error(result.Message);

                    return result;
                }
                catch (Exception e)
                {
                    Plugin.Logger.Debug(e.Message);
                }

                return null;
            }
        }

        public async Task Scrobble(Audio item, LastfmUser user)
        {
            var data = new ScrobbleRequest() 
            {
                Artist    = item.Artists.First(),
                Track     = item.Name,
                Timestamp = Helpers.CurrentTimestamp(),
                ApiKey     = Strings.Keys.LastfmApiKey,
                Method     = Strings.Methods.Scrobble,
                SessionKey = user.SessionKey
            }.ToDictionary();

            //Append the signature
            Helpers.AppendSignature(ref data);

            Plugin.Logger.Info("Scrobbling: {0}", item.Name);

            //Do stuff
            using (var stream = await _httpClient.Post(new HttpRequestOptions()
            {
                EnableHttpCompression = false,
                ResourcePool = Plugin.LastfmResourcePool,
                CancellationToken = CancellationToken.None,
                Url = BuildPostUrl()
            }, data))
            {
                var result = _jsonSerializer.DeserializeFromStream<ScrobbleResponse>(stream);
            }
        }

        public async Task NowPlaying(Audio item, LastfmUser user)
        {
            var data = new NowPlayingRequest()
            {
                Artist = item.Artists.First(),
                Track  = item.Name,

                ApiKey = Strings.Keys.LastfmApiKey,
                Method = Strings.Methods.NowPlaying,
                SessionKey = user.SessionKey
            }.ToDictionary();

            //Append the signature
            Helpers.AppendSignature(ref data);

            Plugin.Logger.Info("Now playing: {0}", item.Name);

            //Do stuff
            using (var stream = await _httpClient.Post(new HttpRequestOptions()
            {
                EnableHttpCompression = false,
                ResourcePool = Plugin.LastfmResourcePool,
                CancellationToken = CancellationToken.None,
                Url = BuildPostUrl()
            }, data))
            {
                var result = _jsonSerializer.DeserializeFromStream<ScrobbleResponse>(stream);
            }
        }

        #region Private methods
        private string BuildGetUrl(string method)
        {
            return String.Format("{0}/{1}/?method={2}&api_key={3}&format=json", Strings.Endpoints.LastfmApi, API_VERSION, method, Strings.Keys.LastfmApiKey);
        }

        private string BuildPostUrl(bool secure = false)
        {
            if (secure)
                return String.Format("{0}/{1}/?format=json", Strings.Endpoints.LastfmApiS, API_VERSION);

            return String.Format("{0}/{1}/?format=json", Strings.Endpoints.LastfmApi, API_VERSION);
        } 
        #endregion
    }
}
