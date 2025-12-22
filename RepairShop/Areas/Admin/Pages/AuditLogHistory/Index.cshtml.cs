using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.Admin.Pages.AuditLogHistory
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void OnGet() { }

        public async Task<JsonResult> OnGetAll(
            int draw,
            int start,
            int length,
            string? searchValue,
            string? actionFilter,
            string? entityTypeFilter,
            string? userNameFilter,
            DateTime? startDate,
            DateTime? endDate)
        {
            try
            {
                var query = (await _unitOfWork.AuditLog.GetQueryableAsy(
                    a => a.IsActive,
                    includeProperties: "User"
                ));

                var totalRecords = await query.CountAsync();

                // filters
                if (!string.IsNullOrEmpty(actionFilter) && actionFilter != "All")
                    query = query.Where(a => a.Action == actionFilter);

                if (!string.IsNullOrEmpty(entityTypeFilter) && entityTypeFilter != "All")
                    query = query.Where(a => a.EntityType == entityTypeFilter);

                if (!string.IsNullOrEmpty(userNameFilter))
                    query = query.Where(a => a.User != null &&
                                             a.User.UserName.Contains(userNameFilter));

                if (startDate.HasValue)
                    query = query.Where(a => a.CreatedDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.CreatedDate < endDate.Value.AddDays(1));

                // global search
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    query = query.Where(a =>
                        a.Id.ToString().Contains(searchValue) ||
                        (a.User != null && a.User.UserName.ToLower().Contains(searchValue)) ||
                        a.Action.ToLower().Contains(searchValue) ||
                        a.EntityType.ToLower().Contains(searchValue) ||
                        a.Description.ToLower().Contains(searchValue)
                    );
                }

                var filteredRecords = await query.CountAsync();

                var data = await query
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip(start)
                    .Take(length)
                    .Select(a => new
                    {
                        id = a.Id,
                        logNumber = $"LOG-{a.Id:D6}",
                        user = a.User != null ? a.User.UserName : null,
                        action = a.Action,
                        entityType = a.EntityType,
                        description = a.Description,
                        createdDate = a.CreatedDate
                    })
                    .ToListAsync();

                return new JsonResult(new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        public async Task<JsonResult> OnGetEntityTypes()
        {
            var entityTypes = (await _unitOfWork.AuditLog.GetAllAsy(a => a.IsActive))
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return new JsonResult(new { entityTypes });
        }

        public async Task<JsonResult> OnGetActions()
        {
            var actions = (await _unitOfWork.AuditLog.GetAllAsy(a => a.IsActive))
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return new JsonResult(new { actions });
        }
    }

}
