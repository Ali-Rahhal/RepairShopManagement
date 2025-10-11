using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models.Helpers;
using System.ComponentModel.DataAnnotations;

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

        public string? Description { get; set; }
        [Required]
        public string Status { get; set; } = SD.Status_Job_New;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        [Required]
        public int ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } //belongs to client

        [Required]
        public string UserId { get; set; }
        [ValidateNever]
        public AppUser User { get; set; } //created by a user

        [ValidateNever]
        public ICollection<TransactionBody> BrokenParts { get; set; } //has many parts
    }
}
