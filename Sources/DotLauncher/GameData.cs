using System;
using Newtonsoft.Json;

namespace DotLauncher
{
    [JsonObject(MemberSerialization.OptOut)]
    internal class GameData
    {
        public string AppId { get; }

        public uint LaunchCount { get; set; }

        public DateTime LastActivity { get; set; }

        public bool IsFavorite { get; set; }

        public DateTime AddedToFavorites { get; set; }

        public GameData(string appId)
        {
            AppId = appId;
        }
    }
}
