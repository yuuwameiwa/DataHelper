using System.Reflection;
using DataHelper.Attributes;

namespace DataHelper.DataWriter
{
    public static class DataWriterService
    {
        public static SqlTableAttribute GetSqlAttributes(object data)
        {
            return data.GetType().GetCustomAttribute<SqlTableAttribute>();
        }

        public static void CheckSqlAttributes(object data)
        {
            SqlTableAttribute tableAttribute = GetSqlAttributes(data);

            if (string.IsNullOrEmpty(tableAttribute.TableName))
                throw new ArgumentNullException($"{nameof(data)} cannot have null TableName attribute. Check TableAttribute for more.");
        }

        public static RedisTableAttribute GetRedisAttributes(object data)
        {
            return data.GetType().GetCustomAttribute<RedisTableAttribute>();
        }

        public static void CheckRedisAttributes(object data)
        {
            RedisTableAttribute tableAttribute = GetRedisAttributes(data);

            if (tableAttribute == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
        }

        public static Dictionary<string, dynamic> CreateDictionary(object data)
        {
            SqlTableAttribute tableAttribute = GetSqlAttributes(data);

            // Get properties from model
            PropertyInfo[] properties = data.GetType().GetProperties();

            // Create dictionary
            Dictionary<string, dynamic> propertiesDictionary = new Dictionary<string, dynamic>();
            foreach (PropertyInfo property in properties)
            {
                // Skip Identity Column insertion
                if (tableAttribute.IdentityColumn != null && property.Name == tableAttribute.IdentityColumn)
                    continue;

                propertiesDictionary.Add(property.Name, property.GetValue(data));
            }

            return propertiesDictionary;
        }
    }
}
