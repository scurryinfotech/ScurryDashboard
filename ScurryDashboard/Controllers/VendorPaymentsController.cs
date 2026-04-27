
    using Microsoft.AspNetCore.Mvc;
    using System.Text;
    using System.Text.Json;
    namespace ScurryDashboard.Controllers
    {
        public class VendorPaymentsController(IHttpClientFactory http, IConfiguration config) : Controller
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
                var res = await Client().GetAsync(_api + "/api/VendorPayments/GetAll");
                var data = await res.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }

            [HttpGet]
            public async Task<IActionResult> GetByOrder(int orderId)
            {
                var res = await Client().GetAsync(_api + $"/api/VendorPayments/GetByOrder/{orderId}");
                var data = await res.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }

            [HttpGet]
            public async Task<IActionResult> GetByVendor(int vendorId)
            {
                var res = await Client().GetAsync(_api + $"/api/VendorPayments/GetByVendor/{vendorId}");
                var data = await res.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }

            [HttpPost]
            public async Task<IActionResult> Save([FromBody] object model)
            {
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var res = await Client().PostAsync(_api + "/api/VendorPayments/Save", content);
                var body = await res.Content.ReadAsStringAsync();

                return res.IsSuccessStatusCode
                    ? Content(body, "application/json")
                    : StatusCode((int)res.StatusCode, body);
            }
        }
    }

