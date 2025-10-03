namespace OtpSharpCli;

public class ClipboardHelper
{
    public bool CopyCodeToClipboard(List<OtpAccount> accounts, int visibleCodeIndex)
    {
        if (visibleCodeIndex < 0 || visibleCodeIndex >= accounts.Count)
        {
            return false;
        }

        try
        {
            var code = TotpGenerator.GenerateCode(accounts[visibleCodeIndex].Secret);

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
                clipboardCommand = "xclip";
                if (!IsCommandAvailable("xclip"))
                {
                    if (IsCommandAvailable("xsel"))
                    {
                        clipboardCommand = "xsel";
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
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

    private bool IsCommandAvailable(string command)
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

    private string GetClipboardArguments(string command)
    {
        return command switch
        {
            "xclip" => "-selection clipboard",
            "xsel" => "--clipboard --input",
            _ => ""
        };
    }
}
