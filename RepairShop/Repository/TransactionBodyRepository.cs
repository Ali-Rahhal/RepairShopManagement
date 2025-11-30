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
    public class TransactionBodyRepository : Repository<TransactionBody>, ITransactionBodyRepository
    {
        private readonly AppDbContext _db;

        public TransactionBodyRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(TransactionBody transactionBody)
        {
            await _db.TransactionBodies.AddAsync(transactionBody);

            await _db.SaveChangesAsync();

            transactionBody.Code = transactionBody.Id.ToString();

            await UpdateAsy(transactionBody);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<TransactionBody> transactionBodies)
        {
            await _db.TransactionBodies.AddRangeAsync(transactionBodies);
            await _db.SaveChangesAsync();
            foreach (var transactionBody in transactionBodies)
            {
                transactionBody.Code = transactionBody.Id.ToString();
            }

            await UpdateRangeAsy(transactionBodies);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(TransactionBody transactionBody)
        {
            _db.TransactionBodies.Update(transactionBody);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<TransactionBody> transactionBodies)
        {
            _db.TransactionBodies.UpdateRange(transactionBodies);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(TransactionBody transactionBody)
        {
            transactionBody.IsActive = false;
            await UpdateAsy(transactionBody);
            await Task.CompletedTask;
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<TransactionBody> transactionBodies)
        {
            foreach (var item in transactionBodies)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}