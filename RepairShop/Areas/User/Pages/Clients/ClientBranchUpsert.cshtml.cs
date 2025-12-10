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
    public class ClientBranchUpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClientBranchUpsertModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public Client BranchForUpsert { get; set; }

        public async Task<IActionResult> OnGet([FromQuery]int? id, [FromQuery]int? parentId)
        {
            BranchForUpsert = new Client();
            if (id == null || id == 0)
            {
                if (parentId == null || parentId == 0)
                {
                    return NotFound();
                }
                BranchForUpsert.ParentClientId = parentId.GetValueOrDefault();
                return Page();
            }
            else
            {
                BranchForUpsert = await _unitOfWork.Client.GetAsy(o => o.Id == id && o.IsActive == true);
                if (BranchForUpsert == null)
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
                if (BranchForUpsert == null)
                {
                    return NotFound();
                }

                if (BranchForUpsert.Id == 0)
                {
                    await _unitOfWork.Client.AddAsy(BranchForUpsert);
                    TempData["success"] = "Branch created successfully";
                }
                else
                {
                    var branchFromDb = await _unitOfWork.Client.GetAsy(c => c.Id == BranchForUpsert.Id && c.IsActive == true);
                    if (branchFromDb == null) return NotFound();
                    branchFromDb.Name = BranchForUpsert.Name;
                    branchFromDb.Phone = BranchForUpsert.Phone;
                    branchFromDb.Email = BranchForUpsert.Email;
                    branchFromDb.Address = BranchForUpsert.Address;
                    await _unitOfWork.Client.UpdateAsy(branchFromDb);
                    TempData["success"] = "Branch updated successfully";
                }
                await _unitOfWork.SaveAsy();

                return RedirectToPage("ClientBranchIndex", new { id = BranchForUpsert.ParentClientId });
            }
            return Page();
        }
    }
}
