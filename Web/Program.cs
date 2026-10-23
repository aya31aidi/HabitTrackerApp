using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
// The database runs on SQL Server 2014 (compatibility level 120), so EF Core must not
// generate OPENJSON queries, which only exist since SQL Server 2016
builder.Services.AddDbContext<WebContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("HabitTrackerContext") ?? throw new InvalidOperationException("Connection string 'HabitTrackerContext' not found."),
        sql => sql.UseCompatibilityLevel(120)));

// Every page requires a signed-in user unless the action is marked [AllowAnonymous]
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;

        // A cookie can outlive its account (deleted by the admin), so drop it in that case
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            if (principal == null || principal.IsAdmin())
                return;

            var db = context.HttpContext.RequestServices.GetRequiredService<WebContext>();
            var userId = principal.GetUserId();
            if (!await db.Users.AnyAsync(u => u.Id == userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<HabitStatsService>();

var app = builder.Build();

// Keep the database schema up to date with the latest migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days, worth adjusting for production (https://aka.ms/aspnetcore-hsts)
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
