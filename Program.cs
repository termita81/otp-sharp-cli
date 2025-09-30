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
        var lastRefresh = DateTime.MinValue;
        var visibleCodeIndex = -1; // -1 means no code is visible
        var codeVisibleSince = DateTime.MinValue;

        // Initial clear and setup
        Console.Clear();
        Console.CursorVisible = false;

        while (true)
        {
            var now = DateTime.Now;

            // Auto-hide code after 10 seconds
            if (visibleCodeIndex >= 0 && (now - codeVisibleSince).TotalSeconds >= 10)
            {
                visibleCodeIndex = -1;
            }

            // Refresh display every second
            if ((now - lastRefresh).TotalSeconds >= 1)
            {
                RefreshMainDisplay(storage, databaseFile, visibleCodeIndex, codeVisibleSince);
                lastRefresh = now;
            }

            // Check for input
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                var keyChar = char.ToLower(keyInfo.KeyChar);

                var result = HandleDirectKeyInput(keyInfo, keyChar, storage, databaseFile, ref visibleCodeIndex, ref codeVisibleSince);
                if (result)
                {
                    Console.CursorVisible = true;
                    return; // Exit application
                }

                // Force immediate refresh after any key press
                RefreshMainDisplay(storage, databaseFile, visibleCodeIndex, codeVisibleSince);
                lastRefresh = DateTime.Now;
            }

            Thread.Sleep(50); // Small delay to prevent excessive CPU usage
        }
    }

    static void RefreshMainDisplay(AccountStorage storage, string databaseFile, int visibleCodeIndex = -1, DateTime codeVisibleSince = default)
    {
        Console.SetCursorPosition(0, 0);
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
            DisplayAccountList(accounts, visibleCodeIndex, codeVisibleSince);
        }

        Console.WriteLine("\nCommands:");
        Console.WriteLine("  [a]dd account  [d]elete account  [c]opy code  [q]uit");
        if (accounts.Count > 0)
        {
            Console.WriteLine($"  [1-{accounts.Count}] show/hide specific account code");
        }
        Console.WriteLine("\nPress any key to execute command...");

        // Clear any remaining content from previous display
        ClearToEndOfConsole();
    }

    static bool HandleDirectKeyInput(ConsoleKeyInfo keyInfo, char keyChar, AccountStorage storage, string databaseFile, ref int visibleCodeIndex, ref DateTime codeVisibleSince)
    {
        var accounts = storage.LoadAccounts();

        switch (keyChar)
        {
            case 'a':
                AddAccount(storage);
                visibleCodeIndex = -1; // Hide any visible code after adding account
                return false;
            case 'd':
                RemoveAccount(storage);
                visibleCodeIndex = -1; // Hide any visible code after removing account
                return false;
            case 'c':
                if (CopyCodeToClipboard(accounts, visibleCodeIndex))
                {
                    ShowTemporaryMessage("📋 Code copied to clipboard!");
                }
                else
                {
                    ShowTemporaryMessage("❌ No code visible to copy");
                }
                return false;
            case 'q':
                return true; // Exit
            default:
                // Try to parse as account index
                if (char.IsDigit(keyChar))
                {
                    int accountIndex = keyChar - '0'; // Convert char to int
                    if (accountIndex >= 1 && accountIndex <= accounts.Count)
                    {
                        int targetIndex = accountIndex - 1;

                        // Toggle: if this code is already visible, hide it; otherwise show it
                        if (visibleCodeIndex == targetIndex)
                        {
                            visibleCodeIndex = -1; // Hide the code
                        }
                        else
                        {
                            visibleCodeIndex = targetIndex; // Show this code
                            codeVisibleSince = DateTime.Now;
                        }
                    }
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

    static void DisplayAccountList(List<OtpAccount> accounts, int visibleCodeIndex = -1, DateTime codeVisibleSince = default)
    {
        var remaining = TotpGenerator.GetRemainingSeconds();
        Console.WriteLine($"Time to new codes: {remaining,2}s\n");
        Console.WriteLine("Accounts:");

        for (int i = 0; i < accounts.Count; i++)
        {
            var accountName = accounts[i].Name.PadRight(20);

            var timerText = new StringBuilder(" ");
            var code = "●●●●●●";

            if (visibleCodeIndex == i)
            {
                code = TotpGenerator.GenerateCode(accounts[i].Secret);

                var secondsVisible = (int)(DateTime.Now - codeVisibleSince).TotalSeconds;

                timerText.Append($"{new string('#', 10 - secondsVisible).PadRight(10, ' ')}");
            }
            else
            {
                timerText.Append(new string(' ', 10));
            }
            Console.WriteLine($"{i + 1}. {accountName} {code} {timerText}");

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

    static void ClearToEndOfConsole()
    {
        try
        {
            var currentTop = Console.CursorTop;
            var windowHeight = Console.WindowHeight;

            for (int i = currentTop; i < windowHeight; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth - 1));
            }

            Console.SetCursorPosition(0, currentTop);
        }
        catch
        {
            // Fallback if console operations fail
        }
    }

    static bool CopyCodeToClipboard(List<OtpAccount> accounts, int visibleCodeIndex)
    {
        if (visibleCodeIndex < 0 || visibleCodeIndex >= accounts.Count)
        {
            return false; // No code is visible or invalid index
        }

        try
        {
            var code = TotpGenerator.GenerateCode(accounts[visibleCodeIndex].Secret);

            // Detect operating system and use appropriate clipboard command
            string clipboardCommand;
            if (OperatingSystem.IsWindows())
            {
                clipboardCommand = "clip";
            }
            else if (OperatingSystem.IsMacOS())
            {
                clipboardCommand = "pbcopy";
            }
            else if (OperatingSystem.IsLinux())
            {
                // Try xclip first, fall back to xsel if not available
                clipboardCommand = "xclip";
                if (!IsCommandAvailable("xclip"))
                {
                    if (IsCommandAvailable("xsel"))
                    {
                        clipboardCommand = "xsel";
                    }
                    else
                    {
                        return false; // No clipboard utility available
                    }
                }
            }
            else
            {
                return false; // Unsupported operating system
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = clipboardCommand,
                    Arguments = GetClipboardArguments(clipboardCommand),
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.StandardInput.Write(code);
            process.StandardInput.Close();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    static bool IsCommandAvailable(string command)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    static string GetClipboardArguments(string command)
    {
        return command switch
        {
            "xclip" => "-selection clipboard",
            "xsel" => "--clipboard --input",
            _ => ""
        };
    }

    static void ShowTemporaryMessage(string message)
    {
        // Save current cursor position
        var originalLeft = Console.CursorLeft;
        var originalTop = Console.CursorTop;

        // Show message at bottom of screen
        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        Console.Write(new string(' ', Console.WindowWidth - 1)); // Clear line
        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        Console.Write(message);

        // Restore cursor position
        Console.SetCursorPosition(originalLeft, originalTop);

        // Message will be cleared on next refresh
    }
}
