using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IShopExpenseRepository
    {
        Task<IEnumerable<ShopExpense>> GetAllAsync();
        Task<ShopExpense?> GetByIdAsync(int expenseId);
        Task<int> InsertAsync(ShopExpenseRequest req);
        Task UpdateAsync(int expenseId, ShopExpenseRequest req);
        Task SoftDeleteAsync(int expenseId, string modifiedBy);
        Task<IEnumerable<ShopExpenseLog>> GetLogsAsync(int? expenseId);
    }

}
