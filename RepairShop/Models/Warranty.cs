namespace RepairShop.Models
{
    public class Warranty
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }//Active or Expired
        public bool IsActive { get; set; } = true;


        public int SerialNumberId { get; set; }
        public SerialNumber SerialNumber { get; set; } // covers a serial number
    }
}
