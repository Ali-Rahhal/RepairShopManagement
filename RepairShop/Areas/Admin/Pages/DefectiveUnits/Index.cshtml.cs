using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System;
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
                var orderDir = Request.Form["order[0][dir]"].FirstOrDefault();

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

                // Server-side ordering
                query = orderColumnIndex switch
                {
                    "0" => orderDir == "asc"
                        ? query.OrderBy(du => du.SerialNumber.Value)
                        : query.OrderByDescending(du => du.SerialNumber.Value),
                    "1" => orderDir == "asc"
                        ? query.OrderBy(du => du.SerialNumber.Model.Name)
                        : query.OrderByDescending(du => du.SerialNumber.Model.Name),
                    "2" => orderDir == "asc"
                        ? query.OrderBy(du => du.SerialNumber.Client.Name)
                        : query.OrderByDescending(du => du.SerialNumber.Client.Name),
                    "4" => orderDir == "asc"
                        ? query.OrderBy(du => du.ReportedDate)
                        : query.OrderByDescending(du => du.ReportedDate),
                    "7" => orderDir == "asc"
                        ? query.OrderBy(du => du.Status == SD.Status_DU_Reported ? 1 :
                                                du.Status == SD.Status_DU_QuotationSent ? 2 :
                                                du.Status == SD.Status_DU_QuotationConfirmed ? 3 :
                                                du.Status == SD.Status_DU_UnderRepair ? 4 :
                                                du.Status == SD.Status_DU_Fixed ? 5 : 6)
                        : query.OrderByDescending(du => du.Status == SD.Status_DU_Reported ? 1 :
                                                        du.Status == SD.Status_DU_QuotationSent ? 2 :
                                                        du.Status == SD.Status_DU_QuotationConfirmed ? 3 :
                                                        du.Status == SD.Status_DU_UnderRepair ? 4 :
                                                        du.Status == SD.Status_DU_Fixed ? 5 : 6),
                    "10" => orderDir == "asc"
                        ? query.OrderBy(du => du.ResolvedDate)
                        : query.OrderByDescending(du => du.ResolvedDate),
                    _ => query.OrderByDescending(du => du.LastModifiedDate ?? du.ReportedDate)
                                .ThenBy(du => du.Status == SD.Status_DU_Reported ? 1 :
                                                du.Status == SD.Status_DU_QuotationSent ? 2 :
                                                du.Status == SD.Status_DU_QuotationConfirmed ? 3 :
                                                du.Status == SD.Status_DU_UnderRepair ? 4 :
                                                du.Status == SD.Status_DU_Fixed ? 5 : 6)
                                .ThenByDescending(du => du.ReportedDate) // DEFAULT ordering
                };

                var data = await query
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
                        invoiceByBachir = du.InvoiceByBachir,
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

        public async Task<IActionResult> OnGetQuotationChange(int? id, bool?isConfirmed = false)
        {
            if (id == null || id == 0)
            {
                return new JsonResult(new { success = false, message = "Error while changing quotation status" });
            }

            var defectiveUnit = await _unitOfWork.DefectiveUnit.GetAsy(du => du.IsActive == true && du.Id == id, includeProperties: "SerialNumber,SerialNumber.Client");

            if (defectiveUnit == null)
            {
                return new JsonResult(new { success = false, message = "Error while changing quotation status" });
            }

            DefectiveUnit oldDefectiveUnit = defectiveUnit.Clone();

            if(isConfirmed != true)
            {
                defectiveUnit.Status = SD.Status_DU_QuotationSent;
            }
            else
            {
                defectiveUnit.Status = SD.Status_DU_QuotationConfirmed;
            }

            defectiveUnit.LastModifiedDate = DateTime.Now;
            await _unitOfWork.DefectiveUnit.UpdateAsy(defectiveUnit);

            await _unitOfWork.SaveAsy();

            await _auditLogService.AddLogAsy(SD.Action_Update, SD.Entity_DefectiveUnit, defectiveUnit.Id, oldDefectiveUnit);

            return new JsonResult(new { success = true, message = "Quotation status changed successfully to " + (isConfirmed == true ? "confirmed" : "sent")});
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

            if (Env.Feature_DUQuotationStatus)
            {
                if (defectiveUnitToBeAdded.Status != SD.Status_DU_QuotationConfirmed)
                {
                    return new JsonResult(new { success = false, message = "Defective unit must have status 'Quotation confirmed' before adding to transaction" });
                }
            }
            else
            {
                if (defectiveUnitToBeAdded.Status != SD.Status_DU_Reported)
                {
                    return new JsonResult(new { success = false, message = "Defective unit must have status 'Reported' before adding to transaction" });
                }
            }

            TransactionHeader thForDu = new TransactionHeader()
                {
                    DefectiveUnitId = (int)DuId,
                    UserId = currentUserId
                };

            DefectiveUnit oldDefectiveUnit = defectiveUnitToBeAdded.Clone();

            defectiveUnitToBeAdded.Status = SD.Status_DU_UnderRepair;
            defectiveUnitToBeAdded.LastModifiedDate = DateTime.Now;
            await _unitOfWork.DefectiveUnit.UpdateAsy(defectiveUnitToBeAdded);

            await _unitOfWork.TransactionHeader.AddAsy(thForDu);

            await _unitOfWork.SaveAsy();
            await _auditLogService.AddLogAsy(SD.Action_Update, SD.Entity_DefectiveUnit, defectiveUnitToBeAdded.Id, oldDefectiveUnit);
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

        public async Task<IActionResult> OnGetNotes(long id)
        {
            var notes = await _unitOfWork.DefectiveUnitNote.GetAllAsy(
                n => n.DefectiveUnitId == id && n.IsActive,
                includeProperties: "User");

            var result = notes
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new
                {
                    note = x.Note,
                    user = x.User.UserName,
                    date = x.CreatedDate.ToString("dd/MM/yyyy hh:mm tt")
                });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAddNote(
            long defectiveUnitId,
            string note,
            [FromServices] SignInManager<AppUser> SignInManager)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Note cannot be empty."
                });
            }

            var currentUser =
                SignInManager.UserManager.GetUserId(User);

            var duNote = new DefectiveUnitNote
            {
                DefectiveUnitId = defectiveUnitId,
                Note = note.Trim(),
                UserId = currentUser
            };

            await _unitOfWork.DefectiveUnitNote.AddAsy(duNote);

            await _unitOfWork.SaveAsy();

            return new JsonResult(new
            {
                success = true
            });
        }
    }
}