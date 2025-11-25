using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Warranties
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
        public Warranty WarrantyForUpsert { get; set; }

        // Properties for bulk serial number creation
        [BindProperty]
        public string NewSerialNumbersInput { get; set; }

        [BindProperty]
        public int SelectedModelId { get; set; }

        [BindProperty]
        public int SelectedClientId { get; set; }

        [BindProperty]
        public int? SelectedMaintenanceContractId { get; set; }

        public IEnumerable<SelectListItem> ModelList { get; set; }
        public IEnumerable<SelectListItem> ClientList { get; set; }
        public IEnumerable<SelectListItem> MaintenanceContractList { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            // Populate dropdowns
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                // Create new warranty
                WarrantyForUpsert = new Warranty
                {
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(1), // Default 1 year warranty
                    SerialNumbers = new List<SerialNumber>()
                };
                return Page();
            }
            else
            {
                // Edit existing warranty
                WarrantyForUpsert = await _unitOfWork.Warranty.GetAsy(
                    w => w.Id == id && w.IsActive == true,
                    includeProperties: "SerialNumbers,SerialNumbers.Model,SerialNumbers.Client,SerialNumbers.MaintenanceContract",
                    tracked: true
                );

                if (WarrantyForUpsert == null)
                {
                    return NotFound();
                }

                // If editing, populate the maintenance contracts for the client of the first serial number
                if (WarrantyForUpsert.SerialNumbers?.Any() == true)
                {
                    var firstSerial = WarrantyForUpsert.SerialNumbers.First();
                    await PopulateMaintenanceContracts(firstSerial.ClientId);
                }

                return Page();
            }
        }

        // AJAX endpoint to get maintenance contracts for a client
        public async Task<JsonResult> OnGetMaintenanceContractsByClient(int clientId)
        {
            await PopulateMaintenanceContracts(clientId);
            return new JsonResult(MaintenanceContractList);
        }

        public async Task<IActionResult> OnPost()
        {
            if (WarrantyForUpsert.Id != 0)
            {
                ModelState.Remove("NewSerialNumbersInput");
            }
            if (ModelState.IsValid)
            {
                if (WarrantyForUpsert == null)
                {
                    return NotFound();
                }

                // Validate dates
                if (WarrantyForUpsert.EndDate <= WarrantyForUpsert.StartDate)
                {
                    ModelState.AddModelError("WarrantyForUpsert.EndDate", "End date must be after start date.");
                    await PopulateDropdowns();
                    await PopulateMaintenanceContracts(SelectedClientId);
                    return Page();
                }

                // Set status based on dates
                WarrantyForUpsert.Status = WarrantyForUpsert.EndDate < DateTime.Now ? "Expired" : "Active";

                if (WarrantyForUpsert.Id == 0)
                {
                    // CREATE NEW WARRANTY WITH BULK SERIAL NUMBERS
                    if (string.IsNullOrWhiteSpace(NewSerialNumbersInput))
                    {
                        ModelState.AddModelError("NewSerialNumbersInput", "Please enter at least one serial number.");
                        await PopulateDropdowns();
                        await PopulateMaintenanceContracts(SelectedClientId);
                        return Page();
                    }

                    if (SelectedModelId == 0)
                    {
                        ModelState.AddModelError("SelectedModelId", "Please select a model.");
                        await PopulateDropdowns();
                        await PopulateMaintenanceContracts(SelectedClientId);
                        return Page();
                    }

                    if (SelectedClientId == 0)
                    {
                        ModelState.AddModelError("SelectedClientId", "Please select a client.");
                        await PopulateDropdowns();
                        await PopulateMaintenanceContracts(SelectedClientId);
                        return Page();
                    }

                    // Parse serial numbers using SPACES as separators
                    var serialNumberValues = NewSerialNumbersInput
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(sn => sn.Trim())
                        .Where(sn => !string.IsNullOrWhiteSpace(sn))
                        .Distinct()
                        .ToList();

                    if (serialNumberValues.Count == 0)
                    {
                        ModelState.AddModelError("NewSerialNumbersInput", "Please enter valid serial numbers separated by spaces.");
                        await PopulateDropdowns();
                        await PopulateMaintenanceContracts(SelectedClientId);
                        return Page();
                    }

                    // Check if serial numbers are btw 3 and 40 characters
                    foreach (var sn in serialNumberValues)
                    {
                        if (sn.Length < 3 || sn.Length > 40)
                        {
                            ModelState.AddModelError("NewSerialNumbersInput", "Serial numbers must be between 3 and 40 characters.");
                            await PopulateDropdowns();
                            await PopulateMaintenanceContracts(SelectedClientId);
                            return Page();
                        }
                    }

                    // Check for duplicate serial numbers
                    var existingSerialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                        sn => serialNumberValues.Contains(sn.Value) && sn.IsActive == true
                    )).ToList();

                    if (existingSerialNumbers.Count != 0)
                    {
                        var duplicateNumbers = string.Join(", ", existingSerialNumbers.Select(sn => sn.Value));
                        ModelState.AddModelError("NewSerialNumbersInput",
                            $"The following serial numbers already exist: {duplicateNumbers}. Please use unique serial numbers.");
                        await PopulateDropdowns();
                        await PopulateMaintenanceContracts(SelectedClientId);
                        return Page();
                    }

                    // Create new serial numbers
                    var newSerialNumbers = new List<SerialNumber>();
                    foreach (var serialNumberValue in serialNumberValues)
                    {
                        var serialNumber = new SerialNumber
                        {
                            Value = serialNumberValue,
                            ModelId = SelectedModelId,
                            ClientId = SelectedClientId,
                            MaintenanceContractId = SelectedMaintenanceContractId,
                            ReceivedDate = DateTime.Now,
                            IsActive = true
                        };
                        newSerialNumbers.Add(serialNumber);
                    }

                    // Create warranty with serial numbers
                    WarrantyForUpsert.SerialNumbers = newSerialNumbers;
                    await _unitOfWork.Warranty.AddAsy(WarrantyForUpsert);
                    TempData["success"] = $"Warranty created successfully with {newSerialNumbers.Count} serial number(s)";
                }
                else
                {
                    // UPDATE EXISTING WARRANTY (serial numbers remain unchanged)
                    await _unitOfWork.Warranty.UpdateAsy(WarrantyForUpsert);
                    TempData["success"] = "Warranty updated successfully";
                }

                await _unitOfWork.SaveAsy();
                return RedirectToPage("Index");
            }

            await PopulateDropdowns();
            await PopulateMaintenanceContracts(SelectedClientId);
            return Page();
        }

        private async Task PopulateDropdowns()
        {
            // Populate Models dropdown
            var models = (await _unitOfWork.Model.GetAllAsy(m => m.IsActive == true))
                .OrderBy(m => m.Name)
                .ToList();

            ModelList = models.Select(m => new SelectListItem
            {
                Text = m.Name,
                Value = m.Id.ToString()
            });

            // Populate Clients dropdown
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true))
                .OrderBy(c => c.Name)
                .ToList();

            ClientList = clients.Select(c => new SelectListItem
            {
                Text = $"{c.Name}{(c.Branch != null ? $" - {c.Branch}" : "")}",
                Value = c.Id.ToString()
            });

            // Initialize empty maintenance contracts list
            MaintenanceContractList = new List<SelectListItem> { new SelectListItem { Text = "No Contract", Value = "" } };
        }

        private async Task PopulateMaintenanceContracts(int clientId)
        {
            var contracts = (await _unitOfWork.MaintenanceContract.GetAllAsy(
                mc => mc.IsActive == true && mc.ClientId == clientId,
                includeProperties: "Client"
            ))
            .OrderBy(mc => mc.Id)
            .ToList();

            MaintenanceContractList = contracts.Select(mc => new SelectListItem
            {
                Text = $"Contract #{mc.Id} - {mc.Status}",
                Value = mc.Id.ToString()
            }).ToList();

            // Add empty option
            var maintenanceContractList = MaintenanceContractList.ToList();
            maintenanceContractList.Insert(0, new SelectListItem { Text = "No Contract", Value = "" });
            MaintenanceContractList = maintenanceContractList;
        }
    }
}