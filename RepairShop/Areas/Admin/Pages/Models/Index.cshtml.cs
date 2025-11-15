using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Models
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
            var modelList = (await _unitOfWork.Model.GetAllAsy(m => m.IsActive == true)).ToList();
            return new JsonResult(new { data = modelList });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var modelToBeDeleted = await _unitOfWork.Model.GetAsy(m => m.Id == id && m.IsActive == true);
            if (modelToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // Check if model has associated serial numbers
            var hasSerialNumbers = (await _unitOfWork.SerialNumber
                .GetAllAsy(sn => sn.IsActive == true && sn.ModelId == modelToBeDeleted.Id));

            if (hasSerialNumbers.Any())
            {
                return new JsonResult(new { success = false, message = "Model cannot be deleted because it has associated serial numbers" });
            }

            await _unitOfWork.Model.RemoveAsy(modelToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Model deleted successfully" });
        }
    }
}