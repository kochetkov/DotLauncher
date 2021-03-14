using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DotLauncher.Utils
{
    public static class WebUtils
    {
        private static string cacheDirectory;

        public static void Init(string cacheDir)
        {
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            cacheDirectory = cacheDir;
        }

        public static string Download(string url, Encoding encoding)
        {
            var sha512 = new SHA512Managed();

            var cachedDataFilename = BitConverter.ToString(sha512.ComputeHash(encoding.GetBytes(url)))
                .Replace("-", string.Empty)
                .ToLower();

            var cachedDataPath = PathUtils.Combine(cacheDirectory, cachedDataFilename);

            if (File.Exists(cachedDataPath))
            {
                return File.ReadAllText(cachedDataPath);
            }

            var webClient = new WebClient();
            var downloadedData = webClient.DownloadData(url);

            File.WriteAllBytes(cachedDataPath, downloadedData);
            
            return encoding.GetString(downloadedData);
        }
    }
}
