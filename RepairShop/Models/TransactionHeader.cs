using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class TransactionHeader
    {
        public int Id { get; set; }

        [Required]
        public string PrinterModel { get; set; }
        [Required]
        public string SerialNumber { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        [Required]
        public int ClientId { get; set; }
        public Client Client { get; set; }

        [Required]
        public string AppUserId { get; set; }
        public AppUser User { get; set; }

        public ICollection<TransactionBody> Parts { get; set; }
    }
}
