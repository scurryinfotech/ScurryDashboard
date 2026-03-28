using System.Data;
using Microsoft.Data.SqlClient;

namespace OrderService.Repository.Service
{
    public abstract class SalaryRepoBase
    {
        protected readonly string _cs;

        protected SalaryRepoBase(IConfiguration cfg)
            => _cs = cfg.GetConnectionString("ConnStringDb")!;

        protected SqlCommand Proc(SqlConnection c, string name)
        {
            var cmd = c.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = name;
            return cmd;
        }

        protected static decimal D(SqlDataReader r, string col) =>
            r.IsDBNull(r.GetOrdinal(col)) ? 0m : r.GetDecimal(r.GetOrdinal(col));

        protected static int I(SqlDataReader r, string col) =>
            r.IsDBNull(r.GetOrdinal(col)) ? 0 : r.GetInt32(r.GetOrdinal(col));

        protected static string? S(SqlDataReader r, string col) =>
            r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col));

        protected static DateTime DT(SqlDataReader r, string col) =>
            r.IsDBNull(r.GetOrdinal(col)) ? DateTime.MinValue : r.GetDateTime(r.GetOrdinal(col));

        protected static bool HasCol(SqlDataReader r, string col)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (r.GetName(i).Equals(col, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }
}