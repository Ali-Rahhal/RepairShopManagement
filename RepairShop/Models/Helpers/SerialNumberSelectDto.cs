namespace RepairShop.Models.Helpers
{
    public class SerialNumberSelectDto
    {
        public long Id { get; set; }
        public string Value { get; set; }
        public string ModelName { get; set; }
        public DateTime ReceivedDate { get; set; }
    }
}
