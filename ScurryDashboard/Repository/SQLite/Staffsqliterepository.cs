using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;

namespace OrderService.Repository.SQLite
{
    public class StaffSQLiteRepository : IStaffRepository
    {
        private readonly string _sqliteCs;
        private readonly string _sqlServerCs;

        public StaffSQLiteRepository(IConfiguration cfg)
        {
            _sqliteCs = cfg.GetConnectionString("SQLiteConnection")!;
            _sqlServerCs = cfg.GetConnectionString("ConnStringDb")!;
        }

        // ─── MAP ─────────────────────────────────────────────
        private static Staff MapStaff(SqliteDataReader r)
        {
            
            for (int i = 0; i < r.FieldCount; i++)
                Console.WriteLine($"[{i}] {r.GetName(i)}");

            return new()
            {
                StaffId = r.GetInt32(r.GetOrdinal("StaffId")),
                FullName = r.GetString(r.GetOrdinal("FullName")),
                RoleId = r.IsDBNull(r.GetOrdinal("RoleId"))
                                    ? 0 : r.GetInt32(r.GetOrdinal("RoleId")),
                RoleName = null,
                Phone = r.IsDBNull(r.GetOrdinal("Phone"))
                                    ? null : r.GetString(r.GetOrdinal("Phone")),
                Email = r.IsDBNull(r.GetOrdinal("Email"))
                                    ? null : r.GetString(r.GetOrdinal("Email")),
                CNIC = r.IsDBNull(r.GetOrdinal("CNIC"))
                                    ? null : r.GetString(r.GetOrdinal("CNIC")),
                Department = r.IsDBNull(r.GetOrdinal("Department"))
                                    ? null : r.GetString(r.GetOrdinal("Department")),
                Salary = r.IsDBNull(r.GetOrdinal("Salary"))
                                    ? 0m : r.GetDecimal(r.GetOrdinal("Salary")),
                JoinDate = r.IsDBNull(r.GetOrdinal("JoinDate"))
                                    ? null
                                    : DateTime.Parse(r.GetString(r.GetOrdinal("JoinDate"))),
                IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
                IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
                CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
                ModifiedAt = r.IsDBNull(r.GetOrdinal("ModifiedAt"))
                                    ? null
                                    : DateTime.Parse(r.GetString(r.GetOrdinal("ModifiedAt"))),
                ModifiedBy = r.IsDBNull(r.GetOrdinal("ModifiedBy"))
                                    ? null : r.GetString(r.GetOrdinal("ModifiedBy")),
                AvalFDelivery = r.GetInt32(r.GetOrdinal("AvalfDelivery")) == 1,
                CreatedBy = r.IsDBNull(r.GetOrdinal("CreatedBy"))
                ? null : r.GetInt32(r.GetOrdinal("CreatedBy")),
            };
        }
        // ─── GET ALL ─────────────────────────────────────────
        public async Task<IEnumerable<Staff>> GetAllStaffAsync()
        {
            var list = new List<Staff>();

            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var cmd = SQLiteHelper.Query(con, @"
                SELECT StaffId, FullName, RoleId, Phone, Email,
                       CNIC, Department, Salary, JoinDate,
                       IsActive, IsDeleted, CreatedAt,
                       ModifiedAt, ModifiedBy, AvalfDelivery,CreatedBy
                FROM   Staff
                WHERE  IsDeleted = 0
                ");

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(MapStaff(rdr));

            return list;
        }

        // ─── GET BY ID ───────────────────────────────────────
        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var cmd = SQLiteHelper.Query(con, @"
                SELECT StaffId, FullName, RoleId, Phone, Email,
                       CNIC, Department, Salary, JoinDate,
                       IsActive, IsDeleted, CreatedAt,
                       ModifiedAt, ModifiedBy, AvalfDelivery,CreatedBy
                FROM   Staff
                WHERE  StaffId   = @StaffId
                AND    IsDeleted = 0");

            cmd.Parameters.AddWithValue("@StaffId", staffId);

            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? MapStaff(rdr) : null;
        }

        // ─── INSERT ──────────────────────────────────────────
        public async Task<int> InsertStaffAsync(StaffRequest req)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var sqliteCmd = SQLiteHelper.Query(con, @"
                INSERT INTO Staff
                    (FullName, RoleId, Phone, Email, CNIC,
                     Department, Salary, JoinDate, IsActive,
                     IsDeleted, AvalfDelivery, CreatedAt,
                     ModifiedBy, CreatedBy)
                VALUES
                    (@FullName, @RoleId, @Phone, @Email, @CNIC,
                     @Department, @Salary, @JoinDate, @IsActive,
                     0, 0, datetime('now'),
                     @ModifiedBy, @CreatedBy);
                SELECT last_insert_rowid();");

            sqliteCmd.Parameters.AddWithValue("@FullName",
                req.FullName);
            sqliteCmd.Parameters.AddWithValue("@RoleId",
                req.RoleId);
            sqliteCmd.Parameters.AddWithValue("@Phone",
                (object?)req.Phone ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Email",
                (object?)req.Email ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@CNIC",
                (object?)req.CNIC ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Department",
                (object?)req.Department ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Salary",
                req.Salary);
            sqliteCmd.Parameters.AddWithValue("@JoinDate",
                req.JoinDate.HasValue
                    ? req.JoinDate.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@IsActive",
                req.IsActive ? 1 : 0);
            sqliteCmd.Parameters.AddWithValue("@ModifiedBy",
                req.ModifiedBy);
            sqliteCmd.Parameters.AddWithValue("@CreatedBy",
                (object?)req.CreatedBy ?? DBNull.Value);

            var newId = Convert.ToInt32(
                await sqliteCmd.ExecuteScalarAsync());

            if (IsSqlServerAvailable())
            {
                try
                {
                    await using var sqlCon =
                        new SqlConnection(_sqlServerCs);
                    await sqlCon.OpenAsync();
                    var sqlCmd = DbHelper.Proc(sqlCon, "sp_InsertStaff");
                    sqlCmd.Parameters.AddWithValue("@FullName",
                        req.FullName);
                    sqlCmd.Parameters.AddWithValue("@RoleId",
                        req.RoleId);
                    sqlCmd.Parameters.AddWithValue("@Phone",
                        (object?)req.Phone ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Email",
                        (object?)req.Email ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@CNIC",
                        (object?)req.CNIC ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Department",
                        (object?)req.Department ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Salary",
                        req.Salary);
                    sqlCmd.Parameters.AddWithValue("@JoinDate",
                        (object?)req.JoinDate ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@IsActive",
                        req.IsActive);
                    sqlCmd.Parameters.AddWithValue("@ModifiedBy",
                        req.ModifiedBy);
                    await sqlCmd.ExecuteNonQueryAsync();

                    await LogSyncAsync(con, newId,
                        "INSERT", isSynced: true);
                }
                catch
                {
                    await LogSyncAsync(con, newId,
                        "INSERT", isSynced: false);
                }
            }
            else
            {
                await LogSyncAsync(con, newId,
                    "INSERT", isSynced: false);
            }

            return newId;
        }

        // ─── UPDATE ──────────────────────────────────────────
        public async Task UpdateStaffAsync(int staffId, StaffRequest req)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var sqliteCmd = SQLiteHelper.Query(con, @"
                UPDATE Staff SET
                    FullName   = @FullName,
                    RoleId     = @RoleId,
                    Phone      = @Phone,
                    Email      = @Email,
                    CNIC       = @CNIC,
                    Department = @Department,
                    Salary     = @Salary,
                    JoinDate   = @JoinDate,
                    IsActive   = @IsActive,
                    ModifiedAt = datetime('now'),
                    ModifiedBy = @ModifiedBy
                WHERE StaffId   = @StaffId
                AND   IsDeleted = 0");

            sqliteCmd.Parameters.AddWithValue("@StaffId",
                staffId);
            sqliteCmd.Parameters.AddWithValue("@FullName",
                req.FullName);
            sqliteCmd.Parameters.AddWithValue("@RoleId",
                req.RoleId);
            sqliteCmd.Parameters.AddWithValue("@Phone",
                (object?)req.Phone ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Email",
                (object?)req.Email ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@CNIC",
                (object?)req.CNIC ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Department",
                (object?)req.Department ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Salary",
                req.Salary);
            sqliteCmd.Parameters.AddWithValue("@JoinDate",
                req.JoinDate.HasValue
                    ? req.JoinDate.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@IsActive",
                req.IsActive ? 1 : 0);
            sqliteCmd.Parameters.AddWithValue("@ModifiedBy",
                req.ModifiedBy);

            await sqliteCmd.ExecuteNonQueryAsync();

            if (IsSqlServerAvailable())
            {
                try
                {
                    await using var sqlCon =
                        new SqlConnection(_sqlServerCs);
                    await sqlCon.OpenAsync();
                    var sqlCmd = DbHelper.Proc(sqlCon, "sp_UpdateStaff");
                    sqlCmd.Parameters.AddWithValue("@StaffId",
                        staffId);
                    sqlCmd.Parameters.AddWithValue("@FullName",
                        req.FullName);
                    sqlCmd.Parameters.AddWithValue("@RoleId",
                        req.RoleId);
                    sqlCmd.Parameters.AddWithValue("@Phone",
                        (object?)req.Phone ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Email",
                        (object?)req.Email ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@CNIC",
                        (object?)req.CNIC ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Department",
                        (object?)req.Department ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Salary",
                        req.Salary);
                    sqlCmd.Parameters.AddWithValue("@JoinDate",
                        (object?)req.JoinDate ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@IsActive",
                        req.IsActive);
                    sqlCmd.Parameters.AddWithValue("@ModifiedBy",
                        req.ModifiedBy);
                    await sqlCmd.ExecuteNonQueryAsync();

                    await LogSyncAsync(con, staffId,
                        "UPDATE", isSynced: true);
                }
                catch
                {
                    await LogSyncAsync(con, staffId,
                        "UPDATE", isSynced: false);
                }
            }
            else
            {
                await LogSyncAsync(con, staffId,
                    "UPDATE", isSynced: false);
            }
        }

        // ─── SOFT DELETE ─────────────────────────────────────
        public async Task SoftDeleteStaffAsync(
            int staffId, string modifiedBy)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var sqliteCmd = SQLiteHelper.Query(con, @"
                UPDATE Staff SET
                    IsDeleted  = 1,
                    IsActive   = 0,
                    ModifiedAt = datetime('now'),
                    ModifiedBy = @ModifiedBy
                WHERE StaffId = @StaffId");

            sqliteCmd.Parameters.AddWithValue("@StaffId", staffId);
            sqliteCmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            await sqliteCmd.ExecuteNonQueryAsync();

            if (IsSqlServerAvailable())
            {
                try
                {
                    await using var sqlCon =
                        new SqlConnection(_sqlServerCs);
                    await sqlCon.OpenAsync();
                    var sqlCmd = DbHelper.Proc(
                        sqlCon, "sp_SoftDeleteStaff");
                    sqlCmd.Parameters.AddWithValue(
                        "@StaffId", staffId);
                    sqlCmd.Parameters.AddWithValue(
                        "@ModifiedBy", modifiedBy);
                    await sqlCmd.ExecuteNonQueryAsync();

                    await LogSyncAsync(con, staffId,
                        "DELETE", isSynced: true);
                }
                catch
                {
                    await LogSyncAsync(con, staffId,
                        "DELETE", isSynced: false);
                }
            }
            else
            {
                await LogSyncAsync(con, staffId,
                    "DELETE", isSynced: false);
            }
        }

        // ─── GET LOGS ────────────────────────────────────────
        public async Task<IEnumerable<StaffLog>> GetStaffLogsAsync(
            int? staffId)
        {
            var list = new List<StaffLog>();

            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var sql = staffId.HasValue
                ? @"SELECT LogId, StaffId, Action, OldValues,
                           NewValues, ChangedBy, ChangedAt
                    FROM   StaffLog
                    WHERE  StaffId = @StaffId
                    ORDER  BY ChangedAt DESC"
                : @"SELECT LogId, StaffId, Action, OldValues,
                           NewValues, ChangedBy, ChangedAt
                    FROM   StaffLog
                    ORDER  BY ChangedAt DESC";

            var cmd = SQLiteHelper.Query(con, sql);

            if (staffId.HasValue)
                cmd.Parameters.AddWithValue(
                    "@StaffId", staffId.Value);

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new StaffLog
                {
                    LogId = rdr.GetInt32(0),
                    StaffId = rdr.IsDBNull(1) ? 0
                                    : rdr.GetInt32(1),
                    Action = rdr.IsDBNull(2) ? ""
                                    : rdr.GetString(2),
                    OldValues = rdr.IsDBNull(3) ? null
                                    : rdr.GetString(3),
                    NewValues = rdr.IsDBNull(4) ? null
                                    : rdr.GetString(4),
                    ChangedBy = rdr.IsDBNull(5) ? null
                                    : rdr.GetString(5),
                    ChangedAt = rdr.IsDBNull(6) ? DateTime.Now
                                    : DateTime.Parse(rdr.GetString(6))
                });

            return list;
        }

        // ─── SYNC LOG ─────────────────────────────────────────
        private static async Task LogSyncAsync(
            SqliteConnection con, int recordId,
            string action, bool isSynced = false)
        {
            var cmd = SQLiteHelper.Query(con, @"
                INSERT INTO SyncLog
                    (TableName, RecordId, Action,
                     IsSynced, CreatedAt)
                VALUES
                    ('Staff', @RecordId, @Action,
                     @IsSynced, datetime('now'))");

            cmd.Parameters.AddWithValue("@RecordId", recordId);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@IsSynced", isSynced ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── SQL SERVER CHECK ─────────────────────────────────
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