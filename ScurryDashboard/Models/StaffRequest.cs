namespace ScurryDashboard.Models
{
    public class StaffRequest
    {
        public string FullName { get; set; } = "";
        public int RoleId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? CNIC { get; set; }
        public string? Department { get; set; }
        public decimal Salary { get; set; }
        public DateTime? JoinDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string ModifiedBy { get; set; } = "System";
    }

}
