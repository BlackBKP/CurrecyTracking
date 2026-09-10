var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CurrecyTracking.Interfaces.ICurrencyMaster, CurrecyTracking.Services.CurrencyMasterService>();
builder.Services.AddHttpClient<CurrecyTracking.Interfaces.IFrankfurterService, CurrecyTracking.Services.FrankfurterService>(client =>
{
    client.BaseAddress = new Uri("https://api.frankfurter.dev/v1/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
