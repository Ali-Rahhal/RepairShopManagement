using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            var claimsIdentity = (ClaimsIdentity)User.Identity;// Get the user's identity. Explanation: User.Identity contains
                                                               // information about the currently logged-in user.
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;// Get the user's ID. Explanation: ClaimTypes.NameIdentifier is a
                                                                                   // standard claim type that represents the unique identifier of the user.


            var THList = (await _unitOfWork.TransactionHeader.GetAllAsy(t => t.IsActive == true && t.UserId == userId, includeProperties: "Client")).ToList();
            return new JsonResult(new { data = THList });//We return JsonResult because we will call this method using AJAX
        }

        //API CALL for deleting a TH//Didnt use OnPostDelete because it needs the link to send a form and it causes issues with DataTables
        public async Task<IActionResult> OnGetDelete(int? id)//The route is /User/TransactionHeaders/Index?handler=Delete&id=1
        {
            var THToBeDeleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id);
            if (THToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _unitOfWork.TransactionHeader.RemoveAsy(THToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Transaction deleted successfully" });
        }
    }
}
