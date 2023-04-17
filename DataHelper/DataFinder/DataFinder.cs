using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;

namespace DataHelper.DataFinder
{
    public class DataFinder
    {
        private DataContainer _dataContainer;

        public DataFinder(DataContainer dataContainer)
        {
            _dataContainer = dataContainer;
        }

        /// <summary>
        /// Accepts class. Every property will be used in WHERE.
        /// </summary>
        /// <returns> Returns passed class</returns>
        public T FindInSql<T>(object data) where T : class
        {
            if (_dataContainer.SqlConn == null)
                throw new ArgumentNullException("Sql connection is not established.");

            // Method must have tableName to be able to write to database.
            string tableName = DataFinderService.GetTableNameProperty(data);

            // Create dictionary of properties and values
            Dictionary<string, dynamic> dataDict = DataFinderService.CreateDictionary(data);

            using (_dataContainer.SqlConn)
            {
                SqlCommand sqlCommand = new SqlCommand();

                sqlCommand.Connection = _dataContainer.SqlConn;

                string sqlBuildQuery = "SELECT * FROM " + tableName + " WHERE ";

                List<int> hashList = new List<int>();

                foreach (KeyValuePair<string, dynamic> kvp in dataDict)
                {
                    int hashCode = kvp.Value.GetHashCode();

                    if (hashCode < 0)
                        hashCode *= -1;

                    while (hashList.Contains(hashCode))
                        hashCode++;

                    hashList.Add(hashCode);

                    sqlBuildQuery += kvp.Key + " = @" + hashCode.ToString() + " AND ";

                    sqlCommand.Parameters.AddWithValue(hashCode.ToString(), kvp.Value);
                }

                sqlBuildQuery = sqlBuildQuery.Substring(0, sqlBuildQuery.LastIndexOf("AND")).Trim() + ";";

                sqlCommand.CommandText = sqlBuildQuery;

                // TODO: Bring out to DataReader
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return null;

                    T result = Activator.CreateInstance<T>();

                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string propertyName = reader.GetName(i);
                            PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

                            if (property != null && property.CanWrite)
                            {
                                object value = reader.GetValue(i);
                                property.SetValue(result, value);
                            }
                        }
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Find data by key
        /// </summary>
        public T FindInRedis<T>(string key) where T : class
        {
            IDatabase redisDatabase = _dataContainer.Redis;
            if (redisDatabase.KeyExists(key))
            {
                string keyData = redisDatabase.StringGet(key);
                return JsonSerializer.Deserialize<T>(keyData);
            }
            return null;
        }
    }
}