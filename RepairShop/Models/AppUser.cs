using Microsoft.AspNetCore.Identity;

namespace RepairShop.Models
{
    public class AppUser : IdentityUser
    {
        public string Role { get; set; }
        public ICollection<TransactionHeader> Transactions { get; set; }
    }
}
