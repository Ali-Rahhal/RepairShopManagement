using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using System.Security.Claims;

namespace RepairShop.Areas.Admin.Pages.DefectiveUnits
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
            var defectiveUnitList = (await _unitOfWork.DefectiveUnit.GetAllAsy(
                filter: du => du.IsActive == true,
                includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client,Warranty,MaintenanceContract"
            )).ToList();

            // Format the data for better display
            var formattedData = defectiveUnitList.Select(du => new
            {
                id = du.Id,
                reportedDate = du.ReportedDate,
                description = du.Description,
                hasAccessories = du.HasAccessories,
                status = du.Status,
                resolvedDate = du.ResolvedDate?.ToString("dd/MM/yyyy") ?? "Not resolved",
                serialNumber = du.SerialNumber?.Value ?? "N/A",
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

            var relatedTransactionHeaders = (await _unitOfWork.TransactionHeader.GetAllAsy(th => th.IsActive == true && th.DefectiveUnitId == id)).ToList();
            if (relatedTransactionHeaders.Count > 0)
            {
                return new JsonResult(new { success = false, message = "Cannot delete this report because it has related transaction headers" });
            }

            await _unitOfWork.DefectiveUnit.RemoveAsy(defectiveUnitToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Defective unit report deleted successfully" });
        }

        public async Task<IActionResult> OnGetAddToTransaction(int? DuId)
        {
            if (DuId == null || DuId == 0)
            {
                return new JsonResult(new { success = false, message = "Error while adding to transaction" });
            }

            var defectiveUnitToBeAdded = await _unitOfWork.DefectiveUnit.GetAsy(du => du.Id == DuId, includeProperties: "SerialNumber,SerialNumber.Client");

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (defectiveUnitToBeAdded == null || currentUserId == null)
            {
                return new JsonResult(new { success = false, message = "Error while adding to transaction" });
            }

            TransactionHeader thForDu = new TransactionHeader()
            {
                DefectiveUnitId = (int)DuId,
                ClientId = defectiveUnitToBeAdded.SerialNumber.ClientId,
                UserId = currentUserId
            };

            defectiveUnitToBeAdded.Status = SD.Status_DU_UnderRepair;
            await _unitOfWork.DefectiveUnit.UpdateAsy(defectiveUnitToBeAdded);

            await _unitOfWork.TransactionHeader.AddAsy(thForDu);

            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Defective unit added to transaction successfully" });
        }


    }
}