using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.SerialNumbers
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
        public SerialNumber SerialNumberForUpsert { get; set; }

        public IEnumerable<SelectListItem> ModelList { get; set; }
        public IEnumerable<SelectListItem> ClientList { get; set; }
        public IEnumerable<SelectListItem> MaintenanceContractList { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            SerialNumberForUpsert = new SerialNumber();

            // Populate dropdowns
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                SerialNumberForUpsert = await _unitOfWork.SerialNumber.GetAsy(
                    sn => sn.Id == id,
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
                    TempData["success"] = "Serial number created successfully";
                }
                else
                {
                    await _unitOfWork.SerialNumber.UpdateAsy(SerialNumberForUpsert);
                    TempData["success"] = "Serial number updated successfully";
                }

                await _unitOfWork.SaveAsy();
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
                includeProperties: "Client"
            ))
            .OrderBy(mc => mc.Id)
            .Select(mc => new
            {
                id = mc.Id,
                text = $"Contract #{mc.Id} - {mc.Client.Name} ({mc.Status})"
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
                Text = $"{c.Name}",
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