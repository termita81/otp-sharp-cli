using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OtpSharpCli;

public static class CryptoHelper
{
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;

    public static string EncryptData(string data, SecureString password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iv = RandomNumberGenerator.GetBytes(IvSize);

        byte[]? key = null;
        IntPtr passwordPtr = IntPtr.Zero;

        try
        {
            passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
            var passwordBytes = SecureStringToBytes(password);

            try
            {
                key = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                var dataBytes = Encoding.UTF8.GetBytes(data);
                var encryptedData = aes.EncryptCbc(dataBytes, iv);

                var result = new EncryptedData
                {
                    Salt = Convert.ToBase64String(salt),
                    Iv = Convert.ToBase64String(iv),
                    Data = Convert.ToBase64String(encryptedData)
                };

                return JsonSerializer.Serialize(result);
            }
            finally
            {
                if (passwordBytes != null)
                    Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }
        finally
        {
            if (key != null)
                Array.Clear(key, 0, key.Length);

            if (passwordPtr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
        }
    }

    public static string DecryptData(string encryptedJson, SecureString password)
    {
        if (string.IsNullOrWhiteSpace(encryptedJson))
            throw new ArgumentException("Encrypted data cannot be null or empty");

        EncryptedData? encryptedData;
        try
        {
            var options = new JsonSerializerOptions
            {
                MaxDepth = 5,
                PropertyNameCaseInsensitive = false
            };
            encryptedData = JsonSerializer.Deserialize<EncryptedData>(encryptedJson, options);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid encrypted data format", ex);
        }

        if (encryptedData?.Salt == null || encryptedData.Iv == null || encryptedData.Data == null)
            throw new ArgumentException("Incomplete encrypted data");

        if (!IsValidBase64(encryptedData.Salt) ||
            !IsValidBase64(encryptedData.Iv) ||
            !IsValidBase64(encryptedData.Data))
            throw new ArgumentException("Invalid Base64 encoding in encrypted data");

        var salt = Convert.FromBase64String(encryptedData.Salt);
        var iv = Convert.FromBase64String(encryptedData.Iv);
        var data = Convert.FromBase64String(encryptedData.Data);

        byte[]? key = null;
        IntPtr passwordPtr = IntPtr.Zero;

        try
        {
            passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
            var passwordBytes = SecureStringToBytes(password);

            try
            {
                key = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                var decryptedData = aes.DecryptCbc(data, iv);
                return Encoding.UTF8.GetString(decryptedData);
            }
            finally
            {
                if (passwordBytes != null)
                    Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }
        finally
        {
            if (key != null)
                Array.Clear(key, 0, key.Length);

            if (passwordPtr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
        }
    }

    private static byte[] SecureStringToBytes(SecureString secureString)
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            var length = secureString.Length;
            var bytes = new byte[length * 2];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);

            var utf8Bytes = Encoding.UTF8.GetBytes(Encoding.Unicode.GetString(bytes));
            Array.Clear(bytes, 0, bytes.Length);
            return utf8Bytes;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }

    private static bool IsValidBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return false;

        try
        {
            Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private class EncryptedData
    {
        public string Salt { get; set; } = string.Empty;
        public string Iv { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}