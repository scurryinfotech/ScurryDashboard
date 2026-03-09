using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ScurryDashboard.Models;
namespace ScurryDashboard.Controllers
{
    public class DailyExpensesController : BaseApiController
    {
        public DailyExpensesController(IHttpClientFactory f, IConfiguration c) : base(f, c) { }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var res = await CreateClient().GetAsync("/api/dailyexpenses");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<IEnumerable<DailyExpense>>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/dailyexpenses/{id}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<DailyExpense>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] DailyExpense model)
        {
            try
            {
                var res = await CreateClient().PostAsync("/api/dailyexpenses", JsonBody(model));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut]
        public async Task<IActionResult> Update(int id, [FromBody] DailyExpense model)
        {
            try
            {
                var res = await CreateClient().PutAsync($"/api/dailyexpenses/{id}", JsonBody(model));
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
                var res = await CreateClient().DeleteAsync($"/api/dailyexpenses/{id}?modifiedBy={modifiedBy}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(int? dailyExpenseId)
        {
            try
            {
                var url = dailyExpenseId.HasValue ? $"/api/dailyexpenses/logs?dailyExpenseId={dailyExpenseId}" : "/api/dailyexpenses/logs";
                var res = await CreateClient().GetAsync(url);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }

}
