using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;

namespace RepairShop.Repository
{
    public class ReceptionNoteItemRepository : Repository<ReceptionNoteItem>, IReceptionNoteItemRepository
    {
        private readonly AppDbContext _db;

        public ReceptionNoteItemRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(ReceptionNoteItem receptionNoteItem)
        {
            await _db.ReceptionNoteItems.AddAsync(receptionNoteItem);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<ReceptionNoteItem> receptionNoteItems)
        {
            await _db.ReceptionNoteItems.AddRangeAsync(receptionNoteItems);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(ReceptionNoteItem receptionNoteItem)
        {
            _db.ReceptionNoteItems.Update(receptionNoteItem);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<ReceptionNoteItem> receptionNoteItems)
        {
            _db.ReceptionNoteItems.UpdateRange(receptionNoteItems);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(ReceptionNoteItem receptionNoteItem)
        {
            receptionNoteItem.IsActive = false;
            await UpdateAsy(receptionNoteItem);
            await Task.CompletedTask;
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<ReceptionNoteItem> receptionNoteItems)
        {
            foreach (var item in receptionNoteItems)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}
