using System.IO;
using System.Text.Json;

namespace DotLauncher.Utils
{
    public static class JsonUtils
    {
        public static string Serialize<T>(T obj)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            return JsonSerializer.Serialize(obj, options);
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
    }
}
