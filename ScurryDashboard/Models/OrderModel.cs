namespace ScurryDashboard.Models
{
    public class OrderModel
    {
        public int? selectedTable { get; set; }
        public string customerName { get; set; }
        public string userPhone { get; set; }
        public string? Address { get; set; }
        public string? OrderType { get; set; }
        public string? deliveryType { get; set; }
        public string? specialInstruction { get; set; }
        public int userId { get; set; }          
        public int? userName { get; set; }       

        public List<OrderItem> orderItems { get; set; }

        public class OrderItem
        {
            public int item_id { get; set; }
            public int full { get; set; }
            public int half { get; set; }
            public decimal Price { get; set; }
        }
    }
}
