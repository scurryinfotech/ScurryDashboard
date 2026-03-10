namespace ScurryDashboard.Models
{
    public class Attendance
    {
        public int AttendanceId { get; set; }
        public int StaffId { get; set; }
        public string? FullName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; } = "Present";
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public decimal OvertimeHours { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }

}
