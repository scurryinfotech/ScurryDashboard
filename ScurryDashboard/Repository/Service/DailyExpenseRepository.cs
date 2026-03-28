using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;

namespace OrderService.Repository.Service
{
    public class DailyExpenseRepository : IDailyExpenseRepository
    {
        private readonly string _cs;
        public DailyExpenseRepository(IConfiguration cfg) =>
            _cs = cfg.GetConnectionString("ConnStringDb")!;

        private static DailyExpense Map(SqlDataReader r) => new()
        {
            DailyExpenseId = r.GetInt32(r.GetOrdinal("DailyExpenseId")),
            Title = r.GetString(r.GetOrdinal("Title")),
            Category = r.IsDBNull(r.GetOrdinal("Category")) ? null : r.GetString(r.GetOrdinal("Category")),
            Amount = r.GetDecimal(r.GetOrdinal("Amount")),
            ExpenseDate = r.GetDateTime(r.GetOrdinal("ExpenseDate")),
            PaidBy = r.IsDBNull(r.GetOrdinal("PaidBy")) ? null : r.GetString(r.GetOrdinal("PaidBy")),
            PaymentMode = r.IsDBNull(r.GetOrdinal("PaymentMode")) ? null : r.GetString(r.GetOrdinal("PaymentMode")),
            Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString(r.GetOrdinal("Notes")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            IsDeleted = r.GetBoolean(r.GetOrdinal("IsDeleted")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        };

        public async Task<IEnumerable<DailyExpense>> GetAllAsync()
        {
            var list = new List<DailyExpense>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            await using var rdr = await DbHelper.Proc(con, "sp_GetAllDailyExpenses").ExecuteReaderAsync();
            while (await rdr.ReadAsync()) list.Add(Map(rdr));
            return list;
        }

        public async Task<DailyExpense?> GetByIdAsync(int id)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_GetDailyExpenseById");
            cmd.Parameters.AddWithValue("@DailyExpenseId", id);
            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? Map(rdr) : null;
        }

        public async Task<int> InsertAsync(DailyExpenseRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_InsertDailyExpense");
            cmd.Parameters.AddWithValue("@Title", req.Title);
            cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", req.Amount);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
            cmd.Parameters.AddWithValue("@PaymentMode",req.PaymentMode);
            cmd.Parameters.AddWithValue("@PaidBy", (object?)req.PaidBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)req.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            await cmd.ExecuteNonQueryAsync();
            return 1;
        }

        public async Task UpdateAsync(int id, DailyExpenseRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_UpdateDailyExpense");
            cmd.Parameters.AddWithValue("@DailyExpenseId", id);
            cmd.Parameters.AddWithValue("@Title", req.Title);
            cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", req.Amount);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
            cmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode); 
            cmd.Parameters.AddWithValue("@PaidBy", (object?)req.PaidBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)req.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteAsync(int id, string modifiedBy)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_SoftDeleteDailyExpense");
            cmd.Parameters.AddWithValue("@DailyExpenseId", id);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<DailyExpenseLog>> GetLogsAsync(int? id)
        {
            var list = new List<DailyExpenseLog>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_GetDailyExpenseLogs");
            cmd.Parameters.AddWithValue("@DailyExpenseId", (object?)id ?? DBNull.Value);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new DailyExpenseLog
                {
                    LogId = rdr.GetInt32(0),
                    DailyExpenseId = rdr.GetInt32(1),
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
