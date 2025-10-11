using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        public int ClientId { get; set; }
        public void OnGet(int? id = null)
        {
            ClientId = id.GetValueOrDefault();
        }

        //AJAX CALLS for getting all serial numbers of a client in Json format for DataTables
        public async Task<JsonResult> OnGetAll(int? CLientId)//The route is /User/Clients/Index?handler=All&ClientId=1
        {
            var clientSNList = (await _unitOfWork.SerialNumber
                .GetAllAsy(sn => sn.IsActive == true && sn.ClientId == CLientId,
                            includeProperties: "Model,MaintenanceContract")).ToList();

            var formattedList = clientSNList.Select(sn => new
            {
                value = sn.Value,
                modelName = sn.Model.Name,
                contractNumber = sn.MaintenanceContract != null ? $"Contract {sn.MaintenanceContract.Id:D4}" : "No Contract"
            });

            return new JsonResult(new { data = formattedList });//We return JsonResult because we will call this method using AJAX
        }
    }
}
