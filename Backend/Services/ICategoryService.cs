using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryDTO>> GetAllAsync();
        Task<CategoryDTO> CreateAsync(CreateCategoryDTO dto);
        Task DeleteAsync(int id);
    }
}