using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace RepairShop.Models
{
    public class MaintenanceContract
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [ValidateNever]
        public string Status { get; set; }// active or expired
        public bool IsActive { get; set; } = true;

        public int ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } // belongs to client
    }
}
