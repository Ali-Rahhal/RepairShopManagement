using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.User.Pages.PreventiveMaintenanceRecords
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Add a property to hold client list for the dropdown
        [BindProperty]
        public List<SelectListItem> ClientList { get; set; } = new();

        [BindProperty]
        public long SelectedClientId { get; set; }

        public async Task OnGet(long? clientId)
        {
            // Populate client dropdown
            var clients = await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true, includeProperties: "ParentClient");

            ClientList = clients
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ParentClientId != null ? $"{c.ParentClient?.Name ?? "N/A"}{($" - {c.Name}")}" : $"{c.Name}"
                })
                .ToList();

            ClientList.Insert(0, new SelectListItem { Value = "0", Text = "-- Select Client --" });

            // If a client ID is passed, auto-select it
            if (clientId.HasValue && clientId.Value > 0)
            {
                SelectedClientId = clientId.Value;
            }
        }

        // Modify this method to accept client filter
        public async Task<JsonResult> OnGetAll(long clientId = 0)
        {
            var records = (await _unitOfWork.PreventiveMaintenanceRecord
                .GetAllAsy(pm => pm.ClientId == clientId && pm.IsActive == true, includeProperties: "SerialNumber,SerialNumber.Model,Client,User")).ToList();
            
            return new JsonResult(new { data = records });
        }

        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var recordToBeDeleted = await _unitOfWork.PreventiveMaintenanceRecord.GetAsy(o => o.Id == id && o.IsActive == true, includeProperties: "SerialNumber,Client,User");
            if (recordToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _unitOfWork.PreventiveMaintenanceRecord.RemoveAsy(recordToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Record deleted successfully" });
        }
    }
}