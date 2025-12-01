using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.ComponentModel.DataAnnotations;

namespace RepairShop.Areas.Admin.Pages.MaintenanceContracts
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMaintenanceContractService _mcService;

        public UpsertModel(IUnitOfWork unitOfWork, IMaintenanceContractService mcService)
        {
            _unitOfWork = unitOfWork;
            _mcService = mcService;
        }

        [BindProperty]
        public MaintenanceContract MaintenanceContractForUpsert { get; set; }

        public IEnumerable<SelectListItem> ClientList { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            MaintenanceContractForUpsert = new MaintenanceContract
            {
                StartDate = DateTime.Now.Date,
                EndDate = DateTime.Now.Date.AddYears(1)
            };

            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                return Page();
            }

            var mc = await _unitOfWork.MaintenanceContract.GetAsy(mc => mc.Id == id && mc.IsActive, includeProperties: "Client");
            if (mc == null)
                return NotFound();

            MaintenanceContractForUpsert = mc;
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return Page();
            }

            if (MaintenanceContractForUpsert.EndDate <= MaintenanceContractForUpsert.StartDate)
            {
                ModelState.AddModelError("MaintenanceContractForUpsert.EndDate", "End date must be after start date.");
                await PopulateDropdowns();
                return Page();
            }

            // Check if this maintenance contract has serial numbers of another client before allowing to change client
            var mcSerialNumber = await _unitOfWork.SerialNumber.GetAsy(s => s.MaintenanceContractId == MaintenanceContractForUpsert.Id && s.IsActive == true);
            if (mcSerialNumber != null && mcSerialNumber.ClientId != MaintenanceContractForUpsert.ClientId)
            {
                ModelState.AddModelError("MaintenanceContractForUpsert.ClientId", "Client cannot be changed if serial numbers of another client are assigned to this maintenance contract.");
                await PopulateDropdowns();
                return Page();
            }


            MaintenanceContractForUpsert.Status = MaintenanceContractForUpsert.EndDate < DateTime.Now ? "Expired" : "Active";

            bool isNew = MaintenanceContractForUpsert.Id == 0;

            if (isNew)
            {
                await _unitOfWork.MaintenanceContract.AddAsy(MaintenanceContractForUpsert);
                await _unitOfWork.SaveAsy();
                TempData["success"] = "Maintenance contract created successfully";

                // After creating, redirect to assign serial numbers page
                return RedirectToPage("AssignSerialNumbers", new { id = MaintenanceContractForUpsert.Id });
            }
            else
            {
                var mcFromDb = await _unitOfWork.MaintenanceContract.GetAsy(m => m.Id == MaintenanceContractForUpsert.Id && m.IsActive == true);
                if (mcFromDb == null) return NotFound();
                mcFromDb.StartDate = MaintenanceContractForUpsert.StartDate;
                mcFromDb.EndDate = MaintenanceContractForUpsert.EndDate;
                mcFromDb.ClientId = MaintenanceContractForUpsert.ClientId;
                mcFromDb.Status = MaintenanceContractForUpsert.Status;
                await _unitOfWork.MaintenanceContract.UpdateAsy(mcFromDb);
                await _unitOfWork.SaveAsy();
                TempData["success"] = "Maintenance contract updated successfully";

                // Stay on upsert; user can click assign
                await PopulateDropdowns();
                return RedirectToPage("Upsert", new { id = MaintenanceContractForUpsert.Id });
            }
        }

        private async Task PopulateDropdowns()
        {
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive)).OrderBy(c => c.Name).ToList();
            ClientList = clients.Select(c => new SelectListItem { Text = $"{c.Name}{(c.Branch != null ? $" - {c.Branch}" : "")}", Value = c.Id.ToString() });
        }
    }
}