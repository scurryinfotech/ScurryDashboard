using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface ISalaryPaymentRepository
    {
        Task<string> InsertPaymentAsync(InsertPaymentRequest req);
        Task<EmployeeSalaryBalance?> GetBalanceAsync(int staffId);
        Task<EmployeePaymentHistory> GetHistoryAsync(int staffId);
    }
}
