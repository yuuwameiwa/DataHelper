namespace DataHelper.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SqlTableAttribute : Attribute
    {
        public string TableName { get; set; }
        public string? IdentityColumn { get; set; }

        public SqlTableAttribute(string tableName, string identityColumn = null)
        {
            if (tableName == null)
                throw new ArgumentNullException($"{nameof(tableName)} is null");

            TableName = tableName;
            IdentityColumn = identityColumn;
        }
    }
}