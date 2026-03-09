namespace ScurryDashboard.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
