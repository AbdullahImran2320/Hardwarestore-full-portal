using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Models;
using HardwareStorePortal.API.Repositories;

namespace HardwareStorePortal.API.Services
{
    public class BillService : IBillService
    {
        private readonly IBillRepository _billRepository;

        public BillService(IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }

        public async Task<List<BillDTO>> GetAllBillsAsync()
        {
            var bills = await _billRepository.GetAllAsync();
            return bills.Select(MapToDTO).ToList();
        }

        public async Task<BillDTO?> GetBillByIdAsync(int id)
        {
            var bill = await _billRepository.GetByIdAsync(id);
            return bill == null ? null : MapToDTO(bill);
        }

        public async Task<BillDTO> CreateBillAsync(CreateBillDTO dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Bill must have at least one item.");

            var method = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod.Trim();
            if (method != "Cash" && method != "Online")
                throw new InvalidOperationException("Payment method must be 'Cash' or 'Online'.");

            var bill = new Bill
            {
                CustomerId = dto.CustomerId,
                PaidAmount = dto.PaidAmount,
                PaymentMethod = method,
                BillDate = DateTime.UtcNow
            };

            var stockTransactions = new List<StockTransaction>();
            var productsToUpdate = new List<Product>();
            decimal total = 0;

            foreach (var item in dto.Items)
            {
                var product = await _billRepository.GetProductByIdAsync(item.ProductId);

                if (product == null)
                    throw new InvalidOperationException($"Product with Id {item.ProductId} not found.");

                if (product.StockQty < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{product.Name}'. Available: {product.StockQty}, Requested: {item.Quantity}");

                var grossLineTotal = product.SalePrice * item.Quantity;

                if (item.DiscountAmount < 0)
                    throw new InvalidOperationException($"Discount for '{product.Name}' cannot be negative.");

                if (item.DiscountAmount > grossLineTotal)
                    throw new InvalidOperationException(
                        $"Discount for '{product.Name}' (Rs. {item.DiscountAmount}) cannot exceed the line total (Rs. {grossLineTotal}).");

                var lineTotal = grossLineTotal - item.DiscountAmount;
                total += lineTotal;

                bill.BillItems.Add(new BillItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.SalePrice,
                    DiscountAmount = item.DiscountAmount,
                    LineTotal = lineTotal
                });
                product.StockQty -= item.Quantity;
                productsToUpdate.Add(product);

                stockTransactions.Add(new StockTransaction
                {
                    ProductId = product.Id,
                    Type = "Sale",
                    Quantity = item.Quantity
                    // Reference gets set inside the repository once Bill.Id exists
                });
            }

            bill.TotalAmount = total;
            bill.Status = DetermineStatus(total, dto.PaidAmount);

            var created = await _billRepository.CreateBillWithItemsAsync(bill, stockTransactions, productsToUpdate);

            // Re-fetch with includes so the returned DTO has full product/customer info
            var full = await _billRepository.GetByIdAsync(created.Id);
            return MapToDTO(full!);
        }

        public async Task<List<BillDTO>> GetBillsByCustomerAsync(int customerId)
        {
            var bills = await _billRepository.GetByCustomerIdAsync(customerId);
            return bills.Select(MapToDTO).ToList();
        }

        public async Task<List<BillDTO>> GetTodaysBillsAsync()
        {
            var bills = await _billRepository.GetTodaysBillsAsync();
            return bills.Select(MapToDTO).ToList();
        }

        public async Task<bool> DeleteBillAsync(int id)
        {
            var bill = await _billRepository.GetByIdAsync(id);
            if (bill == null) return false;

            // Deleting a bill puts the sold items back into stock — otherwise the
            // inventory would stay permanently short by whatever this bill sold.
            var reversalTransactions = new List<StockTransaction>();
            var productsToUpdate = new List<Product>();

            foreach (var item in bill.BillItems)
            {
                var product = await _billRepository.GetProductByIdAsync(item.ProductId);
                if (product == null) continue; // product may have been deleted separately; skip restocking it

                product.StockQty += item.Quantity;
                productsToUpdate.Add(product);

                reversalTransactions.Add(new StockTransaction
                {
                    ProductId = product.Id,
                    Type = "Reversal",
                    Quantity = item.Quantity,
                    Reference = $"Bill #{bill.Id} deleted"
                });
            }

            return await _billRepository.DeleteBillAsync(bill, reversalTransactions, productsToUpdate);
        }

        private static string DetermineStatus(decimal total, decimal paid)
        {
            if (paid <= 0) return "Unpaid";
            if (paid >= total) return "Paid";
            return "Partial";
        }

        private static BillDTO MapToDTO(Bill b)
        {
            return new BillDTO
            {
                Id = b.Id,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer?.Name,
                BillDate = b.BillDate,
                TotalAmount = b.TotalAmount,
                PaidAmount = b.PaidAmount,
                OutstandingAmount = b.TotalAmount - b.PaidAmount,
                Status = b.Status,
                PaymentMethod = b.PaymentMethod,
                Items = b.BillItems.Select(bi => new BillItemDTO
                {
                    ProductId = bi.ProductId,
                    ProductName = bi.Product?.Name ?? string.Empty,
                    Quantity = bi.Quantity,
                    UnitPrice = bi.UnitPrice,
                    DiscountAmount = bi.DiscountAmount,
                    LineTotal = bi.LineTotal
                }).ToList()
            };
        }
    }
}