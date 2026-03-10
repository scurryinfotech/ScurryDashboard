namespace ScurryDashboard.Models
{
    public class DashboardSummary
    {
        public int TotalEmployees { get; set; }
        public decimal TotalPayroll { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public int PaidCount { get; set; }
        public int PartialCount { get; set; }
        public int PendingCount { get; set; }
    }
}
