using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace OrderService.Helpers
{
    internal static class SQLiteHelper
    {
        // Async open — SQLite connection
        public static async Task<SqliteConnection> OpenAsync(string cs)
        {
            var con = new SqliteConnection(cs);
            await con.OpenAsync();
            return con;
        }

        // Direct SQL query command
        public static SqliteCommand Query(SqliteConnection con, string sql)
        {
            var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            return cmd;
        }
    }
}
