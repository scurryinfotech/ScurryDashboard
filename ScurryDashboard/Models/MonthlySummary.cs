namespace ScurryDashboard.Models
{
    public class MonthlySummary
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = "";
        public decimal BasicSalary { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int HalfDays { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public int TotalMarkedDays { get; set; }
    }
}
