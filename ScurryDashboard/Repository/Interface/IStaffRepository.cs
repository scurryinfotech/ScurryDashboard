using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IStaffRepository
    {
        Task<IEnumerable<Staff>> GetAllStaffAsync();
        Task<Staff?> GetStaffByIdAsync(int staffId);
        Task<int> InsertStaffAsync(StaffRequest req);
        Task UpdateStaffAsync(int staffId, StaffRequest req);
        Task SoftDeleteStaffAsync(int staffId, string modifiedBy);
        Task<IEnumerable<StaffLog>> GetStaffLogsAsync(int? staffId);
    }
}
