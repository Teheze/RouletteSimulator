using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using RouletteSimulator.Models;
using Microsoft.EntityFrameworkCore;
using RouletteSimulator.Data;

namespace RouletteSimulator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouletteAPIController : ControllerBase
    {
        private readonly RouletteDbContext _context;
        private readonly Random random = new Random();

        public RouletteAPIController(RouletteDbContext context)
        {
            _context = context;
        }

        [HttpGet("coins/{userId}")]
        public async Task<ActionResult<int>> GetUserCoins(int userId)
        {
            var userCoins = await _context.UserCoins.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userCoins == null)
                return NotFound("User not found.");

            return userCoins.Coins;
        }

        [HttpPost("bet")]
        public async Task<ActionResult> PlaceBet([FromBody] Bet bet)
        {
            var userCoins = await _context.UserCoins.FirstOrDefaultAsync(u => u.UserId == bet.UserId);
            if (userCoins == null)
                return BadRequest("User not found.");

            if (userCoins.Coins < bet.Amount)
                return BadRequest("Not enough coins.");

            int result = random.Next(0, 15);
            string resultColor = result == 0 ? "green" : (result <= 7 ? "red" : "black");

            if (resultColor == bet.ColorBet)
            {
                int multiplier = resultColor == "green" ? 14 : 2;
                userCoins.Coins += (bet.Amount * multiplier) - bet.Amount;
            }
            else
            {
                userCoins.Coins -= bet.Amount;
            }

            _context.Bets.Add(bet);
            await _context.SaveChangesAsync();

            return Ok(new { result = result, resultColor = resultColor, currentCoins = userCoins.Coins });
        }
    }
}