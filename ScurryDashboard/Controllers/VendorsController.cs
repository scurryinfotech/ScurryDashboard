using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class VendorsController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly string _api;

        public VendorsController(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _api = config["Api:Url"];
        }

        private HttpClient Client()
        {
            return _http.CreateClient(); 
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await Client().GetAsync(_api + "/api/Vendors/GetAll");
            var data = await res.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await Client().GetAsync(_api + $"/api/Vendors/GetById/{id}");
            var data = await res.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] object model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await Client().PostAsync(_api + "/api/Vendors/Save", content);
            var body = await res.Content.ReadAsStringAsync();

            return res.IsSuccessStatusCode
                ? Content(body, "application/json")
                : StatusCode((int)res.StatusCode, body);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await Client().DeleteAsync(_api + $"/api/Vendors/Delete/{id}");
            return res.IsSuccessStatusCode ? Ok() : StatusCode((int)res.StatusCode);
        }

        [HttpGet]
        public async Task<IActionResult> GetLedger(int? vendorId = null)
        {
            var url = _api + "/api/Vendors/GetLedger" +
                      (vendorId.HasValue ? $"?vendorId={vendorId}" : "");

            var res = await Client().GetAsync(url);
            var data = await res.Content.ReadAsStringAsync();

            return Content(data, "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var res = await Client().GetAsync(_api + "/api/Vendors/GetDashboardStats");
            var data = await res.Content.ReadAsStringAsync();

            return Content(data, "application/json");
        }


    }
}