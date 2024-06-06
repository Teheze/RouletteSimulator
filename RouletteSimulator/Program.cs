using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RouletteSimulator.Data;
using RouletteSimulator.Models;
using System;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("default");
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<RouletteDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<AppUser, IdentityRole>(
    options =>
    {
        options.Password.RequiredUniqueChars = 0;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<RouletteDbContext>().AddDefaultTokenProviders();


var app = builder.Build();


app.UseCors("AllowAll");


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



// Every new user gets 100 free points to play
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<RouletteDbContext>();
    context.Database.EnsureCreated();

    // Checks if table UserCoins is empty
    if (!context.UserCoins.Any())
    {
        var newUserCoins = new UserCoins { Coins = 100 };
        context.UserCoins.Add(newUserCoins);
        context.SaveChanges();
    }
}


app.Run();