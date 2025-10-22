using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.MaintenanceContracts
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void OnGet()
        {
        }

        // API for DataTable
        public async Task<JsonResult> OnGetAll()
        {
            var contractList = (await _unitOfWork.MaintenanceContract.GetAllAsy(
                mc => mc.IsActive == true,
                includeProperties: "Client"
            )).ToList();

            // Update status for contracts that have expired
            var updatedContracts = new List<MaintenanceContract>();
            foreach (var contract in contractList)
            {
                var newStatus = contract.EndDate < DateTime.Now ? "Expired" : "Active";
                if (contract.Status != newStatus)
                {
                    contract.Status = newStatus;
                    updatedContracts.Add(contract);
                }
            }

            // Save status changes to database
            if (updatedContracts.Count > 0)
            {
                foreach (var contract in updatedContracts)
                {
                    await _unitOfWork.MaintenanceContract.UpdateAsy(contract);
                }
                await _unitOfWork.SaveAsy();
            }

            // Format the data for better display
            var formattedData = contractList.Select(mc => new
            {
                id = mc.Id,
                contractNumber = $"CONTRACT-{mc.Id:D4}",
                clientName = mc.Client?.Name ?? "N/A",
                clientId = mc.ClientId,
                startDate = mc.StartDate,
                endDate = mc.EndDate,
                status = mc.Status,
                daysRemaining = (mc.EndDate - DateTime.Now).Days,
                isExpired = mc.EndDate < DateTime.Now
            });

            return new JsonResult(new { data = formattedData });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var contractToBeDeleted = await _unitOfWork.MaintenanceContract.GetAsy(mc => mc.Id == id);
            if (contractToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // Check if contract has associated serial numbers
            var hasSerialNumbers = (await _unitOfWork.SerialNumber
                .GetAllAsy(sn => sn.IsActive == true && sn.MaintenanceContractId == contractToBeDeleted.Id))
                .Any();

            if (hasSerialNumbers)
            {
                return new JsonResult(new { success = false, message = "Contract cannot be deleted because it has associated serial numbers" });
            }

            await _unitOfWork.MaintenanceContract.RemoveAsy(contractToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Maintenance contract deleted successfully" });
        }

        // API for Clients (for filter dropdown)
        public async Task<JsonResult> OnGetClients()
        {
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true))
                .Select(c => new { id = c.Id, name = c.Name, phone = c.Phone })
                .OrderBy(c => c.name)
                .ToList();

            return new JsonResult(new { clients });
        }

        // API to get serial numbers for a client (both available and assigned)
        public async Task<JsonResult> OnGetClientSerialNumbers(int contractId, int clientId)
        {
            // Get serial numbers already assigned to this contract
            var assignedSerialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.IsActive == true && sn.MaintenanceContractId == contractId,
                includeProperties: "Model"
            ))
            .Select(sn => new
            {
                id = sn.Id,
                value = sn.Value,
                modelName = sn.Model.Name,
                isAssigned = true
            })
            .OrderBy(sn => sn.value)
            .ToList();

            // Get available serial numbers (no contract)
            var availableSerialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.IsActive == true && sn.ClientId == clientId && sn.MaintenanceContractId == null,
                includeProperties: "Model"
            ))
            .Select(sn => new
            {
                id = sn.Id,
                value = sn.Value,
                modelName = sn.Model.Name,
                isAssigned = false
            })
            .OrderBy(sn => sn.value)
            .ToList();

            // Combine both lists
            var allSerialNumbers = assignedSerialNumbers.Concat(availableSerialNumbers).ToList();

            return new JsonResult(new
            {
                serialNumbers = allSerialNumbers,
                assignedCount = assignedSerialNumbers.Count,
                availableCount = availableSerialNumbers.Count
            });
        }

        // API to assign/remove contract from serial numbers
        public async Task<IActionResult> OnPostAssignToSerialNumbers(int contractId, List<int> serialNumberIds)
        {
            try
            {
                var contract = await _unitOfWork.MaintenanceContract.GetAsy(mc => mc.Id == contractId);
                if (contract == null)
                {
                    return new JsonResult(new { success = false, message = "Contract not found" });
                }

                // Get all serial numbers for this client
                var clientSerialNumbers = await _unitOfWork.SerialNumber.GetAllAsy(
                    sn => sn.IsActive == true && sn.ClientId == contract.ClientId
                );

                int assignedCount = 0;
                int removedCount = 0;

                foreach (var serialNumber in clientSerialNumbers)
                {
                    if (serialNumberIds.Contains(serialNumber.Id))
                    {
                        // Assign contract to this serial number
                        if (serialNumber.MaintenanceContractId != contractId)
                        {
                            serialNumber.MaintenanceContractId = contractId;
                            await _unitOfWork.SerialNumber.UpdateAsy(serialNumber);
                            assignedCount++;
                        }
                    }
                    else
                    {
                        // Remove contract from this serial number if it was previously assigned
                        if (serialNumber.MaintenanceContractId == contractId)
                        {
                            serialNumber.MaintenanceContractId = null;
                            await _unitOfWork.SerialNumber.UpdateAsy(serialNumber);
                            removedCount++;
                        }
                    }
                }

                await _unitOfWork.SaveAsy();

                var message = new List<string>();
                if (assignedCount > 0) message.Add($"Assigned to {assignedCount} serial number(s)");
                if (removedCount > 0) message.Add($"Removed from {removedCount} serial number(s)");

                return new JsonResult(new
                {
                    success = true,
                    message = message.Count > 0 ? string.Join(" and ", message) : "No changes made",
                    assignedCount = assignedCount,
                    removedCount = removedCount
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error updating serial numbers: {ex.Message}" });
            }
        }
    }
}