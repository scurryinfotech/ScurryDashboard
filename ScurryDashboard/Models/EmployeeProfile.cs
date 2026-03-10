namespace ScurryDashboard.Models
{
    public class EmployeeProfile
    {
        public Staff? StaffInfo { get; set; }
        public IEnumerable<Attendance> MonthAttendance { get; set; } = new List<Attendance>();
        public IEnumerable<Payroll> PayrollHistory { get; set; } = new List<Payroll>();
        public MonthlySummary? MonthlySummary { get; set; }
    }
}
