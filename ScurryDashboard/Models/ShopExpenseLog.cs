namespace ScurryDashboard.Models
{

    public class ShopExpenseLog
    {
        public int LogId { get; set; }
        public int ExpenseId { get; set; }
        public string Action { get; set; } = "";
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }

}
