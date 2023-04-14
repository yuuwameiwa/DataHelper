using System.Text.Json;

namespace DataHelper
{
    public static class DataLoader
    {
        public static T TryGetFromFile<T>(string path, Func<string, T> action) where T : class
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("Path is empty.");

            string file = File.ReadAllText(path);
            return TryGetFromJson<T>(file, action);
        }

        public static T TryGetFromJson<T>(string file, Func<string, T> action) where T : class
        {
            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException("File is empty.");

            JsonElement obj = JsonSerializer.Deserialize<JsonElement>(file);
            string name = typeof(T).Name;
            string? connectionString = obj.GetProperty("ConnectionStrings").GetProperty(name).GetString();

            if (connectionString != null)
                return TryGetDatabase<T>(connectionString, action);

            throw new ArgumentNullException("The transmitted <T> is not contained in the file");
        }

        public static T TryGetDatabase<T>(string connectionString, Func<string, T> action) where T : class
        {
            return action(connectionString);
        }
    }
}
