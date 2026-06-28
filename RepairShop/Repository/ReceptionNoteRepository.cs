using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;

namespace RepairShop.Repository
{
    public class ReceptionNoteRepository : Repository<ReceptionNote>, IReceptionNoteRepository
    {
        private readonly AppDbContext _db;

        public ReceptionNoteRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(ReceptionNote receptionNote)
        {
            await _db.ReceptionNotes.AddAsync(receptionNote);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<ReceptionNote> receptionNotes)
        {
            await _db.ReceptionNotes.AddRangeAsync(receptionNotes);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(ReceptionNote receptionNote)
        {
            _db.ReceptionNotes.Update(receptionNote);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<ReceptionNote> receptionNotes)
        {
            _db.ReceptionNotes.UpdateRange(receptionNotes);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(ReceptionNote receptionNote)
        {
            receptionNote.IsActive = false;
            await UpdateAsy(receptionNote);
            await Task.CompletedTask;
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<ReceptionNote> receptionNotes)
        {
            foreach (var item in receptionNotes)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}
