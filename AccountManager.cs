using OtpSharp;

namespace OtpSharp;

public class AccountManager
{
    private readonly AccountStorage _storage;

    public AccountManager(AccountStorage storage)
    {
        _storage = storage;
    }

    public void AddAccount()
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
                _storage.AddAccount(name, secret);
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

    public void RemoveAccount()
    {
        var accounts = _storage.LoadAccounts();

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
                if (_storage.RemoveAccount(accountName))
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
