using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using OrderService.Helpers;

namespace OrderService.Services
{
    public class SyncService : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SyncService> _logger;

        public SyncService(IConfiguration config,
                           ILogger<SyncService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ─── MAIN LOOP — har 30 sec mein chalta hai ──────────
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ SyncService Started!");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (IsSqlServerAvailable())
                    {
                        _logger.LogInformation(
                            "🟢 SQL Server Online — Syncing...");
                        await SyncAllPendingRecords();
                    }
                    else
                    {
                        _logger.LogInformation(
                            "🔴 SQL Server Offline — Waiting...");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"❌ Sync Error: {ex.Message}");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        // ─── SAARE PENDING RECORDS SYNC KARO ─────────────────
        private async Task SyncAllPendingRecords()
        {
            var sqliteCs = _config
                .GetConnectionString("SQLiteConnection")!;
            var sqlCs = _config
                .GetConnectionString("ConnStringDb")!;

            await using var sqliteCon =
                await SQLiteHelper.OpenAsync(sqliteCs);

            var cmd = SQLiteHelper.Query(sqliteCon, @"
                SELECT Id, TableName, RecordId, Action
                FROM   SyncLog
                WHERE  IsSynced = 0
                ORDER  BY Id ASC");

            var pending = new List<(int LogId,
                string Table, int RecordId, string Action)>();

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                pending.Add((
                    rdr.GetInt32(0),
                    rdr.GetString(1),
                    rdr.GetInt32(2),
                    rdr.GetString(3)
                ));

            if (!pending.Any())
            {
                _logger.LogInformation("✅ No pending records.");
                return;
            }

            _logger.LogInformation(
                $"📤 Syncing {pending.Count} records...");

            await using var sqlCon = new SqlConnection(sqlCs);
            await sqlCon.OpenAsync();

            foreach (var (logId, table, recordId, action)
                     in pending)
            {
                try
                {
                    switch (table)
                    {
                        case "DailyExpenses":
                            await SyncDailyExpense(
                                sqliteCon, sqlCon,
                                recordId, action);
                            break;

                        case "ShopExpenses":
                            await SyncShopExpense(
                                sqliteCon, sqlCon,
                                recordId, action);
                            break;

                        case "Staff":
                            await SyncStaff(
                                sqliteCon, sqlCon,
                                recordId, action);
                            break;

                        case "Orders":
                            await SyncOrder(
                                sqliteCon, sqlCon,
                                recordId, action);
                            break;

                        case "OrderItems":
                            await SyncOrderItem(
                                sqliteCon, sqlCon,
                                recordId, action);
                            break;

                        case "OrderSummary":
                            await SyncOrderSummary(
                                sqliteCon, sqlCon,
                                recordId, action);
                            break;

                        default:
                            _logger.LogWarning(
                                $"⚠️ Unknown table: {table}");
                            break;
                    }

                    // ✅ Synced mark karo
                    var markCmd = SQLiteHelper.Query(
                        sqliteCon, @"
                        UPDATE SyncLog
                        SET    IsSynced = 1,
                               SyncedAt = datetime('now')
                        WHERE  Id = @Id");
                    markCmd.Parameters.AddWithValue("@Id", logId);
                    await markCmd.ExecuteNonQueryAsync();

                    _logger.LogInformation(
                        $"✅ {table} | {action} | Id:{recordId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"❌ {table} | {action} | " +
                        $"Id:{recordId} | {ex.Message}");
                }
            }

            _logger.LogInformation("🎉 All Synced!");
        }

        // ─── 1. DAILY EXPENSES ────────────────────────────────
        private async Task SyncDailyExpense(
            SqliteConnection sqlite, SqlConnection sql,
            int id, string action)
        {
            if (action == "DELETE")
            {
                var cmd = DbHelper.Proc(sql,
                    "sp_SoftDeleteDailyExpense");
                cmd.Parameters.AddWithValue(
                    "@DailyExpenseId", id);
                cmd.Parameters.AddWithValue(
                    "@ModifiedBy", "AutoSync");
                await cmd.ExecuteNonQueryAsync();
                return;
            }

            var getCmd = SQLiteHelper.Query(sqlite, @"
                SELECT Title, Category, Amount, ExpenseDate,
                       PaymentMode, PaidBy, Notes,
                       IsActive, ModifiedBy, CreatedBy
                FROM   DailyExpenses
                WHERE  DailyExpenseId = @Id");
            getCmd.Parameters.AddWithValue("@Id", id);

            await using var rdr =
                await getCmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return;

            var spName = action == "INSERT"
                ? "sp_InsertDailyExpense"
                : "sp_UpdateDailyExpense";

            var cmd2 = DbHelper.Proc(sql, spName);
            if (action == "UPDATE")
                cmd2.Parameters.AddWithValue(
                    "@DailyExpenseId", id);

            cmd2.Parameters.AddWithValue(
                "@Title", rdr.GetString(0));
            cmd2.Parameters.AddWithValue(
                "@Category", rdr.IsDBNull(1)
                    ? DBNull.Value : rdr.GetString(1));
            cmd2.Parameters.AddWithValue(
                "@Amount", rdr.GetDecimal(2));
            cmd2.Parameters.AddWithValue(
                "@ExpenseDate", DateTime.Parse(rdr.GetString(3)));
            cmd2.Parameters.AddWithValue(
                "@PaymentMode", rdr.GetString(4));
            cmd2.Parameters.AddWithValue(
                "@PaidBy", rdr.IsDBNull(5)
                    ? DBNull.Value : rdr.GetString(5));
            cmd2.Parameters.AddWithValue(
                "@Notes", rdr.IsDBNull(6)
                    ? DBNull.Value : rdr.GetString(6));
            cmd2.Parameters.AddWithValue(
                "@IsActive", rdr.GetInt32(7) == 1);
            cmd2.Parameters.AddWithValue(
                "@ModifiedBy", rdr.IsDBNull(8)
                    ? "AutoSync" : rdr.GetString(8));

            await cmd2.ExecuteNonQueryAsync();
        }

        // ─── 2. SHOP EXPENSES ─────────────────────────────────
        private async Task SyncShopExpense(
            SqliteConnection sqlite, SqlConnection sql,
            int id, string action)
        {
            if (action == "DELETE")
            {
                var cmd = DbHelper.Proc(sql,
                    "sp_UpsertShopExpense");
                cmd.Parameters.AddWithValue("@ExpenseId", id);
                cmd.Parameters.AddWithValue("@Title", "Deleted");
                cmd.Parameters.AddWithValue("@Amount", 0);
                cmd.Parameters.AddWithValue("@ExpenseDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@PaymentMode", "Cash");
                cmd.Parameters.AddWithValue("@IsActive", false);
                cmd.Parameters.AddWithValue("@ModifiedBy", "AutoSync");
                await cmd.ExecuteNonQueryAsync();
                return;
            }

            var getCmd = SQLiteHelper.Query(sqlite, @"
                SELECT Title, Category, Amount, ExpenseDate,
                       PaymentMode, Description,
                       IsActive, ModifiedBy, CreatedBy
                FROM   ShopExpenses
                WHERE  ExpenseId = @Id");
            getCmd.Parameters.AddWithValue("@Id", id);

            await using var rdr =
                await getCmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return;

            var cmd2 = DbHelper.Proc(sql, "sp_UpsertShopExpense");
            cmd2.Parameters.AddWithValue(
                "@ExpenseId", action == "UPDATE" ? id : 0);
            cmd2.Parameters.AddWithValue(
                "@Title", rdr.GetString(0));
            cmd2.Parameters.AddWithValue(
                "@Category", rdr.IsDBNull(1)
                    ? DBNull.Value : rdr.GetString(1));
            cmd2.Parameters.AddWithValue(
                "@Amount", rdr.GetDecimal(2));
            cmd2.Parameters.AddWithValue(
                "@ExpenseDate", DateTime.Parse(rdr.GetString(3)));
            cmd2.Parameters.AddWithValue(
                "@PaymentMode", rdr.GetString(4));
            cmd2.Parameters.AddWithValue(
                "@Description", rdr.IsDBNull(5)
                    ? DBNull.Value : rdr.GetString(5));
            cmd2.Parameters.AddWithValue(
                "@IsActive", rdr.GetInt32(6) == 1);
            cmd2.Parameters.AddWithValue(
                "@ModifiedBy", rdr.IsDBNull(7)
                    ? "AutoSync" : rdr.GetString(7));

            await cmd2.ExecuteNonQueryAsync();
        }

        // ─── 3. STAFF ─────────────────────────────────────────
        private async Task SyncStaff(
            SqliteConnection sqlite, SqlConnection sql,
            int id, string action)
        {
            if (action == "DELETE")
            {
                var cmd = DbHelper.Proc(sql, "sp_UpdateStaff");
                cmd.Parameters.AddWithValue("@StaffId", id);
                cmd.Parameters.AddWithValue("@IsActive", false);
                cmd.Parameters.AddWithValue("@ModifiedBy", "AutoSync");
                await cmd.ExecuteNonQueryAsync();
                return;
            }

            var getCmd = SQLiteHelper.Query(sqlite, @"
                SELECT FullName, RoleId, Phone, Email,
                       CNIC, Department, Salary, JoinDate,
                       IsActive, ModifiedBy, AvalfDelivery
                FROM   Staff
                WHERE  StaffId = @Id");
            getCmd.Parameters.AddWithValue("@Id", id);

            await using var rdr =
                await getCmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return;

            var spName = action == "INSERT"
                ? "sp_InsertStaff"
                : "sp_UpdateStaff";

            var cmd2 = DbHelper.Proc(sql, spName);
            if (action == "UPDATE")
                cmd2.Parameters.AddWithValue("@StaffId", id);

            cmd2.Parameters.AddWithValue(
                "@FullName", rdr.GetString(0));
            cmd2.Parameters.AddWithValue(
                "@RoleId", rdr.IsDBNull(1)
                    ? DBNull.Value : rdr.GetInt32(1));
            cmd2.Parameters.AddWithValue(
                "@Phone", rdr.IsDBNull(2)
                    ? DBNull.Value : rdr.GetString(2));
            cmd2.Parameters.AddWithValue(
                "@Email", rdr.IsDBNull(3)
                    ? DBNull.Value : rdr.GetString(3));
            cmd2.Parameters.AddWithValue(
                "@CNIC", rdr.IsDBNull(4)
                    ? DBNull.Value : rdr.GetString(4));
            cmd2.Parameters.AddWithValue(
                "@Department", rdr.IsDBNull(5)
                    ? DBNull.Value : rdr.GetString(5));
            cmd2.Parameters.AddWithValue(
                "@Salary", rdr.IsDBNull(6)
                    ? DBNull.Value : rdr.GetDecimal(6));
            cmd2.Parameters.AddWithValue(
                "@JoinDate", rdr.IsDBNull(7)
                    ? DBNull.Value
                    : DateTime.Parse(rdr.GetString(7)));
            cmd2.Parameters.AddWithValue(
                "@IsActive", rdr.GetInt32(8) == 1);
            cmd2.Parameters.AddWithValue(
                "@ModifiedBy", rdr.IsDBNull(9)
                    ? "AutoSync" : rdr.GetString(9));

            await cmd2.ExecuteNonQueryAsync();
        }

        // ─── 4. ORDERS ────────────────────────────────────────
        private async Task SyncOrder(
     SqliteConnection sqlite, SqlConnection sql,
     int id, string action)
        {
            try
            {
                if (action == "DELETE")
                {
                    try
                    {
                        var cmd = DbHelper.Proc(sql,
                            "sp_UpdateTableOrderStatus");
                        cmd.Parameters.AddWithValue(
                            "@OrderId", id.ToString());
                        cmd.Parameters.AddWithValue("@StatusId", 4);
                        cmd.Parameters.AddWithValue("@Payment_mode", "");
                        cmd.Parameters.AddWithValue("@RowsAffected", 0);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception(
                            $"[SyncOrder] DELETE failed for OrderId {id}. " +
                            $"SQL Error {ex.Number}: {ex.Message}", ex);
                    }
                    return;
                }

                SqliteDataReader rdr;
                try
                {
                    var getCmd = SQLiteHelper.Query(sqlite, @"
                SELECT OrderId, OrderStatus, FullPortion,
                       HalfPortion, TableNo, item_id,
                       Price, customerName, phone,
                       OrderType, Address, payment_mode,
                       DeliveryType, specialInstructions,
                       UserId, DeliveryStaffId, CreatedBy
                FROM   Orders
                WHERE  Id = @Id");
                    getCmd.Parameters.AddWithValue("@Id", id);

                    rdr = await getCmd.ExecuteReaderAsync();
                }
                catch (SqliteException ex)
                {
                    throw new Exception(
                        $"[SyncOrder] Failed to read Order Id {id} from SQLite. " +
                        $"SQLite Error {ex.SqliteErrorCode}: {ex.Message}", ex);
                }

                await using (rdr)
                {
                    if (!await rdr.ReadAsync())
                    {
                        // No matching record found — nothing to sync
                        return;
                    }

                    if (action == "INSERT")
                    {
                        try
                        {
                            var cmd2 = DbHelper.Proc(sql, "sp_PlaceOrder");
                            cmd2.Parameters.AddWithValue(
                                "@OrderId", rdr.IsDBNull(0)
                                    ? DBNull.Value : rdr.GetString(0));
                            cmd2.Parameters.AddWithValue(
                                "@OrderStatus", rdr.GetInt32(1));
                            cmd2.Parameters.AddWithValue(
                                "@FullPortion", rdr.IsDBNull(2)
                                    ? DBNull.Value : rdr.GetInt32(2));
                            cmd2.Parameters.AddWithValue(
                                "@HalfPortion", rdr.IsDBNull(3)
                                    ? DBNull.Value : rdr.GetInt32(3));
                            cmd2.Parameters.AddWithValue(
                                "@TableNo", rdr.IsDBNull(4)
                                    ? DBNull.Value : rdr.GetInt32(4));
                            cmd2.Parameters.AddWithValue(
                                "@item_id", rdr.IsDBNull(5)
                                    ? DBNull.Value : rdr.GetInt32(5));
                            cmd2.Parameters.AddWithValue(
                                "@Price", rdr.IsDBNull(6)
                                    ? DBNull.Value : rdr.GetDecimal(6));
                            cmd2.Parameters.AddWithValue(
                                "@customerName", rdr.IsDBNull(7)
                                    ? DBNull.Value : rdr.GetString(7));
                            cmd2.Parameters.AddWithValue(
                                "@phone", rdr.IsDBNull(8)
                                    ? DBNull.Value : rdr.GetString(8));
                            cmd2.Parameters.AddWithValue(
                                "@OrderType", rdr.IsDBNull(9)
                                    ? DBNull.Value : rdr.GetString(9));
                            cmd2.Parameters.AddWithValue(
                                "@Address", rdr.IsDBNull(10)
                                    ? DBNull.Value : rdr.GetString(10));
                            cmd2.Parameters.AddWithValue(
                                "@payment_mode", rdr.IsDBNull(11)
                                    ? DBNull.Value : rdr.GetString(11));
                            cmd2.Parameters.AddWithValue(
                                "@DeliveryType", rdr.IsDBNull(12)
                                    ? DBNull.Value : rdr.GetString(12));
                            cmd2.Parameters.AddWithValue(
                                "@specialInstructions", rdr.IsDBNull(13)
                                    ? DBNull.Value : rdr.GetString(13));
                            cmd2.Parameters.AddWithValue(
                                "@UserId", rdr.IsDBNull(14)
                                    ? DBNull.Value : rdr.GetInt32(14));
                            cmd2.Parameters.AddWithValue(
                                "@DeliveryStaffId", rdr.IsDBNull(15)
                                    ? DBNull.Value : rdr.GetInt32(15));
                            cmd2.Parameters.AddWithValue(
                                "@CreatedBy", rdr.IsDBNull(16)
                                    ? DBNull.Value : rdr.GetInt32(16));

                            await cmd2.ExecuteNonQueryAsync();
                        }
                        catch (SqlException ex)
                        {
                            throw new Exception(
                                $"[SyncOrder] INSERT failed for OrderId {id} " +
                                $"via sp_PlaceOrder. SQL Error {ex.Number}: {ex.Message}", ex);
                        }
                    }
                    else if (action == "UPDATE")
                    {
                        try
                        {
                            var cmd2 = DbHelper.Proc(sql,
                                "sp_UpdateTableOrderStatus");
                            cmd2.Parameters.AddWithValue(
                                "@OrderId", rdr.IsDBNull(0)
                                    ? id.ToString() : rdr.GetString(0));
                            cmd2.Parameters.AddWithValue(
                                "@StatusId", rdr.GetInt32(1));
                            cmd2.Parameters.AddWithValue(
                                "@Payment_mode", rdr.IsDBNull(11)
                                    ? "" : rdr.GetString(11));
                            cmd2.Parameters.AddWithValue(
                                "@RowsAffected", 0);

                            await cmd2.ExecuteNonQueryAsync();
                        }
                        catch (SqlException ex)
                        {
                            throw new Exception(
                                $"[SyncOrder] UPDATE failed for OrderId {id} " +
                                $"via sp_UpdateTableOrderStatus. SQL Error {ex.Number}: {ex.Message}", ex);
                        }
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"[SyncOrder] Unknown action '{action}' for OrderId {id}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                // Log the full exception here (replace with your logger)
                Console.WriteLine(
                    $"[SyncOrder] Unhandled error | Action: {action} | OrderId: {id} | " +
                    $"Error: {ex.Message}");

                // Re-throw to let the caller decide whether to retry / alert
                throw;
            }
        }

        // ─── 5. ORDER ITEMS ───────────────────────────────────
        private async Task SyncOrderItem(
            SqliteConnection sqlite, SqlConnection sql,
            int id, string action)
        {
            if (action == "DELETE") return;

            var getCmd = SQLiteHelper.Query(sqlite, @"
                SELECT OrderId, CoffeeId, Quantity,
                       Price, TotalPrice, CreatedBy
                FROM   OrderItems
                WHERE  Id = @Id");
            getCmd.Parameters.AddWithValue("@Id", id);

            await using var rdr =
                await getCmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return;

            var cmd2 = DbHelper.Proc(sql, "sp_AddOrderItem");
            cmd2.Parameters.AddWithValue(
                "@OrderId", rdr.IsDBNull(0)
                    ? DBNull.Value : rdr.GetInt32(0));
            cmd2.Parameters.AddWithValue(
                "@CoffeeId", rdr.IsDBNull(1)
                    ? DBNull.Value : rdr.GetInt32(1));
            cmd2.Parameters.AddWithValue(
                "@Quantity", rdr.GetInt32(2));
            cmd2.Parameters.AddWithValue(
                "@Price", rdr.IsDBNull(3)
                    ? DBNull.Value : rdr.GetDecimal(3));
            cmd2.Parameters.AddWithValue(
                "@CreatedBy", rdr.IsDBNull(5)
                    ? DBNull.Value : rdr.GetInt32(5));

            await cmd2.ExecuteNonQueryAsync();
        }

        private async Task SyncOrderSummary(
            SqliteConnection sqlite, SqlConnection sql,
            int id, string action)
        {
            if (action == "DELETE") return;

            var getCmd = SQLiteHelper.Query(sqlite, @"
                SELECT OrderId, CustomerName, Phone,
                       TotalAmount, DiscountAmount,
                       FinalAmount, PaymentMode,
                       specialInstruction
                FROM   OrderSummary
                WHERE  SummaryId = @Id");
            getCmd.Parameters.AddWithValue("@Id", id);

            await using var rdr =
                await getCmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return;

            var cmd2 = DbHelper.Proc(sql, "sp_SaveOrderSummary");
            cmd2.Parameters.AddWithValue(
                "@OrderId", rdr.GetString(0));
            cmd2.Parameters.AddWithValue(
                "@CustomerName", rdr.IsDBNull(1)
                    ? DBNull.Value : rdr.GetString(1));
            cmd2.Parameters.AddWithValue(
                "@Phone", rdr.IsDBNull(2)
                    ? DBNull.Value : rdr.GetString(2));
            cmd2.Parameters.AddWithValue(
                "@TotalAmount", rdr.GetDecimal(3));
            cmd2.Parameters.AddWithValue(
                "@DiscountAmount", rdr.GetDecimal(4));
            cmd2.Parameters.AddWithValue(
                "@FinalAmount", rdr.GetDecimal(5));
            cmd2.Parameters.AddWithValue(
                "@PaymentMode", rdr.IsDBNull(6)
                    ? DBNull.Value : rdr.GetString(6));
            cmd2.Parameters.AddWithValue(
                "@specialInstruction", rdr.IsDBNull(7)
                    ? DBNull.Value : rdr.GetString(7));

            await cmd2.ExecuteNonQueryAsync();
        }


        private bool IsSqlServerAvailable()
        {
            try
            {
                using var con = new SqlConnection(
                    _config.GetConnectionString("ConnStringDb"));
                con.Open();
                return true;
            }
            catch { return false; }
        }
    }
}