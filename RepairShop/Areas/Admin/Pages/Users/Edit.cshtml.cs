using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RepairShop.Areas.Admin.Pages.Users
{
    [Authorize(Roles = SD.Role_Admin)]
    public class EditModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public EditModel(IUnitOfWork unitOfWork, 
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public AppUser? userForUpdate { get; set; }
        [BindProperty]
        public InputModel Input { get; set; }
        public class InputModel
        {
            public string? Id { get; set; }

            [Required]
            [RegularExpression("^[0-9]+$")]
            [Range(1, 99999)]
            public string UserCode { get; set; }

            [Required]
            public string Role { get; set; }
            [ValidateNever]
            public IEnumerable<SelectListItem> RoleList { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string? OldPassword { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }


        public async Task<IActionResult> OnGet(string? id)
        {
            userForUpdate = new AppUser();

            userForUpdate = await _unitOfWork.AppUser.GetAsy(o => (o.Id).Equals(id));
            if (userForUpdate == null)
            {
                return NotFound();
            }
            Input = new InputModel()
            {
                Id = userForUpdate.Id,
                UserCode = userForUpdate.UserName,
                Role = userForUpdate.Role,
                RoleList = _roleManager.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                })
            };

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            userForUpdate = await _unitOfWork.AppUser.GetAsy(o => (o.Id).Equals(Input.Id));
            do
            {
                if (ModelState.IsValid)
                {
                    if (userForUpdate == null)
                    {
                        return NotFound();
                    }
                    var exists = (await _unitOfWork.AppUser.GetAllAsy()).Select(u => u.UserName).Contains(Input.UserCode);
                    if (exists && !Input.UserCode.Equals(userForUpdate.UserName))
                    {
                        ModelState.AddModelError(string.Empty, "UserCode already exists. Please choose a different UserCode.");
                        break;
                    }
                    userForUpdate.UserName = Input.UserCode;
                    userForUpdate.Role = Input.Role;

                    if(Input.OldPassword != null)
                    {
                        if (Input.NewPassword == null)
                        {
                            ModelState.AddModelError(string.Empty, "Please enter a NewPassword.");
                            break;
                        }
                        var result = await _userManager.ChangePasswordAsync(userForUpdate, Input.OldPassword, Input.NewPassword);
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            break;
                        }
                    }
                    
                    await _unitOfWork.AppUser.UpdateAsy(userForUpdate);
                    await _unitOfWork.SaveAsy();
                    await _userManager.AddToRoleAsync(userForUpdate, Input.Role);//assign role to user based on selection
                    TempData["success"] = "User updated successfully";
                    return RedirectToPage("Index");
                }
            }while(false);

            Input.RoleList = _roleManager.Roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            });
            return Page();
        }
    }
}

