using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Models;
using HardwareStorePortal.API.Repositories;

namespace HardwareStorePortal.API.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;

        public CustomerService(ICustomerRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CustomerDTO>> GetAllCustomersAsync()
        {
            var customers = await _repository.GetAllAsync();
            return customers.Select(MapToDTO).ToList();
        }

        public async Task<List<CustomerWithBalanceDTO>> GetWithBalancesAsync()
        {
            var rows = await _repository.GetAllWithBalancesAsync();
            return rows.Select(r => new CustomerWithBalanceDTO
            {
                Id = r.Customer.Id,
                Name = r.Customer.Name,
                Phone = r.Customer.Phone,
                Address = r.Customer.Address,
                TotalOutstanding = r.TotalOutstanding,
                BillCount = r.BillCount
            }).ToList();
        }

        public async Task<CustomerDTO?> GetCustomerByIdAsync(int id)
        {
            var customer = await _repository.GetByIdAsync(id);
            return customer == null ? null : MapToDTO(customer);
        }

        public async Task<CustomerDTO> CreateCustomerAsync(CreateCustomerDTO dto)
        {
            var customer = new Customer
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Address = dto.Address
            };

            var created = await _repository.AddAsync(customer);
            return MapToDTO(created);
        }

        public async Task<bool> UpdateCustomerAsync(int id, UpdateCustomerDTO dto)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null) return false;

            customer.Name = dto.Name;
            customer.Phone = dto.Phone;
            customer.Address = dto.Address;

            return await _repository.UpdateAsync(customer);
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            // Repository throws InvalidOperationException if outstanding balance > 0
            return await _repository.DeleteAsync(id);
        }

        private static CustomerDTO MapToDTO(Customer c)
        {
            return new CustomerDTO
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Address = c.Address
            };
        }
    }
}