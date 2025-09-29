using System.Text.Json;

namespace OtpSharp;

public class AccountStorage
{
    private readonly string _databaseFile;
    private readonly string _password;

    public AccountStorage(string password, string databaseFile)
    {
        _password = password;
        _databaseFile = databaseFile;
    }

    public List<OtpAccount> LoadAccounts()
    {
        if (!File.Exists(_databaseFile))
            return [];

        try
        {
            var encryptedData = File.ReadAllText(_databaseFile);
            var decryptedData = CryptoHelper.DecryptData(encryptedData, _password);
            return JsonSerializer.Deserialize<List<OtpAccount>>(decryptedData) ?? [];
        }
        catch
        {
            throw new UnauthorizedAccessException("Invalid password or corrupted database");
        }
    }

    public void SaveAccounts(List<OtpAccount> accounts)
    {
        var jsonData = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
        var encryptedData = CryptoHelper.EncryptData(jsonData, _password);
        File.WriteAllText(_databaseFile, encryptedData);
    }

    public void AddAccount(string name, string secret)
    {
        var accounts = LoadAccounts();
        accounts.Add(new OtpAccount { Name = name, Secret = secret });
        SaveAccounts(accounts);
    }

    public bool RemoveAccount(string name)
    {
        var accounts = LoadAccounts();
        var accountToRemove = accounts.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (accountToRemove != null)
        {
            accounts.Remove(accountToRemove);
            SaveAccounts(accounts);
            return true;
        }

        return false;
    }
}