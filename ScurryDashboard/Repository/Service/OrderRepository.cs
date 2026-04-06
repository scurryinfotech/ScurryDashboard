using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderService.Model;
using OrderService.Repository.Interface;
using System.Data;
using System.Net;
using System.Transactions;

namespace OrderService.Repository.Service
{
    public class OrderRepository : IOrderRepository
    {
        private IConfiguration Configuration;
        private SqlConnection con;
        private string _connectionString;
        private object _configuration;

        //private IUserRepository _userRepository;
        public OrderRepository(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        public async Task<bool> SetTableCount(string userName, int count)
        {
            try
            {
                connection();

                using (SqlCommand cmd = new SqlCommand(@"UPDATE UserTableMaster SET TableCount = @Count WHERE UserId = (SELECT Id FROM Users WHERE Username = @UserName)", con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@Count", count);
                    cmd.Parameters.AddWithValue("@UserName", userName ?? (object)DBNull.Value);
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0)
                    {
                        using (SqlCommand ins = new SqlCommand(@"INSERT INTO UserTableMaster (UserId, TableCount) SELECT Id, @Count FROM Users WHERE Username = @UserName", con))
                        {
                            ins.CommandType = CommandType.Text;
                            ins.Parameters.AddWithValue("@Count", count);
                            ins.Parameters.AddWithValue("@UserName", userName ?? (object)DBNull.Value);
                            await ins.ExecuteNonQueryAsync();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SetTableCount Error: " + ex.Message);
                return false;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        private void connection()
        {
            string constr = this.Configuration.GetConnectionString("ConnStringDb");
            con = new SqlConnection(constr);
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
            }
        }

        // Changed to async to match IOrderRepository signature (Task<int>)
        public async Task<int> GetTableCount(string userName)
        {
            int tableCount = 0;
            try
            {
                connection();
                using (SqlCommand cmd = new SqlCommand("sp_GetTableCount", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        tableCount = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return tableCount;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return tableCount;
        }

        public async Task<List<OrderListModel>> GetOrder(string userName)
        {
            List<OrderListModel> orderList = new List<OrderListModel>();
            try
            {
                connection();
                using (SqlCommand cmd = new SqlCommand("sp_GetOrder", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        foreach (DataRow dr in dt.Rows)
                        {
                            var order = new OrderListModel
                            {
                                TableNo = dr["TableNo"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TableNo"]),
                                Id = dr["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Id"]),
                                OrderId = dr["OrderId"] == DBNull.Value ? string.Empty : dr["OrderId"].ToString(),
                                OrderStatusId = dr["OrderStatusId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["OrderStatusId"]),
                                ItemName = dr["ItemName"] == DBNull.Value ? string.Empty : dr["ItemName"].ToString(),
                                HalfPortion = dr["HalfPortion"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HalfPortion"]),
                                FullPortion = dr["FullPortion"] == DBNull.Value ? 0 : Convert.ToInt32(dr["FullPortion"]),
                                Price = dr["Price"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Price"]),
                                OrderStatus = dr["OrderStatus"] == DBNull.Value ? string.Empty : dr["OrderStatus"].ToString(),
                                Date = dr["Date"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["Date"]),
                                ModifiedDate = dr["ModifiedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["ModifiedDate"]),
                                customerName = dr["customerName"] == DBNull.Value ? string.Empty : dr["customerName"].ToString(),
                                phone = dr["phone"] == DBNull.Value ? string.Empty : dr["phone"].ToString(),
                                OrderType = dr["OrderType"] == DBNull.Value ? string.Empty : dr["OrderType"].ToString(),
                                Address = dr["Address"] == DBNull.Value ? string.Empty : dr["Address"].ToString(),
                                CreatedDate = dr["CreatedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["CreatedDate"]),
                                specialInstructions = dr["specialInstructions"] == DBNull.Value ? string.Empty : dr["specialInstructions"].ToString(),
                                paymentMode = dr.Table.Columns.Contains("payment_mode") && dr["payment_mode"] != DBNull.Value ? dr["payment_mode"].ToString() : string.Empty,
                                IsActive = dr["IsActive"] == DBNull.Value ? 0 : Convert.ToInt32(dr["IsActive"]),
                            };
                            orderList.Add(order);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return orderList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return orderList;
        }
        public async Task<List<MenuCategory>> GetMenuCategory(string UserName)
        {
            List<MenuCategory> categoryList = new List<MenuCategory>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_GetMenuCategory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        categoryList.Add(new MenuCategory
                        {
                            CategoryId = Convert.ToInt32(row["category_id"]),
                            CategoryName = row["category_name"].ToString(),
                            Description = row["description"].ToString(),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            CreatedBy = row["CreatedBy"].ToString(),
                            ModifiedDate = row["ModifiedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["ModifiedDate"]),
                            ModifiedBy = row["ModifiedBy"].ToString(),
                            IsActive = Convert.ToBoolean(row["IsActive"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return categoryList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return categoryList;
        }

        public async Task<List<MenuSubcategory>> GetMenuSubcategory(string UserName)
        {
            List<MenuSubcategory> subcategoryList = new List<MenuSubcategory>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_GetMenuSubcategory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Console.WriteLine("Row count from SP: " + dt.Rows.Count);


                if (dt.Rows.Count > 0)
                {

                    foreach (DataRow row in dt.Rows)
                    {
                        subcategoryList.Add(new MenuSubcategory
                        {
                            SubcategoryId = row["subcategory_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["subcategory_id"]),
                            CategoryId = row["category_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["category_id"]),
                            SubcategoryName = row["subcategory_name"] == DBNull.Value ? string.Empty : Convert.ToString(row["subcategory_name"]),
                            Description = row["description"] == DBNull.Value ? string.Empty : Convert.ToString(row["description"]),
                            DisplayOrder = row["display_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["display_order"]),
                            IsActive = row["IsActive"] == DBNull.Value ? false : Convert.ToBoolean(row["IsActive"])
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return subcategoryList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return subcategoryList;
        }

        public async Task<List<MenuItem>> GetMenuItem(string UserName)
        {
            List<MenuItem> itemList = new List<MenuItem>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_GetMenuItem", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        itemList.Add(new MenuItem
                        {
                            ItemId = Convert.ToInt32(row["item_id"]),
                            SubcategoryId = Convert.ToInt32(row["subcategory_id"]),

                            ItemName = row["item_name"]?.ToString(),

                            Description = row["description"] == DBNull.Value? null: row["description"].ToString(),

                            ImageSrc = row["image_data"] == DBNull.Value ? null: row["image_data"].ToString(),

                            //ImagePath = row["image_path"] == DBNull.Value ? null: row["image_path"].ToString(),

                            Price1 = row["price1"] == DBNull.Value? 0: Convert.ToDecimal(row["price1"]),

                            Price2 = row["price2"] == DBNull.Value? 0: Convert.ToDecimal(row["price2"]),

                            Count1 = row["count1"] == DBNull.Value? 0: Convert.ToInt32(row["count1"]),

                            Count2 = row["count2"] == DBNull.Value? 0: Convert.ToInt32(row["count2"]),

                            Title = row["title"] == DBNull.Value? null: row["title"].ToString(),

                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),

                            CreatedBy = row["CreatedBy"] == DBNull.Value? null : row["CreatedBy"].ToString(),

                            ModifiedDate = row["ModifiedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["ModifiedDate"]),

                            ModifiedBy = row["ModifiedBy"] == DBNull.Value ? null : row["ModifiedBy"].ToString(),

                            IsActive = Convert.ToBoolean(row["IsActive"])
                        });

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return itemList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return itemList;
        }

        public async Task<bool> AddOrder(OrderModel order)

        {
            bool flag = false;
            connection();
            try
            {
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    SqlCommand cmd = new SqlCommand("sp_InsertOrder", con, transaction);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TableNo", Convert.ToInt32(order.selectedTable));
                    cmd.Parameters.AddWithValue("@CreatedBy", Convert.ToInt32(order.userName));
                    cmd.Parameters.AddWithValue("@CustomerName", order.customerName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", order.userPhone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@orderType", order.OrderType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@address", order.Address ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@specialInstruction", order.specialInstruction ?? (object)DBNull.Value);


                    // Table-valued parameter
                    var orderItemsTable = new DataTable();
                    orderItemsTable.Columns.Add("item_id", typeof(int));
                    orderItemsTable.Columns.Add("FullPortion", typeof(int));
                    orderItemsTable.Columns.Add("HalfPortion", typeof(int));
                    orderItemsTable.Columns.Add("Price", typeof(double));

                    foreach (var item in order.orderItems)
                    {
                        int itemValue = item.item_id > 0 ? item.item_id : 0;
                        orderItemsTable.Rows.Add(itemValue, item.full, item.half, item.Price);
                    }

                    // Use SqlParameter with Structured type
                    var orderItemsParam = new SqlParameter("@OrderItems", SqlDbType.Structured)
                    {
                        TypeName = "dbo.OrderItemTableType",
                        Value = orderItemsTable
                    };
                    cmd.Parameters.Add(orderItemsParam);

                    // Add the output parameter
                    var insertedCountParam = new SqlParameter("@InsertedCount", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(insertedCountParam);

                    await cmd.ExecuteNonQueryAsync();

                    int insertedCount = insertedCountParam.Value != DBNull.Value ? (int)insertedCountParam.Value : 0;

                    if (insertedCount > 0)
                    {
                        transaction.Commit();
                        flag = true;
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return flag;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return flag;
        }

        public async Task<CustomerAddressDto?> GetCustomerAddressOnline(string? userId)
        {
            try
            {
                connection();

                using var cmd = new SqlCommand("sp_GetCustomerAddress", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@UserId", SqlDbType.Int)
                   .Value = string.IsNullOrEmpty(userId) ? (object)DBNull.Value : int.Parse(userId);

                if (con.State == ConnectionState.Closed)
                    con.Open();

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new CustomerAddressDto
                    {
                        CustomerName = reader["customerName"]?.ToString(),
                        Address = reader["Address"]?.ToString()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetCustomerAddressOnline: " + ex.Message);
                return null;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }


        public async Task<Tuple<bool, string, int>> IsAuthenticated(string username, string password)
        {
            bool isSuccess = false;
            string token = string.Empty;
            int Id = 0;

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_IsAuthenticated", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);

                SqlParameter userExistFlag = new SqlParameter("@userExistFlag", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                SqlParameter Token = new SqlParameter("@Token", SqlDbType.VarChar, 3000) { Direction = ParameterDirection.Output };
                SqlParameter IdParam = new SqlParameter("@Id", SqlDbType.Int) { Direction = ParameterDirection.Output };

                cmd.Parameters.Add(userExistFlag);
                cmd.Parameters.Add(Token);
                cmd.Parameters.Add(IdParam);

                await cmd.ExecuteNonQueryAsync();

                isSuccess = Convert.ToBoolean(userExistFlag.Value);
                token = Convert.ToString(Token.Value);
                Id = Convert.ToInt32(IdParam.Value);

                if (isSuccess)
                    return Tuple.Create(true, token, Id);
                else
                    return Tuple.Create(false, "", 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return Tuple.Create(false, "", 0);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        public async Task<bool> InsertToken(string username, string token, DateTime expiryDate)
        {
            bool flag = false;
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_InsertToken", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);
                int i = await cmd.ExecuteNonQueryAsync();
                if (i > 0)
                {
                    flag = true;
                }
            }
            catch (Exception ex)
            {

                return flag;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return flag;
        }

        public async Task<bool> SoftDeleteOrder(int itemId)
        {
            bool flag = false;

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_SoftDeleteOrder", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                int i = await cmd.ExecuteNonQueryAsync();
                if (i > 0)
                {
                    flag = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return flag;
        }

        public async Task<bool> UpdateOrderStatus(OrderListModel updatedOrders)
        {
            bool flag = false;
            try
            {
                connection();
                using var cmd = new SqlCommand("sp_UpdateOrderStatus", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 50).Value = updatedOrders.OrderId;
                cmd.Parameters.Add("@StatusId", SqlDbType.Int).Value = updatedOrders.OrderStatusId;
                cmd.Parameters.Add("@Payment_mode", SqlDbType.NVarChar, 50).Value = updatedOrders.paymentMode ?? (object)DBNull.Value;
                

                var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(rowsParam);


                _ = await cmd.ExecuteNonQueryAsync();

                var rows = rowsParam.Value is int n ? n : 0;
                flag = rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return flag;
        }

        public async Task<bool> UpdateTableOrderStatus(OrderListModel updatedTableOrders)
        {
            bool flag = false;
            try
            {
                connection();
                using var cmd = new SqlCommand("sp_UpdateTableOrderStatus", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 50).Value = updatedTableOrders.OrderId;
                cmd.Parameters.Add("@StatusId", SqlDbType.Int).Value = updatedTableOrders.OrderStatusId;
                cmd.Parameters.Add("@Payment_mode", SqlDbType.NVarChar, 50).Value = updatedTableOrders.paymentMode ?? (object)DBNull.Value;

                var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(rowsParam);


                _ = await cmd.ExecuteNonQueryAsync();

                var rows = rowsParam.Value is int n ? n : 0;
                flag = rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return flag;
        }

        public async Task<List<OrderHistoryModel>> GetOrderHistory(string userName)
        {
            List<OrderHistoryModel> historyList = new List<OrderHistoryModel>();

            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                    throw new ArgumentException("Username cannot be empty", nameof(userName));


                connection();

                using (SqlCommand cmd = new SqlCommand("sp_GetOrderHistory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", userName);

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            var item = new OrderHistoryModel
                            {
                                OrderId = dr["OrderId"] != DBNull.Value ? dr["OrderId"].ToString() : "",
                                CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : "",
                                Phone = dr["Phone"] != DBNull.Value ? dr["Phone"].ToString() : "",
                                TableNo = dr["TableNo"] != DBNull.Value ? Convert.ToInt32(dr["TableNo"]) : 0,
                                ItemName = dr["ItemName"] != DBNull.Value ? dr["ItemName"].ToString() : "",
                                FullPortion = dr["FullPortion"] != DBNull.Value ? Convert.ToInt32(dr["FullPortion"]) : 0,
                                HalfPortion = dr["HalfPortion"] != DBNull.Value ? Convert.ToInt32(dr["HalfPortion"]) : 0,
                                Price = dr["Price"] != DBNull.Value ? Convert.ToDecimal(dr["Price"]) : 0,
                                TotalAmount = dr["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(dr["TotalAmount"]) : 0,
                                DiscountAmount = dr["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(dr["DiscountAmount"]) : 0,
                                FinalAmount = dr["FinalAmount"] != DBNull.Value ? Convert.ToDecimal(dr["FinalAmount"]) : 0,
                                PaymentMode = dr["PaymentMode"] != DBNull.Value ? dr["PaymentMode"].ToString() : "",
                                Date = dr["Date"] != DBNull.Value ? Convert.ToDateTime(dr["Date"]) : DateTime.MinValue,
                                SpecialInstruction = dr["specialInstructions"] != DBNull.Value ? dr["specialInstructions"].ToString() : ""
                            };

                            historyList.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetOrderHistory: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return historyList;
        }

        public async Task<bool> InsertOrderSummary(OrderSummaryModel summary)
        {
            bool flag = false;

            try
            {
                connection();

                using var cmd = new SqlCommand("sp_InsertOrderSummary", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 100).Value = summary.OrderId;
                cmd.Parameters.Add("@CustomerName", SqlDbType.NVarChar, 100).Value = summary.CustomerName ?? (object)DBNull.Value;
                cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = summary.Phone ?? (object)DBNull.Value;
                cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal).Value = summary.TotalAmount;
                cmd.Parameters.Add("@DiscountAmount", SqlDbType.Decimal).Value = summary.DiscountAmount;
                cmd.Parameters.Add("@FinalAmount", SqlDbType.Decimal).Value = summary.FinalAmount;
                cmd.Parameters.Add("@PaymentMode", SqlDbType.NVarChar, 50).Value = summary.PaymentMode ?? (object)DBNull.Value;

                // OUTPUT PARAMETER
                var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(rowsParam);

                // EXECUTE
                _ = await cmd.ExecuteNonQueryAsync();

                var rows = rowsParam.Value is int n ? n : 0;
                flag = rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return flag;
        }

        public async Task<bool> InsertOrderSummaryOnline(OrderSummaryModel summary)
        {
            bool flag = false;

            try
            {
                connection();

                using var cmd = new SqlCommand("sp_InsertOrderSummary", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 100).Value = summary.OrderId;
                cmd.Parameters.Add("@CustomerName", SqlDbType.NVarChar, 100).Value = summary.CustomerName ?? (object)DBNull.Value;
                cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = summary.Phone ?? (object)DBNull.Value;
                cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal).Value = summary.TotalAmount;
                cmd.Parameters.Add("@DiscountAmount", SqlDbType.Decimal).Value = summary.DiscountAmount;
                cmd.Parameters.Add("@FinalAmount", SqlDbType.Decimal).Value = summary.FinalAmount;
                cmd.Parameters.Add("@PaymentMode", SqlDbType.NVarChar, 20).Value = summary.PaymentMode;

                // OUTPUT PARAMETER
                var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(rowsParam);

                // EXECUTE
                _ = await cmd.ExecuteNonQueryAsync();

                var rows = rowsParam.Value is int n ? n : 0;
                flag = rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return flag;
        }

        public async Task<List<OrderService.Model.OrderBillModel>> GetBillByOrderId(string orderId)
        {
            var list = new List<OrderService.Model.OrderBillModel>();

            try
            {
                connection();

                using var cmd = new SqlCommand("sp_GetBillByOrderId", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 100).Value = orderId;

                using var dr = await cmd.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    list.Add(new OrderService.Model.OrderBillModel
                    {
                        SummaryId = dr["SummaryId"] != DBNull.Value ? Convert.ToInt32(dr["SummaryId"]) : 0,
                        OrderId = dr["OrderId"]?.ToString() ?? "",
                        CustomerName = dr["CustomerName"]?.ToString() ?? "",
                        Phone = dr["Phone"]?.ToString() ?? "",

                        TotalAmount = dr["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(dr["TotalAmount"]) : 0,
                        DiscountAmount = dr["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(dr["DiscountAmount"]) : 0,
                        FinalAmount = dr["FinalAmount"] != DBNull.Value ? Convert.ToDecimal(dr["FinalAmount"]) : 0,

                        CreatedDate = dr["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDate"]) : DateTime.MinValue,
                        CompletedDate = dr["CompletedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CompletedDate"]) : DateTime.MinValue,

                        ItemId = dr["item_id"] != DBNull.Value ? Convert.ToInt32(dr["item_id"]) : 0,
                        ItemName = dr["ItemName"]?.ToString() ?? "",
                        Price = dr["Price"] != DBNull.Value ? Convert.ToDecimal(dr["Price"]) : 0,

                        FullPortion = dr["FullPortion"] != DBNull.Value ? Convert.ToInt32(dr["FullPortion"]) : 0,
                        HalfPortion = dr["HalfPortion"] != DBNull.Value ? Convert.ToInt32(dr["HalfPortion"]) : 0,
                        TableNo = dr["TableNo"] != DBNull.Value ? Convert.ToInt32(dr["TableNo"]) : 0,

                        PaymentMode = dr["payment_mode"]?.ToString() ?? "",
                        SpecialInstructions = dr["specialInstructions"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching bill: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return list;
        }
        public async Task<int> GetFixedDiscountAsync()
        {
            int discount = 0;
            try
            {
                connection();
                using var cmd = new SqlCommand("Sp_getFixedDiscount", con);
                cmd.CommandType = CommandType.StoredProcedure;

                if (con.State == ConnectionState.Closed)
                    await con.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                {
                    discount = Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching fixed discount: " + ex.Message);
                discount = 0;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return discount;
        }
        #region 
        public async Task<bool> GetAvailabilityOnline()
        {
            bool isAvailable = false;

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SELECT SettingValue FROM AppSettings WHERE SettingKey=@IsOrderingAvailableOnline", con);
                cmd.Parameters.Add("@IsOrderingAvailableOnline", SqlDbType.NVarChar, 100).Value = "IsOrderingAvailableOnline";
                cmd.CommandType = CommandType.Text;

                if (con.State == ConnectionState.Closed)
                    con.Open();

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    isAvailable = Convert.ToBoolean(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking availability: " + ex.Message);

            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return isAvailable;
        }
        public async Task<bool> UpdateAvailabilityOnline(bool isAvailable)
        {
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'IsOrderingAvailable'", con);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@value", isAvailable ? "true" : "false");

                if (con.State == ConnectionState.Closed)
                    con.Open();

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating availability: " + ex.Message);
                return false;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }
        public async Task<List<OrderListModel>> GetOrderHomeDelivery(int userId)
        {
            List<OrderListModel> orderList = new List<OrderListModel>();
            try
            {
                connection();
                using (SqlCommand cmd = new SqlCommand("sp_GetOrdersByUserId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        foreach (DataRow dr in dt.Rows)
                        {
                            var order = new OrderListModel
                            {
                                TableNo = dr["TableNo"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TableNo"]),
                                Id = dr["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Id"]),
                                OrderId = dr["OrderId"] == DBNull.Value ? string.Empty : dr["OrderId"].ToString(),
                                OrderStatusId = dr["OrderStatusId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["OrderStatusId"]),
                                ItemName = dr["ItemName"] == DBNull.Value ? string.Empty : dr["ItemName"].ToString(),
                                HalfPortion = dr["HalfPortion"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HalfPortion"]),
                                FullPortion = dr["FullPortion"] == DBNull.Value ? 0 : Convert.ToInt32(dr["FullPortion"]),
                                Price = dr["Price"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Price"]),
                                OrderStatus = dr["OrderStatus"] == DBNull.Value ? string.Empty : dr["OrderStatus"].ToString(),
                                Date = dr["Date"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["Date"]),
                                ModifiedDate = dr["ModifiedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["ModifiedDate"]),
                                customerName = dr["customerName"] == DBNull.Value ? string.Empty : dr["customerName"].ToString(),
                                phone = dr["phone"] == DBNull.Value ? string.Empty : dr["phone"].ToString(),
                                OrderType = dr["OrderType"] == DBNull.Value ? string.Empty : dr["OrderType"].ToString(),
                                Address = dr["Address"] == DBNull.Value ? string.Empty : dr["Address"].ToString(),
                                CreatedDate = dr["CreatedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["CreatedDate"]),
                                specialInstructions = dr["specialInstructions"] == DBNull.Value ? string.Empty : dr["specialInstructions"].ToString(),
                                userId = dr["userId"] == DBNull.Value ? string.Empty : dr["userId"].ToString(),
                                IsActive = dr["IsActive"] == DBNull.Value ? 0 : Convert.ToInt32(dr["IsActive"]),
                                Discount = dr["DiscountAmount"] == DBNull.Value ? string.Empty : dr["DiscountAmount"].ToString(),
                                DeliveryName = dr["DeliveryName"] == DBNull.Value ? string.Empty : dr["DeliveryName"].ToString(),
                                DeliveryPhone = dr["DeliveryPhone"] == DBNull.Value ? string.Empty : dr["DeliveryPhone"].ToString(),
                            };
                            orderList.Add(order);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return orderList;
                throw new NotImplementedException();
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return orderList;
        }
        public async Task<bool> placeOnline(OrderModel order)
        {
            bool flag = false;
            connection();
            try
            {
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    SqlCommand cmd = new SqlCommand("sp_InsertOrderOnline", con, transaction);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TableNo", order.selectedTable ?? 0);


                    cmd.Parameters.AddWithValue("@CreatedBy", Convert.ToInt32(order.userName));
                    cmd.Parameters.AddWithValue("@CustomerName", order.customerName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", order.userPhone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@orderType", order.OrderType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@address", order.Address ?? (object)DBNull.Value);
                    //cmd.Parameters.AddWithValue("@OrderType", order.OrderType ?? (object)DBNull.Value);
                    //cmd.Parameters.AddWithValue("@DeliveryType", order.DeliveryType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@specialInstruction", order.specialInstruction ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@userId", order.userId);


                    // Table-valued parameter
                    var orderItemsTable = new DataTable();
                    orderItemsTable.Columns.Add("item_id", typeof(int));
                    orderItemsTable.Columns.Add("FullPortion", typeof(int));
                    orderItemsTable.Columns.Add("HalfPortion", typeof(int));
                    orderItemsTable.Columns.Add("Price", typeof(double));

                    foreach (var item in order.orderItems)
                    {
                        int itemValue = item.item_id > 0 ? item.item_id : 0;
                        orderItemsTable.Rows.Add(itemValue, item.full, item.half, item.Price);
                    }

                    // Use SqlParameter with Structured type
                    var orderItemsParam = new SqlParameter("@OrderItems", SqlDbType.Structured)
                    {
                        TypeName = "dbo.OrderItemTableType",
                        Value = orderItemsTable
                    };
                    cmd.Parameters.Add(orderItemsParam);

                    // Add the output parameter
                    var insertedCountParam = new SqlParameter("@InsertedCount", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(insertedCountParam);

                    await cmd.ExecuteNonQueryAsync();

                    int insertedCount = insertedCountParam.Value != DBNull.Value ? (int)insertedCountParam.Value : 0;

                    if (insertedCount > 0)
                    {
                        transaction.Commit();
                        flag = true;
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return flag;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return flag;
        }

        //This is for the status update for the online orders
        public async Task<bool> UpdateOnlineStatus(OnlineOrderModel updatedOrders)
        {
            bool flag = false;

            try
            {
                connection();

                using (var cmd = new SqlCommand("sp_UpdateOrderStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 50)
                        .Value = updatedOrders.OrderId;

                    cmd.Parameters.Add("@StatusId", SqlDbType.Int)
                        .Value = updatedOrders.OrderStatus;

                    cmd.Parameters.Add("@Payment_mode", SqlDbType.NVarChar, 50)
                        .Value = string.IsNullOrEmpty(updatedOrders.paymentMode)
                                ? DBNull.Value
                                : updatedOrders.paymentMode;

                    cmd.Parameters.Add("@DeliveryStaffId", SqlDbType.Int)
                        .Value = updatedOrders.DeliveryStaffId.HasValue
                                ? updatedOrders.DeliveryStaffId.Value
                                : DBNull.Value;

                    var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(rowsParam);

                    await cmd.ExecuteNonQueryAsync();

                    int rows = (int)(rowsParam.Value ?? 0);
                    flag = rows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return flag;
        }
        public async Task<bool> RejectCoffeeOrder(string OrderId)
        {
            bool flag = false;

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_SoftDeleteCoffee", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OrderId", OrderId);
                int i = await cmd.ExecuteNonQueryAsync();
                if (i > 0)
                {
                    flag = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return flag;
        }
        public async Task<bool> RejectOnlineOrder(string OrderId)
        {
            bool flag = false;

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_SoftDeleteOnline", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OrderId", OrderId);
                int i = await cmd.ExecuteNonQueryAsync();
                if (i > 0)
                {
                    flag = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return flag;
        }
        #endregion

        #region
        public async Task<bool> GetAvailability()
        {
            bool isAvailable = false;

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SELECT SettingValue FROM AppSettings WHERE SettingKey = 'IsOrderingAvailable'", con);
                cmd.CommandType = CommandType.Text;

                if (con.State == ConnectionState.Closed)
                    con.Open();

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    string value = result.ToString().ToLower();
                    isAvailable = value == "true";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking availability: " + ex.Message);

            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return isAvailable;
        }
        public async Task<bool> UpdateAvailability(bool isAvailable)
        {
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE AppSettings SET SettingValue = @value WHERE SettingKey = 'IsOrderingAvailableOnline'", con);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@value", isAvailable ? "true" : "false");

                if (con.State == ConnectionState.Closed)
                    con.Open();

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating availability: " + ex.Message);
                return false;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }
        public async Task<List<CoffeeMenu>> GetCoffeeMenu(string UserName)
        {
            List<CoffeeMenu> itemList = new List<CoffeeMenu>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_GetCoffeeItem", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        itemList.Add(new CoffeeMenu
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            CoffeeName = row["CoffeeName"].ToString(),
                            Description = row["Description"].ToString(),
                            Image = row["Image"].ToString(),
                            ImageUrl = row["ImageUrl"] == DBNull.Value ? null : row["ImageUrl"].ToString(),
                            Price = row["Price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Price"]),
                            CreatedDate = (DateTime)(row["CreatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["CreatedDate"])),
                            IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"])

                        });

                    }
                }
                return itemList;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

                return itemList;

            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return itemList;
        }
        public async Task<bool> CoffeeOrder(CoffeeOrder order)
        {
            bool flag = false;
            connection();
            try
            {
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    // Step 1️⃣: Insert into CoffeeOrders
                    SqlCommand cmdOrder = new SqlCommand(@"
                INSERT INTO CoffeeOrders (OrderNumber, CustomerName, FloorNo, RoomNo, TotalAmount, OrderStatus, CustomerPhone)
                OUTPUT INSERTED.OrderId
                VALUES (@OrderNumber, @CustomerName, @FloorNo, @RoomNo, @TotalAmount, '2', @CustomerPhone);
            ", con, transaction);

                    // Generate unique order number like ORD20251015001
                    string orderNumber = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                    decimal totalAmount = order.orderItems.Sum(i => (decimal)i.Price * i.quantity);

                    cmdOrder.Parameters.AddWithValue("@OrderNumber", orderNumber);
                    cmdOrder.Parameters.AddWithValue("@CustomerName", order.customerName ?? (object)DBNull.Value);
                    cmdOrder.Parameters.AddWithValue("@FloorNo", order.Floor ?? (object)DBNull.Value);
                    cmdOrder.Parameters.AddWithValue("@RoomNo", order.RoomNo ?? (object)DBNull.Value);
                    cmdOrder.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    cmdOrder.Parameters.AddWithValue("@CustomerPhone", order.customerPhone ?? (object)DBNull.Value);

                    int orderId = Convert.ToInt32(await cmdOrder.ExecuteScalarAsync());

                    // Step 2️⃣: Insert multiple rows into OrderItems
                    foreach (var item in order.orderItems)
                    {
                        SqlCommand cmdItem = new SqlCommand(@"
                    INSERT INTO OrderItems (OrderId, CoffeeId, Quantity, Price)
                    VALUES (@OrderId, @CoffeeId, @Quantity, @Price);
                ", con, transaction);

                        cmdItem.Parameters.AddWithValue("@OrderId", orderId);
                        cmdItem.Parameters.AddWithValue("@CoffeeId", item.item_id);
                        cmdItem.Parameters.AddWithValue("@Quantity", item.quantity);
                        cmdItem.Parameters.AddWithValue("@Price", item.Price);

                        await cmdItem.ExecuteNonQueryAsync();
                    }

                    // ✅ Commit if all good
                    transaction.Commit();
                    flag = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                flag = false;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return flag;
        }

        public async Task<List<GetOrderCoffeeDetails>> GetCoffeeOrdersDetails(string UserName)
        {
            List<GetOrderCoffeeDetails> itemList = new List<GetOrderCoffeeDetails>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("GetCoffeeOrdersDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        itemList.Add(new GetOrderCoffeeDetails
                        {
                            OrderId = row["OrderId"] == DBNull.Value ? 0 : Convert.ToInt32(row["OrderId"]),
                            OrderNumber = row["OrderNumber"]?.ToString(),
                            CustomerName = row["CustomerName"]?.ToString(),
                            CustomerPhone = row["CustomerPhone"]?.ToString(),
                            OrderDate = (DateTime)(row["OrderDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["OrderDate"])),
                            OrderStatus = row["OrderStatus"]?.ToString(),
                            CoffeeName = row["CoffeeName"]?.ToString(),
                            Description = row["Description"]?.ToString(),
                            Quantity = row["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(row["Quantity"]),
                            Price = row["Price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Price"]),
                            TotalPrice = row["TotalPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(row["TotalPrice"])


                        });

                    }
                }
                return itemList;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

                return itemList;

            }

            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return itemList;
        }

        public async Task<bool> UpdateCoffeeOrderStatus(updateCoffeeDetails updatedOrders)
        {
            bool flag = false;
            try
            {
                connection();
                using var cmd = new SqlCommand("sp_UpdateCoffeeOrderStatus", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 50).Value = updatedOrders.OrderId;
                cmd.Parameters.Add("@StatusId", SqlDbType.Int).Value = updatedOrders.Status;


                var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(rowsParam);


                _ = await cmd.ExecuteNonQueryAsync();

                var rows = rowsParam.Value is int n ? n : 0;
                flag = rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return flag;
        }

        public Task GetOrderOrderHistoryDash(string username)
        {
            throw new NotImplementedException();
        }
        public async Task<bool> ResetPasswordOnline(string phone, string newPassword)
        {
            bool flag = false;

            try
            {
                connection();

                using var cmd = new SqlCommand("sp_ResetPassword", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = phone;
                cmd.Parameters.Add("@Password", SqlDbType.NVarChar, 200).Value = newPassword;

                var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(rowsParam);

                await cmd.ExecuteNonQueryAsync();

                int rows = rowsParam.Value is int n ? n : 0;
                flag = rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error ResetPasswordOnline: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return flag;
        }
        public async Task<bool> CheckPhoneExists(string phone)
        {
            try
            {
                connection();

                using var cmd = new SqlCommand("sp_CheckPhoneExists", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Phone", phone);

                var output = new SqlParameter("@Exists", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(output);

                await cmd.ExecuteNonQueryAsync();

                return Convert.ToBoolean(output.Value);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }


    }

    #endregion
}
