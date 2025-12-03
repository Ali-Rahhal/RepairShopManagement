using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RepairShop.Models
{
    public class Client
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }

        [Required]
        public string Name { get; set; }
        public string? Branch { get; set; }
        [Required]
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [Required]
        public string Address { get; set; }
        public bool IsActive { get; set; } = true;

        [ValidateNever]
        [JsonIgnore]
        public ICollection<SerialNumber> SerialNumbers { get; set; } //has many serial numbers

        [ValidateNever]
        public ICollection<PreventiveMaintenanceRecord> PreventiveMaintenanceRecords { get; set; }
    }
}
