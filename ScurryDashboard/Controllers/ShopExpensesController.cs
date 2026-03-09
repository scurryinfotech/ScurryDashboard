using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ScurryDashboard.Models;

namespace ScurryDashboard.Controllers
{
    public class ShopExpensesController : BaseApiController
    {
        public ShopExpensesController(IHttpClientFactory f, IConfiguration c) : base(f, c) { }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var res = await CreateClient().GetAsync("/api/shopexpenses");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<IEnumerable<ShopExpense>>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/shopexpenses/{id}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<ShopExpense>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] ShopExpense model)
        {
            try
            {
                var res = await CreateClient().PostAsync("/api/shopexpenses", JsonBody(model));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut]
        public async Task<IActionResult> Update(int id, [FromBody] ShopExpense model)
        {
            try
            {
                var res = await CreateClient().PutAsync($"/api/shopexpenses/{id}", JsonBody(model));
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
                var res = await CreateClient().DeleteAsync($"/api/shopexpenses/{id}?modifiedBy={modifiedBy}");
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(int? expenseId)
        {
            try
            {
                var url = expenseId.HasValue ? $"/api/shopexpenses/logs?expenseId={expenseId}" : "/api/shopexpenses/logs";
                var res = await CreateClient().GetAsync(url);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                return Json(JsonSerializer.Deserialize<object>(body, _json));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
