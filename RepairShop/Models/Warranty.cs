using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class Warranty
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [ValidateNever]
        public string Status { get; set; }//Active or Expired
        public bool IsActive { get; set; } = true;

        [ValidateNever]
        public ICollection<SerialNumber> SerialNumbers { get; set; } // covers serial numbers
    }
}
