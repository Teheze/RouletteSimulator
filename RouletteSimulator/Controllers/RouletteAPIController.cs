using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouletteSimulator.Data;
using RouletteSimulator.Models;
using RouletteSimulator.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RouletteSimulator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RouletteAPIController : ControllerBase
    {
        private readonly RouletteDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RouletteAPIController(RouletteDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("coins")]
        public async Task<ActionResult<int>> GetUserCoins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var userCoins = await _context.UserCoins.FirstOrDefaultAsync(u => u.UserId == user.Id);
            if (userCoins == null)
            {
                return NotFound("User coins entry not found.");
            }

            return userCoins.Coins;
        }

        [HttpPost("bet")]
        public async Task<IActionResult> PlaceBet([FromBody] Bet bet)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            bet.UserId = user.Id;

            if (bet.Amount <= 0)
            {
                return BadRequest("Invalid bet amount.");
            }

            var userCoins = await _context.UserCoins.FirstOrDefaultAsync(u => u.UserId == bet.UserId);
            if (userCoins == null)
            {
                return NotFound("User coins entry not found.");
            }

            if (userCoins.Coins < bet.Amount)
            {
                return BadRequest("Insufficient funds.");
            }

            if (RouletteDrawService.NextDrawTime - DateTime.UtcNow < TimeSpan.FromSeconds(5))
            {
                return BadRequest("Bets cannot be placed in the last 5 seconds before a draw.");
            }

            var currentRoundBets = await _context.Bets
            .Where(b => b.UserId == user.Id && b.Date == RouletteDrawService.NextDrawTime)
            .ToListAsync();

            if (currentRoundBets.Count >= 3)
            {
                return BadRequest("You can only place 3 bets per round.");
            }

            userCoins.Coins -= bet.Amount;
            bet.Date = RouletteDrawService.NextDrawTime;
            _context.Bets.Add(bet);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Result = RouletteDrawService.LastDrawResult,
                ResultColor = RouletteDrawService.LastDrawResult == 0 ? "Green" : RouletteDrawService.LastDrawResult <= 7 ? "Red" : "Black",
                CurrentCoins = userCoins.Coins
            });
        }

        public async Task HandlePayouts(string userId)
        {
            var userBets = await _context.Bets
                .Where(b => b.UserId == userId && b.Date == RouletteDrawService.NextDrawTime)
                .ToListAsync();

            var userCoins = await _context.UserCoins.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userCoins == null)
            {
                return;
            }

            foreach (var bet in userBets)
            {
                bool isWin = false;
                int payoutMultiplier = 0;

                if (bet.BetType == "red" && RouletteDrawService.LastDrawResult > 0 && RouletteDrawService.LastDrawResult <= 7)
                {
                    isWin = true;
                    payoutMultiplier = 2; // 2 payout
                }
                else if (bet.BetType == "black" && RouletteDrawService.LastDrawResult > 7)
                {
                    isWin = true;
                    payoutMultiplier = 2; // 2 payout
                }
                else if (bet.BetType == "green" && RouletteDrawService.LastDrawResult == 0)
                {
                    isWin = true;
                    payoutMultiplier = 14; // 14 payout
                }

                if (isWin)
                {
                    userCoins.Coins += bet.Amount * payoutMultiplier;
                }
            }

            await _context.SaveChangesAsync();
        }

        [HttpGet("last-10")]
        public async Task<IActionResult> GetLast10Results()
        {
            var last10Results = await _context.RouletteResults
                .OrderByDescending(r => r.Date)
                .Take(10)
                .Select(r => new { r.Result, ResultColor = r.Result == 0 ? "Green" : r.Result <= 7 ? "Red" : "Black", r.Date })
                .ToListAsync();

            return Ok(last10Results);
        }

        [HttpGet("bets")]
        public async Task<IActionResult> GetUserBets()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var currentRoundBets = await _context.Bets
                .Where(b => b.UserId == user.Id && b.Date == RouletteDrawService.NextDrawTime.Date)
                .ToListAsync();

            return Ok(currentRoundBets);
        }

        [HttpGet("next-draw")]
        public ActionResult<DateTime> GetNextDrawTime()
        {
            return RouletteDrawService.NextDrawTime;
        }

        [HttpGet("current-bets")]
        public async Task<IActionResult> GetCurrentRoundBets()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var currentRoundBets = await _context.Bets
                .Where(b => b.UserId == user.Id && b.Date == RouletteDrawService.NextDrawTime)
                .ToListAsync();

            var betSummary = currentRoundBets
                .GroupBy(b => b.BetType)
                .Select(g => new
                {
                    BetType = g.Key,
                    BetCount = g.Count(),
                    TotalAmount = g.Sum(b => b.Amount)
                })
                .ToList();

            return Ok(betSummary); 
        }
    }
}
