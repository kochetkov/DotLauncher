using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace DotLauncher.Utils
{
    public static class SystemUtils
    {
        public static Dictionary<string, string> GetSystemInformation()
        {
            return GetStaticProperties(typeof(SystemInformation));
        }

        public static Dictionary<string, string> GetEnvironmentInformation()
        {
            var result = GetStaticProperties(typeof(Environment));

            var framework = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName;

            result.Remove("StackTrace");
            result["Framework"] = framework;

            return result;
        }

        private static Dictionary<string, string> GetStaticProperties(Type type)
        {
            var result = new Dictionary<string, string>();

            var properties = type.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(type);
                result[propertyInfo.Name] = value?.ToString() ?? "null";
            }

            return result;
        }
    }
}
