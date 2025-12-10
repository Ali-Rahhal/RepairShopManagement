using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        //AJAX CALLS for getting all branches of a client in Json format for DataTables
        public async Task<JsonResult> OnGetAll(int? ParentId)//The route is /User/Clients/ClientBranchIndex?handler=All&ParentId=1
        {
            var clientBranchList = (await _unitOfWork.Client
                .GetAllAsy(sn => sn.IsActive == true && sn.ParentClientId == ParentId))
                .ToList();

            var formattedList = clientBranchList.Select(b => new
            {
                id = b.Id,
                branchName = b.Name,
                phone = b.Phone,
                email = b.Email,
                address = b.Address
            });

            return new JsonResult(new { data = formattedList });//We return JsonResult because we will call this method using AJAX
        }
    }
}
