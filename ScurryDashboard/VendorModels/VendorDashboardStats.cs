namespace ScurryDashboard.VendorModels
{

    public class VendorDashboardStatsDto
    {
        public int TotalVendors { get; set; }
        public int ActiveVendors { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int PendingOrders { get; set; }
    }
}
