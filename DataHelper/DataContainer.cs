using Microsoft.Data.SqlClient;
using StackExchange.Redis;

namespace DataHelper
{
    public class DataContainer
    {
        public SqlConnection SqlConn { get; set; }
        public IDatabase? Redis { get; set; }
    }
}
