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
    public class PartRepository : Repository<Part>, IPartRepository
    {
        private readonly AppDbContext _db;

        public PartRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(Part part)
        {
            await _db.Parts.AddAsync(part);

            await _db.SaveChangesAsync();

            part.Code = part.Id.ToString();

            await UpdateAsy(part);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<Part> parts)
        {
            await _db.Parts.AddRangeAsync(parts);
            await _db.SaveChangesAsync();
            foreach (var part in parts)
            {
                part.Code = part.Id.ToString();
            }

            await UpdateRangeAsy(parts);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(Part part)
        {
            _db.Parts.Update(part);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<Part> parts)
        {
            _db.Parts.UpdateRange(parts);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(Part part)
        {
            part.IsActive = false;
            await UpdateAsy(part);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<Part> parts)
        {
            foreach (var item in parts)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}