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

        public long? ParentClientId { get; set; }
        public Client? ParentClient { get; set; }
        [ValidateNever]
        [JsonIgnore]
        public ICollection<Client> Branches { get; set; }

        [Required]
        public string Name { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string? Phone { get; set; }

        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        [ValidateNever]
        [JsonIgnore]
        public ICollection<SerialNumber> SerialNumbers { get; set; } //has many serial numbers

        [ValidateNever]
        [JsonIgnore]
        public ICollection<PreventiveMaintenanceRecord> PreventiveMaintenanceRecords { get; set; }
    }
}
