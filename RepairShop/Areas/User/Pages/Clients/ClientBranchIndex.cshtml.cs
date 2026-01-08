using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.User.Pages.Clients
{
    public class ClientBranchIndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        public ClientBranchIndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public int ParentId { get; set; }
        public void OnGet(int? id = null)
        {
            ParentId = id.GetValueOrDefault();
        }

        // Updated with server-side pagination
        public async Task<JsonResult> OnGetAll(
            int draw,
            int start = 0,
            int length = 10)
        {
            var search = Request.Query["search[value]"].ToString();
            var orderColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
            var orderDir = Request.Query["order[0][dir]"].FirstOrDefault() ?? "asc";

            var parentId = Request.Query["ParentId"].FirstOrDefault();
            if (!int.TryParse(parentId, out int parentIdInt))
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
            var query = await _unitOfWork.Client
                .GetQueryableAsy(sn => sn.IsActive == true && sn.ParentClientId == parentIdInt);

            var recordsTotal = await query.CountAsync();

            // Global search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(b =>
                    b.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (b.Phone ?? "").Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (b.Email ?? "").Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (b.Address ?? "").Contains(search, StringComparison.CurrentCultureIgnoreCase));
            }

            var recordsFiltered = await query.CountAsync();

            // Server-side ordering
            query = orderColumnIndex switch
            {
                "0" => orderDir == "asc" ? query.OrderBy(b => b.Name) : query.OrderByDescending(b => b.Name),
                "1" => orderDir == "asc" ? query.OrderBy(b => b.Phone) : query.OrderByDescending(b => b.Phone),
                "2" => orderDir == "asc" ? query.OrderBy(b => b.Email) : query.OrderByDescending(b => b.Email),
                "3" => orderDir == "asc" ? query.OrderBy(b => b.Address) : query.OrderByDescending(b => b.Address),
                _ => query.OrderBy(b => b.Name) // default
            };

            // Pagination
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(b => new
                {
                    id = b.Id,
                    branchName = b.Name,
                    phone = b.Phone,
                    email = b.Email,
                    address = b.Address
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