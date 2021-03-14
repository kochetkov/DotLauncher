using System.Text.RegularExpressions;

namespace DotLauncher.Utils
{
    internal static class StringExtensions
    {
        public static string NormalizeGameName(string name)
        {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }

            var newName = name;
            newName = newName.RemoveTrademarks();
            newName = newName.Replace("_", " ");
            newName = newName.Replace(".", " ");
            newName = RemoveTrademarks(newName);
            newName = Regex.Replace(newName, @"\[.*?\]", "");
            newName = Regex.Replace(newName, @"\(.*?\)", "");
            newName = Regex.Replace(newName, @"\s*:\s*", ": ");
            newName = Regex.Replace(newName, @"\s+", " ");

            if (Regex.IsMatch(newName, @",\s*The$"))
            {
                newName = "The " + Regex.Replace(newName, @",\s*The$", string.Empty, RegexOptions.IgnoreCase);
            }

            return newName.Trim();
        }

        private static string RemoveTrademarks(this string str) =>
            string.IsNullOrEmpty(str) 
                ? str 
                : Regex.Replace(str, @"[™©®]", string.Empty);
    }
}
