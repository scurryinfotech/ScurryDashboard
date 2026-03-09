namespace ScurryDashboard.Models
{
    public class ShopExpenseRequest
    {
        public string Title { get; set; } = "";
        public string? Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string ModifiedBy { get; set; } = "System";
    }
}
