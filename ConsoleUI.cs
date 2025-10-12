using System.Security;
using System.Text;

namespace OtpSharpCli;

public class ConsoleUI
{
    public SecureString GetPassword()
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

    public void DisplayAccountList(List<OtpAccount> accounts, int visibleCodeIndex = -1, DateTime codeVisibleSince = default)
    {
        var remaining = TotpGenerator.GetRemainingSeconds();
        Console.WriteLine($"Time to new codes: {remaining,2}s\n");
        Console.WriteLine("Accounts:");

        for (int i = 0; i < accounts.Count; i++)
        {
            var accountName = accounts[i].Name.PadRight(20);

            var timerText = new StringBuilder(" ");
            var code = "******";

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

    public void RefreshMainDisplay(AccountStorage storage, string databaseFile, int visibleCodeIndex = -1, DateTime codeVisibleSince = default)
    {
        Console.SetCursorPosition(0, 0);
        Console.WriteLine("OTP Sharp - One-Time Password Generator");
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

        ClearToEndOfConsole();
    }

    public string GetDatabaseInfo(string databaseFile)
    {
        var fileStatus = File.Exists(databaseFile) ? "" : " (new)";
        return $"Database: {databaseFile}{fileStatus}";
    }

    public void ClearToEndOfConsole()
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

    public void ShowTemporaryMessage(string message)
    {
        var originalLeft = Console.CursorLeft;
        var originalTop = Console.CursorTop;

        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        Console.Write(message);

        Console.SetCursorPosition(originalLeft, originalTop);
    }

    public (string? name, string? secret) GetAccountInput()
    {
        Console.WriteLine("\n--- Add New Account ---");
        Console.Write("Account name: ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowMessage("Invalid name.");
            return (null, null);
        }

        Console.Write("Secret key (Base32): ");
        var secret = Console.ReadLine()?.Trim().Replace(" ", "").Replace("-", "");

        if (string.IsNullOrEmpty(secret))
        {
            ShowMessage("Invalid secret.");
            return (null, null);
        }

        return (name, secret);
    }

    public bool ConfirmTestCode(string testCode)
    {
        Console.WriteLine($"Test code: {testCode}");
        Console.Write("Does this match your authenticator app? (y/n): ");

        var confirm = Console.ReadKey().KeyChar;
        Console.WriteLine();

        return confirm == 'y' || confirm == 'Y';
    }

    public string? SelectAccountToRemove(List<OtpAccount> accounts)
    {
        if (accounts.Count == 0)
        {
            Console.WriteLine("\n--- Remove Account ---");
            Console.WriteLine("No accounts to remove.");
            WaitForKey();
            return null;
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
            return accounts[choice - 1].Name;
        }

        if (choice == 0)
        {
            ShowMessage("Account removal cancelled.");
        }
        else
        {
            ShowMessage("ERROR: Invalid selection.");
        }

        return null;
    }

    public bool ConfirmRemoval(string accountName)
    {
        Console.Write($"Are you sure you want to remove '{accountName}'? (y/n): ");
        var confirm = Console.ReadKey().KeyChar;
        Console.WriteLine();

        return confirm == 'y' || confirm == 'Y';
    }

    public void ShowMessage(string message)
    {
        Console.WriteLine(message);
        WaitForKey();
    }

    public void WaitForKey()
    {
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}
