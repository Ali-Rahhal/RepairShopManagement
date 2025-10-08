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
    public class SerialNumberRepository : Repository<SerialNumber>, ISerialNumberRepository
    {
        private readonly AppDbContext _db;

        public SerialNumberRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsy(SerialNumber serialNumber)
        {
            _db.SerialNumbers.Update(serialNumber);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<SerialNumber> serialNumbers)
        {
            _db.SerialNumbers.UpdateRange(serialNumbers);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(SerialNumber serialNumber)
        {
            serialNumber.IsActive = false;
            await UpdateAsy(serialNumber);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<SerialNumber> serialNumbers)
        {
            foreach (var item in serialNumbers)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}