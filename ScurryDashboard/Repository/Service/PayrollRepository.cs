using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Repository.Interface;
using System.Data;

namespace OrderService.Repository.Service
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly string _cs;
        public PayrollRepository(IConfiguration cfg) =>
            _cs = cfg.GetConnectionString("ConnStringDb")!;

        private SqlCommand Proc(SqlConnection con, string name)
        { var cmd = con.CreateCommand(); cmd.CommandType = CommandType.StoredProcedure; cmd.CommandText = name; return cmd; }

        private static Payroll MapP(SqlDataReader r) => new()
        {
            PayrollId = r.GetInt32(r.GetOrdinal("PayrollId")),
            StaffId = r.GetInt32(r.GetOrdinal("StaffId")),
            FullName = r.IsDBNull(r.GetOrdinal("FullName")) ? null : r.GetString(r.GetOrdinal("FullName")),
            PayMonth = r.GetInt32(r.GetOrdinal("PayMonth")),
            PayYear = r.GetInt32(r.GetOrdinal("PayYear")),
            BasicSalary = r.GetDecimal(r.GetOrdinal("BasicSalary")),
            PresentDays = r.GetInt32(r.GetOrdinal("PresentDays")),
            AbsentDays = r.GetInt32(r.GetOrdinal("AbsentDays")),
            LeaveDays = r.GetInt32(r.GetOrdinal("LeaveDays")),
            HalfDays = r.GetInt32(r.GetOrdinal("HalfDays")),
            OvertimeHours = r.GetDecimal(r.GetOrdinal("OvertimeHours")),
            OvertimeAmount = r.GetDecimal(r.GetOrdinal("OvertimeAmount")),
            Deductions = r.GetDecimal(r.GetOrdinal("Deductions")),
            NetSalary = r.GetDecimal(r.GetOrdinal("NetSalary")),
            Status = r.GetString(r.GetOrdinal("Status")),
            PaidOn = r.IsDBNull(r.GetOrdinal("PaidOn")) ? null : r.GetDateTime(r.GetOrdinal("PaidOn")).ToString("yyyy-MM-dd"),
            Remarks = r.IsDBNull(r.GetOrdinal("Remarks")) ? null : r.GetString(r.GetOrdinal("Remarks")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        };

        public async Task<IEnumerable<Payroll>> GetByStaffAsync(int staffId, int? month, int? year)
        {
            var list = new List<Payroll>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_GetPayrollByStaff");
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@Month", (object?)month ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Year", (object?)year ?? DBNull.Value);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) list.Add(MapP(rdr));
            return list;
        }

        public async Task<Payroll?> GenerateAsync(GeneratePayrollRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_GeneratePayroll");
            cmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            cmd.Parameters.AddWithValue("@Month", req.Month);
            cmd.Parameters.AddWithValue("@Year", req.Year);
            cmd.Parameters.AddWithValue("@OvertimeRatePerHour", req.OvertimeRatePerHour);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? MapP(rdr) : null;
        }

        public async Task MarkPaidAsync(int payrollId, string modifiedBy)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_MarkPayrollPaid");
            cmd.Parameters.AddWithValue("@PayrollId", payrollId);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
