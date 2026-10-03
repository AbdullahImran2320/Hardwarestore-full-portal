using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Models;
using HardwareStorePortal.API.Repositories;

namespace HardwareStorePortal.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repository;

        public PaymentService(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<PaymentDTO> AddPaymentAsync(CreatePaymentDTO dto)
        {
            var bill = await _repository.GetBillByIdAsync(dto.BillId);
            if (bill == null)
                throw new InvalidOperationException($"Bill with Id {dto.BillId} not found.");

            var outstanding = bill.TotalAmount - bill.PaidAmount;
            if (dto.Amount <= 0)
                throw new InvalidOperationException("Payment amount must be greater than zero.");
            if (dto.Amount > outstanding)
                throw new InvalidOperationException(
                    $"Payment exceeds outstanding balance. Outstanding: {outstanding}, Attempted: {dto.Amount}");

            var method = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod.Trim();
            if (method != "Cash" && method != "Online")
                throw new InvalidOperationException("Payment method must be 'Cash' or 'Online'.");

            var payment = new Payment
            {
                BillId = dto.BillId,
                Amount = dto.Amount,
                Note = dto.Note,
                PaymentMethod = method
            };

            bill.PaidAmount += dto.Amount;
            bill.Status = bill.PaidAmount >= bill.TotalAmount ? "Paid"
                         : bill.PaidAmount > 0 ? "Partial"
                         : "Unpaid";

            var created = await _repository.AddPaymentAsync(payment, bill);

            return new PaymentDTO
            {
                Id = created.Id,
                BillId = created.BillId,
                Amount = created.Amount,
                PaymentDate = created.PaymentDate,
                Note = created.Note,
                PaymentMethod = created.PaymentMethod
            };
        }

        public async Task<List<PaymentDTO>> GetPaymentsByBillAsync(int billId)
        {
            var payments = await _repository.GetByBillIdAsync(billId);
            return payments.Select(p => new PaymentDTO
            {
                Id = p.Id,
                BillId = p.BillId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                Note = p.Note,
                PaymentMethod = p.PaymentMethod
            }).ToList();
        }
    }
}