using OtpSharpCli;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("OTP Sharp - One-Time Password Generator");
        Console.WriteLine("==========================================");

        try
        {
            var databaseFile = args.Length > 0
                ? args[0]
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "otp-accounts.json");

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
}
