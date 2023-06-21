using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using ZlagodaPrj.Data;

var builder = WebApplication.CreateBuilder(args);

ConnectionManager.connectionString = builder.Configuration["ConnectionString"];
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleManager.ONLY_MANAGERS_POLICY, policy =>
                      policy.RequireClaim(ClaimTypes.Role, RoleManager.MANAGER_ROLE));
    
    options.AddPolicy(RoleManager.ONLY_CASHIERS_POLICY, policy =>
                      policy.RequireClaim(ClaimTypes.Role, RoleManager.CASHIER_ROLE));
    
    options.AddPolicy(RoleManager.CASHIERS_OR_MANAGERS_POLICY, policy =>
                      policy.RequireClaim(ClaimTypes.Role, RoleManager.CASHIER_ROLE, RoleManager.MANAGER_ROLE));
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

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
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
