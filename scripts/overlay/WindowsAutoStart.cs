using Microsoft.Win32;

namespace JellyKeyboardOverlay.Overlay;

internal static class WindowsAutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "JellyKeys";

    public static string BuildCommand(string executablePath, string projectPath)
    {
        var command = $"\"{executablePath}\"";
        var executableName = Path.GetFileName(executablePath);
        if (executableName.Contains("Godot", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(Path.Combine(projectPath, "project.godot")))
        {
            command += $" --path \"{projectPath.TrimEnd(Path.DirectorySeparatorChar)}\"";
        }
        return command;
    }

    public static bool IsEnabled(string command)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return string.Equals(key?.GetValue(ValueName) as string, command, StringComparison.OrdinalIgnoreCase);
    }

    public static void Toggle(string command)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("自启动仅支持 Windows。");
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("无法打开当前用户的自启动设置。");
        if (string.Equals(key.GetValue(ValueName) as string, command, StringComparison.OrdinalIgnoreCase))
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        else
        {
            key.SetValue(ValueName, command, RegistryValueKind.String);
        }
    }
}
