using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;
using System.Data;

namespace OrderService.Repository.Service
{
    public class OrderSQLiteRepository : IOrderRepository
    {
        private readonly string _sqliteCs;
        private readonly string _sqlServerCs;

        public OrderSQLiteRepository(IConfiguration cfg)
        {
            _sqliteCs = cfg.GetConnectionString("SQLiteConnection")!;
            _sqlServerCs = cfg.GetConnectionString("ConnStringDb")!;
        }

        // ════════════════════════════════════════════════════════
        //  MAPPERS
        // ════════════════════════════════════════════════════════

        private static OrderListModel MapOrder(SqliteDataReader r) => new()
        {
            Id = r.IsDBNull(r.GetOrdinal("Id")) ? 0 : r.GetInt32(r.GetOrdinal("Id")),
            OrderId = r.IsDBNull(r.GetOrdinal("OrderId")) ? string.Empty : r.GetString(r.GetOrdinal("OrderId")),
            OrderStatusId = r.IsDBNull(r.GetOrdinal("OrderStatus")) ? 0 : r.GetInt32(r.GetOrdinal("OrderStatus")),
            OrderStatus = r.IsDBNull(r.GetOrdinal("StatusName")) ? string.Empty : r.GetString(r.GetOrdinal("StatusName")),
            ItemName = r.IsDBNull(r.GetOrdinal("item_name")) ? string.Empty : r.GetString(r.GetOrdinal("item_name")),
            FullPortion = r.IsDBNull(r.GetOrdinal("FullPortion")) ? 0 : r.GetInt32(r.GetOrdinal("FullPortion")),
            HalfPortion = r.IsDBNull(r.GetOrdinal("HalfPortion")) ? 0 : r.GetInt32(r.GetOrdinal("HalfPortion")),
            TableNo = r.IsDBNull(r.GetOrdinal("TableNo")) ? 0 : r.GetInt32(r.GetOrdinal("TableNo")),
            Price = r.IsDBNull(r.GetOrdinal("Price")) ? 0 : r.GetDecimal(r.GetOrdinal("Price")),
            customerName = r.IsDBNull(r.GetOrdinal("customerName")) ? string.Empty : r.GetString(r.GetOrdinal("customerName")),
            phone = r.IsDBNull(r.GetOrdinal("phone")) ? string.Empty : r.GetString(r.GetOrdinal("phone")),
            OrderType = r.IsDBNull(r.GetOrdinal("OrderType")) ? string.Empty : r.GetString(r.GetOrdinal("OrderType")),
            Address = r.IsDBNull(r.GetOrdinal("Address")) ? string.Empty : r.GetString(r.GetOrdinal("Address")),
            paymentMode = r.IsDBNull(r.GetOrdinal("payment_mode")) ? string.Empty : r.GetString(r.GetOrdinal("payment_mode")),
            specialInstructions = r.IsDBNull(r.GetOrdinal("specialInstructions")) ? string.Empty : r.GetString(r.GetOrdinal("specialInstructions")),
            IsActive = r.IsDBNull(r.GetOrdinal("IsActive")) ? 0 : r.GetInt32(r.GetOrdinal("IsActive")),
            CreatedDate = r.IsDBNull(r.GetOrdinal("CreatedDate")) ? DateTime.MinValue : DateTime.Parse(r.GetString(r.GetOrdinal("CreatedDate"))),
            ModifiedDate = r.IsDBNull(r.GetOrdinal("ModifiedDate")) ? DateTime.MinValue : DateTime.Parse(r.GetString(r.GetOrdinal("ModifiedDate"))),
            Date = r.IsDBNull(r.GetOrdinal("CreatedDate")) ? DateTime.MinValue : DateTime.Parse(r.GetString(r.GetOrdinal("CreatedDate"))),
        };

        private static OrderListModel MapOrderWithDelivery(SqliteDataReader r)
        {
            var o = MapOrder(r);
            o.userId = r.IsDBNull(r.GetOrdinal("UserId")) ? string.Empty : r.GetString(r.GetOrdinal("UserId"));
            o.Discount = r.IsDBNull(r.GetOrdinal("DiscountAmount")) ? string.Empty : r.GetString(r.GetOrdinal("DiscountAmount"));
            o.DeliveryName = r.IsDBNull(r.GetOrdinal("DeliveryName")) ? string.Empty : r.GetString(r.GetOrdinal("DeliveryName"));
            o.DeliveryPhone = r.IsDBNull(r.GetOrdinal("DeliveryPhone")) ? string.Empty : r.GetString(r.GetOrdinal("DeliveryPhone"));
            return o;
        }

        // ════════════════════════════════════════════════════════
        //  TABLE COUNT
        // ════════════════════════════════════════════════════════

        public async Task<int> GetTableCount(string userName)
        {
            int tableCount = 0;
            try
            {
                // DEBUG: remove this log once fixed
                Console.WriteLine($"[GetTableCount] userName received: '{userName}'");

                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
            SELECT utm.TableCount
            FROM   UserTableMaster utm
            INNER JOIN Users u ON u.Id = utm.UserId
            WHERE  u.Username = @UserName");   

                cmd.Parameters.AddWithValue("@UserName", userName);
                var result = await cmd.ExecuteScalarAsync();

                Console.WriteLine($"[GetTableCount] DB result: '{result}'"); 

                if (result != null && result != DBNull.Value)
                    tableCount = Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetTableCount Error: " + ex.Message);
            }
            return tableCount;
        }


        // ════════════════════════════════════════════════════════
        //  ORDERS — GET
        // ════════════════════════════════════════════════════════

        public async Task<List<OrderListModel>> GetOrder(string userName)
        {
            var list = new List<OrderListModel>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT o.Id, o.OrderId, o.OrderStatus, o.FullPortion, o.HalfPortion,
                           o.TableNo, o.CreatedDate, o.ModifiedDate, o.IsActive,
                           o.item_id, o.Price, o.customerName, o.phone, o.OrderType,
                           o.Address, o.payment_mode, o.specialInstructions,
                           mi.item_name,
                           osm.Name AS StatusName
                    FROM   Orders o
                    LEFT  JOIN menu_items        mi  ON mi.item_id = o.item_id
                    LEFT  JOIN OrderStatusMaster osm ON osm.Id     = o.OrderStatus
                    INNER JOIN Users             u   ON u.Id       = o.CreatedBy
                    WHERE  u.Username = @UserName
                      AND (o.IsActive = 1 OR o.OrderStatus IN (1, 3))
                    ORDER  BY o.CreatedDate DESC, o.OrderId DESC
                    LIMIT  400");
                cmd.Parameters.AddWithValue("@UserName", userName);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync()) list.Add(MapOrder(rdr));
            }
            catch (Exception ex) { Console.WriteLine("GetOrder Error: " + ex.Message); }
            return list;
        }

        public async Task<List<OrderListModel>> GetOrderHomeDelivery(int userId)
        {
            var list = new List<OrderListModel>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT o.Id, o.OrderId, o.OrderStatus, o.FullPortion, o.HalfPortion,
                           o.TableNo, o.CreatedDate, o.ModifiedDate, o.IsActive,
                           o.item_id, o.Price, o.customerName, o.phone, o.OrderType,
                           o.Address, o.payment_mode, o.specialInstructions,
                           CAST(o.UserId AS TEXT) AS UserId,
                           mi.item_name,
                           osm.Name   AS StatusName,
                           os.DiscountAmount,
                           s.FullName AS DeliveryName,
                           s.Phone    AS DeliveryPhone
                    FROM   Orders o
                    LEFT  JOIN menu_items        mi  ON mi.item_id  = o.item_id
                    LEFT  JOIN OrderStatusMaster osm ON osm.Id      = o.OrderStatus
                    LEFT  JOIN OrderSummary      os  ON os.OrderId  = o.OrderId
                    LEFT  JOIN Staff             s   ON s.StaffId   = o.DeliveryStaffId
                    WHERE  o.UserId = @UserId");
                cmd.Parameters.AddWithValue("@UserId", userId);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync()) list.Add(MapOrderWithDelivery(rdr));
            }
            catch (Exception ex) { Console.WriteLine("GetOrderHomeDelivery Error: " + ex.Message); }
            return list;
        }

        // ════════════════════════════════════════════════════════
        //  ORDERS — INSERT
        // ════════════════════════════════════════════════════════

        public async Task<bool> AddOrder(OrderModel order)
        {
            bool flag = false;
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            await using var tx = await con.BeginTransactionAsync() as SqliteTransaction;
            try
            {
                // Reuse existing active orderId for the same table
                var checkCmd = SQLiteHelper.Query(con,
                    "SELECT OrderId FROM Orders WHERE TableNo = @TableNo AND IsActive = 1 ORDER BY Id DESC LIMIT 1");
                checkCmd.Transaction = tx;
                checkCmd.Parameters.AddWithValue("@TableNo", order.selectedTable ?? 0);
                var existingOrderId = (await checkCmd.ExecuteScalarAsync())?.ToString();

                string orderId;
                if (existingOrderId != null)
                {
                    orderId = existingOrderId;
                }
                else
                {
                    var maxCmd = SQLiteHelper.Query(con, "SELECT IFNULL(MAX(Id), 0) FROM Orders");
                    maxCmd.Transaction = tx;
                    int maxId = Convert.ToInt32(await maxCmd.ExecuteScalarAsync());
                    orderId = $"ORD_{maxId + 1}_{order.selectedTable}_{order.userName}";
                }

                foreach (var item in order.orderItems)
                {
                    var ins = SQLiteHelper.Query(con, @"
                        INSERT INTO Orders
                            (OrderId, OrderStatus, item_id, FullPortion, HalfPortion, Price,
                             TableNo, CreatedBy, customerName, phone, OrderType, Address,
                             specialInstructions, IsActive)
                        VALUES
                            (@OrderId, 1, @item_id, @FullPortion, @HalfPortion, @Price,
                             @TableNo, @CreatedBy, @CustomerName, @phone, @OrderType, @Address,
                             @specialInstruction, 1)");
                    ins.Transaction = tx;
                    ins.Parameters.AddWithValue("@OrderId", orderId);
                    ins.Parameters.AddWithValue("@item_id", item.item_id);
                    ins.Parameters.AddWithValue("@FullPortion", item.full);
                    ins.Parameters.AddWithValue("@HalfPortion", item.half);
                    ins.Parameters.AddWithValue("@Price", item.Price);
                    ins.Parameters.AddWithValue("@TableNo", order.selectedTable ?? 0);
                    ins.Parameters.AddWithValue("@CreatedBy", order.userName);
                    ins.Parameters.AddWithValue("@CustomerName", (object?)order.customerName ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@phone", (object?)order.userPhone ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@OrderType", (object?)order.OrderType ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Address", (object?)order.Address ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@specialInstruction", (object?)order.specialInstruction ?? DBNull.Value);
                    await ins.ExecuteNonQueryAsync();
                }

                var idCmd = SQLiteHelper.Query(con, "SELECT last_insert_rowid();");
                idCmd.Transaction = tx;
                int newId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());

                await tx!.CommitAsync();
                flag = true;
                await LogSyncAsync(con, newId, "INSERT", IsSqlServerAvailable());
            }
            catch (Exception ex)
            {
                await tx!.RollbackAsync();
                Console.WriteLine("AddOrder Error: " + ex.Message);
            }
            return flag;
        }

        public async Task<bool> placeOnline(OrderModel order)
        {
            bool flag = false;
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            await using var tx = await con.BeginTransactionAsync() as SqliteTransaction;
            try
            {
                if (Convert.ToInt32(order.userName) <= 0)
                    return false;

                var maxCmd = SQLiteHelper.Query(con, "SELECT IFNULL(MAX(Id), 0) FROM Orders");
                maxCmd.Transaction = tx;
                int maxId = Convert.ToInt32(await maxCmd.ExecuteScalarAsync());
                string orderId = $"ORD_{maxId + 1}_{order.selectedTable ?? 0}_{order.userName}";

                foreach (var item in order.orderItems)
                {
                    var ins = SQLiteHelper.Query(con, @"
                        INSERT INTO Orders
                            (OrderId, OrderStatus, item_id, FullPortion, HalfPortion, Price,
                             TableNo, CreatedBy, customerName, phone, OrderType, Address,
                             specialInstructions, UserId)
                        VALUES
                            (@OrderId, 1, @item_id, @FullPortion, @HalfPortion, @Price,
                             @TableNo, @CreatedBy, @CustomerName, @phone, @OrderType, @Address,
                             @specialInstruction, @UserId)");
                    ins.Transaction = tx;
                    ins.Parameters.AddWithValue("@OrderId", orderId);
                    ins.Parameters.AddWithValue("@item_id", item.item_id);
                    ins.Parameters.AddWithValue("@FullPortion", item.full);
                    ins.Parameters.AddWithValue("@HalfPortion", item.half);
                    ins.Parameters.AddWithValue("@Price", item.Price);
                    ins.Parameters.AddWithValue("@TableNo", order.selectedTable ?? 0);
                    ins.Parameters.AddWithValue("@CreatedBy", order.userName);
                    ins.Parameters.AddWithValue("@CustomerName", (object?)order.customerName ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@phone", (object?)order.userPhone ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@OrderType", (object?)order.OrderType ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Address", (object?)order.Address ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@specialInstruction", (object?)order.specialInstruction ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@UserId", order.userId);
                    await ins.ExecuteNonQueryAsync();
                }

                await tx!.CommitAsync();
                flag = true;
            }
            catch (Exception ex)
            {
                await tx!.RollbackAsync();
                Console.WriteLine("placeOnline Error: " + ex.Message);
            }
            return flag;
        }

        // ════════════════════════════════════════════════════════
        //  ORDERS — STATUS UPDATE
        // ════════════════════════════════════════════════════════

        public async Task<bool> UpdateOrderStatus(OrderListModel order)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    UPDATE Orders SET
                        OrderStatus  = @StatusId,
                        payment_mode = @PaymentMode,
                        ModifiedDate = datetime('now'),
                        IsActive     = CASE WHEN @StatusId = 4 THEN 0 ELSE IsActive END
                    WHERE  OrderId      = @OrderId
                      AND  OrderStatus != 5");
                cmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                cmd.Parameters.AddWithValue("@StatusId", order.OrderStatusId);
                cmd.Parameters.AddWithValue("@PaymentMode", (object?)order.paymentMode ?? DBNull.Value);
                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows > 0 && order.OrderStatusId == 4)
                {
                    var staffCmd = SQLiteHelper.Query(con, @"
                        UPDATE Staff SET AvalfDelivery = 1
                        WHERE  StaffId = (SELECT DeliveryStaffId FROM Orders WHERE OrderId = @OrderId LIMIT 1)");
                    staffCmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                    await staffCmd.ExecuteNonQueryAsync();
                }
                else if (rows > 0)
                {
                    var staffCmd = SQLiteHelper.Query(con, @"
                        UPDATE Staff SET AvalfDelivery = 0
                        WHERE  StaffId = (SELECT DeliveryStaffId FROM Orders WHERE OrderId = @OrderId LIMIT 1)");
                    staffCmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                    await staffCmd.ExecuteNonQueryAsync();
                }

                return rows > 0;
            }
            catch (Exception ex) { Console.WriteLine("UpdateOrderStatus Error: " + ex.Message); return false; }
        }

        public async Task<bool> UpdateOnlineStatus(OnlineOrderModel order)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    UPDATE Orders SET
                        OrderStatus     = @StatusId,
                        payment_mode    = @PaymentMode,
                        DeliveryStaffId = CASE WHEN @DeliveryStaffId IS NOT NULL THEN @DeliveryStaffId ELSE DeliveryStaffId END,
                        ModifiedDate    = datetime('now'),
                        IsActive        = CASE WHEN @StatusId = 4 THEN 0 ELSE IsActive END
                    WHERE  OrderId      = @OrderId
                      AND  OrderStatus != 5");
                cmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                cmd.Parameters.AddWithValue("@StatusId", order.OrderStatus);
                cmd.Parameters.AddWithValue("@PaymentMode", string.IsNullOrEmpty(order.paymentMode) ? (object)DBNull.Value : order.paymentMode);
                cmd.Parameters.AddWithValue("@DeliveryStaffId", order.DeliveryStaffId.HasValue ? order.DeliveryStaffId.Value : (object)DBNull.Value);
                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    int avail = order.OrderStatus == 4 ? 1 : 0;
                    var staffCmd = SQLiteHelper.Query(con, @"
                        UPDATE Staff SET AvalfDelivery = @Avail
                        WHERE  StaffId = (SELECT DeliveryStaffId FROM Orders WHERE OrderId = @OrderId LIMIT 1)");
                    staffCmd.Parameters.AddWithValue("@Avail", avail);
                    staffCmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                    await staffCmd.ExecuteNonQueryAsync();
                }

                return rows > 0;
            }
            catch (Exception ex) { Console.WriteLine("UpdateOnlineStatus Error: " + ex.Message); return false; }
        }

        public async Task<bool> UpdateTableOrderStatus(OrderListModel order)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    UPDATE Orders SET
                        OrderStatus  = @StatusId,
                        payment_mode = @PaymentMode,
                        ModifiedDate = datetime('now'),
                        IsActive     = CASE WHEN @StatusId = 3 THEN 0 ELSE IsActive END
                    WHERE  OrderId      = @OrderId
                      AND  OrderStatus != 4");
                cmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                cmd.Parameters.AddWithValue("@StatusId", order.OrderStatusId);
                cmd.Parameters.AddWithValue("@PaymentMode", (object?)order.paymentMode ?? DBNull.Value);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("UpdateTableOrderStatus Error: " + ex.Message); return false; }
        }



        public async Task<bool> SoftDeleteOrder(int itemId)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "UPDATE Orders SET IsActive = 0, OrderStatus = 5 WHERE Id = @ItemId");
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("SoftDeleteOrder Error: " + ex.Message); return false; }
        }

        public async Task<bool> RejectOnlineOrder(string orderId)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "UPDATE Orders SET IsActive = 0, OrderStatus = 5 WHERE OrderId = @OrderId");
                cmd.Parameters.AddWithValue("@OrderId", orderId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("RejectOnlineOrder Error: " + ex.Message); return false; }
        }



        public async Task<List<OrderHistoryModel>> GetOrderHistory(string userName)
        {
            var list = new List<OrderHistoryModel>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT o.OrderId, o.customerName, o.phone, o.TableNo,
                           mi.item_name, o.FullPortion, o.HalfPortion, o.Price,
                           o.specialInstructions, o.payment_mode,
                           os.TotalAmount, os.DiscountAmount, os.FinalAmount,
                           o.CreatedDate
                    FROM   Orders o
                    LEFT  JOIN OrderSummary os ON os.OrderId = o.OrderId
                    LEFT  JOIN menu_items   mi ON mi.item_id = o.item_id
                    INNER JOIN Users         u ON u.Id       = o.CreatedBy
                    WHERE  u.Username = @UserName AND o.IsActive = 0
                    ORDER  BY o.CreatedDate DESC, o.OrderId DESC");
                cmd.Parameters.AddWithValue("@UserName", userName);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    list.Add(new OrderHistoryModel
                    {
                        OrderId = rdr.IsDBNull(0) ? "" : rdr.GetString(0),
                        CustomerName = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                        Phone = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        TableNo = rdr.IsDBNull(3) ? 0 : rdr.GetInt32(3),
                        ItemName = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                        FullPortion = rdr.IsDBNull(5) ? 0 : rdr.GetInt32(5),
                        HalfPortion = rdr.IsDBNull(6) ? 0 : rdr.GetInt32(6),
                        Price = rdr.IsDBNull(7) ? 0 : rdr.GetDecimal(7),
                        SpecialInstruction = rdr.IsDBNull(8) ? "" : rdr.GetString(8),
                        PaymentMode = rdr.IsDBNull(9) ? "" : rdr.GetString(9),
                        TotalAmount = rdr.IsDBNull(10) ? 0 : rdr.GetDecimal(10),
                        DiscountAmount = rdr.IsDBNull(11) ? 0 : rdr.GetDecimal(11),
                        FinalAmount = rdr.IsDBNull(12) ? 0 : rdr.GetDecimal(12),
                        Date = rdr.IsDBNull(13) ? DateTime.MinValue : DateTime.Parse(rdr.GetString(13)),
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetOrderHistory Error: " + ex.Message); }
            return list;
        }


        public async Task<bool> InsertOrderSummary(OrderSummaryModel summary)
            => await InsertSummaryInternal(summary);

        public async Task<bool> InsertOrderSummaryOnline(OrderSummaryModel summary)
            => await InsertSummaryInternal(summary);

        private async Task<bool> InsertSummaryInternal(OrderSummaryModel summary)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    INSERT INTO OrderSummary
                        (OrderId, CustomerName, Phone, TotalAmount, DiscountAmount, FinalAmount, PaymentMode)
                    VALUES
                        (@OrderId, @CustomerName, @Phone, @TotalAmount, @DiscountAmount, @FinalAmount, @PaymentMode)");
                cmd.Parameters.AddWithValue("@OrderId", summary.OrderId);
                cmd.Parameters.AddWithValue("@CustomerName", (object?)summary.CustomerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", (object?)summary.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TotalAmount", summary.TotalAmount);
                cmd.Parameters.AddWithValue("@DiscountAmount", summary.DiscountAmount);
                cmd.Parameters.AddWithValue("@FinalAmount", summary.FinalAmount);
                cmd.Parameters.AddWithValue("@PaymentMode", (object?)summary.PaymentMode ?? DBNull.Value);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("InsertOrderSummary Error: " + ex.Message); return false; }
        }

        public async Task<List<OrderBillModel>> GetBillByOrderId(string orderId)
        {
            var list = new List<OrderBillModel>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT s.SummaryId, s.OrderId, s.CustomerName, s.Phone,
                           s.TotalAmount, s.DiscountAmount, s.FinalAmount,
                           s.CreatedDate, s.CompletedDate,
                           o.item_id, mi.item_name, o.Price,
                           o.FullPortion, o.HalfPortion, o.TableNo,
                           o.OrderStatus, o.OrderType,
                           o.specialInstructions, o.payment_mode
                    FROM   OrderSummary s
                    INNER JOIN Orders     o  ON s.OrderId  = o.OrderId
                    LEFT  JOIN menu_items mi ON mi.item_id = o.item_id
                    WHERE  s.OrderId = @OrderId
                    ORDER  BY o.Id ASC");
                cmd.Parameters.AddWithValue("@OrderId", orderId);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    list.Add(new OrderBillModel
                    {
                        SummaryId = rdr.IsDBNull(0) ? 0 : rdr.GetInt32(0),
                        OrderId = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                        CustomerName = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        Phone = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                        TotalAmount = rdr.IsDBNull(4) ? 0 : rdr.GetDecimal(4),
                        DiscountAmount = rdr.IsDBNull(5) ? 0 : rdr.GetDecimal(5),
                        FinalAmount = rdr.IsDBNull(6) ? 0 : rdr.GetDecimal(6),
                        CreatedDate = rdr.IsDBNull(7) ? DateTime.MinValue : DateTime.Parse(rdr.GetString(7)),
                        CompletedDate = rdr.IsDBNull(8) ? DateTime.MinValue : DateTime.Parse(rdr.GetString(8)),
                        ItemId = rdr.IsDBNull(9) ? 0 : rdr.GetInt32(9),
                        ItemName = rdr.IsDBNull(10) ? "" : rdr.GetString(10),
                        Price = rdr.IsDBNull(11) ? 0 : rdr.GetDecimal(11),
                        FullPortion = rdr.IsDBNull(12) ? 0 : rdr.GetInt32(12),
                        HalfPortion = rdr.IsDBNull(13) ? 0 : rdr.GetInt32(13),
                        TableNo = rdr.IsDBNull(14) ? 0 : rdr.GetInt32(14),
                        PaymentMode = rdr.IsDBNull(18) ? "" : rdr.GetString(18),
                        SpecialInstructions = rdr.IsDBNull(17) ? "" : rdr.GetString(17),
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetBillByOrderId Error: " + ex.Message); }
            return list;
        }



        public async Task<List<MenuCategory>> GetMenuCategory(string userName)
        {
            var list = new List<MenuCategory>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT mc.category_id, mc.category_name, mc.description,
                           mc.CreatedDate, mc.CreatedBy, mc.ModifiedDate, mc.ModifiedBy, mc.IsActive
                    FROM   menu_categories mc
                    INNER JOIN Users u ON u.Id = mc.CreatedBy
                    WHERE  mc.IsActive = 1 OR (mc.IsActive = 0 AND u.Username = @UserName)");
                cmd.Parameters.AddWithValue("@UserName", userName);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    list.Add(new MenuCategory
                    {
                        CategoryId = rdr.GetInt32(0),
                        CategoryName = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                        Description = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        CreatedDate = rdr.IsDBNull(3) ? DateTime.MinValue : DateTime.Parse(rdr.GetString(3)),
                        CreatedBy = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                        ModifiedDate = rdr.IsDBNull(5) ? null : (DateTime?)DateTime.Parse(rdr.GetString(5)),
                        ModifiedBy = rdr.IsDBNull(6) ? "" : rdr.GetString(6),
                        IsActive = !rdr.IsDBNull(7) && rdr.GetInt32(7) == 1,
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetMenuCategory Error: " + ex.Message); }
            return list;
        }

        public async Task<List<MenuSubcategory>> GetMenuSubcategory(string userName)
        {
            var list = new List<MenuSubcategory>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT ms.subcategory_id, ms.category_id, ms.subcategory_name,
                           ms.description, ms.display_order, ms.IsActive
                    FROM   menu_subcategories ms
                    INNER JOIN Users u ON u.Id = ms.CreatedBy
                    WHERE  ms.IsActive = 1 OR (ms.IsActive = 0 AND u.Username = @UserName)");
                cmd.Parameters.AddWithValue("@UserName", userName);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    list.Add(new MenuSubcategory
                    {
                        SubcategoryId = rdr.IsDBNull(0) ? 0 : rdr.GetInt32(0),
                        CategoryId = rdr.IsDBNull(1) ? 0 : rdr.GetInt32(1),
                        SubcategoryName = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        Description = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                        DisplayOrder = rdr.IsDBNull(4) ? 0 : rdr.GetInt32(4),
                        IsActive = !rdr.IsDBNull(5) && rdr.GetInt32(5) == 1,
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetMenuSubcategory Error: " + ex.Message); }
            return list;
        }

        public async Task<List<MenuItem>> GetMenuItem(string userName)
        {
            var list = new List<MenuItem>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT mi.item_id, mi.subcategory_id, mi.item_name, mi.description,
                           mi.image_data, mi.price1, mi.price2, mi.count1, mi.count2, mi.title,
                           mi.CreatedDate, mi.CreatedBy, mi.ModifiedDate, mi.ModifiedBy, mi.IsActive
                    FROM   menu_items mi
                    INNER JOIN Users u ON u.Id = mi.CreatedBy
                    WHERE  mi.IsActive = 1 OR (mi.IsActive = 0 AND u.Username = @UserName)");
                cmd.Parameters.AddWithValue("@UserName", userName);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    list.Add(new MenuItem
                    {
                        ItemId = rdr.GetInt32(0),
                        SubcategoryId = rdr.GetInt32(1),
                        ItemName = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                        Description = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                        ImageSrc = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                        Price1 = rdr.IsDBNull(5) ? 0 : rdr.GetDecimal(5),
                        Price2 = rdr.IsDBNull(6) ? 0 : rdr.GetDecimal(6),
                        Count1 = rdr.IsDBNull(7) ? 0 : rdr.GetInt32(7),
                        Count2 = rdr.IsDBNull(8) ? 0 : rdr.GetInt32(8),
                        Title = rdr.IsDBNull(9) ? null : rdr.GetString(9),
                        CreatedDate = rdr.IsDBNull(10) ? DateTime.MinValue : DateTime.Parse(rdr.GetString(10)),
                        CreatedBy = rdr.IsDBNull(11) ? null : rdr.GetString(11),
                        ModifiedDate = rdr.IsDBNull(12) ? null : (DateTime?)DateTime.Parse(rdr.GetString(12)),
                        ModifiedBy = rdr.IsDBNull(13) ? null : rdr.GetString(13),
                        IsActive = !rdr.IsDBNull(14) && rdr.GetInt32(14) == 1,
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetMenuItem Error: " + ex.Message); }
            return list;
        }


        public async Task<List<CoffeeMenu>> GetCoffeeMenu(string userName)
        {
            var list = new List<CoffeeMenu>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT Id, CoffeeName, Description, Image, ImageUrl, price, CreatedDate, IsActive
                    FROM   HotCoffeeVarieties
                    WHERE  IsActive = 1");
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    list.Add(new CoffeeMenu
                    {
                        Id = rdr.GetInt32(0),
                        CoffeeName = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                        Description = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        Image = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                        ImageUrl = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                        Price = rdr.IsDBNull(5) ? 0 : rdr.GetDecimal(5),
                        CreatedDate = rdr.IsDBNull(6) ? DateTime.MinValue : DateTime.Parse(rdr.GetString(6)),
                        IsActive = !rdr.IsDBNull(7) && rdr.GetInt32(7) == 1,
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetCoffeeMenu Error: " + ex.Message); }
            return list;
        }

        public async Task<bool> CoffeeOrder(CoffeeOrder order)
        {
            bool flag = false;
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            await using var tx = await con.BeginTransactionAsync() as SqliteTransaction;
            try
            {
                string orderNumber = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                decimal totalAmount = order.orderItems.Sum(i => (decimal)i.Price * i.quantity);

                var cmdOrder = SQLiteHelper.Query(con, @"
                    INSERT INTO CoffeeOrders
                        (OrderNumber, CustomerName, FloorNo, RoomNo, TotalAmount, OrderStatus, CustomerPhone)
                    VALUES
                        (@OrderNumber, @CustomerName, @FloorNo, @RoomNo, @TotalAmount, 2, @CustomerPhone);
                    SELECT last_insert_rowid();");
                cmdOrder.Transaction = tx;
                cmdOrder.Parameters.AddWithValue("@OrderNumber", orderNumber);
                cmdOrder.Parameters.AddWithValue("@CustomerName", (object?)order.customerName ?? DBNull.Value);
                cmdOrder.Parameters.AddWithValue("@FloorNo", (object?)order.Floor ?? DBNull.Value);
                cmdOrder.Parameters.AddWithValue("@RoomNo", (object?)order.RoomNo ?? DBNull.Value);
                cmdOrder.Parameters.AddWithValue("@TotalAmount", totalAmount);
                cmdOrder.Parameters.AddWithValue("@CustomerPhone", (object?)order.customerPhone ?? DBNull.Value);
                int orderId = Convert.ToInt32(await cmdOrder.ExecuteScalarAsync());

                foreach (var item in order.orderItems)
                {
                    var cmdItem = SQLiteHelper.Query(con, @"
                        INSERT INTO OrderItems (OrderId, CoffeeId, Quantity, Price)
                        VALUES (@OrderId, @CoffeeId, @Quantity, @Price)");
                    cmdItem.Transaction = tx;
                    cmdItem.Parameters.AddWithValue("@OrderId", orderId);
                    cmdItem.Parameters.AddWithValue("@CoffeeId", item.item_id);
                    cmdItem.Parameters.AddWithValue("@Quantity", item.quantity);
                    cmdItem.Parameters.AddWithValue("@Price", item.Price);
                    await cmdItem.ExecuteNonQueryAsync();
                }

                await tx!.CommitAsync();
                flag = true;
            }
            catch (Exception ex)
            {
                await tx!.RollbackAsync();
                Console.WriteLine("CoffeeOrder Error: " + ex.Message);
            }
            return flag;
        }

        public async Task<List<GetOrderCoffeeDetails>> GetCoffeeOrdersDetails(string userName)
        {
            var list = new List<GetOrderCoffeeDetails>();
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT co.OrderId, co.OrderNumber, co.CustomerName, co.CustomerPhone,
                           co.CreatedDate AS OrderDate, osm.Name AS OrderStatus,
                           h.CoffeeName,  h.Description,
                           oi.Quantity,   oi.Price, oi.TotalPrice
                    FROM   CoffeeOrders       co
                    INNER JOIN OrderItems         oi  ON oi.OrderId  = co.OrderId
                    INNER JOIN HotCoffeeVarieties h   ON h.Id        = oi.CoffeeId
                    INNER JOIN OrderStatusMaster  osm ON osm.Id      = co.OrderStatus
                    WHERE  co.IsActive = 1
                    ORDER  BY co.CreatedDate DESC");
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    list.Add(new GetOrderCoffeeDetails
                    {
                        OrderId = rdr.IsDBNull(0) ? 0 : rdr.GetInt32(0),
                        OrderNumber = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                        CustomerName = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        CustomerPhone = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                        OrderDate = rdr.IsDBNull(4) ? DateTime.MinValue : DateTime.Parse(rdr.GetString(4)),
                        OrderStatus = rdr.IsDBNull(5) ? "" : rdr.GetString(5),
                        CoffeeName = rdr.IsDBNull(6) ? "" : rdr.GetString(6),
                        Description = rdr.IsDBNull(7) ? "" : rdr.GetString(7),
                        Quantity = rdr.IsDBNull(8) ? 0 : rdr.GetInt32(8),
                        Price = rdr.IsDBNull(9) ? 0 : rdr.GetDecimal(9),
                        TotalPrice = rdr.IsDBNull(10) ? 0 : rdr.GetDecimal(10),
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetCoffeeOrdersDetails Error: " + ex.Message); }
            return list;
        }

        public async Task<bool> UpdateCoffeeOrderStatus(updateCoffeeDetails order)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    UPDATE CoffeeOrders SET
                        OrderStatus  = @StatusId,
                        ModifiedDate = datetime('now'),
                        IsActive     = CASE WHEN @StatusId = 4 THEN 0 ELSE IsActive END
                    WHERE  OrderNumber = @OrderId");
                cmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                cmd.Parameters.AddWithValue("@StatusId", order.Status);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("UpdateCoffeeOrderStatus Error: " + ex.Message); return false; }
        }

        public async Task<bool> RejectCoffeeOrder(string orderId)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "UPDATE CoffeeOrders SET IsActive = 0, OrderStatus = 5 WHERE OrderNumber = @OrderId");
                cmd.Parameters.AddWithValue("@OrderId", orderId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("RejectCoffeeOrder Error: " + ex.Message); return false; }
        }



        public async Task<Tuple<bool, string, int>> IsAuthenticated(string username, string password)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "SELECT Id FROM Customers WHERE loginame = @Username AND Password = @Password LIMIT 1");
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    return Tuple.Create(true, string.Empty, Convert.ToInt32(result));
                return Tuple.Create(false, "", 0);
            }
            catch (Exception ex) { Console.WriteLine("IsAuthenticated Error: " + ex.Message); return Tuple.Create(false, "", 0); }
        }

        public async Task<bool> InsertToken(string username, string token, DateTime expiryDate)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    UPDATE Users SET
                        JwtToken           = @Token,
                        JwtTokenExpiryDate = @ExpiryDate,
                        ModifiedDate       = datetime('now')
                    WHERE  Username = @Username");
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate.ToString("yyyy-MM-dd HH:mm:ss"));
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("InsertToken Error: " + ex.Message); return false; }
        }

        public async Task<bool> ResetPasswordOnline(string phone, string newPassword)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "UPDATE Customers SET Password = @Password WHERE Phone = @Phone");
                cmd.Parameters.AddWithValue("@Phone", phone);
                cmd.Parameters.AddWithValue("@Password", newPassword);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("ResetPasswordOnline Error: " + ex.Message); return false; }
        }

        public async Task<bool> CheckPhoneExists(string phone)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "SELECT COUNT(1) FROM Customers WHERE Phone = @Phone");
                cmd.Parameters.AddWithValue("@Phone", phone);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex) { Console.WriteLine("CheckPhoneExists Error: " + ex.Message); return false; }
        }



        public async Task<CustomerAddressDto?> GetCustomerAddressOnline(string? userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return null;
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con, @"
                    SELECT Address, customerName
                    FROM   Orders
                    WHERE  UserId = @UserId
                      AND  Address IS NOT NULL
                      AND  Address != ''
                    ORDER  BY CreatedDate DESC LIMIT 1");
                cmd.Parameters.AddWithValue("@UserId", int.Parse(userId));
                await using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                    return new CustomerAddressDto
                    {
                        Address = rdr.IsDBNull(0) ? null : rdr.GetString(0),
                        CustomerName = rdr.IsDBNull(1) ? null : rdr.GetString(1),
                    };
                return null;
            }
            catch (Exception ex) { Console.WriteLine("GetCustomerAddressOnline Error: " + ex.Message); return null; }
        }


        public async Task<int> GetFixedDiscountAsync()
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "SELECT SettingValue FROM AppSettings WHERE SettingKey = 'FixedDiscount'");
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex) { Console.WriteLine("GetFixedDiscount Error: " + ex.Message); return 0; }
        }

        public async Task<bool> GetAvailability()
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "SELECT SettingValue FROM AppSettings WHERE SettingKey = 'IsOrderingAvailable'");
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString()?.ToLower() == "true" || result?.ToString() == "1";
            }
            catch (Exception ex) { Console.WriteLine("GetAvailability Error: " + ex.Message); return false; }
        }

        // NOTE: UpdateAvailability in SQL Server repo updates 'IsOrderingAvailableOnline' key — kept identical
        public async Task<bool> UpdateAvailability(bool isAvailable)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'IsOrderingAvailableOnline'");
                cmd.Parameters.AddWithValue("@value", isAvailable ? "true" : "false");
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("UpdateAvailability Error: " + ex.Message); return false; }
        }

        public async Task<bool> GetAvailabilityOnline()
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "SELECT SettingValue FROM AppSettings WHERE SettingKey = 'IsOrderingAvailableOnline'");
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString()?.ToLower() == "true" || result?.ToString() == "1";
            }
            catch (Exception ex) { Console.WriteLine("GetAvailabilityOnline Error: " + ex.Message); return false; }
        }

        // NOTE: UpdateAvailabilityOnline in SQL Server repo updates 'IsOrderingAvailable' key — kept identical
        public async Task<bool> UpdateAvailabilityOnline(bool isAvailable)
        {
            try
            {
                await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
                var cmd = SQLiteHelper.Query(con,
                    "UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'IsOrderingAvailable'");
                cmd.Parameters.AddWithValue("@value", isAvailable ? "true" : "false");
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Console.WriteLine("UpdateAvailabilityOnline Error: " + ex.Message); return false; }
        }



        public Task GetOrderOrderHistoryDash(string username)
            => throw new NotImplementedException();


        private static async Task LogSyncAsync(SqliteConnection con, int recordId, string action, bool isSynced = false)
        {
            var cmd = SQLiteHelper.Query(con, @"
                INSERT INTO SyncLog (TableName, RecordId, Action, IsSynced, CreatedAt)
                VALUES ('Orders', @RecordId, @Action, @IsSynced, datetime('now'))");
            cmd.Parameters.AddWithValue("@RecordId", recordId);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@IsSynced", isSynced ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }

        private bool IsSqlServerAvailable()
        {
            try { using var con = new SqlConnection(_sqlServerCs); con.Open(); return true; }
            catch { return false; }
        }
    }
}