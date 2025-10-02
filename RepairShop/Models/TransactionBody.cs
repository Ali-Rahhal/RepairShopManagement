using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RepairShop.Models.Helpers;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class TransactionBody
    {
        public int Id { get; set; }
        public int? PartId { get; set; }   // Nullable if sometimes no replacement part is used
        [ValidateNever]
        public Part Part { get; set; }
        [Required]
        public string PartName { get; set; }
        [Required]
        public string Status { get; set; } = SD.Status_Part_Pending; // Pending, Fixed, Replaced
        public bool IsActive { get; set; } = true;

        public int TransactionHeaderId { get; set; }
        [ValidateNever]
        public TransactionHeader TransactionHeader { get; set; } //belongs to a transaction header
    }
}
