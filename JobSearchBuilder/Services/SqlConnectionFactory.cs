using Microsoft.Data.SqlClient;
using System.Data;

namespace JobSearchBuilder.Services
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateOpenConnection()
        {
            SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public string LastInsertIdSql => "SELECT CAST(SCOPE_IDENTITY() AS INT)";
    }
}
