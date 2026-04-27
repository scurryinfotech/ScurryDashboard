namespace OrderService.Model
{
    public class MuzztechSendResponse
    {
        public string Status { get; set; }
        public string Details { get; set; }  // session id
        public string OTP { get; set; }
    }
}
