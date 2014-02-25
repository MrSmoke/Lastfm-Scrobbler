namespace LastfmScrobbler.Utils
{
    using LastfmScrobbler.Resources;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    public static class Helpers
    {
        public static string CreateMD5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = MD5.Create();

            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
                sb.Append(hashBytes[i].ToString("X2"));

            return sb.ToString();
        }

        public static void AppendSignature(ref Dictionary<string, string> data)
        {
            data.Add("api_sig", CreateSig(data));
        }

        public static int ToTimestamp(DateTime date)
        {
            return Convert.ToInt32((date - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
        }

        public static DateTime FromTimestamp(double timestamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            dtDateTime = dtDateTime.AddSeconds(timestamp).ToLocalTime();

            return dtDateTime;
        }

        public static int CurrentTimestamp()
        {
            return ToTimestamp(DateTime.Now);
        }

        public static string DictionaryToQueryString(Dictionary<string, string> data)
        {
            return String.Join("&", data.Where(k => !String.IsNullOrWhiteSpace(k.Value)).Select(kvp => String.Format("{0}={1}", Uri.EscapeUriString(kvp.Key), Uri.EscapeUriString(kvp.Value))));
        }

        private static string CreateSig(Dictionary<string, string> data)
        {
            StringBuilder s = new StringBuilder();

            foreach (var item in data.OrderBy(x => x.Key))
                s.Append(String.Format("{0}{1}", item.Key, item.Value));

            //Append seceret
            s.Append(Strings.Keys.LastfmApiSeceret);

            return Helpers.CreateMD5Hash(s.ToString());
        }
    }
}
