using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.SerialNumbers
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
            var serialNumberList = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.IsActive == true,
                includeProperties: "Model,Client,Client.ParentClient,MaintenanceContract"
            )).ToList();

            var formattedData = serialNumberList.Select(sn => new
            {
                id = sn.Id,
                value = sn.Value,
                modelName = sn.Model.Name,
                clientName = sn.Client.ParentClient != null ? sn.Client.ParentClient.Name : sn.Client.Name,
                branchName = sn.Client.ParentClient != null ? sn.Client.Name : "N/A",
                maintenanceContractId = sn.MaintenanceContractId ?? null,
                warrantyId = sn.WarrantyId ?? null,
                receivedDate = sn.ReceivedDate
            });

            return new JsonResult(new { data = formattedData });
        }

        // API for Delete
        public async Task<IActionResult> OnPostDelete(int? id, string reason)
        {
            var serialNumberToBeDeleted = await _unitOfWork.SerialNumber.GetAsy(sn => sn.Id == id && sn.IsActive == true);
            if (serialNumberToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // Check if serial number is referenced in any defective units
            var isUsedInDefectiveUnits = (await _unitOfWork.DefectiveUnit
                .GetAllAsy(du => du.SerialNumberId == serialNumberToBeDeleted.Id && du.IsActive == true));

            if (isUsedInDefectiveUnits.Any())
            {
                return new JsonResult(new { success = false, message = "Serial number cannot be deleted because it is used in defective units" });
            }

            var isUsedInPreventiveMaintenances = (await _unitOfWork.PreventiveMaintenanceRecord.GetAllAsy(pm => pm.SerialNumberId == serialNumberToBeDeleted.Id && pm.IsActive == true));

            if (isUsedInPreventiveMaintenances.Any())
            {
                return new JsonResult(new { success = false, message = "Serial number cannot be deleted because it is used in preventive maintenance records" });
            }

            serialNumberToBeDeleted.InactiveReason = reason;

            await _unitOfWork.SerialNumber.RemoveAsy(serialNumberToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Serial number deleted successfully" });
        }

        // API for Models (for filter dropdown)
        public async Task<JsonResult> OnGetModels()
        {
            var models = (await _unitOfWork.Model.GetAllAsy(m => m.IsActive == true 
                            && m.SerialNumbers.Any(sn => sn.IsActive == true), 
                            includeProperties: "SerialNumbers"))
                                .Select(m => new { id = m.Id, name = m.Name })
                                .OrderBy(m => m.name)
                                .ToList();

            return new JsonResult(new { models });
        }

        // API for Clients (for filter dropdown)
        public async Task<JsonResult> OnGetClients()
        {
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true
                            && c.ParentClient == null
                            && c.SerialNumbers.Any(sn => sn.IsActive == true),
                            includeProperties: "SerialNumbers"))
                                .Select(c => new { id = c.Id, name = c.Name })
                                .OrderBy(c => c.name)
                                .ToList();

            return new JsonResult(new { clients });
        }
    }
}