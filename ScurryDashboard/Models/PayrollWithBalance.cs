namespace ScurryDashboard.Models
{
    public class PayrollWithBalance
    {
        public int PayrollId { get; set; }
        public int PayMonth { get; set; }
        public int PayYear { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal NetSalary { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal Deductions { get; set; }
        public string PayrollStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }  // always computed
    }
}
