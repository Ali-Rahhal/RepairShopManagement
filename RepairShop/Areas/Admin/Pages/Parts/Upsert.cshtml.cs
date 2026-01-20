using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RepairShop.Areas.Admin.Pages.Parts
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpsertModel(IUnitOfWork unitOfWork, AuditLogService als, IHttpContextAccessor hca)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = als;
            _httpContextAccessor = hca;
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
                PartForUpsert = await _unitOfWork.Part.GetAsy(p => p.Id == id && p.IsActive == true);
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

                var normalized = NormalizeCategory(PartForUpsert.Category);
                if (normalized == string.Empty)
                {
                    ModelState.AddModelError("PartForUpsert.Category", "Category cannot be empty.");
                    return Page();
                }
                PartForUpsert.Category = normalized;

                if (PartForUpsert.Id == 0)
                {
                    await _unitOfWork.Part.AddAsy(PartForUpsert);
                    await _unitOfWork.SaveAsy();

                    // After part is created and saved
                    await _unitOfWork.PartStockHistory.AddAsy(new PartStockHistory
                    {   
                        UserId = _httpContextAccessor.HttpContext?.User?
                                    .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? null,
                        PartId = PartForUpsert.Id,
                        QuantityChange = PartForUpsert.Quantity,   // initial stock
                        QuantityAfter = PartForUpsert.Quantity,
                        Reason = "Initial stock on part creation",
                        CreatedDate = DateTime.Now
                    });

                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Create, SD.Entity_Part, PartForUpsert.Id);
                    TempData["success"] = "Part created successfully";
                }
                else
                {
                    var partFromDb = await _unitOfWork.Part.GetAsy(p => p.Id == PartForUpsert.Id && p.IsActive == true);
                    if (partFromDb == null) return NotFound();

                    var quantityDiff = PartForUpsert.Quantity - partFromDb.Quantity;

                    // Make a copy of the old part
                    var oldPart = new Part
                    {
                        Id = partFromDb.Id,
                        Code = partFromDb.Code,
                        Name = partFromDb.Name,
                        Category = partFromDb.Category,
                        Quantity = partFromDb.Quantity,
                        Price = partFromDb.Price,
                        IsActive = partFromDb.IsActive
                    };
                    partFromDb.Name = PartForUpsert.Name;
                    partFromDb.Category = PartForUpsert.Category;
                    partFromDb.Quantity = PartForUpsert.Quantity;
                    partFromDb.Price = PartForUpsert.Price;
                    await _unitOfWork.Part.UpdateAsy(partFromDb);
                    await _unitOfWork.SaveAsy();

                    if (quantityDiff != 0)
                    {
                        var history = new PartStockHistory
                        {
                            UserId = _httpContextAccessor.HttpContext?.User?
                                        .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? null,
                            PartId = partFromDb.Id,
                            QuantityChange = quantityDiff, // +ve or -ve
                            QuantityAfter = partFromDb.Quantity,
                            Reason = quantityDiff > 0
                                ? $"Manual stock increase by admin"
                                : $"Manual stock decrease by admin",
                            CreatedDate = DateTime.Now
                        };

                        await _unitOfWork.PartStockHistory.AddAsy(history);
                        await _unitOfWork.SaveAsy();
                    }

                    await _auditLogService.AddLogAsy<Part>(SD.Action_Update, SD.Entity_Part, PartForUpsert.Id, oldPart);
                    TempData["success"] = "Part updated successfully";
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
