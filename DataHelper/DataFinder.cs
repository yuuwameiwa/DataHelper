using System.Reflection;
using Microsoft.Data.SqlClient;

namespace DataHelper
{
    public class DataFinder
    {
        private DataContainer _dataContainer;

        public DataFinder(DataContainer dataContainer)
        {
            _dataContainer = dataContainer;
        }

        public T FindInSql<T>(object data) where T : class
        {
            if (_dataContainer.SqlConn == null)
                throw new ArgumentNullException("Sql connection is not established.");

            if (data == null)
                throw new ArgumentNullException($"Object {nameof(data)} is null");

            // Method must have tableName to be able to write to database.
            PropertyInfo tableNameProperty = data.GetType().GetProperty("TableName");
            if (tableNameProperty == null)
                throw new ArgumentNullException($"Object {nameof(data)} does not contain TableName property");
            string tableName = tableNameProperty.GetValue(data).ToString();

            // Create dictionary of properties and values
            Dictionary<string, dynamic> modelDict = new Dictionary<string, dynamic>();
            PropertyInfo[] properties = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                modelDict[property.Name] = property.GetValue(data);
            }

            using (_dataContainer.SqlConn)
            {
                SqlCommand sqlCommand = new SqlCommand();

                sqlCommand.Connection = _dataContainer.SqlConn;

                string sqlBuildQuery = "SELECT * FROM " + tableName + " WHERE ";

                List<int> hashList = new List<int>();

                foreach (KeyValuePair<string, dynamic> kvp in modelDict)
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
    }
}
