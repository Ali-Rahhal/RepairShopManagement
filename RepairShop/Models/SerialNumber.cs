using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Models
{
    public class SerialNumber
    {
        public long Id { get; set; }
        [MaxLength(50)]
        public string? Code { get; set; }
        public string Value { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public long ModelId { get; set; }
        [ValidateNever]
        public Model Model { get; set; } // belongs to Model

        public long ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } // owned by Client

        public long? WarrantyId { get; set; }
        [ValidateNever]
        public Warranty Warranty { get; set; } // covered by Warranty

        public long? MaintenanceContractId { get; set; }
        [ValidateNever]
        public MaintenanceContract MaintenanceContract { get; set; } // covered by MaintenanceContract
    }
}
