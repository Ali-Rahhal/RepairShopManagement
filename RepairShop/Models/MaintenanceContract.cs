using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class MaintenanceContract
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public long ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } // belongs to client
    }
}
