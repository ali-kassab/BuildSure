using BuildSure.Web.Data;
using BuildSure.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Register services with the Dependency Injection container ----------

// MVC: controllers + Razor views.
builder.Services.AddControllersWithViews();

// EF Core: one BuildSureDbContext per HTTP request (scoped), using SQL Server.
// The connection string lives in appsettings.json -> "ConnectionStrings:BuildSureDb".
builder.Services.AddDbContext<BuildSureDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BuildSureDb")));

// Our own business service. Scoped because it depends on the (scoped) DbContext.
builder.Services.AddScoped<IApplicationService, ApplicationService>();

// The system clock, injected so unit tests can control "now".
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// ---------- 2. Optional: apply migrations + seed sample data at startup ----------
// Enabled in appsettings.Development.json. In production you normally run
// "dotnet ef database update" or a generated SQL script instead.
if (app.Configuration.GetValue<bool>("Database:MigrateAndSeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BuildSureDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

// ---------- 3. Configure the HTTP request pipeline (middleware, in order) ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

// Conventional routing: /Applications/Details/5 -> ApplicationsController.Details(id: 5)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
