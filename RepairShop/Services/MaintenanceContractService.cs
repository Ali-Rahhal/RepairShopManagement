using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;


namespace RepairShop.Services
{
    public class MaintenanceContractService : IMaintenanceContractService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MaintenanceContractService(IUnitOfWork uow)
        {
            _unitOfWork = uow;
        }

        public async Task<(List<SerialNumberSelectDto> available, List<SerialNumberSelectDto> assigned)> LoadSerialNumbersForAssignmentAsync(long clientId, long contractId)
        {
            var serials = (await _unitOfWork.SerialNumber.GetAllAsy(sn => sn.ClientId == clientId && sn.IsActive, includeProperties: "Model,MaintenanceContract"))
                          .OrderBy(sn => sn.Value)
                          .ToList();

            var assigned = serials.Where(sn => sn.MaintenanceContractId == contractId)
                                  .Select(sn => new SerialNumberSelectDto { Id = sn.Id, Value = sn.Value, ModelName = sn.Model?.Name ?? "-", ReceivedDate = sn.ReceivedDate })
                                  .ToList();

            var available = serials.Where(sn => sn.MaintenanceContractId == null || sn.MaintenanceContractId == contractId || (sn.MaintenanceContract != null && sn.MaintenanceContract.EndDate < DateTime.Now))
                                   .Select(sn => new SerialNumberSelectDto { Id = sn.Id, Value = sn.Value, ModelName = sn.Model?.Name ?? "-", ReceivedDate = sn.ReceivedDate })
                                   .ToList();

            // remove duplicates in available that are in assigned
            available = available.Where(a => !assigned.Any(x => x.Id == a.Id)).ToList();

            return (available, assigned);
        }

        public async Task<(bool isValid, string errorMessage)> AssignSerialNumbersAsync(long contractId, List<long> selectedSerialNumberIds)
        {
            selectedSerialNumberIds = selectedSerialNumberIds ?? new List<long>();

            // Validate existence and availability
            var errorMessages = new List<string>();
            var validIds = new List<long>();

            foreach (var id in selectedSerialNumberIds)
            {
                var sn = await _unitOfWork.SerialNumber.GetAsy(s => s.Id == id && s.IsActive, includeProperties: "MaintenanceContract");
                if (sn == null)
                {
                    errorMessages.Add($"Serial number id {id} not found.");
                    continue;
                }

                if (sn.MaintenanceContractId.HasValue && sn.MaintenanceContractId != contractId)
                {
                    var current = sn.MaintenanceContract;
                    if (current != null && current.EndDate >= DateTime.Now)
                    {
                        errorMessages.Add($"Serial number {sn.Value} is covered by active contract (Contract-{current.Id:D4}) until {current.EndDate:yyyy-MM-dd}.");
                        continue;
                    }
                }

                validIds.Add(id);
            }

            if (errorMessages.Any())
                return (false, string.Join(" ", errorMessages));

            // Unassign serials currently assigned to this contract (but not in selected list)
            var existing = (await _unitOfWork.SerialNumber.GetAllAsy(sn => sn.MaintenanceContractId == contractId && sn.IsActive, tracked: true)).ToList();
            foreach (var sn in existing)
            {
                if (!validIds.Contains(sn.Id))
                {
                    sn.MaintenanceContractId = null;
                    await _unitOfWork.SerialNumber.UpdateAsy(sn);
                }
            }

            // Assign selected
            foreach (var id in validIds)
            {
                var sn = await _unitOfWork.SerialNumber.GetAsy(s => s.Id == id && s.IsActive, tracked: true);
                if (sn != null)
                {
                    sn.MaintenanceContractId = contractId;
                    await _unitOfWork.SerialNumber.UpdateAsy(sn);
                }
            }

            await _unitOfWork.SaveAsy();
            return (true, string.Empty);
        }
    }
}