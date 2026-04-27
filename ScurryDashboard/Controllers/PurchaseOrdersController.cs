using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class PurchaseOrdersController(IHttpClientFactory http, IConfiguration config) : Controller
    {
        private readonly IHttpClientFactory _http = http;
        private readonly string _api = config["Api:Url"];

        private HttpClient Client()
        {
            return _http.CreateClient(); 
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await Client().GetAsync(_api + "/api/PurchaseOrders/GetAll");
            var data = await res.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await Client().GetAsync(_api + $"/api/PurchaseOrders/GetById/{id}");
            var data = await res.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> GetByVendor(int vendorId)
        {
            var res = await Client().GetAsync(_api + $"/api/PurchaseOrders/GetByVendor/{vendorId}");
            var data = await res.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] object model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await Client().PostAsync(_api + "/api/PurchaseOrders/Save", content);
            var body = await res.Content.ReadAsStringAsync();

            return res.IsSuccessStatusCode
                ? Content(body, "application/json")
                : StatusCode((int)res.StatusCode, body);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await Client().DeleteAsync(_api + $"/api/PurchaseOrders/Delete/{id}");
            return res.IsSuccessStatusCode ? Ok() : StatusCode((int)res.StatusCode);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyTotals(int? year = null, int? vendorId = null)
        {
            var url = _api + "/api/PurchaseOrders/GetMonthlyTotals";

            var qs = new List<string>();
            if (year.HasValue) qs.Add($"year={year}");
            if (vendorId.HasValue) qs.Add($"vendorId={vendorId}");

            if (qs.Any())
                url += "?" + string.Join("&", qs);

            var res = await Client().GetAsync(url);
            var data = await res.Content.ReadAsStringAsync();

            return Content(data, "application/json");
        }
    }
}
