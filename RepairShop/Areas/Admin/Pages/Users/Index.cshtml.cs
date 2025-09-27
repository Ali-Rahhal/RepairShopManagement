using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task OnGet()
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

            await _unitOfWork.AppUser.RemoveAsy(userToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "User deleted successfully" });
        }
    }
}
