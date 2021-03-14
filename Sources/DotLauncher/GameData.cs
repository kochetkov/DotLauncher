using System;

namespace DotLauncher
{
    internal class GameData
    {
        public string AppId { get; set; }

        public uint LaunchCount { get; set; }

        public DateTime LastActivity { get; set; }

        public bool IsFavorite { get; set; }

        public DateTime AddedToFavorites { get; set; }
    }
}
