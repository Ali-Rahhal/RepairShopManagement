using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services.Helper;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        public async Task<IActionResult> OnGet(int? id, int? headerId)
        {
            tbForUpsert = new TransactionBody();
            if (id == null || id == 0)
            {
                tbForUpsert.TransactionHeaderId = headerId.Value;
                return Page();
            }
            else
            {
                tbForUpsert = await _unitOfWork.TransactionBody.GetAsy(o => o.Id == id);
                if (tbForUpsert == null)
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
                if (tbForUpsert == null)
                {
                    return NotFound();
                }

                if (tbForUpsert.Id == 0)
                {
                    await _unitOfWork.TransactionBody.AddAsy(tbForUpsert);

                    // Update the corresponding TransactionHeader's status to 'In Progress'.
                    var Header = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == tbForUpsert.TransactionHeaderId);
                    Header.Status = SD.Status_Job_InProgress;
                    await _unitOfWork.TransactionHeader.UpdateAsy(Header);
                    await _unitOfWork.SaveAsy();
                    TempData["success"] = "Part created successfully";
                }
                else
                {
                    await _unitOfWork.TransactionBody.UpdateAsy(tbForUpsert);
                    await _unitOfWork.SaveAsy();
                    TempData["success"] = "Part updated successfully";
                }
                
                return RedirectToPage("Index", new { HeaderId = tbForUpsert.TransactionHeaderId });
            }
            return Page();
        }
    }
}
