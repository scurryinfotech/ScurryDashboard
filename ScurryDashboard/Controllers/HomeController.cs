using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScurryDashboard.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private IConfiguration Configuration;
        private readonly string apiPort;
        private readonly string userName;
        private readonly string token;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            Configuration = configuration;
            apiPort = configuration["Api:Url"];
            userName = configuration["Api:user"];
            token = configuration["Api:Token"];
        }
        public async Task<IActionResult> GetOrder()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/GetOrder?UserName=" + userName;
            string jwtToken = token;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await client.GetAsync(apiUrl);
                var rawJson = await response.Content.ReadAsStringAsync();

                var orders = JsonSerializer.Deserialize<List<OrderListModel>>(rawJson);


                var offlineOrders = orders
                    ?.Where(o => o.OrderType != null && o.OrderType.Equals("Offline", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Json(offlineOrders);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Order API");
                return StatusCode(500, "Error calling Order API");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailabilityHomeDelivery()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/GetAvailabilityOnline";//?UserName=" + userName
            
            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
           
            try
            {
                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {error}");
                    return StatusCode((int)response.StatusCode, error);
                }

                var rawJson = await response.Content.ReadAsStringAsync();
                return Content(rawJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetAvailabilityHomeDelivery API");
                return StatusCode(500, "Error calling GetAvailabilityHomeDelivery API");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTableCount()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/GetTableCount?UserName=" + userName;
            string jwtToken = token;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                var rawJson = await response.Content.ReadAsStringAsync();

                return Content(rawJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetTableCount API");
                return StatusCode(500, "Error calling GetTableCount API");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SoftDeleteOrder([FromBody] int id)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/SoftDeleteOrder?UserName=" + userName;
            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            // Send ItemId as JSON
            var json = JsonSerializer.Serialize(id);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
                return Ok();
            return StatusCode((int)response.StatusCode, "Failed to soft delete order");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderItem([FromBody] OrderListModel updatedOrder)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/UpdateOrderItem?UserName=" + userName;
            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var json = JsonSerializer.Serialize(updatedOrder);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
                return Ok();
            return StatusCode((int)response.StatusCode, "Failed to update order");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTableOrderItem([FromBody] OrderListModel updatedOrder)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/UpdateTableOrderItem?UserName=" + userName;
            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var json = JsonSerializer.Serialize(updatedOrder);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
                return Ok();
            return StatusCode((int)response.StatusCode, "Failed to update order");
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrderSummary([FromBody] OrderSummaryModel summary)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var apiUrl = apiPort + "/api/Order/SaveOrderSummary";
                var json = JsonSerializer.Serialize(summary);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                    return Ok("Summary saved");

                return StatusCode((int)response.StatusCode, "Failed to save summary");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrderSummaryOnline([FromBody] OrderSummaryModel summary)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var apiUrl = apiPort + "/api/Order/SaveOrderSummaryOnline";
                var json = JsonSerializer.Serialize(summary);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                    return Ok("Summary saved");

                return StatusCode((int)response.StatusCode, "Failed to save summary");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderHistory()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/GetOrderHistory?UserName=" + userName;


            string jwtToken = token;

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await client.GetAsync(apiUrl);
                var rawJson = await response.Content.ReadAsStringAsync();

                var orders = JsonSerializer.Deserialize<List<OrderHistoryModel>>(rawJson,
                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


                if (orders == null)
                    return Json(new List<OrderHistoryModel>());


                DateTime threeDaysAgo = DateTime.Now.AddDays(-2).Date;
                DateTime today = DateTime.Now.Date;

                return Json(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Order API");
                return StatusCode(500, "Error calling Order API");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBillData(string orderId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                apiPort + "/api/Order/GetBillByOrderId?orderId="+ orderId
            );

            string json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserModel user)
        {
            if (user == null || string.IsNullOrEmpty(user.loginame) || string.IsNullOrEmpty(user.Password))
                return BadRequest("Username and password are required.");

            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/Login";

            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Optionally, deserialize the response if you expect a token or user info
                    return Content(responseContent, "application/json");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Unauthorized(responseContent);
                }
                else
                {
                    return BadRequest("Invalid details please login in with valid details");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Login API");
                return StatusCode(500, "Error calling Login API");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Signup([FromBody] UserModel user)
        {
            if (user == null || string.IsNullOrEmpty(user.loginame) || string.IsNullOrEmpty(user.Password))
                return BadRequest("Username and password are required.");

            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/AddUser";
            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Optionally, deserialize the response if you expect a token or user info  
                    return Content(responseContent, "application/json");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Unauthorized(responseContent);
                }
                else
                {
                    return BadRequest("The email you have used is already present.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AddUser API");
                return StatusCode(500, "Error calling AddUser API");
            }
        }

        [HttpGet]
        public IActionResult Home()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult SignupDash()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region This line is  for getting online orders 

        [HttpPost]
        public async Task<IActionResult> SetAvailabilityHomeDelivery([FromBody] bool isAvailable)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/SetAvailabilityHome";

            string jwtToken = token;

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var json = JsonSerializer.Serialize(isAvailable);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {

                    return Ok(new { success = true, value = isAvailable });
                }

                var error = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, error);
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Error calling SetAvailability API");
            }
        }
        public async Task<IActionResult> GetOrderOnline()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/GetOrderOnline?UserName=" + userName;
            string jwtToken = token;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await client.GetAsync(apiUrl);
                var rawJson = await response.Content.ReadAsStringAsync();
                var orders = JsonSerializer.Deserialize<List<OrderListModel>>(rawJson);

                return Json(orders);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Order API");
                return StatusCode(500, "Error calling Order API");
            }
        }
        // this is for the online update status

        [HttpPut]
        public async Task<IActionResult> UpdateOnlineStatus([FromBody] UpdateonlineOrder updatedOrder)
        {
            var client = _httpClientFactory.CreateClient();

            var apiUrl = apiPort + "/api/Order/UpdateOrderStatus";
            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var json = JsonSerializer.Serialize(updatedOrder);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
                return Ok();
            return StatusCode((int)response.StatusCode, "Failed to update order");
        }

        [HttpPost]
        public async Task<IActionResult> RejectOnlineOrder([FromBody] string OrderId)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/RejectOnlineOrder";

            var json = JsonSerializer.Serialize(OrderId);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
                return Ok();
            return StatusCode((int)response.StatusCode, "Failed to delete order");
        }
        #endregion

        #region Coffee app methods
        [HttpPost]
        public async Task<IActionResult> SetAvailability([FromBody] bool isAvailable)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/SetAvailability";

            string jwtToken = token;

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var json = JsonSerializer.Serialize(isAvailable);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {

                    return Ok(new { success = true, value = isAvailable });
                }

                var error = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, error);
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Error calling SetAvailability API");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoffeeAvailability()
        {
            var client = _httpClientFactory.CreateClient();

            var apiUrl = apiPort + "/api/Order/GetAvailability?UserName=" + userName;

            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {error}");
                    return StatusCode((int)response.StatusCode, error);
                }

                var rawJson = await response.Content.ReadAsStringAsync();

                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    if (bool.TryParse(rawJson.Trim('"'), out var boolVal))
                    {
                        return Json(boolVal);
                    }

                    using var doc = JsonDocument.Parse(rawJson);


                    return Content(rawJson, "application/json");
                }
                catch
                {
                    return Content(rawJson, "application/json");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetCoffeeAvailability API");
                return StatusCode(500, "Error calling GetCoffeeAvailability API");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoffeeOrders()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/CoffeeOrdersDetails?UserName=" + userName;

            string jwtToken = token;

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await client.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {error}");
                    return StatusCode((int)response.StatusCode, error);
                }

                var rawJson = await response.Content.ReadAsStringAsync();

                // ? FIX: Allow case-insensitive property name matching
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var orders = JsonSerializer.Deserialize<List<GetOrderCoffeeDetails>>(rawJson, options);

                return Json(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Order API");
                return StatusCode(500, "Error calling Order API");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCoffeeOrderStatus([FromBody] updateCoffeeDetails updatedCoffeeOrder)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = apiPort + "/api/Order/UpdateCoffeeOrder?UserName=" + userName;
            string jwtToken = token;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var json = JsonSerializer.Serialize(updatedCoffeeOrder);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
                return Ok();
            return StatusCode((int)response.StatusCode, "Failed to update coffee order");
        }
        #endregion
    }
}

