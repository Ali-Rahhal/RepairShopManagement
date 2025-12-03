using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RepairShop.Models
{
    public class PreventiveMaintenanceRecord
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }

        [Required]
        [Display(Name = "Client")]
        public long ClientId { get; set; }

        [ValidateNever]
        public Client Client { get; set; }

        [Required]
        [Display(Name = "Department/Location")]
        [StringLength(200)]
        public string DepartmentLocation { get; set; }

        [Required]
        [Display(Name = "Serial Number")]
        public long SerialNumberId { get; set; }

        [ValidateNever]
        public SerialNumber SerialNumber { get; set; }

        [Display(Name = "IP Address")]
        [StringLength(50)]
        public string? IpAddress { get; set; }

        [Display(Name = "Date of Purchase")]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Problem")]
        [StringLength(500)]
        public string? Problem { get; set; }

        [Display(Name = "Solution")]
        [StringLength(500)]
        public string? Solution { get; set; }

        [Required]
        [Display(Name = "Date of Checkup")]
        public DateTime CheckupDate { get; set; }

        [Display(Name = "Comment")]
        [StringLength(1000)]
        public string? Comment { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Maintained By")]
        public string? UserId { get; set; }

        [ValidateNever]
        public AppUser? User { get; set; }
    }
}
