namespace OrderService.Model
{
    public class CoffeeMenu
    {
        public int Id { get; set; }
        public string CoffeeName { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string? Ingredients { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public List<CoffeeItem> orderItems { get; set; }

        public string customerName { get; set; }
        public string Floor { get; set; }
        public string RoomNo { get; set; }
        public string customerPhone { get; set; }



    }
    public class CoffeeItem
    {
        public int item_id { get; set; }
        public int quantity { get; set; }
        public double Price { get; set; }
    }
}
