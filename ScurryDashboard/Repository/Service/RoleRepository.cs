using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;

namespace OrderService.Repository.Service
{
    public class RoleRepository : IRoleRepository
    {
        private readonly string _cs;

        public RoleRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("ConnStringDb");
        }

        public async Task<IEnumerable<Role>> GetActiveRolesAsync()
        {
            var list = new List<Role>();

            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            var cmd = DbHelper.Proc(con, "sp_GetActiveRoles");

            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                list.Add(new Role
                {
                    RoleId = rdr.GetInt32(0),
                    RoleName = rdr.GetString(1)
                });
            }

            return list;
        }
    }
}