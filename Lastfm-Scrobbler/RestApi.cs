namespace LastfmScrobbler
{
    using Api;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Model.Serialization;
    using MediaBrowser.Model.Services;
    using Models.Responses;

    [Route("/Lastfm/Login", "POST")]
    public class Login : IReturn<MobileSessionResponse>
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RestApi : IService
    {
        private readonly LastfmApiClient _apiClient;

        public RestApi(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);
        }

        public object Post(Login request)
        {
            return _apiClient.RequestSession(request.Username, request.Password);
        }
    }
}
