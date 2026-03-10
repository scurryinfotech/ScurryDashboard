namespace ScurryDashboard.Models
{
    public class GeneratePayrollRequest
    {
        public int StaffId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal OvertimeRatePerHour { get; set; } = 150;
        public string ModifiedBy { get; set; } = "Admin";
    }

}
