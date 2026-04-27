namespace ScurryDashboard.VendorModels
{
    public class VendorPaymentModel
    {
        public int PaymentId { get; set; }
        public int PurchaseOrderId { get; set; }
        public int VendorId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentMethod { get; set; }

        // ✅ These 3 are the ones causing your error — make nullable
        public string? InvoiceNumber { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedByStaffId { get; set; }
        public string? ModifiedBy { get; set; }

        // ✅ JOIN fields — never sent from frontend, must be nullable
        public string? VendorName { get; set; }
        public string? CreatedByName { get; set; }
    }
}