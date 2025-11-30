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
    public class TransactionHeaderRepository : Repository<TransactionHeader>, ITransactionHeaderRepository
    {
        private readonly AppDbContext _db;

        public TransactionHeaderRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(TransactionHeader transactionHeader)
        {
            await _db.TransactionHeaders.AddAsync(transactionHeader);

            await _db.SaveChangesAsync();

            transactionHeader.Code = transactionHeader.Id.ToString();

            await UpdateAsy(transactionHeader);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<TransactionHeader> transactionHeaders)
        {
            await _db.TransactionHeaders.AddRangeAsync(transactionHeaders);
            await _db.SaveChangesAsync();
            foreach (var transactionHeader in transactionHeaders)
            {
                transactionHeader.Code = transactionHeader.Id.ToString();
            }

            await UpdateRangeAsy(transactionHeaders);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(TransactionHeader transactionHeader)
        {
            _db.TransactionHeaders.Update(transactionHeader);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<TransactionHeader> transactionHeaders)
        {
            _db.TransactionHeaders.UpdateRange(transactionHeaders);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(TransactionHeader transactionHeader)
        {
            transactionHeader.IsActive = false;
            await UpdateAsy(transactionHeader);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<TransactionHeader> transactionHeaders)
        {
            foreach (var item in transactionHeaders)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}