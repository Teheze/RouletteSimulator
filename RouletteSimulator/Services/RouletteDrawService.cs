using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RouletteSimulator.Controllers;
using RouletteSimulator.Data;
using RouletteSimulator.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RouletteSimulator.Services
{
    public class RouletteDrawService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RouletteDrawService> _logger;
        private static DateTime _nextDrawTime = DateTime.UtcNow.AddSeconds(30); 
        private static int _lastDrawResult;
        private static readonly Random _random = new Random();

        public RouletteDrawService(IServiceProvider serviceProvider, ILogger<RouletteDrawService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public static DateTime NextDrawTime => _nextDrawTime;
        public static int LastDrawResult => _lastDrawResult;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = _nextDrawTime - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                await Draw();
                _nextDrawTime = DateTime.UtcNow.AddSeconds(20);
                _logger.LogInformation($"Next draw at {_nextDrawTime}");
            }
        }

        private async Task Draw()
        {
            _lastDrawResult = _random.Next(0, 15);
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RouletteDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var rouletteResult = new RouletteResult
                {
                    Result = _lastDrawResult,
                    Date = DateTime.UtcNow
                };
                context.RouletteResults.Add(rouletteResult);
                await context.SaveChangesAsync();

                var userIds = await context.Bets
                    .Where(b => b.Date == NextDrawTime)
                    .Select(b => b.UserId)
                    .Distinct()
                    .ToListAsync();

                foreach (var userId in userIds)
                {
                    await new RouletteAPIController(context, userManager).HandlePayouts(userId); // Handle payouts for each user
                }
            }
            _logger.LogInformation($"Drawn number: {_lastDrawResult}");
        }
    }
}
