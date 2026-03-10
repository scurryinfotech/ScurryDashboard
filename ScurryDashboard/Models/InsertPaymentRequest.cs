namespace ScurryDashboard.Models
{
    public class InsertPaymentRequest
    {
        public int StaffId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentDate { get; set; } = "";
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentType { get; set; } = "Full";
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = "Admin";
    }
}
