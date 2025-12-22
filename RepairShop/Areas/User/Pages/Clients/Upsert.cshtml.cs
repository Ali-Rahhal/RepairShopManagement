using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using RepairShop.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace RepairShop.Areas.User.Pages.Clients
{
    [Authorize(Roles = SD.Role_Admin)]
    public class UpsertModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuditLogService _auditLogService;

        public UpsertModel(IUnitOfWork unitOfWork, AuditLogService als)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = als;
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
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Create, SD.Entity_Client, clientForUpsert.Id);
                    TempData["success"] = "Client created successfully";
                }
                else
                {
                    var clientFromDb = await _unitOfWork.Client.GetAsy(c => c.Id == clientForUpsert.Id && c.IsActive == true);
                    if (clientFromDb == null) return NotFound();
                    clientFromDb.Name = clientForUpsert.Name;
                    clientFromDb.Phone = clientForUpsert.Phone;
                    clientFromDb.Email = clientForUpsert.Email;
                    clientFromDb.Address = clientForUpsert.Address;
                    await _unitOfWork.Client.UpdateAsy(clientFromDb);
                    await _unitOfWork.SaveAsy();
                    await _auditLogService.AddLogAsy(SD.Action_Update, SD.Entity_Client, clientFromDb.Id);
                    TempData["success"] = "Client updated successfully";
                }
                

                return RedirectToPage("Index");
            }
            return Page();
        }
    }
}
