using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class PartStockHistory
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }

        public long PartId { get; set; }
        public Part Part { get; set; }

        // +ve = added, -ve = removed
        public int QuantityChange { get; set; }

        // Quantity AFTER the change (snapshot)
        public int QuantityAfter { get; set; }

        // Why did it change
        [MaxLength(500)]
        public string Reason { get; set; }
        // Examples:
        // "Initial Stock"
        // "Used in TransactionBody #123"
        // "Manual Adjustment"
        // "Purchase"

        public bool IsActive { get; set; } = true;

        public string? UserId { get; set; }
        [ValidateNever]
        public AppUser? User { get; set; }

        public long? TransactionBodyId { get; set; }
        public TransactionBody? TransactionBody { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

}
