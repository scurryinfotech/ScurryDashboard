using Microsoft.Data.Sqlite;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;

namespace OrderService.Repository.SQLite
{
    public class PayrollSQLiteRepository : IPayrollRepository
    {
        private readonly string _sqliteCs;

        public PayrollSQLiteRepository(IConfiguration cfg)
        {
            _sqliteCs = cfg.GetConnectionString("SQLiteConnection")!;
        }

        // ─── MAP ─────────────────────────────────────────────
        private static Payroll MapP(SqliteDataReader r) => new()
        {
            PayrollId = r.GetInt32(r.GetOrdinal("PayrollId")),
            StaffId = r.GetInt32(r.GetOrdinal("StaffId")),
            FullName = r.IsDBNull(r.GetOrdinal("FullName"))
                                ? null : r.GetString(r.GetOrdinal("FullName")),
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
            PaidOn = r.IsDBNull(r.GetOrdinal("PaidOn"))
                                ? null : r.GetString(r.GetOrdinal("PaidOn")),
            Remarks = r.IsDBNull(r.GetOrdinal("Remarks"))
                                ? null : r.GetString(r.GetOrdinal("Remarks")),
            CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt")))
        };

        // ─── GET BY STAFF ─────────────────────────────────────
        public async Task<IEnumerable<Payroll>> GetByStaffAsync(
            int staffId, int? month, int? year)
        {
            var list = new List<Payroll>();

            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var sql = @"
                SELECT p.PayrollId, p.StaffId, s.FullName,
                       p.PayMonth, p.PayYear, p.BasicSalary,
                       p.PresentDays, p.AbsentDays, p.LeaveDays,
                       p.HalfDays, p.OvertimeHours, p.OvertimeAmount,
                       p.Deductions, p.NetSalary, p.Status,
                       p.PaidOn, p.Remarks, p.CreatedAt
                FROM   Payroll p
                LEFT JOIN Staff s ON s.StaffId = p.StaffId
                WHERE  p.StaffId = @StaffId";

            if (month.HasValue)
                sql += " AND p.PayMonth = @Month AND p.PayYear = @Year";

            sql += " ORDER BY p.PayYear DESC, p.PayMonth DESC";

            var cmd = SQLiteHelper.Query(con, sql);
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            if (month.HasValue)
            {
                cmd.Parameters.AddWithValue("@Month", month.Value);
                cmd.Parameters.AddWithValue("@Year", year ?? DateTime.Now.Year);
            }

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(MapP(rdr));

            return list;
        }

        // ─── GENERATE ─────────────────────────────────────────
        public async Task<Payroll?> GenerateAsync(GeneratePayrollRequest req)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            // ── 1. Pull attendance summary for this staff/month/year
            var attCmd = SQLiteHelper.Query(con, @"
                SELECT
                    COUNT(CASE WHEN Status = 'Present'  THEN 1 END) AS PresentDays,
                    COUNT(CASE WHEN Status = 'Absent'   THEN 1 END) AS AbsentDays,
                    COUNT(CASE WHEN Status = 'Leave'    THEN 1 END) AS LeaveDays,
                    COUNT(CASE WHEN Status = 'HalfDay'  THEN 1 END) AS HalfDays,
                    IFNULL(SUM(OvertimeHours), 0)                   AS OvertimeHours
                FROM Attendance
                WHERE StaffId  = @StaffId
                AND   strftime('%m', Date) = printf('%02d', @Month)
                AND   strftime('%Y', Date) = CAST(@Year AS TEXT)");

            attCmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            attCmd.Parameters.AddWithValue("@Month", req.Month);
            attCmd.Parameters.AddWithValue("@Year", req.Year);

            int presentDays = 0, absentDays = 0, leaveDays = 0, halfDays = 0;
            decimal overtimeHours = 0;

            await using (var attRdr = await attCmd.ExecuteReaderAsync())
            {
                if (await attRdr.ReadAsync())
                {
                    presentDays = attRdr.IsDBNull(0) ? 0 : attRdr.GetInt32(0);
                    absentDays = attRdr.IsDBNull(1) ? 0 : attRdr.GetInt32(1);
                    leaveDays = attRdr.IsDBNull(2) ? 0 : attRdr.GetInt32(2);
                    halfDays = attRdr.IsDBNull(3) ? 0 : attRdr.GetInt32(3);
                    overtimeHours = attRdr.IsDBNull(4) ? 0 : attRdr.GetDecimal(4);
                }
            }

            // No attendance data → return null (same as SQL Server version)
            if (presentDays == 0 && absentDays == 0 && leaveDays == 0 && halfDays == 0)
                return null;

            // ── 2. Pull basic salary from Staff table
            var salCmd = SQLiteHelper.Query(con,
                "SELECT Salary FROM Staff WHERE StaffId = @StaffId AND IsDeleted = 0");
            salCmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            var basicSalary = Convert.ToDecimal(await salCmd.ExecuteScalarAsync() ?? 0m);

            // ── 3. Calculate
            var overtimeAmount = overtimeHours * req.OvertimeRatePerHour;
            var perDayRate = basicSalary / 26;                        // 26 working days
            var deductions = (absentDays + (halfDays * 0.5m)) * perDayRate;
            var netSalary = basicSalary + overtimeAmount - deductions;

            // ── 4. Insert payroll record
            var insCmd = SQLiteHelper.Query(con, @"
                INSERT INTO Payroll
                    (StaffId, PayMonth, PayYear, BasicSalary,
                     PresentDays, AbsentDays, LeaveDays, HalfDays,
                     OvertimeHours, OvertimeAmount, Deductions,
                     NetSalary, Status, CreatedAt, ModifiedBy)
                VALUES
                    (@StaffId, @Month, @Year, @BasicSalary,
                     @PresentDays, @AbsentDays, @LeaveDays, @HalfDays,
                     @OvertimeHours, @OvertimeAmount, @Deductions,
                     @NetSalary, 'Pending', datetime('now'), @ModifiedBy);
                SELECT last_insert_rowid();");

            insCmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            insCmd.Parameters.AddWithValue("@Month", req.Month);
            insCmd.Parameters.AddWithValue("@Year", req.Year);
            insCmd.Parameters.AddWithValue("@BasicSalary", basicSalary);
            insCmd.Parameters.AddWithValue("@PresentDays", presentDays);
            insCmd.Parameters.AddWithValue("@AbsentDays", absentDays);
            insCmd.Parameters.AddWithValue("@LeaveDays", leaveDays);
            insCmd.Parameters.AddWithValue("@HalfDays", halfDays);
            insCmd.Parameters.AddWithValue("@OvertimeHours", overtimeHours);
            insCmd.Parameters.AddWithValue("@OvertimeAmount", overtimeAmount);
            insCmd.Parameters.AddWithValue("@Deductions", deductions);
            insCmd.Parameters.AddWithValue("@NetSalary", netSalary);
            insCmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);

            var newId = Convert.ToInt32(await insCmd.ExecuteScalarAsync());

            // ── 5. Return the generated record
            return new Payroll
            {
                PayrollId = newId,
                StaffId = req.StaffId,
                PayMonth = req.Month,
                PayYear = req.Year,
                BasicSalary = basicSalary,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LeaveDays = leaveDays,
                HalfDays = halfDays,
                OvertimeHours = overtimeHours,
                OvertimeAmount = overtimeAmount,
                Deductions = deductions,
                NetSalary = netSalary,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
        }

        // ─── MARK PAID ────────────────────────────────────────
        public async Task MarkPaidAsync(int payrollId, string modifiedBy)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var cmd = SQLiteHelper.Query(con, @"
                UPDATE Payroll SET
                    Status     = 'Paid',
                    PaidOn     = date('now'),
                    ModifiedBy = @ModifiedBy
                WHERE PayrollId = @PayrollId");

            cmd.Parameters.AddWithValue("@PayrollId", payrollId);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}