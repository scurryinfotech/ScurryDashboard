namespace ScurryDashboard.Model
{
    public class OrderListModel2
    {
        public int Id { get; set; }
        public int TableNo { get; set; }
        public string OrderId { get; set; }
        public int OrderStatusId { get; set; }
        public string OrderStatus { get; set; }
        public string ItemName { get; set; }
        public int HalfPortion { get; set; }
        public int FullPortion { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string customerName { get; set; }
        public string phone { get; set; }
        public string OrderType { get; set; }
        public string Address { get; set; }
        public string specialInstructions { get; set; }
        public int IsActive { get; set; }

        // ── existing payment field ──
        public string paymentMode { get; set; }

        // ── NEW payment fields ──
        public string PaymentStatus { get; set; }
        public string RazorpayOrderId { get; set; }
        public string RazorpayPaymentId { get; set; }
        public string PaymentLabel { get; set; }

        // ── Added to match mapping in repository
        public string? userId { get; set; }
        public string? Discount { get; set; }
        public string? DeliveryName { get; set; }
        public string? DeliveryPhone { get; set; }

        public decimal? PaymentAmount { get; set; }
        public string? PaymentCurrency { get; set; }
        public string? PaymentReceipt { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? PaymentCreatedAt { get; set; }
        public DateTime? PaymentUpdatedAt { get; set; }
        public string? ResolvedPaymentMode { get; set; }
    }
}
