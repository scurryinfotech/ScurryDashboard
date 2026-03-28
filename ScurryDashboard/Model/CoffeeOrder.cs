namespace OrderService.Model
{
    public class CoffeeOrder
    {
        // Matches the JS payload property names used by the client
        public string customerName { get; set; } = string.Empty;
        public string customerPhone { get; set; } = string.Empty;
        public string? Floor { get; set; }
        public string? RoomNo { get; set; }

        // Items array: item_id, quantity, Price
        public List<CoffeeOrderItem> orderItems { get; set; } = new();

        // Client sends ISO string; model binder will parse to DateTime
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }

    public class CoffeeOrderItem
    {
        public int item_id { get; set; }
        public int quantity { get; set; }
        public decimal Price { get; set; }
    }
}
