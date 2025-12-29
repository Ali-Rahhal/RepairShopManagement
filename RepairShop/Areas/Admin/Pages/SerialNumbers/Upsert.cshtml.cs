using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;

namespace RepairShop.Areas.Admin.Pages.SerialNumbers
{
    [Authorize]
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
        public SerialNumber SerialNumberForUpsert { get; set; }

        public IEnumerable<SelectListItem> ModelList { get; set; }
        public IEnumerable<SelectListItem> ClientList { get; set; }
        public IEnumerable<SelectListItem> MaintenanceContractList { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? id { get; set; }
        [BindProperty(SupportsGet = true)]
        public long? clientId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? returnUrl { get; set; }

        public async Task<IActionResult> OnGet()
        {
            SerialNumberForUpsert = new SerialNumber();

            // Populate dropdowns
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                // If a clientId is provided, pre-select it and load related contracts
                if (clientId.HasValue)
                {
                    SerialNumberForUpsert.ClientId = clientId.Value;
                    await PopulateMaintenanceContracts(clientId.Value);
                }
                return Page();
            }
            else
            {
                SerialNumberForUpsert = await _unitOfWork.SerialNumber.GetAsy(
                    sn => sn.Id == id && sn.IsActive == true,
                    includeProperties: "Model,Client,MaintenanceContract"
                );

                if (SerialNumberForUpsert == null)
                {
                    return NotFound();
                }

                // If editing, populate contracts for the selected client
                if (SerialNumberForUpsert.ClientId > 0)
                {
                    await PopulateMaintenanceContracts(SerialNumberForUpsert.ClientId);
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                if (SerialNumberForUpsert == null)
                {
                    return NotFound();
                }
                
                // Check maximum length and minimum length
                if (SerialNumberForUpsert.Value.Length < 3 || SerialNumberForUpsert.Value.Length > 40)
                {
                    ModelState.AddModelError("SerialNumberForUpsert.Value", "Serial number must be between 3 and 40 characters long.");
                    await PopulateDropdowns();
                    if (SerialNumberForUpsert.ClientId > 0)
                    {
                        await PopulateMaintenanceContracts(SerialNumberForUpsert.ClientId);
                    }
                    return Page();
                }

                // Check if serial number contains spaces
                if (SerialNumberForUpsert.Value.Trim().Contains(' '))
                {
                    ModelState.AddModelError("SerialNumberForUpsert.Value", "Serial number cannot contain spaces.");
                    await PopulateDropdowns();
                    if (SerialNumberForUpsert.ClientId > 0)
                    {
                        await PopulateMaintenanceContracts(SerialNumberForUpsert.ClientId);
                    }
                    return Page();
                }

                // Check if serial number already exists (for create operation)
                if (SerialNumberForUpsert.Id == 0)
                {
                    var existingSerialNumber = await _unitOfWork.SerialNumber.GetAsy(
                        sn => sn.Value == SerialNumberForUpsert.Value && sn.IsActive == true
                    );
                    if (existingSerialNumber != null)
                    {
                        ModelState.AddModelError("SerialNumberForUpsert.Value", "Serial number already exists.");
                        await PopulateDropdowns();
                        if (SerialNumberForUpsert.ClientId > 0)
                        {
                            await PopulateMaintenanceContracts(SerialNumberForUpsert.ClientId);
                        }
                        return Page();
                    }

                    await _unitOfWork.SerialNumber.AddAsy(SerialNumberForUpsert);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Create, SD.Entity_SerialNumber, SerialNumberForUpsert.Id);
                    TempData["success"] = "Serial number created successfully";
                }
                else
                {
                    // For updates, check if serial number conflicts with OTHER records
                    var conflictingSerialNumber = await _unitOfWork.SerialNumber.GetAsy(
                        sn => sn.Value == SerialNumberForUpsert.Value
                           && sn.IsActive == true
                           && sn.Id != SerialNumberForUpsert.Id
                    );
                    if (conflictingSerialNumber != null)
                    {
                        ModelState.AddModelError("SerialNumberForUpsert.Value", $"Serial number '{SerialNumberForUpsert.Value}' already exists.");
                        await PopulateDropdowns();
                        if (SerialNumberForUpsert.ClientId > 0)
                        {
                            await PopulateMaintenanceContracts(SerialNumberForUpsert.ClientId);
                        }
                        return Page();
                    }

                    var snFromDb = await _unitOfWork.SerialNumber.GetAsy(s => s.Id == SerialNumberForUpsert.Id && s.IsActive == true);
                    if (snFromDb == null) return NotFound();
                    snFromDb.Value = SerialNumberForUpsert.Value;
                    snFromDb.ModelId = SerialNumberForUpsert.ModelId;
                    snFromDb.ClientId = SerialNumberForUpsert.ClientId;
                    snFromDb.MaintenanceContractId = SerialNumberForUpsert.MaintenanceContractId;
                    await _unitOfWork.SerialNumber.UpdateAsy(snFromDb);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Update, SD.Entity_SerialNumber, snFromDb.Id);
                    TempData["success"] = "Serial number updated successfully";
                }

                if (clientId != null)
                {
                    return RedirectToPage("Upsert", new { clientId, returnUrl });
                }
                return RedirectToPage("Index");
            }

            await PopulateDropdowns();
            if (SerialNumberForUpsert.ClientId > 0)
            {
                await PopulateMaintenanceContracts(SerialNumberForUpsert.ClientId);
            }
            return Page();
        }

        // API endpoint to get contracts by client
        public async Task<JsonResult> OnGetContractsByClient(int clientId)
        {
            var contracts = (await _unitOfWork.MaintenanceContract.GetAllAsy(
                mc => mc.IsActive == true && mc.ClientId == clientId,
                includeProperties: "Client,Client.ParentClient"
            ))
            .OrderBy(mc => mc.Id)
            .Select(mc => new
            {
                id = mc.Id,
                text = mc.Client.ParentClient != null
                    ? $"Contract #{mc.Id} - {mc.Client.ParentClient.Name} - {mc.Client.Name} ({mc.Status})"
                    : $"Contract #{mc.Id} - {mc.Client.Name} ({mc.Status})"
            })
            .ToList();

            return new JsonResult(contracts);
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
            MaintenanceContractList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Select a client first", Value = "" }
            };
        }

        private async Task PopulateMaintenanceContracts(long clientId)
        {
            var contracts = (await _unitOfWork.MaintenanceContract.GetAllAsy(
                mc => mc.IsActive == true && mc.ClientId == clientId,
                includeProperties: "Client,Client.ParentClient"
            ))
            .OrderBy(mc => mc.Id)
            .ToList();

            MaintenanceContractList = contracts.Select(mc => new SelectListItem
            {
                Text = mc.Client.ParentClient != null
                    ? $"Contract #{mc.Id} - {mc.Client.ParentClient.Name} - {mc.Client.Name} ({mc.Status})"
                    : $"Contract #{mc.Id} - {mc.Client.Name} ({mc.Status})",
                Value = mc.Id.ToString()
            }).ToList();

            // Add empty option
            var maintenanceContractList = MaintenanceContractList.ToList();
            maintenanceContractList.Insert(0, new SelectListItem { Text = "No Contract", Value = "" });
            MaintenanceContractList = maintenanceContractList;
        }
    }
}