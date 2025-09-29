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
            var fileStatus = File.Exists(databaseFile) ? "existing" : "new";

            Console.WriteLine($"Using database: {databaseFile} ({fileStatus})");

            var password = GetPassword();
            var storage = new AccountStorage(password, databaseFile);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("🔐 OTP Sharp - One-Time Password Generator");
                Console.WriteLine("==========================================");
                fileStatus = File.Exists(databaseFile) ? "existing" : "new";
                Console.WriteLine($"Database: {databaseFile} ({fileStatus})");

                var accounts = storage.LoadAccounts();

                if (accounts.Count == 0)
                {
                    Console.WriteLine("No accounts found. Add your first account!");
                }
                else
                {
                    DisplayCodes(accounts);
                }

                Console.WriteLine("\nCommands:");
                Console.WriteLine("  [a]dd account  [d]elete account  [r]efresh  [q]uit");
                Console.Write("\nChoice: ");

                var input = Console.ReadKey(true).KeyChar;

                switch (input)
                {
                    case 'a':
                    case 'A':
                        AddAccount(storage);
                        break;
                    case 'd':
                    case 'D':
                        RemoveAccount(storage);
                        break;
                    case 'r':
                    case 'R':
                        break;
                    case 'q':
                    case 'Q':
                        return;
                }

                if (input != 'a' && input != 'A' && input != 'd' && input != 'D')
                {
                    Thread.Sleep(1000);
                }
            }
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

    static string GetPassword()
    {
        Console.Write("Enter master password: ");

        if (!Console.IsInputRedirected)
        {
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
        else
        {
            return Console.ReadLine() ?? "";
        }
    }

    static void DisplayCodes(List<OtpAccount> accounts)
    {
        var remaining = TotpGenerator.GetRemainingSeconds();
        Console.WriteLine($"Time remaining: {remaining}s\n");

        foreach (var account in accounts)
        {
            try
            {
                var code = TotpGenerator.GenerateCode(account.Secret);
                Console.WriteLine($"{account.Name.PadRight(20)} {code}");
            }
            catch
            {
                Console.WriteLine($"{account.Name.PadRight(20)} ERROR");
            }
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
