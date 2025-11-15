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
                includeProperties: "Model,Client,MaintenanceContract"
            )).ToList();

            return new JsonResult(new { data = serialNumberList });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var serialNumberToBeDeleted = await _unitOfWork.SerialNumber.GetAsy(sn => sn.Id == id && sn.IsActive == true);
            if (serialNumberToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // Check if serial number is referenced in any defective units
            var isUsedInDefectiveUnits = (await _unitOfWork.DefectiveUnit
                .GetAllAsy(du => du.IsActive == true && du.SerialNumberId == serialNumberToBeDeleted.Id));

            if (isUsedInDefectiveUnits.Any())
            {
                return new JsonResult(new { success = false, message = "Serial number cannot be deleted because it is used in defective units" });
            }

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
                            && c.SerialNumbers.Any(sn => sn.IsActive == true),
                            includeProperties: "SerialNumbers"))
                                .Select(c => new { id = c.Id, name = c.Name })
                                .OrderBy(c => c.name)
                                .ToList();

            return new JsonResult(new { clients });
        }
    }
}