using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.ProcessTransactions
{
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

        public async Task<JsonResult> OnGetAll()
        {
            var THList = (await _unitOfWork.TransactionHeader
                    .GetAllAsy(t => (t.Status == SD.Status_Job_Delivered || t.Status == SD.Status_Job_Processed) && t.IsActive == true,
                    includeProperties: "BrokenParts,BrokenParts.Part,DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model,DefectiveUnit.SerialNumber.Client"))
                    .ToList();

            var formattedData = THList.Select(t => new
            {
                id = t.Id,
                clientName = t.DefectiveUnit?.SerialNumber?.Client?.Name ?? "N/A",
                clientBranch = t.DefectiveUnit?.SerialNumber?.Client?.Branch ?? "N/A",
                receivedDate = t.DefectiveUnit?.SerialNumber?.ReceivedDate.ToString("dd-MM-yyyy HH:mm tt") ?? "N/A",
                fixedDate = t.DefectiveUnit?.ResolvedDate,
                modelName = t.DefectiveUnit?.SerialNumber?.Model?.Name ?? "N/A",
                serialNumber = t.DefectiveUnit?.SerialNumber?.Value ?? "N/A",
                issue = t.DefectiveUnit?.Description ?? "N/A",
                spareParts = t.BrokenParts?.Where(bp => bp.Status == SD.Status_Part_Replaced)
                                .Select(bp => new 
                                    { 
                                        Name = bp.Part?.Name ?? "N/A",
                                        Price = bp.Part?.Price ?? 0
                                    })
                                        .ToList() ?? [],
                cost = t.BrokenParts?.Where(bp => bp.Status == SD.Status_Part_Replaced).Sum(bp => bp.Part?.Price ?? 0),
                laborFees = t.LaborFees ?? 0,
                comment = t.Comment ?? "N/A",
                status = t.Status
            });

            return new JsonResult(new { data = formattedData });
        }

        // NEW: Method to process transaction
        public async Task<JsonResult> OnGetProcessTransaction(int id, string comment = "")
        {
            try
            {
                var transaction = await _unitOfWork.TransactionHeader.GetAsy(
                    th => th.Id == id && th.IsActive == true
                );

                if (transaction == null)
                {
                    return new JsonResult(new { success = false, message = "Transaction not found." });
                }

                // Update transaction status
                transaction.Status = SD.Status_Job_Processed;
                transaction.ProcessedDate = DateTime.Now;

                // Update comment if provided
                if (!string.IsNullOrEmpty(comment))
                {
                    transaction.Comment = comment.Trim();
                }

                // Update the transaction
                await _unitOfWork.TransactionHeader.UpdateAsy(transaction);
                await _unitOfWork.SaveAsy();

                return new JsonResult(new
                {
                    success = true,
                    message = "Transaction marked as processed successfully."
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Error processing transaction: {ex.Message}"
                });
            }
        }
    }
}
