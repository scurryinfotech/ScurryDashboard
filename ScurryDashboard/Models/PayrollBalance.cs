namespace ScurryDashboard.Models
{
    public class PayrollBalance
    {
        public int PayrollId { get; set; }
        public int PayMonth { get; set; }
        public int PayYear { get; set; }
        public decimal NetSalary { get; set; }
        public string PayrollStatus { get; set; } = "Pending";
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; } 
    }
}
