using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace RepairShop.Models
{
    public class AppUser : IdentityUser
    {
        public string? Role { get; set; }
        public bool IsActive { get; set; } = true;
        [ValidateNever]
        public ICollection<TransactionHeader> Transactions { get; set; } //creates many repair jobs

    }
}
