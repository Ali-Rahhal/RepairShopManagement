using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.User.Pages.TransactionHeaders
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
        public TransactionHeader thForUpsert { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            thForUpsert = new TransactionHeader();
            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                thForUpsert = await _unitOfWork.TransactionHeader.GetAsy(
                    o => o.Id == id,
                    includeProperties: "DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model,DefectiveUnit.SerialNumber.Client"
                );
                if (thForUpsert == null)
                {
                    return NotFound();
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                if (thForUpsert == null)
                {
                    return NotFound();
                }

                if (thForUpsert.DefectiveUnitId == 0)
                {
                    ModelState.AddModelError(string.Empty, "Please select a defective unit");
                    return Page();
                }

                

                // Get the defective unit to set the ClientId
                var defectiveUnit = await _unitOfWork.DefectiveUnit.GetAsy(
                    du => du.Id == thForUpsert.DefectiveUnitId,
                    includeProperties: "SerialNumber,SerialNumber.Client"
                );

                if (defectiveUnit == null)
                {
                    ModelState.AddModelError(string.Empty, "Selected defective unit not found");
                    return Page();
                }

                // Set the ClientId from the defective unit's serial number
                thForUpsert.ClientId = defectiveUnit.SerialNumber.ClientId;

                if (defectiveUnit.Status != SD.Status_DU_Reported)
                {
                    ModelState.AddModelError(string.Empty, "Selected defective unit is already in use or has been completed");
                    return Page();
                }

                if (thForUpsert.Id == 0)
                {
                    await _unitOfWork.TransactionHeader.AddAsy(thForUpsert);
                    TempData["success"] = "Transaction created successfully";
                }
                else
                {
                    await _unitOfWork.TransactionHeader.UpdateAsy(thForUpsert);
                    TempData["success"] = "Transaction updated successfully";
                }
                await _unitOfWork.SaveAsy();

                return RedirectToPage("Index");
            }
            return Page();
        }

        /// This method is used to search for defective units based on a given term.
        /// It returns a JSON result containing a list of defective units that match the search term.
        public async Task<IActionResult> OnGetSearchDefectiveUnits(string term)
        {
            // If the term is null or whitespace, return an empty list
            if (string.IsNullOrWhiteSpace(term))
                return new JsonResult(new { data = new List<object>() });

            // Search defective units by serial number, model, or client name
            var defectiveUnits = (await _unitOfWork.DefectiveUnit.GetAllAsy(
                du => du.IsActive == true &&
                     (du.SerialNumber.Value.Contains(term) ||
                      du.SerialNumber.Model.Name.Contains(term) ||
                      du.SerialNumber.Client.Name.Contains(term)),
                includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client"
            ))
            .Take(10) // Limit results for performance
            .Select(du => new
            {
                id = du.Id,
                serialNumberId = du.SerialNumberId,
                serialNumber = du.SerialNumber.Value,
                model = du.SerialNumber.Model.Name,
                clientId = du.SerialNumber.ClientId,
                clientName = du.SerialNumber.Client.Name,
                description = du.Description,
                status = du.Status,
                reportedDate = du.ReportedDate.ToString("yyyy-MM-dd")
            })
            .ToList();

            return new JsonResult(new { data = defectiveUnits });
        }

        /// This method is used to get details of a specific defective unit.
        public async Task<IActionResult> OnGetDefectiveUnitDetails(int id)
        {
            var defectiveUnit = await _unitOfWork.DefectiveUnit.GetAsy(
                du => du.Id == id,
                includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client"
            );

            if (defectiveUnit == null)
            {
                return new JsonResult(new { success = false, message = "Defective unit not found" });
            }

            var result = new
            {
                success = true,
                defectiveUnitId = defectiveUnit.Id,
                serialNumberId = defectiveUnit.SerialNumberId,
                serialNumber = defectiveUnit.SerialNumber.Value,
                model = defectiveUnit.SerialNumber.Model.Name,
                clientId = defectiveUnit.SerialNumber.ClientId,
                clientName = defectiveUnit.SerialNumber.Client.Name,
                description = defectiveUnit.Description,
                status = defectiveUnit.Status,
                reportedDate = defectiveUnit.ReportedDate.ToString("yyyy-MM-dd")
            };

            return new JsonResult(result);
        }
    }
}