namespace LastfmScrobbler.Utils
{
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    public static class Helpers
    {
        public static string CreateMd5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            var md5 = MD5.Create();

            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes  = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            var sb = new StringBuilder();

            foreach (var b in hashBytes)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static void AppendSignature(Dictionary<string, string> data)
        {
            data.Add("api_sig", CreateSignature(data));
        }

        public static int ToTimestamp(DateTime date)
        {
            return Convert.ToInt32((date - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
        }

        public static int CurrentTimestamp()
        {
            return ToTimestamp(DateTime.Now);
        }

        public static string DictionaryToQueryString(Dictionary<string, string> data)
        {
            return string.Join("&",
                data.Where(k => !string.IsNullOrWhiteSpace(k.Value)).Select(kvp =>
                    $"{Uri.EscapeUriString(kvp.Key)}={Uri.EscapeUriString(kvp.Value)}"));
        }

        private static string CreateSignature(Dictionary<string, string> data)
        {
            var s = new StringBuilder();

            foreach (var item in data.OrderBy(x => x.Key))
                s.Append($"{item.Key}{item.Value}");

            //Append seceret
            s.Append(Strings.Keys.LastfmApiSeceret);

            return CreateMd5Hash(s.ToString());
        }
    }
}
