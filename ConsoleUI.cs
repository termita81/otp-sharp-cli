using System.Text;
using OtpSharp;

namespace OtpSharp;

public class ConsoleUI
{
    public string GetPassword()
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

    public void DisplayAccountList(List<OtpAccount> accounts, int visibleCodeIndex = -1, DateTime codeVisibleSince = default)
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

    public void RefreshMainDisplay(AccountStorage storage, string databaseFile, int visibleCodeIndex = -1, DateTime codeVisibleSince = default)
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
}
