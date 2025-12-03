using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;

namespace RepairShop.Repository
{
    public class PreventiveMaintenanceRecordRepository : Repository<PreventiveMaintenanceRecord>, IPreventiveMaintenanceRecordRepository
    {
        private readonly AppDbContext _db;

        public PreventiveMaintenanceRecordRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(PreventiveMaintenanceRecord preventiveMaintenanceRecord)
        {
            await _db.PreventiveMaintenanceRecords.AddAsync(preventiveMaintenanceRecord);

            await _db.SaveChangesAsync();

            preventiveMaintenanceRecord.Code = preventiveMaintenanceRecord.Id.ToString();

            await UpdateAsy(preventiveMaintenanceRecord);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<PreventiveMaintenanceRecord> preventiveMaintenanceRecords)
        {
            await _db.PreventiveMaintenanceRecords.AddRangeAsync(preventiveMaintenanceRecords);
            await _db.SaveChangesAsync();
            foreach (var preventiveMaintenanceRecord in preventiveMaintenanceRecords)
            {
                preventiveMaintenanceRecord.Code = preventiveMaintenanceRecord.Id.ToString();
            }

            await UpdateRangeAsy(preventiveMaintenanceRecords);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(PreventiveMaintenanceRecord preventiveMaintenanceRecord)
        {
            _db.PreventiveMaintenanceRecords.Update(preventiveMaintenanceRecord);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<PreventiveMaintenanceRecord> preventiveMaintenanceRecords)
        {
            _db.PreventiveMaintenanceRecords.UpdateRange(preventiveMaintenanceRecords);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(PreventiveMaintenanceRecord preventiveMaintenanceRecord)
        {
            preventiveMaintenanceRecord.IsActive = false;
            await UpdateAsy(preventiveMaintenanceRecord);
            await Task.CompletedTask;
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<PreventiveMaintenanceRecord> preventiveMaintenanceRecords)
        {
            foreach (var item in preventiveMaintenanceRecords)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}
