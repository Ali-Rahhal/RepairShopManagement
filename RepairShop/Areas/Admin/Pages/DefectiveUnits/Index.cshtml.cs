using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.DefectiveUnits
{
    [Authorize(Roles = SD.Role_Admin)]
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
            var defectiveUnitList = (await _unitOfWork.DefectiveUnit.GetAllAsy(
                filter: du => du.IsActive == true,
                includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client,Warranty,MaintenanceContract"
            )).ToList();

            // Format the data for better display
            var formattedData = defectiveUnitList.Select(du => new
            {
                id = du.Id,
                reportedDate = du.ReportedDate.ToString("yyyy-MM-dd"),
                description = du.Description,
                status = du.Status,
                isResolved = du.IsResolved,
                resolvedDate = du.ResolvedDate?.ToString("yyyy-MM-dd") ?? "Not resolved",
                serialNumber = du.SerialNumber?.Value ?? "N/A",
                modelName = du.SerialNumber?.Model?.Name ?? "N/A",
                clientName = du.SerialNumber?.Client?.Name ?? "N/A",
                warrantyCovered = du.WarrantyId != null ? "Yes" : "No",
                contractCovered = du.MaintenanceContractId != null ? "Yes" : "No",
                daysSinceReported = (DateTime.Now - du.ReportedDate).Days
            });

            return new JsonResult(new { data = formattedData });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var defectiveUnitToBeDeleted = await _unitOfWork.DefectiveUnit.GetAsy(du => du.Id == id);
            if (defectiveUnitToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _unitOfWork.DefectiveUnit.RemoveAsy(defectiveUnitToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Defective unit report deleted successfully" });
        }

        // API for Status Update
        public async Task<IActionResult> OnGetUpdateStatus(int? id, string status)
        {
            var defectiveUnit = await _unitOfWork.DefectiveUnit.GetAsy(du => du.Id == id);
            if (defectiveUnit == null)
            {
                return new JsonResult(new { success = false, message = "Defective unit not found" });
            }

            defectiveUnit.Status = status;
            if (status == "Fixed")
            {
                defectiveUnit.IsResolved = true;
                defectiveUnit.ResolvedDate = DateTime.Now;
            }
            else if (status == "OutOfService")
            {
                defectiveUnit.IsResolved = true;
                defectiveUnit.ResolvedDate = DateTime.Now;
            }

            await _unitOfWork.DefectiveUnit.UpdateAsy(defectiveUnit);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = $"Status updated to {status}" });
        }
    }
}