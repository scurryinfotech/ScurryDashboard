using Microsoft.AspNetCore.Mvc;
using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IOrderRepository
    {
        int GetTableCount(string userName);
        Task<List<OrderListModel>> GetOrder(string UserName);
        Task<List<MenuCategory>> GetMenuCategory(string UserName);
        Task<List<MenuSubcategory>> GetMenuSubcategory(string UserName);
        Task<List<MenuItem>> GetMenuItem(string UserName);
        Task<bool> AddOrder(OrderModel order);
        Task<Tuple<bool, string,int>> IsAuthenticated(string username, string password);
        Task<bool> InsertToken(string username, string token, DateTime expiryDate);
        Task<bool> SoftDeleteOrder(int itemId);
        Task<bool> UpdateOrderStatus(OrderListModel updatedOrders);
        Task<List<OrderListModel>> GetOrderHomeDelivery(int userId);
        Task<bool> placeOnline(OrderModel order);
        Task<bool> UpdateOnlineStatus(OnlineOrderModel updatedOrders);
        Task<List<CoffeeMenu>> GetCoffeeMenu(string username);
        Task<bool> CoffeeOrder(CoffeeOrder order);
        Task<List<GetOrderCoffeeDetails>> GetCoffeeOrdersDetails(string username);
        Task<bool> UpdateCoffeeOrderStatus(updateCoffeeDetails updatedOrders);
        Task<bool> GetAvailability();
        Task<bool> UpdateAvailability(bool isAvailable);
        Task<bool> InsertOrderSummary(OrderSummaryModel model);
        Task<List<OrderBillModel>> GetBillByOrderId(string orderId);
        Task GetOrderOrderHistoryDash(string username);
        Task<List<OrderHistoryModel>> GetOrderHistory(string username);
        Task<bool> RejectOnlineOrder(string orderId);
        Task<bool> RejectCoffeeOrder(string orderId);
        Task<bool> ResetPasswordOnline(string phone, string newPassword);
        Task<bool> InsertOrderSummaryOnline(OrderSummaryModel summary);
        Task<bool> GetAvailabilityOnline();
        Task<bool> UpdateTableOrderStatus(OrderListModel updatedTableOrders);
        Task<bool> CheckPhoneExists(string phone);
        Task<CustomerAddressDto?> GetCustomerAddressOnline(string userId);
        Task<int> GetFixedDiscountAsync();
    }
}
