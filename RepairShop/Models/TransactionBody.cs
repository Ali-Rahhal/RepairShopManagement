using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class TransactionBody
    {
        public int Id { get; set; }

        [Required]
        public string PartName { get; set; }

        [Required]
        public string Status { get; set; } // Pending, Fixed, Replaced

        
        public int TransactionHeaderId { get; set; }
        public TransactionHeader TransactionHeader { get; set; }
    }
}
