using RepairShop.Models;

namespace RepairShop.Repository.IRepository
{
    public interface IPreventiveMaintenanceRecordRepository : IRepository<PreventiveMaintenanceRecord>
    {
        Task AddAsy(PreventiveMaintenanceRecord preventiveMaintenanceRecord);

        Task AddRangeAsy(IEnumerable<PreventiveMaintenanceRecord> preventiveMaintenanceRecords);

        Task UpdateAsy(PreventiveMaintenanceRecord preventiveMaintenanceRecord);

        Task UpdateRangeAsy(IEnumerable<PreventiveMaintenanceRecord> preventiveMaintenanceRecords);
    }
}
