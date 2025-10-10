using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.MaintenanceContracts
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpsertModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public MaintenanceContract MaintenanceContractForUpsert { get; set; }

        public IEnumerable<SelectListItem> ClientList { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            MaintenanceContractForUpsert = new MaintenanceContract
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddYears(1) // Default 1 year contract
            };

            // Populate dropdowns
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                MaintenanceContractForUpsert = await _unitOfWork.MaintenanceContract.GetAsy(
                    mc => mc.Id == id,
                    includeProperties: "Client"
                );

                if (MaintenanceContractForUpsert == null)
                {
                    return NotFound();
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                if (MaintenanceContractForUpsert == null)
                {
                    return NotFound();
                }

                // Validate dates
                if (MaintenanceContractForUpsert.EndDate <= MaintenanceContractForUpsert.StartDate)
                {
                    ModelState.AddModelError("MaintenanceContractForUpsert.EndDate", "End date must be after start date.");
                    await PopulateDropdowns();
                    return Page();
                }

                // Auto-set status based on current date
                MaintenanceContractForUpsert.Status = MaintenanceContractForUpsert.EndDate < DateTime.Now ? "Expired" : "Active";

                if (MaintenanceContractForUpsert.Id == 0)
                {
                    await _unitOfWork.MaintenanceContract.AddAsy(MaintenanceContractForUpsert);
                    TempData["success"] = "Maintenance contract created successfully";
                }
                else
                {
                    await _unitOfWork.MaintenanceContract.UpdateAsy(MaintenanceContractForUpsert);
                    TempData["success"] = "Maintenance contract updated successfully";
                }

                await _unitOfWork.SaveAsy();
                return RedirectToPage("Index");
            }

            await PopulateDropdowns();
            return Page();
        }

        private async Task PopulateDropdowns()
        {
            // Populate Clients dropdown
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true))
                .OrderBy(c => c.Name)
                .ToList();

            ClientList = clients.Select(c => new SelectListItem
            {
                Text = $"{c.Name}",
                Value = c.Id.ToString()
            });
        }
    }
}