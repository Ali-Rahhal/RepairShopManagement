using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RepairShop.Models.Helpers;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class TransactionBody
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }
        public long? PartId { get; set; }   // Nullable if sometimes no replacement part is used
        [ValidateNever]
        public Part Part { get; set; }
        [Required]
        public string BrokenPartName { get; set; }
        [Required]
        public string Status { get; set; } // Pending, Fixed, Replaced
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? WaitingPartDate { get; set; }
        public DateTime? FixedDate { get; set; }
        public DateTime? ReplacedDate { get; set; }
        public DateTime? NotRepairableDate { get; set; }
        public DateTime? NotReplaceableDate { get; set; }
        public bool IsActive { get; set; } = true;

        public long TransactionHeaderId { get; set; }
        [ValidateNever]
        public TransactionHeader TransactionHeader { get; set; } //belongs to a transaction header
    }
}
