using System;
using System.IO;
using System.Reflection;
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
            AppDataDirectoryPath = PathUtils.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DotLauncher"
            );

            if (!Directory.Exists(AppDataDirectoryPath))
            {
                Directory.CreateDirectory(AppDataDirectoryPath);
            }

            Logger.Init(PathUtils.Combine(AppDataDirectoryPath, "Logs"));
            WebUtils.Init(PathUtils.Combine(AppDataDirectoryPath, "Cache"));

            try
            {
                var applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
                Logger.Info($"Application version: {applicationVersion}");

                var systemInformation = SystemUtils.GetSystemInformation();
                Logger.Info("System information collected", systemInformation);

                var environmentInformation = SystemUtils.GetEnvironmentInformation();
                Logger.Info("Environment information collected", environmentInformation);

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
            catch (Exception e)
            {
                Logger.Fatal("Unhandled exception thrown", e);
                throw;
            }

            Logger.Info("Application terminated successfully");
        }
    }
}
