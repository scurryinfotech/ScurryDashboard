namespace OrderService.Model
{
    public class OrderSummaryModel
    {
        public string OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentMode { get; set; }

    }
}
