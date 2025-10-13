using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RepairShop.Models.Helpers;

namespace RepairShop.Models
{
    public class DefectiveUnit
    {
        public int Id { get; set; }
        public DateTime ReportedDate { get; set; } = DateTime.Now;
        public string Description { get; set; } // e.g. "Screen flickering", "Power failure"
        public string Status { get; set; } = SD.Status_DU_Reported; // e.g. Reported, UnderRepair, Fixed, Out of Service
        public bool IsActive { get; set; } = true;

        // Relationships
        public int SerialNumberId { get; set; }
        [ValidateNever]
        public SerialNumber SerialNumber { get; set; } // which device failed

        public int? WarrantyId { get; set; }
        [ValidateNever]
        public Warranty Warranty { get; set; } // if covered by warranty

        public int? MaintenanceContractId { get; set; }
        [ValidateNever]
        public MaintenanceContract MaintenanceContract { get; set; } // if covered by contract

        public DateTime? ResolvedDate { get; set; }
    }
}
