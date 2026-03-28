using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Repository.Interface;

namespace OrderService.Repository.Service
{
    public class SalaryPaymentRepository : SalaryRepoBase, ISalaryPaymentRepository
    {
        public SalaryPaymentRepository(IConfiguration cfg) : base(cfg) { }

        // ── Insert Salary Payment ───────────────────────────────────
        public async Task<string> InsertPaymentAsync(InsertPaymentRequest req)
        {
            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            var cmd = Proc(con, "sp_InsertSalaryPayment");

            cmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            cmd.Parameters.AddWithValue("@Amount", req.Amount);
            cmd.Parameters.AddWithValue("@PaymentDate", req.PaymentDate);
            cmd.Parameters.AddWithValue("@PaymentMethod", req.PaymentMethod);
            cmd.Parameters.AddWithValue("@PaymentType", req.PaymentType);
            cmd.Parameters.AddWithValue("@Reason", (object?)req.Reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", req.CreatedBy);

            await using var rdr = await cmd.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
                return S(rdr, "Message") ?? "Payment processed.";

            return "Payment processed.";
        }

        // ── Get Employee Salary Balance ─────────────────────────────
        public async Task<EmployeeSalaryBalance?> GetBalanceAsync(int staffId)
        {
            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            var cmd = Proc(con, "sp_GetEmployeeSalaryBalance");
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            await using var rdr = await cmd.ExecuteReaderAsync();

            // ── Result Set 1 : Employee Info ─────────────────────────
            if (!await rdr.ReadAsync())
                return null;

            var emp = new EmployeeSalaryBalance
            {
                StaffId = I(rdr, "StaffId"),
                FullName = S(rdr, "FullName") ?? "",
                Department = S(rdr, "Department"),
                BasicSalary = D(rdr, "BasicSalary"),
                IsActive = rdr.GetBoolean(rdr.GetOrdinal("IsActive"))
            };

            // ── Result Set 2 : Payroll Breakdown ─────────────────────
            var payrollBalances = new List<PayrollBalance>();

            await rdr.NextResultAsync();

            while (await rdr.ReadAsync())
            {
                payrollBalances.Add(new PayrollBalance
                {
                    PayrollId = I(rdr, "PayrollId"),
                    PayMonth = I(rdr, "PayMonth"),
                    PayYear = I(rdr, "PayYear"),
                    NetSalary = D(rdr, "NetSalary"),
                    PayrollStatus = S(rdr, "PayrollStatus") ?? "Pending",
                    TotalPaid = D(rdr, "TotalPaid"),
                    Balance = D(rdr, "Balance")
                });
            }

            emp.PayrollBreakdown = payrollBalances;

       
            await rdr.NextResultAsync();

            if (await rdr.ReadAsync())
                emp.TotalOutstanding = D(rdr, "TotalOutstanding");

            return emp;
        }

        // ── Get Employee Payment History ────────────────────────────
        public async Task<EmployeePaymentHistory> GetHistoryAsync(int staffId)
        {
            var result = new EmployeePaymentHistory();

            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            var cmd = Proc(con, "sp_GetEmployeePaymentHistory");
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            await using var rdr = await cmd.ExecuteReaderAsync();

            // ── Result Set 1 : Payroll Records ───────────────────────
            var payrolls = new List<PayrollWithBalance>();

            while (await rdr.ReadAsync())
            {
                payrolls.Add(new PayrollWithBalance
                {
                    PayrollId = I(rdr, "PayrollId"),
                    PayMonth = I(rdr, "PayMonth"),
                    PayYear = I(rdr, "PayYear"),

                    BasicSalary = D(rdr, "BasicSalary"),
                    NetSalary = D(rdr, "NetSalary"),

                    OvertimeAmount = D(rdr, "OvertimeAmount"),
                    Deductions = D(rdr, "Deductions"),

                    PayrollStatus = S(rdr, "PayrollStatus") ?? "Pending",

                    CreatedAt = DT(rdr, "CreatedAt"),

                    TotalPaid = D(rdr, "TotalPaid"),
                    Balance = D(rdr, "Balance")
                });
            }

            result.Payrolls = payrolls;

            // ── Result Set 2 : Payment Transactions ──────────────────
            var payments = new List<SalaryPayment>();

            await rdr.NextResultAsync();

            while (await rdr.ReadAsync())
            {
                payments.Add(new SalaryPayment
                {
                    PaymentId = I(rdr, "PaymentId"),
                    StaffId = staffId,

                    PayrollId = rdr.IsDBNull(rdr.GetOrdinal("PayrollId"))
                        ? null
                        : I(rdr, "PayrollId"),

                    Amount = D(rdr, "Amount"),

                    PaymentDate = rdr
                        .GetDateTime(rdr.GetOrdinal("PaymentDate"))
                        .ToString("yyyy-MM-dd"),

                    PaymentMethod = S(rdr, "PaymentMethod") ?? "",
                    PaymentType = S(rdr, "PaymentType") ?? "",

                    Reason = S(rdr, "Reason"),
                    Description = S(rdr, "Description"),

                    CreatedAt = DT(rdr, "CreatedAt"),
                    CreatedBy = S(rdr, "CreatedBy"),

                    PayMonth = rdr.IsDBNull(rdr.GetOrdinal("PayMonth"))
                        ? null
                        : I(rdr, "PayMonth"),

                    PayYear = rdr.IsDBNull(rdr.GetOrdinal("PayYear"))
                        ? null
                        : I(rdr, "PayYear")
                });
            }

            result.Payments = payments;

            return result;
        }
    }
}