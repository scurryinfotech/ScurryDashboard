using Microsoft.AspNetCore.Mvc;
using ScurryDashboard.Models;
using System.Text.Json;
using OrderService.Repository.Interface;

namespace ScurryDashboard.Controllers
{
    public class RolesController : BaseApiController
    {
        private readonly IRoleRepository _sqlite;

        public RolesController(
            IHttpClientFactory f,
            IConfiguration c,
            IRoleRepository sqlite
        ) : base(f, c)
        {
            _sqlite = sqlite;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var res = await CreateClient().GetAsync("/api/roles");
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<IEnumerable<Role>>(body, _json));
                }

                Console.WriteLine($"[RolesController] API returned {res.StatusCode}, falling back to SQLite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RolesController] API error: {ex.Message}, falling back to SQLite.");
            }

            var offlineRoles = await _sqlite.GetActiveRolesAsync();
            return Json(offlineRoles);
        }
    }
}