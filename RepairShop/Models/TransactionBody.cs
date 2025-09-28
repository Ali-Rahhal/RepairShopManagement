using RepairShop.Models.Helpers;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class TransactionBody
    {
        public int Id { get; set; }
        [Required]
        public string PartName { get; set; }
        [Required]
        public string Status { get; set; } = SD.Status_Part_Pending; // Pending, Fixed, Replaced
        public bool IsActive { get; set; } = true;

        public int TransactionHeaderId { get; set; }
        public TransactionHeader TransactionHeader { get; set; } //belongs to a transaction header
    }
}
