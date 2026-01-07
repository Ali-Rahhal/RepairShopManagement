using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class AuditLog
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }

        [Required]
        public string Action {  get; set; }

        [MaxLength(50)]
        [Required]
        public string EntityType { get; set; } = null!;// e.g. Client, SerialNumber
        [Required]
        public long EntityId { get; set; }

        [MaxLength(500)]
        [Required]
        public string Description { get; set; }

        public string? UserId { get; set; }
        [ValidateNever]
        public AppUser? User { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
