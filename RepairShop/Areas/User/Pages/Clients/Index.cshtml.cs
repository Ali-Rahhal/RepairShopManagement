using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.User.Pages.Clients
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

        // Server-side pagination, filtering and ordering
        public async Task<JsonResult> OnGetAll(
            int draw,
            int start = 0,
            int length = 10)
        {
            var search = Request.Query["search[value]"].ToString();
            var orderColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
            var orderDir = Request.Query["order[0][dir]"].FirstOrDefault() ?? "asc";

            // Base query - only main clients (no branches)
            var query = await _unitOfWork.Client
                .GetQueryableAsy(c => c.ParentClientId == null && c.IsActive == true,
                            includeProperties: "Branches");

            var recordsTotal = await query.CountAsync();

            // Global search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (c.Phone ?? "").Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (c.Email ?? "").Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (c.Address ?? "").Contains(search, StringComparison.CurrentCultureIgnoreCase));
            }

            var recordsFiltered = await query.CountAsync();

            // Server-side ordering
            query = orderColumnIndex switch
            {
                "0" => orderDir == "asc" ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
                "1" => orderDir == "asc" ? query.OrderBy(c => c.Branches.Count(b => b.IsActive))
                                         : query.OrderByDescending(c => c.Branches.Count(b => b.IsActive)),
                "2" => orderDir == "asc" ? query.OrderBy(c => c.Phone) : query.OrderByDescending(c => c.Phone),
                "3" => orderDir == "asc" ? query.OrderBy(c => c.Email) : query.OrderByDescending(c => c.Email),
                "4" => orderDir == "asc" ? query.OrderBy(c => c.Address) : query.OrderByDescending(c => c.Address),
                _ => query.OrderBy(c => c.Name) // default
            };

            // Pagination
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    branchCount = c.Branches.Count(b => b.IsActive),
                    phone = c.Phone,
                    email = c.Email,
                    address = c.Address
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

        // Delete method
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var clientToBeDeleted = await _unitOfWork.Client.GetAsy(o => o.Id == id && o.IsActive == true, includeProperties: "Branches");
            if (clientToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            var SNsRelatedToClient = (await _unitOfWork.SerialNumber.GetAllAsy(sn => sn.IsActive == true && sn.ClientId == clientToBeDeleted.Id));
            if (SNsRelatedToClient.Any())
            {
                return new JsonResult(new { success = false, message = "Cannot be deleted because it has related serial numbers" });
            }

            if (clientToBeDeleted.Branches.Count != 0)
            {
                foreach (var branch in clientToBeDeleted.Branches)
                {
                    var SNsRelatedToBranch = (await _unitOfWork.SerialNumber.GetAllAsy(sn => sn.IsActive == true && sn.ClientId == branch.Id));
                    if (SNsRelatedToBranch.Any())
                    {
                        return new JsonResult(new { success = false, message = "Client cannot be deleted because its branches have related serial numbers" });
                    }
                }
                foreach (var branch in clientToBeDeleted.Branches)
                {
                    await _unitOfWork.Client.RemoveAsy(branch);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Delete, SD.Entity_Branch, branch.Id);
                }
            }

            await _unitOfWork.Client.RemoveAsy(clientToBeDeleted);
            await _unitOfWork.SaveAsy();
            await _auditLogService.AddLogAsy(SD.Action_Delete, SD.Entity_Client, clientToBeDeleted.Id);
            return new JsonResult(new { success = true, message = "Deleted successfully" });
        }
    }
}