using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetActiveRolesAsync();
    }

}
