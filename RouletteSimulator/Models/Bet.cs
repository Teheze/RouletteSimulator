using System.ComponentModel.DataAnnotations;

namespace RouletteSimulator.Models
{
    public class Bet
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        [Required]
        public string BetType { get; set; }
        [Required]
        public int Amount { get; set; }
        [Required]
        public DateTime Date { get; set; }
    }

}