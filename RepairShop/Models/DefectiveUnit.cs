namespace RepairShop.Models
{
    public class DefectiveUnit
    {
        public int Id { get; set; }
        public DateTime ReportedDate { get; set; }
        public string Description { get; set; } // e.g. "Screen flickering", "Power failure"
        public string Status { get; set; } // e.g. Reported, UnderRepair, Fixed, Replaced
        public bool IsResolved { get; set; }
        public bool IsActive { get; set; } = true;

        // Relationships
        public int SerialNumberId { get; set; }
        public SerialNumber SerialNumber { get; set; } // which device failed

        public int? WarrantyId { get; set; }
        public Warranty Warranty { get; set; } // if covered by warranty

        public int? MaintenanceContractId { get; set; }
        public MaintenanceContract MaintenanceContract { get; set; } // if covered by contract

        public DateTime? ResolvedDate { get; set; }
    }
}
