namespace OrderService.Model
{
    public class OrderListModel
    {
        public int? TableNo { get; set; }
        public int Id { get; set; }
        public string? OrderId { get; set; }
        public string ItemName { get; set; }
        public int HalfPortion { get; set; }
        public int FullPortion { get; set; }
        public decimal Price { get; set; }
        public int? OrderStatusId { get; set; }
        public string OrderStatus { get; set; }
        public int? statusId { get; set; }
        public DateTime Date { get; set; }
        public int IsActive { get; set; }
        public string? customerName { get; set; }
        public string? phone { get; set; }
        public string? OrderType { get; set; }
        public string? Address { get; set; }
        public string? PaymentMode { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? createdDate { get; set; }
        public string? specialInstructions { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? paymentMode { get => PaymentMode; set => PaymentMode = value; }
        public string? userId { get; set; }
        public string? Discount { get; set; }
        public string? DeliveryName { get; set; }
        public string? DeliveryPhone { get; set; }
    }
}