var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Url"] ?? "https://localhost:7104");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register ShopExpense repository implementation based on configuration
var useSqlite = builder.Configuration.GetValue<bool>("UseSQLite", true);
if (useSqlite)
{
    builder.Services.AddScoped<OrderService.Repository.Interface.IShopExpenseRepository, OrderService.Repository.Service.ShopExpenseSQLiteRepository>();
    builder.Services.AddScoped<OrderService.Repository.Interface.IOrderRepository, OrderService.Repository.Service.OrderSQLiteRepository>();

}
else
{
    builder.Services.AddScoped<OrderService.Repository.Interface.IShopExpenseRepository, OrderService.Repository.Service.ShopExpenseRepository>();
    builder.Services.AddScoped<OrderService.Repository.Interface.IOrderRepository, OrderService.Repository.Service.OrderRepository>();

}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();     

app.UseSession();     

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); 