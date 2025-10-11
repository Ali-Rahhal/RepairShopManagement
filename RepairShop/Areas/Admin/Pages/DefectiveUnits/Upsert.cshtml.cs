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

        public List<SerialNumber> AvailableSerialNumbers { get; set; }
        public Warranty SelectedWarranty { get; set; }
        public MaintenanceContract SelectedMaintenanceContract { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            DefectiveUnitForUpsert = new DefectiveUnit
            {
                ReportedDate = DateTime.Now
            };

            // Load available serial numbers
            await LoadAvailableSerialNumbers();

            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                DefectiveUnitForUpsert = await _unitOfWork.DefectiveUnit.GetAsy(
                    du => du.Id == id,
                    includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client,Warranty,MaintenanceContract"
                );

                if (DefectiveUnitForUpsert == null)
                {
                    return NotFound();
                }

                // Load warranty and contract info if they exist
                if (DefectiveUnitForUpsert.WarrantyId.HasValue)
                {
                    SelectedWarranty = await _unitOfWork.Warranty.GetAsy(w => w.Id == DefectiveUnitForUpsert.WarrantyId);
                }

                if (DefectiveUnitForUpsert.MaintenanceContractId.HasValue)
                {
                    SelectedMaintenanceContract = await _unitOfWork.MaintenanceContract.GetAsy(mc => mc.Id == DefectiveUnitForUpsert.MaintenanceContractId);
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                if (DefectiveUnitForUpsert == null)
                {
                    return NotFound();
                }

                if (DefectiveUnitForUpsert.Id == 0)
                {
                    await _unitOfWork.DefectiveUnit.AddAsy(DefectiveUnitForUpsert);
                    TempData["success"] = "Defective unit reported successfully";
                }
                else
                {
                    await _unitOfWork.DefectiveUnit.UpdateAsy(DefectiveUnitForUpsert);
                    TempData["success"] = "Defective unit updated successfully";
                }

                await _unitOfWork.SaveAsy();
                return RedirectToPage("Index");
            }

            await LoadAvailableSerialNumbers();
            return Page();
        }

        // API for searching serial numbers
        public async Task<JsonResult> OnGetSearchSerialNumbers(string searchTerm)
        {
            var serialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.IsActive == true &&
                     (sn.Value.Contains(searchTerm) ||
                      sn.Model.Name.Contains(searchTerm) ||
                      sn.Client.Name.Contains(searchTerm)),
                includeProperties: "Model,Client,Warranty,MaintenanceContract"
            ))
            .Take(10) // Limit results for performance
            .Select(sn => new
            {
                id = sn.Id,
                value = sn.Value,
                modelName = sn.Model.Name,
                clientName = sn.Client.Name,
                hasWarranty = sn.Warranty != null && sn.Warranty.Status == "Active",
                hasContract = sn.MaintenanceContract != null && sn.MaintenanceContract.Status == "Active",
                warrantyId = sn.Warranty?.Id,
                maintenanceContractId = sn.MaintenanceContract?.Id
            })
            .ToList();

            return new JsonResult(serialNumbers);
        }

        // API for getting serial number details
        public async Task<JsonResult> OnGetSerialNumberDetails(int id)
        {
            var serialNumber = await _unitOfWork.SerialNumber.GetAsy(
                sn => sn.Id == id,
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
                clientName = serialNumber.Client.Name,
                hasActiveWarranty = serialNumber.Warranty != null && serialNumber.Warranty.Status == "Active",
                hasActiveContract = serialNumber.MaintenanceContract != null && serialNumber.MaintenanceContract.Status == "Active",
                warrantyId = serialNumber.Warranty?.Id,
                maintenanceContractId = serialNumber.MaintenanceContract?.Id
            };

            return new JsonResult(result);
        }

        private async Task LoadAvailableSerialNumbers()
        {
            AvailableSerialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.IsActive == true,
                includeProperties: "Model,Client"
            ))
            .OrderBy(sn => sn.Value)
            .ToList();
        }
    }
}