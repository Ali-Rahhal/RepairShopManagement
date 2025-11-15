using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Parts
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
            var partList = (await _unitOfWork.Part.GetAllAsy(p => p.IsActive == true)).ToList();
            return new JsonResult(new { data = partList });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var partToBeDeleted = await _unitOfWork.Part.GetAsy(p => p.Id == id && p.IsActive == true);
            if (partToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // check if part is referenced in transaction body
            var isUsed = (await _unitOfWork.TransactionBody
                .GetAllAsy(tb => tb.IsActive == true && tb.PartId == partToBeDeleted.Id))
                .Any();

            if (isUsed)
            {
                return new JsonResult(new { success = false, message = "Part cannot be deleted because it is used in a transaction" });
            }

            await _unitOfWork.Part.RemoveAsy(partToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Part deleted successfully" });
        }
    }
}
