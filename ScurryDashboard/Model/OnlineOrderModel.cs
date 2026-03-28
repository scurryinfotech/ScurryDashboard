namespace OrderService.Model
{
    public class OnlineOrderModel
    {
        public string OrderId { get; set; }
        public int OrderStatus { get; set; }

        public string? paymentMode { get; set; }

        public int? DeliveryStaffId { get; set; }

    }
}
