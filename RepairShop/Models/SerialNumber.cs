using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class SerialNumber
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }
        public string Value { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public int ModelId { get; set; }
        [ValidateNever]
        public Model Model { get; set; } // belongs to Model

        public int ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } // owned by Client

        public int? WarrantyId { get; set; }
        [ValidateNever]
        public Warranty Warranty { get; set; } // covered by Warranty

        public int? MaintenanceContractId { get; set; }
        [ValidateNever]
        public MaintenanceContract MaintenanceContract { get; set; } // covered by MaintenanceContract
    }
}
