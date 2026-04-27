using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;
namespace OrderService.Repository.Service
{
    public class ShopExpenseRepository : IShopExpenseRepository
    {
        private readonly string _cs;
        public ShopExpenseRepository(IConfiguration cfg) =>
            _cs = cfg.GetConnectionString("ConnStringDb")!;

        private static ShopExpense Map(SqlDataReader r) => new()
        {
            ExpenseId = r.GetInt32(r.GetOrdinal("ExpenseId")),
            Title = r.GetString(r.GetOrdinal("Title")),
            Category = r.IsDBNull(r.GetOrdinal("Category")) ? null : r.GetString(r.GetOrdinal("Category")),
            Amount = r.GetDecimal(r.GetOrdinal("Amount")),
            ExpenseDate = r.GetDateTime(r.GetOrdinal("ExpenseDate")),
            
            Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            IsDeleted = r.GetBoolean(r.GetOrdinal("IsDeleted")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            PaymentMode = r.GetString(r.GetOrdinal("PaymentMode")),
        };

        public async Task<IEnumerable<ShopExpense>> GetAllAsync()
        {
            var list = new List<ShopExpense>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            await using var rdr = await DbHelper.Proc(con, "sp_GetAllShopExpenses").ExecuteReaderAsync();
            while (await rdr.ReadAsync()) list.Add(Map(rdr));
            return list;
        }

        public async Task<ShopExpense?> GetByIdAsync(int id)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_GetShopExpenseById");
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? Map(rdr) : null;
        }

        public async Task<int> InsertAsync(ShopExpenseRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_InsertShopExpense");
            cmd.Parameters.AddWithValue("@Title", req.Title);
            cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", req.Amount);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            cmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
            await cmd.ExecuteNonQueryAsync();
            return 1;
        }

        public async Task UpdateAsync(int id, ShopExpenseRequest req)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_UpdateShopExpense");
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            cmd.Parameters.AddWithValue("@Title", req.Title);
            cmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", req.Amount);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", req.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            cmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteAsync(int id, string modifiedBy)
        {
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_SoftDeleteShopExpense");
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<ShopExpenseLog>> GetLogsAsync(int? id)
        {
            var list = new List<ShopExpenseLog>();
            await using var con = new SqlConnection(_cs); await con.OpenAsync();
            var cmd = DbHelper.Proc(con, "sp_GetShopExpenseLogs");
            cmd.Parameters.AddWithValue("@ExpenseId", (object?)id ?? DBNull.Value);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new ShopExpenseLog
                {
                    LogId = rdr.GetInt32(0),
                    ExpenseId = rdr.GetInt32(1),
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
