using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.User.Pages.Clients
{
    public class ClientSNIndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        public ClientSNIndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public long ClientId { get; set; }

        [BindProperty]
        public long ParentId { get; set; }

        public void OnGet(long? id = null, long parentId = 0)
        {
            if (parentId > 0)
            {
                ParentId = parentId;
            }

            ClientId = id.GetValueOrDefault();
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

            var clientIdParam = Request.Query["ClientId"].FirstOrDefault();
            if (!long.TryParse(clientIdParam, out long clientId))
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
            var query = await _unitOfWork.SerialNumber
                .GetQueryableAsy(sn => sn.IsActive == true && sn.ClientId == clientId,
                            includeProperties: "Model,MaintenanceContract");

            var recordsTotal = await query.CountAsync();

            // Global search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(sn =>
                    sn.Value.ToLower().Contains(search) ||
                    sn.Model.Name.ToLower().Contains(search) ||
                    (sn.MaintenanceContract != null && ("Contract " + sn.MaintenanceContract.Id).ToLower().Contains(search)));
            }

            var recordsFiltered = await query.CountAsync();

            // Server-side ordering
            query = orderColumnIndex switch
            {
                "0" => orderDir == "asc" ? query.OrderBy(sn => sn.Value) : query.OrderByDescending(sn => sn.Value),
                "1" => orderDir == "asc" ? query.OrderBy(sn => sn.Model.Name) : query.OrderByDescending(sn => sn.Model.Name),
                "2" => orderDir == "asc" ? query.OrderBy(sn => sn.MaintenanceContract != null ? sn.MaintenanceContract.Id : 0)
                                         : query.OrderByDescending(sn => sn.MaintenanceContract != null ? sn.MaintenanceContract.Id : 0),
                _ => query.OrderBy(sn => sn.Value) // default ordering by serial number value
            };

            // Pagination
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(sn => new
                {
                    value = sn.Value,
                    modelName = sn.Model.Name,
                    contractNumber = sn.MaintenanceContract != null ? $"Contract {sn.MaintenanceContract.Id:D4}" : "No Contract"
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
    }
}