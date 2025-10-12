using System.Security.Cryptography;
using System.Text;

namespace OtpSharpCli;

public static class TotpGenerator
{
    private const int TimeStep = 30;
    private const int CodeLength = 6;

    public static string GenerateCode(string secret)
    {
        var secretBytes = Base32Decode(secret);
        var timeCounter = GetTimeCounter();
        return GenerateHotp(secretBytes, timeCounter);
    }

    public static int GetRemainingSeconds()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return TimeStep - (int)(now % TimeStep);
    }

    private static long GetTimeCounter()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return now / TimeStep;
    }

    private static string GenerateHotp(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[hash.Length - 1] & 0x0F;
        var code = ((hash[offset] & 0x7F) << 24) |
                   ((hash[offset + 1] & 0xFF) << 16) |
                   ((hash[offset + 2] & 0xFF) << 8) |
                   (hash[offset + 3] & 0xFF);

        code %= (int)Math.Pow(10, CodeLength);
        return code.ToString($"D{CodeLength}");
    }

    private static byte[] Base32Decode(string base32)
    {
        if (string.IsNullOrWhiteSpace(base32))
            throw new ArgumentException("Base32 string cannot be null or empty");

        base32 = base32.Replace(" ", "").Replace("-", "").ToUpper();

        if (base32.Length == 0)
            throw new ArgumentException("Base32 string is empty after cleanup");

        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = new StringBuilder();

        foreach (var c in base32)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
                throw new ArgumentException($"Invalid Base32 character: {c}");
            bits.Append(Convert.ToString(value, 2).PadLeft(5, '0'));
        }

        if (bits.Length % 8 != 0 && bits.Length % 8 > 5)
            throw new ArgumentException("Invalid Base32 string length");

        var result = new List<byte>();
        for (int i = 0; i < bits.Length; i += 8)
        {
            if (i + 8 <= bits.Length)
            {
                var byteString = bits.ToString(i, 8);
                result.Add(Convert.ToByte(byteString, 2));
            }
        }

        if (result.Count == 0)
            throw new ArgumentException("Base32 decoding resulted in empty data");

        return result.ToArray();
    }
}