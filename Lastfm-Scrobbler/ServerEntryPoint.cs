namespace LastfmScrobbler
{
    using Api;
    using Configuration;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Common.Security;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Controller.Plugins;
    using MediaBrowser.Controller.Session;
    using MediaBrowser.Model.Entities;
    using MediaBrowser.Model.Logging;
    using MediaBrowser.Model.Serialization;
    using System.Linq;
    using System.Threading.Tasks;


    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint, IRequiresRegistration
    {
        private readonly ISessionManager  _sessionManager;
        private readonly IJsonSerializer  _jsonSerializer;
        private readonly IUserDataManager _userDataManager;

        private LastfmApiClient _apiClient;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager"></param>
        public ServerEntryPoint(ISessionManager sessionManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IUserDataManager userDataManager)
        {
            Plugin.Logger = logManager.GetLogger(Plugin.Instance.Name);

            _sessionManager  = sessionManager;
            _jsonSerializer  = jsonSerializer;
            _userDataManager = userDataManager;

            _apiClient = new LastfmApiClient(httpClient, _jsonSerializer);
            
            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            //Bind events
            _sessionManager.PlaybackStart   += this.PlaybackStart;
            _sessionManager.PlaybackStopped += this.PlaybackStopped;
            _userDataManager.UserDataSaved  += this.UserDataSaved;
        }

        /// <summary>
        /// Let last fm know when a user favourites or unfavourites a track
        /// </summary>
        void UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            //We also only care about User rating changes
            if (!e.SaveReason.Equals(UserDataSaveReason.UpdateUserRating))
                return;

            var LastfmUser = Utils.UserHelpers.GetUser(e.UserId);

            if (LastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find user");
                return;
            }

            if (string.IsNullOrWhiteSpace(LastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            var item = e.Item as Audio;

            _apiClient.LoveTrack(item, LastfmUser, e.UserData.IsFavorite);
        }


        /// <summary>
        /// Let last.fm know when a track has finished.
        
        /// Playback stopped is run when a track is finished.
        /// </summary>
        private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            var item = e.Item as Audio;

            //Make sure the track has been fully played
            if (!e.PlayedToCompletion)
            {
                Plugin.Logger.Debug("'{0}' not played to completion, not scrobbling", item.Name);
                return;
            }

            //Played to completion will sometimes be true even if the track has only played 10% so check the playback ourselfs (it must use the app settings or something)
            //Make sure 80% of the track has been played back
            var playPercent = ((double)e.PlaybackPositionTicks / item.RunTimeTicks) * 100;
            if (playPercent < 80)
            {
                Plugin.Logger.Debug("'{0}' only played {1}%, not scrobbling", item.Name, playPercent);
                return;
            }

            var LastfmUser = Utils.UserHelpers.GetUser(e.Users.First());

            if (LastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find user");
                return;
            }

            //User doesn't want to scrobble
            if (!LastfmUser.Options.Scrobble)
            {
                Plugin.Logger.Debug("{0} ({1}) does not want to scrobble", e.Users.FirstOrDefault().Name, LastfmUser.Username);
                return;
            }

            if (string.IsNullOrWhiteSpace(LastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            _apiClient.Scrobble(item, LastfmUser);
        }

        /// <summary>
        /// Let Last.fm know when a user has started listening to a track
        /// </summary>
        private async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            var LastfmUser = Utils.UserHelpers.GetUser(e.Users.First());

            if (LastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find user");
                return;
            }

            //User doesn't want to scrobble
            if (!LastfmUser.Options.Scrobble)
            {
                Plugin.Logger.Debug("{0} ({1}) does not want to scrobble", e.Users.FirstOrDefault().Name, LastfmUser.Username);
                return;
            }

            if (string.IsNullOrWhiteSpace(LastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            var item = e.Item as Audio;
            _apiClient.NowPlaying(item, LastfmUser);
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="oldConfig">The old config.</param>
        /// <param name="newConfig">The new config.</param>
        public void OnConfigurationUpdated(PluginConfiguration oldConfig, PluginConfiguration newConfig)
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //Unbind events
            _sessionManager.PlaybackStart   -= this.PlaybackStart;
            _sessionManager.PlaybackStopped -= this.PlaybackStopped;
            _userDataManager.UserDataSaved  -= this.UserDataSaved;

            //Clean up
            _apiClient = null;

        }

        /// <summary>
        /// Loads our registration information
        ///
        /// </summary>
        /// <returns></returns>
        public async Task LoadRegistrationInfoAsync()
        {
            //Plugin.Instance.Registration = await PluginSecurityManager.GetRegistrationStatus("LastfmScrobbler", "[**MB2CompatibleFeature**]").ConfigureAwait(false);
        }
    }
}
