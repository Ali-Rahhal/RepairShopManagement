using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using System.Security.Claims;

namespace RepairShop.Areas.User.Pages.TransactionBodies
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty(SupportsGet = true)]
        public int HeaderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string HeaderStatus { get; set; }

        public void OnGet(int HeaderId)
        {
            this.HeaderId = HeaderId;
            HeaderStatus = _unitOfWork.TransactionHeader.GetAsy(o => o.Id == HeaderId).Result.Status;
        }

        //API CALLS for getting all TBs in Json format for DataTables
        public async Task<JsonResult> OnGetAll(int headerId)//The route is /User/TransactionBodies/Index?handler=All&headerId=1
        {
            var TBList = (await _unitOfWork.TransactionBody.GetAllAsy(t => t.IsActive == true && t.TransactionHeaderId == headerId, includeProperties: "Part")).ToList();
            return new JsonResult(new { data = TBList });//We return JsonResult because we will call this method using AJAX
        }

        //API CALL for changing TB status
        public async Task<IActionResult> OnGetStatus(int? id, int? choice)//The route is /User/TransactionBodies/Index?handler=Status&id=1
        {
            var TB = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == id);
            if (TB == null)
            {
                return new JsonResult(new { success = false, message = "Error while changing status" });
            }

            // Handle different choices
            switch (choice)
            {
                case 0: // Not Repairable
                    TB.Status = SD.Status_Part_NotRepairable;
                    break;
                case 1: // Fixed
                    TB.Status = SD.Status_Part_Fixed;
                    break;
                case 2: // Not Replaceable
                    TB.Status = SD.Status_Part_NotReplaceable;

                    // If not replaceable and a part was selected, increment inventory
                    if (TB.PartId.HasValue)
                    {
                        var replacementPart = await _unitOfWork.Part.GetAsy(p => p.Id == TB.PartId.Value);
                        if (replacementPart != null && replacementPart.Quantity >= 0)
                        {
                            replacementPart.Quantity++;
                            await _unitOfWork.Part.UpdateAsy(replacementPart);
                        }
                    }
                    break;
                case 3: // Replaced
                    TB.Status = SD.Status_Part_Replaced;
                    break;
                default:
                    return new JsonResult(new { success = false, message = "Invalid choice" });
            }

            await _unitOfWork.TransactionBody.UpdateAsy(TB);
            await _unitOfWork.SaveAsy();

            return new JsonResult(new { success = true, message = "Status changed successfully" });
        }

        //API CALL for deleting a TB
        public async Task<IActionResult> OnGetDelete(int? id)//The route is /User/TransactionBodies/Index?handler=Delete&id=1
        {
            var TBToBeDeleted = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == id);
            if (TBToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }
            if (TBToBeDeleted.Status != SD.Status_Part_Pending_Repair && TBToBeDeleted.Status != SD.Status_Part_Pending_Replace)
            {
                return new JsonResult(new { success = false, message = "You can only delete pending parts" });
            }
            if (TBToBeDeleted.Status == SD.Status_Part_Pending_Replace)
            {
                // If not replaceable and a part was selected, increment inventory
                if (TBToBeDeleted.PartId.HasValue)
                {
                    var replacementPart = await _unitOfWork.Part.GetAsy(p => p.Id == TBToBeDeleted.PartId.Value);
                    if (replacementPart != null && replacementPart.Quantity >= 0)
                    {
                        replacementPart.Quantity++;
                        await _unitOfWork.Part.UpdateAsy(replacementPart);
                    }
                }
            }

            await _unitOfWork.TransactionBody.RemoveAsy(TBToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Part deleted successfully" });
        }
    }
}
