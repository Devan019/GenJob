using GenJobMVC.Data.MyAuthMySQL.Data;
using GenJobMVC.Models;
using GenJobMVC.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Redis.OM;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file if it exists
DotNetEnv.Env.Load();

// Add services to the container.
builder.Services.AddControllersWithViews();

// MySQL connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Identity setup
builder.Services
    .AddDefaultIdentity<User>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 6;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";       // your custom controller
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Needed for Identity UIs

builder.Services.AddHttpClient();  //for make apis

//for redis
var redisConnectionString = builder.Configuration["REDIS_CONNECTION_STRING"];
builder.Services.AddSingleton(new RedisConnectionProvider(redisConnectionString));
builder.Services.AddHostedService<IndexCreationService>();
builder.Services.Configure<AI_API>(builder.Configuration.GetSection("AI_API"));
// Register ATS configuration
builder.Services.AddSingleton(ATSConfiguration.LoadFromEnvironment());

// Register ATS services
builder.Services.AddScoped<GenJobMVC.Services.IResumeParserService, GenJobMVC.Services.ResumeParserService>();
builder.Services.AddScoped<GenJobMVC.Services.IATSScoringService, GenJobMVC.Services.ATSScoringService>();
builder.Services.AddScoped<GenJobMVC.Services.IRealATSService, GenJobMVC.Services.RealATSService>();

builder.Services.AddHttpClient<GenJobMVC.Services.RealATSService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "GenJob-ATS/1.0");
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
app.UseAuthentication();  // MUST be before Authorization

app.UseAuthorization();

app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard/ats",
    defaults: new { controller = "ATS", action = "Index" });


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // Maps Identity pages

app.Run();
