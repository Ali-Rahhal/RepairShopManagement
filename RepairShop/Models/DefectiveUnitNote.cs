using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class DefectiveUnitNote
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }

        public long DefectiveUnitId { get; set; }

        [ValidateNever]
        public DefectiveUnit DefectiveUnit { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Note { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string UserId { get; set; }

        [ValidateNever]
        public AppUser User { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
