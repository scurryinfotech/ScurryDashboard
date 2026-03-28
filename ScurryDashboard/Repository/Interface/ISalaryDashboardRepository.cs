using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface ISalaryDashboardRepository
    {

        Task<IEnumerable<SalaryDashboardRow>> GetDashboardAsync(int month, int year);
        Task<DashboardSummary?> GetSummaryAsync(int month, int year);
        Task<PayrollRecord?> GeneratePayrollAsync(GeneratePayrollRequest req);
    }
}
