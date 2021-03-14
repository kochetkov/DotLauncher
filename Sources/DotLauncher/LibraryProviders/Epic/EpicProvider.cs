using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DotLauncher.Utils;

namespace DotLauncher.LibraryProviders.Epic
{
    internal class EpicProvider : ILibraryProvider
    {
        private static readonly string ManifestsPath =
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\Epic\EpicGamesLauncher\Data\Manifests";

        public Color BrandColor { get; } = Color.FromArgb(255, 255, 255);

        public IEnumerable<GameDescriptor> CollectInstalledGames()
        {
            if (!Directory.Exists(ManifestsPath)) { yield break; }

            var manifestsFiles = Directory.GetFiles(ManifestsPath, "*.item", SearchOption.TopDirectoryOnly);

            foreach (var manifestsFile in manifestsFiles)
            {
                var jsonString = File.ReadAllText(manifestsFile);
                var name = JsonUtils.GetStringProperty(jsonString, "DisplayName");
                var appId = JsonUtils.GetStringProperty(jsonString, "AppName");

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