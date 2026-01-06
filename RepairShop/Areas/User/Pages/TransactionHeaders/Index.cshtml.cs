using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Linq.Expressions;
using System.Security.Claims;

namespace RepairShop.Areas.User.Pages.TransactionHeaders
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
            string? statusFilter = null,
            string? clientFilter = null,
            DateTime? minDate = null,
            DateTime? maxDate = null)
        {
            try
            {
                var search = Request.Form["search[value]"].FirstOrDefault();
                var orderColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
                var orderDir = Request.Form["order[0][dir]"].FirstOrDefault();

                // Get user info
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userRole = claimsIdentity.FindFirst(ClaimTypes.Role).Value;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Start with base query
                IQueryable<TransactionHeader> query = null;

                if (userRole == SD.Role_Admin)
                {
                    query = await _unitOfWork.TransactionHeader
                        .GetQueryableAsy(t => t.IsActive == true,
                        includeProperties: "User,DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model,DefectiveUnit.SerialNumber.Client,DefectiveUnit.SerialNumber.Client.ParentClient");
                }
                else
                {
                    query = await _unitOfWork.TransactionHeader
                        .GetQueryableAsy(t => t.IsActive == true && t.UserId == userId,
                        includeProperties: "DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model,DefectiveUnit.SerialNumber.Client,DefectiveUnit.SerialNumber.Client.ParentClient");
                }

                var recordsTotal = await query.CountAsync();

                // 🔍 Global search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(t =>
                        t.DefectiveUnit.SerialNumber.Value.ToLower().Contains(search) ||
                        t.DefectiveUnit.SerialNumber.Model.Name.ToLower().Contains(search) ||
                        t.Status.ToLower().Contains(search));
                }

                // 🏷️ Status filter
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                {
                    query = query.Where(t => t.Status == statusFilter);
                }

                // 👥 Client filter
                if (!string.IsNullOrEmpty(clientFilter) && clientFilter != "All")
                {
                    query = query.Where(t =>
                        (t.DefectiveUnit.SerialNumber.Client.ParentClient != null &&
                         t.DefectiveUnit.SerialNumber.Client.ParentClient.Name == clientFilter) ||
                        (t.DefectiveUnit.SerialNumber.Client.ParentClient == null &&
                         t.DefectiveUnit.SerialNumber.Client.Name == clientFilter));
                }

                // 📅 Date range filter
                if (minDate.HasValue)
                {
                    query = query.Where(t => t.CreatedDate >= minDate.Value);
                }

                if (maxDate.HasValue)
                {
                    var maxDateWithTime = maxDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(t => t.CreatedDate <= maxDateWithTime);
                }

                var recordsFiltered = await query.CountAsync();

                // 🗂️ Apply ordering (multi-level when no specific column is ordered)
                query = ApplyOrdering(query, orderColumnIndex, orderDir);

                // 📄 Pagination
                var data = await query
                    .Skip(start)
                    .Take(length)
                    .Select(t => new
                    {
                        id = t.Id,
                        user = t.User != null ? new { userName = t.User.UserName } : null,
                        model = t.DefectiveUnit.SerialNumber.Model.Name,
                        serialNumber = t.DefectiveUnit.SerialNumber.Value,
                        duDescription = t.DefectiveUnit.Description,
                        status = t.Status,
                        clientName = t.DefectiveUnit.SerialNumber.Client.ParentClient != null
                            ? t.DefectiveUnit.SerialNumber.Client.ParentClient.Name
                            : t.DefectiveUnit.SerialNumber.Client.Name,
                        branchName = t.DefectiveUnit.SerialNumber.Client.ParentClient != null
                            ? t.DefectiveUnit.SerialNumber.Client.Name
                            : "N/A",
                        lastModifiedDate = t.LastModifiedDate ?? t.CreatedDate,
                        createdDate = t.CreatedDate,
                        inProgressDate = t.InProgressDate,
                        completedOrOutOfServiceDate = t.CompletedOrOutOfServiceDate,
                        deliveredDate = t.DeliveredDate,
                        processedDate = t.ProcessedDate,
                        defectiveUnitId = t.DefectiveUnitId,
                        // For sorting
                        statusPriority = GetStatusPriority(t.Status),
                        lastModifiedTimestamp = t.LastModifiedDate ?? t.CreatedDate,
                        createdTimestamp = t.CreatedDate
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

        private static int GetStatusPriority(string status)
        {
            return status switch
            {
                "New" => 1,
                "InProgress" => 2,
                "Completed" => 3,
                "Delivered" => 4,
                "Processed" => 5,
                "OutOfService" => 6,
                _ => 7
            };
        }

        private IQueryable<TransactionHeader> ApplyOrdering(IQueryable<TransactionHeader> query, string? orderColumnIndex, string? orderDir)
        {
            // If no specific column is ordered by user, apply default multi-level ordering
            if (string.IsNullOrEmpty(orderColumnIndex) || string.IsNullOrEmpty(orderDir))
            {
                // DEFAULT ORDERING: LastModifiedDate desc → Status asc → CreatedDate desc
                return query
                    .OrderByDescending(t => t.LastModifiedDate ?? t.CreatedDate) // 1st level: ModifiedDate desc
                    .ThenBy(t => t.Status == "New" ? 1 :              // 2nd level: Status priority asc
                               t.Status == "InProgress" ? 2 :
                               t.Status == "Completed" ? 3 :
                               t.Status == "Delivered" ? 4 :
                               t.Status == "Processed" ? 5 :
                               t.Status == "OutOfService" ? 6 : 7)
                    .ThenByDescending(t => t.CreatedDate);            // 3rd level: CreatedDate desc
            }

            // User has clicked on a column to sort - apply single column ordering
            Expression<Func<TransactionHeader, object>> orderExpr = orderColumnIndex switch
            {
                "0" => t => t.User != null ? t.User.UserName : "", // User column
                "1" => t => t.DefectiveUnit.SerialNumber.Model.Name,
                "2" => t => t.DefectiveUnit.SerialNumber.Value,
                "4" => t => t.Status == "New" ? 1 :
                           t.Status == "InProgress" ? 2 :
                           t.Status == "Completed" ? 3 :
                           t.Status == "Delivered" ? 4 :
                           t.Status == "Processed" ? 5 :
                           t.Status == "OutOfService" ? 6 : 7,
                "5" => t => t.DefectiveUnit.SerialNumber.Client.ParentClient != null
                            ? t.DefectiveUnit.SerialNumber.Client.ParentClient.Name
                            : t.DefectiveUnit.SerialNumber.Client.Name,
                "6" => t => t.DefectiveUnit.SerialNumber.Client.ParentClient != null
                            ? t.DefectiveUnit.SerialNumber.Client.Name
                            : "N/A",
                "7" => t => t.CreatedDate,
                "9" => t => t.LastModifiedDate ?? t.CreatedDate, // Hidden LastModifiedDate column
                _ => t => t.LastModifiedDate ?? t.CreatedDate // Default
            };

            return orderDir == "asc"
                ? query.OrderBy(orderExpr)
                : query.OrderByDescending(orderExpr);
        }

        // ✅ API for Clients (for filter dropdown)
        public async Task<JsonResult> OnGetClients()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userRole = claimsIdentity.FindFirst(ClaimTypes.Role).Value;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IQueryable<TransactionHeader> query;

            if (userRole == SD.Role_Admin)
            {
                query = await _unitOfWork.TransactionHeader
                    .GetQueryableAsy(t => t.IsActive == true,
                    includeProperties: "DefectiveUnit.SerialNumber.Client,DefectiveUnit.SerialNumber.Client.ParentClient");
            }
            else
            {
                query = await _unitOfWork.TransactionHeader
                    .GetQueryableAsy(t => t.IsActive == true && t.UserId == userId,
                    includeProperties: "DefectiveUnit.SerialNumber.Client,DefectiveUnit.SerialNumber.Client.ParentClient");
            }

            var clients = await query
                .Select(t => t.DefectiveUnit.SerialNumber.Client.ParentClient != null
                    ? t.DefectiveUnit.SerialNumber.Client.ParentClient.Name
                    : t.DefectiveUnit.SerialNumber.Client.Name)
                .Distinct()
                .OrderBy(name => name)
                .Select(name => new { id = name, name = name })
                .ToListAsync();

            return new JsonResult(new { clients });
        }

        // ✅ API for Statuses (for filter dropdown)
        public async Task<JsonResult> OnGetStatuses()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userRole = claimsIdentity.FindFirst(ClaimTypes.Role).Value;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IQueryable<TransactionHeader> query;

            if (userRole == SD.Role_Admin)
            {
                query = await _unitOfWork.TransactionHeader
                    .GetQueryableAsy(t => t.IsActive == true);
            }
            else
            {
                query = await _unitOfWork.TransactionHeader
                    .GetQueryableAsy(t => t.IsActive == true && t.UserId == userId);
            }

            var statuses = await query
                .Select(t => t.Status)
                .Distinct()
                .OrderBy(status => status)
                .Select(status => new { id = status, name = status })
                .ToListAsync();

            return new JsonResult(new { statuses });
        }

        //AJAX CALL for changing status from New to InProgress
        public async Task<IActionResult> OnGetChangeStatus(int? id)//The route is /User/TransactionHeaders/Index?handler=ChangeStatus&id=1
        {
            var THToBeChanged = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id && o.IsActive == true, includeProperties: "DefectiveUnit");
            if (THToBeChanged == null)
            {
                return new JsonResult(new { success = false, message = "Error while changing status" });
            }

            THToBeChanged.Status = SD.Status_Job_InProgress;
            THToBeChanged.InProgressDate = DateTime.Now;
            THToBeChanged.LastModifiedDate = DateTime.Now;
            await _unitOfWork.TransactionHeader.UpdateAsy(THToBeChanged);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Status changed successfully" });
        }

        //AJAX CALL for deleting a TH//Didnt use OnPostDelete because it needs the link to send a form and it causes issues with DataTables
        public async Task<IActionResult> OnGetDelete(int? id, [FromServices] DeleteService _dlt)//The route is /User/TransactionHeaders/Index?handler=Delete&id=1
        {
            var THToBeDeleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id && o.IsActive == true);
            if (THToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _dlt.DeleteTransactionHeaderAsync(THToBeDeleted.Id);
            
            return new JsonResult(new { success = true, message = "Transaction deleted successfully" });
        }

        // Method to validate if transaction can be completed
        public async Task<JsonResult> OnGetCanComplete(int? id)
        {
            var THToBeCompleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id && o.IsActive == true, includeProperties: "BrokenParts,DefectiveUnit");
            if (THToBeCompleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while completing" });
            }

            //Check if there are any pending parts
            var pendingParts = THToBeCompleted.BrokenParts
                .Where(o => (o.Status == SD.Status_Part_Pending_Repair
                                || o.Status == SD.Status_Part_Pending_Replace
                                || o.Status == SD.Status_Part_Waiting_Part) && o.IsActive == true).ToList();
            if (pendingParts.Count > 0)
            {
                return new JsonResult(new { success = false, message = "You have pending parts" });
            }

            //check if there are any active parts
            var partCount = THToBeCompleted.BrokenParts.Count(o => o.IsActive == true);
            if (partCount == 0)
            {
                return new JsonResult(new { success = false, message = "Parts must be reported before marking as completed" });
            }

            // If all checks pass
            return new JsonResult(new { success = true, message = "Transaction can be completed" });
        }

        // Method to actually complete the transaction with labor fees
        public async Task<JsonResult> OnPostCompleteStatus(int id, double laborFees)
        {
            var THToBeCompleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id && o.IsActive == true, includeProperties: "BrokenParts,DefectiveUnit");
            if (THToBeCompleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while completing" });
            }

            // Update labor fees
            THToBeCompleted.LaborFees = laborFees;

            //check if there are non-repairable parts
            var nonRepairableParts = THToBeCompleted.BrokenParts.Count(o => o.IsActive == true && (o.Status == SD.Status_Part_NotReplaceable
                                                                                                    || o.Status == SD.Status_Part_NotRepairable));
            if (nonRepairableParts > 0)
            {
                THToBeCompleted.Status = SD.Status_Job_OutOfService;
                THToBeCompleted.CompletedOrOutOfServiceDate = DateTime.Now;
                THToBeCompleted.DefectiveUnit.Status = SD.Status_DU_OutOfService;
                THToBeCompleted.DefectiveUnit.ResolvedDate = DateTime.Now;
                THToBeCompleted.LastModifiedDate = DateTime.Now;
                await _unitOfWork.TransactionHeader.UpdateAsy(THToBeCompleted);
                await _unitOfWork.SaveAsy();
                return new JsonResult(new { success = true, message = "Transaction is out of service" });
            }
            else
            {
                THToBeCompleted.Status = SD.Status_Job_Completed;
                THToBeCompleted.CompletedOrOutOfServiceDate = DateTime.Now;
                THToBeCompleted.DefectiveUnit.Status = SD.Status_DU_Fixed;
                THToBeCompleted.DefectiveUnit.ResolvedDate = DateTime.Now;
                THToBeCompleted.LastModifiedDate = DateTime.Now;
                await _unitOfWork.TransactionHeader.UpdateAsy(THToBeCompleted);
                await _unitOfWork.SaveAsy();
                return new JsonResult(new { success = true, message = "Transaction completed successfully" });
            }
        }

        //AJAX CALL for marking transaction as delivered
        public async Task<IActionResult> OnPostDeliverStatus(int id)//The route is /User/TransactionHeaders/Index?handler=DeliverStatus&id=1
        {
            var THToBeCompleted = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id && o.IsActive == true);
            if (THToBeCompleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while marking as delivered" });
            }

            if (THToBeCompleted.Status != SD.Status_Job_Completed)
            {
                return new JsonResult(new { success = false, message = "You can only mark a completed transaction as delivered" });
            }

            THToBeCompleted.Status = SD.Status_Job_Delivered;
            THToBeCompleted.DeliveredDate = DateTime.Now;
            THToBeCompleted.LastModifiedDate = DateTime.Now;
            await _unitOfWork.TransactionHeader.UpdateAsy(THToBeCompleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Transaction marked as delivered successfully" });
        }

        // Method to download the PDF report
        public async Task<IActionResult> OnGetDownloadPdf(int id)
        {
            var transactionHeader = await _unitOfWork.TransactionHeader.GetAsy(
                th => th.Id == id && th.IsActive == true,
                includeProperties: "DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model,DefectiveUnit.SerialNumber.Client,DefectiveUnit.SerialNumber.Client.ParentClient,User"
            );

            if (transactionHeader == null)
            {
                return NotFound();
            }

            var reportService = new TransactionReportService();
            var pdfBytes = reportService.GenerateTransactionReportPdf(transactionHeader);

            return File(pdfBytes, "application/pdf",
                $"TransactionReport_{transactionHeader.DefectiveUnit.SerialNumber?.Value}_{transactionHeader.Id}_{DateTime.Now:yyyy-MM-dd hh:mm tt}.pdf");
        }
    }
}