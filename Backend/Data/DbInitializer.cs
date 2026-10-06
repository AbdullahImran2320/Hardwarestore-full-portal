using HardwareStorePortal.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HardwareStorePortal.API.Data
{
    // Everything that used to run inline in Program.cs at startup.
    // It is also called after a database restore.
    public static class DbInitializer
    {
        private static readonly (string Username, string Role, string Password)[] SeedUsers =
        {
            ("admin", "Admin", "Admin123!"),
            ("Muneeb", "Admin", "muneeb786"),
            ("Shahid", "Staff", "Shahid123!")
        };

        public static void Initialize(AppDbContext db)
        {
            db.Database.Migrate();

            var productsNeedingCategory = db.Products
                .Where(p => p.CategoryId == null && p.LegacyCategoryText != null && p.LegacyCategoryText != "")
                .ToList();

            foreach (var product in productsNeedingCategory)
            {
                var categoryName = product.LegacyCategoryText!.Trim();

                var category = db.Categories.FirstOrDefault(c => c.Name.ToLower() == categoryName.ToLower());
                if (category == null)
                {
                    category = new Category { Name = categoryName };
                    db.Categories.Add(category);
                    db.SaveChanges();
                }

                product.CategoryId = category.Id;
            }

            db.SaveChanges();

            if (!db.Users.Any())
            {
                foreach (var seed in SeedUsers)
                {
                    db.Users.Add(new User
                    {
                        Username = seed.Username,
                        Role = seed.Role,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(seed.Password),
                        MustChangePassword = true
                    });
                }
                db.SaveChanges();
            }
            else
            {
                FlagSeedPasswordsForChange(db);
            }
        }

        // The seed passwords are public (they are in the source code), so any account that
        // still uses one of them must pick a new password at next login.
        private static void FlagSeedPasswordsForChange(AppDbContext db)
        {
            var changed = false;

            foreach (var seed in SeedUsers)
            {
                var username = seed.Username;
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                if (user == null || user.MustChangePassword) continue;

                try
                {
                    if (BCrypt.Net.BCrypt.Verify(seed.Password, user.PasswordHash))
                    {
                        user.MustChangePassword = true;
                        changed = true;
                    }
                }
                catch
                {
                    // Unreadable hash: leave this account alone.
                }
            }

            if (changed) db.SaveChanges();
        }
    }
}
