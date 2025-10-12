using OtpSharpCli;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("OTP Sharp - One-Time Password Generator");
        Console.WriteLine("==========================================");

        try
        {
            var databaseFile = GetSafeDatabasePath(args);

            var ui = new ConsoleUI();
            Console.WriteLine(ui.GetDatabaseInfo(databaseFile));

            var password = ui.GetPassword();
            var storage = new AccountStorage(password, databaseFile);

            var app = new OtpApplication(storage, databaseFile);
            app.Run();
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("ERROR: Invalid password or corrupted database");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }

    static string GetSafeDatabasePath(string[] args)
    {
        string databaseFile;

        if (args.Length > 0)
        {
            var userPath = args[0];

            if (string.IsNullOrWhiteSpace(userPath))
                throw new ArgumentException("Database file path cannot be empty");

            var fullPath = Path.GetFullPath(userPath);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (!fullPath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase) &&
                !fullPath.StartsWith(appData, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Database file must be within user profile or application data directories");
            }

            databaseFile = fullPath;
        }
        else
        {
            databaseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "otp-accounts.json");
        }

        return databaseFile;
    }
}
