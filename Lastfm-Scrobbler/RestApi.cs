namespace LastfmScrobbler
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Api;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Controller.Net;
    using MediaBrowser.Model.Serialization;
    using MediaBrowser.Model.Services;
    using Models;

    [Route("/LastFm/callback")]
    public class LastfmCallback
    {
        public string Token { get; set; }
        public Guid UserId { get; set; }
    }

    public class RestApi : IService, IHasResultFactory, IDisposable
    {
        private readonly IUserManager _userManager;
        private readonly LastfmApiClient _apiClient;

        public RestApi(IJsonSerializer jsonSerializer, IHttpClient httpClient, IUserManager userManager)
        {
            _userManager = userManager;
            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);
        }

        public async Task<object> Get(LastfmCallback callback)
        {
            if (string.IsNullOrWhiteSpace(callback.Token))
                return Redirect(AuthResult.MissingToken);

            //Check user is valid
            var user = _userManager.GetUserById(callback.UserId);
            if (user == null)
                return Redirect(AuthResult.InvalidUser);

            var session = await _apiClient.GetSession(callback.Token);
            if (session.IsError())
                return Redirect(AuthResult.BadSession);

            var userConfig =
                Plugin.Instance.PluginConfiguration.LastfmUsers.FirstOrDefault(
                    u => u.MediaBrowserUserId.Equals(user.Id));

            if (userConfig == null)
            {
                Plugin.Logger.Info("Creating config for user: {0}", user.Id);

                //create new config for user
                Plugin.Instance.PluginConfiguration.LastfmUsers.Add(new LastfmUser
                {
                    SessionKey = session.Session.Key,
                    MediaBrowserUserId = user.Id,
                    Options = new LastFmUserOptions
                    {
                        Scrobble = true
                    },
                    Username = session.Session.Name
                });
            }
            else
            {
                Plugin.Logger.Info("Update config for user: {0}", user.Id);

                //update existing
                userConfig.SessionKey = session.Session.Key;
                userConfig.Username = session.Session.Name;
            }

            Plugin.Instance.SaveConfiguration();

            return await Redirect(AuthResult.Success);
        }

        private enum AuthResult
        {
            Success = 200,
            MissingToken = 10,
            BadSession = 20,
            InvalidUser = 30
        }

        private Task<object> Redirect(AuthResult result)
        {
            //return ResultFactory.GetStaticFileResult(Request, "");

            return Task.FromResult<object>(result.ToString());
        }

        public void Dispose()
        {
            _apiClient?.Dispose();
        }

        public IRequest Request { get; set; }
        public IHttpResultFactory ResultFactory { get; set; }
    }
}
