using Microsoft.AspNetCore.Mvc;
using ScurryDashboard.Models;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class RolesController : BaseApiController
    {
        public RolesController(IHttpClientFactory f, IConfiguration c) : base(f, c) { }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var res = await CreateClient().GetAsync("/api/roles");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<IEnumerable<Role>>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
