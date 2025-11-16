using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RepairShop.Models
{
    public class TransactionHeader
    {
        public int Id { get; set; }

        [Required]
        public int DefectiveUnitId { get; set; }
        [ValidateNever]
        [DeleteBehavior(DeleteBehavior.Restrict)] // Add this to prevent cascade issues
        public DefectiveUnit DefectiveUnit { get; set; } //belongs to defective unit

        [Required]
        public string Status { get; set; } = SD.Status_Job_New;
        public DateTime? LastModifiedDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? InProgressDate { get; set; }
        public DateTime? CompletedOrOutOfServiceDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public Double? LaborFees { get; set; }
        public string? Comment { get; set; } //optional comment for the admin when changing status to Processed

        public bool IsActive { get; set; } = true;

        [Required]
        public string UserId { get; set; }
        [ValidateNever]
        public AppUser User { get; set; } //created by a user

        [ValidateNever]
        public ICollection<TransactionBody> BrokenParts { get; set; } //has many parts
    }
}
