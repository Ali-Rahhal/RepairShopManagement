using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.Admin.Pages.Warranties
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuditLogService _auditLogService;

        public UpsertModel(IUnitOfWork unitOfWork, AuditLogService als)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = als;
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

                    // Check for spaces
                    if (NewSerialNumbersInput.Trim().Contains(' '))
                    {
                        ModelState.AddModelError("NewSerialNumbersInput", "Spaces are not allowed in serial numbers, please use semicolons.");
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

                    // Parse serial numbers
                    var serialNumberValues = NewSerialNumbersInput
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(sn => sn.Trim())
                        .Where(sn => !string.IsNullOrWhiteSpace(sn))
                        .Distinct()
                        .ToList();

                    if (serialNumberValues.Count == 0)
                    {
                        ModelState.AddModelError("NewSerialNumbersInput", "Please enter valid serial numbers separated by semicolons.");
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

                    // Add new serial numbers to database
                    await _unitOfWork.SerialNumber.AddRangeAsy(newSerialNumbers);
                    await _unitOfWork.SaveAsy();
                    foreach (var sn in newSerialNumbers)
                    {
                        await _auditLogService.AddLogAsy(SD.Action_Create, SD.Entity_SerialNumber, sn.Id);
                    }

                    // Create warranty with serial numbers
                    WarrantyForUpsert.SerialNumbers = newSerialNumbers;
                    await _unitOfWork.Warranty.AddAsy(WarrantyForUpsert);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Create, SD.Entity_Warranty, WarrantyForUpsert.Id);
                    TempData["success"] = $"Warranty created successfully with {newSerialNumbers.Count} serial number(s)";
                }
                else
                {
                    // UPDATE EXISTING WARRANTY (serial numbers remain unchanged)
                    var warrantyFromDb = await _unitOfWork.Warranty.GetAsy(w => w.Id == WarrantyForUpsert.Id && w.IsActive == true);
                    if (warrantyFromDb == null) return NotFound();
                    warrantyFromDb.StartDate = WarrantyForUpsert.StartDate;
                    warrantyFromDb.EndDate = WarrantyForUpsert.EndDate;
                    await _unitOfWork.Warranty.UpdateAsy(warrantyFromDb);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Update, SD.Entity_Warranty, warrantyFromDb.Id);
                    TempData["success"] = "Warranty updated successfully";
                }

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
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true, includeProperties: "ParentClient"))
                .OrderBy(c => c.Name)
                .ToList();

            ClientList = clients.Select(c => new SelectListItem
            {
                Text = c.ParentClient != null
                    ? $"{c.ParentClient.Name} - {c.Name}"
                    : $"{c.Name}",
                Value = c.Id.ToString()
            });

            // Initialize empty maintenance contracts list
            MaintenanceContractList = new List<SelectListItem> { new SelectListItem { Text = "No Contract", Value = "" } };
        }

        private async Task PopulateMaintenanceContracts(long clientId)
        {
            var contracts = (await _unitOfWork.MaintenanceContract.GetAllAsy(
                mc => mc.IsActive == true && mc.ClientId == clientId,
                includeProperties: "Client"
            ))
            .OrderBy(mc => mc.Id)
            .ToList();

            MaintenanceContractList = contracts.Select(mc => new SelectListItem
            {
                Text = $"Contract #{mc.Id} - {(mc.EndDate > DateTime.Now ? "Active" : "Expired")}",
                Value = mc.Id.ToString()
            }).ToList();

            // Add empty option
            var maintenanceContractList = MaintenanceContractList.ToList();
            maintenanceContractList.Insert(0, new SelectListItem { Text = "No Contract", Value = "" });
            MaintenanceContractList = maintenanceContractList;
        }

        public async Task<JsonResult> OnPostChangeSerialClient(long serialId, long clientId)
        {
            var serial = await _unitOfWork.SerialNumber.GetAsy(
                s => s.Id == serialId && s.IsActive
            );

            if (serial == null)
                return new JsonResult(new { success = false, message = "Serial not found" });

            serial.ClientId = clientId;

            await _unitOfWork.SerialNumber.UpdateAsy(serial);
            await _unitOfWork.SaveAsy();

            await _auditLogService.AddLogAsy(
                SD.Action_Update,
                SD.Entity_SerialNumber,
                serial.Id
            );

            return new JsonResult(new { success = true });
        }

        public async Task<JsonResult> OnPostChangeAllSerialClients(long warrantyId, long clientId)
        {
            var serials = await _unitOfWork.SerialNumber.GetAllAsy(
                s => s.WarrantyId == warrantyId && s.IsActive
            );

            if (!serials.Any())
                return new JsonResult(new { success = false, message = "No serials found" });

            foreach (var serial in serials)
            {
                serial.ClientId = clientId;
                await _auditLogService.AddLogAsy(
                    SD.Action_Update,
                    SD.Entity_SerialNumber,
                    serial.Id
                );
            }

            await _unitOfWork.SerialNumber.UpdateRangeAsy(serials);
            await _unitOfWork.SaveAsy();

            return new JsonResult(new { success = true });
        }

    }
}