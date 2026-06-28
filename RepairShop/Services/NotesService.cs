using RepairShop.Models;
using RepairShop.Repository.IRepository;

namespace RepairShop.Services
{
    public class NotesService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotesService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task AddToReceptionNotesAsync(SerialNumber SerialNumber)
        {
            var today = DateTime.Today;

            var receptionNote = await _unitOfWork.ReceptionNote.GetAsy(
                r =>
                    r.ClientId == SerialNumber.ClientId &&
                    r.IsActive &&
                    !r.IsPrinted
            );

            if (receptionNote == null)
            {
                var client = await _unitOfWork.Client.GetAsy(
                    c => c.Id == SerialNumber.ClientId
                );

                client.NoteSequence++;

                receptionNote = new ReceptionNote
                {
                    ClientId = client.Id,
                    Code = $"{client.Code}-01{client.NoteSequence:D8}",
                    CreatedDate = DateTime.Now
                };

                await _unitOfWork.ReceptionNote.AddAsy(receptionNote);

                await _unitOfWork.Client.UpdateAsy(client);

                await _unitOfWork.SaveAsy();
            }

            var item = new ReceptionNoteItem
            {
                ReceptionNoteId = receptionNote.Id,
                SerialNumberId = SerialNumber.Id
            };

            await _unitOfWork.ReceptionNoteItem.AddAsy(item);

            await _unitOfWork.SaveAsy();
        }
        public async Task AddToReceptionNotesAsync(List<SerialNumber> SerialNumbers)
        {
            foreach (var sn in SerialNumbers)
            {

                var receptionNote = await _unitOfWork.ReceptionNote.GetAsy(
                    r =>
                        r.ClientId == sn.ClientId &&
                        r.IsActive &&
                        !r.IsPrinted
                );

                if (receptionNote == null)
                {
                    var client = await _unitOfWork.Client.GetAsy(
                        c => c.Id == sn.ClientId
                    );

                    client.NoteSequence++;

                    receptionNote = new ReceptionNote
                    {
                        ClientId = client.Id,
                        Code = $"{client.Code}-01{client.NoteSequence:D8}",
                        CreatedDate = DateTime.Now
                    };

                    await _unitOfWork.ReceptionNote.AddAsy(receptionNote);

                    await _unitOfWork.Client.UpdateAsy(client);

                    await _unitOfWork.SaveAsy();
                }

                var item = new ReceptionNoteItem
                {
                    ReceptionNoteId = receptionNote.Id,
                    SerialNumberId = sn.Id
                };

                await _unitOfWork.ReceptionNoteItem.AddAsy(item);
            }
            
            await _unitOfWork.SaveAsy();
        }
    }
}
