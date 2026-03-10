namespace ScurryDashboard.Models
{
    public class BulkAttendanceRequest
    {
        public string AttendanceDate { get; set; } = "";
        public string DefaultStatus { get; set; } = "Present";
        public string ModifiedBy { get; set; } = "Admin";
    }
}
