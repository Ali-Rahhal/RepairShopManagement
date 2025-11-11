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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public EditModel(
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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

            userForUpdate = await _userManager.FindByIdAsync(id);
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
            if (!ModelState.IsValid)
            {
                Input.RoleList = await GetRoleList();
                return Page();
            }

            userForUpdate = await _userManager.FindByIdAsync(Input.Id);
            if (userForUpdate == null)
            {
                return NotFound();
            }

            // UserCode uniqueness check
            var existingUser = await _userManager.FindByNameAsync(Input.UserCode);
            if (existingUser != null && existingUser.Id != userForUpdate.Id)
            {
                ModelState.AddModelError(string.Empty, "UserCode already exists. Please choose a different UserCode.");
                Input.RoleList = await GetRoleList();
                return Page();
            }

            // Update user properties
            userForUpdate.UserName = Input.UserCode;
            userForUpdate.Role = Input.Role;

            // Handle password change
            var passwordResult = await HandlePasswordChange();
            if (passwordResult != null) return passwordResult;

            // Update roles
            var roleResult = await UpdateUserRoles();
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                Input.RoleList = await GetRoleList();
                return Page();
            }

            // Update user
            var updateResult = await _userManager.UpdateAsync(userForUpdate);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                Input.RoleList = await GetRoleList();
                return Page();
            }

            TempData["success"] = "User updated successfully";
            return RedirectToPage("Index");
        }

        private async Task<IEnumerable<SelectListItem>> GetRoleList()
        {
            return _roleManager.Roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            });
        }

        private async Task<IdentityResult> UpdateUserRoles()
        {
            var currentRoles = await _userManager.GetRolesAsync(userForUpdate);
            var removeResult = await _userManager.RemoveFromRolesAsync(userForUpdate, currentRoles);
            if (!removeResult.Succeeded) return removeResult;

            return await _userManager.AddToRoleAsync(userForUpdate, Input.Role);
        }

        private async Task<IActionResult> HandlePasswordChange()
        {
            if (string.IsNullOrEmpty(Input.OldPassword) && string.IsNullOrEmpty(Input.NewPassword))
            {
                return null; // No password change requested
            }

            if (string.IsNullOrEmpty(Input.OldPassword) || string.IsNullOrEmpty(Input.NewPassword))
            {
                ModelState.AddModelError(string.Empty, "Both current password and new password are required to change password.");
                Input.RoleList = await GetRoleList();
                return Page();
            }

            var isOldPasswordValid = await _userManager.CheckPasswordAsync(userForUpdate, Input.OldPassword);
            if (!isOldPasswordValid)
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                Input.RoleList = await GetRoleList();
                return Page();
            }

            var changeResult = await _userManager.ChangePasswordAsync(userForUpdate, Input.OldPassword, Input.NewPassword);
            if (!changeResult.Succeeded)
            {
                foreach (var error in changeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                Input.RoleList = await GetRoleList();
                return Page();
            }

            return null;
        }
    }
}

