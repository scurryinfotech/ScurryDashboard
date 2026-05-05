using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using OrderService.Model;
using OrderService.Repository.Interface;

namespace ScurryDashboard.Controllers
{
    public class PayrollController : BaseApiController
    {
        private readonly IPayrollRepository _sqlite;

        public PayrollController(
            IHttpClientFactory f,
            IConfiguration c,
            IPayrollRepository sqlite
        ) : base(f, c)
        {
            _sqlite = sqlite;
        }

        // GET /Payroll/GetByStaff?staffId=5
        [HttpGet]
        public async Task<IActionResult> GetByStaff(int staffId, int? month, int? year)
        {
            try
            {
                var url = $"/api/payroll/staff/{staffId}";
                if (month.HasValue) url += $"?month={month}&year={year}";

                var res = await CreateClient().GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }

                Console.WriteLine($"[PayrollController] API {res.StatusCode} → SQLite fallback.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayrollController] API unreachable: {ex.Message} → SQLite fallback.");
            }

            var offline = await _sqlite.GetByStaffAsync(staffId, month, year);
            return Json(offline);
        }

        // POST /Payroll/Generate
        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequest req)
        {
            try
            {
                var json = JsonSerializer.Serialize(req);
                var res = await CreateClient().PostAsync("/api/payroll/generate",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }

                Console.WriteLine($"[PayrollController] API {res.StatusCode} → SQLite fallback.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayrollController] API unreachable: {ex.Message} → SQLite fallback.");
            }

            var data = await _sqlite.GenerateAsync(req);
            if (data == null)
                return BadRequest(new { message = "Check attendance data first." });

            return Json(data);
        }

        // PUT /Payroll/MarkPaid?payrollId=5&modifiedBy=Admin
        [HttpPut]
        public async Task<IActionResult> MarkPaid(int payrollId, string modifiedBy = "Admin")
        {
            try
            {
                var res = await CreateClient().PutAsync(
                    $"/api/payroll/markpaid/{payrollId}?modifiedBy={modifiedBy}", null!);

                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }

                Console.WriteLine($"[PayrollController] API {res.StatusCode} → SQLite fallback.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayrollController] API unreachable: {ex.Message} → SQLite fallback.");
            }

            await _sqlite.MarkPaidAsync(payrollId, modifiedBy);
            return Json(new { message = "Marked as paid.", source = "offline" });
        }
    }
}