namespace ScurryDashboard.Models
{
    public class DailyExpenseLog
    {
        public int LogId { get; set; }
        public int DailyExpenseId { get; set; }
        public string Action { get; set; } = "";
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
    }
}
