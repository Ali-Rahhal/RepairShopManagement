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

        //AJAX CALLS for getting all THs in Json format for DataTables
        public async Task<JsonResult> OnGetAll()//The route is /User/TransactionHeaders/Index?handler=All
        {
            var THList = new List<TransactionHeader>();
            // Get the user's identity. Explanation: User.Identity contains
            // information about the currently logged-in user.
            var claimsIdentity = (ClaimsIdentity)User.Identity;

            // Get the user's role. Explanation: ClaimTypes.Role
            // is a standard claim type that represents the role of the user.
            var userRole = claimsIdentity.FindFirst(ClaimTypes.Role).Value;
            if (userRole == SD.Role_Admin)//If the user is an admin get all transactions
            {
                THList = (await _unitOfWork.TransactionHeader
                    .GetAllAsy(t => t.IsActive == true,
                    includeProperties: "Client,User,DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model")).ToList();
            }
            else//If the user is not an admin get only their transactions
            {
                // Get the user's ID. Explanation: ClaimTypes.NameIdentifier is a
                // standard claim type that represents the unique identifier of the user.
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                THList = (await _unitOfWork.TransactionHeader
                    .GetAllAsy(t => t.IsActive == true && t.UserId == userId,
                    includeProperties: "Client,DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model")).ToList();
            }

            // Format the data for better display
            var formattedData = THList.Select(t => new
            {
                id = t.Id,
                user = t.User != null ? new { userName = t.User.UserName } : null,
                model = t.DefectiveUnit?.SerialNumber?.Model?.Name ?? "N/A",
                serialNumber = t.DefectiveUnit?.SerialNumber?.Value ?? "N/A",
                status = t.Status,
                client = t.Client != null ? new { name = t.Client.Name } : null,
                createdDate = t.CreatedDate,
                description = t.Description,
                defectiveUnitId = t.DefectiveUnitId
            });

            return new JsonResult(new { data = formattedData });
        }

        //AJAX CALL for changing status from New to InProgress
        public async Task<IActionResult> OnGetChangeStatus(int? id)//The route is /User/TransactionHeaders/Index?handler=ChangeStatus&id=1
        {
            var THToBeChanged = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id, includeProperties: "DefectiveUnit");
            if (THToBeChanged == null)
            {
                return new JsonResult(new { success = false, message = "Error while changing status" });
            }

            THToBeChanged.Status = SD.Status_Job_InProgress;
            THToBeChanged.DefectiveUnit.Status = SD.Status_Part_Pending_Repair;
            await _unitOfWork.TransactionHeader.UpdateAsy(THToBeChanged);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Status changed successfully" });
        }

        //AJAX CALL for deleting a TH//Didnt use OnPostDelete because it needs the link to send a form and it causes issues with DataTables
        public async Task<IActionResult> OnGetDelete(int? id)//The route is /User/TransactionHeaders/Index?handler=Delete&id=1
        {
            var THToBeDeleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id);
            if (THToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // Only new transactions can be deleted
            if (THToBeDeleted.Status != SD.Status_Job_New)
            {
                return new JsonResult(new { success = false, message = "You can only delete a new transaction" });
            }

            await _unitOfWork.TransactionHeader.RemoveAsy(THToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Transaction deleted successfully" });
        }

        //AJAX CALL for completing the transaction
        public async Task<IActionResult> OnGetCompleteStatus(int? id)//The route is /User/TransactionHeaders/Index?handler=CompleteStatus&id=1
        {
            var THToBeCompleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id, includeProperties: "BrokenParts,DefectiveUnit");
            if (THToBeCompleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while completing" });
            }

            //Check if there are any pending parts
            var pendingParts = THToBeCompleted.BrokenParts
                .Where(o => (o.Status == SD.Status_Part_Pending_Repair
                                || o.Status == SD.Status_Part_Pending_Replace
                                || o.Status == SD.Status_Part_Waiting_Part) && o.IsActive == true).ToList();
            if (pendingParts.Count > 0)
            {
                return new JsonResult(new { success = false, message = "You have pending parts" });
            }

            //check if there are any active parts
            var partCount = THToBeCompleted.BrokenParts.Count(o => o.IsActive == true);
            if (partCount == 0)
            {
                return new JsonResult(new { success = false, message = "You have no parts to complete" });
            }

            //check if there are non-repairable parts
            var nonRepairableParts = THToBeCompleted.BrokenParts.Count(o => o.Status == SD.Status_Part_NotReplaceable);
            if (nonRepairableParts > 0)
            {
                THToBeCompleted.Status = SD.Status_Job_OutOfService;
                THToBeCompleted.DefectiveUnit.Status = SD.Status_DU_OutOfService;
                THToBeCompleted.DefectiveUnit.ResolvedDate = DateTime.Now;
                await _unitOfWork.TransactionHeader.UpdateAsy(THToBeCompleted);
                await _unitOfWork.SaveAsy();
                return new JsonResult(new { success = true, message = "Transaction is out of service" });
            }
            else
            {
                THToBeCompleted.Status = SD.Status_Job_Completed;
                await _unitOfWork.TransactionHeader.UpdateAsy(THToBeCompleted);
                THToBeCompleted.DefectiveUnit.Status = SD.Status_DU_Fixed;
                THToBeCompleted.DefectiveUnit.ResolvedDate = DateTime.Now;
                await _unitOfWork.SaveAsy();
                return new JsonResult(new { success = true, message = "Transaction completed successfully" });
            }
        }
    }
}