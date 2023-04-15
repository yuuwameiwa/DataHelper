﻿using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace DataHelper
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
            SqlCommand sqlCommand = GetSqlCommand(data);
            using (_dataContainer.SqlConn)
                sqlCommand.ExecuteNonQuery();
        }

        public int WriteToSqlWithIdentity(object data)
        {
            SqlCommand sqlCommand = GetSqlCommand(data);
            using (_dataContainer.SqlConn)
                return Convert.ToInt32(sqlCommand.ExecuteScalar());
        }

        private SqlCommand GetSqlCommand(object data)
        {
            // Method must have tableName attribute to be able to write to database.
            Type modelType = data.GetType();
            TableAttribute tableAttribute = modelType.GetCustomAttribute<TableAttribute>();
            if (string.IsNullOrEmpty(tableAttribute.TableName))
                throw new ArgumentNullException($"У {nameof(data)} не может быть значения null атрибута TableName");

            // Get properties from model
            PropertyInfo[] properties = data.GetType().GetProperties();

            // Create dictionary
            Dictionary<string, dynamic> propertiesDictionary = new Dictionary<string, dynamic>();
            foreach (PropertyInfo property in properties)
            {
                if (tableAttribute.IdentityColumn != null && property.Name == tableAttribute.IdentityColumn)
                    continue;

                propertiesDictionary.Add(property.Name, property.GetValue(data));
            }

            SqlCommand sqlCommand = new SqlCommand();

            sqlCommand.Connection = _dataContainer.SqlConn;

            string sqlBuildQuery = "INSERT INTO " + tableAttribute.TableName + " (";

            // Добавить COLUMNS в запрос
            foreach (KeyValuePair<string, dynamic> kvp in propertiesDictionary)
            {
                if (kvp.Value != null)
                    sqlBuildQuery += kvp.Key + ",";
            }

            // Убрать последнюю запятую и добавить VALUES
            sqlBuildQuery = sqlBuildQuery.TrimEnd(',') + ") VALUES (";

            // Create list to check the unique VALUES() hash codes of transferred model values.
            List<int> hashList = new List<int>();

            // Добавить VALUES в запрос
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

            // If has identity column - get an id
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

            if (data == null)
                throw new ArgumentNullException($"Object {nameof(data)} is null");

            // Method must have tableName attribute to be able to write to database.
            Type modelType = data.GetType();
            TableAttribute tableAttribute = modelType.GetCustomAttribute<TableAttribute>();
            if (string.IsNullOrEmpty(tableAttribute.TableName))
                throw new ArgumentNullException($"Model {nameof(data)} must have TableName attribute");

            if (!string.IsNullOrEmpty(tableAttribute.IdentityColumn))
            {
                int identityColumnValue = (int)data.GetType().GetProperty(tableAttribute.IdentityColumn).GetValue(data);
                string key = tableAttribute.TableName.ToLower() + identityColumnValue.ToString();
                string keyHash = key.GetHashCode().ToString();

                string dataJson = JsonSerializer.Serialize(data);

                _dataContainer.Redis.StringSet(keyHash, dataJson);
            }
        }
    }
}