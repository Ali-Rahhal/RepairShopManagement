using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

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
                includeProperties: "Client,Client.ParentClient"
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
                clientName = mc.Client?.ParentClient != null ? mc.Client?.ParentClient.Name : mc.Client?.Name,
                clientBranch = mc.Client?.ParentClient != null ? mc.Client?.Name : "N/A",
                startDate = mc.StartDate,
                endDate = mc.EndDate,
                status = mc.Status,
                daysRemaining = (mc.EndDate - DateTime.Now).Days,
                isExpired = mc.EndDate < DateTime.Now
            });

            return new JsonResult(new { data = formattedData });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id, [FromServices] DeleteService _dlt)
        {
            var contractToBeDeleted = await _unitOfWork.MaintenanceContract.GetAsy(mc => mc.Id == id && mc.IsActive == true);
            if (contractToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _dlt.DeleteMaintenanceContractAsync(contractToBeDeleted.Id);
            
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
    }
}