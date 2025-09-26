using Microsoft.AspNetCore.Identity;

namespace RepairShop.Models
{
    public class AppUser : IdentityUser
    {
        public string Role { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<TransactionHeader> Transactions { get; set; }

    }
}
