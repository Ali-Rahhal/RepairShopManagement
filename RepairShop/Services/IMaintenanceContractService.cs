using RepairShop.Models.Helpers;


namespace RepairShop.Services
{
    public interface IMaintenanceContractService
    {
        Task<(List<SerialNumberSelectDto> available, List<SerialNumberSelectDto> assigned)> LoadSerialNumbersForAssignmentAsync(long clientId, long contractId);
        Task<(bool isValid, string errorMessage)> AssignSerialNumbersAsync(long contractId, List<long> selectedSerialNumberIds);
    }
}
