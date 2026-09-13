using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace JellyKeyboardOverlay.Input;

public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const uint WmSysKeyDown = 0x0104;
    private const uint WmSysKeyUp = 0x0105;
    private const uint WmQuit = 0x0012;
    private const uint LlkhfExtended = 0x01;

    private readonly ConcurrentQueue<RawKeyEvent> _events = new();
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly HookProc _callback;
    private Thread? _thread;
    private IntPtr _hookHandle;
    private uint _threadId;
    private volatile bool _disposed;

    public GlobalKeyboardHook()
    {
        _callback = HookCallback;
    }

    public bool IsRunning => _hookHandle != IntPtr.Zero;
    public string? StartupError { get; private set; }

    public bool Start()
    {
        if (_thread is not null)
        {
            return IsRunning;
        }

        _thread = new Thread(HookThreadMain)
        {
            IsBackground = true,
            Name = "JellyKeyboard.GlobalInput",
        };
        _thread.Start();

        if (!_ready.Wait(TimeSpan.FromSeconds(3)))
        {
            StartupError = "Global keyboard listener timed out during startup.";
            return false;
        }

        return IsRunning;
    }

    public bool TryDequeue(out RawKeyEvent keyEvent) => _events.TryDequeue(out keyEvent);

    private void HookThreadMain()
    {
        _threadId = GetCurrentThreadId();

        try
        {
            var module = GetModuleHandle(null);
            _hookHandle = SetWindowsHookEx(WhKeyboardLl, _callback, module, 0);
            if (_hookHandle == IntPtr.Zero)
            {
                StartupError = new Win32Exception(Marshal.GetLastWin32Error(), "Could not start the global keyboard listener.").Message;
                return;
            }
        }
        finally
        {
            _ready.Set();
        }

        try
        {
            while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
            {
                // Low-level hook callbacks are dispatched by this message loop.
            }
        }
        finally
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && !_disposed)
        {
            var message = unchecked((uint)wParam.ToInt64());
            var pressed = message is WmKeyDown or WmSysKeyDown;
            var released = message is WmKeyUp or WmSysKeyUp;

            if (pressed || released)
            {
                var data = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                _events.Enqueue(new RawKeyEvent(
                    data.VirtualKey,
                    data.ScanCode,
                    (data.Flags & LlkhfExtended) != 0,
                    pressed,
                    message is WmSysKeyDown or WmSysKeyUp));
            }
        }

        return CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
        }

        _thread?.Join(TimeSpan.FromSeconds(2));
        _ready.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint VirtualKey;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Window;
        public uint MessageId;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public Point Point;
        public uint Private;
    }

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetMessage(out Message message, IntPtr window, uint filterMin, uint filterMax);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

