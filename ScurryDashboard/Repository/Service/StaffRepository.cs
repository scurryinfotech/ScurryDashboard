using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Repository.Interface;
using OrderService.Helpers;

namespace OrderService.Repository.Service
{
    public class StaffRepository : IStaffRepository
    {
        private readonly string _cs;
        public StaffRepository(IConfiguration cfg) =>
            _cs = cfg.GetConnectionString("ConnStringDb")!;

        private static Staff MapStaff(SqlDataReader r) => new()
        {
            StaffId = r.GetInt32(r.GetOrdinal("StaffId")),
            FullName = r.GetString(r.GetOrdinal("FullName")),
            RoleId = r.GetInt32(r.GetOrdinal("RoleId")),
            RoleName = r.IsDBNull(r.GetOrdinal("RoleName")) ? null : r.GetString(r.GetOrdinal("RoleName")),
            Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
            Email = r.IsDBNull(r.GetOrdinal("Email")) ? null : r.GetString(r.GetOrdinal("Email")),
            CNIC = r.IsDBNull(r.GetOrdinal("CNIC")) ? null : r.GetString(r.GetOrdinal("CNIC")),
            Department = r.IsDBNull(r.GetOrdinal("Department")) ? null : r.GetString(r.GetOrdinal("Department")),
            Salary = r.GetDecimal(r.GetOrdinal("Salary")),
            JoinDate = r.IsDBNull(r.GetOrdinal("JoinDate")) ? null : r.GetDateTime(r.GetOrdinal("JoinDate")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            IsDeleted = r.GetBoolean(r.GetOrdinal("IsDeleted")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            AvalFDelivery = r.GetBoolean(r.GetOrdinal("AvalfDelivery")),
        };

        public async Task<IEnumerable<Staff>> GetAllStaffAsync()
        {
            try
            {
                var list = new List<Staff>();
                await using var con = new SqlConnection(_cs);
                await con.OpenAsync();

                var cmd = DbHelper.Proc(con, "sp_GetAllStaff");
                await using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                    list.Add(MapStaff(rdr));

                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_GetStaffById");
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? MapStaff(rdr) : null;
        }

        public async Task<int> InsertStaffAsync(StaffRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_InsertStaff");
            cmd.Parameters.AddWithValue("@FullName", req.FullName);
            cmd.Parameters.AddWithValue("@RoleId", req.RoleId);
            cmd.Parameters.AddWithValue("@Phone", (object?)req.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object?)req.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CNIC", (object?)req.CNIC ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Department", (object?)req.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Salary", req.Salary);
            cmd.Parameters.AddWithValue("@JoinDate", (object?)req.JoinDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            await cmd.ExecuteNonQueryAsync();
            return 1;
        }

        public async Task UpdateStaffAsync(int staffId, StaffRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_UpdateStaff");
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FullName", req.FullName);
            cmd.Parameters.AddWithValue("@RoleId", req.RoleId);
            cmd.Parameters.AddWithValue("@Phone", (object?)req.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object?)req.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CNIC", (object?)req.CNIC ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Department", (object?)req.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Salary", req.Salary);
            cmd.Parameters.AddWithValue("@JoinDate", (object?)req.JoinDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteStaffAsync(int staffId, string modifiedBy)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_SoftDeleteStaff");
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<StaffLog>> GetStaffLogsAsync(int? staffId)
        {
            var list = new List<StaffLog>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_GetStaffLogs");
            cmd.Parameters.AddWithValue("@StaffId", (object?)staffId ?? DBNull.Value);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new StaffLog
                {
                    LogId = rdr.GetInt32(0),
                    StaffId = rdr.GetInt32(1),
                    Action = rdr.GetString(2),
                    OldValues = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    NewValues = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    ChangedBy = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    ChangedAt = rdr.GetDateTime(6)
                });
            return list;
        }
    }

}
