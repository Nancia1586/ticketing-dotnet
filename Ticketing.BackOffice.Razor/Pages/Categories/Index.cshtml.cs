using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ICategoryService _categoryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ICategoryService categoryService, UserManager<ApplicationUser> userManager)
        {
            _categoryService = categoryService;
            _userManager = userManager;
        }

        public IList<Category> Categories { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewData["CurrentUser"] = await _userManager.GetUserAsync(User);
            }
            Categories = (await _categoryService.GetAllCategoriesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id, bool isActive)
        {
            await _categoryService.ToggleCategoryStatusAsync(id, isActive);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                TempData["SuccessMessage"] = "Catégorie supprimée avec succès.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Une erreur est survenue lors de la suppression.";
            }
            return RedirectToPage();
        }
    }
}

