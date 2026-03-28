using Microsoft.Data.SqlClient;
using OrderService.Model;

using OrderService.Repository.Interface;
using Twilio.TwiML.Messaging;


namespace OrderService.Repository.Service
{
    public class SalaryDashboardRepository : SalaryRepoBase, ISalaryDashboardRepository
    {
        public SalaryDashboardRepository(IConfiguration cfg) : base(cfg) { }

        // ── Get Salary Dashboard ─────────────────────────────────────
        public async Task<IEnumerable<SalaryDashboardRow>> GetDashboardAsync(int month, int year)
        {
            var list = new List<SalaryDashboardRow>();

            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            var cmd = Proc(con, "sp_GetSalaryDashboard");
            cmd.Parameters.AddWithValue("@PayMonth", month);
            cmd.Parameters.AddWithValue("@PayYear", year);

            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                list.Add(new SalaryDashboardRow
                {
                    StaffId = I(rdr, "StaffId"),
                    FullName = S(rdr, "FullName") ?? "",
                    Department = S(rdr, "Department"),

                    BasicSalary = D(rdr, "BasicSalary"),
                    PayrollId = rdr.IsDBNull(rdr.GetOrdinal("PayrollId"))
                        ? null
                        : I(rdr, "PayrollId"),

                    NetSalary = D(rdr, "NetSalary"),

                    WorkingDays = HasCol(rdr, "WorkingDays")
                        ? I(rdr, "WorkingDays")
                        : 0,

                    PresentDays = I(rdr, "PresentDays"),
                    AbsentDays = I(rdr, "AbsentDays"),

                    OvertimeHours = D(rdr, "OvertimeHours"),
                    OvertimeAmount = D(rdr, "OvertimeAmount"),

                    Deductions = D(rdr, "Deductions"),

                    PayrollStatus = S(rdr, "PayrollStatus") ?? "NotGenerated",

                    PaidThisMonth = D(rdr, "PaidThisMonth"),
                    AdvanceThisMonth = D(rdr, "AdvanceThisMonth"),
                    BalanceThisMonth = D(rdr, "BalanceThisMonth"),

                    TotalOutstanding = D(rdr, "TotalOutstanding")
                });
            }

            return list;
        }

        // ── Dashboard Summary ───────────────────────────────────────
        public async Task<DashboardSummary?> GetSummaryAsync(int month, int year)
        {
            try
            {
                await using var con = new SqlConnection(_cs);
                await con.OpenAsync();

                var cmd = Proc(con, "sp_GetSalaryDashboardSummary");

                cmd.Parameters.AddWithValue("@PayMonth", month);
                cmd.Parameters.AddWithValue("@PayYear", year);

                await using var rdr = await cmd.ExecuteReaderAsync();

                if (!await rdr.ReadAsync())
                    return null;

                return new DashboardSummary
                {
                    TotalEmployees = I(rdr, "TotalEmployees"),
                    TotalPayroll = D(rdr, "TotalPayroll"),
                    TotalPaid = D(rdr, "TotalPaid"),
                    TotalPending = D(rdr, "TotalPending"),

                    PaidCount = I(rdr, "PaidCount"),
                    PartialCount = I(rdr, "PartialCount"),
                    PendingCount = I(rdr, "PendingCount")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        // ── Generate Payroll ────────────────────────────────────────
        public async Task<PayrollRecord?> GeneratePayrollAsync(GeneratePayrollRequest req)
        {
            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            var cmd = Proc(con, "sp_GeneratePayroll");

            cmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            cmd.Parameters.AddWithValue("@PayMonth", req.Month);
            cmd.Parameters.AddWithValue("@PayYear", req.Year);
            cmd.Parameters.AddWithValue("@OvertimeRatePerHour", req.OvertimeRatePerHour);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);

            await using var rdr = await cmd.ExecuteReaderAsync();

            if (!await rdr.ReadAsync())
                return null;

            return new PayrollRecord
            {
                PayrollId = I(rdr, "PayrollId"),
                StaffId = I(rdr, "StaffId"),
                FullName = S(rdr, "FullName"),

                PayMonth = I(rdr, "PayMonth"),
                PayYear = I(rdr, "PayYear"),

                BasicSalary = D(rdr, "BasicSalary"),

                WorkingDays = HasCol(rdr, "WorkingDays")
                    ? I(rdr, "WorkingDays")
                    : 0,

                PresentDays = I(rdr, "PresentDays"),
                AbsentDays = I(rdr, "AbsentDays"),
                LeaveDays = I(rdr, "LeaveDays"),
                HalfDays = I(rdr, "HalfDays"),

                OvertimeHours = D(rdr, "OvertimeHours"),
                OvertimeAmount = D(rdr, "OvertimeAmount"),

                Deductions = D(rdr, "Deductions"),

                NetSalary = D(rdr, "NetSalary"),

                Status = S(rdr, "Status") ?? "Pending",

                PaidOn = rdr.IsDBNull(rdr.GetOrdinal("PaidOn"))
                    ? null
                    : rdr.GetDateTime(rdr.GetOrdinal("PaidOn")).ToString("yyyy-MM-dd"),

                CreatedAt = DT(rdr, "CreatedAt")
            };
        }
    }
}