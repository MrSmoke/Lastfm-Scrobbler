namespace LastfmScrobbler.Utils
{
    using System;
    using System.Text.RegularExpressions;

    public static class StringHelper
    {
        //This coiuld also be [a-z][0-9]
        private static readonly Regex SanitiseRegex = new Regex("[\\~#%&*{}/:<>?,-.()|\"-]", RegexOptions.Compiled);

        public static bool IsLike(string s, string t)
        {
            //Placeholder until we have a better way
            var source = SanitiseString(s);
            var target = SanitiseString(t);

            return source.Equals(target, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitiseString(string s)
        {
            //Remove invalid chars and then all spaces
            return Regex.Replace(SanitiseRegex.Replace(s, string.Empty), @"\s+", string.Empty);
        }
    }
}
