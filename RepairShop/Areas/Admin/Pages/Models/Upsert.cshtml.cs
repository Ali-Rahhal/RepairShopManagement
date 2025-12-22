using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Text.RegularExpressions;

namespace RepairShop.Areas.Admin.Pages.Models
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuditLogService _auditLogService;

        public UpsertModel(IUnitOfWork unitOfWork, AuditLogService als)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = als;
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

                var normalized = NormalizeCategory(ModelForUpsert.Category);
                if (normalized == string.Empty)
                {
                    ModelState.AddModelError("ModelForUpsert.Category", "Category cannot be empty.");
                    return Page();
                }
                ModelForUpsert.Category = normalized;

                //// Uniqueness check
                //bool exists = (await _unitOfWork.Model.GetAllAsy(m => m.Id != ModelForUpsert.Id && m.Category == normalized && m.IsActive == true)).Any();

                //if (exists)
                //{
                //    ModelState.AddModelError("Category", "Category already exists or too similar to an existing category.");
                //    return Page();
                //}

                if (ModelForUpsert.Id == 0)
                {
                    await _unitOfWork.Model.AddAsy(ModelForUpsert);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Create, SD.Entity_Model, ModelForUpsert.Id);
                    TempData["success"] = "Model created successfully";
                }
                else
                {
                    var modelFromDb = await _unitOfWork.Model.GetAsy(m => m.Id == ModelForUpsert.Id && m.IsActive == true);
                    if (modelFromDb == null) return NotFound();
                    modelFromDb.Name = ModelForUpsert.Name;
                    modelFromDb.Category = ModelForUpsert.Category;
                    await _unitOfWork.Model.UpdateAsy(modelFromDb);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Update, SD.Entity_Model, modelFromDb.Id);
                    TempData["success"] = "Model updated successfully";
                }

                return RedirectToPage("Index");
            }
            return Page();
        }

        private static string NormalizeCategory(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove special characters and spaces using regex
            input = Regex.Replace(input, @"[^a-zA-Z0-9]", "");

            input = input.Trim().ToLower();

            // Capitalize first letter
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}