namespace OrderService.Model
{
    public class MenuSubcategory
    {
        public int SubcategoryId { get; set; }
        public int CategoryId { get; set; }
        public string SubcategoryName { get; set; }
        public string Description { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }

}
