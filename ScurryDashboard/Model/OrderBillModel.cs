namespace OrderService.Model
{
    public class OrderBillModel
    {
        public int SummaryId { get; set; }
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime CompletedDate { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int FullPortion { get; set; }
        public int HalfPortion { get; set; }
        public decimal Price { get; set; }
        public int TableNo { get; set; }
        public string PaymentMode { get; set; }
        public string SpecialInstructions { get; set; }
    }
}
