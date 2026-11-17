using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using IOPath = System.IO.Path;

[assembly: AssemblyTitle("Windows Driver Foundation")]
[assembly: AssemblyDescription("Windows Driver Foundation - User-mode Driver Framework Host Process")]
[assembly: AssemblyProduct("Microsoft Windows Operating System")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("Copyright (C) Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyVersion("10.0.26100.9949")]
[assembly: AssemblyFileVersion("10.0.26100.9949")]

namespace WinLockKFC
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Diag.Enabled = Array.IndexOf(args, "--diag") >= 0
                || Environment.GetEnvironmentVariable("WINLOCK_DIAG") == "1";
            Diag.Log("main args=" + string.Join(" ", args));
            if (Array.IndexOf(args, "--selftest") >= 0)
            {
                SelfTest.Run();
                return;
            }

            if (Array.IndexOf(args, "--policy-selftest") >= 0)
            {
                bool policyTestCreated;
                using (Mutex policyTestMutex = new Mutex(true, "WinLockKFC_Prank_3015", out policyTestCreated))
                {
                    if (policyTestCreated)
                    {
                        PolicyGuard.SelfTest();
                    }
                }
                return;
            }

            if (Array.IndexOf(args, "--restore") >= 0)
            {
                bool restoreCreated;
                using (Mutex restoreMutex = new Mutex(true, "WinLockKFC_Prank_3015", out restoreCreated))
                {
                    if (restoreCreated)
                    {
                        PolicyGuard.RestoreStaleBackup();
                    }
                }
                return;
            }

            bool preview = Array.IndexOf(args, "--preview") >= 0;
            bool smoke = Array.IndexOf(args, "--smoke") >= 0;
            bool loud = Array.IndexOf(args, "--loud") >= 0;
            bool created;
            using (Mutex mutex = new Mutex(true, "WinLockKFC_Prank_3015", out created))
            {
                if (!created)
                {
                    return;
                }

                PolicyGuard.RestoreStaleBackup();

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                LockWindow window = new LockWindow(preview, smoke, loud);
                app.SessionEnding += delegate(object sender, SessionEndingCancelEventArgs e)
                {
                    if (!preview && window.IsLocked && !window.IsClosing)
                    {
                        PolicyGuard.Restore();
                        e.Cancel = true;

                        DispatcherTimer reapply = new DispatcherTimer();
                        reapply.Interval = TimeSpan.FromSeconds(1);
                        reapply.Tick += delegate
                        {
                            reapply.Stop();
                            if (window.IsLocked && !window.IsClosing)
                            {
                                PolicyGuard.Apply();
                            }
                        };
                        reapply.Start();
                        return;
                    }
                    PolicyGuard.Restore();
                };
                app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    Diag.Log("dispatcher exception: " + e.Exception);
                    PolicyGuard.Restore();
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    Diag.Log("domain exception: " + e.ExceptionObject);
                    PolicyGuard.Restore();
                };
                Diag.Log("starting app.Run");
                AppDomain.CurrentDomain.ProcessExit += delegate { Diag.Log("process exit"); };
                app.Run(window);
                Diag.Log("app.Run returned");
                GC.KeepAlive(mutex);
            }
        }
    }

    internal static class Diag
    {
        public static bool Enabled;

        public static void Log(string message)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                string path = IOPath.Combine(IOPath.GetTempPath(), "winlock_debug.log");
                using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " pid=" + Process.GetCurrentProcess().Id + " " + message);
                }
            }
            catch
            {
            }
        }
    }

    internal static class AppPaths
    {
        public static string WorkDir
        {
            get
            {
                string dir = Environment.GetEnvironmentVariable("WINLOCK_WORKDIR");
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    return dir;
                }
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
    }

    internal static class SelfTest
    {
        public static void Run()
        {
            List<string> lines = new List<string>();
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string[] names = asm.GetManifestResourceNames();
                foreach (string name in names)
                {
                    lines.Add("resource: " + name);
                }

                using (Stream s = asm.GetManifestResourceStream("kfc.png"))
                {
                    lines.Add("kfc bytes: " + (s == null ? -1 : s.Length));
                    if (s != null)
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = s;
                        bmp.EndInit();
                        bmp.Freeze();
                        lines.Add("kfc image: " + bmp.PixelWidth + "x" + bmp.PixelHeight);
                    }
                }

                foreach (string musicResource in new string[] { "music.mp3", "music_loud.mp3" })
                {
                    using (Stream s = asm.GetManifestResourceStream(musicResource))
                    {
                        lines.Add(musicResource + " bytes: " + (s == null ? -1 : s.Length));
                        if (s != null)
                        {
                            byte[] header = new byte[4];
                            s.Read(header, 0, 4);
                            lines.Add(musicResource + " header: " + BitConverter.ToString(header));
                        }
                    }
                }

                foreach (string spriteResource in new string[] { "cat.png", "chicken.png" })
                {
                    using (Stream s = asm.GetManifestResourceStream(spriteResource))
                    {
                        lines.Add(spriteResource + " bytes: " + (s == null ? -1 : s.Length));
                        if (s != null)
                        {
                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = s;
                            bmp.EndInit();
                            bmp.Freeze();
                            lines.Add(spriteResource + " image: " + bmp.PixelWidth + "x" + bmp.PixelHeight);
                        }
                    }
                }

                int sfxCount = 0;
                long sfxBytes = 0;
                foreach (string resourceName in names)
                {
                    if (resourceName.StartsWith("sfx", StringComparison.OrdinalIgnoreCase)
                        && resourceName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    {
                        sfxCount++;
                        using (Stream s = asm.GetManifestResourceStream(resourceName))
                        {
                            sfxBytes += s == null ? 0 : s.Length;
                        }
                    }
                }
                lines.Add("sfx count: " + sfxCount + " bytes: " + sfxBytes);
            }
            catch (Exception ex)
            {
                lines.Add("ERROR: " + ex);
            }

            try
            {
                File.WriteAllLines(IOPath.Combine(IOPath.GetTempPath(), "winlock_selftest.txt"), lines.ToArray());
            }
            catch
            {
            }
        }
    }

    internal static class PolicyGuard
    {
        private const string BackupFileName = "winlock_kfc_policy_backup.dat";
        private static readonly object Sync = new object();
        private static PolicySpec[] _specs = new PolicySpec[]
        {
            new PolicySpec(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr"),
            new PolicySpec(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableLockWorkstation"),
            new PolicySpec(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableChangePassword"),
            new PolicySpec(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "HideFastUserSwitching"),
            new PolicySpec(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoLogoff"),
            new PolicySpec(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoClose")
        };

        private static List<PolicyState> _states;
        private static bool _applied;
        private static string _lastError;

        public static string BackupPath
        {
            get { return IOPath.Combine(IOPath.GetTempPath(), BackupFileName); }
        }

        public static string LastError
        {
            get
            {
                lock (Sync)
                {
                    return _lastError;
                }
            }
        }

        public static bool IsApplied
        {
            get
            {
                lock (Sync)
                {
                    return _applied;
                }
            }
        }

        public static bool Apply()
        {
            lock (Sync)
            {
                if (_applied)
                {
                    return true;
                }

                _lastError = null;

                if (!RestoreStaleBackupLocked())
                {
                    return false;
                }

                List<PolicyState> states = new List<PolicyState>();
                try
                {
                    foreach (PolicySpec spec in _specs)
                    {
                        states.Add(PolicyState.Capture(spec));
                    }

                    WriteBackup(states);

                    foreach (PolicyState state in states)
                    {
                        state.Apply();
                    }

                    _states = states;
                    _applied = true;
                    Log("applied");
                    return true;
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                    Log("apply failed: " + ex.Message);

                    foreach (PolicyState state in states)
                    {
                        try
                        {
                            state.Restore();
                        }
                        catch
                        {
                        }
                    }

                    try
                    {
                        if (File.Exists(BackupPath))
                        {
                            File.Delete(BackupPath);
                        }
                    }
                    catch
                    {
                    }

                    _states = null;
                    _applied = false;
                    return false;
                }
            }
        }

        public static void Restore()
        {
            lock (Sync)
            {
                List<PolicyState> states = _states;
                _states = null;
                _applied = false;
                if (states == null)
                {
                    return;
                }

                bool allRestored = true;
                foreach (PolicyState state in states)
                {
                    try
                    {
                        state.Restore();
                    }
                    catch
                    {
                        allRestored = false;
                    }
                }

                if (allRestored)
                {
                    try
                    {
                        if (File.Exists(BackupPath))
                        {
                            File.Delete(BackupPath);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static bool RestoreStaleBackup()
        {
            lock (Sync)
            {
                return RestoreStaleBackupLocked();
            }
        }

        public static void SelfTest()
        {
            lock (Sync)
            {
                List<string> lines = new List<string>();
                const string testRoot = @"Software\WinLockKFC_PolicyTest";
                PolicySpec[] savedSpecs = _specs;
                PolicySpec[] testSpecs = new PolicySpec[]
                {
                    new PolicySpec(testRoot + @"\System", "DisableTaskMgr"),
                    new PolicySpec(testRoot + @"\System", "DisableLockWorkstation"),
                    new PolicySpec(testRoot + @"\System", "DisableChangePassword"),
                    new PolicySpec(testRoot + @"\System", "HideFastUserSwitching"),
                    new PolicySpec(testRoot + @"\Explorer", "NoLogoff"),
                    new PolicySpec(testRoot + @"\Explorer", "NoClose")
                };

                try
                {
                    try { Registry.CurrentUser.DeleteSubKeyTree(testRoot, false); } catch { }
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(testRoot + @"\System"))
                    {
                        key.SetValue("DisableTaskMgr", 7, RegistryValueKind.DWord);
                    }

                    _specs = testSpecs;
                    _states = null;
                    _applied = false;

                    bool applied = Apply();
                    string duringTaskMgr = ReadDword(testRoot + @"\System", "DisableTaskMgr");
                    string duringLock = ReadDword(testRoot + @"\System", "DisableLockWorkstation");
                    lines.Add("apply=" + applied);
                    lines.Add("during: DisableTaskMgr=" + duringTaskMgr + " DisableLockWorkstation=" + duringLock);

                    Restore();
                    string afterTaskMgr = ReadDword(testRoot + @"\System", "DisableTaskMgr");
                    string afterLock = ReadDword(testRoot + @"\System", "DisableLockWorkstation");
                    lines.Add("after: DisableTaskMgr=" + afterTaskMgr + " DisableLockWorkstation=" + afterLock);

                    bool passed = applied
                        && duringTaskMgr == "1"
                        && duringLock == "1"
                        && afterTaskMgr == "7"
                        && afterLock == "<absent>";
                    lines.Add("RESULT=" + (passed ? "PASS" : "FAIL"));
                }
                catch (Exception ex)
                {
                    lines.Add("ERROR: " + ex);
                }
                finally
                {
                    _specs = savedSpecs;
                    _states = null;
                    _applied = false;
                    try { Registry.CurrentUser.DeleteSubKeyTree(testRoot, false); } catch { }
                }

                try
                {
                    File.WriteAllLines(IOPath.Combine(IOPath.GetTempPath(), "winlock_policytest.txt"), lines.ToArray());
                }
                catch
                {
                }
            }
        }

        private static string ReadDword(string subKey, string name)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey, false))
            {
                if (key == null)
                {
                    return "<absent>";
                }
                object value = key.GetValue(name, null);
                return value == null ? "<absent>" : value.ToString();
            }
        }

        private static bool RestoreStaleBackupLocked()
        {
            if (!File.Exists(BackupPath))
            {
                return true;
            }

            List<PolicyState> states;
            try
            {
                states = ReadBackup();
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Log("stale backup unreadable: " + ex.Message);
                return false;
            }

            bool allRestored = true;
            foreach (PolicyState state in states)
            {
                try
                {
                    state.Restore();
                }
                catch
                {
                    allRestored = false;
                }
            }

            if (allRestored)
            {
                try
                {
                    File.Delete(BackupPath);
                }
                catch
                {
                }
            }

            return allRestored;
        }

        private static void WriteBackup(List<PolicyState> states)
        {
            string tempPath = BackupPath + ".tmp";
            using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(1);
                writer.Write(states.Count);
                foreach (PolicyState state in states)
                {
                    writer.Write(state.Spec.SubKey);
                    writer.Write(state.Spec.Name);
                    writer.Write(state.Existed);
                    writer.Write((int)state.Kind);
                    if (state.Existed)
                    {
                        WriteValue(writer, state.Kind, state.Value);
                    }
                }
            }

            File.Copy(tempPath, BackupPath, true);
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }
        }

        private static List<PolicyState> ReadBackup()
        {
            List<PolicyState> states = new List<PolicyState>();
            using (FileStream fs = new FileStream(BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int version = reader.ReadInt32();
                if (version != 1)
                {
                    throw new InvalidDataException("Unknown policy backup version");
                }

                int count = reader.ReadInt32();
                if (count < 0 || count > 64)
                {
                    throw new InvalidDataException("Invalid policy backup count");
                }

                for (int i = 0; i < count; i++)
                {
                    string subKey = reader.ReadString();
                    string name = reader.ReadString();
                    bool existed = reader.ReadBoolean();
                    RegistryValueKind kind = (RegistryValueKind)reader.ReadInt32();
                    object value = null;
                    if (existed)
                    {
                        value = ReadValue(reader, kind);
                    }

                    PolicyState state = new PolicyState();
                    state.Spec = new PolicySpec(subKey, name);
                    state.Existed = existed;
                    state.Kind = kind;
                    state.Value = value;
                    states.Add(state);
                }
            }
            return states;
        }

        private static void WriteValue(BinaryWriter writer, RegistryValueKind kind, object value)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    writer.Write(Convert.ToInt32(value));
                    break;
                case RegistryValueKind.QWord:
                    writer.Write(Convert.ToInt64(value));
                    break;
                case RegistryValueKind.Binary:
                    byte[] binary = value as byte[];
                    if (binary == null)
                    {
                        binary = new byte[0];
                    }
                    writer.Write(binary.Length);
                    writer.Write(binary);
                    break;
                case RegistryValueKind.MultiString:
                    string[] strings = value as string[];
                    if (strings == null)
                    {
                        strings = new string[0];
                    }
                    writer.Write(strings.Length);
                    foreach (string s in strings)
                    {
                        writer.Write(s ?? "");
                    }
                    break;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    writer.Write(Convert.ToString(value) ?? "");
                    break;
                default:
                    writer.Write(Convert.ToString(value) ?? "");
                    break;
            }
        }

        private static object ReadValue(BinaryReader reader, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    return reader.ReadInt32();
                case RegistryValueKind.QWord:
                    return reader.ReadInt64();
                case RegistryValueKind.Binary:
                    int length = reader.ReadInt32();
                    if (length < 0 || length > 1024 * 1024)
                    {
                        throw new InvalidDataException("Invalid binary policy value");
                    }
                    return reader.ReadBytes(length);
                case RegistryValueKind.MultiString:
                    int count = reader.ReadInt32();
                    if (count < 0 || count > 1024)
                    {
                        throw new InvalidDataException("Invalid multi-string policy value");
                    }
                    string[] strings = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        strings[i] = reader.ReadString();
                    }
                    return strings;
                default:
                    return reader.ReadString();
            }
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    IOPath.Combine(IOPath.GetTempPath(), "winlock_policy.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }

        private sealed class PolicySpec
        {
            public readonly string SubKey;
            public readonly string Name;

            public PolicySpec(string subKey, string name)
            {
                SubKey = subKey;
                Name = name;
            }
        }

        private sealed class PolicyState
        {
            public PolicySpec Spec;
            public bool Existed;
            public RegistryValueKind Kind;
            public object Value;

            public static PolicyState Capture(PolicySpec spec)
            {
                PolicyState state = new PolicyState();
                state.Spec = spec;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(spec.SubKey, false))
                {
                    if (key != null)
                    {
                        string actualName = null;
                        foreach (string valueName in key.GetValueNames())
                        {
                            if (string.Equals(valueName, spec.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                actualName = valueName;
                                break;
                            }
                        }

                        if (actualName != null)
                        {
                            state.Existed = true;
                            state.Kind = key.GetValueKind(actualName);
                            state.Value = key.GetValue(actualName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        }
                    }
                }
                return state;
            }

            public void Apply()
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(Spec.SubKey))
                {
                    if (key == null)
                    {
                        throw new InvalidOperationException("Cannot open policy key");
                    }
                    key.SetValue(Spec.Name, 1, RegistryValueKind.DWord);
                }
            }

            public void Restore()
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Spec.SubKey, true))
                {
                    if (key == null)
                    {
                        if (!Existed)
                        {
                            return;
                        }

                        using (RegistryKey created = Registry.CurrentUser.CreateSubKey(Spec.SubKey))
                        {
                            if (created != null)
                            {
                                WriteValue(created, Spec.Name, Kind, Value);
                            }
                        }
                        return;
                    }

                    if (Existed)
                    {
                        WriteValue(key, Spec.Name, Kind, Value);
                    }
                    else
                    {
                        key.DeleteValue(Spec.Name, false);
                    }
                }
            }

            private static void WriteValue(RegistryKey key, string name, RegistryValueKind kind, object value)
            {
                switch (kind)
                {
                    case RegistryValueKind.DWord:
                        key.SetValue(name, Convert.ToInt32(value), RegistryValueKind.DWord);
                        break;
                    case RegistryValueKind.QWord:
                        key.SetValue(name, Convert.ToInt64(value), RegistryValueKind.QWord);
                        break;
                    case RegistryValueKind.Binary:
                        byte[] binary = value as byte[];
                        if (binary == null)
                        {
                            binary = new byte[0];
                        }
                        key.SetValue(name, binary, RegistryValueKind.Binary);
                        break;
                    case RegistryValueKind.MultiString:
                        string[] strings = value as string[];
                        if (strings == null)
                        {
                            strings = new string[0];
                        }
                        key.SetValue(name, strings, RegistryValueKind.MultiString);
                        break;
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                        key.SetValue(name, Convert.ToString(value) ?? "", kind);
                        break;
                    default:
                        key.SetValue(name, value);
                        break;
                }
            }
        }
    }

    internal sealed class Sprite
    {
        public Image View;
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public double Angle;
        public double Spin;
        public double W;
        public double H;
        public double Radius;
        public RotateTransform Rotation;
    }

    internal sealed class FlyingCat
    {
        public Image View;
        public Polyline Trail;
        public double X;
        public double Y;
        public double W;
        public double H;
        public double BaseY;
        public double Direction;
        public double BaseSpeed;
        public double SpeedScale = 1.0;
        public double NextSpeedAt;
        public double Phase;
        public ScaleTransform Flip;
        public RotateTransform Tilt;
        public double LastTrailX;
        public double LastTrailY;
    }

    internal sealed class WalkingChicken
    {
        public Image View;
        public double X;
        public double Y;
        public double W;
        public double H;
        public double BaseY;
        public double Vx;
        public double Phase;
        public double BornAt;
        public double LifeSeconds;
        public ScaleTransform Flip;
        public RotateTransform Tilt;
    }

    internal sealed class Wing
    {
        public Image View;
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public double Angle;
        public double Spin;
        public double W;
        public double H;
        public double BornAt;
        public double LifeSeconds;
        public RotateTransform Rotation;
    }

    internal sealed class SfxVoice
    {
        public MediaPlayer Player;
        public DateTime KillAt;
        public bool Released;
    }

    internal sealed class Achievement
    {
        public string Id;
        public string Title;
        public string Description;
    }

    public sealed class LockWindow : Window
    {
        private const string Password = "3015";
        private const int MusicStartSeconds = 9;
        private const double MusicVolume = 0.175;
        private const double LoudVolumeMultiplier = 4.5;
        private const string MusicFile = "music.mp3";
        private const string MusicLoudFile = "music_loud.mp3";
        private const int TotalSeconds = 480;
        private const int MaxSprites = 30;
        private const int MaxCats = 4;
        private const int MaxChickens = 8;
        private const int MaxSfxVoices = 3;
        private const string SfxTempFolder = "winlock_kfc_sfx";

        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private const uint EsContinuous = 0x80000000;
        private const uint EsSystemRequired = 0x00000001;
        private const uint EsDisplayRequired = 0x00000002;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KbdLlHookStruct
        {
            public uint VkCode;
            public uint ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private readonly bool _preview;
        private readonly bool _smoke;
        private bool _loud;
        private readonly Random _random = new Random();

        private readonly double _primaryW;
        private readonly double _primaryH;
        private readonly double _primaryX;
        private readonly double _primaryY;
        private readonly double _virtualW;
        private readonly double _virtualH;

        private readonly DateTime _createdAt = DateTime.UtcNow;

        private Canvas _root;
        private Grid _content;
        private Canvas _spriteCanvas;
        private TextBox _codeBox;
        private TextBlock _errorText;
        private Run _timerRun;
        private string _lastTimerText = "";

        private DateTime _startTime;
        private bool _locked = true;
        private bool _allowClose;
        private bool _closing;
        private bool _timeoutStarted;
        private bool _previewSaved;
        private string _entered = "";

        private DispatcherTimer _clock;
        private DispatcherTimer _topmostTimer;
        private DispatcherTimer _taskmgrTimer;
        private DispatcherTimer _previewTimer;
        private DispatcherTimer _errorTimer;
        private DispatcherTimer _closeTimer;
        private DispatcherTimer _heartbeat;

        private MediaPlayer _media;
        private string _tempAudio;
        private IntPtr _hookHandle = IntPtr.Zero;
        private LowLevelKeyboardProc _hookProc;

        private readonly List<Sprite> _sprites = new List<Sprite>();
        private int _collisions;
        private BitmapSource _kfcSource;
        private DateTime _lastFrame;
        private bool _rendering;
        private double _nextSpawnAt;

        private Canvas _fxCanvas;
        private readonly List<FlyingCat> _cats = new List<FlyingCat>();
        private readonly List<WalkingChicken> _chickens = new List<WalkingChicken>();
        private BitmapSource _catSource;
        private BitmapSource _chickenSource;
        private BitmapSource _wingSource;
        private readonly List<Wing> _wings = new List<Wing>();
        private double _animalTime;

        private readonly object _sfxLock = new object();
        private readonly List<SfxVoice> _sfxVoices = new List<SfxVoice>();
        private string[] _sfxNames = new string[0];
        private readonly List<string> _sfxDeck = new List<string>();
        private string _lastSfxName = "";
        private string _sfxDir;
        private DispatcherTimer _sfxTimer;
        private double _sfxClock;
        private double _nextSfxAt;
        private int _sfxEvents;

        private readonly List<MediaPlayer> _cuePlayers = new List<MediaPlayer>();
        private bool _cuesReady;

        private Viewbox _viewbox;
        private TranslateTransform _shakeTransform;
        private DispatcherTimer _shakeTimer;
        private DateTime _shakeStart;
        private Canvas _crackLayer;
        private int _wrongAttempts;

        private Canvas _dangerLayer;
        private Rectangle _dangerRect;
        private bool _dangerStarted;
        private double _dangerClock;
        private MediaPlayer _heartbeatCue;

        private bool _disco;
        private bool _loudBeforeDisco;
        private double _animalBoost = 1.0;
        private Canvas _discoLayer;
        private Rectangle _discoRect;
        private DispatcherTimer _discoTimer;
        private double _discoHue;
        private int _comboStep;
        private DateTime _comboAt;

        private readonly Queue<Achievement> _achQueue = new Queue<Achievement>();
        private readonly HashSet<string> _achDone = new HashSet<string>();
        private DispatcherTimer _achTimer;
        private Canvas _toastLayer;
        private bool _toastBusy;
        private int _toastToken;
        private int _capsPresses;
        private int _closeAttempts;
        private int _catPets;
        private int _bucketHits;
        private int _backspaces;
        private int _enterPresses;
        private int _digitsTyped;
        private int _altF4;
        private int _discoToggles;
        private int _loudToggles;
        private int _chickensSpawned;
        private readonly HashSet<FlyingCat> _catsPetted = new HashSet<FlyingCat>();

        private Canvas _captchaLayer;
        private UniformGrid _captchaGrid;
        private TextBlock _captchaStatus;
        private int _captchaFails;
        private readonly HashSet<int> _captchaSelected = new HashSet<int>();
        private readonly HashSet<int> _captchaCats = new HashSet<int>();

        public LockWindow(bool preview, bool smoke, bool loud)
        {
            _preview = preview;
            _smoke = smoke;
            _loud = loud;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = preview;
            Topmost = !preview;
            Background = new SolidColorBrush(Color.FromRgb(18, 45, 108));
            Title = preview ? "WinLock KFC Preview" : "Windows Driver Foundation";
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            _primaryW = preview ? 1280.0 : SystemParameters.PrimaryScreenWidth;
            _primaryH = preview ? 720.0 : SystemParameters.PrimaryScreenHeight;
            _primaryX = preview ? 0.0 : -SystemParameters.VirtualScreenLeft;
            _primaryY = preview ? 0.0 : -SystemParameters.VirtualScreenTop;
            _virtualW = preview ? 1280.0 : SystemParameters.VirtualScreenWidth;
            _virtualH = preview ? 720.0 : SystemParameters.VirtualScreenHeight;

            if (preview)
            {
                Width = 1280;
                Height = 720;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = SystemParameters.VirtualScreenLeft;
                Top = SystemParameters.VirtualScreenTop;
                Width = _virtualW;
                Height = _virtualH;
            }

            BuildScene();
            Closing += OnClosing;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        public bool IsLocked
        {
            get { return _locked; }
        }

        public bool IsClosing
        {
            get { return _closing; }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Diag.Log("onclosing allowClose=" + _allowClose);
            if (!_allowClose)
            {
                _closeAttempts++;
                e.Cancel = true;
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Diag.Log("onclosed");
            if (_smoke)
            {
                SmokeLog("window closed");
            }
            Cleanup();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Diag.Log("onloaded preview=" + _preview + " smoke=" + _smoke + " loud=" + _loud);
            if (_preview)
            {
                _startTime = DateTime.UtcNow;
                _clock = new DispatcherTimer();
                _clock.Interval = TimeSpan.FromMilliseconds(250);
                _clock.Tick += delegate { if (_preview) UpdateClock(); };
                _clock.Start();
                SpawnInitialSprites(3);
                SpawnCats(2);
                SpawnChickenGroup(true);
                StartRendering();

                _previewTimer = new DispatcherTimer();
                _previewTimer.Interval = TimeSpan.FromMilliseconds(1400);
                _previewTimer.Tick += delegate { SavePreviewAndClose(); };
                _previewTimer.Start();
                return;
            }

            _startTime = DateTime.UtcNow;
            Activate();
            Focus();

            bool policiesApplied = PolicyGuard.Apply();
            if (!_preview && !_smoke)
            {
                WritePolicyStatus(policiesApplied);
            }
            InstallKeyboardHook();

            _clock = new DispatcherTimer();
            _clock.Interval = TimeSpan.FromMilliseconds(200);
            _clock.Tick += delegate { UpdateClock(); };
            _clock.Start();

            _topmostTimer = new DispatcherTimer();
            _topmostTimer.Interval = TimeSpan.FromMilliseconds(750);
            _topmostTimer.Tick += delegate
            {
                if (_closing || !_locked)
                {
                    return;
                }
                Topmost = true;
                if (!IsActive)
                {
                    Activate();
                }
            };
            _topmostTimer.Start();

            _taskmgrTimer = new DispatcherTimer();
            _taskmgrTimer.Interval = TimeSpan.FromMilliseconds(500);
            _taskmgrTimer.Tick += delegate { KillTaskManager(); };
            _taskmgrTimer.Start();

            _heartbeat = new DispatcherTimer();
            _heartbeat.Interval = TimeSpan.FromSeconds(1);
            _heartbeat.Tick += delegate
            {
                Diag.Log("hb locked=" + _locked + " closing=" + _closing + " visible=" + IsVisible
                    + " active=" + IsActive + " topmost=" + Topmost
                    + " size=" + (int)ActualWidth + "x" + (int)ActualHeight
                    + " sprites=" + _sprites.Count + " media=" + (_media != null)
                    + " collisions=" + _collisions
                    + " cats=" + _cats.Count + " chickens=" + _chickens.Count
                    + " sfx=" + _sfxVoices.Count + " sfxEvents=" + _sfxEvents
                    + " elapsed=" + (int)(DateTime.UtcNow - _startTime).TotalSeconds);
            };
            if (Diag.Enabled)
            {
                _heartbeat.Start();
            }

            SetThreadExecutionState(EsContinuous | EsSystemRequired | EsDisplayRequired);

            SpawnInitialSprites(3);
            SpawnCats(3);
            StartRendering();

            StartMusic();
            StartSfx();
            StartAtmosphere();

            if (_smoke)
            {
                SmokeLog("loaded; hook=" + (_hookHandle != IntPtr.Zero) + "; policies=" + policiesApplied + (string.IsNullOrEmpty(PolicyGuard.LastError) ? "" : "; policyError=" + PolicyGuard.LastError));
                DispatcherTimer smokeTimer = new DispatcherTimer();
                smokeTimer.Interval = TimeSpan.FromMilliseconds(2500);
                smokeTimer.Tick += delegate
                {
                    smokeTimer.Stop();
                    SmokeEnterCode();
                };
                smokeTimer.Start();
            }
        }

        private void WritePolicyStatus(bool applied)
        {
            try
            {
                string path = IOPath.Combine(AppPaths.WorkDir, "policy_status.txt");
                string text;
                if (applied)
                {
                    text = "ПОЛНЫЙ режим: пункты Ctrl+Alt+Del скрыты, пока винлокер открыт.\r\n"
                         + "При закрытии или перезагрузке настройки восстанавливаются.\r\n";
                }
                else
                {
                    text = "ЧАСТИЧНЫЙ режим: нет прав на запись в политики Windows.\r\n"
                         + "Для полного режима запусти WinLockKFC.exe от имени администратора.\r\n"
                         + "Сейчас Диспетчер задач закрывается, а выход/перезагрузка отменяются.\r\n"
                         + "Причина: " + (PolicyGuard.LastError ?? "неизвестна") + "\r\n";
                }
                File.WriteAllText(path, text, new System.Text.UTF8Encoding(true));
                Diag.Log("policy status written: " + path);
            }
            catch
            {
            }
        }

        private void SmokeEnterCode()
        {
            try
            {
                if (!_locked)
                {
                    return;
                }
                _entered = Password;
                _codeBox.Text = Password;
                SmokeLog("entering password");
                CheckCode();
                SmokeLog("unlocked; locked=" + _locked);
                DispatcherTimer close = new DispatcherTimer();
                close.Interval = TimeSpan.FromMilliseconds(2000);
                close.Tick += delegate
                {
                    close.Stop();
                    SmokeLog("smoke closing");
                    ClosePrank();
                };
                close.Start();
            }
            catch (Exception ex)
            {
                SmokeLog("smoke error: " + ex);
            }
        }

        private void SmokeLog(string message)
        {
            try
            {
                File.AppendAllText(IOPath.Combine(IOPath.GetTempPath(), "winlock_smoke.txt"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }

        private void BuildScene()
        {
            _root = new Canvas();
            _root.Width = _virtualW;
            _root.Height = _virtualH;
            _root.ClipToBounds = true;
            _root.Background = Grad(Color.FromRgb(126, 159, 220), Color.FromRgb(167, 193, 238), 90);

            Rectangle rootBg = new Rectangle();
            rootBg.Width = _virtualW;
            rootBg.Height = _virtualH;
            rootBg.Fill = _root.Background;
            Canvas.SetLeft(rootBg, 0);
            Canvas.SetTop(rootBg, 0);
            _root.Children.Add(rootBg);

            _content = BuildContent();
            _viewbox = new Viewbox();
            _viewbox.Width = _primaryW;
            _viewbox.Height = _primaryH;
            _viewbox.Stretch = Stretch.Uniform;
            _viewbox.Child = _content;
            _shakeTransform = new TranslateTransform(0, 0);
            _viewbox.RenderTransform = _shakeTransform;
            Canvas.SetLeft(_viewbox, _primaryX);
            Canvas.SetTop(_viewbox, _primaryY);
            Panel.SetZIndex(_viewbox, 10);
            _root.Children.Add(_viewbox);

            _spriteCanvas = new Canvas();
            _spriteCanvas.Width = _virtualW;
            _spriteCanvas.Height = _virtualH;
            _spriteCanvas.IsHitTestVisible = false;
            _spriteCanvas.Background = null;
            Panel.SetZIndex(_spriteCanvas, 100);
            _root.Children.Add(_spriteCanvas);

            _fxCanvas = new Canvas();
            _fxCanvas.Width = _virtualW;
            _fxCanvas.Height = _virtualH;
            _fxCanvas.IsHitTestVisible = false;
            _fxCanvas.Background = null;
            Panel.SetZIndex(_fxCanvas, 105);
            _root.Children.Add(_fxCanvas);

            BuildEffectLayers();
            _root.MouseLeftButtonDown += OnRootMouseDown;

            Content = _root;
        }

        private Grid BuildContent()
        {
            Grid root = new Grid();
            root.Width = 1280;
            root.Height = 720;
            root.ClipToBounds = true;
            root.Background = Grad(Color.FromRgb(166, 192, 235), Color.FromRgb(126, 159, 219), 90);

            RowDefinition rowHeader = new RowDefinition();
            rowHeader.Height = new GridLength(96);
            RowDefinition rowBody = new RowDefinition();
            rowBody.Height = new GridLength(516);
            RowDefinition rowFooter = new RowDefinition();
            rowFooter.Height = new GridLength(108);
            root.RowDefinitions.Add(rowHeader);
            root.RowDefinitions.Add(rowBody);
            root.RowDefinitions.Add(rowFooter);

            Brush barBg = Grad(Color.FromRgb(5, 37, 120), Color.FromRgb(48, 102, 201), 90);
            SolidColorBrush barEdge = new SolidColorBrush(Color.FromRgb(105, 149, 230));
            barEdge.Freeze();

            Border header = new Border();
            header.Background = barBg;
            header.BorderBrush = barEdge;
            header.BorderThickness = new Thickness(0, 0, 0, 2);
            Grid.SetRow(header, 0);

            TextBlock headerText = new TextBlock();
            headerText.Text = "Windows заблокирован!";
            headerText.FontFamily = new FontFamily("Tahoma");
            headerText.FontSize = 48;
            headerText.FontWeight = FontWeights.Bold;
            headerText.Foreground = Brushes.White;
            headerText.HorizontalAlignment = HorizontalAlignment.Center;
            headerText.VerticalAlignment = VerticalAlignment.Center;
            headerText.Effect = Shadow(3, 3, 0.75);
            header.Child = headerText;
            root.Children.Add(header);

            Grid body = new Grid();
            body.Background = Grad(Color.FromRgb(173, 198, 241), Color.FromRgb(132, 167, 226), 90);
            Grid.SetRow(body, 1);

            Rectangle stripes = new Rectangle();
            stripes.Fill = CreateStripeBrush();
            stripes.IsHitTestVisible = false;
            body.Children.Add(stripes);

            Canvas canvas = new Canvas();
            canvas.ClipToBounds = true;
            body.Children.Add(canvas);

            TextBlock message1 = new TextBlock();
            message1.Width = 1176;
            message1.FontFamily = new FontFamily("Tahoma");
            message1.FontSize = 22;
            message1.LineHeight = 31;
            message1.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            message1.Foreground = new SolidColorBrush(Color.FromRgb(242, 247, 255));
            message1.Effect = Shadow(2, 2, 0.6);
            message1.Inlines.Add(new Run("Microsoft Security обнаружил нарушения использования сети интернет."));
            message1.Inlines.Add(new LineBreak());
            message1.Inlines.Add(new Run("Причина: Вы смотрели фильмы содержащие гей-порно."));
            Canvas.SetLeft(message1, 52);
            Canvas.SetTop(message1, 28);
            canvas.Children.Add(message1);

            TextBlock message2 = new TextBlock();
            message2.Width = 1176;
            message2.FontFamily = new FontFamily("Tahoma");
            message2.FontSize = 22;
            message2.LineHeight = 31;
            message2.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            message2.Foreground = new SolidColorBrush(Color.FromRgb(242, 247, 255));
            message2.Effect = Shadow(2, 2, 0.6);

            Run titleRun = new Run("Для разблокировки Windows необходимо:");
            titleRun.FontWeight = FontWeights.Bold;
            message2.Inlines.Add(titleRun);
            message2.Inlines.Add(new LineBreak());
            message2.Inlines.Add(new Run("Пополнить номер абонента Билайн 8-800-555-35-35 на сумму 400 рублей"));
            message2.Inlines.Add(new LineBreak());
            message2.Inlines.Add(new Run("Оплатить можно через терминал для оплаты сотовой связи."));
            message2.Inlines.Add(new LineBreak());
            message2.Inlines.Add(new Run("После оплаты, на выданном терминалом чеке, Вы найдёте Ваш"));
            message2.Inlines.Add(new LineBreak());
            message2.Inlines.Add(new Run("персональный код разблокировки, который необходимо ввести ниже."));
            Canvas.SetLeft(message2, 52);
            Canvas.SetTop(message2, 126);
            canvas.Children.Add(message2);

            Grid keypadHost = new Grid();
            keypadHost.Width = 1280;
            keypadHost.Height = 46;
            StackPanel keypad = new StackPanel();
            keypad.Orientation = Orientation.Horizontal;
            keypad.HorizontalAlignment = HorizontalAlignment.Center;
            keypad.VerticalAlignment = VerticalAlignment.Center;
            for (int i = 0; i <= 9; i++)
            {
                int digit = i;
                Button b = MakeButton(digit.ToString(), 50, 44, 24, false);
                b.Click += delegate { AppendDigit(digit); };
                keypad.Children.Add(b);
            }
            Button clear = MakeButton("очистить", 112, 44, 17, false);
            clear.Click += delegate { ClearCode(); };
            keypad.Children.Add(clear);
            keypadHost.Children.Add(keypad);
            Canvas.SetLeft(keypadHost, 0);
            Canvas.SetTop(keypadHost, 312);
            canvas.Children.Add(keypadHost);

            Grid codeHost = new Grid();
            codeHost.Width = 1280;
            codeHost.Height = 46;
            StackPanel codeRow = new StackPanel();
            codeRow.Orientation = Orientation.Horizontal;
            codeRow.HorizontalAlignment = HorizontalAlignment.Center;
            codeRow.VerticalAlignment = VerticalAlignment.Center;

            TextBlock codeLabel = new TextBlock();
            codeLabel.Text = "Ваш код:";
            codeLabel.FontFamily = new FontFamily("Tahoma");
            codeLabel.FontSize = 25;
            codeLabel.FontWeight = FontWeights.Bold;
            codeLabel.Foreground = Brushes.White;
            codeLabel.VerticalAlignment = VerticalAlignment.Center;
            codeLabel.Margin = new Thickness(0, 0, 14, 0);
            codeLabel.Effect = Shadow(2, 2, 0.65);
            codeRow.Children.Add(codeLabel);

            _codeBox = new TextBox();
            _codeBox.Width = 340;
            _codeBox.Height = 44;
            _codeBox.FontFamily = new FontFamily("Tahoma");
            _codeBox.FontSize = 26;
            _codeBox.Foreground = Brushes.Black;
            _codeBox.Background = Brushes.White;
            _codeBox.BorderBrush = new SolidColorBrush(Color.FromRgb(95, 126, 170));
            _codeBox.BorderThickness = new Thickness(2);
            _codeBox.Padding = new Thickness(8, 0, 8, 0);
            _codeBox.VerticalContentAlignment = VerticalAlignment.Center;
            _codeBox.IsReadOnly = true;
            _codeBox.IsTabStop = false;
            _codeBox.Focusable = false;
            _codeBox.Margin = new Thickness(0, 0, 14, 0);
            codeRow.Children.Add(_codeBox);

            Button unlock = MakeButton("ВХОД В СИСТЕМУ", 260, 44, 18, true);
            unlock.Click += delegate { CheckCode(); };
            codeRow.Children.Add(unlock);

            codeHost.Children.Add(codeRow);
            Canvas.SetLeft(codeHost, 0);
            Canvas.SetTop(codeHost, 378);
            canvas.Children.Add(codeHost);

            _errorText = new TextBlock();
            _errorText.Width = 1280;
            _errorText.Text = "Неверный код разблокировки";
            _errorText.TextAlignment = TextAlignment.Center;
            _errorText.FontFamily = new FontFamily("Tahoma");
            _errorText.FontSize = 21;
            _errorText.FontWeight = FontWeights.Bold;
            _errorText.Foreground = new SolidColorBrush(Color.FromRgb(255, 60, 60));
            _errorText.Effect = Shadow(2, 2, 0.7);
            _errorText.Visibility = Visibility.Hidden;
            Canvas.SetLeft(_errorText, 0);
            Canvas.SetTop(_errorText, 432);
            canvas.Children.Add(_errorText);

            root.Children.Add(body);

            Border footer = new Border();
            footer.Background = barBg;
            footer.BorderBrush = barEdge;
            footer.BorderThickness = new Thickness(0, 2, 0, 0);
            Grid.SetRow(footer, 2);

            TextBlock footerText = new TextBlock();
            footerText.FontFamily = new FontFamily("Tahoma");
            footerText.FontSize = 21;
            footerText.LineHeight = 28;
            footerText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            footerText.TextAlignment = TextAlignment.Center;
            footerText.Foreground = Brushes.White;
            footerText.HorizontalAlignment = HorizontalAlignment.Center;
            footerText.VerticalAlignment = VerticalAlignment.Center;
            footerText.Effect = Shadow(2, 2, 0.5);

            footerText.Inlines.Add(new Run("Если в течение "));
            _timerRun = new Run("08:00");
            _timerRun.FontWeight = FontWeights.Bold;
            _timerRun.Foreground = new SolidColorBrush(Color.FromRgb(255, 231, 130));
            footerText.Inlines.Add(_timerRun);
            footerText.Inlines.Add(new Run(" с момента появления данного сообщения, не будет введён код,"));
            footerText.Inlines.Add(new LineBreak());
            footerText.Inlines.Add(new Run("все данные, включая Windows и bios будут БЕЗВОЗВРАТНО УДАЛЕНЫ. Попытка"));
            footerText.Inlines.Add(new LineBreak());
            footerText.Inlines.Add(new Run("переустановить систему приведёт к нарушениям работы компьютера."));
            footer.Child = footerText;
            root.Children.Add(footer);

            PreviewKeyDown += OnPreviewKeyDown;

            return root;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_locked || _preview)
            {
                return;
            }

            if (e.Key == Key.CapsLock)
            {
                _capsPresses++;
            }
            UpdateKfcCombo(e.Key);

            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                AppendDigit((int)(e.Key - Key.D0));
                e.Handled = true;
                return;
            }
            if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                AppendDigit((int)(e.Key - Key.NumPad0));
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Back)
            {
                if (_entered.Length > 0)
                {
                    _entered = _entered.Substring(0, _entered.Length - 1);
                    _codeBox.Text = _entered;
                    _backspaces++;
                    AwardAt("backspace10", _backspaces, 10, "Стиратель", "Backspace нажат 10 раз");
                }
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Enter)
            {
                _enterPresses++;
                AwardAt("enter5", _enterPresses, 5, "Нетерпеливый", "Enter нажат 5 раз");
                CheckCode();
                e.Handled = true;
            }
        }

        private void AppendDigit(int digit)
        {
            if (!_locked || _entered.Length >= 4)
            {
                return;
            }
            _entered += digit.ToString();
            _codeBox.Text = _entered;
            _errorText.Visibility = Visibility.Hidden;
            _digitsTyped++;
            AwardAt("firstdigit", _digitsTyped, 1, "Первый палец", "Введена первая цифра");
            if (_entered.Length >= 4)
            {
                UnlockAchievement("all4", "Четыре цифры", "Код введён полностью");
            }
        }

        private void ClearCode()
        {
            _entered = "";
            _codeBox.Text = "";
            _errorText.Visibility = Visibility.Hidden;
        }

        private void CheckCode()
        {
            if (!_locked)
            {
                return;
            }

            Diag.Log("check code len=" + _entered.Length + " value='" + _entered + "'");

            if (_entered == Password)
            {
                _entered = "";
                _codeBox.Text = "";
                _errorText.Visibility = Visibility.Hidden;
                BeginCaptcha();
                return;
            }

            _entered = "";
            _codeBox.Text = "";
            _errorText.Visibility = Visibility.Visible;
            RegisterWrongAttempt();

            if (_errorTimer != null)
            {
                _errorTimer.Stop();
            }
            _errorTimer = new DispatcherTimer();
            _errorTimer.Interval = TimeSpan.FromMilliseconds(1600);
            _errorTimer.Tick += delegate
            {
                _errorTimer.Stop();
                _errorText.Visibility = Visibility.Hidden;
            };
            _errorTimer.Start();
        }

        private void UpdateClock()
        {
            if (!_locked)
            {
                return;
            }

            TimeSpan elapsed = DateTime.UtcNow - _startTime;
            TimeSpan remaining = TimeSpan.FromSeconds(TotalSeconds) - elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            string text = string.Format("{0:00}:{1:00}", (int)remaining.TotalMinutes, remaining.Seconds);
            if (text != _lastTimerText)
            {
                _lastTimerText = text;
                if (_timerRun != null)
                {
                    _timerRun.Text = text;
                }
            }

            if (!_preview && remaining.TotalSeconds <= 30.0 && remaining > TimeSpan.Zero)
            {
                StartDangerMode();
            }

            if (remaining <= TimeSpan.Zero && !_timeoutStarted && !_preview)
            {
                _timeoutStarted = true;
                DoReboot();
            }
        }

        private void UnlockPrank()
        {
            if (!_locked)
            {
                return;
            }

            _locked = false;
            PolicyGuard.Restore();
            if (_smoke)
            {
                SmokeLog("unlock prank");
            }
            RemoveCaptcha();
            StopAtmosphere();
            PlayFinale();
            StopLock();
            ShowReveal();
            UnlockAchievement("solved", "Взломщик", "Код и капча пройдены");
        }

        private void StopLock()
        {
            if (_clock != null)
            {
                _clock.Stop();
            }
            if (_topmostTimer != null)
            {
                _topmostTimer.Stop();
            }
            if (_taskmgrTimer != null)
            {
                _taskmgrTimer.Stop();
            }
            UninstallKeyboardHook();
            StopMusic();
            SetThreadExecutionState(EsContinuous);
        }

        private void KillTaskManager()
        {
            if (!_locked || _closing || _preview)
            {
                return;
            }

            try
            {
                Process[] processes = Process.GetProcessesByName("Taskmgr");
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private void ShowReveal()
        {
            Canvas overlay = new Canvas();
            overlay.Background = new SolidColorBrush(Color.FromArgb(220, 2, 24, 66));
            overlay.Width = _virtualW;
            overlay.Height = _virtualH;

            Grid area = new Grid();
            area.Width = _primaryW;
            area.Height = _primaryH;
            Canvas.SetLeft(area, _primaryX);
            Canvas.SetTop(area, _primaryY);
            overlay.Children.Add(area);

            StackPanel panel = new StackPanel();
            panel.HorizontalAlignment = HorizontalAlignment.Center;
            panel.VerticalAlignment = VerticalAlignment.Center;
            area.Children.Add(panel);

            TextBlock big = new TextBlock();
            big.Text = "ШУТКА!";
            big.FontFamily = new FontFamily("Tahoma");
            big.FontSize = 68;
            big.FontWeight = FontWeights.Bold;
            big.Foreground = Brushes.White;
            big.HorizontalAlignment = HorizontalAlignment.Center;
            big.TextAlignment = TextAlignment.Center;
            big.Effect = Shadow(4, 4, 0.8);
            panel.Children.Add(big);

            TextBlock line1 = new TextBlock();
            line1.Text = "Это был пранк. Ничего не удалено.";
            line1.FontFamily = new FontFamily("Tahoma");
            line1.FontSize = 30;
            line1.Foreground = new SolidColorBrush(Color.FromRgb(255, 238, 170));
            line1.HorizontalAlignment = HorizontalAlignment.Center;
            line1.TextAlignment = TextAlignment.Center;
            line1.Margin = new Thickness(0, 20, 0, 0);
            line1.Effect = Shadow(2, 2, 0.6);
            panel.Children.Add(line1);

            TextBlock line2 = new TextBlock();
            line2.Text = "С тебя ведро крылышек КФС!";
            line2.FontFamily = new FontFamily("Tahoma");
            line2.FontSize = 26;
            line2.Foreground = Brushes.White;
            line2.HorizontalAlignment = HorizontalAlignment.Center;
            line2.TextAlignment = TextAlignment.Center;
            line2.Margin = new Thickness(0, 12, 0, 0);
            line2.Effect = Shadow(2, 2, 0.6);
            panel.Children.Add(line2);

            Button action = MakeButton("ЗАКРЫТЬ", 260, 50, 18, true);
            action.Margin = new Thickness(0, 34, 0, 0);
            action.HorizontalAlignment = HorizontalAlignment.Center;
            action.Click += delegate
            {
                if (_closeTimer != null)
                {
                    _closeTimer.Stop();
                }
                ClosePrank();
            };
            panel.Children.Add(action);

            TextBlock hint = new TextBlock();
            hint.Text = "P.S. Это не вирус. Можешь выдохнуть.";
            hint.FontFamily = new FontFamily("Tahoma");
            hint.FontSize = 14;
            hint.Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));
            hint.HorizontalAlignment = HorizontalAlignment.Center;
            hint.Margin = new Thickness(0, 26, 0, 0);
            panel.Children.Add(hint);

            Panel.SetZIndex(overlay, 50);
            _root.Children.Add(overlay);

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(20);
            _closeTimer.Tick += delegate
            {
                _closeTimer.Stop();
                ClosePrank();
            };
            _closeTimer.Start();
        }

        private void DoReboot()
        {
            if (_closing)
            {
                return;
            }

            _closing = true;
            _allowClose = true;
            _locked = false;

            PolicyGuard.Restore();

            try
            {
                string shutdownExe = IOPath.Combine(Environment.SystemDirectory, "shutdown.exe");
                ProcessStartInfo psi = new ProcessStartInfo(shutdownExe, "/r /t 0 /f");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process.Start(psi);
            }
            catch
            {
            }

            Cleanup();
            try
            {
                Close();
            }
            catch
            {
            }
            Application.Current.Shutdown();
        }

        private void ClosePrank()
        {
            if (_closing)
            {
                return;
            }

            _closing = true;
            _allowClose = true;
            Cleanup();
            try
            {
                Close();
            }
            catch
            {
            }
            Application.Current.Shutdown();
        }

        private void Cleanup()
        {
            Diag.Log("cleanup start");
            _locked = false;
            try { if (_clock != null) _clock.Stop(); } catch { }
            try { if (_topmostTimer != null) _topmostTimer.Stop(); } catch { }
            try { if (_taskmgrTimer != null) _taskmgrTimer.Stop(); } catch { }
            try { if (_previewTimer != null) _previewTimer.Stop(); } catch { }
            try { if (_errorTimer != null) _errorTimer.Stop(); } catch { }
            try { if (_closeTimer != null) _closeTimer.Stop(); } catch { }
            try { if (_heartbeat != null) _heartbeat.Stop(); } catch { }
            RemoveCaptcha();
            StopAtmosphere();
            UninstallKeyboardHook();
            StopMusic();
            StopSfx();
            StopRendering();
            bool policiesWereApplied = PolicyGuard.IsApplied;
            PolicyGuard.Restore();
            try { SetThreadExecutionState(EsContinuous); } catch { }
            try
            {
                DeleteTempAudio("winlock_kfc_music.mp3");
                DeleteTempAudio("winlock_kfc_music_loud.mp3");
            }
            catch
            {
            }
            if (_smoke)
            {
                SmokeLog("cleanup; policiesWereApplied=" + policiesWereApplied);
            }
        }

        private void StartMusic()
        {
            try
            {
                string resourceName = _loud ? MusicLoudFile : MusicFile;
                Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (res == null && _loud)
                {
                    resourceName = MusicFile;
                    res = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                }
                if (res == null)
                {
                    return;
                }

                _tempAudio = IOPath.Combine(
                    IOPath.GetTempPath(),
                    _loud ? "winlock_kfc_music_loud.mp3" : "winlock_kfc_music.mp3");
                using (res)
                {
                    using (FileStream fs = new FileStream(_tempAudio, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        res.CopyTo(fs);
                    }
                }

                MediaPlayer player = new MediaPlayer();
                _media = player;
                Diag.Log("startmusic file=" + resourceName + " temp=" + _tempAudio
                    + " volume=" + (_smoke ? 0.0 : (_loud ? Math.Min(1.0, MusicVolume * LoudVolumeMultiplier) : MusicVolume))
                    + " loud=" + _loud);
                player.Volume = _smoke ? 0.0 : (_loud ? Math.Min(1.0, MusicVolume * LoudVolumeMultiplier) : MusicVolume);
                player.MediaOpened += delegate
                {
                    if (!ReferenceEquals(_media, player))
                    {
                        return;
                    }
                    try
                    {
                        if (_smoke)
                        {
                            SmokeLog("media opened; loud=" + _loud);
                        }
                        Diag.Log("media opened; loud=" + _loud);
                        player.Position = TimeSpan.FromSeconds(MusicStartSeconds);
                        player.Play();
                    }
                    catch
                    {
                    }
                };
                player.MediaEnded += delegate
                {
                    if (!ReferenceEquals(_media, player))
                    {
                        return;
                    }
                    try
                    {
                        player.Position = TimeSpan.FromSeconds(MusicStartSeconds);
                        player.Play();
                    }
                    catch
                    {
                    }
                };
                player.MediaFailed += delegate { };
                player.Open(new Uri(_tempAudio, UriKind.Absolute));
            }
            catch
            {
                _media = null;
            }
        }

        private void StopMusic()
        {
            MediaPlayer player = _media;
            _media = null;
            if (player == null)
            {
                return;
            }

            try
            {
                player.Stop();
                player.Close();
            }
            catch
            {
            }
        }

        private void ToggleLoud()
        {
            Diag.Log("toggle loud -> " + (!_loud));
            if (!_locked || _closing || _preview || _smoke)
            {
                return;
            }

            _loud = !_loud;
            StopMusic();
            StartMusic();
            _loudToggles++;
            AwardAt("loud", _loudToggles, 1, "Погромче!", "Ctrl+Shift+L сработал");
        }

        private static void DeleteTempAudio(string fileName)
        {
            try
            {
                string path = IOPath.Combine(IOPath.GetTempPath(), fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private void StartRendering()
        {
            if (_rendering)
            {
                return;
            }
            _rendering = true;
            _lastFrame = DateTime.UtcNow;
            _nextSpawnAt = 10;
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopRendering()
        {
            if (!_rendering)
            {
                return;
            }
            _rendering = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            double dt = (now - _lastFrame).TotalSeconds;
            _lastFrame = now;
            if (dt <= 0)
            {
                return;
            }
            if (dt > 0.1)
            {
                dt = 0.1;
            }

            for (int i = 0; i < _sprites.Count; i++)
            {
                Sprite s = _sprites[i];
                s.X += s.Vx * dt;
                s.Y += s.Vy * dt;
                s.Angle += s.Spin * dt;

                if (s.X < 0)
                {
                    s.X = 0;
                    s.Vx = Math.Abs(s.Vx);
                }
                else if (s.X + s.W > _virtualW)
                {
                    s.X = _virtualW - s.W;
                    s.Vx = -Math.Abs(s.Vx);
                }
                if (s.Y < 0)
                {
                    s.Y = 0;
                    s.Vy = Math.Abs(s.Vy);
                }
                else if (s.Y + s.H > _virtualH)
                {
                    s.Y = _virtualH - s.H;
                    s.Vy = -Math.Abs(s.Vy);
                }

                Canvas.SetLeft(s.View, s.X);
                Canvas.SetTop(s.View, s.Y);
                s.Rotation.Angle = s.Angle;
            }

            ResolveSpriteCollisions();
            UpdateAnimals(dt);
            UpdateWings(dt);
            UpdateDangerPulse(dt);

            if (_locked && !_preview && _sprites.Count < MaxSprites)
            {
                double elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
                if (elapsed >= _nextSpawnAt)
                {
                    _nextSpawnAt = elapsed + 10;
                    SpawnSprite(true);
                }
            }
        }

        private void ResolveSpriteCollisions()
        {
            const double restitution = 0.92;

            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < _sprites.Count; i++)
                {
                    Sprite a = _sprites[i];
                    for (int j = i + 1; j < _sprites.Count; j++)
                    {
                        Sprite b = _sprites[j];

                        double ax = a.X + a.W * 0.5;
                        double ay = a.Y + a.H * 0.5;
                        double bx = b.X + b.W * 0.5;
                        double by = b.Y + b.H * 0.5;
                        double dx = bx - ax;
                        double dy = by - ay;
                        double minDistance = a.Radius + b.Radius;
                        double distanceSq = dx * dx + dy * dy;

                        if (distanceSq >= minDistance * minDistance || distanceSq < 0.0001)
                        {
                            continue;
                        }

                        double distance = Math.Sqrt(distanceSq);
                        double nx = dx / distance;
                        double ny = dy / distance;
                        double overlap = minDistance - distance;

                        a.X -= nx * overlap * 0.5;
                        a.Y -= ny * overlap * 0.5;
                        b.X += nx * overlap * 0.5;
                        b.Y += ny * overlap * 0.5;

                        double relativeX = b.Vx - a.Vx;
                        double relativeY = b.Vy - a.Vy;
                        double normalSpeed = relativeX * nx + relativeY * ny;

                        if (normalSpeed < 0)
                        {
                            double impulse = -(1.0 + restitution) * normalSpeed * 0.5;
                            a.Vx -= impulse * nx;
                            a.Vy -= impulse * ny;
                            b.Vx += impulse * nx;
                            b.Vy += impulse * ny;

                            double spinKick = impulse * 0.16;
                            a.Spin -= spinKick;
                            b.Spin += spinKick;
                            _collisions++;
                        }
                    }
                }

                for (int i = 0; i < _sprites.Count; i++)
                {
                    Sprite s = _sprites[i];
                    double maxX = Math.Max(0, _virtualW - s.W);
                    double maxY = Math.Max(0, _virtualH - s.H);
                    s.X = Math.Max(0, Math.Min(maxX, s.X));
                    s.Y = Math.Max(0, Math.Min(maxY, s.Y));
                }
            }

            for (int i = 0; i < _sprites.Count; i++)
            {
                Sprite s = _sprites[i];
                Canvas.SetLeft(s.View, s.X);
                Canvas.SetTop(s.View, s.Y);
            }
        }

        private static LinearGradientBrush RainbowBrush()
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 0);
            Color[] colors = new Color[]
            {
                Color.FromRgb(255, 48, 48),
                Color.FromRgb(255, 150, 32),
                Color.FromRgb(255, 232, 44),
                Color.FromRgb(64, 224, 96),
                Color.FromRgb(48, 172, 255),
                Color.FromRgb(96, 96, 255),
                Color.FromRgb(198, 84, 255)
            };
            for (int i = 0; i < colors.Length; i++)
            {
                brush.GradientStops.Add(new GradientStop(colors[i], i / (double)(colors.Length - 1)));
            }
            brush.Freeze();
            return brush;
        }

        private void SpawnCats(int count)
        {
            if (_catSource == null)
            {
                _catSource = LoadBitmap("cat.png");
                if (_catSource == null)
                {
                    Diag.Log("cat sprite missing");
                    return;
                }
            }

            for (int i = 0; i < count && _cats.Count < MaxCats; i++)
            {
                double width = 118 + _random.NextDouble() * 58;
                double height = width * _catSource.PixelHeight / (double)_catSource.PixelWidth;

                Image img = new Image();
                img.Source = _catSource;
                img.Width = width;
                img.Height = height;
                img.Stretch = Stretch.Uniform;
                img.IsHitTestVisible = false;
                img.RenderTransformOrigin = new Point(0.5, 0.5);

                ScaleTransform flip = new ScaleTransform(1, 1);
                RotateTransform tilt = new RotateTransform(0);
                TransformGroup group = new TransformGroup();
                group.Children.Add(flip);
                group.Children.Add(tilt);
                img.RenderTransform = group;

                Polyline trail = new Polyline();
                trail.Stroke = RainbowBrush();
                trail.StrokeThickness = 13;
                trail.StrokeStartLineCap = PenLineCap.Round;
                trail.StrokeEndLineCap = PenLineCap.Round;
                trail.StrokeLineJoin = PenLineJoin.Round;
                trail.Opacity = 0.62;
                trail.IsHitTestVisible = false;
                Panel.SetZIndex(trail, 0);
                _fxCanvas.Children.Add(trail);

                FlyingCat cat = new FlyingCat();
                cat.View = img;
                cat.Trail = trail;
                cat.W = width;
                cat.H = height;
                cat.Flip = flip;
                cat.Tilt = tilt;
                cat.Direction = _random.NextDouble() < 0.5 ? -1 : 1;
                cat.BaseSpeed = 100 + _random.NextDouble() * 90;
                cat.SpeedScale = 1.0;
                cat.Phase = _random.NextDouble() * Math.PI * 2;
                cat.NextSpeedAt = _animalTime + 1.0 + _random.NextDouble() * 2.5;
                cat.LastTrailX = double.NaN;
                cat.LastTrailY = double.NaN;
                cat.X = _random.NextDouble() * Math.Max(1.0, _virtualW - width);
                cat.BaseY = 20 + _random.NextDouble() * Math.Max(1.0, _virtualH * 0.60);
                cat.Y = cat.BaseY;

                flip.ScaleX = cat.Direction > 0 ? -1 : 1;
                Canvas.SetLeft(img, cat.X);
                Canvas.SetTop(img, cat.Y);
                Panel.SetZIndex(img, 10);
                _fxCanvas.Children.Add(img);
                _cats.Add(cat);
            }

            Diag.Log("cats spawned total=" + _cats.Count);
        }

        private void SpawnChickenGroup(bool onScreen)
        {
            if (_chickenSource == null)
            {
                _chickenSource = LoadBitmap("chicken.png");
                if (_chickenSource == null)
                {
                    Diag.Log("chicken sprite missing");
                    return;
                }
            }

            int room = MaxChickens - _chickens.Count;
            if (room <= 0)
            {
                return;
            }

            int count = Math.Min(room, 2 + (_random.NextDouble() < 0.6 ? 1 : 0));
            bool fromLeft = _random.NextDouble() < 0.5;

            for (int i = 0; i < count; i++)
            {
                double width = 84 + _random.NextDouble() * 38;
                double height = width * _chickenSource.PixelHeight / (double)_chickenSource.PixelWidth;
                double direction = fromLeft ? 1 : -1;
                double x;
                if (onScreen)
                {
                    x = 20 + _random.NextDouble() * Math.Max(1.0, _virtualW - width - 40);
                }
                else
                {
                    x = fromLeft
                        ? -width - 30 - i * (width * 0.85)
                        : _virtualW + 30 + i * (width * 0.85);
                }
                SpawnChicken(x, direction, width, height);
            }

            Diag.Log("chickens spawned count=" + count + " total=" + _chickens.Count + " onScreen=" + onScreen);
        }

        private void SpawnChicken(double x, double direction, double width, double height)
        {
            Image img = new Image();
            img.Source = _chickenSource;
            img.Width = width;
            img.Height = height;
            img.Stretch = Stretch.Uniform;
            img.IsHitTestVisible = false;
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            ScaleTransform flip = new ScaleTransform(direction > 0 ? -1 : 1, 1);
            RotateTransform tilt = new RotateTransform(0);
            TransformGroup group = new TransformGroup();
            group.Children.Add(flip);
            group.Children.Add(tilt);
            img.RenderTransform = group;

            WalkingChicken ch = new WalkingChicken();
            ch.View = img;
            ch.Flip = flip;
            ch.Tilt = tilt;
            ch.W = width;
            ch.H = height;
            ch.X = x;
            ch.BaseY = Math.Max(0, _virtualH - height - (6 + _random.NextDouble() * 30));
            ch.Y = ch.BaseY;
            ch.Vx = direction * (42 + _random.NextDouble() * 36);
            ch.Phase = _random.NextDouble() * Math.PI * 2;
            ch.BornAt = _animalTime;
            ch.LifeSeconds = 20 + _random.NextDouble() * 14;

            Canvas.SetLeft(img, ch.X);
            Canvas.SetTop(img, ch.Y);
            Panel.SetZIndex(img, 20);
            _fxCanvas.Children.Add(img);
            _chickens.Add(ch);
            _chickensSpawned++;
            AwardAt("chicken2", _chickensSpawned, 2, "Куриный десант", "Куры выбежали");
            AwardAt("chicken12", _chickensSpawned, 12, "Птицефабрика", "12 кур за пранк");
        }

        private void UpdateAnimals(double dt)
        {
            _animalTime += dt;

            for (int i = _cats.Count - 1; i >= 0; i--)
            {
                FlyingCat cat = _cats[i];
                if (_animalTime >= cat.NextSpeedAt)
                {
                    cat.NextSpeedAt = _animalTime + 1.4 + _random.NextDouble() * 2.6;
                    cat.SpeedScale = 0.40 + _random.NextDouble() * 1.55;
                }

                cat.X += cat.Direction * cat.BaseSpeed * cat.SpeedScale * _animalBoost * dt;
                cat.Y = cat.BaseY + Math.Sin(_animalTime * 1.6 + cat.Phase) * 24;
                double maxY = Math.Max(0, _virtualH - cat.H - 4);
                if (cat.Y < 2)
                {
                    cat.Y = 2;
                }
                else if (cat.Y > maxY)
                {
                    cat.Y = maxY;
                }

                bool wrapped = false;
                if (cat.Direction > 0 && cat.X > _virtualW + 40)
                {
                    cat.X = -cat.W - 40;
                    wrapped = true;
                }
                else if (cat.Direction < 0 && cat.X + cat.W < -40)
                {
                    cat.X = _virtualW + 40;
                    wrapped = true;
                }
                if (wrapped)
                {
                    cat.Trail.Points.Clear();
                    cat.LastTrailX = double.NaN;
                    cat.LastTrailY = double.NaN;
                }

                Canvas.SetLeft(cat.View, cat.X);
                Canvas.SetTop(cat.View, cat.Y);
                cat.Flip.ScaleX = cat.Direction > 0 ? -1 : 1;
                cat.Tilt.Angle = Math.Sin(_animalTime * 2.1 + cat.Phase) * 4.5;

                double tailX = cat.X + cat.W * 0.5 - cat.Direction * cat.W * 0.42;
                double tailY = cat.Y + cat.H * 0.46;
                if (double.IsNaN(cat.LastTrailX)
                    || Math.Abs(tailX - cat.LastTrailX) + Math.Abs(tailY - cat.LastTrailY) > 7)
                {
                    cat.Trail.Points.Add(new Point(tailX, tailY));
                    cat.LastTrailX = tailX;
                    cat.LastTrailY = tailY;
                    while (cat.Trail.Points.Count > 40)
                    {
                        cat.Trail.Points.RemoveAt(0);
                    }
                }
            }

            for (int i = _chickens.Count - 1; i >= 0; i--)
            {
                WalkingChicken ch = _chickens[i];
                ch.X += ch.Vx * dt;
                ch.Y = ch.BaseY - Math.Abs(Math.Sin(_animalTime * 6.2 + ch.Phase)) * 5;
                Canvas.SetLeft(ch.View, ch.X);
                Canvas.SetTop(ch.View, ch.Y);
                ch.Flip.ScaleX = ch.Vx > 0 ? -1 : 1;
                ch.Tilt.Angle = Math.Sin(_animalTime * 8.5 + ch.Phase) * 2.2;

                double age = _animalTime - ch.BornAt;
                double left = ch.LifeSeconds - age;
                if (left < 0.8)
                {
                    ch.View.Opacity = Math.Max(0, left / 0.8);
                }

                bool offscreen = ch.Vx > 0
                    ? ch.X > _virtualW + ch.W
                    : ch.X + ch.W < -ch.W;
                if (left <= 0 || offscreen)
                {
                    _fxCanvas.Children.Remove(ch.View);
                    _chickens.RemoveAt(i);
                }
            }
        }

        private static string[] GetSfxResourceNames()
        {
            List<string> names = new List<string>();
            string[] all = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            for (int i = 0; i < all.Length; i++)
            {
                string name = all[i];
                if (name.StartsWith("sfx", StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(name);
                }
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names.ToArray();
        }

        private void StartSfx()
        {
            if (_preview || _smoke)
            {
                return;
            }

            _sfxNames = GetSfxResourceNames();
            if (_sfxNames.Length == 0)
            {
                Diag.Log("sfx: no embedded resources");
                return;
            }

            _sfxDir = IOPath.Combine(IOPath.GetTempPath(), SfxTempFolder);
            try
            {
                if (Directory.Exists(_sfxDir))
                {
                    Directory.Delete(_sfxDir, true);
                }
            }
            catch
            {
            }
            try
            {
                Directory.CreateDirectory(_sfxDir);
            }
            catch
            {
            }

            int extracted = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string name in _sfxNames)
            {
                try
                {
                    string target = IOPath.Combine(_sfxDir, name);
                    using (Stream s = asm.GetManifestResourceStream(name))
                    {
                        if (s == null)
                        {
                            continue;
                        }
                        using (FileStream fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            s.CopyTo(fs);
                        }
                    }
                    extracted++;
                }
                catch (Exception ex)
                {
                    Diag.Log("sfx extract fail " + name + ": " + ex.Message);
                }
            }
            Diag.Log("sfx ready: " + extracted + "/" + _sfxNames.Length + " in " + _sfxDir);

            ExtractCues();

            _sfxClock = 0;
            _nextSfxAt = 5 + _random.NextDouble() * 5;
            _sfxTimer = new DispatcherTimer();
            _sfxTimer.Interval = TimeSpan.FromMilliseconds(500);
            _sfxTimer.Tick += delegate { TickSfx(); };
            _sfxTimer.Start();
        }

        private void TickSfx()
        {
            if (_closing || !_locked)
            {
                return;
            }

            _sfxClock += 0.5;
            PruneSfx();

            if (_sfxClock < _nextSfxAt)
            {
                return;
            }
            _nextSfxAt = _sfxClock + 5 + _random.NextDouble() * 5;

            int active;
            lock (_sfxLock)
            {
                active = _sfxVoices.Count;
            }
            if (active >= MaxSfxVoices)
            {
                Diag.Log("sfx event skipped: active=" + active);
                return;
            }

            int wanted = RollSfxCount();
            int room = MaxSfxVoices - active;
            if (wanted > room)
            {
                wanted = room;
            }
            if (wanted <= 0)
            {
                return;
            }

            List<string> picks = PickSfx(wanted);
            if (picks.Count == 0)
            {
                return;
            }

            double[] volumes = new double[] { 0.85, 0.68, 0.55 };
            double gain = _loud ? 1.18 : 1.0;
            bool rooster = false;
            for (int i = 0; i < picks.Count; i++)
            {
                string name = picks[i];
                if (name.IndexOf("rooster", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    rooster = true;
                }
                double volume = volumes[Math.Min(i, volumes.Length - 1)] * gain;
                PlayOneSfx(name, Math.Min(1.0, volume));
            }

            _sfxEvents++;
            Diag.Log("sfx event #" + _sfxEvents + " count=" + picks.Count
                + " files=" + string.Join(",", picks.ToArray()) + " active=" + active);

            if (rooster)
            {
                SpawnChickenGroup(false);
            }
        }

        private int RollSfxCount()
        {
            double r = _random.NextDouble();
            if (r < 0.12)
            {
                return 3;
            }
            if (r < 0.38)
            {
                return 2;
            }
            return 1;
        }

        private void RefillSfxDeck()
        {
            _sfxDeck.Clear();
            for (int i = 0; i < _sfxNames.Length; i++)
            {
                _sfxDeck.Add(_sfxNames[i]);
            }

            for (int i = _sfxDeck.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                string tmp = _sfxDeck[i];
                _sfxDeck[i] = _sfxDeck[j];
                _sfxDeck[j] = tmp;
            }

            if (_sfxDeck.Count > 1 && _sfxDeck[_sfxDeck.Count - 1] == _lastSfxName)
            {
                int j = _random.Next(_sfxDeck.Count - 1);
                string tmp = _sfxDeck[_sfxDeck.Count - 1];
                _sfxDeck[_sfxDeck.Count - 1] = _sfxDeck[j];
                _sfxDeck[j] = tmp;
            }
        }

        private List<string> PickSfx(int count)
        {
            List<string> result = new List<string>();
            if (_sfxNames.Length == 0)
            {
                return result;
            }

            int guard = 0;
            while (result.Count < count && guard < 400)
            {
                guard++;
                if (_sfxDeck.Count == 0)
                {
                    RefillSfxDeck();
                }

                string candidate = _sfxDeck[_sfxDeck.Count - 1];
                _sfxDeck.RemoveAt(_sfxDeck.Count - 1);

                bool candidateRooster = candidate.IndexOf("rooster", StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasRooster = false;
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].IndexOf("rooster", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasRooster = true;
                        break;
                    }
                }

                if (candidateRooster && hasRooster)
                {
                    _sfxDeck.Insert(0, candidate);
                    continue;
                }

                result.Add(candidate);
                _lastSfxName = candidate;
            }
            return result;
        }

        private void PlayOneSfx(string fileName, double volume)
        {
            try
            {
                string path = IOPath.Combine(_sfxDir, fileName);
                MediaPlayer player = new MediaPlayer();
                player.Volume = volume;

                SfxVoice voice = new SfxVoice();
                voice.Player = player;
                voice.KillAt = DateTime.UtcNow.AddSeconds(30);

                player.MediaOpened += delegate
                {
                    try
                    {
                        if (!voice.Released)
                        {
                            player.Play();
                        }
                    }
                    catch
                    {
                    }
                };
                player.MediaEnded += delegate { ReleaseSfx(voice); };
                player.MediaFailed += delegate { ReleaseSfx(voice); };

                lock (_sfxLock)
                {
                    _sfxVoices.Add(voice);
                }
                player.Open(new Uri(path, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                Diag.Log("sfx play fail " + fileName + ": " + ex.Message);
            }
        }

        private void PruneSfx()
        {
            List<SfxVoice> expired = new List<SfxVoice>();
            lock (_sfxLock)
            {
                DateTime now = DateTime.UtcNow;
                for (int i = 0; i < _sfxVoices.Count; i++)
                {
                    if (now >= _sfxVoices[i].KillAt)
                    {
                        expired.Add(_sfxVoices[i]);
                    }
                }
            }
            for (int i = 0; i < expired.Count; i++)
            {
                ReleaseSfx(expired[i]);
            }
        }

        private void ReleaseSfx(SfxVoice voice)
        {
            if (voice == null)
            {
                return;
            }
            lock (_sfxLock)
            {
                if (voice.Released)
                {
                    return;
                }
                voice.Released = true;
                _sfxVoices.Remove(voice);
            }
            try
            {
                voice.Player.Stop();
            }
            catch
            {
            }
            try
            {
                voice.Player.Close();
            }
            catch
            {
            }
        }

        private void StopSfx()
        {
            try
            {
                if (_sfxTimer != null)
                {
                    _sfxTimer.Stop();
                }
            }
            catch
            {
            }
            _sfxTimer = null;

            List<SfxVoice> voices;
            lock (_sfxLock)
            {
                voices = new List<SfxVoice>(_sfxVoices);
            }
            for (int i = 0; i < voices.Count; i++)
            {
                ReleaseSfx(voices[i]);
            }

            try
            {
                if (!string.IsNullOrEmpty(_sfxDir) && Directory.Exists(_sfxDir))
                {
                    Directory.Delete(_sfxDir, true);
                    Diag.Log("sfx temp deleted");
                }
            }
            catch (Exception ex)
            {
                Diag.Log("sfx temp delete fail: " + ex.Message);
            }
        }

        private void SpawnInitialSprites(int count)
        {
            double[,] positions = new double[,]
            {
                { 0.82, 0.01 },
                { 0.015, 0.63 },
                { 0.82, 0.61 },
                { 0.00, 0.005 },
                { 0.43, 0.015 },
                { 0.20, 0.42 },
                { 0.64, 0.40 },
                { 0.35, 0.72 }
            };

            for (int i = 0; i < count; i++)
            {
                double fx = positions[i % positions.Length, 0];
                double fy = positions[i % positions.Length, 1];
                SpawnSpriteAt(fx, fy, 1.0);
            }
        }

        private void SpawnSprite(bool randomPosition)
        {
            if (randomPosition)
            {
                SpawnSpriteAt(_random.NextDouble() * 0.85, _random.NextDouble() * 0.8, 1.0);
            }
            else
            {
                SpawnSpriteAt(0.4, 0.4, 1.0);
            }
        }

        private void SpawnSpriteAt(double fx, double fy, double sizeScale)
        {
            if (_kfcSource == null)
            {
                _kfcSource = LoadBitmap("kfc.png");
                if (_kfcSource == null)
                {
                    return;
                }
            }

            double width = (82 + _random.NextDouble() * 62) * sizeScale;
            double height = width * _kfcSource.PixelHeight / _kfcSource.PixelWidth;
            if (height > 190)
            {
                height = 190;
                width = height * _kfcSource.PixelWidth / _kfcSource.PixelHeight;
            }

            Image img = new Image();
            img.Source = _kfcSource;
            img.Width = width;
            img.Height = height;
            img.Stretch = Stretch.Uniform;
            img.Opacity = 1.0;
            img.IsHitTestVisible = false;
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            RotateTransform rot = new RotateTransform(0);
            img.RenderTransform = rot;

            Sprite s = new Sprite();
            s.View = img;
            s.W = width;
            s.H = height;
            s.Radius = Math.Min(width, height) * 0.40;
            s.X = Math.Max(0, Math.Min(_virtualW - width, fx * _virtualW));
            s.Y = Math.Max(0, Math.Min(_virtualH - height, fy * _virtualH));
            s.Angle = _random.NextDouble() * 360;
            s.Spin = (_random.NextDouble() - 0.5) * 55;
            s.Rotation = rot;

            double speed = 38 + _random.NextDouble() * 70;
            double direction = _random.NextDouble() * Math.PI * 2;
            s.Vx = Math.Cos(direction) * speed;
            s.Vy = Math.Sin(direction) * speed;

            rot.Angle = s.Angle;
            Canvas.SetLeft(img, s.X);
            Canvas.SetTop(img, s.Y);
            _spriteCanvas.Children.Add(img);
            _sprites.Add(s);
        }

        private BitmapSource LoadBitmap(string resourceName)
        {
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (s == null)
                    {
                        return null;
                    }
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = s;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch
            {
                return null;
            }
        }

        private void SavePreviewAndClose()
        {
            if (_previewSaved)
            {
                return;
            }
            _previewSaved = true;
            if (_previewTimer != null)
            {
                _previewTimer.Stop();
            }

            try
            {
                _root.UpdateLayout();
                int w = (int)Math.Round(_root.ActualWidth > 0 ? _root.ActualWidth : _virtualW);
                int h = (int)Math.Round(_root.ActualHeight > 0 ? _root.ActualHeight : _virtualH);
                RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(_root);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                string path = IOPath.Combine(AppPaths.WorkDir, "preview.png");
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(fs);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(IOPath.Combine(AppPaths.WorkDir, "preview_error.txt"), ex.ToString());
                }
                catch
                {
                }
            }

            _allowClose = true;
            Cleanup();
            try
            {
                Close();
            }
            catch
            {
            }
            Application.Current.Shutdown();
        }

        private void InstallKeyboardHook()
        {
            if (_hookHandle != IntPtr.Zero || _preview)
            {
                return;
            }

            _hookProc = HookCallback;
            _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookProc, GetModuleHandle(null), 0);
        }

        private void UninstallKeyboardHook()
        {
            if (_hookHandle == IntPtr.Zero)
            {
                return;
            }
            try
            {
                UnhookWindowsHookEx(_hookHandle);
            }
            catch
            {
            }
            _hookHandle = IntPtr.Zero;
            _hookProc = null;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WmKeyDown || msg == WmSysKeyDown || msg == WmKeyUp || msg == WmSysKeyUp)
                {
                    KbdLlHookStruct info = (KbdLlHookStruct)Marshal.PtrToStructure(lParam, typeof(KbdLlHookStruct));
                    int vk = (int)info.VkCode;
                    bool isDown = msg == WmKeyDown || msg == WmSysKeyDown;

                    bool ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                    bool alt = (GetAsyncKeyState(0x12) & 0x8000) != 0;
                    bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0;
                    bool win = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;

                    if (ctrl && shift && vk == 0x51)
                    {
                        if (isDown)
                        {
                            Dispatcher.BeginInvoke(new Action(EmergencyExit));
                        }
                        return (IntPtr)1;
                    }

                    if (ctrl && shift && vk == 0x4C)
                    {
                        if (isDown)
                        {
                            Dispatcher.BeginInvoke(new Action(ToggleLoud));
                        }
                        return (IntPtr)1;
                    }

                    if (vk == 0x5B || vk == 0x5C)
                    {
                        return (IntPtr)1;
                    }

                    if (win)
                    {
                        return (IntPtr)1;
                    }

                    if (alt && (vk == 0x09 || vk == 0x1B || vk == 0x73 || vk == 0x20))
                    {
                        if (isDown && vk == 0x73)
                        {
                            _closeAttempts++;
                            _altF4++;
                            if (_altF4 >= 1)
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    UnlockAchievement("altf4", "Alt+F4", "Классика жанра");
                                }));
                            }
                        }
                        return (IntPtr)1;
                    }

                    if (ctrl && (vk == 0x1B || (shift && vk == 0x1B)))
                    {
                        return (IntPtr)1;
                    }

                    if (ctrl && alt && vk == 0x2E)
                    {
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private void EmergencyExit()
        {
            if (_closing)
            {
                return;
            }
            ClosePrank();
        }

        private void BuildEffectLayers()
        {
            _discoLayer = new Canvas();
            _discoLayer.Width = _virtualW;
            _discoLayer.Height = _virtualH;
            _discoLayer.IsHitTestVisible = false;
            _discoLayer.Visibility = Visibility.Collapsed;
            _discoRect = new Rectangle();
            _discoRect.Width = _virtualW;
            _discoRect.Height = _virtualH;
            _discoRect.Opacity = 0.30;
            _discoRect.IsHitTestVisible = false;
            _discoLayer.Children.Add(_discoRect);
            Panel.SetZIndex(_discoLayer, 90);
            _root.Children.Add(_discoLayer);

            _dangerLayer = new Canvas();
            _dangerLayer.Width = _virtualW;
            _dangerLayer.Height = _virtualH;
            _dangerLayer.IsHitTestVisible = false;
            _dangerRect = new Rectangle();
            _dangerRect.Width = _virtualW;
            _dangerRect.Height = _virtualH;
            _dangerRect.Fill = new SolidColorBrush(Color.FromRgb(255, 26, 26));
            _dangerRect.Opacity = 0;
            _dangerRect.IsHitTestVisible = false;
            _dangerLayer.Children.Add(_dangerRect);
            Panel.SetZIndex(_dangerLayer, 96);
            _root.Children.Add(_dangerLayer);

            _crackLayer = new Canvas();
            _crackLayer.Width = _virtualW;
            _crackLayer.Height = _virtualH;
            _crackLayer.IsHitTestVisible = false;
            Panel.SetZIndex(_crackLayer, 200);
            _root.Children.Add(_crackLayer);

            _toastLayer = new Canvas();
            _toastLayer.Width = _virtualW;
            _toastLayer.Height = _virtualH;
            _toastLayer.IsHitTestVisible = true;
            Panel.SetZIndex(_toastLayer, 220);
            _root.Children.Add(_toastLayer);
        }

        private void OnRootMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_locked || _preview || _closing || _captchaLayer != null)
            {
                return;
            }

            Point p = e.GetPosition(_root);
            for (int i = _cats.Count - 1; i >= 0; i--)
            {
                FlyingCat cat = _cats[i];
                if (p.X >= cat.X && p.X <= cat.X + cat.W && p.Y >= cat.Y && p.Y <= cat.Y + cat.H)
                {
                    OnCatClicked(cat);
                    e.Handled = true;
                    return;
                }
            }

            for (int i = _sprites.Count - 1; i >= 0; i--)
            {
                Sprite s = _sprites[i];
                double cx = s.X + s.W * 0.5;
                double cy = s.Y + s.H * 0.5;
                double dx = p.X - cx;
                double dy = p.Y - cy;
                double reach = Math.Max(s.W, s.H) * 0.52;
                if (dx * dx + dy * dy <= reach * reach)
                {
                    BurstWings(s);
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnCatClicked(FlyingCat cat)
        {
            _catPets++;
            PlayCue("sfx07.mp3", 1.0);
            AwardAt("cat1", _catPets, 1, "Мяу", "Кот поглажен");
            AwardAt("cat5", _catPets, 5, "Кошатник", "Поглажено 5 котов");
            AwardAt("cat20", _catPets, 20, "Мяу-маньяк", "Поглажено 20 котов");
            _catsPetted.Add(cat);
            AwardAt("cat3", _catsPetted.Count, 3, "Все свои", "Поглажены все три кота");
        }

        private void BurstWings(Sprite s)
        {
            if (_preview || _fxCanvas == null)
            {
                return;
            }
            if (_wingSource == null)
            {
                _wingSource = LoadBitmap("wing.png");
                if (_wingSource == null)
                {
                    return;
                }
            }

            int count = 4 + _random.Next(3);
            double cx = s.X + s.W * 0.5;
            double cy = s.Y + s.H * 0.5;
            double rad = s.Angle * Math.PI / 180.0;
            double rimX = cx + Math.Sin(rad) * (s.H * 0.42);
            double rimY = cy - Math.Cos(rad) * (s.H * 0.42);
            for (int i = 0; i < count; i++)
            {
                double width = 34.0 + _random.NextDouble() * 26.0;
                double height = width * _wingSource.PixelHeight / (double)_wingSource.PixelWidth;

                Image img = new Image();
                img.Source = _wingSource;
                img.Width = width;
                img.Height = height;
                img.Stretch = Stretch.Uniform;
                img.IsHitTestVisible = false;
                img.RenderTransformOrigin = new Point(0.5, 0.5);

                RotateTransform rot = new RotateTransform(_random.NextDouble() * 360.0);
                ScaleTransform flip = new ScaleTransform(_random.NextDouble() < 0.5 ? -1.0 : 1.0, 1.0);
                TransformGroup group = new TransformGroup();
                group.Children.Add(flip);
                group.Children.Add(rot);
                img.RenderTransform = group;

                double along = (_random.NextDouble() - 0.5) * s.W * 0.5;
                double startX = rimX + Math.Cos(rad) * along - width * 0.5;
                double startY = rimY + Math.Sin(rad) * along - height * 0.5;

                Wing wing = new Wing();
                wing.View = img;
                wing.W = width;
                wing.H = height;
                wing.X = startX;
                wing.Y = startY;

                double direction = -Math.PI * 0.5 + (_random.NextDouble() - 0.5) * 1.1;
                double speed = 230.0 + _random.NextDouble() * 150.0;
                wing.Vx = Math.Cos(direction) * speed + s.Vx * 0.3 + along * 0.8;
                wing.Vy = Math.Sin(direction) * speed + s.Vy * 0.3;
                wing.Angle = rot.Angle;
                wing.Spin = (_random.NextDouble() - 0.5) * 640.0;
                wing.Rotation = rot;
                wing.BornAt = _animalTime;
                wing.LifeSeconds = 1.5 + _random.NextDouble() * 0.9;

                Canvas.SetLeft(img, wing.X);
                Canvas.SetTop(img, wing.Y);
                _fxCanvas.Children.Add(img);
                _wings.Add(wing);
            }
            Diag.Log("bucket wings count=" + count);
            PlayCue("cue_crunch.mp3", 1.0);
            _bucketHits++;
            AwardAt("bucket1", _bucketHits, 1, "Крылья!", "Ведро нажато");
            AwardAt("bucket10", _bucketHits, 10, "Куриная фабрика", "Разбито 10 вёдер");
            AwardAt("bucket30", _bucketHits, 30, "Крылья, ноги и хвосты", "Разбито 30 вёдер");
        }

        private void UpdateWings(double dt)
        {
            for (int i = _wings.Count - 1; i >= 0; i--)
            {
                Wing w = _wings[i];
                double age = _animalTime - w.BornAt;
                if (age >= w.LifeSeconds)
                {
                    if (_fxCanvas != null)
                    {
                        _fxCanvas.Children.Remove(w.View);
                    }
                    _wings.RemoveAt(i);
                    continue;
                }

                w.Vy += 1250.0 * dt;
                w.Vx *= (1.0 - 0.55 * dt);
                w.X += w.Vx * dt;
                w.Y += w.Vy * dt;
                w.Angle += w.Spin * dt;
                Canvas.SetLeft(w.View, w.X);
                Canvas.SetTop(w.View, w.Y);
                w.Rotation.Angle = w.Angle;

                double fadeStart = w.LifeSeconds * 0.55;
                w.View.Opacity = age < fadeStart ? 1.0 : Math.Max(0.0, 1.0 - (age - fadeStart) / (w.LifeSeconds - fadeStart));
            }
        }

        private void RegisterWrongAttempt()
        {
            if (_preview || _smoke)
            {
                return;
            }

            _wrongAttempts++;
            Diag.Log("wrong attempt #" + _wrongAttempts);
            AwardAt("wrong1", _wrongAttempts, 1, "Не угадал", "Первая неверная попытка");
            AwardAt("wrong3", _wrongAttempts, 3, "Трещина", "3 неверных кода");
            AwardAt("wrong5", _wrongAttempts, 5, "Осколки", "5 неверных кодов");
            AwardAt("wrong10", _wrongAttempts, 10, "Это уже личное", "10 неверных кодов");
            PlayCue("cue_glass.mp3", 1.0);
            ShakeScreen();
            ShowCracks();

            if (_wrongAttempts == 3)
            {
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(420);
                timer.Tick += delegate
                {
                    timer.Stop();
                    PlayCue("sfx08.mp3", 1.0);
                };
                timer.Start();
            }
        }

        private void ShakeScreen()
        {
            if (_viewbox == null || _shakeTransform == null)
            {
                return;
            }
            if (_shakeTimer != null)
            {
                _shakeTimer.Stop();
            }
            _shakeStart = DateTime.UtcNow;
            _shakeTimer = new DispatcherTimer();
            _shakeTimer.Interval = TimeSpan.FromMilliseconds(16);
            _shakeTimer.Tick += delegate
            {
                double ms = (DateTime.UtcNow - _shakeStart).TotalMilliseconds;
                if (ms >= 460)
                {
                    _shakeTimer.Stop();
                    _shakeTransform.X = 0;
                    _shakeTransform.Y = 0;
                    return;
                }
                double fade = 1.0 - ms / 460.0;
                _shakeTransform.X = (_random.NextDouble() - 0.5) * 36.0 * fade;
                _shakeTransform.Y = (_random.NextDouble() - 0.5) * 28.0 * fade;
            };
            _shakeTimer.Start();
        }

        private void ShowCracks()
        {
            if (_crackLayer == null)
            {
                return;
            }

            _crackLayer.Children.Clear();
            _crackLayer.Opacity = 1.0;

            double cx = _virtualW * (0.25 + _random.NextDouble() * 0.5);
            double cy = _virtualH * (0.18 + _random.NextDouble() * 0.55);

            SolidColorBrush crack = new SolidColorBrush(Color.FromArgb(235, 250, 252, 255));
            crack.Freeze();

            for (int i = 0; i < 11; i++)
            {
                double angle = (i / 11.0) * Math.PI * 2.0 + _random.NextDouble() * 0.25;
                Polyline line = new Polyline();
                line.Stroke = crack;
                line.StrokeThickness = 1.8;
                line.StrokeLineJoin = PenLineJoin.Round;
                line.IsHitTestVisible = false;

                double x = cx;
                double y = cy;
                line.Points.Add(new Point(x, y));
                int segments = 3 + _random.Next(3);
                for (int s = 0; s < segments; s++)
                {
                    double len = (46 + _random.NextDouble() * 120) * (1.0 + s * 0.35);
                    x += Math.Cos(angle) * len;
                    y += Math.Sin(angle) * len;
                    angle += (_random.NextDouble() - 0.5) * 0.55;
                    line.Points.Add(new Point(x, y));
                }
                _crackLayer.Children.Add(line);
            }

            double opacity = 1.0;
            DispatcherTimer fade = new DispatcherTimer();
            fade.Interval = TimeSpan.FromMilliseconds(45);
            fade.Tick += delegate
            {
                opacity -= 0.075;
                if (opacity <= 0)
                {
                    fade.Stop();
                    _crackLayer.Children.Clear();
                    _crackLayer.Opacity = 1.0;
                    return;
                }
                _crackLayer.Opacity = opacity;
            };
            fade.Start();
        }

        private void UpdateKfcCombo(Key key)
        {
            DateTime now = DateTime.UtcNow;
            if (key == Key.K)
            {
                _comboStep = 1;
                _comboAt = now;
                return;
            }
            if (key == Key.F && _comboStep == 1 && (now - _comboAt).TotalMilliseconds < 1200)
            {
                _comboStep = 2;
                _comboAt = now;
                return;
            }
            if (key == Key.C && _comboStep == 2 && (now - _comboAt).TotalMilliseconds < 1200)
            {
                _comboStep = 0;
                ToggleDisco();
                return;
            }
            if (key != Key.F && key != Key.C)
            {
                _comboStep = 0;
            }
        }

        private void ToggleDisco()
        {
            if (!_locked || _closing || _preview || _smoke)
            {
                return;
            }

            _disco = !_disco;
            Diag.Log("disco=" + _disco);
            if (_disco)
            {
                _loudBeforeDisco = _loud;
                _loud = true;
                _animalBoost = 1.85;
                _discoHue = 0;
                TickDisco();
                if (_discoLayer != null)
                {
                    _discoLayer.Visibility = Visibility.Visible;
                }
                if (_discoTimer == null)
                {
                    _discoTimer = new DispatcherTimer();
                    _discoTimer.Interval = TimeSpan.FromMilliseconds(110);
                    _discoTimer.Tick += delegate { TickDisco(); };
                }
                _discoTimer.Start();
                StopMusic();
                StartMusic();
                UnlockAchievement("disco", "ДИСКОТЕКА", "Секретная комбинация найдена");
                _discoToggles++;
                AwardAt("disco2", _discoToggles, 2, "Снова в клуб", "Дискотека включена дважды");
            }
            else
            {
                _loud = _loudBeforeDisco;
                _animalBoost = 1.0;
                if (_discoTimer != null)
                {
                    _discoTimer.Stop();
                }
                if (_discoLayer != null)
                {
                    _discoLayer.Visibility = Visibility.Collapsed;
                }
                StopMusic();
                StartMusic();
            }
        }

        private void TickDisco()
        {
            if (_discoRect == null)
            {
                return;
            }
            _discoHue = (_discoHue + 11.0) % 360.0;
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);
            for (int i = 0; i <= 6; i++)
            {
                double hue = (_discoHue + i * (360.0 / 7.0)) % 360.0;
                brush.GradientStops.Add(new GradientStop(HsvToColor(hue, 0.85, 1.0), i / 6.0));
            }
            _discoRect.Fill = brush;
        }

        private static Color HsvToColor(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double h = hue / 60.0;
            double x = c * (1.0 - Math.Abs(h % 2.0 - 1.0));
            double r = 0;
            double g = 0;
            double b = 0;
            if (h < 1) { r = c; g = x; }
            else if (h < 2) { r = x; g = c; }
            else if (h < 3) { g = c; b = x; }
            else if (h < 4) { g = x; b = c; }
            else if (h < 5) { r = x; b = c; }
            else { r = c; b = x; }
            double m = value - c;
            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255.0),
                (byte)Math.Round((g + m) * 255.0),
                (byte)Math.Round((b + m) * 255.0));
        }

        private void StartDangerMode()
        {
            if (_dangerStarted || _preview || _smoke)
            {
                return;
            }
            _dangerStarted = true;
            _dangerClock = 0;
            Diag.Log("danger mode started");
            UnlockAchievement("danger", "Красная зона", "Осталось меньше 30 секунд");
            if (_dangerLayer != null)
            {
                _dangerLayer.Visibility = Visibility.Visible;
            }
            _heartbeatCue = PlayCue("cue_heartbeat.mp3", 0.55, true);
            if (_timerRun != null)
            {
                _timerRun.Foreground = new SolidColorBrush(Color.FromRgb(255, 64, 64));
            }
        }

        private void UpdateDangerPulse(double dt)
        {
            if (!_dangerStarted || _dangerRect == null)
            {
                return;
            }
            _dangerClock += dt;
            _dangerRect.Opacity = 0.10 + 0.16 * (0.5 + 0.5 * Math.Sin(_dangerClock * 5.6));
        }

        private void StartAtmosphere()
        {
            if (_preview || _smoke)
            {
                return;
            }
            StartAchievements();
        }

        private void StopAtmosphere()
        {
            try { if (_shakeTimer != null) _shakeTimer.Stop(); } catch { }
            try { if (_discoTimer != null) _discoTimer.Stop(); } catch { }
            try { if (_achTimer != null) _achTimer.Stop(); } catch { }

            if (_shakeTransform != null)
            {
                _shakeTransform.X = 0;
                _shakeTransform.Y = 0;
            }
            if (_discoLayer != null)
            {
                _discoLayer.Visibility = Visibility.Collapsed;
            }
            if (_dangerLayer != null)
            {
                _dangerLayer.Visibility = Visibility.Collapsed;
            }
            if (_dangerRect != null)
            {
                _dangerRect.Opacity = 0;
            }
            _dangerStarted = false;
            if (_crackLayer != null)
            {
                _crackLayer.Children.Clear();
                _crackLayer.Opacity = 1.0;
            }
            if (_toastLayer != null)
            {
                _toastLayer.Children.Clear();
            }
            _toastToken++;
            if (_wings.Count > 0)
            {
                for (int i = 0; i < _wings.Count; i++)
                {
                    if (_fxCanvas != null)
                    {
                        _fxCanvas.Children.Remove(_wings[i].View);
                    }
                }
                _wings.Clear();
            }
            _achQueue.Clear();
            _toastBusy = false;
            _disco = false;
            _animalBoost = 1.0;
            StopCues();
        }

        private void StartAchievements()
        {
            if (_achTimer != null)
            {
                return;
            }
            _achTimer = new DispatcherTimer();
            _achTimer.Interval = TimeSpan.FromMilliseconds(500);
            _achTimer.Tick += delegate { TickAchievements(); };
            _achTimer.Start();
        }

        private void TickAchievements()
        {
            if (!_locked || _closing)
            {
                return;
            }
            int elapsed = (int)(DateTime.UtcNow - _startTime).TotalSeconds;
            AwardAt("min1", elapsed, 60, "Первая минута", "Осталось ещё семь");
            AwardAt("min3", elapsed, 180, "Терпение", "Три минуты позади");
            AwardAt("five", elapsed, 300, "5 минут без паники", "Ты всё ещё здесь");
            AwardAt("min7", elapsed, 420, "Финишная прямая", "Семь минут. Держись");
            AwardAt("caps", _capsPresses, 12, "Нажал Caps Lock 12 раз", "Зачем?");
            AwardAt("close", _closeAttempts, 8, "Пытался закрыть окно 8 раз", "Упорство");
            AwardAt("close20", _closeAttempts, 20, "Не сдаёшься", "20 попыток закрыть");
            AwardAt("collision50", _collisions, 50, "Ведряной бильярд", "50 столкновений");
            AwardAt("collision200", _collisions, 200, "Физика вне закона", "200 столкновений");
            AwardAt("sfx10", _sfxEvents, 10, "Шумный сосед", "10 звуковых событий");
            AwardAt("sfx40", _sfxEvents, 40, "Без тишины", "40 звуковых событий");
        }

        private void AwardAt(string id, int value, int threshold, string title, string description)
        {
            if (value >= threshold)
            {
                UnlockAchievement(id, title, description);
            }
        }

        private void UnlockAchievement(string id, string title, string description)
        {
            if (_preview || _smoke || _closing)
            {
                return;
            }
            if (!_achDone.Add(id))
            {
                return;
            }
            Diag.Log("achievement unlocked: " + id);
            Achievement achievement = new Achievement();
            achievement.Id = id;
            achievement.Title = title;
            achievement.Description = description;
            _achQueue.Enqueue(achievement);
            if (!_toastBusy)
            {
                ShowNextToast();
            }
        }

        private void ShowNextToast()
        {
            if (_toastLayer == null || _achQueue.Count == 0)
            {
                _toastBusy = false;
                return;
            }

            _toastBusy = true;
            int token = ++_toastToken;
            Achievement achievement = _achQueue.Dequeue();
            Diag.Log("toast show: " + achievement.Id + " queue=" + _achQueue.Count);

            double width = 372.0;
            double height = 96.0;
            double boxHeight = height + 16.0;
            double left = _primaryX + Math.Max(10.0, _primaryW - width - 22.0);
            double top = _primaryY + Math.Max(10.0, _primaryH - boxHeight - 26.0);

            Canvas box = null;
            Border card = BuildToastCard(achievement, delegate { CloseToast(box); });

            box = new Canvas();
            box.Width = width;
            box.Height = boxHeight;
            Canvas.SetLeft(box, left);
            Canvas.SetTop(box, top);
            box.Effect = Shadow(16, 3, 0.38);

            Polygon tail = new Polygon();
            tail.Points.Add(new Point(width - 56.0, height - 1.0));
            tail.Points.Add(new Point(width - 30.0, height - 1.0));
            tail.Points.Add(new Point(width - 30.0, height + 16.0));
            tail.Fill = new SolidColorBrush(Color.FromRgb(227, 239, 250));
            tail.Stroke = new SolidColorBrush(Color.FromArgb(70, 120, 148, 173));
            tail.StrokeThickness = 1.0;
            tail.StrokeLineJoin = PenLineJoin.Round;
            tail.IsHitTestVisible = false;
            box.Children.Add(tail);

            Canvas.SetLeft(card, 0);
            Canvas.SetTop(card, 0);
            box.Children.Add(card);

            TranslateTransform slide = new TranslateTransform(width + 70.0, 0);
            box.RenderTransform = slide;
            _toastLayer.Children.Add(box);
            PlayCue("cue_achievement.mp3", 1.0);

            int frames = 0;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += delegate
            {
                if (token != _toastToken)
                {
                    timer.Stop();
                    return;
                }
                frames++;
                if (frames >= 16)
                {
                    timer.Stop();
                    slide.X = 0;
                    DispatcherTimer hold = new DispatcherTimer();
                    hold.Interval = TimeSpan.FromMilliseconds(3600);
                    hold.Tick += delegate
                    {
                        hold.Stop();
                        if (token != _toastToken)
                        {
                            return;
                        }
                        FinishToast(box, slide, width, token);
                    };
                    hold.Start();
                    return;
                }
                slide.X = (width + 70.0) * (1.0 - frames / 16.0);
            };
            timer.Start();
        }

        private void CloseToast(Canvas box)
        {
            _toastToken++;
            if (_toastLayer != null && box != null)
            {
                _toastLayer.Children.Remove(box);
            }
            _toastBusy = false;
            ShowNextToast();
        }

        private void FinishToast(Canvas box, TranslateTransform slide, double width, int token)
        {
            int frames = 0;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += delegate
            {
                if (token != _toastToken)
                {
                    timer.Stop();
                    return;
                }
                frames++;
                if (frames >= 12)
                {
                    timer.Stop();
                    if (_toastLayer != null)
                    {
                        _toastLayer.Children.Remove(box);
                    }
                    ShowNextToast();
                    return;
                }
                slide.X = (width + 70.0) * (frames / 12.0);
            };
            timer.Start();
        }

        private Border BuildToastCard(Achievement achievement, MouseButtonEventHandler onClose)
        {
            Border card = new Border();
            card.Width = 372;
            card.Height = 96;
            card.CornerRadius = new CornerRadius(5);
            card.Background = Grad(Color.FromRgb(253, 254, 255), Color.FromRgb(227, 239, 250), 90);
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(140, 166, 191));
            card.BorderThickness = new Thickness(1);

            Grid root = new Grid();

            Border gloss = new Border();
            gloss.Height = 44;
            gloss.VerticalAlignment = VerticalAlignment.Top;
            gloss.CornerRadius = new CornerRadius(5, 5, 0, 0);
            gloss.Background = Grad(Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90);
            gloss.IsHitTestVisible = false;
            root.Children.Add(gloss);

            Grid grid = new Grid();
            grid.Margin = new Thickness(12, 10, 26, 10);
            ColumnDefinition iconColumn = new ColumnDefinition();
            iconColumn.Width = new GridLength(46);
            ColumnDefinition textColumn = new ColumnDefinition();
            grid.ColumnDefinitions.Add(iconColumn);
            grid.ColumnDefinitions.Add(textColumn);

            if (_kfcSource == null)
            {
                _kfcSource = LoadBitmap("kfc.png");
            }
            if (_kfcSource != null)
            {
                Image icon = new Image();
                icon.Source = _kfcSource;
                icon.Stretch = Stretch.Uniform;
                icon.Width = 38;
                icon.Height = 38;
                icon.HorizontalAlignment = HorizontalAlignment.Left;
                icon.VerticalAlignment = VerticalAlignment.Center;
                icon.IsHitTestVisible = false;
                Grid.SetColumn(icon, 0);
                grid.Children.Add(icon);
            }

            StackPanel text = new StackPanel();
            text.Margin = new Thickness(8, 0, 0, 0);
            text.VerticalAlignment = VerticalAlignment.Center;

            TextBlock title = new TextBlock();
            title.Text = achievement.Title;
            title.FontFamily = new FontFamily("Segoe UI");
            title.FontSize = 12.5;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            title.TextTrimming = TextTrimming.CharacterEllipsis;
            text.Children.Add(title);

            TextBlock description = new TextBlock();
            description.Text = achievement.Description;
            description.FontFamily = new FontFamily("Segoe UI");
            description.FontSize = 11.5;
            description.Foreground = new SolidColorBrush(Color.FromRgb(62, 78, 94));
            description.TextTrimming = TextTrimming.CharacterEllipsis;
            description.Margin = new Thickness(0, 3, 0, 0);
            text.Children.Add(description);

            Grid.SetColumn(text, 1);
            grid.Children.Add(text);
            root.Children.Add(grid);

            Border inner = new Border();
            inner.Margin = new Thickness(1);
            inner.CornerRadius = new CornerRadius(4);
            inner.BorderBrush = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255));
            inner.BorderThickness = new Thickness(1);
            inner.IsHitTestVisible = false;
            root.Children.Add(inner);

            SolidColorBrush closeInk = new SolidColorBrush(Color.FromRgb(90, 106, 122));
            Border closeBox = new Border();
            closeBox.Width = 18;
            closeBox.Height = 18;
            closeBox.CornerRadius = new CornerRadius(2);
            closeBox.HorizontalAlignment = HorizontalAlignment.Right;
            closeBox.VerticalAlignment = VerticalAlignment.Top;
            closeBox.Margin = new Thickness(0, 3, 3, 0);
            closeBox.Background = Brushes.Transparent;
            TextBlock closeGlyph = new TextBlock();
            closeGlyph.Text = "×";
            closeGlyph.FontFamily = new FontFamily("Segoe UI");
            closeGlyph.FontSize = 12;
            closeGlyph.Foreground = closeInk;
            closeGlyph.HorizontalAlignment = HorizontalAlignment.Center;
            closeGlyph.VerticalAlignment = VerticalAlignment.Center;
            closeBox.Child = closeGlyph;
            closeBox.MouseEnter += delegate
            {
                closeBox.Background = new SolidColorBrush(Color.FromRgb(206, 60, 60));
                closeGlyph.Foreground = Brushes.White;
            };
            closeBox.MouseLeave += delegate
            {
                closeBox.Background = Brushes.Transparent;
                closeGlyph.Foreground = closeInk;
            };
            closeBox.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs args)
            {
                args.Handled = true;
                if (onClose != null)
                {
                    onClose(s, args);
                }
            };
            root.Children.Add(closeBox);

            card.Child = root;
            return card;
        }

        private void BeginCaptcha()
        {
            Diag.Log("captcha begin smoke=" + _smoke + " existing=" + (_captchaLayer != null));
            if (_smoke)
            {
                FinishUnlock();
                return;
            }
            if (_captchaLayer != null)
            {
                return;
            }
            _captchaFails = 0;
            BuildCaptcha();
        }

        private void BuildCaptcha()
        {
            Canvas layer = new Canvas();
            layer.Width = _virtualW;
            layer.Height = _virtualH;
            layer.Background = new SolidColorBrush(Color.FromArgb(165, 3, 16, 44));
            Panel.SetZIndex(layer, 250);

            double cardWidth = 560.0;
            double cardHeight = 670.0;

            Border card = new Border();
            card.Width = cardWidth;
            card.CornerRadius = new CornerRadius(6);
            card.Background = Grad(Color.FromRgb(248, 251, 255), Color.FromRgb(231, 241, 250), 90);
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(124, 156, 185));
            card.BorderThickness = new Thickness(1);
            card.Effect = Shadow(20, 4, 0.5);
            Canvas.SetLeft(card, _primaryX + Math.Max(8.0, (_primaryW - cardWidth) / 2.0));
            Canvas.SetTop(card, _primaryY + Math.Max(8.0, (_primaryH - cardHeight) / 2.0));
            layer.Children.Add(card);

            Grid shell = new Grid();
            RowDefinition barRow = new RowDefinition();
            barRow.Height = new GridLength(36);
            RowDefinition bodyRow = new RowDefinition();
            bodyRow.Height = GridLength.Auto;
            RowDefinition footerRow = new RowDefinition();
            footerRow.Height = new GridLength(52);
            shell.RowDefinitions.Add(barRow);
            shell.RowDefinitions.Add(bodyRow);
            shell.RowDefinitions.Add(footerRow);
            card.Child = shell;

            Border bar = new Border();
            bar.Background = Grad(Color.FromRgb(237, 246, 254), Color.FromRgb(201, 223, 245), 90);
            bar.BorderBrush = new SolidColorBrush(Color.FromRgb(168, 194, 219));
            bar.BorderThickness = new Thickness(0, 0, 0, 1);
            bar.CornerRadius = new CornerRadius(6, 6, 0, 0);
            Grid.SetRow(bar, 0);
            shell.Children.Add(bar);

            Grid barGrid = new Grid();
            barGrid.Margin = new Thickness(10, 0, 8, 0);
            ColumnDefinition iconCol = new ColumnDefinition();
            iconCol.Width = new GridLength(22);
            ColumnDefinition nameCol = new ColumnDefinition();
            ColumnDefinition closeCol = new ColumnDefinition();
            closeCol.Width = new GridLength(30);
            barGrid.ColumnDefinitions.Add(iconCol);
            barGrid.ColumnDefinitions.Add(nameCol);
            barGrid.ColumnDefinitions.Add(closeCol);
            bar.Child = barGrid;

            if (_kfcSource == null)
            {
                _kfcSource = LoadBitmap("kfc.png");
            }
            Border titleIcon = new Border();
            titleIcon.Width = 16;
            titleIcon.Height = 16;
            titleIcon.VerticalAlignment = VerticalAlignment.Center;
            if (_kfcSource != null)
            {
                Image icon = new Image();
                icon.Source = _kfcSource;
                icon.Stretch = Stretch.Uniform;
                titleIcon.Child = icon;
            }
            Grid.SetColumn(titleIcon, 0);
            barGrid.Children.Add(titleIcon);

            TextBlock barTitle = new TextBlock();
            barTitle.Text = "Подтвердите, что вы человек";
            barTitle.FontFamily = new FontFamily("Segoe UI");
            barTitle.FontSize = 12.5;
            barTitle.FontWeight = FontWeights.SemiBold;
            barTitle.Foreground = new SolidColorBrush(Color.FromRgb(25, 61, 99));
            barTitle.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(barTitle, 1);
            barGrid.Children.Add(barTitle);

            SolidColorBrush closeInk = new SolidColorBrush(Color.FromRgb(45, 84, 121));
            Border closeBox = new Border();
            closeBox.Width = 22;
            closeBox.Height = 17;
            closeBox.CornerRadius = new CornerRadius(2);
            closeBox.VerticalAlignment = VerticalAlignment.Center;
            closeBox.HorizontalAlignment = HorizontalAlignment.Right;
            closeBox.Background = Brushes.Transparent;
            closeBox.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            closeBox.BorderThickness = new Thickness(1);
            TextBlock closeGlyph = new TextBlock();
            closeGlyph.Text = "×";
            closeGlyph.FontFamily = new FontFamily("Segoe UI");
            closeGlyph.FontSize = 14;
            closeGlyph.Foreground = closeInk;
            closeGlyph.HorizontalAlignment = HorizontalAlignment.Center;
            closeGlyph.VerticalAlignment = VerticalAlignment.Center;
            closeBox.Child = closeGlyph;
            closeBox.MouseEnter += delegate
            {
                closeBox.Background = new SolidColorBrush(Color.FromRgb(206, 60, 60));
                closeBox.BorderBrush = new SolidColorBrush(Color.FromRgb(170, 40, 40));
                closeGlyph.Foreground = Brushes.White;
            };
            closeBox.MouseLeave += delegate
            {
                closeBox.Background = Brushes.Transparent;
                closeBox.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                closeGlyph.Foreground = closeInk;
            };
            closeBox.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs args)
            {
                args.Handled = true;
                if (_captchaStatus != null)
                {
                    _captchaStatus.Foreground = new SolidColorBrush(Color.FromRgb(178, 40, 40));
                    _captchaStatus.Text = "Эту капчу нельзя закрыть. Выберите котов.";
                }
            };
            Grid.SetColumn(closeBox, 2);
            barGrid.Children.Add(closeBox);

            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(22, 16, 22, 12);
            Grid.SetRow(panel, 1);
            shell.Children.Add(panel);

            TextBlock question = new TextBlock();
            question.Text = "Выберите все квадратики с британскими вислоухими";
            question.FontFamily = new FontFamily("Segoe UI");
            question.FontSize = 13;
            question.Foreground = new SolidColorBrush(Color.FromRgb(27, 27, 27));
            panel.Children.Add(question);

            Border box = new Border();
            box.Margin = new Thickness(0, 12, 0, 0);
            box.Padding = new Thickness(3);
            box.Background = new SolidColorBrush(Color.FromRgb(243, 247, 251));
            box.BorderBrush = new SolidColorBrush(Color.FromRgb(175, 196, 216));
            box.BorderThickness = new Thickness(1);
            box.CornerRadius = new CornerRadius(2);
            _captchaGrid = new UniformGrid();
            _captchaGrid.Rows = 3;
            _captchaGrid.Columns = 3;
            box.Child = _captchaGrid;
            panel.Children.Add(box);

            _captchaStatus = new TextBlock();
            _captchaStatus.FontFamily = new FontFamily("Segoe UI");
            _captchaStatus.FontSize = 12;
            _captchaStatus.Margin = new Thickness(0, 8, 0, 0);
            _captchaStatus.Foreground = new SolidColorBrush(Color.FromRgb(178, 40, 40));
            _captchaStatus.Text = "";
            panel.Children.Add(_captchaStatus);

            Border footer = new Border();
            footer.Background = Grad(Color.FromRgb(248, 251, 255), Color.FromRgb(237, 244, 250), 90);
            footer.BorderBrush = new SolidColorBrush(Color.FromRgb(210, 223, 234));
            footer.BorderThickness = new Thickness(0, 1, 0, 0);
            footer.CornerRadius = new CornerRadius(0, 0, 6, 6);
            Grid.SetRow(footer, 2);
            shell.Children.Add(footer);

            Button check = MakeAeroButton("Проверить", 132, 30);
            check.HorizontalAlignment = HorizontalAlignment.Right;
            check.VerticalAlignment = VerticalAlignment.Center;
            check.Margin = new Thickness(0, 0, 16, 0);
            check.Click += delegate { CaptchaCheck(); };
            footer.Child = check;

            _captchaSelected.Clear();
            _captchaCats.Clear();
            _captchaFails = 0;
            RebuildCaptchaTiles();

            _captchaLayer = layer;
            _root.Children.Add(layer);
        }

        private void RebuildCaptchaTiles()
        {
            if (_captchaGrid == null)
            {
                return;
            }
            _captchaGrid.Children.Clear();
            _captchaSelected.Clear();
            _captchaCats.Clear();
            while (_captchaCats.Count < 3)
            {
                _captchaCats.Add(_random.Next(9));
            }
            for (int i = 0; i < 9; i++)
            {
                _captchaGrid.Children.Add(MakeCaptchaTile(i));
            }
        }

        private Border MakeCaptchaTile(int index)
        {
            Border tile = new Border();
            tile.Width = 156;
            tile.Height = 156;
            tile.Margin = new Thickness(4);
            tile.Background = Brushes.White;
            tile.BorderBrush = new SolidColorBrush(Color.FromRgb(183, 202, 220));
            tile.BorderThickness = new Thickness(1);
            tile.CornerRadius = new CornerRadius(2);

            Grid content = new Grid();

            Image image = new Image();
            image.Stretch = Stretch.Uniform;
            image.Margin = new Thickness(14);
            image.RenderTransformOrigin = new Point(0.5, 0.5);
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            if (_captchaCats.Contains(index))
            {
                if (_catSource == null)
                {
                    _catSource = LoadBitmap("cat.png");
                }
                image.Source = _catSource;
                if (_random.NextDouble() < 0.5)
                {
                    image.RenderTransform = new ScaleTransform(-1.0, 1.0);
                }
                else
                {
                    double zoom = 0.86 + _random.NextDouble() * 0.14;
                    image.RenderTransform = new ScaleTransform(zoom, zoom);
                }
            }
            else if (_random.NextDouble() < 0.5)
            {
                if (_kfcSource == null)
                {
                    _kfcSource = LoadBitmap("kfc.png");
                }
                image.Source = _kfcSource;
            }
            else
            {
                if (_chickenSource == null)
                {
                    _chickenSource = LoadBitmap("chicken.png");
                }
                image.Source = _chickenSource;
            }
            content.Children.Add(image);

            Border badge = new Border();
            badge.Width = 22;
            badge.Height = 22;
            badge.CornerRadius = new CornerRadius(11);
            badge.Background = new SolidColorBrush(Color.FromRgb(45, 124, 214));
            badge.BorderBrush = Brushes.White;
            badge.BorderThickness = new Thickness(2);
            badge.HorizontalAlignment = HorizontalAlignment.Left;
            badge.VerticalAlignment = VerticalAlignment.Top;
            badge.Margin = new Thickness(6);
            badge.Visibility = Visibility.Collapsed;
            badge.IsHitTestVisible = false;
            TextBlock check = new TextBlock();
            check.Text = "✓";
            check.FontFamily = new FontFamily("Segoe UI");
            check.FontSize = 12;
            check.FontWeight = FontWeights.Bold;
            check.Foreground = Brushes.White;
            check.HorizontalAlignment = HorizontalAlignment.Center;
            check.VerticalAlignment = VerticalAlignment.Center;
            badge.Child = check;
            content.Children.Add(badge);

            tile.Child = content;

            bool selected = false;
            tile.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs args)
            {
                selected = !selected;
                if (selected)
                {
                    _captchaSelected.Add(index);
                    tile.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 124, 214));
                    tile.BorderThickness = new Thickness(2);
                    tile.Background = new SolidColorBrush(Color.FromRgb(234, 243, 253));
                    badge.Visibility = Visibility.Visible;
                }
                else
                {
                    _captchaSelected.Remove(index);
                    tile.BorderBrush = new SolidColorBrush(Color.FromRgb(183, 202, 220));
                    tile.BorderThickness = new Thickness(1);
                    tile.Background = Brushes.White;
                    badge.Visibility = Visibility.Collapsed;
                }
                args.Handled = true;
            };
            tile.MouseEnter += delegate
            {
                if (!selected)
                {
                    tile.Background = new SolidColorBrush(Color.FromRgb(244, 249, 254));
                }
            };
            tile.MouseLeave += delegate
            {
                if (!selected)
                {
                    tile.Background = Brushes.White;
                }
            };
            return tile;
        }

        private void CaptchaCheck()
        {
            if (_captchaLayer == null)
            {
                return;
            }

            _captchaFails++;
            Diag.Log("captcha check fails=" + _captchaFails + " selected=" + _captchaSelected.Count + " cats=" + _captchaCats.Count);
            AwardAt("captcha1", _captchaFails, 1, "Робот?", "Капча не пройдена");
            if (_captchaFails >= 3)
            {
                UnlockAchievement("captcha3", "Ладно, проходи", "Капча пропущена автоматически");
                if (_captchaStatus != null)
                {
                    _captchaStatus.Foreground = new SolidColorBrush(Color.FromRgb(30, 130, 60));
                    _captchaStatus.Text = "Ладно, проходи. Ты человек.";
                }
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(900);
                timer.Tick += delegate
                {
                    timer.Stop();
                    RemoveCaptcha();
                    FinishUnlock();
                };
                timer.Start();
                return;
            }

            string[] messages = new string[]
            {
                "Неверно. Попробуйте ещё раз.",
                "Робот detected. Выберите заново.",
                "Почти получилось. Ещё разок."
            };
            if (_captchaStatus != null)
            {
                _captchaStatus.Text = messages[Math.Min(_captchaFails - 1, messages.Length - 1)];
            }
            RebuildCaptchaTiles();
        }

        private void RemoveCaptcha()
        {
            if (_captchaLayer == null)
            {
                return;
            }
            try
            {
                _root.Children.Remove(_captchaLayer);
            }
            catch
            {
            }
            _captchaLayer = null;
            _captchaGrid = null;
            _captchaStatus = null;
        }

        private void FinishUnlock()
        {
            UnlockPrank();
        }

        private void PlayFinale()
        {
            if (_preview || _smoke)
            {
                return;
            }
            PlayCue("cue_fanfare.mp3", 0.9);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(260);
            timer.Tick += delegate
            {
                timer.Stop();
                PlayCue("sfx_rooster_b.mp3", 0.9);
            };
            timer.Start();
        }

        private MediaPlayer PlayCue(string fileName, double volume)
        {
            return PlayCue(fileName, volume, false);
        }

        private MediaPlayer PlayCue(string fileName, double volume, bool loop)
        {
            if (_preview || _smoke || _closing || string.IsNullOrEmpty(_sfxDir))
            {
                return null;
            }

            try
            {
                string path = IOPath.Combine(_sfxDir, fileName);
                if (!File.Exists(path))
                {
                    return null;
                }

                MediaPlayer player = new MediaPlayer();
                player.Volume = Math.Max(0.0, Math.Min(1.0, volume));
                _cuePlayers.Add(player);

                player.MediaEnded += delegate
                {
                    if (loop && !_closing)
                    {
                        try
                        {
                            player.Position = TimeSpan.Zero;
                            player.Play();
                            return;
                        }
                        catch
                        {
                        }
                    }
                    ReleaseCue(player);
                };
                player.MediaFailed += delegate { ReleaseCue(player); };
                player.Open(new Uri(path, UriKind.Absolute));
                player.Play();
                Diag.Log("cue " + fileName + " volume=" + player.Volume.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " loop=" + loop);
                return player;
            }
            catch
            {
                return null;
            }
        }

        private void ReleaseCue(MediaPlayer player)
        {
            if (player == null)
            {
                return;
            }
            _cuePlayers.Remove(player);
            try { player.Stop(); } catch { }
            try { player.Close(); } catch { }
        }

        private void StopCues()
        {
            List<MediaPlayer> players = new List<MediaPlayer>(_cuePlayers);
            _cuePlayers.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                try { players[i].Stop(); } catch { }
                try { players[i].Close(); } catch { }
            }
            _heartbeatCue = null;
        }

        private void ExtractCues()
        {
            string[] cues = new string[] { "cue_glass.mp3", "cue_heartbeat.mp3", "cue_fanfare.mp3", "cue_crunch.mp3", "cue_achievement.mp3" };
            Assembly assembly = Assembly.GetExecutingAssembly();
            int ready = 0;
            for (int i = 0; i < cues.Length; i++)
            {
                try
                {
                    using (Stream source = assembly.GetManifestResourceStream(cues[i]))
                    {
                        if (source == null)
                        {
                            continue;
                        }
                        string target = IOPath.Combine(_sfxDir, cues[i]);
                        using (FileStream fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            source.CopyTo(fs);
                        }
                        ready++;
                    }
                }
                catch
                {
                }
            }
            _cuesReady = ready == cues.Length;
            Diag.Log("cues ready: " + ready + "/" + cues.Length);
        }

        private Button MakeButton(string text, double width, double height, double fontSize, bool bold)        {
            Style style = GetButtonStyle();

            Button b = new Button();
            b.Content = text;
            b.Width = width;
            b.Height = height;
            b.FontFamily = new FontFamily("Tahoma");
            b.FontSize = fontSize;
            b.FontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
            b.Foreground = Brushes.Black;
            b.Background = Grad(Color.FromRgb(255, 255, 255), Color.FromRgb(222, 222, 222), 90);
            b.BorderBrush = new SolidColorBrush(Color.FromRgb(112, 112, 112));
            b.BorderThickness = new Thickness(1);
            b.Margin = new Thickness(4);
            b.Padding = new Thickness(0);
            b.FocusVisualStyle = null;
            b.Style = style;
            b.UseLayoutRounding = true;
            b.SnapsToDevicePixels = true;
            return b;
        }

        private Style _buttonStyle;

        private Style _aeroButtonStyle;

        private Button MakeAeroButton(string text, double width, double height)
        {
            Button b = new Button();
            b.Content = text;
            b.Width = width;
            b.Height = height;
            b.FontFamily = new FontFamily("Segoe UI");
            b.FontSize = 12.5;
            b.Foreground = new SolidColorBrush(Color.FromRgb(10, 46, 77));
            b.Background = Grad(Color.FromRgb(251, 254, 255), Color.FromRgb(217, 238, 252), 90);
            b.BorderBrush = new SolidColorBrush(Color.FromRgb(74, 141, 191));
            b.BorderThickness = new Thickness(1);
            b.Margin = new Thickness(0);
            b.Padding = new Thickness(0);
            b.FocusVisualStyle = null;
            b.Style = GetAeroButtonStyle();
            b.UseLayoutRounding = true;
            b.SnapsToDevicePixels = true;
            return b;
        }

        private Style GetAeroButtonStyle()
        {
            if (_aeroButtonStyle != null)
            {
                return _aeroButtonStyle;
            }

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.SnapsToDevicePixelsProperty, true);

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger pressed = new Trigger();
            pressed.Property = Button.IsPressedProperty;
            pressed.Value = true;
            pressed.Setters.Add(new Setter(Border.BackgroundProperty, Grad(Color.FromRgb(188, 222, 245), Color.FromRgb(157, 201, 231), 90), "bd"));
            template.Triggers.Add(pressed);

            Trigger hover = new Trigger();
            hover.Property = Button.IsMouseOverProperty;
            hover.Value = true;
            hover.Setters.Add(new Setter(Border.BackgroundProperty, Grad(Color.FromRgb(246, 252, 255), Color.FromRgb(206, 234, 251), 90), "bd"));
            template.Triggers.Add(hover);

            _aeroButtonStyle = new Style(typeof(Button));
            _aeroButtonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));
            return _aeroButtonStyle;
        }

        private Style GetButtonStyle()
        {
            if (_buttonStyle != null)
            {
                return _buttonStyle;
            }

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.SnapsToDevicePixelsProperty, true);

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger pressed = new Trigger();
            pressed.Property = Button.IsPressedProperty;
            pressed.Value = true;
            pressed.Setters.Add(new Setter(Border.BackgroundProperty, Grad(Color.FromRgb(190, 190, 190), Color.FromRgb(225, 225, 225), 90), "bd"));
            template.Triggers.Add(pressed);

            Trigger hover = new Trigger();
            hover.Property = Button.IsMouseOverProperty;
            hover.Value = true;
            hover.Setters.Add(new Setter(Border.BackgroundProperty, Grad(Color.FromRgb(255, 250, 225), Color.FromRgb(235, 235, 235), 90), "bd"));
            template.Triggers.Add(hover);

            _buttonStyle = new Style(typeof(Button));
            _buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));
            return _buttonStyle;
        }

        private static LinearGradientBrush Grad(Color start, Color end, double angle)
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);
            brush.GradientStops.Add(new GradientStop(start, 0));
            brush.GradientStops.Add(new GradientStop(end, 1));
            brush.Freeze();
            return brush;
        }

        private static DropShadowEffect Shadow(double blur, double depth, double opacity)
        {
            DropShadowEffect effect = new DropShadowEffect();
            effect.Color = Color.FromRgb(0, 12, 50);
            effect.BlurRadius = blur;
            effect.ShadowDepth = depth;
            effect.Opacity = opacity;
            effect.RenderingBias = RenderingBias.Performance;
            return effect;
        }

        private static Brush CreateStripeBrush()
        {
            DrawingGroup group = new DrawingGroup();
            using (DrawingContext dc = group.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 100, 100));
                SolidColorBrush stripe = new SolidColorBrush(Color.FromArgb(16, 255, 255, 255));
                stripe.Freeze();
                Pen pen = new Pen(stripe, 16);
                pen.Freeze();
                dc.DrawLine(pen, new Point(-50, 110), new Point(110, -50));
                dc.DrawLine(pen, new Point(50, 160), new Point(160, 50));
            }

            DrawingBrush brush = new DrawingBrush(group);
            brush.TileMode = TileMode.Tile;
            brush.Viewport = new Rect(0, 0, 100, 100);
            brush.ViewportUnits = BrushMappingMode.Absolute;
            brush.Stretch = Stretch.None;
            brush.Freeze();
            return brush;
        }
    }
}
