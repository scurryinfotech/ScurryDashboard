using System.Threading.Tasks;
using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IUserRepository
    {
        Task<int> AddUser(UserModel user);
        Task<UserModel?> LoginAsync(string loginame, string password);

    }
}