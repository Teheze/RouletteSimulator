using System.ComponentModel.DataAnnotations;

namespace RouletteSimulator.Models
{
    public class Bet
    {
        [Key]
        public int BetId { get; set; }
        public int UserId { get; set; }
        public int Amount { get; set; }
        public string ColorBet { get; set; }
    }
}
