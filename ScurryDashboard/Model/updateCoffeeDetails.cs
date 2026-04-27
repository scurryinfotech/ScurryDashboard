using System.Text.Json.Serialization;
namespace OrderService.Model
{
    public class updateCoffeeDetails
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}
