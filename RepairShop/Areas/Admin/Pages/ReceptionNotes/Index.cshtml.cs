using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.Admin.Pages.ReceptionNotes
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

        #region DataTable

        public async Task<JsonResult> OnPostAll(
            int draw,
            int start = 0,
            int length = 10)
        {
            try
            {
                var search = Request.Form["search[value]"].FirstOrDefault();

                var orderColumn =
                    Request.Form["order[0][column]"].FirstOrDefault();

                var orderDir =
                    Request.Form["order[0][dir]"].FirstOrDefault();

                var query = await _unitOfWork.ReceptionNote.GetQueryableAsy(
                    rn => rn.IsActive,
                    includeProperties:
                        "Client,Items");

                var recordsTotal = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();

                    query = query.Where(r =>
                        r.Code.ToLower().Contains(search) ||
                        r.Client.Name.ToLower().Contains(search));
                }

                var recordsFiltered = await query.CountAsync();

                query = orderColumn switch
                {
                    "0" => orderDir == "asc"
                        ? query.OrderBy(x => x.Code)
                        : query.OrderByDescending(x => x.Code),

                    "1" => orderDir == "asc"
                        ? query.OrderBy(x => x.Client.Name)
                        : query.OrderByDescending(x => x.Client.Name),

                    "2" => orderDir == "asc"
                        ? query.OrderBy(x => x.Items.Count)
                        : query.OrderByDescending(x => x.Items.Count),

                    "3" => orderDir == "asc"
                        ? query.OrderBy(x => x.CreatedDate)
                        : query.OrderByDescending(x => x.CreatedDate),

                    "4" => orderDir == "asc"
                        ? query.OrderBy(x => x.IsPrinted)
                        : query.OrderByDescending(x => x.IsPrinted),

                    _ => query.OrderByDescending(x => x.CreatedDate)
                };

                var data = await query
                    .Skip(start)
                    .Take(length)
                    .Select(x => new
                    {
                        id = x.Id,

                        code = x.Code,

                        clientName =
                            x.Client.ParentClient != null
                            ? x.Client.ParentClient.Name
                            : x.Client.Name,

                        deviceCount = x.Items.Count,

                        createdDate = x.CreatedDate,

                        isPrinted = x.IsPrinted
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

        #endregion

        #region Modal

        public async Task<IActionResult> OnGetDetails(long id)
        {
            var receptionNote =
                await _unitOfWork.ReceptionNote.GetAsy(
                    x => x.Id == id && x.IsActive,
                    includeProperties:
                    "Items,Items.SerialNumber,Items.SerialNumber.Model");

            if (receptionNote == null)
            {
                return new JsonResult(new List<object>());
            }

            var result = receptionNote.Items
                .Where(x => x.IsActive)
                .OrderBy(x => x.SerialNumber.Value)
                .Select((x, index) => new
                {
                    number = index + 1,

                    serialNumber = x.SerialNumber.Value,

                    model = x.SerialNumber.Model.Name
                });

            return new JsonResult(result);
        }

        #endregion

        #region Print

        public async Task<IActionResult> OnGetPrint(long id, [FromServices] ReceptionNoteReportService _reportService)
        {
            var receptionNote =
                await _unitOfWork.ReceptionNote.GetAsy(
                    x => x.Id == id && x.IsActive,
                    includeProperties:
                    "Client," +
                    "Items," +
                    "Items.SerialNumber," +
                    "Items.SerialNumber.Model");

            if (receptionNote == null)
                return NotFound();

            if (!receptionNote.IsPrinted)
            {
                var noteToUpdate = await _unitOfWork.ReceptionNote.GetAsy(
                    r => r.Id == id,
                    tracked: true
                );
                noteToUpdate.IsPrinted = true;

                await _unitOfWork.ReceptionNote.UpdateAsy(noteToUpdate);

                await _unitOfWork.SaveAsy();
            }

            var pdf =
                _reportService.GenerateReceptionNotePdf(receptionNote);

            return File(
                pdf,
                "application/pdf",
                $"{receptionNote.Code}.pdf");
        }

        #endregion
    }
}
