using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Linq.Expressions;

namespace RepairShop.Areas.Admin.Pages.Warranties
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void OnGet()
        {
        }

        // ✅ SERVER-SIDE PAGINATION + FILTERING + ORDERING
        public async Task<JsonResult> OnPostAll(
            int draw,
            int start = 0,
            int length = 10,
            string? status = null)
        {
            try
            {
                var search = Request.Form["search[value]"].FirstOrDefault();
                var orderColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
                var orderDir = Request.Form["order[0][dir]"].FirstOrDefault() ?? "desc";

                // Start with base query
                var query = await _unitOfWork.Warranty
                    .GetQueryableAsy(w => w.IsActive == true,
                    includeProperties: "SerialNumbers,SerialNumbers.Model,SerialNumbers.Client,SerialNumbers.Client.ParentClient");

                // 🔄 Update status dynamically (Active / Expired)
                var warrantiesToUpdate = new List<Warranty>();
                foreach (var warranty in query)
                {
                    var newStatus = warranty.EndDate < DateTime.Now ? "Expired" : "Active";
                    if (warranty.Status != newStatus)
                    {
                        warranty.Status = newStatus;
                        warrantiesToUpdate.Add(warranty);
                    }
                }

                if (warrantiesToUpdate.Count > 0)
                {
                    foreach (var warranty in warrantiesToUpdate)
                    {
                        await _unitOfWork.Warranty.UpdateAsy(warranty);
                    }
                    await _unitOfWork.SaveAsy();
                }

                var recordsTotal = await query.CountAsync();

                // 🔍 Global search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(w =>
                        w.Id.ToString().Contains(search) ||
                        (w.SerialNumbers != null && w.SerialNumbers.Any(sn =>
                            sn.Value.ToLower().Contains(search))) ||
                        (w.SerialNumbers != null && w.SerialNumbers.Any(sn =>
                            sn.Model.Name.ToLower().Contains(search))) ||
                        (w.SerialNumbers != null && w.SerialNumbers.Any(sn =>
                            (sn.Client.ParentClient != null ?
                                sn.Client.ParentClient.Name :
                                sn.Client.Name).ToLower().Contains(search)))
                    );
                }

                // 🏷 Status filter
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    query = query.Where(w =>
                        status == "Active" ? w.EndDate >= DateTime.Now :
                        status == "Expired" ? w.EndDate < DateTime.Now :
                        w.Status == status);
                }

                var recordsFiltered = await query.CountAsync();

                // 🗂️ Apply ordering
                query = ApplyOrdering(query, orderColumnIndex, orderDir);

                // 📄 Pagination
                var data = await query
                    .Skip(start)
                    .Take(length)
                    .Select(w => new
                    {
                        id = w.Id,
                        warrantyNumber = $"WARRANTY-{w.Id:D4}",
                        startDate = w.StartDate,
                        endDate = w.EndDate,
                        status = w.EndDate < DateTime.Now ? "Expired" : "Active",
                        daysRemaining = (w.EndDate - DateTime.Now).Days,
                        isExpired = w.EndDate < DateTime.Now,
                        coveredCount = w.SerialNumbers.Count(sn => sn.IsActive),

                        // For display - get first serial number details
                        serialNumbers = w.SerialNumbers
                            .Where(sn => sn.IsActive)
                            .Select(sn => sn.Value ?? "N/A")
                            .ToList(),

                        modelName = w.SerialNumbers
                            .Where(sn => sn.IsActive)
                            .Select(sn => sn.Model.Name)
                            .FirstOrDefault() ?? "N/A",

                        clientName = w.SerialNumbers
                            .Where(sn => sn.IsActive)
                            .Select(sn => sn.Client.ParentClient != null ?
                                $"{sn.Client.ParentClient.Name} - {sn.Client.Name}" :
                                sn.Client.Name)
                            .FirstOrDefault() ?? "N/A",

                        // For sorting
                        endDateTimestamp = w.EndDate,
                        startDateTimestamp = w.StartDate,
                        statusPriority = w.EndDate < DateTime.Now ? 2 : 1 // Active=1, Expired=2
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

        private IQueryable<Warranty> ApplyOrdering(IQueryable<Warranty> query, string? orderColumnIndex, string? orderDir)
        {
            // Default ordering: Status (Active first) → EndDate asc → StartDate desc
            if (string.IsNullOrEmpty(orderColumnIndex) || string.IsNullOrEmpty(orderDir))
            {
                return query
                    .OrderByDescending(w => w.Id); // By Id
            }

            // Map column index to expression
            Expression<Func<Warranty, object>> orderExpr = orderColumnIndex switch
            {
                "0" => w => w.Id,                     // Warranty Number
                "2" => w => w.SerialNumbers.Any() ?   // First Serial Number
                           w.SerialNumbers.First().Value : "",
                "3" => w => w.SerialNumbers.Any() ?   // First Model
                           w.SerialNumbers.First().Model.Name : "",
                "4" => w => w.SerialNumbers.Any() ?   // First Client
                           (w.SerialNumbers.First().Client.ParentClient != null ?
                            w.SerialNumbers.First().Client.ParentClient.Name :
                            w.SerialNumbers.First().Client.Name) : "",
                "5" => w => w.StartDate,              // Start Date
                "6" => w => w.EndDate,                // End Date
                "8" => w => w.EndDate < DateTime.Now ? 2 : 1, // Status
                _ => w => w.Id    // Default: Id
            };

            return orderDir == "asc"
                ? query.OrderBy(orderExpr)
                : query.OrderByDescending(orderExpr);
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id, [FromServices] DeleteService _dlt)
        {
            var warrantyToBeDeleted = await _unitOfWork.Warranty.GetAsy(w => w.Id == id && w.IsActive == true);
            if (warrantyToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _dlt.DeleteWarrantyAsync(warrantyToBeDeleted.Id);

            return new JsonResult(new { success = true, message = "Warranty deleted successfully" });
        }

        // ✅ API for Statuses (for filter dropdown)
        public async Task<JsonResult> OnGetStatuses()
        {
            var statuses = new List<object>
            {
                new { id = "All", name = "All Statuses" },
                new { id = "Active", name = "Active" },
                new { id = "Expired", name = "Expired" }
            };

            return new JsonResult(new { statuses });
        }
    }
}