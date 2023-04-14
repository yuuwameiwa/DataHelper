namespace DataHelper
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string TableName { get; set; }
        public string? IdentityColumn { get; set; }

        public TableAttribute(string tableName, string identityColumn = null)
        {
            if (tableName == null)
                throw new ArgumentNullException($"{nameof(tableName)} is null");

            TableName = tableName;
            IdentityColumn = identityColumn;
        }
    }
}