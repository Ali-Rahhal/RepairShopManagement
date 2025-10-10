using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Warranties
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
        public Warranty WarrantyForUpsert { get; set; }

        public IEnumerable<SelectListItem> SerialNumberList { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            WarrantyForUpsert = new Warranty
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddYears(1) // Default 1 year warranty
            };

            // Populate dropdowns
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                WarrantyForUpsert = await _unitOfWork.Warranty.GetAsy(
                    w => w.Id == id,
                    includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client"
                );

                if (WarrantyForUpsert == null)
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
                if (WarrantyForUpsert == null)
                {
                    return NotFound();
                }

                // Validate dates
                if (WarrantyForUpsert.EndDate <= WarrantyForUpsert.StartDate)
                {
                    ModelState.AddModelError("WarrantyForUpsert.EndDate", "End date must be after start date.");
                    await PopulateDropdowns();
                    return Page();
                }

                // Set status based on dates
                WarrantyForUpsert.Status = WarrantyForUpsert.EndDate < DateTime.Now ? "Expired" : "Active";

                // Check if serial number already has an active warranty
                var existingWarranty = await _unitOfWork.Warranty.GetAsy(
                        w => w.SerialNumberId == WarrantyForUpsert.SerialNumberId &&
                             w.IsActive == true &&
                             w.Status == "Active"
                    );
                if (existingWarranty != null)
                {
                    ModelState.AddModelError("WarrantyForUpsert.SerialNumberId", "This serial number already has an active warranty.");
                    await PopulateDropdowns();
                    return Page();
                }

                if (WarrantyForUpsert.Id == 0)
                {
                    await _unitOfWork.Warranty.AddAsy(WarrantyForUpsert);
                    TempData["success"] = "Warranty created successfully";
                }
                else
                {
                    await _unitOfWork.Warranty.UpdateAsy(WarrantyForUpsert);
                    TempData["success"] = "Warranty updated successfully";
                }

                await _unitOfWork.SaveAsy();
                return RedirectToPage("Index");
            }

            await PopulateDropdowns();
            return Page();
        }

        private async Task PopulateDropdowns()
        {
            // Populate Serial Numbers dropdown
            var serialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.IsActive == true,
                includeProperties: "Model,Client"
            ))
            .OrderBy(sn => sn.Value)
            .ToList();

            SerialNumberList = serialNumbers.Select(sn => new SelectListItem
            {
                Text = $"{sn.Value} - {sn.Model.Name} ({sn.Client.Name})",
                Value = sn.Id.ToString()
            });
        }
    }
}