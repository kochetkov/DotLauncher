using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DotLauncher.Utils;

namespace DotLauncher.LibraryProviders.Steam
{
    internal sealed class SteamProvider : ILibraryProvider
    {
        private readonly string steamInstallationDir;
        private readonly string libraryFoldersPath;

        public SteamProvider()
        {
            steamInstallationDir = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam",
                "SteamPath",
                default
            ) as string;

            if (steamInstallationDir != null && Directory.Exists(steamInstallationDir))
            {
                libraryFoldersPath = PathUtils.Combine(steamInstallationDir, "steamapps", "libraryfolders.vdf");
            }
        }

        public Color BrandColor { get; } = Color.FromArgb(60, 145, 174);

        public IEnumerable<GameDescriptor> CollectInstalledGames()
        {
            var libraryFolders = new HashSet<string> { PathUtils.Combine(steamInstallationDir, "steamapps")};

            var librabyFoldersKv = KeyValue.LoadAsText(libraryFoldersPath);

            if (librabyFoldersKv.Name == "LibraryFolders")
            {
                foreach (var child in librabyFoldersKv.Children)
                {
                    if (uint.TryParse(child.Name, out _))
                    {
                        libraryFolders.Add(PathUtils.Combine(child.Value, "steamapps"));
                    }
                }
            }

            foreach (var libraryFolder in libraryFolders)
            {
                var appmanifestFilePaths = Directory.GetFiles(libraryFolder, "appmanifest_*.acf");

                foreach (var appmanifestFilePath in appmanifestFilePaths)
                {
                    var appmanifestKv = KeyValue.LoadAsText(appmanifestFilePath);

                    if (appmanifestKv.Name == "AppState")
                    {
                        string gameName = null;
                        string gameId = null;

                        foreach (var child in appmanifestKv.Children)
                        {
                            switch (child.Name)
                            {
                                case "appid":
                                    gameId = child.Value;
                                    break;
                                case "name":
                                    gameName = StringExtensions.NormalizeGameName(child.Value);
                                    break;
                            }
                        }

                        if (!string.IsNullOrEmpty(gameId) && !string.IsNullOrEmpty(gameName))
                        {
                            // "228980" corresponds to "Steamworks Common Redistributables" and should be skipped
                            if (gameId != "228980") { yield return new GameDescriptor(gameName, gameId); }
                        }
                    }
                }
            }
        }

        public void RunGame(GameDescriptor game)
        {
            ProcessUtils.StartSilent($"steam://run/{game.AppId}");
        }
    }
}
