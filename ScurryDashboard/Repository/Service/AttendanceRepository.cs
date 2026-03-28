using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Repository.Interface;
using System.Data;

namespace OrderService.Repository.Service
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly string _cs;
        public AttendanceRepository(IConfiguration cfg) =>
            _cs = cfg.GetConnectionString("ConnStringDb")!;

        private SqlCommand Proc(SqlConnection con, string name)
        {
            var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = name;
            return cmd;
        }

        private static Attendance MapA(SqlDataReader r) => new()
        {
            AttendanceId = r.GetInt32(r.GetOrdinal("AttendanceId")),
            StaffId = r.GetInt32(r.GetOrdinal("StaffId")),
            FullName = r.IsDBNull(r.GetOrdinal("FullName")) ? null : r.GetString(r.GetOrdinal("FullName")),
            AttendanceDate = r.GetDateTime(r.GetOrdinal("AttendanceDate")),
            Status = r.GetString(r.GetOrdinal("Status")),
            CheckIn = r["CheckIn"] == DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["CheckIn"],
            CheckOut = r["CheckOut"] == DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["CheckOut"],
            OvertimeHours = r.GetDecimal(r.GetOrdinal("OvertimeHours")),
            Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString(r.GetOrdinal("Notes")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        };

        public async Task<IEnumerable<Attendance>> GetByStaffAsync(int staffId, int? month, int? year)
        {
            var list = new List<Attendance>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_GetAttendanceByStaff");
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@Month", (object?)month ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Year", (object?)year ?? DBNull.Value);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) list.Add(MapA(rdr));
            return list;
        }

        public async Task<IEnumerable<DailyAttendanceRow>> GetByDateAsync(string date)
        {
            var list = new List<DailyAttendanceRow>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_GetAttendanceByDate");
            cmd.Parameters.AddWithValue("@AttendanceDate", date);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new DailyAttendanceRow
                {
                    StaffId = rdr.GetInt32(rdr.GetOrdinal("StaffId")),
                    FullName = rdr.GetString(rdr.GetOrdinal("FullName")),
                    RoleName = rdr.IsDBNull(rdr.GetOrdinal("RoleName")) ? null : rdr.GetString(rdr.GetOrdinal("RoleName")),
                    Department = rdr.IsDBNull(rdr.GetOrdinal("Department")) ? null : rdr.GetString(rdr.GetOrdinal("Department")),
                    AttendanceId = rdr.GetInt32(rdr.GetOrdinal("AttendanceId")),
                    Status = rdr.GetString(rdr.GetOrdinal("Status")),
                    CheckIn = rdr.IsDBNull(rdr.GetOrdinal("CheckIn")) ? null : rdr.GetValue(rdr.GetOrdinal("CheckIn"))?.ToString(),
                    CheckOut = rdr.IsDBNull(rdr.GetOrdinal("CheckOut")) ? null : rdr.GetValue(rdr.GetOrdinal("CheckOut"))?.ToString(),
                    OvertimeHours = rdr.GetDecimal(rdr.GetOrdinal("OvertimeHours")),
                    Notes = rdr.IsDBNull(rdr.GetOrdinal("Notes")) ? null : rdr.GetString(rdr.GetOrdinal("Notes")),
                });
            return list;
        }

        public async Task MarkAttendanceAsync(AttendanceRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_InsertAttendance");
            cmd.Parameters.AddWithValue("@StaffId", req.StaffId);
            cmd.Parameters.AddWithValue("@AttendanceDate", req.AttendanceDate);
            cmd.Parameters.AddWithValue("@Status", req.Status);
            cmd.Parameters.AddWithValue("@CheckIn", (object?)req.CheckIn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CheckOut", (object?)req.CheckOut ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OvertimeHours", req.OvertimeHours);
            cmd.Parameters.AddWithValue("@Notes", (object?)req.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task BulkMarkAsync(BulkAttendanceRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_BulkMarkAttendance");
            cmd.Parameters.AddWithValue("@AttendanceDate", req.AttendanceDate);
            cmd.Parameters.AddWithValue("@DefaultStatus", req.DefaultStatus);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<MonthlySummary?> GetMonthlySummaryAsync(int staffId, int month, int year)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = Proc(con, "sp_GetMonthlySummary");
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@Month", month);
            cmd.Parameters.AddWithValue("@Year", year);
            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return null;
            return new MonthlySummary
            {
                StaffId = rdr.GetInt32(rdr.GetOrdinal("StaffId")),
                FullName = rdr.GetString(rdr.GetOrdinal("FullName")),
                BasicSalary = rdr.GetDecimal(rdr.GetOrdinal("BasicSalary")),
                PresentDays = rdr.GetInt32(rdr.GetOrdinal("PresentDays")),
                AbsentDays = rdr.GetInt32(rdr.GetOrdinal("AbsentDays")),
                LeaveDays = rdr.GetInt32(rdr.GetOrdinal("LeaveDays")),
                HalfDays = rdr.GetInt32(rdr.GetOrdinal("HalfDays")),
                TotalOvertimeHours = rdr.GetDecimal(rdr.GetOrdinal("TotalOvertimeHours")),
                TotalMarkedDays = rdr.GetInt32(rdr.GetOrdinal("TotalMarkedDays")),
            };
        }

        public async Task<EmployeeProfile> GetEmployeeProfileAsync(int staffId, int? month, int? year)
        {
            var profile = new EmployeeProfile();
            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();
            var cmd = Proc(con, "sp_GetEmployeeProfile");
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@Month", (object?)month ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Year", (object?)year ?? DBNull.Value);
            await using var rdr = await cmd.ExecuteReaderAsync();

            // ── RS1: Staff ──────────────────────────────────────────────────
            if (await rdr.ReadAsync())
            {
                profile.StaffInfo = new Staff
                {
                    StaffId = rdr.GetInt32(rdr.GetOrdinal("StaffId")),
                    FullName = rdr.IsDBNull(rdr.GetOrdinal("FullName")) ? null : rdr.GetString(rdr.GetOrdinal("FullName")),
                    RoleId = rdr.GetInt32(rdr.GetOrdinal("RoleId")),
                    RoleName = rdr.IsDBNull(rdr.GetOrdinal("RoleName")) ? null : rdr.GetString(rdr.GetOrdinal("RoleName")),
                    Phone = rdr.IsDBNull(rdr.GetOrdinal("Phone")) ? null : rdr.GetString(rdr.GetOrdinal("Phone")),
                    Email = rdr.IsDBNull(rdr.GetOrdinal("Email")) ? null : rdr.GetString(rdr.GetOrdinal("Email")),
                    CNIC = rdr.IsDBNull(rdr.GetOrdinal("CNIC")) ? null : rdr.GetString(rdr.GetOrdinal("CNIC")),
                    Department = rdr.IsDBNull(rdr.GetOrdinal("Department")) ? null : rdr.GetString(rdr.GetOrdinal("Department")),
                    Salary = rdr.IsDBNull(rdr.GetOrdinal("Salary")) ? 0m : rdr.GetDecimal(rdr.GetOrdinal("Salary")),
                    JoinDate = rdr.IsDBNull(rdr.GetOrdinal("JoinDate")) ? null : rdr.GetDateTime(rdr.GetOrdinal("JoinDate")),
                    IsActive = rdr.GetBoolean(rdr.GetOrdinal("IsActive")),
                    CreatedAt = rdr.GetDateTime(rdr.GetOrdinal("CreatedAt"))
                };
            }

            await rdr.NextResultAsync();
            var att = new List<Attendance>();
            while (await rdr.ReadAsync())
                att.Add(new Attendance
                {
                    AttendanceId = rdr.GetInt32(rdr.GetOrdinal("AttendanceId")),
                    StaffId = rdr.GetInt32(rdr.GetOrdinal("StaffId")),
                    AttendanceDate = rdr.GetDateTime(rdr.GetOrdinal("AttendanceDate")),
                    Status = rdr.IsDBNull(rdr.GetOrdinal("Status")) ? null : rdr.GetString(rdr.GetOrdinal("Status")),
                               CheckIn = rdr.IsDBNull(rdr.GetOrdinal("CheckIn"))
                     ? (TimeSpan?)null
                     : rdr.GetTimeSpan(rdr.GetOrdinal("CheckIn")),
                    
                               CheckOut = rdr.IsDBNull(rdr.GetOrdinal("CheckOut"))
                      ? (TimeSpan?)null
                      : rdr.GetTimeSpan(rdr.GetOrdinal("CheckOut")),
                    OvertimeHours = rdr.IsDBNull(rdr.GetOrdinal("OvertimeHours")) ? 0m : rdr.GetDecimal(rdr.GetOrdinal("OvertimeHours")),
                    Notes = rdr.IsDBNull(rdr.GetOrdinal("Notes")) ? null : rdr.GetString(rdr.GetOrdinal("Notes")),
                    IsActive = rdr.GetBoolean(rdr.GetOrdinal("IsActive")),
                    CreatedAt = rdr.GetDateTime(rdr.GetOrdinal("CreatedAt"))
                });
            profile.MonthAttendance = att;

            
            await rdr.NextResultAsync();
            var pays = new List<Payroll>();
            while (await rdr.ReadAsync())
                pays.Add(new Payroll
                {
                    PayrollId = rdr.GetInt32(rdr.GetOrdinal("PayrollId")),
                    StaffId = rdr.GetInt32(rdr.GetOrdinal("StaffId")),
                    PayMonth = rdr.GetInt32(rdr.GetOrdinal("PayMonth")),
                    PayYear = rdr.GetInt32(rdr.GetOrdinal("PayYear")),
                    BasicSalary = rdr.GetDecimal(rdr.GetOrdinal("BasicSalary")),
                    PresentDays = rdr.GetInt32(rdr.GetOrdinal("PresentDays")),
                    AbsentDays = rdr.GetInt32(rdr.GetOrdinal("AbsentDays")),
                    LeaveDays = rdr.GetInt32(rdr.GetOrdinal("LeaveDays")),
                    HalfDays = rdr.GetInt32(rdr.GetOrdinal("HalfDays")),
                    OvertimeHours = rdr.GetDecimal(rdr.GetOrdinal("OvertimeHours")),
                    OvertimeAmount = rdr.GetDecimal(rdr.GetOrdinal("OvertimeAmount")),
                    Deductions = rdr.GetDecimal(rdr.GetOrdinal("Deductions")),
                    NetSalary = rdr.GetDecimal(rdr.GetOrdinal("NetSalary")),
                    Status = rdr.GetString(rdr.GetOrdinal("Status")),
                    PaidOn = rdr.IsDBNull(rdr.GetOrdinal("PaidOn"))
                                     ? null : rdr.GetDateTime(rdr.GetOrdinal("PaidOn")).ToString("yyyy-MM-dd"),
                    CreatedAt = rdr.GetDateTime(rdr.GetOrdinal("CreatedAt"))
                });
            profile.PayrollHistory = pays;

          
            await rdr.NextResultAsync();
            if (await rdr.ReadAsync())
                profile.MonthlySummary = new MonthlySummary
                {
                    StaffId = staffId,
                    PresentDays = rdr.GetInt32(rdr.GetOrdinal("PresentDays")),
                    AbsentDays = rdr.GetInt32(rdr.GetOrdinal("AbsentDays")),
                    LeaveDays = rdr.GetInt32(rdr.GetOrdinal("LeaveDays")),
                    HalfDays = rdr.GetInt32(rdr.GetOrdinal("HalfDays")),
                    TotalOvertimeHours = rdr.GetDecimal(rdr.GetOrdinal("TotalOvertimeHours"))
                };

            return profile;
        }
    }

}
