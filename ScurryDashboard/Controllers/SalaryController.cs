using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class SalaryController : BaseApiController
    {
        public SalaryController(IHttpClientFactory f, IConfiguration c) : base(f, c) { }

        // ── Views ─────────────────────────────────────────────────

        public IActionResult Index() => View();

        public IActionResult PaymentHistory(int staffId)
        {
            ViewBag.StaffId = staffId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard(int? month, int? year)
        {
            try
            {
                var m = month ?? DateTime.Now.Month;
                var y = year ?? DateTime.Now.Year;
                var res = await CreateClient().GetAsync($"/api/salarydashboard?month={m}&year={y}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetSummary(int? month, int? year)
        {
            try
            {
                var m = month ?? DateTime.Now.Month;
                var y = year ?? DateTime.Now.Year;
                var res = await CreateClient().GetAsync($"/api/salarydashboard/summary?month={m}&year={y}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }


        [HttpPost]
        public async Task<IActionResult> GeneratePayroll([FromBody] object req)
        {
            try
            {
                var json = JsonSerializer.Serialize(req);
                var res = await CreateClient().PostAsync("/api/salarydashboard/generate",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /Salary/Pay
        [HttpPost]
        public async Task<IActionResult> Pay([FromBody] object req)
        {
            try
            {
                var json = JsonSerializer.Serialize(req);
                var res = await CreateClient().PostAsync("/api/salarypayment",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /Salary/GetBalance?staffId=5
        [HttpGet]
        public async Task<IActionResult> GetBalance(int staffId)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/salarypayment/balance/{staffId}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // GET /Salary/GetHistory?staffId=5
        [HttpGet]
        public async Task<IActionResult> GetHistory(int staffId)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/salarypayment/history/{staffId}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}