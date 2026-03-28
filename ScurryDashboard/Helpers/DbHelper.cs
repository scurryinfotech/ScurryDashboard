using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace OrderService.Helpers
{
    internal static class DbHelper
    {
        // Async open that returns an open SqlConnection
        public static async Task<SqlConnection> OpenAsync(string cs)
        {
            var con = new SqlConnection(cs);
            await con.OpenAsync();
            return con;
        }

        // Create a SqlCommand configured for a stored procedure
        public static SqlCommand Proc(SqlConnection con, string name)
        {
            var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = name;
            return cmd;
        }
    }
}