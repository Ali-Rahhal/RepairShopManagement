using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RepairShop.Models
{
    public class Model
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool IsActive { get; set; } = true;


        [ValidateNever]
        [JsonIgnore]
        public List<SerialNumber> SerialNumbers { get; set; } // has many serial numbers
    }
}
