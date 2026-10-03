using System.Security.Cryptography;

namespace HardwareStorePortal.API.Services
{
    // The signing key must never be a value that is committed to the (public) repository.
    // The installed app therefore creates its own random key on first run and keeps it
    // next to the database. Developers can still use a real key from user-secrets.
    public static class JwtKeyProvider
    {
        private const string Placeholder = "REPLACE_WITH_YOUR_OWN_SECRET_KEY_IN_USER_SECRETS";

        public static string Resolve(string? configuredKey, string dataFolder, bool isDevelopment)
        {
            if (isDevelopment
                && !string.IsNullOrWhiteSpace(configuredKey)
                && configuredKey != Placeholder
                && configuredKey.Length >= 32)
            {
                return configuredKey;
            }

            var keyFile = Path.Combine(dataFolder, "jwt.key");

            if (File.Exists(keyFile))
            {
                var existing = File.ReadAllText(keyFile).Trim();
                if (existing.Length >= 32) return existing;
            }

            var newKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            File.WriteAllText(keyFile, newKey);
            return newKey;
        }
    }
}
