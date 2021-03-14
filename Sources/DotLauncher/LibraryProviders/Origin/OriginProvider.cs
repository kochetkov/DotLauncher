/*
 * Parts of this code were taken from the https://github.com/JosefNemec/Playnite project
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DotLauncher.Utils;

namespace DotLauncher.LibraryProviders.Origin
{
    internal sealed class OriginProvider : ILibraryProvider
    {
        private static readonly string DataPath =
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\Origin\";

        public Color BrandColor { get; } = Color.FromArgb(245, 108, 45);

        public IEnumerable<GameDescriptor> CollectInstalledGames()
        {
            var contentPath = PathUtils.Combine(DataPath, "LocalContent");

            if (Directory.Exists(contentPath))
            {
                var packages = Directory.GetFiles(contentPath, "*.mfst", SearchOption.AllDirectories);

                foreach (var package in packages)
                {
                    string gameId;
                    string gameName = default;

                    try
                    {
                        gameId = Path.GetFileNameWithoutExtension(package);

                        if (!gameId.StartsWith("Origin"))
                        {
                            // Get game id by fixing file via adding : before integer part of the name
                            // for example OFB-EAST52017 converts to OFB-EAST:52017
                            var match = Regex.Match(gameId, @"^(.*?)(\d+)$");
                            if (!match.Success) { continue; }
                            gameId = match.Groups[1].Value + ":" + match.Groups[2].Value;
                        }

                        var localData = GetGameLocalData(gameId);
                        
                        if (localData.OfferType == "Base Game" || localData.OfferType == "DEMO")
                        {
                            gameName = StringExtensions.NormalizeGameName(localData.DisplayName);
                        }
                    }
                    catch (Exception) { continue; }

                    if (!string.IsNullOrEmpty(gameId) && !string.IsNullOrEmpty(gameName))
                    {
                        yield return new GameDescriptor(gameName, gameId);
                    }
                }
            }
        }

        public void RunGame(GameDescriptor game)
        {
            ProcessUtils.StartSilent($"origin://launchgame/{game.AppId}");
        }

        private static GameLocalData GetGameLocalData(string gameId)
        {
            var url = $@"https://api1.origin.com/ecommerce2/public/{gameId}/en_US";
            var webClient = new WebClient();
            var stringData = Encoding.UTF8.GetString(webClient.DownloadData(url));
            var offerType = JsonUtils.GetStringProperty(stringData, "offerType");
            var displayName = JsonUtils.GetStringProperty(stringData, "localizableAttributes", "displayName");

            return new GameLocalData(offerType, displayName);
        }
    }
}
