using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.Admin.Pages.Models
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuditLogService _auditLogService;

        public IndexModel(IUnitOfWork unitOfWork, AuditLogService als)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = als;
        }

        public void OnGet()
        {
        }

        // ✅ SERVER-SIDE PAGINATION + FILTERING
        public async Task<JsonResult> OnGetAll(
            int draw,
            int start = 0,
            int length = 10,
            string? category = null)
        {
            var searchValue = Request.Query["search[value]"].ToString();
            var orderColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
            var orderDir = Request.Query["order[0][dir]"].FirstOrDefault() ?? "asc"; // default asc

            var query = await _unitOfWork.Model
                .GetQueryableAsy(m => m.IsActive);

            var recordsTotal = await query.CountAsync();

            // 🔍 Global search
            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                searchValue = searchValue.ToLower();
                query = query.Where(m =>
                    m.Name.Contains(searchValue) ||
                    m.Category.Contains(searchValue));
            }

            // 🏷️ Category filter
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                if (category == "Uncategorized")
                {
                    query = query.Where(m => m.Category == null || m.Category == "");
                }
                else
                {
                    query = query.Where(m => m.Category == category);
                }
            }

            var recordsFiltered = query.Count();

            // 🔧 Server-side ordering
            query = orderColumnIndex switch
            {
                "0" => orderDir == "asc"
                    ? query.OrderBy(m => m.Name)
                    : query.OrderByDescending(m => m.Name),
                "1" => orderDir == "asc"
                    ? query.OrderBy(m => m.Category)
                    : query.OrderByDescending(m => m.Category),
                _ => query.OrderBy(m => m.Category) // default ordering
            };

            var data = await query
                .Skip(start)
                .Take(length)
                .ToListAsync();

            return new JsonResult(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }

        // ❌ DELETE — UNCHANGED
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var modelToBeDeleted = await _unitOfWork.Model
                .GetAsy(m => m.Id == id && m.IsActive);

            if (modelToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            var hasSerialNumbers = await _unitOfWork.SerialNumber
                .GetAllAsy(sn => sn.IsActive && sn.ModelId == modelToBeDeleted.Id);

            if (hasSerialNumbers.Any())
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Model cannot be deleted because it has associated serial numbers"
                });
            }

            await _unitOfWork.Model.RemoveAsy(modelToBeDeleted);
            await _unitOfWork.SaveAsy();
            await _auditLogService.AddLogAsy(
                SD.Action_Delete,
                SD.Entity_Model,
                modelToBeDeleted.Id);

            return new JsonResult(new
            {
                success = true,
                message = "Model deleted successfully"
            });
        }

        public async Task<JsonResult> OnGetCategories()
        {
            var categories = (await _unitOfWork.Model
                .GetAllAsy(m => m.IsActive))
                .Select(m => m.Category ?? "Uncategorized")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return new JsonResult(categories);
        }
    }
}
