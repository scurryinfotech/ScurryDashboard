using OrderService.Helpers;
using OrderService.Model;
using OrderService.Repository.Interface;

namespace OrderService.Repository.SQLite
{
    public class RoleSQLiteRepository : IRoleRepository
    {
        private readonly string _sqliteCs;

        public RoleSQLiteRepository(IConfiguration cfg)
        {
            _sqliteCs = cfg.GetConnectionString("SQLiteConnection")!;
        }

        public async Task<IEnumerable<Role>> GetActiveRolesAsync()
        {
            var list = new List<Role>();

            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var cmd = SQLiteHelper.Query(con, @"
                SELECT RoleId, RoleName
                FROM   Roles
                WHERE  IsActive = 1");

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