using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;

namespace OrderService.Repository.Service
{
    public class ShopExpenseSQLiteRepository : IShopExpenseRepository
    {
        private readonly string _sqliteCs;
        private readonly string _sqlServerCs;

        // Serializes all writes — prevents "database is locked"
        private static readonly SemaphoreSlim _writeLock = new(1, 1);

        // ── SQL Server availability cache (TTL = 30 s) ──────────────────
        private static bool _sqlAvailable = false;
        private static DateTime _sqlCheckedAt = DateTime.MinValue;
        private static readonly SemaphoreSlim _sqlCheckLock = new(1, 1);
        private const int SQL_CHECK_TTL_SECONDS = 30;

        public ShopExpenseSQLiteRepository(IConfiguration cfg)
        {
            _sqliteCs = cfg.GetConnectionString("SQLiteConnection")!;
            _sqlServerCs = cfg.GetConnectionString("ConnStringDb")!;
        }

        // ── Open connection with WAL + busy_timeout applied once ────────
        private async Task<SqliteConnection> OpenSQLiteAsync()
        {
            var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            // WAL mode allows concurrent reads during writes.
            // busy_timeout makes SQLite retry instead of immediately throwing Error 5.
            var pragma = con.CreateCommand();
            pragma.CommandText = @"
                PRAGMA journal_mode  = WAL;
                PRAGMA busy_timeout  = 5000;
                PRAGMA synchronous   = NORMAL;";
            await pragma.ExecuteNonQueryAsync();

            return con;
        }

        // ── SQL Server availability — cached, async ─────────────────────
        // FIX: was synchronous (con.Open()), blocking thread pool on every write
        private async Task<bool> IsSqlServerAvailableAsync()
        {
            // Return cached result if still fresh
            if ((DateTime.UtcNow - _sqlCheckedAt).TotalSeconds < SQL_CHECK_TTL_SECONDS)
                return _sqlAvailable;

            await _sqlCheckLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if ((DateTime.UtcNow - _sqlCheckedAt).TotalSeconds < SQL_CHECK_TTL_SECONDS)
                    return _sqlAvailable;

                try
                {
                    await using var con = new SqlConnection(_sqlServerCs);
                    await con.OpenAsync();           // async — does not block thread pool
                    _sqlAvailable = true;
                }
                catch
                {
                    _sqlAvailable = false;
                }

                _sqlCheckedAt = DateTime.UtcNow;
                return _sqlAvailable;
            }
            finally
            {
                _sqlCheckLock.Release();
            }
        }

        // ── MAP ─────────────────────────────────────────────────────────
        private static ShopExpense Map(SqliteDataReader r) => new()
        {
            ExpenseId = r.GetInt32(r.GetOrdinal("ExpenseId")),
            Title = r.GetString(r.GetOrdinal("Title")),
            Category = r.IsDBNull(r.GetOrdinal("Category")) ? null : r.GetString(r.GetOrdinal("Category")),
            Amount = r.GetDecimal(r.GetOrdinal("Amount")),
            ExpenseDate = DateTime.Parse(r.GetString(r.GetOrdinal("ExpenseDate"))),
            Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
            IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
            IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
            CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
            PaymentMode = r.GetString(r.GetOrdinal("PaymentMode")),
        };

        // ── GET ALL ─────────────────────────────────────────────────────
        public async Task<IEnumerable<ShopExpense>> GetAllAsync()
        {
            var list = new List<ShopExpense>();

            await using var con = await OpenSQLiteAsync();
            var cmd = SQLiteHelper.Query(con, @"
                SELECT ExpenseId, Title, Category, Amount,
                       ExpenseDate, Description, IsActive,
                       IsDeleted, CreatedAt, PaymentMode
                FROM   ShopExpenses
                WHERE  IsDeleted = 0
                ORDER  BY CreatedAt DESC");

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(Map(rdr));

            return list;
        }

        // ── GET BY ID ───────────────────────────────────────────────────
        public async Task<ShopExpense?> GetByIdAsync(int id)
        {
            await using var con = await OpenSQLiteAsync();
            var cmd = SQLiteHelper.Query(con, @"
                SELECT ExpenseId, Title, Category, Amount,
                       ExpenseDate, Description, IsActive,
                       IsDeleted, CreatedAt, PaymentMode
                FROM   ShopExpenses
                WHERE  ExpenseId = @ExpenseId
                AND    IsDeleted = 0");

            cmd.Parameters.AddWithValue("@ExpenseId", id);

            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? Map(rdr) : null;
        }

        // ── INSERT ──────────────────────────────────────────────────────
        public async Task<int> InsertAsync(ShopExpenseRequest req)
        {
            await _writeLock.WaitAsync();
            try
            {
                await using var con = await OpenSQLiteAsync();
                await using var tx = await con.BeginTransactionAsync();
                int newId;

                try
                {
                    var cmd = SQLiteHelper.Query(con, @"
                        INSERT INTO ShopExpenses
                            (Title, Category, Amount, ExpenseDate,
                             Description, IsActive, IsDeleted,
                             PaymentMode, ModifiedBy, CreatedAt, CreatedBy)
                        VALUES
                            (@Title, @Category, @Amount, @ExpenseDate,
                             @Description, @IsActive, 0,
                             @PaymentMode, @ModifiedBy, datetime('now'), @CreatedBy)");

                    cmd.Transaction = (SqliteTransaction)tx;
                    cmd.Parameters.AddWithValue("@Title", req.Title);
                    cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Amount", req.Amount);
                    cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", req.IsActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PaymentMode", (object?)req.PaymentMode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
                    cmd.Parameters.AddWithValue("@CreatedBy", (object?)req.CreatedBy ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();

                    var idCmd = SQLiteHelper.Query(con, "SELECT last_insert_rowid();");
                    idCmd.Transaction = (SqliteTransaction)tx;
                    var scalar = await idCmd.ExecuteScalarAsync();
                    newId = scalar is null ? 0 : Convert.ToInt32(scalar);

                    // Commit SQLite BEFORE touching SQL Server — lock released fast
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                // SQL Server sync runs AFTER SQLite commit — write lock still held
                // so LogSyncAsync (another SQLite write) won't deadlock.
                if (await IsSqlServerAvailableAsync())
                {
                    try
                    {
                        await using var sqlCon = new SqlConnection(_sqlServerCs);
                        await sqlCon.OpenAsync();

                        var sqlCmd = DbHelper.Proc(sqlCon, "sp_InsertShopExpense");
                        sqlCmd.Parameters.AddWithValue("@Title", req.Title);
                        sqlCmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
                        sqlCmd.Parameters.AddWithValue("@Amount", req.Amount);
                        sqlCmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
                        sqlCmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
                        sqlCmd.Parameters.AddWithValue("@IsActive", req.IsActive);
                        sqlCmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
                        sqlCmd.Parameters.AddWithValue("@PaymentMode", (object?)req.PaymentMode ?? DBNull.Value);
                        await sqlCmd.ExecuteNonQueryAsync();

                        await LogSyncAsync(con, newId, "INSERT", isSynced: true);
                    }
                    catch (Exception ex)
                    {
                        await LogSyncAsync(con, newId, "INSERT", isSynced: false);
                        Console.WriteLine("SQL Sync Error: " + ex.Message);
                    }
                }
                else
                {
                    await LogSyncAsync(con, newId, "INSERT", isSynced: false);
                }

                return newId;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        // ── UPDATE ──────────────────────────────────────────────────────
        public async Task UpdateAsync(int id, ShopExpenseRequest req)
        {
            await _writeLock.WaitAsync();
            try
            {
                await using var con = await OpenSQLiteAsync();
                await using var tx = await con.BeginTransactionAsync();

                try
                {
                    var cmd = SQLiteHelper.Query(con, @"
                        UPDATE ShopExpenses SET
                            Title       = @Title,
                            Category    = @Category,
                            Amount      = @Amount,
                            ExpenseDate = @ExpenseDate,
                            Description = @Description,
                            IsActive    = @IsActive,
                            PaymentMode = @PaymentMode,
                            ModifiedAt  = datetime('now'),
                            ModifiedBy  = @ModifiedBy
                        WHERE ExpenseId = @ExpenseId
                        AND   IsDeleted = 0");

                    cmd.Transaction = (SqliteTransaction)tx;
                    cmd.Parameters.AddWithValue("@ExpenseId", id);
                    cmd.Parameters.AddWithValue("@Title", req.Title);
                    cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Amount", req.Amount);
                    cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", req.IsActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
                    cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
                    await cmd.ExecuteNonQueryAsync();

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                if (await IsSqlServerAvailableAsync())
                {
                    try
                    {
                        await using var sqlCon = new SqlConnection(_sqlServerCs);
                        await sqlCon.OpenAsync();

                        var sqlCmd = DbHelper.Proc(sqlCon, "sp_UpdateShopExpense");
                        sqlCmd.Parameters.AddWithValue("@ExpenseId", id);
                        sqlCmd.Parameters.AddWithValue("@Title", req.Title);
                        sqlCmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
                        sqlCmd.Parameters.AddWithValue("@Amount", req.Amount);
                        sqlCmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
                        sqlCmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
                        sqlCmd.Parameters.AddWithValue("@IsActive", req.IsActive);
                        sqlCmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
                        sqlCmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
                        await sqlCmd.ExecuteNonQueryAsync();

                        await LogSyncAsync(con, id, "UPDATE", isSynced: true);
                    }
                    catch
                    {
                        await LogSyncAsync(con, id, "UPDATE", isSynced: false);
                    }
                }
                else
                {
                    await LogSyncAsync(con, id, "UPDATE", isSynced: false);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        // ── SOFT DELETE ─────────────────────────────────────────────────
        public async Task SoftDeleteAsync(int id, string modifiedBy)
        {
            await _writeLock.WaitAsync();
            try
            {
                await using var con = await OpenSQLiteAsync();
                await using var tx = await con.BeginTransactionAsync();

                try
                {
                    var cmd = SQLiteHelper.Query(con, @"
                        UPDATE ShopExpenses SET
                            IsDeleted  = 1,
                            IsActive   = 0,
                            ModifiedAt = datetime('now'),
                            ModifiedBy = @ModifiedBy
                        WHERE ExpenseId = @ExpenseId");

                    cmd.Transaction = (SqliteTransaction)tx;
                    cmd.Parameters.AddWithValue("@ExpenseId", id);
                    cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                    await cmd.ExecuteNonQueryAsync();

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                if (await IsSqlServerAvailableAsync())
                {
                    try
                    {
                        await using var sqlCon = new SqlConnection(_sqlServerCs);
                        await sqlCon.OpenAsync();

                        var sqlCmd = DbHelper.Proc(sqlCon, "sp_SoftDeleteShopExpense");
                        sqlCmd.Parameters.AddWithValue("@ExpenseId", id);
                        sqlCmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                        await sqlCmd.ExecuteNonQueryAsync();

                        await LogSyncAsync(con, id, "DELETE", isSynced: true);
                    }
                    catch
                    {
                        await LogSyncAsync(con, id, "DELETE", isSynced: false);
                    }
                }
                else
                {
                    await LogSyncAsync(con, id, "DELETE", isSynced: false);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        // ── GET LOGS ────────────────────────────────────────────────────
        public async Task<IEnumerable<ShopExpenseLog>> GetLogsAsync(int? id)
        {
            var list = new List<ShopExpenseLog>();

            await using var con = await OpenSQLiteAsync();

            var sql = id.HasValue
                ? "SELECT * FROM ShopExpenseLog WHERE ExpenseId = @ExpenseId ORDER BY ChangedAt DESC"
                : "SELECT * FROM ShopExpenseLog ORDER BY ChangedAt DESC";

            var cmd = SQLiteHelper.Query(con, sql);
            if (id.HasValue) cmd.Parameters.AddWithValue("@ExpenseId", id.Value);

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new ShopExpenseLog
                {
                    LogId = rdr.GetInt32(0),
                    ExpenseId = rdr.IsDBNull(1) ? 0 : rdr.GetInt32(1),
                    Action = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                    OldValues = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    NewValues = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    ChangedBy = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    ChangedAt = rdr.IsDBNull(6) ? DateTime.Now : DateTime.Parse(rdr.GetString(6))
                });

            return list;
        }

        // ── SYNC LOG ────────────────────────────────────────────────────
        private static async Task LogSyncAsync(
            SqliteConnection con, int recordId, string action, bool isSynced = false)
        {
            var cmd = SQLiteHelper.Query(con, @"
                INSERT INTO SyncLog (TableName, RecordId, Action, IsSynced, CreatedAt)
                VALUES ('ShopExpenses', @RecordId, @Action, @IsSynced, datetime('now'))");

            cmd.Parameters.AddWithValue("@RecordId", recordId);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@IsSynced", isSynced ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}