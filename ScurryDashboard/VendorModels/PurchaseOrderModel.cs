using System.ComponentModel.DataAnnotations;

namespace ScurryDashboard.VendorModels

{
   
        public class PurchaseOrderModel
        {
            public int PurchaseOrderId { get; set; }

            [Required]
            public int VendorId { get; set; }

            [Required]
            public DateTime OrderDate { get; set; }

            [Required]
            public decimal TotalAmount { get; set; }

            public decimal AdvancePaid { get; set; }
            public decimal RemainingAmount { get; set; }  
            public string? Notes { get; set; }

            public string? Status { get; set; }

            public bool IsDeleted { get; set; }
            public DateTime CreatedAt { get; set; }
            public int? CreatedByStaffId { get; set; }
            public string? ModifiedBy { get; set; }
            public DateTime? ModifiedAt { get; set; }

            // ✅ NOT required — these come from JOINs, never sent on save
            public string? VendorName { get; set; }
            public string? CreatedByName { get; set; }
        }
    }
