using RepairShop.Models.Helpers;
using RepairShop.Repository;
using RepairShop.Repository.IRepository;

namespace RepairShop.Services
{
    public class DeleteService//Caution: this service will delete a table and all related tables of data!!!!
    {
        private readonly IUnitOfWork _uow;

        public DeleteService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // =============================== PART ===============================
        public async Task DeletePartAsync(long partId)
        {
            var tbs = await _uow.TransactionBody.GetAllAsy(
                tb => tb.PartId == partId && tb.IsActive,
                tracked: true
            );

            foreach (var tb in tbs)
                await DeleteTransactionBodyAsync(tb.Id);

            var part = await _uow.Part.GetAsy(
                p => p.Id == partId && p.IsActive,
                tracked: true
            );

            if (part != null)
                await _uow.Part.RemoveAsy(part);

            await _uow.SaveAsy();
        }

        // =============================== TRANSACTION BODY ===============================
        public async Task DeleteTransactionBodyAsync(long tbId)
        {
            var tb = await _uow.TransactionBody.GetAsy(
                t => t.Id == tbId && t.IsActive,
                tracked: true
            );

            if (tb.Status == SD.Status_Part_Pending_Replace || tb.Status == SD.Status_Part_Replaced)
            {
                // If part is selected, increment inventory before deleting the tb
                if (tb.PartId.HasValue)
                {
                    var replacementPart = await _uow.Part.GetAsy(p => p.Id == tb.PartId.Value && p.IsActive == true, tracked: true);
                    if (replacementPart != null && replacementPart.Quantity >= 0)
                    {
                        replacementPart.Quantity++;
                        await _uow.Part.UpdateAsy(replacementPart);
                    }
                }
            }

            if (tb != null)
                await _uow.TransactionBody.RemoveAsy(tb);

            await _uow.SaveAsy();
        }

        // =============================== TRANSACTION HEADER ===============================
        public async Task DeleteTransactionHeaderAsync(long thId)
        {
            var tbs = await _uow.TransactionBody.GetAllAsy(
                tb => tb.TransactionHeaderId == thId && tb.IsActive,
                tracked: true
            );

            foreach (var tb in tbs)
                await DeleteTransactionBodyAsync(tb.Id);

            var th = await _uow.TransactionHeader.GetAsy(
                t => t.Id == thId && t.IsActive,
                tracked: true
            );

            if (th != null)
            {
                await _uow.TransactionHeader.RemoveAsy(th);

                var du = await _uow.DefectiveUnit.GetAsy(
                    d => d.Id == th.DefectiveUnitId && d.IsActive,
                    tracked: true
                );

                if (du != null)
                    await _uow.DefectiveUnit.RemoveAsy(du);
            }

            await _uow.SaveAsy();
        }

        // =============================== DEFECTIVE UNIT ===============================
        public async Task DeleteDefectiveUnitAsync(long duId)
        {
            var th = await _uow.TransactionHeader.GetAsy(
                th => th.DefectiveUnitId == duId && th.IsActive,
                tracked: true
            );

            if (th != null)
            {
                var tbs = await _uow.TransactionBody.GetAllAsy(
                    tb => tb.TransactionHeaderId == th.Id && tb.IsActive,
                    tracked: true
                );

                foreach (var tb in tbs)
                    await DeleteTransactionBodyAsync(tb.Id);

                await _uow.TransactionHeader.RemoveAsy(th);
            }

            var du = await _uow.DefectiveUnit.GetAsy(
                d => d.Id == duId && d.IsActive,
                tracked: true
            );

            if (du != null)
                await _uow.DefectiveUnit.RemoveAsy(du);

            await _uow.SaveAsy();
        }

        // =============================== SERIAL NUMBER ===============================
        public async Task DeleteSerialNumberAsync(long snId)
        {
            var dus = await _uow.DefectiveUnit.GetAllAsy(
                d => d.SerialNumberId == snId && d.IsActive,
                tracked: true
            );

            foreach (var du in dus)
                await DeleteDefectiveUnitAsync(du.Id);

            var sn = await _uow.SerialNumber.GetAsy(
                sn => sn.Id == snId && sn.IsActive,
                tracked: true
            );

            if(sn != null)
                await _uow.SerialNumber.RemoveAsy(sn);

            await _uow.SaveAsy();
        }

        // =============================== MAINTENANCE CONTRACT ===============================
        public async Task DeleteMaintenanceContractAsync(long mcId)
        {
            var sns = await _uow.SerialNumber.GetAllAsy(
                sn => sn.MaintenanceContractId == mcId && sn.IsActive,
                tracked: true
            );

            foreach (var sn in sns)
            {
                sn.MaintenanceContractId = null;
                await _uow.SerialNumber.UpdateAsy(sn);
            }

            var mc = await _uow.MaintenanceContract.GetAsy(
                m => m.Id == mcId && m.IsActive,
                tracked: true
            );

            if (mc != null)
                await _uow.MaintenanceContract.RemoveAsy(mc);

            await _uow.SaveAsy();
        }

        // =============================== WARRANTY ===============================
        public async Task DeleteWarrantyAsync(long warrantyId)
        {
            var sns = await _uow.SerialNumber.GetAllAsy(
                sn => sn.WarrantyId == warrantyId && sn.IsActive,
                tracked: true
            );

            foreach (var sn in sns)
            {
                sn.WarrantyId = null;
                await _uow.SerialNumber.UpdateAsy(sn);
            }

            var w = await _uow.Warranty.GetAsy(
                w => w.Id == warrantyId && w.IsActive,
                tracked: true
            );

            if (w != null)
                await _uow.Warranty.RemoveAsy(w);

            await _uow.SaveAsy();
        }

        // =============================== MODEL ===============================
        public async Task DeleteModelAsync(long modelId)
        {
            var sns = await _uow.SerialNumber.GetAllAsy(
                sn => sn.ModelId == modelId && sn.IsActive,
                tracked: true
            );

            foreach (var sn in sns)
                await DeleteSerialNumberAsync(sn.Id);

            var model = await _uow.Model.GetAsy(
                m => m.Id == modelId && m.IsActive,
                tracked: true
            );

            if (model != null)
                await _uow.Model.RemoveAsy(model);

            await _uow.SaveAsy();
        }

        // =============================== CLIENT ===============================
        public async Task DeleteClientAsync(long clientId)
        {
            var mcs = await _uow.MaintenanceContract.GetAllAsy(
                mc => mc.ClientId == clientId && mc.IsActive,
                tracked: true
            );

            foreach (var mc in mcs)
                await DeleteMaintenanceContractAsync(mc.Id);

            var sns = await _uow.SerialNumber.GetAllAsy(
                sn => sn.ClientId == clientId && sn.IsActive,
                tracked: true
            );

            foreach (var sn in sns)
                await DeleteSerialNumberAsync(sn.Id);

            var client = await _uow.Client.GetAsy(
                c => c.Id == clientId && c.IsActive,
                tracked: true
            );

            if (client != null)
                await _uow.Client.RemoveAsy(client);

            await _uow.SaveAsy();
        }
    }

}
