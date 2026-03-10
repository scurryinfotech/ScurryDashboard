namespace ScurryDashboard.Models
{
    public class AttendanceRequest
    {
        public int StaffId { get; set; }
        public string AttendanceDate { get; set; } = "";
        public string Status { get; set; } = "Present";
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public decimal OvertimeHours { get; set; } = 0;
        public string? Notes { get; set; }
        public string ModifiedBy { get; set; } = "Admin";
    }
}
