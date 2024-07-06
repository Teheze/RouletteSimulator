using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RouletteSimulator.Models;

namespace RouletteSimulator.Data
{
    public class RouletteDbContext : IdentityDbContext<AppUser>
    {
        public RouletteDbContext(DbContextOptions<RouletteDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserCoins> UserCoins { get; set; }
        public DbSet<Bet> Bets { get; set; }
        public DbSet<RouletteResult> RouletteResults { get; set; }
    }
}