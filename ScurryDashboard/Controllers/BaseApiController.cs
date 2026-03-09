using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    public class BaseApiController : Controller
    {
        protected readonly IHttpClientFactory _httpFactory;
        protected readonly IConfiguration _config;
        protected readonly string _apiBase;

        protected static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BaseApiController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _config = config;
            _apiBase = config["Url"] ?? "https://localhost:7104";
        }

        protected HttpClient CreateClient()
        {
            var client = _httpFactory.CreateClient("ApiClient");
            client.BaseAddress = new Uri(_apiBase);

            // Attach JWT from session if available
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        protected StringContent JsonBody<T>(T obj) =>
            new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
    }
}
