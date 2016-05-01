namespace LastfmScrobbler
{
    using System.Threading.Tasks;
    using Api;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Net;
    using MediaBrowser.Model.Serialization;
    using ServiceStack;

    [Route("/LastFm/callback")]
    public class Callback
    {
        public string Token { get; set; }
    }

    public class RestApi : IRestfulService
    {
        private readonly LastfmApiClient _apiClient;

        public RestApi(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);
        }

        public string Get(Callback callback)
        {
            Plugin.Logger.Debug(callback.Token);

            return callback.Token;
        }
    }
}
