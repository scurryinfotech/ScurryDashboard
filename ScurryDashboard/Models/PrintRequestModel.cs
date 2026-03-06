using System;
using System.Collections.Generic;

namespace ScurryDashboard.Models
{
    // DTO used by Print API
    public class PrintItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class PrintRequestModel
    {
        public string OrderId { get; set; }
        public string Customer { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime OrderTime { get; set; }
        public List<PrintItem> Items { get; set; } = new List<PrintItem>();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string PrinterName { get; set; }
    }
}