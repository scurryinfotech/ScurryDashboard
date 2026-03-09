using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ScurryDashboard.Models;   

namespace ScurryDashboard.Controllers
{
    public class StaffController : BaseApiController
    {
        public StaffController(IHttpClientFactory f, IConfiguration c) : base(f, c) { }

        // Renders the view — JS loads data via Ajax
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var res = await CreateClient().GetAsync("/api/staff");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<IEnumerable<Staff>>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/staff/{id}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<Staff>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] Staff model)
        {
            try
            {
                var res = await CreateClient().PostAsync("/api/staff", JsonBody(model));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut]
        public async Task<IActionResult> Update(int id, [FromBody] Staff model)
        {
            try
            {
                var res = await CreateClient().PutAsync($"/api/staff/{id}", JsonBody(model));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id, [FromQuery] string modifiedBy = "Admin")
        {
            try
            {
                var res = await CreateClient().DeleteAsync($"/api/staff/{id}?modifiedBy={modifiedBy}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(int? staffId)
        {
            try
            {
                var url = staffId.HasValue ? $"/api/staff/logs?staffId={staffId}" : "/api/staff/logs";
                var res = await CreateClient().GetAsync(url);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
