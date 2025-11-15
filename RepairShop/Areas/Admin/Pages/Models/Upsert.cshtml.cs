using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Models
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
        public Model ModelForUpsert { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            ModelForUpsert = new Model();
            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                ModelForUpsert = await _unitOfWork.Model.GetAsy(m => m.Id == id && m.IsActive == true);
                if (ModelForUpsert == null)
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
                if (ModelForUpsert == null)
                {
                    return NotFound();
                }

                if (ModelForUpsert.Id == 0)
                {
                    await _unitOfWork.Model.AddAsy(ModelForUpsert);
                    TempData["success"] = "Model created successfully";
                }
                else
                {
                    await _unitOfWork.Model.UpdateAsy(ModelForUpsert);
                    TempData["success"] = "Model updated successfully";
                }

                await _unitOfWork.SaveAsy();
                return RedirectToPage("Index");
            }
            return Page();
        }
    }
}