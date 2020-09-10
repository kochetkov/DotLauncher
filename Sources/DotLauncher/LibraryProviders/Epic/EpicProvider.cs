using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DotLauncher.Utils;
using Newtonsoft.Json.Linq;

namespace DotLauncher.LibraryProviders.Epic
{
    internal class EpicProvider : ILibraryProvider
    {
        private static readonly string ManifestsPath = 
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\Epic\EpicGamesLauncher\Data\Manifests";

        public Color BrandColor { get; } = Color.FromArgb(255, 255, 255);

        public IEnumerable<GameDescriptor> CollectInstalledGames()
        {
            var manifestsFiles = Directory.GetFiles(ManifestsPath, "*.item", SearchOption.TopDirectoryOnly);

            foreach (var manifestsFile in manifestsFiles)
            {
                var manifestObject = JObject.Parse(File.ReadAllText(manifestsFile));

                var name = (string)manifestObject["DisplayName"];
                var appId = (string)manifestObject["AppName"];

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(appId))
                {
                    yield return new GameDescriptor(name, appId);
                }
            }
        }

        public void RunGame(GameDescriptor game)
        {
            ProcessUtils.StartSilent($"com.epicgames.launcher://apps/{game.AppId}?action=launch&silent=true");
        }
    }
}