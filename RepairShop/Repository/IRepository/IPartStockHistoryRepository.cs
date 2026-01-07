using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface IPartStockHistoryRepository : IRepository<PartStockHistory>
    {
        Task AddAsy(PartStockHistory partStockHistory);

        Task AddRangeAsy(IEnumerable<PartStockHistory> partStockHistories);

        Task UpdateAsy(PartStockHistory partStockHistory);

        Task UpdateRangeAsy(IEnumerable<PartStockHistory> partStockHistories);
    }
}