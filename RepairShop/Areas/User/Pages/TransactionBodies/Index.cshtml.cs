using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using System.Security.Claims;

namespace RepairShop.Areas.User.Pages.TransactionBodies
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(IUnitOfWork unitOfWork, IHttpContextAccessor hca)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = hca;
        }

        [BindProperty(SupportsGet = true)]
        public int HeaderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string HeaderStatus { get; set; }

        public void OnGet(int HeaderId)
        {
            this.HeaderId = HeaderId;
            HeaderStatus = _unitOfWork.TransactionHeader.GetAsy(o => o.Id == HeaderId && o.IsActive == true).Result.Status;
        }

        // Updated with server-side pagination, filtering and ordering
        public async Task<JsonResult> OnGetAll(
            int draw,
            int start = 0,
            int length = 10)
        {
            var search = Request.Query["search[value]"].ToString();
            var orderColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
            var orderDir = Request.Query["order[0][dir]"].FirstOrDefault() ?? "asc";

            var headerIdParam = Request.Query["headerId"].FirstOrDefault();
            if (!int.TryParse(headerIdParam, out int headerId))
            {
                return new JsonResult(new
                {
                    draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>()
                });
            }

            // Base query
            var query = await _unitOfWork.TransactionBody
                .GetQueryableAsy(t => t.IsActive == true && t.TransactionHeaderId == headerId,
                            includeProperties: "Part");

            var recordsTotal = await query.CountAsync();

            // Global search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(tb =>
                    tb.BrokenPartName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    tb.Status.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (tb.Part != null && tb.Part.Name.ToLower().Contains(search)));
            }

            var recordsFiltered = await query.CountAsync();

            // Server-side ordering
            query = orderColumnIndex switch
            {
                "0" => orderDir == "asc" ? query.OrderBy(tb => tb.BrokenPartName) : query.OrderByDescending(tb => tb.BrokenPartName),
                "1" => orderDir == "asc" ? query.OrderBy(tb => tb.Status) : query.OrderByDescending(tb => tb.Status),
                "2" => orderDir == "asc" ? query.OrderBy(tb => tb.Part != null ? tb.Part.Name : "") : query.OrderByDescending(tb => tb.Part != null ? tb.Part.Name : ""),
                "3" => orderDir == "asc" ? query.OrderBy(tb => tb.CreatedDate) : query.OrderByDescending(tb => tb.CreatedDate),
                _ => query.OrderByDescending(tb => tb.CreatedDate) // default: newest first
            };

            // Pagination
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(tb => new
                {
                    id = tb.Id,
                    brokenPartName = tb.BrokenPartName,
                    status = tb.Status,
                    partName = tb.Part != null ? tb.Part.Name : "N/A",
                    createdDate = tb.CreatedDate,
                    waitingPartDate = tb.WaitingPartDate,
                    fixedDate = tb.FixedDate,
                    replacedDate = tb.ReplacedDate,
                    notRepairableDate = tb.NotRepairableDate,
                    notReplaceableDate = tb.NotReplaceableDate
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

        //AJAX CALL for checking if part is availabe and change status
        public async Task<IActionResult> OnGetCheckPart(int? id)
        {
            var TB = await _unitOfWork.TransactionBody.GetAsy(tb => tb.Id == id && tb.IsActive == true);
            var part = await _unitOfWork.Part.GetAsy(p => p.Id == TB.PartId && p.IsActive == true);
            if (part.Quantity > 0)
            {
                part.Quantity--;
                await _unitOfWork.Part.UpdateAsy(part);
                TB.Status = SD.Status_Part_Pending_Replace;
                await _unitOfWork.TransactionBody.UpdateAsy(TB);
                await _unitOfWork.SaveAsy();
                await _unitOfWork.PartStockHistory.AddAsy(new PartStockHistory
                {
                    UserId = _httpContextAccessor.HttpContext?.User?
                                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? null,
                    PartId = part.Id,
                    QuantityChange = -1,   // initial stock
                    QuantityAfter = part.Quantity,
                    TransactionBodyId = TB.Id,
                    Reason = $"Used in repair (Transaction #{TB.TransactionHeaderId})",
                    CreatedDate = DateTime.Now
                });

                await _unitOfWork.SaveAsy();
                return new JsonResult(new { success = true, message = "Part is available and it has been selected for replacement" });
            }
            
            return new JsonResult(new { success = false, message = "Part is still not available" });
        }//The route is /User/TransactionBodies/Index?handler=CheckPart&id=1

        //AJAX CALL for changing TB status
        public async Task<IActionResult> OnGetStatus(int? id, int? choice)//The route is /User/TransactionBodies/Index?handler=Status&id=1
        {
            var TB = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == id && o.IsActive == true);
            if (TB == null)
            {
                return new JsonResult(new { success = false, message = "Error while changing status" });
            }

            // Handle different choices
            switch (choice)
            {
                case 0: // Not Repairable
                    TB.Status = SD.Status_Part_NotRepairable;
                    TB.NotRepairableDate = DateTime.Now;
                    break;
                case 1: // Fixed
                    TB.Status = SD.Status_Part_Fixed;
                    TB.FixedDate = DateTime.Now;
                    break;
                case 2: // Not Replaceable
                    TB.Status = SD.Status_Part_NotReplaceable;
                    TB.NotReplaceableDate = DateTime.Now;

                    // If not replaceable and a part was selected, increment inventory
                    if (TB.PartId.HasValue)
                    {
                        var replacementPart = await _unitOfWork.Part.GetAsy(p => p.Id == TB.PartId.Value && p.IsActive == true);
                        if (replacementPart != null && replacementPart.Quantity >= 0)
                        {
                            replacementPart.Quantity++;
                            await _unitOfWork.Part.UpdateAsy(replacementPart);
                            await _unitOfWork.PartStockHistory.AddAsy(new PartStockHistory
                            {
                                UserId = _httpContextAccessor.HttpContext?.User?
                                            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? null,
                                PartId = replacementPart.Id,
                                QuantityChange = 1,   // initial stock
                                QuantityAfter = replacementPart.Quantity,
                                TransactionBodyId = TB.Id,
                                Reason = "Returned to stock (Device irreparable)",
                                CreatedDate = DateTime.Now
                            });
                        }
                    }
                    break;
                case 3: // Replaced
                    TB.Status = SD.Status_Part_Replaced;
                    TB.ReplacedDate = DateTime.Now;
                    break;
                default:
                    return new JsonResult(new { success = false, message = "Invalid choice" });
            }

            await _unitOfWork.TransactionBody.UpdateAsy(TB);
            await _unitOfWork.SaveAsy();
            // Update TransactionHeader last modified date
            var HeaderForBody = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == TB.TransactionHeaderId && o.IsActive == true, tracked: true);
            if (HeaderForBody != null)
            {
                HeaderForBody.LastModifiedDate = DateTime.Now;
                await _unitOfWork.TransactionHeader.UpdateAsy(HeaderForBody);
                await _unitOfWork.SaveAsy();
            }

            return new JsonResult(new { success = true, message = "Status changed successfully" });
        }

        //AJAX CALL for deleting a TB
        public async Task<IActionResult> OnGetDelete(int? id, [FromServices] DeleteService _dlt)//The route is /User/TransactionBodies/Index?handler=Delete&id=1
        {

            var TBToBeDeleted = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == id && o.IsActive == true);
            if (TBToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            await _dlt.DeleteTransactionBodyAsync(TBToBeDeleted.Id);    

            // Update TransactionHeader last modified date
            var HeaderForBody = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == TBToBeDeleted.TransactionHeaderId && o.IsActive == true, tracked: true);
            if (HeaderForBody != null)
            {
                HeaderForBody.LastModifiedDate = DateTime.Now;
                await _unitOfWork.TransactionHeader.UpdateAsy(HeaderForBody);
                await _unitOfWork.SaveAsy();
            }

            return new JsonResult(new { success = true, message = "Part deleted successfully" });
        }
    }
}
