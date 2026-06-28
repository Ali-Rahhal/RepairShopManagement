using RepairShop.Models;

namespace RepairShop.Repository.IRepository
{
    public interface IReceptionNoteRepository : IRepository<ReceptionNote>
    {
        Task AddAsy(ReceptionNote receptionNote);

        Task AddRangeAsy(IEnumerable<ReceptionNote> receptionNotes);

        Task UpdateAsy(ReceptionNote receptionNote);

        Task UpdateRangeAsy(IEnumerable<ReceptionNote> receptionNotes);
    }
}
