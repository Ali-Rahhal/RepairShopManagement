using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace RepairShop.Models
{
    public class SerialNumber
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public bool IsActive { get; set; } = true;

        public int ModelId { get; set; }
        [ValidateNever]
        public Model Model { get; set; } // belongs to Model

        public int ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } // owned by Client

        public int? MaintenanceContractId { get; set; }
        [ValidateNever]
        public MaintenanceContract MaintenanceContract { get; set; } // covered by MaintenanceContract
    }
}
