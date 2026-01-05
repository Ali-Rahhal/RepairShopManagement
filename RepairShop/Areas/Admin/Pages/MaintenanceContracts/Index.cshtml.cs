using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.Admin.Pages.MaintenanceContracts
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

        // ✅ SERVER-SIDE DATATABLE
        public async Task<JsonResult> OnGetAll(
            int draw,
            int start = 0,
            int length = 10,
            string? status = null)
        {
            try
            {
                var search = Request.Query["search[value]"].FirstOrDefault();

                var query = await _unitOfWork.MaintenanceContract.GetQueryableAsy(
                    mc => mc.IsActive == true,
                    includeProperties: "Client,Client.ParentClient"
                );

                // 🔄 Update status dynamically (Active / Expired)
                foreach (var mc in query)
                {
                    var newStatus = mc.EndDate < DateTime.Now ? "Expired" : "Active";
                    if (mc.Status != newStatus)
                        mc.Status = newStatus;
                }
                await _unitOfWork.SaveAsy();

                var recordsTotal = await query.CountAsync();

                // 🔍 Global search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(mc =>
                        mc.Id.ToString().Contains(search) ||
                        mc.Client.Name.ToLower().Contains(search) ||
                        (mc.Client.ParentClient != null &&
                         mc.Client.ParentClient.Name.ToLower().Contains(search))
                    );
                }

                // 🏷 Status filter
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    query = query.Where(mc => mc.Status == status);
                }

                var recordsFiltered = await query.CountAsync();

                var data = await query
                    .OrderByDescending(mc => mc.Id)
                    .Skip(start)
                    .Take(length)
                    .Select(mc => new
                    {
                        id = mc.Id,
                        contractNumber = $"CONTRACT-{mc.Id:D4}",
                        clientName = mc.Client.ParentClient != null
                            ? mc.Client.ParentClient.Name
                            : mc.Client.Name,
                        clientBranch = mc.Client.ParentClient != null
                            ? mc.Client.Name
                            : "N/A",
                        startDate = mc.StartDate,
                        endDate = mc.EndDate,
                        status = mc.Status,
                        daysRemaining = (mc.EndDate - DateTime.Now).Days,
                        isExpired = mc.EndDate < DateTime.Now
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

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id, [FromServices] DeleteService _dlt)
        {
            var contractToBeDeleted = await _unitOfWork.MaintenanceContract.GetAsy(mc => mc.Id == id && mc.IsActive == true);
            if (contractToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _dlt.DeleteMaintenanceContractAsync(contractToBeDeleted.Id);
            
            return new JsonResult(new { success = true, message = "Maintenance contract deleted successfully" });
        }

        // API for Clients (for filter dropdown)
        public async Task<JsonResult> OnGetClients()
        {
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true))
                .Select(c => new { id = c.Id, name = c.Name, phone = c.Phone })
                .OrderBy(c => c.name)
                .ToList();

            return new JsonResult(new { clients });
        }
    }
}