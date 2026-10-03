using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerDTO>> GetAllCustomersAsync();
        Task<CustomerDTO?> GetCustomerByIdAsync(int id);
        Task<CustomerDTO> CreateCustomerAsync(CreateCustomerDTO dto);
        Task<bool> UpdateCustomerAsync(int id, UpdateCustomerDTO dto);
        Task<bool> DeleteCustomerAsync(int id);
    }
}