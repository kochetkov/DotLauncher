using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DotLauncher.Utils
{
    public static class JsonUtils
    {
        public static string Serialize<T>(T obj, bool writeIndented = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIndented,
            };

            var serializedObj = obj is Exception exception
                ? SerializeException(exception, writeIndented)
                : JsonSerializer.Serialize(obj, options);
            
            return serializedObj;
        }

        public static T Deserialize<T>(string jsonString)
            => JsonSerializer.Deserialize<T>(jsonString);

        public static void SerializeToFile<T>(T obj, string fileName)
        {
            var jsonString = Serialize(obj);
            File.WriteAllText(fileName, jsonString);
        }

        public static T DeserializeFromFile<T>(string fileName)
        {
            var jsonString = File.ReadAllText(fileName);
            return Deserialize<T>(jsonString);
        }

        public static string GetStringProperty(string jsonString, params string[] propertyPath)
        {
            var jsonDocument = JsonDocument.Parse(jsonString);
            var jsonElement = jsonDocument.RootElement;
            
            foreach (var propertyName in propertyPath)
            {
                jsonElement = jsonElement.GetProperty(propertyName);
            }

            return jsonElement.GetString();
        }

        private static string SerializeException(Exception exception, bool writeIndented)
        {
            var exceptionProperties = new Dictionary<string, object>
            {
                {"Type", exception.GetType().ToString()},
                {"Message", exception.Message},
                {"Source", exception.Source},
                {"StackTrace", exception.StackTrace}
            };

            return Serialize(exceptionProperties, writeIndented);
        }
    }
}
