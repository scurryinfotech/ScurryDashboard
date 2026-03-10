namespace ScurryDashboard.Models
{
    public class SalaryDashboardRow
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = "";
        public string? Department { get; set; }
        public decimal BasicSalary { get; set; }
        public int? PayrollId { get; set; }
        public decimal NetSalary { get; set; }
        public int WorkingDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal Deductions { get; set; }
        public string PayrollStatus { get; set; } = "NotGenerated";
        public decimal PaidThisMonth { get; set; }
        public decimal AdvanceThisMonth { get; set; }
        public decimal BalanceThisMonth { get; set; } 
        public decimal TotalOutstanding { get; set; }  

        
        public string BalanceColor =>
            BalanceThisMonth > 0 ? "danger"
            : BalanceThisMonth < 0 ? "success"
            : "primary";

        public string BalanceLabel =>
            BalanceThisMonth > 0 ? "Pending"
            : BalanceThisMonth < 0 ? "Overpaid"
            : "Paid";
    }
}
