namespace RepairShop.Models
{
    public class MaintenanceContract
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }// active or expired
        public bool IsActive { get; set; } = true;

        public int ClientId { get; set; }
        public Client Client { get; set; } // belongs to client
    }
}
