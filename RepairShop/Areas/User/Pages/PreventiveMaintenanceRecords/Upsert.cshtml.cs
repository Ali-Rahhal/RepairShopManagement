using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Repository.IRepository;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Areas.User.Pages.PreventiveMaintenanceRecords
{
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpsertModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public PreventiveMaintenanceRecord Record { get; set; }

        // This holds the serial NUMBER string typed by the user (not id)
        [BindProperty]
        [Required(ErrorMessage = "Serial Number is required")]
        public string SerialNumberValue { get; set; }

        // Client ID comes from query string (from Index filter)
        [BindProperty(SupportsGet = true)]
        public long clientId { get; set; }

        public async Task<IActionResult> OnGet(long? id, long clientId)
        {
            this.clientId = clientId;

            if (id == null || id == 0)
            {
                // Creating new record
                Record = new PreventiveMaintenanceRecord();
                Record.ClientId = clientId;
                Record.CheckupDate = DateTime.Today;
            }
            else
            {
                // Editing existing record
                Record = await _unitOfWork.PreventiveMaintenanceRecord.GetAsy(
                    x => x.Id == id && x.IsActive == true,
                    includeProperties: "SerialNumber");

                if (Record == null) return NotFound();

                SerialNumberValue = Record.SerialNumber.Value;
                this.clientId = Record.ClientId;
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            // Validate Serial Number exists
            var serial = await _unitOfWork.SerialNumber.GetAsy(
                s => s.Value == SerialNumberValue && s.ClientId == clientId);

            if (serial == null)
            {
                ModelState.AddModelError("SerialNumberValue", "Serial number not found for this client.");
            }
            else
            {
                Record.SerialNumberId = serial.Id;
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // NEW record
            if (Record.Id == 0)
            {
                Record.ClientId = clientId;
                Record.ModifiedDate = DateTime.Now;
                await _unitOfWork.PreventiveMaintenanceRecord.AddAsy(Record);
                TempData["success"] = "Preventive maintenance record created successfully";
            }
            else
            {
                var recordFromDb = await _unitOfWork.PreventiveMaintenanceRecord.GetAsy(x => x.Id == Record.Id && x.IsActive == true);
                if (recordFromDb == null) return NotFound();
                recordFromDb.DepartmentLocation = Record.DepartmentLocation;
                recordFromDb.SerialNumberId = Record.SerialNumberId;
                recordFromDb.IpAddress = Record.IpAddress;
                recordFromDb.PurchaseDate = Record.PurchaseDate;
                recordFromDb.Problem = Record.Problem;
                recordFromDb.Solution = Record.Solution;
                recordFromDb.CheckupDate = Record.CheckupDate;
                recordFromDb.Comment = Record.Comment;
                recordFromDb.ModifiedDate = DateTime.Now;
                await _unitOfWork.PreventiveMaintenanceRecord.UpdateAsy(recordFromDb);
                TempData["success"] = "Preventive maintenance record updated successfully";
            }

            await _unitOfWork.SaveAsy();
            return RedirectToPage("Index", new { clientId = clientId });
        }
    }
}
