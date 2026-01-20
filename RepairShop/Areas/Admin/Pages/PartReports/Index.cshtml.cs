using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using static RepairShop.Models.Helpers.PartStockHistoryVMs;

namespace RepairShop.Areas.Admin.Pages.PartReports
{
    [Authorize(Roles = SD.Role_Admin)]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PartUsageReportVM Report { get; set; }

        public void OnGet() { }

        public async Task<PartialViewResult> OnPostGenerateReport(
            long partId,
            DateTime startDate,
            DateTime endDate)
        {
            var part = await _unitOfWork.Part.GetAsy(p => p.Id == partId);
            if (part == null) return Partial("_ReportResult", null);

            var historyQuery = await _unitOfWork.PartStockHistory
                .GetQueryableAsy(h => h.PartId == partId && h.CreatedDate <= endDate, includeProperties: "Part,User,TransactionBody,TransactionBody.TransactionHeader,TransactionBody.TransactionHeader.DefectiveUnit,TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber,TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client,TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Model");
            historyQuery = historyQuery.OrderBy(h => h.CreatedDate);

            var qtyAtStart = await historyQuery
                .Where(h => h.CreatedDate >= startDate)
                .OrderBy(h => h.CreatedDate)
                .Select(h => h.QuantityAfter)
                .FirstOrDefaultAsync();

            var qtyAtEnd = await historyQuery
                .OrderByDescending(h => h.CreatedDate)
                .Select(h => h.QuantityAfter)
                .FirstOrDefaultAsync();

            var devicesCount = (await _unitOfWork.TransactionBody
                .GetAllAsy(tb => tb.PartId == partId &&
                                    (tb.Status == SD.Status_Part_Pending_Replace || tb.Status == SD.Status_Part_Replaced) &&
                                    tb.CreatedDate >= startDate &&
                                    tb.CreatedDate <= endDate &&
                                    tb.IsActive == true))
                .Count();

            Report = new PartUsageReportVM
            {
                PartId = part.Id,
                PartName = part.Name,
                StartDate = startDate,
                EndDate = endDate,
                QuantityAtStart = qtyAtStart,
                QuantityAtEnd = qtyAtEnd,
                DevicesCount = devicesCount,
                History = historyQuery
                    .Where(h => h.CreatedDate >= startDate)
                    .Select(h => new PartStockHistoryRowVM
                    {
                        UserName = h.User != null ? h.User.UserName : null,
                        Date = h.CreatedDate,
                        ClientName = h.TransactionBody != null &&
                                 h.TransactionBody.TransactionHeader != null &&
                                 h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                                 h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null &&
                                 h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client != null
                                 ? h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client.Name
                                 : null,
                        SerialNumber = h.TransactionBody != null &&
                                   h.TransactionBody.TransactionHeader != null &&
                                   h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                                   h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null
                                   ? h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Value
                                   : null,
                        ModelName = h.TransactionBody != null &&
                                h.TransactionBody.TransactionHeader != null &&
                                h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                                h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null &&
                                h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Model != null
                                ? h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Model.Name
                                : null,
                        QuantityChange = h.QuantityChange,
                        QuantityAfter = h.QuantityAfter,
                        Reason = h.Reason
                    }).ToList()
            };

            return Partial("_ReportResult", Report);
        }

        // NEW: Server-side DataTable endpoint for all movements
        public async Task<JsonResult> OnPostAllMovements(
            int draw,
            int start = 0,
            int length = 10,
            string? partName = null,
            string? clientName = null,
            string? movementType = null)
        {
            var search = Request.Form["search[value]"].ToString();
            var orderColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
            var orderDir = Request.Form["order[0][dir]"].FirstOrDefault() ?? "asc";

            // Base query
            var query = await _unitOfWork.PartStockHistory
                .GetQueryableAsy(includeProperties: "Part,User,TransactionBody,TransactionBody.TransactionHeader,TransactionBody.TransactionHeader.DefectiveUnit,TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber,TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client,TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Model");

            var recordsTotal = await query.CountAsync();

            // Global search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(h =>
                    h.Part.Name.ToLower().Contains(search) ||
                    (h.Part.Category ?? "").ToLower().Contains(search) ||
                    h.Reason.ToLower().Contains(search) ||
                    (h.TransactionBody != null &&
                     h.TransactionBody.TransactionHeader != null &&
                     h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                     h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null &&
                     h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client != null &&
                     h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client.Name.ToLower().Contains(search)) ||
                    (h.TransactionBody != null &&
                     h.TransactionBody.TransactionHeader != null &&
                     h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                     h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null &&
                     h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Value.ToLower().Contains(search)));
            }

            // Individual filters
            if (!string.IsNullOrWhiteSpace(partName) && partName != "All")
            {
                query = query.Where(h => h.Part.Name.Contains(partName));
            }

            if (!string.IsNullOrWhiteSpace(clientName) && clientName != "All")
            {
                query = query.Where(h =>
                    h.TransactionBody != null &&
                    h.TransactionBody.TransactionHeader != null &&
                    h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                    h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null &&
                    h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client != null &&
                    h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client.Name.Contains(clientName));
            }

            if (!string.IsNullOrWhiteSpace(movementType))
            {
                if (movementType == "positive")
                    query = query.Where(h => h.QuantityChange > 0);
                else if (movementType == "negative")
                    query = query.Where(h => h.QuantityChange < 0);
            }

            var recordsFiltered = await query.CountAsync();

            // Server-side ordering
            query = orderColumnIndex switch
            {
                "0" => orderDir == "asc" ? query.OrderBy(h => (h.User != null ? h.User.UserName : null)) : query.OrderByDescending(h => (h.User != null ? h.User.UserName : null)),
                "1" => orderDir == "asc" ? query.OrderBy(h => h.CreatedDate) : query.OrderByDescending(h => h.CreatedDate),
                "2" => orderDir == "asc" ? query.OrderBy(h => h.Part.Name) : query.OrderByDescending(h => h.Part.Name),
                "3" => orderDir == "asc" ? query.OrderBy(h => h.Part.Category) : query.OrderByDescending(h => h.Part.Category),
                "4" => orderDir == "asc" ? query.OrderBy(h => h.QuantityChange) : query.OrderByDescending(h => h.QuantityChange),
                "5" => orderDir == "asc" ? query.OrderBy(h => h.QuantityAfter) : query.OrderByDescending(h => h.QuantityAfter),
                _ => query.OrderByDescending(h => h.CreatedDate) // default: newest first
            };

            // Pagination
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(h => new
                {
                    user = h.User != null ? h.User.UserName : null,
                    date = h.CreatedDate,
                    partId = h.PartId,
                    partName = h.Part.Name,
                    category = h.Part.Category ?? "Uncategorized",
                    quantityChange = h.QuantityChange,
                    quantityAfter = h.QuantityAfter,
                    clientName = h.TransactionBody != null &&
                                 h.TransactionBody.TransactionHeader != null &&
                                 h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                                 h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null &&
                                 h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client != null
                                 ? h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Client.Name
                                 : null,
                    serialNumber = h.TransactionBody != null &&
                                   h.TransactionBody.TransactionHeader != null &&
                                   h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                                   h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null
                                   ? h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Value
                                   : null,
                    modelName = h.TransactionBody != null &&
                                h.TransactionBody.TransactionHeader != null &&
                                h.TransactionBody.TransactionHeader.DefectiveUnit != null &&
                                h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber != null &&
                                h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Model != null
                                ? h.TransactionBody.TransactionHeader.DefectiveUnit.SerialNumber.Model.Name
                                : null,
                    reason = h.Reason
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

        public async Task<JsonResult> OnGetParts()
        {
            var parts = (await _unitOfWork.Part.GetAllAsy(p => p.IsActive))
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    value = p.Id.ToString(),
                    text = $"{p.Name} ({p.Category ?? "Uncategorized"})"
                });

            return new JsonResult(parts);
        }

        // NEW: Get filter dropdown data
        public async Task<JsonResult> OnGetMovementFilters()
        {
            var parts = (await _unitOfWork.Part.GetAllAsy(p => p.IsActive))
                .Select(p => new { value = p.Name, text = $"{p.Name} ({p.Category ?? "Uncategorized"})" })
                .DistinctBy(p => p.value)
                .OrderBy(p => p.text)
                .ToList();

            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive))
                .Select(c => new { value = c.Name, text = c.Name })
                .DistinctBy(c => c.value)
                .OrderBy(c => c.text)
                .ToList();

            return new JsonResult(new
            {
                parts,
                clients
            });
        }
    }
}