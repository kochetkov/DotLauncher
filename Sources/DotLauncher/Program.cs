using System;
using System.IO;
using DotLauncher.LibraryProviders;
using DotLauncher.LibraryProviders.Epic;
using DotLauncher.LibraryProviders.Origin;
using DotLauncher.LibraryProviders.Steam;
using DotLauncher.Utils;


namespace DotLauncher
{
    internal static class Program
    {
        public static string AppDataDirectoryPath;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            InitAppDataDirectory();

            var launcherProviders = new ILibraryProvider[]
            {
                new SteamProvider(),
                new OriginProvider(),
                new EpicProvider()
            };

            var registry = new Registry(launcherProviders);
            var appContext = new UI.AppContext(registry);
            appContext.Run();
        }

        private static void InitAppDataDirectory()
        {
            AppDataDirectoryPath = PathUtils.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DotLauncher"
            );

            if (!Directory.Exists(AppDataDirectoryPath))
            {
                Directory.CreateDirectory(AppDataDirectoryPath);
            }
        }
    }
}
