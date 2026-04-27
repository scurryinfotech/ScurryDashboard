using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IPayrollRepository
    {
        Task<IEnumerable<Payroll>> GetByStaffAsync(int staffId, int? month, int? year);
        Task<Payroll?> GenerateAsync(GeneratePayrollRequest req);
        Task MarkPaidAsync(int payrollId, string modifiedBy);
    }
}
