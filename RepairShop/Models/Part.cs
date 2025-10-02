using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class Part
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        // optional categorization
        public string Category { get; set; }

        // inventory control
        public int Quantity { get; set; }

        // pricing if needed
        public decimal? Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
