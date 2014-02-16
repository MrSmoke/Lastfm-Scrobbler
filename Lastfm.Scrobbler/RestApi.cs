namespace Lastfm.Scrobbler
{
    using Api;
    using Models.Responses;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Net;
    using MediaBrowser.Model.Serialization;
    using ServiceStack;
    using System.Threading.Tasks;

    [Route("/Lastfm/Login", "POST")]
    [Api(("Restarts the application, if needed"))]
    public class Login
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }


    public class RestApi : IRestfulService
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
