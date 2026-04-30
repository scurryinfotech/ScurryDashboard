using Microsoft.AspNetCore.Mvc;
using OrderService.Repository.Interface;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class DailyExpensesController : BaseApiController
    {
        private readonly IDailyExpenseRepository _repo;
        private readonly bool _useExternalApi;

        public DailyExpensesController(IHttpClientFactory f, IConfiguration c, IDailyExpenseRepository repo) : base(f, c)
        {
            _repo = repo;
            _useExternalApi = c.GetValue<bool>("UseExternalApi");
        }
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                if (_useExternalApi)
                {
                    var res = await CreateClient().GetAsync("/api/dailyexpenses");
                    var body = await res.Content.ReadAsStringAsync();
                    if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                    return Json(JsonSerializer.Deserialize<IEnumerable<OrderService.Model.DailyExpense>>(body, _json));
                }
                else
                {
                    var list = await _repo.GetAllAsync();
                    return Json(list);
                }
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (_useExternalApi)
                {
                    var res = await CreateClient().GetAsync($"/api/dailyexpenses/{id}");
                    var body = await res.Content.ReadAsStringAsync();
                    if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                    return Json(JsonSerializer.Deserialize<OrderService.Model.DailyExpense>(body, _json));
                }
                else
                {
                    var item = await _repo.GetByIdAsync(id);
                    return Json(item);
                }
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] ScurryDashboard.Models.DailyExpense model)
        {
            try
            {
                if (_useExternalApi)
                {
                    var res = await CreateClient().PostAsync("/api/dailyexpenses", JsonBody(model));
                    var body = await res.Content.ReadAsStringAsync();
                    if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }
                else
                {
                    var req = new OrderService.Model.DailyExpenseRequest
                    {
                        Title = model.Title,
                        Category = model.Category,
                        Amount = model.Amount,
                        ExpenseDate = model.ExpenseDate,
                        PaidBy = model.PaidBy,
                        PaymentMode = string.IsNullOrWhiteSpace(model.PaymentMode) ? "Online" : model.PaymentMode,
                        Notes = model.Notes,
                        IsActive = model.IsActive,
                        ModifiedBy = model.ModifiedBy ?? "System",
                    };

                    var newId = await _repo.InsertAsync(req);
                    return Json(new { id = newId });
                }
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut]
        public async Task<IActionResult> Update(int id, [FromBody] ScurryDashboard.Models.DailyExpense model)
        {
            try
            {
                if (_useExternalApi)
                {
                    var res = await CreateClient().PutAsync($"/api/dailyexpenses/{id}", JsonBody(model));
                    var body = await res.Content.ReadAsStringAsync();
                    if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }
                else
                {
                    var req = new OrderService.Model.DailyExpenseRequest
                    {
                        Title = model.Title,
                        Category = model.Category,
                        Amount = model.Amount,
                        ExpenseDate = model.ExpenseDate,
                        PaidBy = model.PaidBy,
                        PaymentMode = string.IsNullOrWhiteSpace(model.PaymentMode) ? "Online" : model.PaymentMode,
                        Notes = model.Notes,
                        IsActive = model.IsActive,
                        ModifiedBy = model.ModifiedBy ?? "System",
                    };

                    await _repo.UpdateAsync(id, req);
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id, [FromQuery] string modifiedBy = "Admin")
        {
            try
            {
                if (_useExternalApi)
                {
                    var res = await CreateClient().DeleteAsync($"/api/dailyexpenses/{id}?modifiedBy={modifiedBy}");
                    var body = await res.Content.ReadAsStringAsync();
                    if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }
                else
                {
                    await _repo.SoftDeleteAsync(id, modifiedBy);
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(int? expenseId)
        {
            try
            {
                if (_useExternalApi)
                {
                    var url = expenseId.HasValue
                        ? $"/api/dailyexpenses/logs?expenseId={expenseId}"
                        : "/api/dailyexpenses/logs";

                    var res = await CreateClient().GetAsync(url);
                    var body = await res.Content.ReadAsStringAsync();
                    if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, new { message = body });
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }
                else
                {
                    var logs = await _repo.GetLogsAsync(expenseId);
                    return Json(logs);
                }
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}