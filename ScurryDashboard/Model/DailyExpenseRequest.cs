namespace OrderService.Model
{
    public class DailyExpenseRequest
    {
        public string Title { get; set; } = "";
        public string? Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string PaymentMode { get; set; }
        public string? PaidBy { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public string ModifiedBy { get; set; } = "System";
        public int? CreatedBy { get; set; }
    }
}
