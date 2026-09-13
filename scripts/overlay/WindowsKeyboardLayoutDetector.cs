using System.Runtime.InteropServices;
using JellyKeyboardOverlay.UI;

namespace JellyKeyboardOverlay.Overlay;

public readonly record struct KeyboardLayoutSuggestion(int? KeyboardType, string Message);

internal static class WindowsKeyboardLayoutDetector
{
    private const uint KeyboardDevice = 1;
    private const uint RidiDeviceInfo = 0x2000000b;
    private const uint Failure = uint.MaxValue;

    public static KeyboardLayoutSuggestion Detect()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new KeyboardLayoutSuggestion(null, "当前系统不支持自动识别");
        }

        try
        {
            var deviceCount = 0u;
            var listSize = (uint)Marshal.SizeOf<RawInputDeviceList>();
            if (GetRawInputDeviceList(null, ref deviceCount, listSize) == Failure || deviceCount == 0)
            {
                return new KeyboardLayoutSuggestion(null, "未读取到键盘信息");
            }
            if (deviceCount > 128)
            {
                return new KeyboardLayoutSuggestion(null, "键盘设备信息不明确");
            }

            var devices = new RawInputDeviceList[(int)deviceCount];
            var returnedCount = GetRawInputDeviceList(devices, ref deviceCount, listSize);
            if (returnedCount == Failure)
            {
                return new KeyboardLayoutSuggestion(null, "未读取到键盘信息");
            }

            var reportedCounts = new HashSet<int>();
            for (var index = 0; index < returnedCount; index++)
            {
                if (devices[index].Type != KeyboardDevice)
                {
                    continue;
                }

                var info = new RawInputDeviceInfo { Size = (uint)Marshal.SizeOf<RawInputDeviceInfo>() };
                var infoSize = info.Size;
                if (GetRawInputDeviceInfoW(devices[index].Device, RidiDeviceInfo, ref info, ref infoSize) == Failure ||
                    info.Type != KeyboardDevice || info.Keyboard.NumberOfKeysTotal == 0)
                {
                    continue;
                }
                reportedCounts.Add((int)info.Keyboard.NumberOfKeysTotal);
            }

            if (reportedCounts.Count == 1)
            {
                var count = reportedCounts.First();
                return KeyboardTypeCatalog.IsSupported(count)
                    ? new KeyboardLayoutSuggestion(count, $"系统建议：{count} 键")
                    : new KeyboardLayoutSuggestion(null, $"系统报告 {count} 键，请手动选择");
            }
            return new KeyboardLayoutSuggestion(null, reportedCounts.Count > 1
                ? "系统返回不同键数，可能包含虚拟设备；可手动选择"
                : "未获得可靠键数，请手动选择");
        }
        catch (Exception)
        {
            return new KeyboardLayoutSuggestion(null, "自动识别不可用，请手动选择");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDeviceList
    {
        public IntPtr Device;
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardDeviceInfo
    {
        public uint Type;
        public uint SubType;
        public uint KeyboardMode;
        public uint NumberOfFunctionKeys;
        public uint NumberOfIndicators;
        public uint NumberOfKeysTotal;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct RawInputDeviceInfo
    {
        [FieldOffset(0)] public uint Size;
        [FieldOffset(4)] public uint Type;
        [FieldOffset(8)] public KeyboardDeviceInfo Keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(
        [Out] RawInputDeviceList[]? devices, ref uint deviceCount, uint listSize);

    [DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfoW(
        IntPtr device, uint command, ref RawInputDeviceInfo info, ref uint infoSize);
}
