using DatabaseHelper.Common;
using System.Data.SqlClient;

namespace DatabaseHelper
{
    public sealed class MsSqlDatabase : Database<SqlConnection, SqlCommand, SqlDataReader>
    {
        public MsSqlDatabase(string connectionString) : base(connectionString) { }
    }
}