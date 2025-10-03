using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace RepairShop.Models
{
    public class AppUser : IdentityUser
    {
        public string? Role { get; set; }
        public bool IsActive { get; set; } = true;
        [ValidateNever]
        [JsonIgnore]
        public ICollection<TransactionHeader> Transactions { get; set; } //creates many repair jobs

    }
}
