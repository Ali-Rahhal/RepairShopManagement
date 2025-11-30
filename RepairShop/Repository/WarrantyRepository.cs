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
    public class WarrantyRepository : Repository<Warranty>, IWarrantyRepository
    {
        private readonly AppDbContext _db;

        public WarrantyRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(Warranty warranty)
        {
            await _db.Warranties.AddAsync(warranty);

            await _db.SaveChangesAsync();

            warranty.Code = warranty.Id.ToString();

            await UpdateAsy(warranty);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<Warranty> warranties)
        {
            await _db.Warranties.AddRangeAsync(warranties);
            await _db.SaveChangesAsync();
            foreach (var warranty in warranties)
            {
                warranty.Code = warranty.Id.ToString();
            }

            await UpdateRangeAsy(warranties);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(Warranty warranty)
        {
            _db.Warranties.Update(warranty);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<Warranty> warranties)
        {
            _db.Warranties.UpdateRange(warranties);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(Warranty warranty)
        {
            warranty.IsActive = false;
            await UpdateAsy(warranty);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<Warranty> warranties)
        {
            foreach (var item in warranties)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}