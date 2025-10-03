using OtpSharp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🔐 OTP Sharp - One-Time Password Generator");
        Console.WriteLine("==========================================");

        try
        {
            var databaseFile = args.Length > 0
                ? args[0]
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "otp-accounts.json");

            var consoleUI = new ConsoleUI();
            Console.WriteLine(consoleUI.GetDatabaseInfo(databaseFile));

            var password = consoleUI.GetPassword();
            var storage = new AccountStorage(password, databaseFile);

            var accountManager = new AccountManager(storage);
            var clipboardHelper = new ClipboardHelper();

            RunMainLoop(storage, databaseFile, consoleUI, accountManager, clipboardHelper);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("❌ Invalid password or corrupted database");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static void RunMainLoop(AccountStorage storage, string databaseFile, ConsoleUI consoleUI, AccountManager accountManager, ClipboardHelper clipboardHelper)
    {
        var lastRefresh = DateTime.MinValue;
        var visibleCodeIndex = -1;
        var codeVisibleSince = DateTime.MinValue;

        Console.Clear();
        Console.CursorVisible = false;

        while (true)
        {
            var now = DateTime.Now;

            if (visibleCodeIndex >= 0 && (now - codeVisibleSince).TotalSeconds >= 10)
            {
                visibleCodeIndex = -1;
            }

            if ((now - lastRefresh).TotalSeconds >= 1)
            {
                consoleUI.RefreshMainDisplay(storage, databaseFile, visibleCodeIndex, codeVisibleSince);
                lastRefresh = now;
            }

            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                var keyChar = char.ToLower(keyInfo.KeyChar);

                var result = HandleDirectKeyInput(keyInfo, keyChar, storage, databaseFile, consoleUI, accountManager, clipboardHelper, ref visibleCodeIndex, ref codeVisibleSince);
                if (result)
                {
                    Console.CursorVisible = true;
                    return;
                }

                consoleUI.RefreshMainDisplay(storage, databaseFile, visibleCodeIndex, codeVisibleSince);
                lastRefresh = DateTime.Now;
            }

            Thread.Sleep(50);
        }
    }

    static bool HandleDirectKeyInput(ConsoleKeyInfo keyInfo, char keyChar, AccountStorage storage, string databaseFile, ConsoleUI consoleUI, AccountManager accountManager, ClipboardHelper clipboardHelper, ref int visibleCodeIndex, ref DateTime codeVisibleSince)
    {
        var accounts = storage.LoadAccounts();

        switch (keyChar)
        {
            case 'a':
                accountManager.AddAccount();
                visibleCodeIndex = -1;
                Console.Clear();
                return false;
            case 'd':
                accountManager.RemoveAccount();
                visibleCodeIndex = -1;
                Console.Clear();
                return false;
            case 'c':
                if (clipboardHelper.CopyCodeToClipboard(accounts, visibleCodeIndex))
                {
                    consoleUI.ShowTemporaryMessage("📋 Code copied to clipboard!");
                }
                else
                {
                    consoleUI.ShowTemporaryMessage("❌ No code visible to copy");
                }
                return false;
            case 'q':
                return true;
            default:
                if (char.IsDigit(keyChar))
                {
                    int accountIndex = keyChar - '0';
                    if (accountIndex >= 1 && accountIndex <= accounts.Count)
                    {
                        int targetIndex = accountIndex - 1;

                        if (visibleCodeIndex == targetIndex)
                        {
                            visibleCodeIndex = -1;
                        }
                        else
                        {
                            visibleCodeIndex = targetIndex;
                            codeVisibleSince = DateTime.Now;
                        }
                    }
                }
                return false;
        }
    }
}
