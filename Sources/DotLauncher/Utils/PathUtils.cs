using System.IO;

namespace DotLauncher.Utils
{
    internal static class PathUtils
    {
        public static string Combine(params string[] pathParts) => Path.GetFullPath(Path.Combine(pathParts));
    }
}
