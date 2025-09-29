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
        private readonly AppDbContext _db;

        public IndexModel(IUnitOfWork unitOfWork, AppDbContext db)
        {
            _unitOfWork = unitOfWork;
            _db = db;
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
            var TBList = (await _unitOfWork.TransactionBody.GetAllAsy(t => t.IsActive == true && t.TransactionHeaderId == headerId)).ToList();
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

            if(choice == 1)
            {
                TB.Status = SD.Status_Part_Fixed;
            }
            else
            {
                TB.Status = SD.Status_Part_NotRepairable;
            }
            
            await _unitOfWork.TransactionBody.UpdateAsy(TB);
            await _unitOfWork.SaveAsy();

            _db.Entry(TB).State = EntityState.Detached;
            //Updates the status of the TransactionHeader if all its Parts are not in a 'pending' status.
            TransactionHeader Header = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == TB.TransactionHeaderId, includeProperties: "Parts");
            var unFinishedParts = Header.Parts.Where(p => p.Status == SD.Status_Part_Pending && p.IsActive == true);
            if (unFinishedParts.Count() == 0)
            {
                Header.Status = SD.Status_Job_Completed;
                await _unitOfWork.TransactionHeader.UpdateAsy(Header);
                await _unitOfWork.SaveAsy();
            }

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

            await _unitOfWork.TransactionBody.RemoveAsy(TBToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Part deleted successfully" });
        }
    }
}
