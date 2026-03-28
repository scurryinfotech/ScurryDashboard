namespace OrderService.Repository.Interface
{
    public interface IJwtService
    {
        Task<Tuple<string, DateTime>> GenerateToken(string username);
        bool ValidateToken(string token);
    }
}
