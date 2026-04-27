using Microsoft.Data.Sqlite;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;

namespace OrderService.Repository.Service
{
    public class DailyExpenseSQLiteRepository : IDailyExpenseRepository
    {
        private readonly string _cs;

        public DailyExpenseSQLiteRepository(IConfiguration cfg) =>
            _cs = cfg.GetConnectionString("SQLiteConnection")!;

        // ─── MAP — SqliteDataReader → DailyExpense ───────────
        private static DailyExpense Map(SqliteDataReader r) => new()
        {
            DailyExpenseId = r.GetInt32(r.GetOrdinal("DailyExpenseId")),
            Title = r.GetString(r.GetOrdinal("Title")),
            Category = r.IsDBNull(r.GetOrdinal("Category")) ? null : r.GetString(r.GetOrdinal("Category")),
            Amount = r.GetDecimal(r.GetOrdinal("Amount")),
            ExpenseDate = DateTime.Parse(r.GetString(r.GetOrdinal("ExpenseDate"))),
            PaidBy = r.IsDBNull(r.GetOrdinal("PaidBy")) ? null : r.GetString(r.GetOrdinal("PaidBy")),
            PaymentMode = r.IsDBNull(r.GetOrdinal("PaymentMode")) ? null : r.GetString(r.GetOrdinal("PaymentMode")),
            Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString(r.GetOrdinal("Notes")),
            IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
            IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
            CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
        };

        // ─── GET ALL ─────────────────────────────────────────
        public async Task<IEnumerable<DailyExpense>> GetAllAsync()
        {
            var list = new List<DailyExpense>();

            await using var con = await SQLiteHelper.OpenAsync(_cs);
            var cmd = SQLiteHelper.Query(con, @"
                SELECT DailyExpenseId, Title, Category, Amount,
                       ExpenseDate, PaidBy, PaymentMode, Notes,
                       IsActive, IsDeleted, CreatedAt
                FROM   DailyExpenses
                WHERE  IsDeleted = 0
                ORDER  BY CreatedAt DESC");

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(Map(rdr));

            return list;
        }

        // ─── GET BY ID ───────────────────────────────────────
        public async Task<DailyExpense?> GetByIdAsync(int id)
        {
            await using var con = await SQLiteHelper.OpenAsync(_cs);
            var cmd = SQLiteHelper.Query(con, @"
                SELECT DailyExpenseId, Title, Category, Amount,
                       ExpenseDate, PaidBy, PaymentMode, Notes,
                       IsActive, IsDeleted, CreatedAt
                FROM   DailyExpenses
                WHERE  DailyExpenseId = @DailyExpenseId
                AND    IsDeleted = 0");

            cmd.Parameters.AddWithValue("@DailyExpenseId", id);

            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? Map(rdr) : null;
        }


        // ─── INSERT ──────────────────────────────────────────
        public async Task<int> InsertAsync(DailyExpenseRequest req)
        {
            await using var con = await SQLiteHelper.OpenAsync(_cs);
            var cmd = SQLiteHelper.Query(con, @"
        INSERT INTO DailyExpenses
            (Title, Category, Amount, ExpenseDate,
             PaymentMode, PaidBy, Notes,
             IsActive, IsDeleted, ModifiedBy,
             CreatedAt, CreatedBy)
        VALUES
            (@Title, @Category, @Amount, @ExpenseDate,
             @PaymentMode, @PaidBy, @Notes,
             @IsActive, 0, @ModifiedBy,
             datetime('now'), @CreatedBy);
        SELECT last_insert_rowid();");

            cmd.Parameters.AddWithValue("@Title", req.Title);
            cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", req.Amount);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
            cmd.Parameters.AddWithValue("@PaidBy", (object?)req.PaidBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)req.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)req.CreatedBy ?? DBNull.Value);

            var newId = await cmd.ExecuteScalarAsync();
            await LogSyncAsync(con, Convert.ToInt32(newId), "INSERT");
            return Convert.ToInt32(newId);
        }

        // ─── UPDATE ──────────────────────────────────────────
        public async Task UpdateAsync(int id, DailyExpenseRequest req)
        {
            await using var con = await SQLiteHelper.OpenAsync(_cs);
            var cmd = SQLiteHelper.Query(con, @"
                UPDATE DailyExpenses SET
                    Title       = @Title,
                    Category    = @Category,
                    Amount      = @Amount,
                    ExpenseDate = @ExpenseDate,
                    PaymentMode = @PaymentMode,
                    PaidBy      = @PaidBy,
                    Notes       = @Notes,
                    IsActive    = @IsActive,
                    ModifiedAt  = datetime('now'),
                    ModifiedBy  = @ModifiedBy
                WHERE DailyExpenseId = @DailyExpenseId
                AND   IsDeleted = 0");

            cmd.Parameters.AddWithValue("@DailyExpenseId", id);
            cmd.Parameters.AddWithValue("@Title", req.Title);
            cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", req.Amount);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
            cmd.Parameters.AddWithValue("@PaidBy", (object?)req.PaidBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)req.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);

            await cmd.ExecuteNonQueryAsync();

            // ── SyncLog mein entry karo
            await LogSyncAsync(con, id, "UPDATE");
        }

        // ─── SOFT DELETE ─────────────────────────────────────
        public async Task SoftDeleteAsync(int id, string modifiedBy)
        {
            await using var con = await SQLiteHelper.OpenAsync(_cs);
            var cmd = SQLiteHelper.Query(con, @"
                UPDATE DailyExpenses SET
                    IsDeleted  = 1,
                    IsActive   = 0,
                    ModifiedAt = datetime('now'),
                    ModifiedBy = @ModifiedBy
                WHERE DailyExpenseId = @DailyExpenseId");

            cmd.Parameters.AddWithValue("@DailyExpenseId", id);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);

            await cmd.ExecuteNonQueryAsync();

            // ── SyncLog mein entry karo
            await LogSyncAsync(con, id, "DELETE");
        }

        // ─── GET LOGS ────────────────────────────────────────
        public async Task<IEnumerable<DailyExpenseLog>> GetLogsAsync(int? id)
        {
            var list = new List<DailyExpenseLog>();

            await using var con = await SQLiteHelper.OpenAsync(_cs);

            var sql = id.HasValue
                ? "SELECT * FROM DailyExpenseLog WHERE DailyExpenseId = @DailyExpenseId ORDER BY ChangedAt DESC"
                : "SELECT * FROM DailyExpenseLog ORDER BY ChangedAt DESC";

            var cmd = SQLiteHelper.Query(con, sql);

            if (id.HasValue)
                cmd.Parameters.AddWithValue("@DailyExpenseId", id.Value);

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new DailyExpenseLog
                {
                    LogId = rdr.GetInt32(0),
                    DailyExpenseId = rdr.IsDBNull(1) ? 0 : rdr.GetInt32(1),
                    Action = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                    OldValues = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    NewValues = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    ChangedBy = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    ChangedAt = rdr.IsDBNull(6) ? DateTime.Now : DateTime.Parse(rdr.GetString(6))
                });

            return list;
        }

        private static async Task LogSyncAsync(SqliteConnection con, int recordId, string action)
        {
            var cmd = SQLiteHelper.Query(con, @"
                INSERT INTO SyncLog (TableName, RecordId, Action, IsSynced, CreatedAt)
                VALUES ('DailyExpenses', @RecordId, @Action, 0, datetime('now'))");

            cmd.Parameters.AddWithValue("@RecordId", recordId);
            cmd.Parameters.AddWithValue("@Action", action);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}