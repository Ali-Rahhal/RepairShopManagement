using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;

namespace RepairShop.Repository
{
    public class DefectiveUnitNoteRepository : Repository<DefectiveUnitNote>, IDefectiveUnitNoteRepository
    {
        private readonly AppDbContext _db;

        public DefectiveUnitNoteRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(DefectiveUnitNote defectiveUnitNote)
        {
            await _db.DefectiveUnitNotes.AddAsync(defectiveUnitNote);

            await _db.SaveChangesAsync();

            defectiveUnitNote.Code = defectiveUnitNote.Id.ToString();

            await UpdateAsy(defectiveUnitNote);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<DefectiveUnitNote> defectiveUnitNotes)
        {
            await _db.DefectiveUnitNotes.AddRangeAsync(defectiveUnitNotes);
            await _db.SaveChangesAsync();
            foreach (var defectiveUnitNote in defectiveUnitNotes)
            {
                defectiveUnitNote.Code = defectiveUnitNote.Id.ToString();
            }

            await UpdateRangeAsy(defectiveUnitNotes);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(DefectiveUnitNote defectiveUnitNote)
        {
            _db.DefectiveUnitNotes.Update(defectiveUnitNote);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<DefectiveUnitNote> defectiveUnitNotes)
        {
            _db.DefectiveUnitNotes.UpdateRange(defectiveUnitNotes);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(DefectiveUnitNote defectiveUnitNote)
        {
            defectiveUnitNote.IsActive = false;
            await UpdateAsy(defectiveUnitNote);
            await Task.CompletedTask;
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<DefectiveUnitNote> defectiveUnitNotes)
        {
            foreach (var item in defectiveUnitNotes)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}
