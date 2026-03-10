using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class AttendanceController : BaseApiController
    {
        public AttendanceController(IHttpClientFactory f, IConfiguration c) : base(f, c) { }

        // GET /Attendance/GetProfile?staffId=5&month=3&year=2026
        [HttpGet]
        public async Task<IActionResult> GetProfile(int staffId, int? month, int? year)
        {
            try
            {
                var url = $"/api/attendance/profile/{staffId}";
                if (month.HasValue) url += $"?month={month}&year={year}";
                var res = await CreateClient().GetAsync(url);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /Attendance/GetByStaff?staffId=5&month=3&year=2026
        [HttpGet]
        public async Task<IActionResult> GetByStaff(int staffId, int? month, int? year)
        {
            try
            {
                var url = $"/api/attendance/staff/{staffId}?month={month}&year={year}";
                var res = await CreateClient().GetAsync(url);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /Attendance/GetByDate?date=2026-03-09
        [HttpGet]
        public async Task<IActionResult> GetByDate(string date)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/attendance/date/{date}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /Attendance/Mark
        [HttpPost]
        public async Task<IActionResult> Mark([FromBody] object req)
        {
            try
            {
                var json = JsonSerializer.Serialize(req);
                var res = await CreateClient().PostAsync("/api/attendance",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /Attendance/BulkMark
        [HttpPost]
        public async Task<IActionResult> BulkMark([FromBody] object req)
        {
            try
            {
                var json = JsonSerializer.Serialize(req);
                var res = await CreateClient().PostAsync("/api/attendance/bulk",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /Attendance/GetSummary?staffId=5&month=3&year=2026
        [HttpGet]
        public async Task<IActionResult> GetSummary(int staffId, int month, int year)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/attendance/summary/{staffId}?month={month}&year={year}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
