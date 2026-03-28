using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IDailyExpenseRepository
    {
        Task<IEnumerable<DailyExpense>> GetAllAsync();
        Task<DailyExpense?> GetByIdAsync(int dailyExpenseId);
        Task<int> InsertAsync(DailyExpenseRequest req);
        Task UpdateAsync(int dailyExpenseId, DailyExpenseRequest req);
        Task SoftDeleteAsync(int dailyExpenseId, string modifiedBy);
        Task<IEnumerable<DailyExpenseLog>> GetLogsAsync(int? dailyExpenseId);
    }
}
