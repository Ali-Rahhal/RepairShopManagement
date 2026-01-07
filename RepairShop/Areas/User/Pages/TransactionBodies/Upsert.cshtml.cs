using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.User.Pages.TransactionBodies
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
        public TransactionBody tbForUpsert { get; set; }

        public List<string> Categories { get; set; }

        public async Task<IActionResult> OnGet(int? id, int? headerId)
        {
            // Load categories for dropdown
            await LoadCategories();

            if (id == null || id == 0)
            {
                // Create mode
                tbForUpsert = new TransactionBody
                {
                    TransactionHeaderId = headerId.Value
                };
                return Page();
            }
            else
            {
                // Edit mode - only load the existing record
                tbForUpsert = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == id && o.IsActive == true);
                if (tbForUpsert == null)
                {
                    return NotFound();
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            // Get the final status from the hidden field
            var finalStatus = Request.Form["finalStatus"].ToString();
            var fromCompletionBtn = Request.Form["fromCompletionBtn"].ToString();
            if (!string.IsNullOrEmpty(finalStatus))
            {
                tbForUpsert.Status = finalStatus;
            }

            if (ModelState.IsValid)
            {
                bool isEdit = false;

                if (tbForUpsert == null)
                {
                    return NotFound();
                }

                if ((tbForUpsert.Status == SD.Status_Part_Pending_Replace || tbForUpsert.Status == SD.Status_Part_Replaced) && tbForUpsert.PartId == null)
                {
                    ModelState.AddModelError(string.Empty, "Please select a replacement part.");
                    await LoadCategories();
                    return Page();
                }

                if (tbForUpsert.Id == 0)
                {
                    bool partDecremented = false;
                    long partId = 0;
                    int partQuantity = 0;
                    // CREATE MODE - Set appropriate dates based on final status
                    SetStatusDates(tbForUpsert);

                    // If status is Replaced or PendingReplacement and a part is selected, decrement inventory
                    if ((tbForUpsert.Status == SD.Status_Part_Replaced || tbForUpsert.Status == SD.Status_Part_Pending_Replace) &&
                        tbForUpsert.PartId.HasValue && tbForUpsert.PartId.Value > 0)
                    {
                        var selectedPart = await _unitOfWork.Part.GetAsy(p => p.Id == tbForUpsert.PartId.Value && p.IsActive == true);
                        if (selectedPart != null && selectedPart.Quantity > 0)
                        {
                            // Decrement part quantity
                            selectedPart.Quantity--;
                            await _unitOfWork.Part.UpdateAsy(selectedPart);
                            partDecremented = true;
                            partId = selectedPart.Id;
                            partQuantity = selectedPart.Quantity;
                            if (tbForUpsert.Status == SD.Status_Part_Pending_Replace)
                            {
                                TempData["success"] = "Broken part added and waiting for replacement";
                            }
                            else
                            {
                                TempData["success"] = "Broken part replaced successfully";
                            }
                            
                        }
                        else
                        {
                            // If part not available, change to waiting status
                            tbForUpsert.Status = SD.Status_Part_Waiting_Part;
                            tbForUpsert.WaitingPartDate = DateTime.Now;
                            TempData["warning"] = "Part not available - marked as waiting for part";
                        }
                    }
                    else if (tbForUpsert.Status == SD.Status_Part_Fixed)
                    {
                        TempData["success"] = "Broken part marked as fixed successfully";
                    }
                    else if (tbForUpsert.Status == SD.Status_Part_NotRepairable || tbForUpsert.Status == SD.Status_Part_NotReplaceable)
                    {
                        TempData["warning"] = $"Broken part marked as {tbForUpsert.Status.Replace("Not", "Not ").ToLower()}";
                    }
                    else
                    {
                        TempData["success"] = "Broken Part added and waiting for repair";
                    }

                    await _unitOfWork.TransactionBody.AddAsy(tbForUpsert);
                    await _unitOfWork.SaveAsy();
                    if (partDecremented)
                    {
                        await _unitOfWork.PartStockHistory.AddAsy(new PartStockHistory
                        {
                            PartId = partId,
                            QuantityChange = -1,
                            QuantityAfter = partQuantity,
                            TransactionBodyId = tbForUpsert.Id, 
                            Reason = $"Used in repair (Transaction #{tbForUpsert.TransactionHeaderId})", 
                            CreatedDate = DateTime.Now 
                        });
                        await _unitOfWork.SaveAsy();
                    }
                }
                else
                {
                    isEdit = true;
                    // EDIT MODE - Only update PartName, ignore other changes
                    var existingTransactionBody = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == tbForUpsert.Id && o.IsActive == true);
                    if (existingTransactionBody == null)
                    {
                        return NotFound();
                    }

                    // Only update the PartName, preserve everything else
                    existingTransactionBody.BrokenPartName = tbForUpsert.BrokenPartName;

                    await _unitOfWork.TransactionBody.UpdateAsy(existingTransactionBody);
                    await _unitOfWork.SaveAsy();
                    TempData["success"] = "Part name updated successfully";
                }

                // Update TransactionHeader last modified date
                var HeaderForBody = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == tbForUpsert.TransactionHeaderId && o.IsActive == true, tracked: true);
                if (HeaderForBody != null)
                {
                    HeaderForBody.LastModifiedDate = DateTime.Now;
                    await _unitOfWork.TransactionHeader.UpdateAsy(HeaderForBody);
                    await _unitOfWork.SaveAsy();
                }
                if(fromCompletionBtn == "True")
                {
                    return RedirectToPage("Upsert", new { HeaderId = tbForUpsert.TransactionHeaderId, fromCompletionBtn = "True", pageReload = "True" });
                }
                // Reload the page
                if(!isEdit)
                {
                    return RedirectToPage("Upsert", new { HeaderId = tbForUpsert.TransactionHeaderId });
                }else
                {
                    return RedirectToPage("Index", new { HeaderId = tbForUpsert.TransactionHeaderId });
                }
                
            }

            await LoadCategories();
            return Page();
        }

        private void SetStatusDates(TransactionBody transactionBody)
        {
            var now = DateTime.Now;

            switch (transactionBody.Status)
            {
                case SD.Status_Part_Fixed:
                    transactionBody.FixedDate = now;
                    break;
                case SD.Status_Part_Replaced:
                    transactionBody.ReplacedDate = now;
                    break;
                case SD.Status_Part_NotRepairable:
                    transactionBody.NotRepairableDate = now;
                    break;
                case SD.Status_Part_NotReplaceable:
                    transactionBody.NotReplaceableDate = now;
                    break;
                case SD.Status_Part_Waiting_Part:
                    transactionBody.WaitingPartDate = now;
                    break;
                    // For pending statuses, no specific date is set
            }
        }

        private async Task LoadCategories()
        {
            Categories = (await _unitOfWork.Part.GetAllAsy(p => p.IsActive && p.Quantity >= 0))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        // AJAX handler to get parts by category
        public async Task<IActionResult> OnGetPartsByCategory(string category)
        {
            var parts = (await _unitOfWork.Part.GetAllAsy(p => p.IsActive && p.Quantity >= 0 && p.Category == category))
                .Select(p => new { p.Id, p.Name })
                .OrderBy(p => p.Name)
                .ToList();

            return new JsonResult(parts);
        }

        // AJAX handler to get part details
        public async Task<IActionResult> OnGetPartDetails(int id)
        {
            var part = await _unitOfWork.Part.GetAsy(p => p.Id == id && p.IsActive);
            if (part == null)
            {
                return new JsonResult(null);
            }

            return new JsonResult(new
            {
                part.Id,
                part.Name,
                part.Quantity,
                part.Price
            });
        }
    }
}