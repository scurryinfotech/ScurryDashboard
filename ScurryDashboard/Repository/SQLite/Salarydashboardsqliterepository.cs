using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;
using System.Data;

namespace OrderService.Repository.SQLite
{
    public class SalaryDashboardSQLiteRepository : ISalaryDashboardRepository
    {
        private readonly string _sqliteCs;
        private readonly string _sqlServerCs;

        public SalaryDashboardSQLiteRepository(IConfiguration cfg)
        {
            _sqliteCs = cfg.GetConnectionString("SQLiteConnection")!;
            _sqlServerCs = cfg.GetConnectionString("ConnStringDb")!;
        }

        // ─── GET DASHBOARD ────────────────────────────────────
        // Offline mein Staff + SalaryPayments se basic dashboard
        public async Task<IEnumerable<SalaryDashboardRow>> GetDashboardAsync(
            int month, int year)
        {
            var list = new List<SalaryDashboardRow>();

            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var cmd = SQLiteHelper.Query(con, @"
                SELECT s.StaffId, s.FullName, s.Department,
                       s.Salary,
                       COALESCE(SUM(p.Amount), 0) AS PaidThisMonth
                FROM   Staff s
                LEFT JOIN SalaryPayments p
                       ON p.StaffId  = s.StaffId
                       AND strftime('%m', p.PaymentDate) = @Month
                       AND strftime('%Y', p.PaymentDate) = @Year
                WHERE  s.IsDeleted = 0
                GROUP  BY s.StaffId");

            cmd.Parameters.AddWithValue("@Month", month.ToString("D2"));
            cmd.Parameters.AddWithValue("@Year", year.ToString());

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var salary = rdr.IsDBNull(3) ? 0m : rdr.GetDecimal(3);
                var paid = rdr.GetDecimal(4);

                list.Add(new SalaryDashboardRow
                {
                    StaffId = rdr.GetInt32(0),
                    FullName = rdr.GetString(1),
                    Department = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                    BasicSalary = salary,
                    NetSalary = salary,
                    PaidThisMonth = paid,
                    BalanceThisMonth = salary - paid,
                    TotalOutstanding = salary - paid,
                    PayrollStatus = paid >= salary ? "Paid" : "Pending",
                    PresentDays = 0,
                    AbsentDays = 0,
                    OvertimeHours = 0,
                    OvertimeAmount = 0,
                    Deductions = 0,
                });
            }

            return list;
        }

        // ─── GET SUMMARY ─────────────────────────────────────
        public async Task<DashboardSummary?> GetSummaryAsync(int month, int year)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var cmd = SQLiteHelper.Query(con, @"
                SELECT
                    COUNT(DISTINCT s.StaffId)             AS TotalEmployees,
                    COALESCE(SUM(s.Salary), 0)            AS TotalPayroll,
                    COALESCE(SUM(p.PaidAmt), 0)           AS TotalPaid,
                    COALESCE(SUM(s.Salary) -
                             SUM(COALESCE(p.PaidAmt,0)),0) AS TotalPending
                FROM Staff s
                LEFT JOIN (
                    SELECT StaffId,
                           SUM(Amount) AS PaidAmt
                    FROM   SalaryPayments
                    WHERE  strftime('%m', PaymentDate) = @Month
                    AND    strftime('%Y', PaymentDate) = @Year
                    GROUP  BY StaffId
                ) p ON p.StaffId = s.StaffId
                WHERE s.IsDeleted = 0");

            cmd.Parameters.AddWithValue("@Month", month.ToString("D2"));
            cmd.Parameters.AddWithValue("@Year", year.ToString());

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return null;

            return new DashboardSummary
            {
                TotalEmployees = rdr.GetInt32(0),
                TotalPayroll = rdr.GetDecimal(1),
                TotalPaid = rdr.GetDecimal(2),
                TotalPending = rdr.GetDecimal(3),
                PaidCount = 0,
                PartialCount = 0,
                PendingCount = 0
            };
        }

        // ─── GENERATE PAYROLL ─────────────────────────────────
        // Offline mein SQL Server se karna padega
        public async Task<PayrollRecord?> GeneratePayrollAsync(
            GeneratePayrollRequest req)
        {
            if (!IsSqlServerAvailable())
                throw new Exception(
                    "Internet connection required to generate payroll.");

            await using var sqlCon = new SqlConnection(_sqlServerCs);
            await sqlCon.OpenAsync();

            var cmd = Proc(sqlCon, "sp_GeneratePayroll");
            cmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            cmd.Parameters.AddWithValue("@PayMonth", req.Month);
            cmd.Parameters.AddWithValue("@PayYear", req.Year);
            cmd.Parameters.AddWithValue("@OvertimeRatePerHour", req.OvertimeRatePerHour);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return null;

            return new PayrollRecord
            {
                PayrollId = rdr.GetInt32(rdr.GetOrdinal("PayrollId")),
                StaffId = rdr.GetInt32(rdr.GetOrdinal("StaffId")),
                FullName = rdr.IsDBNull(rdr.GetOrdinal("FullName"))
                                 ? null : rdr.GetString(rdr.GetOrdinal("FullName")),
                PayMonth = rdr.GetInt32(rdr.GetOrdinal("PayMonth")),
                PayYear = rdr.GetInt32(rdr.GetOrdinal("PayYear")),
                BasicSalary = rdr.GetDecimal(rdr.GetOrdinal("BasicSalary")),
                NetSalary = rdr.GetDecimal(rdr.GetOrdinal("NetSalary")),
                PresentDays = rdr.GetInt32(rdr.GetOrdinal("PresentDays")),
                AbsentDays = rdr.GetInt32(rdr.GetOrdinal("AbsentDays")),
                Deductions = rdr.GetDecimal(rdr.GetOrdinal("Deductions")),
                Status = rdr.GetString(rdr.GetOrdinal("Status")),
                CreatedAt = rdr.GetDateTime(rdr.GetOrdinal("CreatedAt"))
            };
        }

        
        private static SqlCommand Proc(SqlConnection con, string name)
        {
            var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = name;
            return cmd;
        }

        private bool IsSqlServerAvailable()
        {
            try
            {
                using var con = new SqlConnection(_sqlServerCs);
                con.Open();
                return true;
            }
            catch { return false; }
        }
    }
}