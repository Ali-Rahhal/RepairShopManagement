using RepairShop.Models;

namespace RepairShop.Repository.IRepository
{
    public interface IReceptionNoteItemRepository : IRepository<ReceptionNoteItem>
    {
        Task AddAsy(ReceptionNoteItem receptionNoteItem);

        Task AddRangeAsy(IEnumerable<ReceptionNoteItem> receptionNoteItems);

        Task UpdateAsy(ReceptionNoteItem receptionNoteItem);

        Task UpdateRangeAsy(IEnumerable<ReceptionNoteItem> receptionNoteItems);
    }
}
