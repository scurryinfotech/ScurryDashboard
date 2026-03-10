namespace ScurryDashboard.Models
{
    public class Payroll
    {
        public int PayrollId { get; set; }
        public int StaffId { get; set; }
        public string? FullName { get; set; }
        public int PayMonth { get; set; }
        public int PayYear { get; set; }
        public decimal BasicSalary { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int HalfDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public string Status { get; set; } = "Pending";
        public string? PaidOn { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
