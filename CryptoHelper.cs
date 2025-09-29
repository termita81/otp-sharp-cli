using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OtpSharp;

public static class CryptoHelper
{
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;

    public static string EncryptData(string data, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iv = RandomNumberGenerator.GetBytes(IvSize);

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

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

    public static string DecryptData(string encryptedJson, string password)
    {
        var encryptedData = JsonSerializer.Deserialize<EncryptedData>(encryptedJson)!;

        var salt = Convert.FromBase64String(encryptedData.Salt);
        var iv = Convert.FromBase64String(encryptedData.Iv);
        var data = Convert.FromBase64String(encryptedData.Data);

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        var decryptedData = aes.DecryptCbc(data, iv);
        return Encoding.UTF8.GetString(decryptedData);
    }

    private class EncryptedData
    {
        public string Salt { get; set; } = string.Empty;
        public string Iv { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}