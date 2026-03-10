using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class PayrollController : BaseApiController
    {
        public PayrollController(IHttpClientFactory f, IConfiguration c) : base(f, c) { }

        // GET /Payroll/GetByStaff?staffId=5
        [HttpGet]
        public async Task<IActionResult> GetByStaff(int staffId, int? month, int? year)
        {
            try
            {
                var url = $"/api/payroll/staff/{staffId}";
                if (month.HasValue) url += $"?month={month}&year={year}";
                var res = await CreateClient().GetAsync(url);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST /Payroll/Generate
        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] object req)
        {
            try
            {
                var json = JsonSerializer.Serialize(req);
                var res = await CreateClient().PostAsync("/api/payroll/generate",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode,
                    JsonSerializer.Deserialize<object>(body, _json));
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT /Payroll/MarkPaid?payrollId=5&modifiedBy=Admin
        [HttpPut]
        public async Task<IActionResult> MarkPaid(int payrollId, string modifiedBy = "Admin")
        {
            try
            {
                var res = await CreateClient().PutAsync($"/api/payroll/markpaid/{payrollId}?modifiedBy={modifiedBy}", null!);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
