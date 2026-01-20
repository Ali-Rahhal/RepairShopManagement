using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Security.Claims;

namespace RepairShop.Areas.Admin.Pages.Parts
{
    [Authorize(Roles = SD.Role_Admin)]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(IUnitOfWork unitOfWork, AuditLogService als, IHttpContextAccessor hca)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = als;
            _httpContextAccessor = hca;
        }

        public void OnGet()
        {
        }

        public async Task<JsonResult> OnGetAll(
            int draw,
            int start = 0,
            int length = 10,
            string? category = null)
        {
            var search = Request.Query["search[value]"].ToString();
            var orderColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
            var orderDir = Request.Query["order[0][dir]"].FirstOrDefault() ?? "asc";

            // Base query
            var query = await _unitOfWork.Part.GetQueryableAsy(p => p.IsActive);

            var recordsTotal = await query.CountAsync();

            // Global search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Category ?? "").ToLower().Contains(search));
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                if (category == "Uncategorized")
                    query = query.Where(p => string.IsNullOrEmpty(p.Category));
                else
                    query = query.Where(p => p.Category == category);
            }

            var recordsFiltered = await query.CountAsync();

            // Server-side ordering
            query = orderColumnIndex switch
            {
                "0" => orderDir == "asc" ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                "1" => orderDir == "asc" ? query.OrderBy(p => p.Category) : query.OrderByDescending(p => p.Category),
                "2" => orderDir == "asc" ? query.OrderBy(p => p.Quantity) : query.OrderByDescending(p => p.Quantity),
                "3" => orderDir == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.Category) // default
            };

            var data = await query
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    category = string.IsNullOrEmpty(p.Category) ? "Uncategorized" : p.Category,
                    quantity = p.Quantity,
                    price = p.Price
                })
                .ToListAsync();

            return new JsonResult(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var partToBeDeleted = await _unitOfWork.Part.GetAsy(p => p.Id == id && p.IsActive == true);
            if (partToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // check if part is referenced in transaction body
            var isUsed = (await _unitOfWork.TransactionBody
                .GetAllAsy(tb => tb.IsActive == true && tb.PartId == partToBeDeleted.Id))
                .Any();

            if (isUsed)
            {
                return new JsonResult(new { success = false, message = "Part cannot be deleted because it is used in a transaction" });
            }

            await _unitOfWork.PartStockHistory.AddAsy(new PartStockHistory
            {
                UserId = _httpContextAccessor.HttpContext?.User?
                            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? null,
                PartId = partToBeDeleted.Id,
                QuantityChange = -partToBeDeleted.Quantity,
                QuantityAfter = 0,
                Reason = "Part deleted (stock removed)",
                CreatedDate = DateTime.Now
            });
            await _unitOfWork.SaveAsy();
            
            await _unitOfWork.Part.RemoveAsy(partToBeDeleted);
            await _unitOfWork.SaveAsy();
            await _auditLogService.AddLogAsy(SD.Action_Delete, SD.Entity_Part, partToBeDeleted.Id);
            return new JsonResult(new { success = true, message = "Part deleted successfully" });
        }

        public async Task<JsonResult> OnGetCategories()
        {
            var categories = (await _unitOfWork.Part
                .GetAllAsy(p => p.IsActive))
                .Select(p => p.Category ?? "Uncategorized")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return new JsonResult(categories);
        }
    }
}
