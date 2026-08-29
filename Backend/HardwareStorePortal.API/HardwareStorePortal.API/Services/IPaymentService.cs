using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentDTO> AddPaymentAsync(CreatePaymentDTO dto);
        Task<List<PaymentDTO>> GetPaymentsByBillAsync(int billId);
    }
}