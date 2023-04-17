using System.Text.Json;
using DataHelper.Attributes;
using Microsoft.Data.SqlClient;

namespace DataHelper.DataWriter
{
    public class DataWriter
    {
        private DataContainer _dataContainer;

        public DataWriter(DataContainer dataContainer)
        {
            _dataContainer = dataContainer;
        }

        /// <summary>
        /// Method allows to write a model to SQL database using metadata defined in model.
        /// </summary>
        public void WriteToSql(object data)
        {
            if (_dataContainer.SqlConn != null)
            {
                SqlCommand sqlCommand = GetSqlCommand(data);
                using (_dataContainer.SqlConn)
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }

            throw new ArgumentNullException("Sql Connection is not established");
        }

        public int WriteToSqlWithIdentity(object data)
        {
            if (_dataContainer.SqlConn != null)
            {
                SqlCommand sqlCommand = GetSqlCommand(data);
                using (_dataContainer.SqlConn)
                {
                    return Convert.ToInt32(sqlCommand.ExecuteScalar());
                }
            }

            throw new ArgumentNullException("Sql Connection is not established");
        }

        private SqlCommand GetSqlCommand(object data)
        {
            // Method must have tableName attribute to be able to write to database.
            DataWriterService.CheckSqlAttributes(data);

            // Create dictionary
            Dictionary<string, dynamic> propertiesDictionary = DataWriterService.CreateDictionary(data);

            // Get Table Attribute
            SqlTableAttribute tableAttribute = DataWriterService.GetSqlAttributes(data);

            // Start building Query
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = _dataContainer.SqlConn;

            string sqlBuildQuery = "INSERT INTO " + tableAttribute.TableName + " (";

            // Add Columns()
            foreach (KeyValuePair<string, dynamic> kvp in propertiesDictionary)
            {
                if (kvp.Value != null)
                    sqlBuildQuery += kvp.Key + ",";
            }

            // Remove last comma and add Values() to query
            sqlBuildQuery = sqlBuildQuery.TrimEnd(',') + ") VALUES (";

            // Create list to check the unique VALUES() hash codes of transferred model values.
            List<int> hashList = new List<int>();

            // Add Values to query()
            foreach (object value in propertiesDictionary.Values)
            {
                if (value != null)
                {
                    int hashCode = value.GetHashCode();

                    // SQL does not allow negative hash codes
                    if (hashCode < 0)
                        hashCode *= -1;

                    // Hash codes must be unique
                    while (hashList.Contains(hashCode))
                        hashCode++;

                    // Add hash code to VALUES() list
                    hashList.Add(hashCode);

                    // Add model value to Parameters
                    sqlCommand.Parameters.AddWithValue(hashCode.ToString(), value);

                    // Continue building VALUES()
                    sqlBuildQuery += "@" + hashCode.ToString() + ",";
                }
            }

            // Remove last comma
            sqlBuildQuery = sqlBuildQuery.TrimEnd(',') + ")";

            // If has an identity column - get an id
            bool hasIdentityColumn = !string.IsNullOrEmpty(tableAttribute.IdentityColumn);
            if (hasIdentityColumn == true)
            {
                sqlBuildQuery += "; SELECT SCOPE_IDENTITY()";
            }

            sqlCommand.CommandText = sqlBuildQuery;

            return sqlCommand;
        }

        /// <summary>
        /// Method allows to write a model to Redis database using metadata. It also can use Id from inserted sql.
        /// </summary>
        public void WriteToRedis(object data)
        {
            if (_dataContainer.Redis == null)
                throw new ArgumentNullException("Redis connection is not established.");

            // Method must have tableName attribute to be able to write to database.
            DataWriterService.CheckRedisAttributes(data);
            RedisTableAttribute tableAttribute = DataWriterService.GetRedisAttributes(data);

            // Get metadata
            string key = tableAttribute.Key.ToLower() + tableAttribute.Identity.ToLower();
            string keyHash = key.GetHashCode().ToString();
            TimeSpan keyExpire = TimeSpan.FromSeconds(tableAttribute.Expire_s);

            // Store data as an object
            string dataJson = JsonSerializer.Serialize(data);

            _dataContainer.Redis.StringSet(keyHash, dataJson);
            _dataContainer.Redis.KeyExpire(keyHash, keyExpire);
        }
    }
}