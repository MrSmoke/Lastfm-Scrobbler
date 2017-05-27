namespace LastfmScrobbler
{
    using System;
    using Api;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Model.Serialization;
    using MediaBrowser.Model.Services;

    [Route("/LastFm/callback")]
    public class Callback
    {
        public string Token { get; set; }
    }

    public class RestApi : IService, IDisposable
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

        public void Dispose()
        {
            _apiClient?.Dispose();
        }
    }
}
