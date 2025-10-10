using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.Warranties
{
    [Authorize(Roles = SD.Role_Admin)]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void OnGet()
        {
        }

        // API for DataTable
        public async Task<JsonResult> OnGetAll()
        {
            var warrantyList = (await _unitOfWork.Warranty.GetAllAsy(
                w => w.IsActive == true,
                includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client"
            )).ToList();

            // Update status for warranties that have expired
            var updatedWarranties = new List<Warranty>();
            foreach (var warranty in warrantyList)
            {
                var newStatus = warranty.EndDate < DateTime.Now ? "Expired" : "Active";
                if (warranty.Status != newStatus)
                {
                    warranty.Status = newStatus;
                    updatedWarranties.Add(warranty);
                }
            }

            // Save status changes to database
            if (updatedWarranties.Count > 0)
            {
                foreach (var warranty in updatedWarranties)
                {
                    await _unitOfWork.Warranty.UpdateAsy(warranty);
                }
                await _unitOfWork.SaveAsy();
            }


            // Format the data for better display
            var formattedData = warrantyList.Select(w => new
            {
                id = w.Id,
                startDate = w.StartDate,
                endDate = w.EndDate,
                status = w.Status,
                serialNumber = w.SerialNumber?.Value ?? "N/A",
                modelName = w.SerialNumber?.Model?.Name ?? "N/A",
                clientName = w.SerialNumber?.Client?.Name ?? "N/A",
                daysRemaining = (w.EndDate - DateTime.Now).Days,
                isExpired = w.EndDate < DateTime.Now
            });

            return new JsonResult(new { data = formattedData });
        }

        // API for Delete
        public async Task<IActionResult> OnGetDelete(int? id)
        {
            var warrantyToBeDeleted = await _unitOfWork.Warranty.GetAsy(w => w.Id == id, includeProperties: "SerialNumber");
            if (warrantyToBeDeleted == null)
            {
                return new JsonResult(new { success = false, message = "Error while deleting" });
            }

            // Check if warranty is referenced in any transactions
            var isReferenced = warrantyToBeDeleted.SerialNumber.IsActive; // Add your business logic checks here

            if (isReferenced)
            {
                return new JsonResult(new { success = false, message = "Warranty cannot be deleted because it has an active serial number" });
            }

            await _unitOfWork.Warranty.RemoveAsy(warrantyToBeDeleted);
            await _unitOfWork.SaveAsy();
            return new JsonResult(new { success = true, message = "Warranty deleted successfully" });
        }

        // API for Serial Numbers (for filter dropdown)
        public async Task<JsonResult> OnGetSerialNumbers()
        {
            var serialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.IsActive == true,
                includeProperties: "Model,Client"
            ))
            .Select(sn => new {
                id = sn.Id,
                value = sn.Value,
                modelName = sn.Model.Name,
                clientName = sn.Client.Name
            })
            .OrderBy(sn => sn.value)
            .ToList();

            return new JsonResult(new { serialNumbers });
        }
    }
}