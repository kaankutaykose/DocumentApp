using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DocumentApp.Data;
using DocumentApp.Models;
using GleamTech.AspNet.Core;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DocumentSystemConnection") ?? throw new InvalidOperationException("Connection string 'DocumentSystemConnection' not found.");

// Add services to the container.
builder.Services.AddControllersWithViews();


builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DocumentSystemConnection")));

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false; // Rakam zorunlulu�unu kald�r
    options.Password.RequireLowercase = false; // K���k harf zorunlulu�unu kald�r
    options.Password.RequireUppercase = false; // B�y�k harf zorunlulu�unu kald�r
    options.Password.RequireNonAlphanumeric = false; // Alfan�merik olmayan karakter zorunlulu�unu kald�r
    options.Password.RequiredLength = 6; // Minimum karakter say�s�n� belirle
});



builder.Services.AddGleamTech();

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
app.UseGleamTech();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{        
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();            
    var roles = new[] { "Admin", "User" };            
    foreach (var role in roles)            
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }                
}        

app.Run();
