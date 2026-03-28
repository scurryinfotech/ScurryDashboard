namespace OrderService.Model
{
    public class MenuItem
    {
        public int ItemId { get; set; }
        public int SubcategoryId { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string ImageSrc { get; set; }
        public decimal Price1 { get; set; }
        public decimal Price2 { get; set; }
        public int Count1 { get; set; }
        public int Count2 { get; set; }
        public string Title { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }
    }

}
