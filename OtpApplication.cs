namespace OtpSharpCli;

public class OtpApplication
{
    private readonly AccountStorage _storage;
    private readonly string _databaseFile;
    private readonly ConsoleUI _ui;
    private readonly AccountManager _accountManager;
    private readonly ClipboardHelper _clipboardHelper;

    private int _visibleCodeIndex = -1;
    private DateTime _codeVisibleSince = DateTime.MinValue;

    public OtpApplication(AccountStorage storage, string databaseFile)
    {
        _storage = storage;
        _databaseFile = databaseFile;
        _ui = new ConsoleUI();
        _accountManager = new AccountManager(storage);
        _clipboardHelper = new ClipboardHelper();
    }

    public void Run()
    {
        var lastRefresh = DateTime.MinValue;

        Console.Clear();
        Console.CursorVisible = false;

        while (true)
        {
            var now = DateTime.Now;

            if (_visibleCodeIndex >= 0 && (now - _codeVisibleSince).TotalSeconds >= 10)
            {
                _visibleCodeIndex = -1;
            }

            if ((now - lastRefresh).TotalSeconds >= 1)
            {
                _ui.RefreshMainDisplay(_storage, _databaseFile, _visibleCodeIndex, _codeVisibleSince);
                lastRefresh = now;
            }

            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                var keyChar = char.ToLower(keyInfo.KeyChar);

                if (HandleKeyInput(keyChar))
                {
                    Console.CursorVisible = true;
                    return;
                }

                _ui.RefreshMainDisplay(_storage, _databaseFile, _visibleCodeIndex, _codeVisibleSince);
                lastRefresh = DateTime.Now;
            }

            Thread.Sleep(50);
        }
    }

    private bool HandleKeyInput(char keyChar)
    {
        var accounts = _accountManager.GetAccounts();

        switch (keyChar)
        {
            case 'a':
                HandleAddAccount();
                _visibleCodeIndex = -1;
                Console.Clear();
                return false;
            case 'd':
                HandleRemoveAccount();
                _visibleCodeIndex = -1;
                Console.Clear();
                return false;
            case 'c':
                HandleCopyCode(accounts);
                return false;
            case 'q':
                return true;
            default:
                HandleNumberKey(keyChar, accounts);
                return false;
        }
    }

    private void HandleAddAccount()
    {
        var (name, secret) = _ui.GetAccountInput();

        if (name == null || secret == null)
        {
            return;
        }

        if (!_accountManager.ValidateSecret(secret))
        {
            _ui.ShowMessage("ERROR: Invalid secret key format.");
            return;
        }

        var testCode = _accountManager.GenerateTestCode(secret);
        if (!_ui.ConfirmTestCode(testCode))
        {
            _ui.ShowMessage("Account not added.");
            return;
        }

        if (_accountManager.AddAccount(name, secret))
        {
            _ui.ShowMessage("Account added successfully!");
        }
        else
        {
            _ui.ShowMessage("ERROR: Failed to add account.");
        }
    }

    private void HandleRemoveAccount()
    {
        var accounts = _accountManager.GetAccounts();
        var accountName = _ui.SelectAccountToRemove(accounts);

        if (accountName == null)
        {
            return;
        }

        if (!_ui.ConfirmRemoval(accountName))
        {
            _ui.ShowMessage("Account removal cancelled.");
            return;
        }

        if (_accountManager.RemoveAccount(accountName))
        {
            _ui.ShowMessage("Account removed successfully!");
        }
        else
        {
            _ui.ShowMessage("ERROR: Failed to remove account.");
        }
    }

    private void HandleCopyCode(List<OtpAccount> accounts)
    {
        if (_clipboardHelper.CopyCodeToClipboard(accounts, _visibleCodeIndex))
        {
            _ui.ShowTemporaryMessage("Code copied to clipboard!");
        }
        else
        {
            _ui.ShowTemporaryMessage("ERROR: No code visible to copy");
        }
    }

    private void HandleNumberKey(char keyChar, List<OtpAccount> accounts)
    {
        if (char.IsDigit(keyChar))
        {
            int accountIndex = keyChar - '0';
            if (accountIndex >= 1 && accountIndex <= accounts.Count)
            {
                int targetIndex = accountIndex - 1;

                if (_visibleCodeIndex == targetIndex)
                {
                    _visibleCodeIndex = -1;
                }
                else
                {
                    _visibleCodeIndex = targetIndex;
                    _codeVisibleSince = DateTime.Now;
                }
            }
        }
    }
}
