namespace ScurryDashboard.Models
{
    public class StaffLog
    {
        public int LogId { get; set; }
        public int StaffId { get; set; }
        public string Action { get; set; } = "";
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
