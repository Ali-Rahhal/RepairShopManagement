using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository
{
    public class MaintenanceContractRepository : Repository<MaintenanceContract>, IMaintenanceContractRepository
    {
        private readonly AppDbContext _db;

        public MaintenanceContractRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsy(MaintenanceContract maintenanceContract)
        {
            _db.MaintenanceContracts.Update(maintenanceContract);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<MaintenanceContract> maintenanceContracts)
        {
            _db.MaintenanceContracts.UpdateRange(maintenanceContracts);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(MaintenanceContract maintenanceContract)
        {
            maintenanceContract.IsActive = false;
            await UpdateAsy(maintenanceContract);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<MaintenanceContract> maintenanceContracts)
        {
            foreach (var item in maintenanceContracts)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}