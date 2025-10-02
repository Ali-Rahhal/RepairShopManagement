using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Parts
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
        public Part PartForUpsert { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            PartForUpsert = new Part();
            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                PartForUpsert = await _unitOfWork.Part.GetAsy(p => p.Id == id);
                if (PartForUpsert == null)
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
                if (PartForUpsert == null)
                {
                    return NotFound();
                }

                if (PartForUpsert.Id == 0)
                {
                    await _unitOfWork.Part.AddAsy(PartForUpsert);
                    TempData["success"] = "Part created successfully";
                }
                else
                {
                    await _unitOfWork.Part.UpdateAsy(PartForUpsert);
                    TempData["success"] = "Part updated successfully";
                }

                await _unitOfWork.SaveAsy();
                return RedirectToPage("Index");
            }
            return Page();
        }
    }
}
