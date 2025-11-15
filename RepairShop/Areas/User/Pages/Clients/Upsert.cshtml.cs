using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace RepairShop.Areas.User.Pages.Clients
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpsertModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public Client clientForUpsert { get; set; }

        public async Task<IActionResult> OnGet(int? id)
        {
            clientForUpsert = new Client();
            if (id == null || id == 0)
            {
                return Page();
            }
            else
            {
                clientForUpsert = await _unitOfWork.Client.GetAsy(o => o.Id == id && o.IsActive == true);
                if (clientForUpsert == null)
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
                if (clientForUpsert == null)
                {
                    return NotFound();
                }

                if (clientForUpsert.Id == 0)
                {
                    await _unitOfWork.Client.AddAsy(clientForUpsert);
                    TempData["success"] = "Client created successfully";
                }
                else
                {
                    await _unitOfWork.Client.UpdateAsy(clientForUpsert);
                    TempData["success"] = "Client updated successfully";
                }
                await _unitOfWork.SaveAsy();

                return RedirectToPage("Index");
            }
            return Page();
        }
    }
}
