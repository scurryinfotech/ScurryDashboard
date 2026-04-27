using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IAttendanceRepository
    {
        Task<IEnumerable<Attendance>> GetByStaffAsync(int staffId, int? month, int? year);
        Task<IEnumerable<DailyAttendanceRow>> GetByDateAsync(string date);
        Task MarkAttendanceAsync(AttendanceRequest req);
        Task BulkMarkAsync(BulkAttendanceRequest req);
        Task<MonthlySummary?> GetMonthlySummaryAsync(int staffId, int month, int year);
        Task<EmployeeProfile> GetEmployeeProfileAsync(int staffId, int? month, int? year);
    }
}
