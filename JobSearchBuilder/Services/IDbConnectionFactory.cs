using System.Data;

namespace JobSearchBuilder.Services
{
    /// <summary>
    /// Abstracts database connection creation so SqlProfileStore is not
    /// coupled to any specific database engine. Swap the implementation
    /// to target a different database (SQL Server, SQLite, etc.).
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>Returns an already-open connection ready for use.</summary>
        IDbConnection CreateOpenConnection();

        /// <summary>
        /// SQL fragment that returns the integer ID of the last inserted row.
        /// It is appended (with a semicolon separator) to the INSERT statement
        /// so that both run in the same batch, keeping SCOPE_IDENTITY() valid.
        /// SQL Server: "SELECT CAST(SCOPE_IDENTITY() AS INT)"
        /// SQLite:     "SELECT last_insert_rowid()"
        /// </summary>
        string LastInsertIdSql { get; }
    }
}
