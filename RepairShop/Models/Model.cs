using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace RepairShop.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;


        [ValidateNever]
        [JsonIgnore]
        public List<SerialNumber> SerialNumbers { get; set; } // has many serial numbers
    }
}
