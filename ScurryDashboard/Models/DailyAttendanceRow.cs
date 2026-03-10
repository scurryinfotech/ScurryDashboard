namespace ScurryDashboard.Models
{
    public class DailyAttendanceRow
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = "";
        public string? RoleName { get; set; }
        public string? Department { get; set; }
        public int AttendanceId { get; set; }
        public string Status { get; set; } = "NotMarked";
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public decimal OvertimeHours { get; set; }
        public string? Notes { get; set; }
    }
}
