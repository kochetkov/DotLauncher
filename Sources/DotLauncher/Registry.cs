﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using DotLauncher.LibraryProviders;
using DotLauncher.Utils;
using Newtonsoft.Json;

namespace DotLauncher
{
    internal class Registry
    {
        public List<GameDescriptor> InstalledGames { get; }
        public List<GameDescriptor> MostPlayedGames { get; }

        public List<GameDescriptor> FavoriteGames { get; }

        private readonly string gamesDataJsonPath;
        private readonly IEnumerable<ILibraryProvider> libraryProviders;
        private readonly Dictionary<GameDescriptor, ILibraryProvider> gamesLibraryProviders;

        private Dictionary<string, GameData> gamesData;

        public Registry(IEnumerable<ILibraryProvider> libraryProviders)
        {
            gamesDataJsonPath = PathUtils.Combine(Program.AppDataDirectoryPath, "GamesData.json");
            LoadGamesData();

            InstalledGames = new List<GameDescriptor>();
            MostPlayedGames = new List<GameDescriptor>();
            FavoriteGames = new List<GameDescriptor>();
            
            gamesLibraryProviders = new Dictionary<GameDescriptor, ILibraryProvider>();
            this.libraryProviders = libraryProviders;
            Refresh();
        }

        public void Refresh()
        {
            gamesLibraryProviders.Clear();
            InstalledGames.Clear();

            // Update installed games
            foreach (var libraryProvider in libraryProviders)
            {
                var collectedGames = libraryProvider.CollectInstalledGames();

                var tempList = new List<GameDescriptor>();

                foreach (var gameDescriptor in collectedGames)
                {
                    var appId = gameDescriptor.AppId;
                    if (!gamesData.ContainsKey(appId)) { gamesData.Add(appId, new GameData(appId)); }
                    gamesLibraryProviders.Add(gameDescriptor, libraryProvider);
                    tempList.Add(gameDescriptor);
                }

                tempList.Sort();
                InstalledGames.AddRange(tempList);
            }

            // Remove orphaned games data
            var tempGamesData = new Dictionary<string, GameData>();

            foreach (var (appId, gameData) in gamesData)
            {
                if (InstalledGames.Exists(gameDescriptor => gameDescriptor.AppId == appId))
                {
                    tempGamesData.Add(appId, gameData);
                }
            }

            gamesData = tempGamesData;
            SaveGamesData();
            RefreshDynamicLists();
        }

        public void RunGame(GameDescriptor gameDescriptor)
        {
            Debug.Assert(gamesLibraryProviders.ContainsKey(gameDescriptor));
            gamesLibraryProviders[gameDescriptor].RunGame(gameDescriptor);
            
            var gameData = gamesData[gameDescriptor.AppId];
            gameData.LaunchCount++;
            gameData.LastActivity = DateTime.Now;
            SaveGamesData();
        }

        public Color GetGameLibraryBrandColor(GameDescriptor gameDescriptor)
        {
            Debug.Assert(gamesLibraryProviders.ContainsKey(gameDescriptor));
            return gamesLibraryProviders[gameDescriptor].BrandColor;
        }

        public DateTime GetGameLastActivity(GameDescriptor gameDescriptor)
        {
            Debug.Assert(gamesData.ContainsKey(gameDescriptor.AppId));
            return gamesData[gameDescriptor.AppId].LastActivity;
        }

        public uint GetGameLaunchCount(GameDescriptor gameDescriptor)
        {
            Debug.Assert(gamesData.ContainsKey(gameDescriptor.AppId));
            return gamesData[gameDescriptor.AppId].LaunchCount;
        }

        public bool GetGameAddedToFavorites(GameDescriptor gameDescriptor)
        {
            Debug.Assert(gamesData.ContainsKey(gameDescriptor.AppId));
            return gamesData[gameDescriptor.AppId].IsFavorite;
        }

        public void ChangeGameFavoriteStatus(GameDescriptor gameDescriptor)
        {
            Debug.Assert(gamesData.ContainsKey(gameDescriptor.AppId));
            var gameData = gamesData[gameDescriptor.AppId];
            gameData.IsFavorite = !gameData.IsFavorite;
            gameData.AddedToFavorites = gameData.IsFavorite ? DateTime.Now : default;
            SaveGamesData();
            RefreshDynamicLists();
        }

        private void RefreshDynamicLists()
        {
            MostPlayedGames.Clear();
            FavoriteGames.Clear();

            // Update favorite games
            var sortedByAddedToFavorites = gamesData.Where(key => key.Value.IsFavorite)
                .OrderBy(key => key.Value.AddedToFavorites);

            foreach (var (appId, _) in sortedByAddedToFavorites)
            {
                var favGameDescriptor = GetGameDescriptorByAppId(appId);
                FavoriteGames.Add(favGameDescriptor);
            }

            // Update most played games
            var sortedByLaunchCount = gamesData
                .Where(key => !FavoriteGames.Contains(GetGameDescriptorByAppId(key.Value.AppId)) && key.Value.LaunchCount != 0)
                .OrderBy(key => key.Value.LaunchCount)
                .Reverse()
                .Take(5) // TODO: Move mpg count treshold value to settings
                .Reverse();

            foreach (var (appId, gameData) in sortedByLaunchCount)
            {
                var mpgGameDescriptor = GetGameDescriptorByAppId(appId);
                MostPlayedGames.Add(mpgGameDescriptor);
            }
        }

        private GameDescriptor GetGameDescriptorByAppId(string appId) 
            => InstalledGames.Find(gameDescriptor => gameDescriptor.AppId == appId);

        private void LoadGamesData()
        {
            if (File.Exists(gamesDataJsonPath))
            {
                var gamesDataJson = File.ReadAllText(gamesDataJsonPath);
                gamesData = JsonConvert.DeserializeObject<Dictionary<string, GameData>>(gamesDataJson);
            }
            else
            {
                gamesData = new Dictionary<string, GameData>();
            }
        }

        private void SaveGamesData()
        {
            var gamesDataJson = JsonConvert.SerializeObject(gamesData, Formatting.Indented);
            File.WriteAllText(gamesDataJsonPath, gamesDataJson);
        }
    }
}