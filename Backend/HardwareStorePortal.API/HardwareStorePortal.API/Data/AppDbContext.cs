using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bill>().Property(b => b.PaidAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Bill>().Property(b => b.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<BillItem>().Property(b => b.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<BillItem>().Property(b => b.LineTotal).HasPrecision(18, 2);
            modelBuilder.Entity<BillItem>().Property(b => b.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.PurchasePrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.SalePrice).HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }
    }
}