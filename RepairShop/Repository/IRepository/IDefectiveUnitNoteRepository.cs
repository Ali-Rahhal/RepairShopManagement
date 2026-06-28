using RepairShop.Models;

namespace RepairShop.Repository.IRepository
{
    public interface IDefectiveUnitNoteRepository : IRepository<DefectiveUnitNote>
    {
        Task AddAsy(DefectiveUnitNote defectiveUnitNote);

        Task AddRangeAsy(IEnumerable<DefectiveUnitNote> defectiveUnitNotes);

        Task UpdateAsy(DefectiveUnitNote defectiveUnitNote);

        Task UpdateRangeAsy(IEnumerable<DefectiveUnitNote> defectiveUnitNotes);
    }
}
