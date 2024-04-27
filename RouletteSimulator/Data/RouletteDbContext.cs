using Microsoft.EntityFrameworkCore;
using RouletteSimulator.Models;
using System.Collections.Generic;

namespace RouletteSimulator.Data
{
    public class RouletteDbContext : DbContext
    {
        public RouletteDbContext(DbContextOptions<RouletteDbContext> options)
            : base(options)
        {
        }

        public DbSet<Bet> Bets { get; set; }
        public DbSet<UserCoins> UserCoins { get; set; }
    }
}