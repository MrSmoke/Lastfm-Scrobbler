namespace LastfmScrobbler.Resources
{
    public static class Strings
    {
        public static class Endpoints
        {
            public static string LastfmApi  = "ws.audioscrobbler.com";
        }

        public static class Methods
        {
            public static string Scrobble         = "track.scrobble";
            public static string NowPlaying       = "track.updateNowPlaying";
            public static string GetSession       = "auth.getSession";
            public static string TrackLove        = "track.love";
            public static string TrackUnlove      = "track.unlove";
        }

        public static class Keys
        {
            public static string LastfmApiKey     = "e85c2d4e649f4a01dbfec778d758ab2e";
            public static string LastfmApiSeceret = "c7c59e71ebd0da786b6fd7059bcced2b";
        }
    }
}
