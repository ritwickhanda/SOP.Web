using Microsoft.EntityFrameworkCore;
using SOP.Web.Data;
using SOP.Web.Services;
using SOP.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting.WindowsServices;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Encrypted connection string read aur decrypt karna
// Load encryption key first
EncryptionHelper.CurrentEncryptionKey = builder.Configuration["Encryption:Key"];
bool isSetupComplete = builder.Configuration.GetValue<bool>("Encryption:IsSetupComplete");

// Check if setup is needed (key is default or setup not complete)
if (string.IsNullOrEmpty(EncryptionHelper.CurrentEncryptionKey) || EncryptionHelper.CurrentEncryptionKey == "Apki_Ek_Bahut_Strong_Secret_Key_123!" || !isSetupComplete)
{
    // If setup is needed, we need to bypass normal DbContext initialization
    // and redirect to the setup page.
    // We'll add a temporary DbContext for SopDbContext to avoid null reference,
    // but it won't be used until setup is complete.
    builder.Services.AddDbContext<SopDbContext>(options =>
        options.UseSqlServer("Server=setup;Database=setup;User Id=setup;Password=setup;TrustServerCertificate=True;"));
    builder.Services.AddDbContext<ViewscapeDbContext>(options =>
        options.UseSqlServer("Server=setup;Database=setup;User Id=setup;Password=setup;TrustServerCertificate=True;"));

    // Add a flag to indicate setup mode
    builder.Services.AddSingleton(new SetupModeFlag { IsSetupMode = true });
}
else
{
    // Normal operation: decrypt and configure DbContexts
    var sopConn = EncryptionHelper.Decrypt(builder.Configuration.GetConnectionString("SOP") ?? string.Empty);
builder.Services.AddDbContext<SopDbContext>(options =>
        options.UseSqlServer(sopConn));

    var vcConn = EncryptionHelper.Decrypt(builder.Configuration.GetConnectionString("VC") ?? string.Empty);
    builder.Services.AddDbContext<ViewscapeDbContext>(options => options.UseSqlServer(vcConn ?? string.Empty));

    // Add a flag to indicate normal mode
    builder.Services.AddSingleton(new SetupModeFlag { IsSetupMode = false });
}

// Add services regardless of setup mode
builder.Services.AddScoped<WorkflowBuilderService>();
builder.Services.AddScoped<WorkflowExecutionService>();
builder.Services.AddScoped<QuestionManagementService>();
builder.Services.AddScoped<AnswerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Setup redirection if needed
var setupFlag = app.Services.GetRequiredService<SetupModeFlag>();
if (setupFlag.IsSetupMode)
{
    app.MapGet("/", ([FromServices] IConfiguration config) => Results.Redirect("/setup"));
    app.MapGet("/Home", ([FromServices] IConfiguration config) => Results.Redirect("/setup"));
    app.MapGet("/Home/Index", ([FromServices] IConfiguration config) => Results.Redirect("/setup"));
}

app.Run();

// Helper class to pass setup mode flag
public class SetupModeFlag
{
    public bool IsSetupMode { get; set; }
}
