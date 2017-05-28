namespace LastfmScrobbler.Configuration
{
    using System.Collections.Generic;
    using Models;
    using MediaBrowser.Model.Plugins;
    using Resources;

    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public List<LastfmUser> LastfmUsers { get; set; }

        public string ApiKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            LastfmUsers = new List<LastfmUser>();
            ApiKey = Strings.Keys.LastfmApiKey;
        }
    }
}
