using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.DefectiveUnits
{
    [Authorize]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpsertModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public DefectiveUnit DefectiveUnitForUpsert { get; set; }

        [BindProperty]
        public SerialNumber NewSerialNumber { get; set; } // For creating new serial numbers

        public bool ShowNewSerialSection { get; set; }

        public IEnumerable<SelectListItem> ModelList { get; set; }
        public IEnumerable<SelectListItem> ClientList { get; set; }
        public IEnumerable<SelectListItem> MaintenanceContractList { get; set; }

        public List<SerialNumber> AvailableSerialNumbers { get; set; }
        public Warranty SelectedWarranty { get; set; }
        public MaintenanceContract SelectedMaintenanceContract { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            DefectiveUnitForUpsert = new DefectiveUnit();
            NewSerialNumber = new SerialNumber();

            // Populate dropdowns
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                DefectiveUnitForUpsert = await _unitOfWork.DefectiveUnit.GetAsy(
                    du => du.IsActive == true && du.Id == id,
                    includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client,SerialNumber.Warranty,SerialNumber.MaintenanceContract"
                );

                if (DefectiveUnitForUpsert == null)
                {
                    return NotFound();
                }

                // Load warranty and contract info if they exist
                if (DefectiveUnitForUpsert.SerialNumber.WarrantyId.HasValue)
                {
                    SelectedWarranty = await _unitOfWork.Warranty.GetAsy(w => w.Id == DefectiveUnitForUpsert.SerialNumber.WarrantyId && w.IsActive == true);
                }

                if (DefectiveUnitForUpsert.SerialNumber.MaintenanceContractId.HasValue)
                {
                    SelectedMaintenanceContract = await _unitOfWork.MaintenanceContract.GetAsy(mc => mc.Id == DefectiveUnitForUpsert.SerialNumber.MaintenanceContractId && mc.IsActive == true);
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            // Determine if we're creating a new serial number
            bool creatingNewSerial = DefectiveUnitForUpsert.SerialNumberId == 0 &&
                                   !string.IsNullOrEmpty(NewSerialNumber.Value);

            // Clear NewSerialNumber validation errors if we're not creating a new serial number
            if (!creatingNewSerial)
            {
                ModelState.Remove("NewSerialNumber.Value");
                ModelState.Remove("NewSerialNumber.ModelId");
                ModelState.Remove("NewSerialNumber.ClientId");
                ModelState.Remove("NewSerialNumber.MaintenanceContractId");
            }

            if (ModelState.IsValid)
            {
                if (DefectiveUnitForUpsert == null)
                {
                    return NotFound();
                }

                if (creatingNewSerial)
                {
                    // Check if sn contains characters btw 3 and 40
                    if (NewSerialNumber.Value.Length < 3 || NewSerialNumber.Value.Length > 40)
                    {
                        ModelState.AddModelError("NewSerialNumber.Value", "Serial number must be between 3 and 40 characters.");
                        await PopulateDropdowns();
                        ShowNewSerialSection = true;
                        if (NewSerialNumber.ClientId > 0)
                        {
                            await PopulateMaintenanceContracts(NewSerialNumber.ClientId);
                        }
                        return Page();
                    }

                    // Check if sn contains spaces
                    if (NewSerialNumber.Value.Trim().Contains(' '))
                    {
                        ModelState.AddModelError("NewSerialNumber.Value", "Serial number cannot contain spaces.");
                        await PopulateDropdowns();
                        ShowNewSerialSection = true;
                        if (NewSerialNumber.ClientId > 0)
                        {
                            await PopulateMaintenanceContracts(NewSerialNumber.ClientId);
                        }
                        return Page();
                    }

                    // Validate new serial number
                    var existingSerialNumber = await _unitOfWork.SerialNumber.GetAsy(
                        sn => sn.Value == NewSerialNumber.Value && sn.IsActive == true
                    );

                    if (existingSerialNumber != null)
                    {
                        ModelState.AddModelError("NewSerialNumber.Value", "Serial number already exists. Please user another value.");
                        await PopulateDropdowns();
                        ShowNewSerialSection = true;
                        if (NewSerialNumber.ClientId > 0)
                        {
                            await PopulateMaintenanceContracts(NewSerialNumber.ClientId);
                        }
                        return Page();
                    }

                    // Create the new serial number
                    await _unitOfWork.SerialNumber.AddAsy(NewSerialNumber);
                    await _unitOfWork.SaveAsy();

                    // Set the new serial number ID to the defective unit
                    DefectiveUnitForUpsert.SerialNumberId = NewSerialNumber.Id;
                }

                var duplicateReportedDU = await _unitOfWork.DefectiveUnit.GetAsy(du => du.IsActive == true
                    && du.SerialNumberId == DefectiveUnitForUpsert.SerialNumberId
                    && (du.Status == SD.Status_DU_Reported || du.Status == SD.Status_DU_UnderRepair)
                    && du.Id != DefectiveUnitForUpsert.Id);

                if (duplicateReportedDU != null)
                {
                    ModelState.AddModelError(string.Empty, "You have already reported a defective unit for this serial number.");
                    await PopulateDropdowns();
                    return Page();
                }

                if (DefectiveUnitForUpsert.Id == 0)
                {
                    await _unitOfWork.DefectiveUnit.AddAsy(DefectiveUnitForUpsert);
                    await _unitOfWork.SaveAsy();

                    TempData["success"] = "Defective unit reported successfully";

                    // Redirect to same page (Upsert) for a new entry
                    return RedirectToPage("Upsert");
                }
                else
                {
                    await _unitOfWork.DefectiveUnit.UpdateAsy(DefectiveUnitForUpsert);
                    await _unitOfWork.SaveAsy();

                    TempData["success"] = "Defective unit updated successfully";
                    return RedirectToPage("Index");
                }
            }

            await PopulateDropdowns();

            if (creatingNewSerial)
            {
                ShowNewSerialSection = true;
                if (NewSerialNumber.ClientId > 0)
                {
                    await PopulateMaintenanceContracts(NewSerialNumber.ClientId);
                }
            }

            return Page();
        }

        // API for searching serial numbers
        public async Task<JsonResult?> OnGetSearchSerialNumber(string searchTerm)
        {
            var sn = await _unitOfWork.SerialNumber.GetAsy(
                sn => sn.IsActive == true &&
                    sn.Value.Equals(searchTerm),
                includeProperties: "Model,Client,Warranty,MaintenanceContract"
            );

            if (sn == null) return new JsonResult(null);

            var serialNumber = new
            {
                id = sn.Id,
                value = sn.Value,
                modelName = sn.Model.Name,
                clientName = sn.Client.Name,
                clientBranch = sn.Client.Branch ?? "N/A",
                modelId = sn.ModelId,
                clientId = sn.ClientId,
                receivedDate = sn.ReceivedDate.ToString("dd-MM-yyyy HH:mm tt"),
                hasWarranty = sn.Warranty != null && sn.Warranty.Status == "Active",
                hasContract = sn.MaintenanceContract != null && sn.MaintenanceContract.Status == "Active",
                warrantyId = sn.Warranty?.Id,
                maintenanceContractId = sn.MaintenanceContract?.Id
            };

            return new JsonResult(serialNumber);
        }

        // API for getting serial number details
        public async Task<JsonResult> OnGetSerialNumberDetails(int id)
        {
            var serialNumber = await _unitOfWork.SerialNumber.GetAsy(
                sn => sn.Id == id && sn.IsActive == true,
                includeProperties: "Model,Client,Warranty,MaintenanceContract"
            );

            if (serialNumber == null)
            {
                return new JsonResult(new { success = false });
            }

            var result = new
            {
                success = true,
                serialNumberId = serialNumber.Id,
                serialNumberValue = serialNumber.Value,
                modelName = serialNumber.Model.Name,
                clientName = $"{serialNumber.Client.Name}{(serialNumber.Client.Branch != null ? $" - {serialNumber.Client.Branch}" : "")}",
                modelId = serialNumber.ModelId,
                clientId = serialNumber.ClientId,
                hasActiveWarranty = serialNumber.Warranty != null && serialNumber.Warranty.Status == "Active",
                hasActiveContract = serialNumber.MaintenanceContract != null && serialNumber.MaintenanceContract.Status == "Active",
                warrantyId = serialNumber.Warranty?.Id,
                maintenanceContractId = serialNumber.MaintenanceContract?.Id,
                receivedDate = serialNumber.ReceivedDate.ToString("dd-MM-yyyy HH:mm tt")
            };

            return new JsonResult(result);
        }

        // API for getting contracts by client
        public async Task<JsonResult> OnGetContractsByClient(int clientId)
        {
            var contracts = (await _unitOfWork.MaintenanceContract.GetAllAsy(
                mc => mc.IsActive == true && mc.ClientId == clientId,
                includeProperties: "Client"
            ))
            .OrderBy(mc => mc.Id)
            .Select(mc => new
            {
                id = mc.Id,
                text = $"Contract #{mc.Id} - {mc.Client.Name}{(mc.Client.Branch != null ? $" - {mc.Client.Branch}" : "")} ({mc.Status})"
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
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.IsActive == true))
                .OrderBy(c => c.Name)
                .ToList();

            ClientList = clients.Select(c => new SelectListItem
            {
                Text = $"{c.Name}{(c.Branch != null ? $" - {c.Branch}" : "")}",
                Value = c.Id.ToString()
            });

            // Initialize empty maintenance contracts list
            MaintenanceContractList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Select a client first", Value = "" }
            };
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
                Text = $"Contract #{mc.Id} - {mc.Client.Name}{(mc.Client.Branch != null ? $" - {mc.Client.Branch}" : "")} ({mc.Status})",
                Value = mc.Id.ToString()
            }).ToList();

            // Add empty option
            var maintenanceContractList = MaintenanceContractList.ToList();
            maintenanceContractList.Insert(0, new SelectListItem { Text = "No Contract", Value = "" });
            MaintenanceContractList = maintenanceContractList;
        }
    }
}