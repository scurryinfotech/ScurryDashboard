namespace ScurryDashboard.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = "";
        public int RoleId { get; set; }
        public string? RoleName { get; set; }   // joined
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? CNIC { get; set; }
        public string? Department { get; set; }
        public decimal Salary { get; set; }
        public DateTime? JoinDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
