namespace OtpSharpCli;

public class AccountManager
{
    private readonly AccountStorage _storage;

    public AccountManager(AccountStorage storage)
    {
        _storage = storage;
    }

    public bool AddAccount(string name, string secret)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        try
        {
            _storage.AddAccount(name, secret);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool RemoveAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            return false;
        }

        return _storage.RemoveAccount(accountName);
    }

    public List<OtpAccount> GetAccounts()
    {
        return _storage.LoadAccounts();
    }

    public bool ValidateSecret(string secret)
    {
        try
        {
            TotpGenerator.GenerateCode(secret);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateTestCode(string secret)
    {
        return TotpGenerator.GenerateCode(secret);
    }
}
