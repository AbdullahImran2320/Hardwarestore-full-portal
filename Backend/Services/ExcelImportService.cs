using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Services
{
    // Preview-then-commit Excel import. Nothing is written to the database by a preview.
    // Every user-facing problem is reported with InvalidOperationException.
    public class ExcelImportService
    {
        private const int MaxRows = 5000;
        private const int DefaultReorderLevel = 5;

        private readonly AppDbContext _context;

        public ExcelImportService(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // Templates
        // ============================================================

        public byte[] BuildProductTemplate(IEnumerable<string> existingCategories)
        {
            using var wb = new XLWorkbook();

            var ws = wb.Worksheets.Add("Products");
            var headers = new[] { "Name", "Category", "Unit", "Stock", "Purchase Price", "Sale Price", "Reorder Level" };
            WriteHeader(ws, headers);
            ws.Columns().AdjustToContents();

            var help = wb.Worksheets.Add("Instructions");
            help.Cell(1, 1).Value = "How to fill the Products sheet";
            help.Cell(1, 1).Style.Font.Bold = true;
            var lines = new[]
            {
                "Required columns: Name, Category, Unit, Sale Price.",
                "Optional columns: Stock, Purchase Price, Reorder Level (default 5 for new products).",
                "A product is matched by Name + Category (not case sensitive). A match is updated, anything else is created.",
                "A category that does not exist yet is created automatically.",
                "For an existing product, a different Stock value is saved and logged as an 'Adjustment' stock transaction.",
                "Do not rename the column headers. Column order does not matter.",
                "Maximum " + MaxRows + " rows per file. Empty rows are ignored."
            };
            for (var i = 0; i < lines.Length; i++)
                help.Cell(i + 3, 1).Value = lines[i];

            help.Cell(11, 1).Value = "Existing categories";
            help.Cell(11, 1).Style.Font.Bold = true;
            var r = 12;
            foreach (var c in existingCategories)
                help.Cell(r++, 1).Value = c;
            help.Column(1).AdjustToContents();

            return ToBytes(wb);
        }

        public byte[] BuildCustomerTemplate()
        {
            using var wb = new XLWorkbook();

            var ws = wb.Worksheets.Add("Customers");
            WriteHeader(ws, new[] { "Name", "Phone", "Address" });
            ws.Column(2).Style.NumberFormat.Format = "@"; // keep leading zeros in phone numbers
            ws.Columns().AdjustToContents();

            var help = wb.Worksheets.Add("Instructions");
            help.Cell(1, 1).Value = "How to fill the Customers sheet";
            help.Cell(1, 1).Style.Font.Bold = true;
            var lines = new[]
            {
                "Required column: Name. Phone and Address are optional.",
                "A customer is matched by phone number (digits only). If Phone is empty, by Name.",
                "A match is updated, anything else is created. Existing balances and bills are never touched.",
                "Format the Phone column as Text so leading zeros are kept.",
                "Maximum " + MaxRows + " rows per file. Empty rows are ignored."
            };
            for (var i = 0; i < lines.Length; i++)
                help.Cell(i + 3, 1).Value = lines[i];
            help.Column(1).AdjustToContents();

            return ToBytes(wb);
        }

        // ============================================================
        // Products
        // ============================================================

        private class ParsedProduct
        {
            public int RowNumber;
            public string Name = "";
            public string Category = "";
            public string Unit = "";
            public int? Stock;
            public decimal? PurchasePrice;
            public decimal? SalePrice;
            public int? ReorderLevel;
            public List<string> Errors = new();
            public Product? Existing;
        }

        private class ProductPlan
        {
            public List<ParsedProduct> Rows = new();
            public Dictionary<string, Category> Categories = new();
            public List<string> NewCategories = new();
        }

        public async Task<ImportPreviewDTO> PreviewProductsAsync(Stream stream)
        {
            var plan = await PlanProductsAsync(stream);

            var preview = new ImportPreviewDTO
            {
                TotalRows = plan.Rows.Count,
                NewCategories = plan.NewCategories
            };

            foreach (var row in plan.Rows)
            {
                var action = row.Errors.Count > 0 ? "Error" : row.Existing != null ? "Update" : "Create";
                if (action == "Error") preview.ErrorCount++;
                else if (action == "Update") preview.ToUpdate++;
                else preview.ToCreate++;

                preview.Rows.Add(new ImportRowDTO
                {
                    RowNumber = row.RowNumber,
                    Action = action,
                    Messages = row.Errors,
                    Name = row.Name,
                    Category = row.Category,
                    Unit = row.Unit,
                    Stock = row.Stock,
                    PurchasePrice = row.PurchasePrice,
                    SalePrice = row.SalePrice,
                    ReorderLevel = row.ReorderLevel
                });
            }

            return preview;
        }

        public async Task<ImportResultDTO> CommitProductsAsync(Stream stream, bool skipInvalid)
        {
            var plan = await PlanProductsAsync(stream);

            var errorRows = plan.Rows.Count(r => r.Errors.Count > 0);
            if (errorRows > 0 && !skipInvalid)
                throw new InvalidOperationException(errorRows + " row(s) have errors. Fix the file, or choose to skip the invalid rows.");

            var validRows = plan.Rows.Where(r => r.Errors.Count == 0).ToList();
            if (validRows.Count == 0)
                throw new InvalidOperationException("There are no valid rows to import.");

            var result = new ImportResultDTO { Skipped = errorRows };
            var reference = "Excel import " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var name in plan.NewCategories)
                {
                    var category = new Category { Name = name };
                    _context.Categories.Add(category);
                    plan.Categories[name.ToLowerInvariant()] = category;
                    result.CategoriesCreated++;
                }

                foreach (var row in validRows)
                {
                    if (row.Existing != null)
                    {
                        var product = row.Existing;
                        product.Unit = row.Unit;
                        product.SalePrice = row.SalePrice!.Value;
                        if (row.PurchasePrice.HasValue) product.PurchasePrice = row.PurchasePrice.Value;
                        if (row.ReorderLevel.HasValue) product.ReorderLevel = row.ReorderLevel.Value;

                        if (row.Stock.HasValue && row.Stock.Value != product.StockQty)
                        {
                            var difference = row.Stock.Value - product.StockQty;
                            product.StockQty = row.Stock.Value;
                            _context.StockTransactions.Add(new StockTransaction
                            {
                                Product = product,
                                Type = "Adjustment",
                                Quantity = difference,
                                Reference = reference
                            });
                        }

                        result.Updated++;
                    }
                    else
                    {
                        _context.Products.Add(new Product
                        {
                            Name = row.Name,
                            Category = plan.Categories[row.Category.ToLowerInvariant()],
                            Unit = row.Unit,
                            StockQty = row.Stock ?? 0,
                            PurchasePrice = row.PurchasePrice ?? 0,
                            SalePrice = row.SalePrice!.Value,
                            ReorderLevel = row.ReorderLevel ?? DefaultReorderLevel
                        });
                        result.Created++;
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return result;
        }

        private async Task<ProductPlan> PlanProductsAsync(Stream stream)
        {
            var plan = new ProductPlan { Rows = ParseProducts(stream) };

            var products = await _context.Products.Include(p => p.Category).ToListAsync();
            var existing = new Dictionary<string, Product>();
            foreach (var p in products)
            {
                var key = ProductKey(p.Name, p.Category?.Name ?? p.LegacyCategoryText ?? "");
                if (!existing.ContainsKey(key)) existing[key] = p;
            }

            var categories = await _context.Categories.ToListAsync();
            foreach (var c in categories)
            {
                var key = c.Name.ToLowerInvariant();
                if (!plan.Categories.ContainsKey(key)) plan.Categories[key] = c;
            }

            var seen = new Dictionary<string, int>();
            foreach (var row in plan.Rows)
            {
                if (row.Name.Length == 0 || row.Category.Length == 0) continue;

                var key = ProductKey(row.Name, row.Category);
                if (seen.TryGetValue(key, out var firstRow))
                {
                    row.Errors.Add("Duplicate of row " + firstRow + " (same name and category).");
                    continue;
                }
                seen[key] = row.RowNumber;

                if (existing.TryGetValue(key, out var match)) row.Existing = match;
            }

            var newCategoryKeys = new HashSet<string>();
            foreach (var row in plan.Rows)
            {
                if (row.Errors.Count > 0 || row.Category.Length == 0) continue;
                var key = row.Category.ToLowerInvariant();
                if (!plan.Categories.ContainsKey(key) && newCategoryKeys.Add(key))
                    plan.NewCategories.Add(row.Category);
            }

            return plan;
        }

        private static string ProductKey(string name, string category)
        {
            return name.Trim().ToLowerInvariant() + "|" + category.Trim().ToLowerInvariant();
        }

        private static List<ParsedProduct> ParseProducts(Stream stream)
        {
            using var wb = OpenWorkbook(stream);
            var ws = OpenFirstSheet(wb);
            var headers = ReadHeaders(ws);

            var nameCol = FindColumn(headers, "name", "productname", "product");
            var categoryCol = FindColumn(headers, "category");
            var unitCol = FindColumn(headers, "unit");
            var saleCol = FindColumn(headers, "saleprice", "price", "sellingprice");
            var stockCol = FindColumn(headers, "stock", "stockqty", "quantity", "qty");
            var costCol = FindColumn(headers, "purchaseprice", "costprice", "cost");
            var reorderCol = FindColumn(headers, "reorderlevel", "reorder");

            var missing = new List<string>();
            if (nameCol == null) missing.Add("Name");
            if (categoryCol == null) missing.Add("Category");
            if (unitCol == null) missing.Add("Unit");
            if (saleCol == null) missing.Add("Sale Price");
            if (missing.Count > 0)
                throw new InvalidOperationException("Missing required column(s): " + string.Join(", ", missing) + ". Download the template to see the expected headers.");

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaxRows)
                throw new InvalidOperationException("The file has more than " + MaxRows + " rows. Split it into smaller files.");

            var rows = new List<ParsedProduct>();

            for (var r = 2; r <= lastRow; r++)
            {
                var row = new ParsedProduct
                {
                    RowNumber = r,
                    Name = ws.Cell(r, nameCol!.Value).GetString().Trim(),
                    Category = ws.Cell(r, categoryCol!.Value).GetString().Trim(),
                    Unit = ws.Cell(r, unitCol!.Value).GetString().Trim()
                };

                var saleState = ReadNumber(ws.Cell(r, saleCol!.Value), out var sale);
                var stockState = NumberState.Empty; decimal stock = 0;
                var costState = NumberState.Empty; decimal cost = 0;
                var reorderState = NumberState.Empty; decimal reorder = 0;
                if (stockCol != null) stockState = ReadNumber(ws.Cell(r, stockCol.Value), out stock);
                if (costCol != null) costState = ReadNumber(ws.Cell(r, costCol.Value), out cost);
                if (reorderCol != null) reorderState = ReadNumber(ws.Cell(r, reorderCol.Value), out reorder);

                var isBlank = row.Name.Length == 0 && row.Category.Length == 0 && row.Unit.Length == 0
                    && saleState == NumberState.Empty && stockState == NumberState.Empty
                    && costState == NumberState.Empty && reorderState == NumberState.Empty;
                if (isBlank) continue;

                if (row.Name.Length == 0) row.Errors.Add("Name is required.");
                if (row.Category.Length == 0) row.Errors.Add("Category is required.");
                if (row.Unit.Length == 0) row.Errors.Add("Unit is required.");

                if (saleState != NumberState.Ok || sale <= 0)
                    row.Errors.Add("Sale Price must be a number greater than zero.");
                else
                    row.SalePrice = sale;

                if (stockState == NumberState.Invalid || (stockState == NumberState.Ok && !IsWholeNumber(stock)))
                    row.Errors.Add("Stock must be a whole number, zero or more.");
                else if (stockState == NumberState.Ok)
                    row.Stock = (int)stock;

                if (costState == NumberState.Invalid || (costState == NumberState.Ok && cost < 0))
                    row.Errors.Add("Purchase Price must be a number, zero or more.");
                else if (costState == NumberState.Ok)
                    row.PurchasePrice = cost;

                if (reorderState == NumberState.Invalid || (reorderState == NumberState.Ok && !IsWholeNumber(reorder)))
                    row.Errors.Add("Reorder Level must be a whole number, zero or more.");
                else if (reorderState == NumberState.Ok)
                    row.ReorderLevel = (int)reorder;

                rows.Add(row);
            }

            return rows;
        }

        // ============================================================
        // Customers
        // ============================================================

        private class ParsedCustomer
        {
            public int RowNumber;
            public string Name = "";
            public string Phone = "";
            public string Address = "";
            public List<string> Errors = new();
            public Customer? Existing;
        }

        public async Task<ImportPreviewDTO> PreviewCustomersAsync(Stream stream)
        {
            var rows = await PlanCustomersAsync(stream);

            var preview = new ImportPreviewDTO { TotalRows = rows.Count };

            foreach (var row in rows)
            {
                var action = row.Errors.Count > 0 ? "Error" : row.Existing != null ? "Update" : "Create";
                if (action == "Error") preview.ErrorCount++;
                else if (action == "Update") preview.ToUpdate++;
                else preview.ToCreate++;

                preview.Rows.Add(new ImportRowDTO
                {
                    RowNumber = row.RowNumber,
                    Action = action,
                    Messages = row.Errors,
                    Name = row.Name,
                    Phone = row.Phone,
                    Address = row.Address
                });
            }

            return preview;
        }

        public async Task<ImportResultDTO> CommitCustomersAsync(Stream stream, bool skipInvalid)
        {
            var rows = await PlanCustomersAsync(stream);

            var errorRows = rows.Count(r => r.Errors.Count > 0);
            if (errorRows > 0 && !skipInvalid)
                throw new InvalidOperationException(errorRows + " row(s) have errors. Fix the file, or choose to skip the invalid rows.");

            var validRows = rows.Where(r => r.Errors.Count == 0).ToList();
            if (validRows.Count == 0)
                throw new InvalidOperationException("There are no valid rows to import.");

            var result = new ImportResultDTO { Skipped = errorRows };

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in validRows)
                {
                    if (row.Existing != null)
                    {
                        var customer = row.Existing;
                        customer.Name = row.Name;
                        if (row.Address.Length > 0) customer.Address = row.Address;
                        if (string.IsNullOrWhiteSpace(customer.Phone) && row.Phone.Length > 0) customer.Phone = row.Phone;
                        result.Updated++;
                    }
                    else
                    {
                        _context.Customers.Add(new Customer
                        {
                            Name = row.Name,
                            Phone = row.Phone,
                            Address = row.Address.Length > 0 ? row.Address : null
                        });
                        result.Created++;
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return result;
        }

        private async Task<List<ParsedCustomer>> PlanCustomersAsync(Stream stream)
        {
            var rows = ParseCustomers(stream);

            var customers = await _context.Customers.ToListAsync();
            var byPhone = new Dictionary<string, Customer>();
            var byName = new Dictionary<string, Customer>();
            foreach (var c in customers)
            {
                var digits = DigitsOnly(c.Phone);
                if (digits.Length > 0 && !byPhone.ContainsKey(digits)) byPhone[digits] = c;

                var nameKey = c.Name.Trim().ToLowerInvariant();
                if (!byName.ContainsKey(nameKey)) byName[nameKey] = c;
            }

            var seen = new Dictionary<string, int>();
            foreach (var row in rows)
            {
                if (row.Name.Length == 0) continue;

                var digits = DigitsOnly(row.Phone);
                var key = digits.Length > 0 ? "p:" + digits : "n:" + row.Name.ToLowerInvariant();

                if (seen.TryGetValue(key, out var firstRow))
                {
                    row.Errors.Add("Duplicate of row " + firstRow + " (same customer).");
                    continue;
                }
                seen[key] = row.RowNumber;

                Customer? match = null;
                if (digits.Length > 0) byPhone.TryGetValue(digits, out match);
                else byName.TryGetValue(row.Name.ToLowerInvariant(), out match);

                row.Existing = match;
            }

            return rows;
        }

        private static List<ParsedCustomer> ParseCustomers(Stream stream)
        {
            using var wb = OpenWorkbook(stream);
            var ws = OpenFirstSheet(wb);
            var headers = ReadHeaders(ws);

            var nameCol = FindColumn(headers, "name", "customername", "customer");
            var phoneCol = FindColumn(headers, "phone", "phonenumber", "mobile", "contact");
            var addressCol = FindColumn(headers, "address");

            if (nameCol == null)
                throw new InvalidOperationException("Missing required column: Name. Download the template to see the expected headers.");

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaxRows)
                throw new InvalidOperationException("The file has more than " + MaxRows + " rows. Split it into smaller files.");

            var rows = new List<ParsedCustomer>();
            for (var r = 2; r <= lastRow; r++)
            {
                var row = new ParsedCustomer
                {
                    RowNumber = r,
                    Name = ws.Cell(r, nameCol.Value).GetString().Trim(),
                    Phone = phoneCol != null ? ws.Cell(r, phoneCol.Value).GetString().Trim() : "",
                    Address = addressCol != null ? ws.Cell(r, addressCol.Value).GetString().Trim() : ""
                };

                if (row.Name.Length == 0 && row.Phone.Length == 0 && row.Address.Length == 0) continue;

                if (row.Name.Length == 0) row.Errors.Add("Name is required.");
                rows.Add(row);
            }

            return rows;
        }

        // ============================================================
        // Shared helpers
        // ============================================================

        private enum NumberState { Empty, Ok, Invalid }

        private static NumberState ReadNumber(IXLCell cell, out decimal value)
        {
            value = 0;
            if (cell.IsEmpty()) return NumberState.Empty;

            if (cell.DataType == XLDataType.Number)
            {
                var d = cell.GetDouble();
                if (double.IsNaN(d) || Math.Abs(d) > 1_000_000_000_000d) return NumberState.Invalid;
                value = Math.Round((decimal)d, 2);
                return NumberState.Ok;
            }

            var text = cell.GetString().Trim();
            if (text.Length == 0) return NumberState.Empty;

            text = text.Replace(",", "")
                       .Replace("Rs.", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("Rs", "", StringComparison.OrdinalIgnoreCase)
                       .Trim();

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                ? NumberState.Ok
                : NumberState.Invalid;
        }

        private static bool IsWholeNumber(decimal value)
        {
            return value >= 0 && value <= 100_000_000m && value == Math.Floor(value);
        }

        private static string DigitsOnly(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return new string(text.Where(char.IsDigit).ToArray());
        }

        private static XLWorkbook OpenWorkbook(Stream stream)
        {
            try
            {
                return new XLWorkbook(stream);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Could not read the file. Please upload a valid .xlsx Excel file.");
            }
        }

        private static IXLWorksheet OpenFirstSheet(XLWorkbook wb)
        {
            try
            {
                return wb.Worksheet(1);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("The Excel file has no worksheet.");
            }
        }

        private static Dictionary<string, int> ReadHeaders(IXLWorksheet ws)
        {
            var map = new Dictionary<string, int>();
            for (var c = 1; c <= 30; c++)
            {
                var key = NormalizeHeader(ws.Cell(1, c).GetString());
                if (key.Length > 0 && !map.ContainsKey(key)) map[key] = c;
            }
            return map;
        }

        private static string NormalizeHeader(string text)
        {
            return new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        private static int? FindColumn(Dictionary<string, int> headers, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                if (headers.TryGetValue(alias, out var column)) return column;
            }
            return null;
        }

        private static void WriteHeader(IXLWorksheet ws, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var range = ws.Range(1, 1, 1, headers.Length);
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9480F");
            ws.SheetView.FreezeRows(1);
        }

        private static byte[] ToBytes(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
