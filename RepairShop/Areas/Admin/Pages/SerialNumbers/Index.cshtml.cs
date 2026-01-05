using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Linq.Expressions;

namespace RepairShop.Areas.Admin.Pages.SerialNumbers
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
            int? modelId = null,
            int? clientId = null)
        {
            try
            {
                var search = Request.Query["search[value]"].FirstOrDefault();
                // Get ordering info from DataTables
                var orderColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
                var orderDir = Request.Query["order[0][dir]"].FirstOrDefault() ?? "desc"; // default desc

                // Start with base query
                var query = await _unitOfWork.SerialNumber
                    .GetQueryableAsy(sn => sn.IsActive,
                        includeProperties: "Model,Client,Client.ParentClient,MaintenanceContract,Warranty");

                var recordsTotal = await query.CountAsync();

                // 🔍 Global search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(sn =>
                        sn.Value.ToLower().Contains(search) ||
                        sn.Model.Name.ToLower().Contains(search) ||
                        sn.Client.Name.ToLower().Contains(search) ||
                        (sn.Client.ParentClient != null &&
                         sn.Client.ParentClient.Name.ToLower().Contains(search)));
                }

                // 🏷️ Model filter
                if (modelId.HasValue && modelId > 0)
                {
                    query = query.Where(sn => sn.ModelId == modelId);
                }

                // 👥 Client filter (parent clients only)
                if (clientId.HasValue && clientId > 0)
                {
                    query = query.Where(sn =>
                        sn.Client.ParentClientId == clientId || // Branch of parent client
                        (sn.Client.ParentClient == null && sn.ClientId == clientId)); // Parent client itself
                }

                var recordsFiltered = query.Count();

                // Map column index to expression
                Expression<Func<SerialNumber, object>> orderExpr = orderColumnIndex switch
                {
                    "0" => sn => sn.Value,
                    "1" => sn => sn.Model.Name,
                    "2" => sn => sn.Client.ParentClient != null ? sn.Client.ParentClient.Name : sn.Client.Name,
                    "3" => sn => sn.Client.ParentClient != null ? sn.Client.Name : "N/A",
                    "6" => sn => sn.ReceivedDate,
                    _ => sn => sn.ReceivedDate // default ordering
                };

                // Apply ordering
                query = orderDir == "asc"
                    ? query.OrderBy(orderExpr)
                    : query.OrderByDescending(orderExpr);

                // Apply ordering and pagination
                var data = await query
                    .Skip(start)
                    .Take(length)
                    .Select(sn => new
                    {
                        id = sn.Id,
                        value = sn.Value,
                        modelName = sn.Model.Name,
                        clientName = sn.Client.ParentClient != null
                            ? sn.Client.ParentClient.Name
                            : sn.Client.Name,
                        branchName = sn.Client.ParentClient != null
                            ? sn.Client.Name
                            : "N/A",
                        maintenanceContractId = sn.MaintenanceContractId.HasValue
                            ? $"{sn.MaintenanceContractId:D4}"
                            : null,
                        warrantyId = sn.WarrantyId.HasValue
                            ? $"{sn.WarrantyId:D4}"
                            : null,
                        receivedDate = sn.ReceivedDate.ToString("dd-MM-yyyy hh:mm tt")
                    })
                    .ToListAsync(); // Use async version if available

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

        // ❌ DELETE — UNCHANGED
        public async Task<IActionResult> OnPostDelete(int? id, string reason)
        {
            var serialNumberToBeDeleted = await _unitOfWork.SerialNumber.GetAsy(sn => sn.Id == id && sn.IsActive == true);
            if (serialNumberToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // Check if serial number is referenced in any defective units
            var isUsedInDefectiveUnits = (await _unitOfWork.DefectiveUnit
                .GetAllAsy(du => du.SerialNumberId == serialNumberToBeDeleted.Id && du.IsActive == true));

            if (isUsedInDefectiveUnits.Any())
            {
                return new JsonResult(new { success = false, message = "Serial number cannot be deleted because it is used in defective units" });
            }

            var isUsedInPreventiveMaintenances = (await _unitOfWork.PreventiveMaintenanceRecord.GetAllAsy(pm => pm.SerialNumberId == serialNumberToBeDeleted.Id && pm.IsActive == true));

            if (isUsedInPreventiveMaintenances.Any())
            {
                return new JsonResult(new { success = false, message = "Serial number cannot be deleted because it is used in preventive maintenance records" });
            }

            serialNumberToBeDeleted.InactiveReason = reason;

            await _unitOfWork.SerialNumber.RemoveAsy(serialNumberToBeDeleted);
            await _unitOfWork.SaveAsy();
            await _auditLogService.AddLogAsy(SD.Action_Delete, SD.Entity_SerialNumber, serialNumberToBeDeleted.Id);
            return new JsonResult(new { success = true, message = "Serial number deleted successfully" });
        }

        // ✅ API for Models (for filter dropdown) - UNCHANGED
        public async Task<JsonResult> OnGetModels()
        {
            var models = (await _unitOfWork.Model.GetAllAsy(m => m.IsActive == true
                            && m.SerialNumbers.Any(sn => sn.IsActive == true),
                            includeProperties: "SerialNumbers"))
                                .Select(m => new { id = m.Id, name = m.Name })
                                .OrderBy(m => m.name)
                                .ToList();

            return new JsonResult(new { models });
        }

        // ✅ API for Clients (for filter dropdown) - UNCHANGED
        public async Task<JsonResult> OnGetClients()
        {
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true
                            && c.ParentClient == null
                            && c.SerialNumbers.Any(sn => sn.IsActive == true),
                            includeProperties: "SerialNumbers"))
                                .Select(c => new { id = c.Id, name = c.Name })
                                .OrderBy(c => c.name)
                                .ToList();

            return new JsonResult(new { clients });
        }
    }
}