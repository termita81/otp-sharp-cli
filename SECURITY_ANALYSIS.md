# Security Analysis - OTP Sharp CLI

## Executive Summary

This document outlines security vulnerabilities discovered in the OTP Sharp CLI application during a comprehensive security review. The application contains **4 critical** and **2 high-priority** security issues that require immediate attention, particularly around sensitive data handling in memory.

## Critical Issues (Fix Immediately)

### 1. Memory Disclosure Vulnerability - CRITICAL
**File:** `CryptoHelper.cs`
**Severity:** Critical

**Issue:** The derived encryption key remains in managed memory without secure cleanup, potentially allowing memory dumps to expose the key.

**Current Code:**
```csharp
var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
using var aes = Aes.Create();
aes.Key = key; // Key bytes exposed in managed memory
```

**Secure Fix:**
```csharp
public static string EncryptData(string data, string password)
{
    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var iv = RandomNumberGenerator.GetBytes(IvSize);

    using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
    var key = keyDerivation.GetBytes(KeySize);

    try
    {
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
        // Clear sensitive data from memory
        Array.Clear(key, 0, key.Length);
    }
}
```

### 2. Password Memory Exposure - CRITICAL
**File:** `Program.cs`
**Severity:** Critical

**Issue:** Master password stored in StringBuilder and string objects without secure cleanup, persisting in memory until garbage collection.

**Current Code:**
```csharp
var password = new StringBuilder();
// ... password building logic
return password.ToString(); // Exposes password in string pool
```

**Secure Fix:**
```csharp
static SecureString GetPassword()
{
    Console.Write("Enter master password: ");
    var securePassword = new SecureString();

    if (!Console.IsInputRedirected)
    {
        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (securePassword.Length > 0)
                {
                    securePassword.RemoveAt(securePassword.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                securePassword.AppendChar(key.KeyChar);
                Console.Write("*");
            }
        }

        Console.WriteLine();
        securePassword.MakeReadOnly();
        return securePassword;
    }
    else
    {
        var password = Console.ReadLine() ?? "";
        foreach (char c in password)
        {
            securePassword.AppendChar(c);
        }
        securePassword.MakeReadOnly();
        return securePassword;
    }
}
```

**Update AccountStorage constructor:**
```csharp
public class AccountStorage
{
    private readonly SecureString _password;
    // ... rest of implementation with secure string handling
}
```

### 3. Secret Key Memory Exposure - CRITICAL
**File:** `OtpAccount.cs`
**Severity:** Critical

**Issue:** TOTP secret keys stored as plain strings in memory, potentially exposing them through memory dumps.

**Current Code:**
```csharp
public class OtpAccount
{
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty; // Insecure
}
```

**Secure Fix:**
```csharp
public class OtpAccount
{
    public string Name { get; set; } = string.Empty;

    private byte[] _secretBytes = Array.Empty<byte>();

    public void SetSecret(string base32Secret)
    {
        ClearSecret();
        _secretBytes = Base32Decode(base32Secret);
    }

    public byte[] GetSecretBytes()
    {
        var copy = new byte[_secretBytes.Length];
        Array.Copy(_secretBytes, copy, _secretBytes.Length);
        return copy;
    }

    public void ClearSecret()
    {
        if (_secretBytes.Length > 0)
        {
            Array.Clear(_secretBytes, 0, _secretBytes.Length);
        }
    }

    // For JSON serialization only - consider encrypting even this
    public string Secret
    {
        get => Convert.ToBase64String(_secretBytes);
        set => SetSecret(value);
    }
}
```

### 4. JSON Deserialization Vulnerability - CRITICAL
**File:** `CryptoHelper.cs` & `AccountStorage.cs`
**Severity:** Critical

**Issue:** Using unsafe JSON deserialization that could lead to object injection attacks.

**Current Code:**
```csharp
var encryptedData = JsonSerializer.Deserialize<EncryptedData>(encryptedJson)!;
```

**Secure Fix:**
```csharp
public static string DecryptData(string encryptedJson, string password)
{
    if (string.IsNullOrWhiteSpace(encryptedJson))
        throw new ArgumentException("Encrypted data cannot be null or empty");

    EncryptedData? encryptedData;
    try
    {
        var options = new JsonSerializerOptions
        {
            MaxDepth = 5, // Prevent deep object graphs
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

    // Validate Base64 formats
    if (!IsValidBase64(encryptedData.Salt) ||
        !IsValidBase64(encryptedData.Iv) ||
        !IsValidBase64(encryptedData.Data))
        throw new ArgumentException("Invalid Base64 encoding in encrypted data");

    // ... rest of decryption logic
}

private static bool IsValidBase64(string base64)
{
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
```

## High Priority Issues

### 5. Insufficient Input Validation - HIGH
**File:** `TotpGenerator.cs`
**Severity:** High

**Issue:** Base32 decoding silently ignores invalid characters instead of failing securely.

**Current Code:**
```csharp
var value = alphabet.IndexOf(c);
if (value < 0) continue; // Silently ignores invalid chars
```

**Secure Fix:**
```csharp
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

    // Validate proper Base32 padding
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
```

### 6. Path Traversal Vulnerability - HIGH
**File:** `Program.cs`
**Severity:** High

**Issue:** Unsafe file path handling allows directory traversal attacks.

**Current Code:**
```csharp
var databaseFile = args.Length > 0
    ? args[0]  // Unsafe: Could be "../../../etc/passwd"
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "otp-accounts.json");
```

**Secure Fix:**
```csharp
static string GetSafeDatabasePath(string[] args)
{
    string databaseFile;

    if (args.Length > 0)
    {
        var userPath = args[0];

        // Validate and sanitize the path
        if (string.IsNullOrWhiteSpace(userPath))
            throw new ArgumentException("Database file path cannot be empty");

        // Get the full path and ensure it's within allowed directories
        var fullPath = Path.GetFullPath(userPath);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (!fullPath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(appData, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Database file must be within user profile or application data directories");
        }

        databaseFile = fullPath;
    }
    else
    {
        databaseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "otp-accounts.json");
    }

    return databaseFile;
}
```

## Medium Priority Issues

### 7. Weak PBKDF2 Iterations - MEDIUM
**File:** `CryptoHelper.cs`
**Severity:** Medium

**Issue:** 100,000 iterations may be insufficient for modern security standards.

**Recommendation:** Increase to at least 300,000 iterations:
```csharp
private const int Iterations = 300000; // Updated from 100000
```

### 8. Information Disclosure - MEDIUM
**File:** `Program.cs`
**Severity:** Medium

**Issue:** Exception messages may leak sensitive information.

**Current Code:**
```csharp
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}"); // Potential info disclosure
}
```

**Secure Fix:**
```csharp
catch (CryptographicException)
{
    Console.WriteLine("❌ Invalid password or corrupted database");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("❌ Access denied to database file");
}
catch (Exception)
{
    Console.WriteLine("❌ An unexpected error occurred");
}
```

## Low Priority Issues

### 9. Insufficient File Permissions - LOW
**File:** `AccountStorage.cs`
**Severity:** Low

**Issue:** No explicit file permission restrictions on database file.

**Recommendation:** Add file permission restrictions:
```csharp
public void SaveAccounts(List<OtpAccount> accounts)
{
    var jsonData = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
    var encryptedData = CryptoHelper.EncryptData(jsonData, _password);

    // Set restrictive file permissions (owner read/write only)
    File.WriteAllText(_databaseFile, encryptedData);

    if (Environment.OSVersion.Platform == PlatformID.Unix)
    {
        // Set Unix permissions: 600 (owner read/write only)
        File.SetUnixFileMode(_databaseFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
```

### 10. Hardcoded Constants - LOW
**File:** `CryptoHelper.cs`
**Severity:** Low

**Issue:** Cryptographic parameters are hardcoded and not configurable.

**Recommendation:** Consider making iterations configurable based on system performance while maintaining minimum security standards.

## Positive Security Observations

1. **Strong Cryptographic Foundation**: Uses AES-256-CBC with PBKDF2-SHA256, which are industry-standard algorithms
2. **Proper Random Generation**: Uses `RandomNumberGenerator.GetBytes()` for salt and IV generation
3. **Secure TOTP Implementation**: Follows RFC 6238 correctly with proper HMAC-SHA1 implementation
4. **Input Masking**: Password input is properly masked in the console
5. **Exception Handling**: Basic exception handling prevents application crashes
6. **Code Organization**: Well-structured with clear separation of concerns

## Action Plan

### Immediate (Critical - Fix Within 24 Hours)
1. Implement secure memory management for encryption keys
2. Replace string-based password handling with SecureString
3. Implement secure storage for TOTP secrets
4. Add safe JSON deserialization

### Short Term (High Priority - Fix Within 1 Week)
5. Add strict input validation for Base32 decoding
6. Implement safe file path handling

### Medium Term (Medium Priority - Fix Within 1 Month)
7. Increase PBKDF2 iterations
8. Improve exception handling to prevent information disclosure

### Long Term (Low Priority)
9. Add file permission restrictions
10. Make cryptographic parameters configurable

## Testing Recommendations

After implementing fixes:
1. Test memory usage patterns to ensure sensitive data is cleared
2. Verify input validation with malformed Base32 strings
3. Test path traversal attempts
4. Validate JSON deserialization with malicious payloads
5. Performance test with increased PBKDF2 iterations

## Conclusion

The application has a solid architectural foundation but requires immediate security hardening. Focus on memory management for sensitive data as the highest priority, followed by input validation and safe deserialization. Once these critical issues are addressed, the application will provide strong security for TOTP management.