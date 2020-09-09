using System;

namespace DotLauncher
{
    internal class GameDescriptor : IComparable, IEquatable<GameDescriptor>
    {
        public string Name { get; }

        public string AppId { get; }

        public GameDescriptor(string name, string appId)
        {
            Name = name;
            AppId = appId;
        }

        public int CompareTo(object obj)
        {
            if (obj != null && obj is GameDescriptor other)
            {
                return string.Compare(Name, other.Name, StringComparison.Ordinal);
            }

            return -1;
        }

        public bool Equals(GameDescriptor other)
        {
            if (ReferenceEquals(null, other)) {return false;}
            if (ReferenceEquals(this, other)) {return true;}
            return string.Equals(AppId, other.AppId, StringComparison.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {return false;}
            if (ReferenceEquals(this, obj)) {return true;}
            if (obj.GetType() != this.GetType()) {return false;}
            return Equals((GameDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return (AppId != null ? StringComparer.InvariantCulture.GetHashCode(AppId) : 0);
        }
    }
}
