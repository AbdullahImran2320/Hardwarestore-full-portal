using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;

namespace HardwareStorePortal.API.Services
{
    public class ExcelExportService
    {
        private const string MoneyFormat = "#,##0.00";
        private const string DateFormat = "yyyy-mm-dd hh:mm";

        private readonly AppDbContext _context;

        public ExcelExportService(AppDbContext context)
        {
            _context = context;
        }

        // Headers match the product import template, so an export can be edited and imported back.
        public async Task<byte[]> ExportProductsAsync(bool includeCost)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Products");

            var headers = new List<string> { "Name", "Category", "Unit", "Stock", "Sale Price", "Reorder Level" };
            if (includeCost) headers.Add("Purchase Price");
            WriteHeader(ws, headers);

            var row = 2;
            foreach (var p in products)
            {
                ws.Cell(row, 1).Value = p.Name;
                ws.Cell(row, 2).Value = p.Category?.Name ?? p.LegacyCategoryText ?? string.Empty;
                ws.Cell(row, 3).Value = p.Unit;
                ws.Cell(row, 4).Value = (double)p.StockQty;
                SetMoney(ws.Cell(row, 5), p.SalePrice);
                ws.Cell(row, 6).Value = (double)p.ReorderLevel;
                if (includeCost) SetMoney(ws.Cell(row, 7), p.PurchasePrice);
                row++;
            }

            ws.Columns().AdjustToContents();
            return ToBytes(wb);
        }

        public async Task<byte[]> ExportCustomersAsync()
        {
            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();

            // Sums are done in memory because the SQLite provider cannot sum decimals in SQL.
            var bills = await _context.Bills
                .Where(b => b.CustomerId != null)
                .Select(b => new { b.CustomerId, b.TotalAmount, b.PaidAmount })
                .ToListAsync();

            var totals = bills
                .GroupBy(b => b.CustomerId!.Value)
                .ToDictionary(g => g.Key, g => new { Total = g.Sum(x => x.TotalAmount), Paid = g.Sum(x => x.PaidAmount) });

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Customers");
            WriteHeader(ws, new List<string> { "Name", "Phone", "Address", "Total Billed", "Total Paid", "Outstanding" });

            var row = 2;
            foreach (var c in customers)
            {
                decimal total = 0, paid = 0;
                if (totals.TryGetValue(c.Id, out var t))
                {
                    total = t.Total;
                    paid = t.Paid;
                }

                ws.Cell(row, 1).Value = c.Name;
                ws.Cell(row, 2).Value = c.Phone ?? string.Empty;
                ws.Cell(row, 3).Value = c.Address ?? string.Empty;
                SetMoney(ws.Cell(row, 4), total);
                SetMoney(ws.Cell(row, 5), paid);
                SetMoney(ws.Cell(row, 6), total - paid);
                row++;
            }

            ws.Columns().AdjustToContents();
            return ToBytes(wb);
        }

        public async Task<byte[]> ExportBillsAsync(DateTime? from, DateTime? to)
        {
            var query = _context.Bills
                .Include(b => b.Customer)
                .Include(b => b.BillItems)
                    .ThenInclude(i => i.Product)
                .AsQueryable();

            if (from.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(b => b.BillDate >= fromUtc);
            }

            if (to.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
                query = query.Where(b => b.BillDate < toUtc);
            }

            var bills = (await query.ToListAsync()).OrderByDescending(b => b.BillDate).ToList();

            using var wb = new XLWorkbook();

            var ws = wb.Worksheets.Add("Bills");
            WriteHeader(ws, new List<string> { "Bill #", "Date", "Customer", "Total", "Paid", "Outstanding", "Status", "Payment Method" });

            var row = 2;
            foreach (var b in bills)
            {
                ws.Cell(row, 1).Value = (double)b.Id;
                SetDate(ws.Cell(row, 2), b.BillDate);
                ws.Cell(row, 3).Value = b.Customer?.Name ?? "Walk-in";
                SetMoney(ws.Cell(row, 4), b.TotalAmount);
                SetMoney(ws.Cell(row, 5), b.PaidAmount);
                SetMoney(ws.Cell(row, 6), b.TotalAmount - b.PaidAmount);
                ws.Cell(row, 7).Value = b.Status;
                ws.Cell(row, 8).Value = b.PaymentMethod;
                row++;
            }
            ws.Columns().AdjustToContents();

            var items = wb.Worksheets.Add("Items");
            WriteHeader(items, new List<string> { "Bill #", "Date", "Product", "Quantity", "Unit Price", "Discount", "Line Total" });

            row = 2;
            foreach (var b in bills)
            {
                foreach (var i in b.BillItems)
                {
                    items.Cell(row, 1).Value = (double)b.Id;
                    SetDate(items.Cell(row, 2), b.BillDate);
                    items.Cell(row, 3).Value = i.Product?.Name ?? string.Empty;
                    items.Cell(row, 4).Value = (double)i.Quantity;
                    SetMoney(items.Cell(row, 5), i.UnitPrice);
                    SetMoney(items.Cell(row, 6), i.DiscountAmount);
                    SetMoney(items.Cell(row, 7), i.LineTotal);
                    row++;
                }
            }
            items.Columns().AdjustToContents();

            return ToBytes(wb);
        }

        // ---------- helpers ----------

        private static void WriteHeader(IXLWorksheet ws, List<string> headers)
        {
            for (var i = 0; i < headers.Count; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var range = ws.Range(1, 1, 1, headers.Count);
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9480F");

            ws.SheetView.FreezeRows(1);
        }

        private static void SetMoney(IXLCell cell, decimal value)
        {
            cell.Value = (double)value;
            cell.Style.NumberFormat.Format = MoneyFormat;
        }

        private static void SetDate(IXLCell cell, DateTime utcValue)
        {
            var local = DateTime.SpecifyKind(utcValue, DateTimeKind.Utc).ToLocalTime();
            cell.Value = local;
            cell.Style.NumberFormat.Format = DateFormat;
        }

        private static byte[] ToBytes(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
