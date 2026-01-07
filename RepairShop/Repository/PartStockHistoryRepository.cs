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
    public class PartStockHistoryRepository : Repository<PartStockHistory>, IPartStockHistoryRepository
    {
        private readonly AppDbContext _db;

        public PartStockHistoryRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(PartStockHistory partStockHistory)
        {
            await _db.PartStockHistory.AddAsync(partStockHistory);

            await _db.SaveChangesAsync();

            partStockHistory.Code = partStockHistory.Id.ToString();

            await UpdateAsy(partStockHistory);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<PartStockHistory> partStockHistories)
        {
            await _db.PartStockHistory.AddRangeAsync(partStockHistories);
            await _db.SaveChangesAsync();
            foreach (var item in partStockHistories)
            {
                item.Code = item.Id.ToString();
            }

            await UpdateRangeAsy(partStockHistories);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(PartStockHistory partStockHistory)
        {
            _db.PartStockHistory.Update(partStockHistory);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<PartStockHistory> partStockHistories)
        {
            _db.PartStockHistory.UpdateRange(partStockHistories);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(PartStockHistory partStockHistory)
        {
            partStockHistory.IsActive = false;
            await UpdateAsy(partStockHistory);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<PartStockHistory> partStockHistories)
        {
            foreach (var item in partStockHistories)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}