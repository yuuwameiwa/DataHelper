using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataHelper.DataFinder
{
    public static class DataFinderService
    {
        /// <summary>
        /// Get TableName property from SqlAttribute.
        /// </summary>
        public static string GetTableNameProperty(object data)
        {
            PropertyInfo tableNameProperty = data.GetType().GetProperty("TableName");

            if (tableNameProperty == null)
                throw new ArgumentNullException($"Object {nameof(data)} does not contain TableName property");

            return tableNameProperty.GetValue(data).ToString();
        }

        /// <summary>
        /// Create Dictionary based on model.
        /// </summary>
        public static Dictionary<string, dynamic> CreateDictionary(object data)
        {
            Dictionary<string, dynamic> dictionary = new Dictionary<string, dynamic>();

            PropertyInfo[] properties = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo property in properties)
            {
                // Ignore TableName property
                if (property.Name != "TableName")
                    dictionary[property.Name] = property.GetValue(data);
            }

            return dictionary;
        }
    }
}
