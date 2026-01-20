namespace RepairShop.Models.Helpers
{
    public class PartStockHistoryVMs
    {
        public class PartUsageReportVM
        {
            public long PartId { get; set; }
            public string PartName { get; set; }

            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }

            public int QuantityAtStart { get; set; }
            public int QuantityAtEnd { get; set; }
            public int NetChange => QuantityAtEnd - QuantityAtStart;

            public int DevicesCount { get; set; }

            public List<PartStockHistoryRowVM> History { get; set; } = new();
        }

        public class PartStockHistoryRowVM
        {
            public string UserName { get; set; }
            public DateTime Date { get; set; }
            public string ClientName { get; set; }
            public string SerialNumber { get; set; }
            public string ModelName { get; set; }
            public int QuantityChange { get; set; }
            public int QuantityAfter { get; set; }
            public string Reason { get; set; }
        }

        // NEW: ViewModel for detailed movements
        public class PartMovementDetailVM
        {
            public string PartName { get; set; }
            public DateTime Date { get; set; }
            public int QuantityChange { get; set; }
            public int QuantityAfter { get; set; }
            public string Reason { get; set; }
            public string ClientName { get; set; }
            public string SerialNumber { get; set; }
            public string ModelName { get; set; }
        }
    }
}
