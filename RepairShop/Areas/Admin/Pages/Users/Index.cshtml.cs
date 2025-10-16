using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Users
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

        //API CALL for getting all users in Json format for DataTables
        public async Task<JsonResult> OnGetAll()//The route is /Admin/Users/Index?handler=All
        {
            var UserList = (await _unitOfWork.AppUser.GetAllAsy(o => o.IsActive == true)).ToList();
            return new JsonResult(new { data = UserList });//We return JsonResult because we will call this method using AJAX
        }

        //API CALL for deleting a user//Didnt use OnPostDelete because it needs the link to send a form and it causes issues with DataTables
        public async Task<IActionResult> OnGetDelete(string? id)//The route is /Admin/Companies/Index?handler=Delete&id=1
        {
            var userToBeDeleted =  await _unitOfWork.AppUser.GetAsy(o => (o.Id).Equals(id));
            if (userToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            //Check if client has related transactions
            var transactionsRelatedToUser = (await _unitOfWork.TransactionHeader.GetAllAsy(t => t.IsActive == true && t.UserId == userToBeDeleted.Id));
            if (transactionsRelatedToUser.Any())
            {
                return new JsonResult(new { success = false, message = "User cannot be deleted because it has related transactions" });
            }

            //Check if user is last admin
            var admins = await _unitOfWork.AppUser.GetAllAsy(o => o.Role == SD.Role_Admin && o.IsActive == true);
            if (admins.Count() == 1 && admins.First().Id == userToBeDeleted.Id)
            {
                return new JsonResult(new { success = false, message = "Cannot delete last admin" });
            }

            await _unitOfWork.AppUser.RemoveAsy(userToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "User deleted successfully" });
        }
    }
}
