using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using System.Security.Claims;

namespace RepairShop.Areas.User.Pages.TransactionHeaders
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

        //API CALLS for getting all THs in Json format for DataTables
        public async Task<JsonResult> OnGetAll()//The route is /User/TransactionHeaders/Index?handler=All
        {
            var THList = new List<TransactionHeader>();
            // Get the user's identity. Explanation: User.Identity contains
            // information about the currently logged-in user.
            var claimsIdentity = (ClaimsIdentity)User.Identity;

            // Get the user's role. Explanation: ClaimTypes.Role
            // is a standard claim type that represents the role of the user.
            var userRole = claimsIdentity.FindFirst(ClaimTypes.Role).Value;
            if(userRole == SD.Role_Admin)//If the user is an admin get all transactions
            {
                THList = (await _unitOfWork.TransactionHeader
                    .GetAllAsy(t => t.IsActive == true, includeProperties: "Client,User")).ToList();
            }
            else//If the user is not an admin get only their transactions
            {
                // Get the user's ID. Explanation: ClaimTypes.NameIdentifier is a
                // standard claim type that represents the unique identifier of the user.
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                THList = (await _unitOfWork.TransactionHeader
                    .GetAllAsy(t => t.IsActive == true && t.UserId == userId, includeProperties: "Client")).ToList();
            }
            
            return new JsonResult(new { data = THList });//We return JsonResult because we will call this method using AJAX
        }

        //API CALL for changing status from New to InProgress
        public async Task<IActionResult> OnGetChangeStatus(int? id)//The route is /User/TransactionHeaders/Index?handler=ChangeStatus&id=1
        {
            var THToBeChanged = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id);
            if (THToBeChanged == null)
            {
                return new JsonResult(new { success = false, message = "Error while changing status" });
            }

            THToBeChanged.Status = SD.Status_Job_InProgress;
            await _unitOfWork.TransactionHeader.UpdateAsy(THToBeChanged);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Status changed successfully" });
        }
        //API CALL for deleting a TH//Didnt use OnPostDelete because it needs the link to send a form and it causes issues with DataTables
        public async Task<IActionResult> OnGetDelete(int? id)//The route is /User/TransactionHeaders/Index?handler=Delete&id=1
        {
            var THToBeDeleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id);
            if (THToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }
            if (THToBeDeleted.Status != SD.Status_Job_New)
            {
                return new JsonResult(new { success = false, message = "You can only delete a new transaction" });
            }

            await _unitOfWork.TransactionHeader.RemoveAsy(THToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Transaction deleted successfully" });
        }

        //API CALL for canceling a TH
        public async Task<IActionResult> OnGetCancel(int? id)//The route is /User/TransactionHeaders/Index?handler=Cancel&id=1
        {
            var THToBeDeleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id);
            if (THToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while cancelling" });
            }

            THToBeDeleted.Status = SD.Status_Job_Cancelled;
            await _unitOfWork.TransactionHeader.UpdateAsy(THToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Transaction cancelled successfully" });
        }
    }
}
