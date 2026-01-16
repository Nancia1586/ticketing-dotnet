using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);
        Task ToggleCategoryStatusAsync(int id, bool isActive);
        Task<List<(string Name, int Count)>> GetCategoryCountsAsync(int? organizerId = null);
    }
}

