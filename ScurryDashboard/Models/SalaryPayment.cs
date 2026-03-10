namespace ScurryDashboard.Models
{
    public class SalaryPayment
    {
        public int PaymentId { get; set; }
        public int StaffId { get; set; }
        public int? PayrollId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentDate { get; set; } = "";
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentType { get; set; } = "Full";
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public int? PayMonth { get; set; }
        public int? PayYear { get; set; }
    }
}
