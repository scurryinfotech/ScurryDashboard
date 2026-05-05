using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ScurryDashboard.Models;

using OrderService.Repository.Interface; 
namespace ScurryDashboard.Controllers
{
    public class StaffController : BaseApiController
    {
        private readonly IStaffRepository _sqlite; // ← fallback repo

        public StaffController(
            IHttpClientFactory f,
            IConfiguration c,
            IStaffRepository sqlite   // ← inject via DI
        ) : base(f, c)
        {
            _sqlite = sqlite;
        }

        // Renders the view — JS loads data via Ajax
        public IActionResult Index() => View();
        public IActionResult Profile(int id)
        {
            ViewBag.StaffId = id;
            return View();
        }

        // ── GET ALL ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var res = await CreateClient().GetAsync("/api/staff");
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<IEnumerable<Staff>>(body, _json));
                }

                // API returned non-success → fall back to SQLite
                Console.WriteLine($"[StaffController] API returned {res.StatusCode}, falling back to SQLite.");
            }
            catch (Exception ex)
            {
                // API unreachable → fall back to SQLite
                Console.WriteLine($"[StaffController] API error: {ex.Message}, falling back to SQLite.");
            }

            var offlineData = await _sqlite.GetAllStaffAsync();
            return Json(offlineData);
        }

        // ── GET BY ID ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var res = await CreateClient().GetAsync($"/api/staff/{id}");
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<Staff>(body, _json));
                }

                Console.WriteLine($"[StaffController] API returned {res.StatusCode} for GetById({id}), falling back to SQLite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffController] API error: {ex.Message}, falling back to SQLite.");
            }

            var offlineStaff = await _sqlite.GetStaffByIdAsync(id);
            if (offlineStaff == null)
                return NotFound(new { message = $"Staff {id} not found in offline DB either." });

            return Json(offlineStaff);
        }

        // ── INSERT ───────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] Staff model)
        {
            try
            {
                var res = await CreateClient().PostAsync("/api/staff", JsonBody(model));
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }

                Console.WriteLine($"[StaffController] API returned {res.StatusCode} for Insert, falling back to SQLite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffController] API error: {ex.Message}, falling back to SQLite.");
            }

            // Map Staff model → StaffRequest for the SQLite repo
            var req = MapToRequest(model);
            var newId = await _sqlite.InsertStaffAsync(req);
            return Json(new { id = newId, source = "offline" });
        }

        // ── UPDATE ───────────────────────────────────────────────────
        [HttpPut]
        public async Task<IActionResult> Update(int id, [FromBody] Staff model)
        {
            try
            {
                var res = await CreateClient().PutAsync($"/api/staff/{id}", JsonBody(model));
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }

                Console.WriteLine($"[StaffController] API returned {res.StatusCode} for Update({id}), falling back to SQLite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffController] API error: {ex.Message}, falling back to SQLite.");
            }

            var req = MapToRequest(model);
            await _sqlite.UpdateStaffAsync(id, req);
            return Json(new { updated = true, source = "offline" });
        }

        // ── DELETE ───────────────────────────────────────────────────
        [HttpDelete]
        public async Task<IActionResult> Delete(int id, [FromQuery] string modifiedBy = "Admin")
        {
            try
            {
                var res = await CreateClient().DeleteAsync($"/api/staff/{id}?modifiedBy={modifiedBy}");
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }

                Console.WriteLine($"[StaffController] API returned {res.StatusCode} for Delete({id}), falling back to SQLite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffController] API error: {ex.Message}, falling back to SQLite.");
            }

            await _sqlite.SoftDeleteStaffAsync(id, modifiedBy);
            return Json(new { deleted = true, source = "offline" });
        }

        // ── GET LOGS ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetLogs(int? staffId)
        {
            try
            {
                var url = staffId.HasValue
                    ? $"/api/staff/logs?staffId={staffId}"
                    : "/api/staff/logs";

                var res = await CreateClient().GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    return Json(JsonSerializer.Deserialize<object>(body, _json));
                }

                Console.WriteLine($"[StaffController] API returned {res.StatusCode} for GetLogs, falling back to SQLite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaffController] API error: {ex.Message}, falling back to SQLite.");
            }

            var offlineLogs = await _sqlite.GetStaffLogsAsync(staffId);
            return Json(offlineLogs);
        }

        // ── HELPER: map Staff model → StaffRequest ───────────────────
        private static OrderService.Model.StaffRequest MapToRequest(Staff m) => new OrderService.Model.StaffRequest
        {
            FullName = m.FullName,
            RoleId = m.RoleId,
            Phone = m.Phone,
            Email = m.Email,
            CNIC = m.CNIC,
            Department = m.Department,
            Salary = m.Salary,
            JoinDate = m.JoinDate,
            IsActive = m.IsActive,
            ModifiedBy = m.ModifiedBy ?? "System",
            CreatedBy = m.CreatedBy,
        };
    }
}