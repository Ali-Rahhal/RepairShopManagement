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

        public async Task<JsonResult> OnGetParts()
        {
            var parts = (await _unitOfWork.Part.GetAllAsy())
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    id = p.Id,
                    text = $"{p.Name} ({p.Category ?? "Uncategorized"})"
                });

            return new JsonResult(parts);
        }

        public async Task<PartialViewResult> OnPostGenerateReport(
            long partId,
            DateTime startDate,
            DateTime endDate)
        {
            var part = await _unitOfWork.Part.GetAsy(p => p.Id == partId);
            if (part == null) return Partial("_ReportResult", null);

            var historyQuery = await _unitOfWork.PartStockHistory
                .GetQueryableAsy(h => h.PartId == partId && h.CreatedDate <= endDate);
            historyQuery = historyQuery.OrderBy(h => h.CreatedDate);

            var qtyAtStart = await historyQuery
                .Where(h => h.CreatedDate >= startDate)
                .OrderBy(h => h.CreatedDate)
                .Select(h => h.QuantityAfter)
                .FirstOrDefaultAsync(); // Use FirstOrDefaultAsync for async query

            var qtyAtEnd = await historyQuery
                .OrderByDescending(h => h.CreatedDate)
                .Select(h => h.QuantityAfter)
                .FirstOrDefaultAsync(); // Use FirstOrDefaultAsync for async query

            // For devices count - CORRECT VERSION:
            var devicesCount = (await _unitOfWork.TransactionBody
                .GetAllAsy(tb => tb.PartId == partId &&
                                    (tb.Status == SD.Status_Part_Pending_Replace || tb.Status == SD.Status_Part_Replaced) &&
                                    tb.CreatedDate >= startDate &&
                                    tb.CreatedDate <= endDate))
                .Count(); // Use CountAsync for async query

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
                        Date = h.CreatedDate,
                        QuantityChange = h.QuantityChange,
                        QuantityAfter = h.QuantityAfter,
                        Reason = h.Reason
                    }).ToList()
            };

            return Partial("_ReportResult", Report);
        }
    }
}
