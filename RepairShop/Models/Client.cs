using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [Required]
        public string Address { get; set; }

        public ICollection<TransactionHeader> Transactions{ get; set; }

    }
}
