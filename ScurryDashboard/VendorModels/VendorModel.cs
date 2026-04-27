namespace ScurryDashboard.VendorModels
{
    public class VendorDto
    {
        public int VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? CNIC { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedByStaffId { get; set; }

        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public string? CreatedByName { get; set; }
    }
}