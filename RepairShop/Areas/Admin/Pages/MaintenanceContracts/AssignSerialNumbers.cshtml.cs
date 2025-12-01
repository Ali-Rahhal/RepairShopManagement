using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.Admin.Pages.MaintenanceContracts
{
    [Authorize(Roles = "Admin")]
    public class AssignSerialNumbersModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMaintenanceContractService _mcService;

        public AssignSerialNumbersModel(IUnitOfWork unitOfWork, IMaintenanceContractService mcService)
        {
            _unitOfWork = unitOfWork;
            _mcService = mcService;
        }

        [BindProperty(SupportsGet = true)]
        public long ContractId { get; set; }

        [BindProperty]
        public List<long> SelectedSerialNumberIds { get; set; } = new();

        public List<SerialNumberSelectDto> AvailableSerialNumbers { get; set; } = new();
        public List<SerialNumberSelectDto> AssignedSerialNumbers { get; set; } = new();

        public string ContractDisplayId => ContractId.ToString("D4");

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ContractId = id;

            var mc = await _unitOfWork.MaintenanceContract.GetAsy(m => m.Id == id && m.IsActive == true);
            if (mc == null) return NotFound();

            var (available, assigned) = await _mcService.LoadSerialNumbersForAssignmentAsync(mc.ClientId, id);
            AvailableSerialNumbers = available;
            AssignedSerialNumbers = assigned;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ContractId <= 0) return BadRequest();

            // call service to assign
            var (isValid, errorMsg) = await _mcService.AssignSerialNumbersAsync(ContractId, SelectedSerialNumberIds);
            if (!isValid)
            {
                ModelState.AddModelError("", errorMsg);

                // reload lists
                var (available, assigned) = await _mcService.LoadSerialNumbersForAssignmentAsync((await _unitOfWork.MaintenanceContract.GetAsy(m => m.Id == ContractId)).ClientId, ContractId);
                AvailableSerialNumbers = available;
                AssignedSerialNumbers = assigned;
                return Page();
            }

            TempData["success"] = "Serial numbers assigned successfully";
            return RedirectToPage("Upsert", new { id = ContractId });
        }
    }
}