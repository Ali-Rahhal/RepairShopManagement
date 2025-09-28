using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RepairShop.Models.Helpers;
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
        public string Status { get; set; } = SD.Status_Job_New;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        [Required]
        public int ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } //belongs to client

        [Required]
        public string UserId { get; set; }
        [ValidateNever]
        public AppUser User { get; set; } //created by a user

        [ValidateNever]
        public ICollection<TransactionBody> Parts { get; set; } //has many parts
    }
}
