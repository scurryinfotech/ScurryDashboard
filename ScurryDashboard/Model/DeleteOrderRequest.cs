namespace ScurryDashboard.Model
{
    public class DeleteOrderRequest
    {
        public int Id { get; set; }

        public string Reason { get; set; } = string.Empty;

    }
}
