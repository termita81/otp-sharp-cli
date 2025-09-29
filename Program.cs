using System.Text;
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

            Console.WriteLine(GetDatabaseInfo(databaseFile));

            var password = GetPassword();
            var storage = new AccountStorage(password, databaseFile);

            RunMainLoop(storage, databaseFile);
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


    private static string GetDatabaseInfo(string databaseFile)
    {
        var fileStatus = File.Exists(databaseFile) ? "" : " (new)";
        return $"Database: {databaseFile}{fileStatus}";
    }

    static void RunMainLoop(AccountStorage storage, string databaseFile)
    {
        var inputBuffer = new StringBuilder();
        var lastRefresh = DateTime.MinValue;

        while (true)
        {
            var now = DateTime.Now;

            // Refresh display every second
            if ((now - lastRefresh).TotalSeconds >= 1)
            {
                RefreshMainDisplay(storage, databaseFile, inputBuffer.ToString());
                lastRefresh = now;
            }

            // Check for input
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    var input = inputBuffer.ToString().Trim().ToLower();
                    inputBuffer.Clear();

                    if (HandleUserInput(input, storage, databaseFile))
                    {
                        return; // Exit application
                    }

                    // Force immediate refresh after command
                    RefreshMainDisplay(storage, databaseFile, "");
                    lastRefresh = DateTime.Now;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (inputBuffer.Length > 0)
                    {
                        inputBuffer.Length--;
                        RefreshMainDisplay(storage, databaseFile, inputBuffer.ToString());
                        lastRefresh = DateTime.Now;
                    }
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    inputBuffer.Append(keyInfo.KeyChar);
                    RefreshMainDisplay(storage, databaseFile, inputBuffer.ToString());
                    lastRefresh = DateTime.Now;
                }
            }

            Thread.Sleep(50); // Small delay to prevent excessive CPU usage
        }
    }

    static void RefreshMainDisplay(AccountStorage storage, string databaseFile, string currentInput)
    {
        Console.Clear();
        Console.WriteLine("🔐 OTP Sharp - One-Time Password Generator");
        Console.WriteLine("==========================================");

        Console.WriteLine(GetDatabaseInfo(databaseFile));

        var accounts = storage.LoadAccounts();

        if (accounts.Count == 0)
        {
            Console.WriteLine("No accounts found. Add your first account!");
        }
        else
        {
            DisplayAccountList(accounts);
        }

        Console.WriteLine("\nCommands:");
        Console.WriteLine("  [a]dd account  [d]elete account  [v]iew codes  [r]efresh  [q]uit");
        if (accounts.Count > 0)
        {
            Console.WriteLine($"  [1-{accounts.Count}] view specific account code");
        }
        Console.Write($"\nChoice: {currentInput}");
    }

    static bool HandleUserInput(string input, AccountStorage storage, string databaseFile)
    {
        var accounts = storage.LoadAccounts();

        switch (input)
        {
            case "a":
                AddAccount(storage);
                return false;
            case "d":
                RemoveAccount(storage);
                return false;
            case "v":
                ViewCodes(accounts);
                return false;
            case "r":
                return false; // Just refresh
            case "q":
                return true; // Exit
            default:
                // Try to parse as account index
                if (int.TryParse(input, out int accountIndex) &&
                    accountIndex >= 1 && accountIndex <= accounts.Count)
                {
                    ShowSingleCode(accounts[accountIndex - 1]);
                }
                return false;
        }
    }

    static string GetPassword()
    {
        Console.Write("Enter master password: ");

        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";

        var password = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }

        Console.WriteLine();
        return password.ToString();
    }

    static void DisplayAccountList(List<OtpAccount> accounts)
    {
        var remaining = TotpGenerator.GetRemainingSeconds();
        Console.WriteLine($"Time remaining: {remaining}s\n");
        Console.WriteLine("Accounts (codes hidden for security):");

        for (int i = 0; i < accounts.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {accounts[i].Name.PadRight(20)} ●●●●●●");
        }
    }

    static void ViewCodes(List<OtpAccount> accounts)
    {
        if (accounts.Count == 0)
        {
            Console.WriteLine("\nNo accounts to view.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        while (true)
        {
            Console.Clear();
            Console.WriteLine("🔐 OTP Sharp - View Codes");
            Console.WriteLine("========================");

            var remaining = TotpGenerator.GetRemainingSeconds();
            Console.WriteLine($"Time remaining: {remaining}s\n");

            Console.WriteLine("Select account to view code (codes auto-hide after 10 seconds):");
            for (int i = 0; i < accounts.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {accounts[i].Name}");
            }

            Console.WriteLine("\n0. Back to main menu");
            Console.Write($"\nEnter number (1-{accounts.Count}) or 0 to go back: ");

            var input = Console.ReadLine();

            if (int.TryParse(input, out int choice))
            {
                if (choice == 0)
                {
                    return;
                }
                else if (choice >= 1 && choice <= accounts.Count)
                {
                    ShowSingleCode(accounts[choice - 1]);
                }
                else
                {
                    Console.WriteLine("❌ Invalid selection. Press any key to try again...");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("❌ Invalid input. Press any key to try again...");
                Console.ReadKey();
            }
        }
    }

    static void ShowSingleCode(OtpAccount account)
    {
        var startTime = DateTime.Now;
        var lastRefresh = DateTime.MinValue;
        var lastCode = "";
        var lastRemaining = 0;

        while (true)
        {
            var now = DateTime.Now;
            var elapsed = (now - startTime).TotalSeconds;

            // Auto-hide after 10 seconds
            if (elapsed >= 10)
            {
                break;
            }

            // Refresh display every second or when code changes
            if ((now - lastRefresh).TotalSeconds >= 1)
            {
                try
                {
                    var code = TotpGenerator.GenerateCode(account.Secret);
                    var remaining = TotpGenerator.GetRemainingSeconds();

                    // Only clear and redraw if something changed
                    if (code != lastCode || remaining != lastRemaining)
                    {
                        Console.Clear();
                        Console.WriteLine("🔐 OTP Sharp - Code Display");
                        Console.WriteLine("===========================\n");

                        Console.WriteLine($"Account: {account.Name}");
                        Console.WriteLine($"Code:    {code}");
                        Console.WriteLine($"Time remaining: {remaining}s\n");

                        var hideIn = Math.Max(0, 10 - (int)elapsed);
                        Console.WriteLine($"⚠️  Code will auto-hide in {hideIn} seconds...");
                        Console.WriteLine("Press any key to hide immediately and return.");

                        lastCode = code;
                        lastRemaining = remaining;
                    }
                }
                catch
                {
                    Console.Clear();
                    Console.WriteLine("🔐 OTP Sharp - Code Display");
                    Console.WriteLine("===========================\n");
                    Console.WriteLine($"Account: {account.Name}");
                    Console.WriteLine($"Code:    ERROR");
                    Console.WriteLine("\n❌ Failed to generate code. Invalid secret key.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                lastRefresh = now;
            }

            // Check for key press to exit immediately
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
                break;
            }

            Thread.Sleep(100);
        }
    }

    static void AddAccount(AccountStorage storage)
    {
        Console.WriteLine("\n--- Add New Account ---");
        Console.Write("Account name: ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            Console.WriteLine("Invalid name. Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.Write("Secret key (Base32): ");
        var secret = Console.ReadLine()?.Trim().Replace(" ", "").Replace("-", "");

        if (string.IsNullOrEmpty(secret))
        {
            Console.WriteLine("Invalid secret. Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            var testCode = TotpGenerator.GenerateCode(secret);
            Console.WriteLine($"Test code: {testCode}");
            Console.Write("Does this match your authenticator app? (y/n): ");

            var confirm = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (confirm == 'y' || confirm == 'Y')
            {
                storage.AddAccount(name, secret);
                Console.WriteLine("✅ Account added successfully!");
            }
            else
            {
                Console.WriteLine("❌ Account not added.");
            }
        }
        catch
        {
            Console.WriteLine("❌ Invalid secret key format.");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    static void RemoveAccount(AccountStorage storage)
    {
        var accounts = storage.LoadAccounts();

        if (accounts.Count == 0)
        {
            Console.WriteLine("\n--- Remove Account ---");
            Console.WriteLine("No accounts to remove.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("\n--- Remove Account ---");
        Console.WriteLine("Select account to remove:");

        for (int i = 0; i < accounts.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {accounts[i].Name}");
        }

        Console.Write($"\nEnter number (1-{accounts.Count}) or 0 to cancel: ");
        var input = Console.ReadLine();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= accounts.Count)
        {
            var accountName = accounts[choice - 1].Name;
            Console.Write($"Are you sure you want to remove '{accountName}'? (y/n): ");

            var confirm = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (confirm == 'y' || confirm == 'Y')
            {
                if (storage.RemoveAccount(accountName))
                {
                    Console.WriteLine("✅ Account removed successfully!");
                }
                else
                {
                    Console.WriteLine("❌ Failed to remove account.");
                }
            }
            else
            {
                Console.WriteLine("❌ Account removal cancelled.");
            }
        }
        else if (choice == 0)
        {
            Console.WriteLine("❌ Account removal cancelled.");
        }
        else
        {
            Console.WriteLine("❌ Invalid selection.");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}
