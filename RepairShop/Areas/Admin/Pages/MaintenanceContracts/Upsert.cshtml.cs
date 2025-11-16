using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.MaintenanceContracts
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpsertModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public MaintenanceContract MaintenanceContractForUpsert { get; set; }

        [BindProperty]
        public List<int> SelectedSerialNumberIds { get; set; } = new();

        public IEnumerable<SelectListItem> ClientList { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            MaintenanceContractForUpsert = new MaintenanceContract
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddYears(1) // Default 1 year contract
            };

            // Populate dropdowns
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                MaintenanceContractForUpsert = await _unitOfWork.MaintenanceContract.GetAsy(
                    mc => mc.Id == id && mc.IsActive == true,
                    includeProperties: "Client"
                );

                if (MaintenanceContractForUpsert == null)
                {
                    return NotFound();
                }

                // Load currently assigned serial numbers
                var assignedSerialNumbers = await _unitOfWork.SerialNumber.GetAllAsy(
                    sn => sn.MaintenanceContractId == id && sn.IsActive == true
                );
                SelectedSerialNumberIds = assignedSerialNumbers.Select(sn => sn.Id).ToList();

                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                if (MaintenanceContractForUpsert == null)
                {
                    return NotFound();
                }

                // Validate dates
                if (MaintenanceContractForUpsert.EndDate <= MaintenanceContractForUpsert.StartDate)
                {
                    ModelState.AddModelError("MaintenanceContractForUpsert.EndDate", "End date must be after start date.");
                    await PopulateDropdowns();
                    return Page();
                }

                // Auto-set status based on current date
                MaintenanceContractForUpsert.Status = MaintenanceContractForUpsert.EndDate < DateTime.Now ? "Expired" : "Active";

                bool isNew = MaintenanceContractForUpsert.Id == 0;
                int contractId;

                if (isNew)
                {
                    await _unitOfWork.MaintenanceContract.AddAsy(MaintenanceContractForUpsert);
                    await _unitOfWork.SaveAsy();
                    contractId = MaintenanceContractForUpsert.Id;
                    TempData["success"] = "Maintenance contract created successfully";
                }
                else
                {
                    contractId = MaintenanceContractForUpsert.Id;
                    await _unitOfWork.MaintenanceContract.UpdateAsy(MaintenanceContractForUpsert);
                    TempData["success"] = "Maintenance contract updated successfully";
                }

                // Validate and assign selected serial numbers
                var validationResult = await ValidateAndAssignSerialNumbers(contractId, SelectedSerialNumberIds);
                if (!validationResult.isValid)
                {
                    ModelState.AddModelError("", validationResult.errorMessage);
                    await PopulateDropdowns();
                    return Page();
                }

                await _unitOfWork.SaveAsy();
                return RedirectToPage("Index");
            }

            await PopulateDropdowns();
            return Page();
        }

        // AJAX endpoint to search serial numbers
        public async Task<JsonResult> OnGetSearchSerialNumbers(int clientId, string searchTerm = "")
        {
            try
            {
                var serialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                    sn => sn.ClientId == clientId && 
                          sn.IsActive &&
                          (string.IsNullOrEmpty(searchTerm) || 
                           sn.Value.Contains(searchTerm) ||
                           (sn.Model != null && sn.Model.Name.Contains(searchTerm))),
                    includeProperties: "Model,MaintenanceContract"
                ))
                .OrderBy(sn => sn.Value)
                .Select(sn => {
                    var currentContract = sn.MaintenanceContract;
                    var isContractExpired = currentContract?.EndDate < DateTime.Now;
                    var hasActiveContract = currentContract != null && !isContractExpired;
                    var isAvailable = currentContract == null || isContractExpired;

                    return new
                    {
                        id = sn.Id,
                        value = sn.Value,
                        model = sn.Model?.Name ?? "Unknown Model",
                        receivedDate = sn.ReceivedDate.ToString("yyyy-MM-dd"),
                        currentContract = currentContract != null ? $"Contract-{currentContract.Id:D4}" : "None",
                        hasActiveContract = hasActiveContract,
                        isAvailable = isAvailable,
                        contractStatus = currentContract?.Status ?? "No Contract",
                        contractEndDate = currentContract?.EndDate.ToString("yyyy-MM-dd") ?? "N/A"
                    };
                })
                .Take(100) // Limit results for performance
                .ToList();

                return new JsonResult(serialNumbers);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = "An error occurred while searching serial numbers" });
            }
        }

        private async Task PopulateDropdowns()
        {
            // Populate Clients dropdown
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true))
                .OrderBy(c => c.Name)
                .ToList();

            ClientList = clients.Select(c => new SelectListItem
            {
                Text = $"{c.Name}{(c.Branch != null ? $" - {c.Branch}" : "")}",
                Value = c.Id.ToString()
            });
        }

        private async Task<(bool isValid, string errorMessage)> ValidateAndAssignSerialNumbers(int contractId, List<int> selectedSerialNumberIds)
        {
            if (selectedSerialNumberIds == null || selectedSerialNumberIds.Count == 0)
                return (true, "");

            var errorMessages = new List<string>();
            var validSerialNumberIds = new List<int>();

            foreach (var serialNumberId in selectedSerialNumberIds)
            {
                var serialNumber = await _unitOfWork.SerialNumber.GetAsy(
                    sn => sn.Id == serialNumberId && sn.IsActive == true,
                    includeProperties: "MaintenanceContract"
                );

                if (serialNumber == null)
                {
                    errorMessages.Add($"Serial number with ID {serialNumberId} not found.");
                    continue;
                }

                // Check if serial number is available for assignment
                if (serialNumber.MaintenanceContractId.HasValue && serialNumber.MaintenanceContractId != contractId)
                {
                    var currentContract = serialNumber.MaintenanceContract;
                    if (currentContract != null && currentContract.EndDate >= DateTime.Now)
                    {
                        errorMessages.Add($"Serial number {serialNumber.Value} is currently covered by an active maintenance contract (Contract-{currentContract.Id:D4}) that ends on {currentContract.EndDate:yyyy-MM-dd}.");
                        continue;
                    }
                }

                validSerialNumberIds.Add(serialNumberId);
            }

            if (errorMessages.Any())
            {
                return (false, string.Join(" ", errorMessages));
            }

            // First, remove all serial numbers from this contract
            var existingSerialNumbers = await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.MaintenanceContractId == contractId && sn.IsActive == true,
                tracked: true
            );

            foreach (var serialNumber in existingSerialNumbers)
            {
                serialNumber.MaintenanceContractId = null;
                await _unitOfWork.SerialNumber.UpdateAsy(serialNumber);
            }

            // Then assign the valid serial numbers
            foreach (var serialNumberId in validSerialNumberIds)
            {
                var serialNumber = await _unitOfWork.SerialNumber.GetAsy(sn => sn.Id == serialNumberId && sn.IsActive == true, tracked: true);
                if (serialNumber != null)
                {
                    serialNumber.MaintenanceContractId = contractId;
                    await _unitOfWork.SerialNumber.UpdateAsy(serialNumber);
                }
            }

            return (true, "");
        }
    }
}