using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
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
                thForUpsert = await _unitOfWork.TransactionHeader.GetAsy(o => o.Id == id);
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

                if(thForUpsert.ClientId == 0)
                {
                    ModelState.AddModelError(string.Empty, "Please select a client");
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

        /// This method is used to search for clients based on a given term.
        /// It returns a JSON result containing a list of clients whose names contain the given term.
        /// If the term is null or whitespace, it returns an empty list.
        public async Task<IActionResult> OnGetSearchClients(string term)
        {
            // If the term is null or whitespace, return an empty list
            if (string.IsNullOrWhiteSpace(term))
                return new JsonResult(new { data = new List<Client>() });

            // Get all clients whose names contain the term
            var clients = (await _unitOfWork.Client.GetAllAsy(c => c.Name.Contains(term)))
                .Select(c => new { c.Id, c.Name })
                .ToList();

            // Return the list of clients as a JSON result
            return new JsonResult(new { data = clients });
        }
    }
}
