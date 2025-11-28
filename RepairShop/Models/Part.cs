using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RepairShop.Models
{
    public class Part
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }
        [Required]
        public string Name { get; set; }

        // optional categorization
        public string Category { get; set; }

        // inventory control
        public int Quantity { get; set; }

        // pricing if needed
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
