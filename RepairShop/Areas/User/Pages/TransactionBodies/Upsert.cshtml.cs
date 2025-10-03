using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services.Helper;

namespace RepairShop.Areas.User.Pages.TransactionBodies
{
    [Authorize]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpsertModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public TransactionBody tbForUpsert { get; set; }

        public List<string> Categories { get; set; }

        public async Task<IActionResult> OnGet(int? id, int? headerId)
        {
            // Load categories for dropdown
            await LoadCategories();

            if (id == null || id == 0)
            {
                // Create mode
                tbForUpsert = new TransactionBody
                {
                    TransactionHeaderId = headerId.Value
                };
                return Page();
            }
            else
            {
                // Edit mode - only load the existing record
                tbForUpsert = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == id);
                if (tbForUpsert == null)
                {
                    return NotFound();
                }
                return Page();
            }
        }

        // AJAX handler to get parts by category
        public async Task<IActionResult> OnGetPartsByCategory(string category)
        {
            var parts = (await _unitOfWork.Part.GetAllAsy())
                .Where(p => p.IsActive && p.Quantity > 0 && p.Category == category)
                .Select(p => new { p.Id, p.Name })
                .OrderBy(p => p.Name)
                .ToList();

            return new JsonResult(parts);
        }

        // AJAX handler to get part details
        public async Task<IActionResult> OnGetPartDetails(int id)
        {
            var part = await _unitOfWork.Part.GetAsy(p => p.Id == id && p.IsActive);
            if (part == null)
            {
                return new JsonResult(null);
            }

            return new JsonResult(new
            {
                part.Id,
                part.Name,
                part.Quantity,
                part.Price
            });
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                if (tbForUpsert == null)
                {
                    return NotFound();
                }

                if (tbForUpsert.Id == 0)
                {
                    // CREATE MODE
                    // If status is Pending Replace and a part is selected, decrement inventory
                    if (tbForUpsert.Status == SD.Status_Part_Pending_Replace &&
                        tbForUpsert.PartId.HasValue && tbForUpsert.PartId.Value > 0)
                    {
                        var selectedPart = await _unitOfWork.Part.GetAsy(p => p.Id == tbForUpsert.PartId.Value);
                        if (selectedPart != null && selectedPart.Quantity > 0)
                        {
                            // Decrement part quantity
                            selectedPart.Quantity--;
                            await _unitOfWork.Part.UpdateAsy(selectedPart);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Selected replacement part is out of stock.");
                            await LoadCategories();
                            return Page();
                        }
                    }

                    await _unitOfWork.TransactionBody.AddAsy(tbForUpsert);

                    await _unitOfWork.SaveAsy();
                    TempData["success"] = "Part Added successfully";
                }
                else
                {
                    // EDIT MODE - Only update PartName, ignore other changes
                    var existingTransactionBody = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == tbForUpsert.Id);
                    if (existingTransactionBody == null)
                    {
                        return NotFound();
                    }

                    // Only update the PartName, preserve everything else
                    existingTransactionBody.BrokenPartName = tbForUpsert.BrokenPartName;

                    await _unitOfWork.TransactionBody.UpdateAsy(existingTransactionBody);
                    await _unitOfWork.SaveAsy();
                    TempData["success"] = "Part name updated successfully";
                }

                return RedirectToPage("Index", new { HeaderId = tbForUpsert.TransactionHeaderId });
            }

            await LoadCategories();
            return Page();
        }

        private async Task LoadCategories()
        {
            Categories = (await _unitOfWork.Part.GetAllAsy())
                .Where(p => p.IsActive && p.Quantity > 0)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}