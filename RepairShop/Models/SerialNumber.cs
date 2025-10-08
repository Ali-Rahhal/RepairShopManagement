namespace RepairShop.Models
{
    public class SerialNumber
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public bool IsActive { get; set; } = true;

        public int ModelId { get; set; } 
        public Model Model { get; set; } // belongs to Model

        public int ClientId { get; set; }
        public Client Client { get; set; } // owned by Client

        public int? MaintenanceContractId { get; set; }
        public MaintenanceContract MaintenanceContract { get; set; } // covered by MaintenanceContract
    }
}
