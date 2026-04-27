namespace OrderService.Model
{
    public class UserModel
    {
        
        public string loginame { get;  set; }
        public string Password { get;  set; }
        public string? Name { get; set; }   

        public string? phone { get; set; }
        //public int Id { get; set; }
        public string? Address { get; set; }
        public DateTime? CreatedDate { get;  set; }
        public bool? IsActive { get;  set; }
    }
}
