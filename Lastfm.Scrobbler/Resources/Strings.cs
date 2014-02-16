namespace Lastfm.Scrobbler.Resources
{
    using System;

    public static class Strings
    {
        public static class Endpoints
        {
            public static string LastfmApi  = "http://ws.audioscrobbler.com";
            public static string LastfmApiS = "https://ws.audioscrobbler.com";
        }

        public static class Methods
        {
            public static string Scrobble         = "track.scrobble";
            public static string NowPlaying       = "track.updateNowPlaying";
            public static string GetMobileSession = "auth.getMobileSession";
        }

        public static class Keys
        {
            public static string LastfmApiKey = "cb3bdcd415fcb40cd572b137b2b255f5";
            public static string LastfmApiSeceret = "3a08f9fad6ddc4c35b0dce0062cecb5e";
        }
    }
}
