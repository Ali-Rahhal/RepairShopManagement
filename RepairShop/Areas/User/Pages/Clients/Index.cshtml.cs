using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.User.Pages.Clients
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

        //API CALLS for getting all clients in Json format for DataTables
        public async Task<JsonResult> OnGetAll()//The route is /User/Clients/Index?handler=All
        {
            var clientList = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true)).ToList();
            return new JsonResult(new { data = clientList });//We return JsonResult because we will call this method using AJAX
        }

        //API CALL for deleting a client//Didnt use OnPostDelete because it needs the link to send a form and it causes issues with DataTables
        public async Task<IActionResult> OnGetDelete(int? id)//The route is /User/Clients/Index?handler=Delete&id=1
        {
            var clientToBeDeleted = await _unitOfWork.Client.GetAsy(o => o.Id == id);
            if (clientToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _unitOfWork.Client.RemoveAsy(clientToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Client deleted successfully" });
        }
    }
}
