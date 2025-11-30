using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface IMaintenanceContractRepository : IRepository<MaintenanceContract>
    {
        Task AddAsy(MaintenanceContract maintenanceContract);

        Task AddRangeAsy(IEnumerable<MaintenanceContract> maintenanceContracts);

        Task UpdateAsy(MaintenanceContract maintenanceContract);

        Task UpdateRangeAsy(IEnumerable<MaintenanceContract> maintenanceContracts);
    }
}