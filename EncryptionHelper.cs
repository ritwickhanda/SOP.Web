using System.Security.Cryptography;
using System.Text;

namespace SOP.Web.Helpers;

public static class EncryptionHelper
{
    // Encryption key will be set from configuration at startup
    public static string? CurrentEncryptionKey { get; set; }

    // Generate a cryptographically strong random key
    public static string GenerateNewKey() {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // 32 bytes = 256 bits
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            if (string.IsNullOrEmpty(CurrentEncryptionKey))
                throw new InvalidOperationException("Encryption key is not set.");

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using Aes aes = Aes.Create();
            var key = new PasswordDeriveBytes(CurrentEncryptionKey, null);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using MemoryStream ms = new MemoryStream();
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(cipherBytes, 0, cipherBytes.Length);
                cs.Close();
            }
            return Encoding.Unicode.GetString(ms.ToArray());
        }
        catch
        {
            return cipherText; // Decryption fail hone par original return karo (fallback)
        }
    }

    // Is method ka use aap ek baar encrypted string generate karne ke liye kar sakte hain
    public static string Encrypt(string clearText)
    {
        byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
        if (string.IsNullOrEmpty(CurrentEncryptionKey))
            throw new InvalidOperationException("Encryption key is not set.");

        using Aes aes = Aes.Create();
        var key = new PasswordDeriveBytes(CurrentEncryptionKey, null);
        aes.Key = key.GetBytes(32);
        aes.IV = key.GetBytes(16);
        using MemoryStream ms = new MemoryStream();
        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(clearBytes, 0, clearBytes.Length);
            cs.Close();
        }
        return Convert.ToBase64String(ms.ToArray());
    }
}
