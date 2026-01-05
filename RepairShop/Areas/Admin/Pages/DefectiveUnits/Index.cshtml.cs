using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Security.Claims;

namespace RepairShop.Areas.Admin.Pages.DefectiveUnits
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

                var query = await _unitOfWork.DefectiveUnit.GetQueryableAsy(
                    du => du.IsActive == true,
                    includeProperties:
                        "SerialNumber," +
                        "SerialNumber.Model," +
                        "SerialNumber.Client," +
                        "SerialNumber.Client.ParentClient," +
                        "SerialNumber.Warranty," +
                        "SerialNumber.MaintenanceContract"
                );

                var recordsTotal = await query.CountAsync();

                // 🔍 Global search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(du =>
                        du.Description.ToLower().Contains(search) ||
                        du.SerialNumber.Value.ToLower().Contains(search) ||
                        du.SerialNumber.Model.Name.ToLower().Contains(search) ||
                        du.SerialNumber.Client.Name.ToLower().Contains(search) ||
                        (du.SerialNumber.Client.ParentClient != null &&
                         du.SerialNumber.Client.ParentClient.Name.ToLower().Contains(search))
                    );
                }

                // 🏷 Status filter
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    query = query.Where(du => du.Status == status);
                }

                var recordsFiltered = await query.CountAsync();

                var data = await query
                    .OrderBy(du =>
                        du.Status == SD.Status_DU_Reported ? 1 :
                        du.Status == SD.Status_DU_UnderRepair ? 2 :
                        du.Status == SD.Status_DU_Fixed ? 3 : 4
                    )
                    .ThenByDescending(du => du.ReportedDate)
                    .Skip(start)
                    .Take(length)
                    .Select(du => new
                    {
                        id = du.Id,
                        serialNumber = du.SerialNumber.Value,
                        model = du.SerialNumber.Model.Name,
                        clientName = du.SerialNumber.Client.ParentClient != null
                            ? du.SerialNumber.Client.ParentClient.Name
                            : du.SerialNumber.Client.Name,
                        clientBranch = du.SerialNumber.Client.ParentClient != null
                            ? du.SerialNumber.Client.Name
                            : "N/A",
                        reportedDate = du.ReportedDate,
                        description = du.Description,
                        hasAccessories = du.HasAccessories,
                        status = du.Status,
                        daysSinceReported = (DateTime.Now - du.ReportedDate).Days,
                        resolvedDate = du.ResolvedDate != null
                            ? du.ResolvedDate.Value.ToString("dd/MM/yyyy")
                            : "Not resolved",
                        warrantyCovered = du.SerialNumber.WarrantyId != null ? "Yes" : "No",
                        contractCovered = du.SerialNumber.MaintenanceContractId != null ? "Yes" : "No"
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
            var defectiveUnitToBeDeleted = await _unitOfWork.DefectiveUnit.GetAsy(du => du.IsActive == true && du.Id == id);
            if (defectiveUnitToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _dlt.DeleteDefectiveUnitAsync(defectiveUnitToBeDeleted.Id);
            
            return new JsonResult(new { success = true, message = "Defective unit report deleted successfully" });
        }

        public async Task<IActionResult> OnGetAddToTransaction(int? DuId)
        {
            if (DuId == null || DuId == 0)
            {
                return new JsonResult(new { success = false, message = "Error while adding to transaction" });
            }

            var defectiveUnitToBeAdded = await _unitOfWork.DefectiveUnit.GetAsy(du => du.IsActive == true && du.Id == DuId, includeProperties: "SerialNumber,SerialNumber.Client");

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (defectiveUnitToBeAdded == null || currentUserId == null)
            {
                return new JsonResult(new { success = false, message = "Error while adding to transaction" });
            }

            TransactionHeader thForDu = new TransactionHeader()
            {
                DefectiveUnitId = (int)DuId,
                UserId = currentUserId
            };

            defectiveUnitToBeAdded.Status = SD.Status_DU_UnderRepair;
            await _unitOfWork.DefectiveUnit.UpdateAsy(defectiveUnitToBeAdded);

            await _unitOfWork.TransactionHeader.AddAsy(thForDu);

            await _unitOfWork.SaveAsy();
            await _auditLogService.AddLogAsy(SD.Action_Create, SD.Entity_TransactionHeader, thForDu.Id);

            return new JsonResult(new { success = true, message = "Defective unit added to transaction successfully" });
        }

        // Add this method to your IndexModel class
        public async Task<IActionResult> OnGetDownloadPdf(int id)
        {
            var defectiveUnit = await _unitOfWork.DefectiveUnit.GetAsy(
                du => du.IsActive == true && du.Id == id,
                includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client,SerialNumber.Client.ParentClient,SerialNumber.Warranty,SerialNumber.MaintenanceContract"
            );

            if (defectiveUnit == null)
            {
                return NotFound();
            }

            var reportService = new DUReportService();
            var pdfBytes = reportService.GenerateDUReportPdf(defectiveUnit);

            return File(pdfBytes, "application/pdf",
                $"DefectiveUnitReport_{defectiveUnit.SerialNumber?.Value}_{defectiveUnit.Id}_{DateTime.Now:yyyy-MM-dd hh:mm tt}.pdf");
        }
    }
}