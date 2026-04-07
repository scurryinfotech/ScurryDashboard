using Microsoft.AspNetCore.Mvc;
using OrderService.Repository.Interface;
using ScurryDashboard.Models;
using System.Text.Json;

namespace ScurryDashboard.Controllers
{
    // Repository-backed controller exposed under /Repository/{action}
    [Route("Repository/[action]")]
    public class RepositoryController : Controller
    {
        private readonly ILogger<RepositoryController> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IConfiguration _configuration;
        private readonly string _userName;

        public RepositoryController(ILogger<RepositoryController> logger, IOrderRepository orderRepository, IConfiguration configuration)
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _configuration = configuration;
            _userName = configuration["Api:user"];
        }

        // Place an online order (used for delivery/pickup flows)
        [HttpPost]
        public async Task<IActionResult> PlaceOnline([FromBody] ScurryDashboard.Models.OrderModel order)
        {
            if (order == null) return BadRequest("Order required");
            try
            {
                var mapped = MapOrderModel(order);

                // enforce fixed root user for dashboard-originated orders
                int rootId = 2;
                try { rootId = Convert.ToInt32(_configuration["Repository:RootUserId"] ?? "2"); } catch { rootId = 2; }
                mapped.userName = rootId;

                // keep any provided userId (for online orders) if present
                // call repository placeOnline which inserts UserId when provided
                var success = await _orderRepository.placeOnline(mapped);
                if (success) return Ok("Saved");
                return StatusCode(500, "Failed to save online order");
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "Error saving online order via repository");
                return StatusCode(500, "Error saving online order");
            }
        }

        // ----- Mapping helpers between ScurryDashboard.Models and OrderService.Model -----
        private OrderService.Model.OrderModel MapOrderModel(ScurryDashboard.Models.OrderModel src)
        {
            if (src == null) return null!;
            return new OrderService.Model.OrderModel
            {
                selectedTable = src.selectedTable,
                userName = src.userName,
                customerName = src.customerName,
                userPhone = src.userPhone,
                OrderType = src.OrderType,
                Address = src.Address,
                specialInstruction = src.specialInstruction,
                deliveryType = src.deliveryType,
                userId = src.userId,
                orderItems = src.orderItems?.Select(i => new OrderService.Model.OrderModel.OrderItem
                {
                    item_id = i.item_id,
                    full = i.full,
                    half = i.half,
                    Price = i.Price
                }).ToList() ?? new List<OrderService.Model.OrderModel.OrderItem>()
            };
        }

        private OrderService.Model.OrderSummaryModel MapSummary(ScurryDashboard.Models.OrderSummaryModel src)
        {
            if (src == null) return null!;
            return new OrderService.Model.OrderSummaryModel
            {
                OrderId = src.OrderId,
                CustomerName = src.CustomerName,
                Phone = src.Phone,
                TotalAmount = src.TotalAmount,
                DiscountAmount = src.DiscountAmount,
                FinalAmount = src.FinalAmount,
                PaymentMode = src.PaymentMode
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetOrder(string? userName = null)
        {
            try
            {
                // If the caller didn't supply a userName (the JS calls /Repository/GetOrder without query)
                // fall back to configured API user. MVC will bind `userName` to null when not provided.
                var effectiveUser = !string.IsNullOrWhiteSpace(userName) ? userName : _userName;
                var orders = await _orderRepository.GetOrder(effectiveUser);
             
                var tableOrderTypes = new[] { "offline", "dine", "table", "walk-in", "walkin" };
                var offlineOrders = orders?.Where(o => o != null && (
                        (o.TableNo > 0) ||
                        string.IsNullOrEmpty(o.OrderType) ||
                        tableOrderTypes.Contains((o.OrderType ?? string.Empty).ToLowerInvariant())
                    )).ToList() ?? new List<OrderService.Model.OrderListModel>();

                // Project to a lightweight camel-cased shape expected by the frontend (home.js)
                var payload = offlineOrders.Select(o => new 
                {
                    id = o.Id,
                    orderId = o.OrderId,
                    orderStatusId = o.OrderStatusId,
                    orderStatus = o.OrderStatus,
                    itemName = o.ItemName,
                    halfPortion = o.HalfPortion,
                    fullPortion = o.FullPortion,
                    tableNo = o.TableNo,
                    price = o.Price,
                    customerName = o.customerName,
                    phone = o.phone,
                    date = o.Date,
                    specialInstructions = o.specialInstructions,
                    isActive = o.IsActive,
                    paymentMode = o.paymentMode,
                    address = o.Address,
                    orderType = o.OrderType,
                }).ToList();

                return Json(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders from repository");
                return StatusCode(500, "Error getting orders");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveTableOrder([FromBody] ScurryDashboard.Models.OrderModel order)
        {
            if (order == null) return BadRequest("Order required");
            try
            {
                var mapped = MapOrderModel(order);

                // enforce fixed root user for dashboard-originated orders
                // config key: Repository:RootUserId (optional) — falls back to 2
                int rootId = 2;
                try { rootId = Convert.ToInt32(_configuration["Repository:RootUserId"] ?? "2"); } catch { rootId = 2; }
                mapped.userName = rootId;

                // ensure selected table from UI is used
                mapped.selectedTable = order.selectedTable;

                var success = await _orderRepository.AddOrder(mapped);
                if (success) return Ok("Saved");
                return StatusCode(500, "Failed to save order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order via repository");
                return StatusCode(500, "Error saving order");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailabilityHomeDelivery()
        {
            try
            {
                var isAvailable = await _orderRepository.GetAvailabilityOnline();
                return Json(isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting availability from repository");
                return StatusCode(500, "Error getting availability");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SoftDeleteOrder([FromBody] int id)
        {
            try
            {
                var ok = await _orderRepository.SoftDeleteOrder(id);
                if (ok) return Ok();
                return StatusCode(500, "Failed to soft delete order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting order");
                return StatusCode(500, "Error soft deleting order");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderItem([FromBody] ScurryDashboard.Models.OrderListModel updatedOrder)
        {
            try
            {
                var mapped = new OrderService.Model.OrderListModel
                {
                    TableNo = updatedOrder.TableNo,
                    Id = updatedOrder.Id,
                    OrderId = updatedOrder.OrderId,
                    ItemName = updatedOrder.ItemName,
                    HalfPortion = updatedOrder.HalfPortion,
                    FullPortion = updatedOrder.FullPortion,
                    Price = updatedOrder.Price,
                    OrderStatusId = updatedOrder.OrderStatusId,
                    OrderStatus = updatedOrder.OrderStatus,
                    statusId = updatedOrder.statusId,
                    Date = updatedOrder.Date,
                    IsActive = updatedOrder.IsActive,
                    customerName = updatedOrder.customerName,
                    phone = updatedOrder.phone,
                    OrderType = updatedOrder.OrderType,
                    Address = updatedOrder.Address,
                    PaymentMode = updatedOrder.PaymentMode,
                    ModifiedDate = updatedOrder.ModifiedDate,
                    createdDate = updatedOrder.createdDate,
                    specialInstructions = updatedOrder.specialInstructions,
                    CreatedDate = updatedOrder.CreatedDate,
                    userId = updatedOrder.userId,
                    Discount = updatedOrder.Discount,
                    DeliveryName = updatedOrder.DeliveryName,
                    DeliveryPhone = updatedOrder.DeliveryPhone
                };
                var ok = await _orderRepository.UpdateOrderStatus(mapped);
                if (ok) return Ok();
                return StatusCode(500, "Failed to update order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                return StatusCode(500, "Error updating order");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTableOrderItem([FromBody] ScurryDashboard.Models.OrderListModel updatedOrder)
        {
            try
            {
                var mapped = new OrderService.Model.OrderListModel
                {
                    TableNo = updatedOrder.TableNo,
                    Id = updatedOrder.Id,
                    OrderId = updatedOrder.OrderId,
                    ItemName = updatedOrder.ItemName,
                    HalfPortion = updatedOrder.HalfPortion,
                    FullPortion = updatedOrder.FullPortion,
                    Price = updatedOrder.Price,
                    OrderStatusId = updatedOrder.OrderStatusId,
                    OrderStatus = updatedOrder.OrderStatus,
                    statusId = updatedOrder.statusId,
                    Date = updatedOrder.Date,
                    IsActive = updatedOrder.IsActive,
                    customerName = updatedOrder.customerName,
                    phone = updatedOrder.phone,
                    OrderType = updatedOrder.OrderType,
                    Address = updatedOrder.Address,
                    PaymentMode = updatedOrder.PaymentMode,
                    ModifiedDate = updatedOrder.ModifiedDate,
                    createdDate = updatedOrder.createdDate,
                    specialInstructions = updatedOrder.specialInstructions,
                    CreatedDate = updatedOrder.CreatedDate,
                    userId = updatedOrder.userId,
                    Discount = updatedOrder.Discount,
                    DeliveryName = updatedOrder.DeliveryName,
                    DeliveryPhone = updatedOrder.DeliveryPhone
                };
                var ok = await _orderRepository.UpdateTableOrderStatus(mapped);
                if (ok) return Ok();
                return StatusCode(500, "Failed to update order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating table order");
                return StatusCode(500, "Error updating table order");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrderSummary([FromBody] ScurryDashboard.Models.OrderSummaryModel summary)
        {
            try
            {
                var mapped = MapSummary(summary);
                var ok = await _orderRepository.InsertOrderSummary(mapped);
                if (ok) return Ok("Summary saved");
                return StatusCode(500, "Failed to save summary");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving summary");
                return StatusCode(500, "Error saving summary");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrderSummaryOnline([FromBody] ScurryDashboard.Models.OrderSummaryModel summary)
        {
            try
            {
                var mapped = MapSummary(summary);
                var ok = await _orderRepository.InsertOrderSummaryOnline(mapped);
                if (ok) return Ok("Summary saved");
                return StatusCode(500, "Failed to save summary");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving summary online");
                return StatusCode(500, "Error saving summary online");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderHistory()
        {
            try
            {
                var orders = await _orderRepository.GetOrderHistory(_userName);
                return Json(orders ?? new List<OrderService.Model.OrderHistoryModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order history");
                return StatusCode(500, "Error getting order history");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBillData(string orderId)
        {
            try
            {
                var bills = await _orderRepository.GetBillByOrderId(orderId);
                return Json(bills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bill data");
                return StatusCode(500, "Error getting bill data");
            }
        }

        [HttpGet]
        public IActionResult Home()
        {
            return View("Home");
        }

        [HttpGet]
        public IActionResult NewOrder()
        {
            return View("NewOrder");
        }

        [HttpGet]
        public IActionResult TableManager()
        {
            return View("TableManager");
        }

        public IActionResult Index()
        {
            return View("Index");
        }

        public IActionResult ManageMenu()
        {
            return View("ManageMenu");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        public IActionResult SignupDash()
        {
            return View("SignupDash");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region Online orders
        [HttpPost]
        public async Task<IActionResult> SetAvailabilityHomeDelivery([FromBody] bool isAvailable)
        {
            try
            {
                // IOrderRepository exposes GetAvailabilityOnline and UpdateAvailability - no UpdateAvailabilityOnline
                // Map to UpdateAvailability if appropriate, otherwise call GetAvailabilityOnline for reads
                var ok = await _orderRepository.UpdateAvailability(isAvailable);
                if (ok) return Ok(new { success = true, value = isAvailable });
                return StatusCode(500, "Failed to update availability");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting availability");
                return StatusCode(500, "Error setting availability");
            }
        }

        public async Task<IActionResult> GetOrderOnline()
        {
            try
            {
                int.TryParse(_userName, out var userId);
                var orders = await _orderRepository.GetOrderHomeDelivery(userId);
                return Json(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online orders");
                return StatusCode(500, "Error getting online orders");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateOnlineStatus([FromBody] UpdateonlineOrder updatedOrder)
        {
            try
            {
                var ok = await _orderRepository.UpdateOnlineStatus(new OrderService.Model.OnlineOrderModel
                {
                    OrderId = updatedOrder.OrderId,
                    OrderStatus = updatedOrder.OrderStatus,
                    paymentMode = updatedOrder.paymentMode,
                    DeliveryStaffId = updatedOrder.DeliveryStaffId
                });
                if (ok) return Ok();
                return StatusCode(500, "Failed to update order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating online status");
                return StatusCode(500, "Error updating online status");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectOnlineOrder([FromBody] string OrderId)
        {
            try
            {
                var ok = await _orderRepository.RejectOnlineOrder(OrderId);
                if (ok) return Ok();
                return StatusCode(500, "Failed to reject order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting online order");
                return StatusCode(500, "Error rejecting online order");
            }
        }
        #endregion

        #region Coffee app methods
        [HttpPost]
        public async Task<IActionResult> SetAvailability([FromBody] bool isAvailable)
        {
            try
            {
                var ok = await _orderRepository.UpdateAvailability(isAvailable);
                if (ok) return Ok(new { success = true, value = isAvailable });
                return StatusCode(500, "Failed to update availability");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting availability");
                return StatusCode(500, "Error setting availability");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoffeeAvailability()
        {
            try
            {
                var avail = await _orderRepository.GetAvailability();
                return Json(avail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coffee availability");
                return StatusCode(500, "Error getting availability");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoffeeOrders()
        {
            try
            {
                var orders = await _orderRepository.GetCoffeeOrdersDetails(_userName);
                return Json(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coffee orders");
                return StatusCode(500, "Error getting coffee orders");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCoffeeOrderStatus([FromBody] System.Text.Json.JsonElement payload)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var model = JsonSerializer.Deserialize<ScurryDashboard.Models.UpdateCoffeeDetails>(payload.GetRawText(), options);
                if (model == null) return BadRequest("Invalid payload");

                var ok = await _orderRepository.UpdateCoffeeOrderStatus(new OrderService.Model.updateCoffeeDetails
                {
                    OrderId = model.OrderId,
                    Status = model.Status
                });
                if (ok) return Ok();
                return StatusCode(500, "Failed to update coffee order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating coffee order status");
                return StatusCode(500, "Error updating coffee order status");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectCoffeeOrder([FromBody] string OrderId)
        {
            try
            {
                var ok = await _orderRepository.RejectCoffeeOrder(OrderId);
                if (ok) return Ok();
                return StatusCode(500, "Failed to delete order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting coffee order");
                return StatusCode(500, "Error rejecting coffee order");
            }
        }
        #endregion

        #region Menu endpoints
        [HttpGet]
        public async Task<IActionResult> GetTableCount()
        {
            try
            {
                var count = await _orderRepository.GetTableCount(_userName); 
                return Json(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table count");
                return StatusCode(500, "Error getting table count");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuCategory()
        {
            try
            {
                var categories = await _orderRepository.GetMenuCategory(_userName);
                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting menu categories");
                return StatusCode(500, "Error getting menu categories");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuSubcategory()
        {
            try
            {
                var subs = await _orderRepository.GetMenuSubcategory(_userName);
                return Json(subs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting menu subcategories");
                return StatusCode(500, "Error getting menu subcategories");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItem()
        {
            try
            {
                var items = await _orderRepository.GetMenuItem(_userName);
                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting menu items");
                return StatusCode(500, "Error getting menu items");
            }
        }

        #endregion

        #region Manage menu (create/update) - forwarding form data through repository isn't implemented here
        [HttpPost]
        public async Task<IActionResult> SaveMenuCategory([FromBody] JsonElement payload)
        {
            return StatusCode(501, "Not implemented via repository");
        }

        [HttpPut]
        public async Task<IActionResult> SaveMenuCategory(int id, [FromBody] JsonElement payload)
        {
            return StatusCode(501, "Not implemented via repository");
        }

        [HttpPost]
        public async Task<IActionResult> SaveMenuSubcategory([FromBody] JsonElement payload)
        {
            return StatusCode(501, "Not implemented via repository");
        }

        [HttpPut]
        public async Task<IActionResult> SaveMenuSubcategory(int id, [FromBody] JsonElement payload)
        {
            return StatusCode(501, "Not implemented via repository");
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> SaveMenuItem()
        {
            return StatusCode(501, "Not implemented via repository");
        }

        [HttpPut]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> SaveMenuItem(int id)
        {
            return StatusCode(501, "Not implemented via repository");
        }

        [HttpPost]
        public async Task<IActionResult> SaveTableCount([FromBody] int count)
        {
            if (count <= 0) return BadRequest(new { success = false, message = "Table count must be greater than 0" });
            try
            {
                // Use OrderSummary insert to persist table count (stored in TotalAmount field)
                var summary = new OrderService.Model.OrderSummaryModel
                {
                    OrderId = string.Empty,
                    CustomerName = "",
                    Phone = "",
                    TotalAmount = count,
                    DiscountAmount = 0,
                    FinalAmount = count,
                    PaymentMode = string.Empty
                };

                var saved = await _orderRepository.InsertOrderSummary(summary);
                if (saved) return Ok(new { success = true, message = "Table count saved" });
                return StatusCode(500, new { success = false, message = "Failed to save table count" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving table count");
                return StatusCode(500, new { success = false, message = "Error saving table count" });
            }
        }

        #endregion
    }
}
