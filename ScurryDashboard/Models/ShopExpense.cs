namespace ScurryDashboard.Models
{
    public class ShopExpense
    {
        public int ExpenseId { get; set; }
        public string Title { get; set; } = "";
        public string? Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
