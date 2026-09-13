using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace JellyKeyboardOverlay.Overlay;

/// <summary>
/// Windows notification-area integration. Tray callbacks enqueue commands;
/// Godot scene changes are performed later on the main thread.
/// </summary>
internal sealed class WindowsSystemTrayIcon : IDisposable
{
	private const int GwlpWndProc = -4;
	private const uint WmApp = 0x8000;
	private const uint CallbackMessage = WmApp + 0x31;
	private const uint WmNull = 0x0000;
	private const uint WmContextMenu = 0x007B;
	private const uint WmLeftButtonDoubleClick = 0x0203;
	private const uint WmRightButtonUp = 0x0205;
	private const uint NimAdd = 0x00000000;
	private const uint NimModify = 0x00000001;
	private const uint NimDelete = 0x00000002;
	private const uint NimSetVersion = 0x00000004;
	private const uint NifMessage = 0x00000001;
	private const uint NifIcon = 0x00000002;
	private const uint NifTip = 0x00000004;
	private const uint NifShowTip = 0x00000080;
	private const uint NotifyIconVersion4 = 4;
	private const uint ShgfiIcon = 0x000000100;
	private const uint ShgfiSmallIcon = 0x000000001;
	private const uint ImageIcon = 1;
	private const uint LrLoadFromFile = 0x00000010;
	private const uint LrDefaultSize = 0x00000040;
	private const uint IconId = 1;
	private const uint OpenToolbarMenuId = 1001;
	private const uint ExitMenuId = 1002;
	private const uint AutoStartMenuId = 1003;
	private const uint MfString = 0x00000000;
	private const uint MfSeparator = 0x00000800;
	private const uint MfChecked = 0x00000008;
	private const uint TpmRightButton = 0x0002;
	private const uint TpmNoNotify = 0x0080;
	private const uint TpmReturnCommand = 0x0100;
	private const uint DwmwaWindowCornerPreference = 33;
	private const int DwmwcpRound = 2;
	private const string PopupMenuWindowClass = "#32768";

	private readonly IntPtr _windowHandle;
	private readonly uint _windowThreadId;
	private readonly string _executablePath;
	private readonly string _customIconPath;
	private readonly string _startupCommand;
	private readonly ConcurrentQueue<TrayEvent> _commands = new();
	private readonly WindowProcedure _windowProcedure;
	private readonly uint _taskbarCreatedMessage;
	private IntPtr _previousWindowProcedure;
	private IntPtr _iconHandle;
	private bool _ownsIconHandle;
	private bool _isReady;
	private bool _version4;
	private bool _usingCustomIcon;
	private bool _menuRequestPending;
	private bool _menuTracking;

	private WindowsSystemTrayIcon(IntPtr windowHandle, string executablePath, string customIconPath, string startupCommand)
	{
		_windowHandle = windowHandle;
		_windowThreadId = GetWindowThreadProcessId(windowHandle, out _);
		_executablePath = executablePath;
		_customIconPath = customIconPath;
		_startupCommand = startupCommand;
		_windowProcedure = HandleWindowMessage;
		_taskbarCreatedMessage = RegisterWindowMessageW("TaskbarCreated");
	}

	public bool IsReady => _isReady;
	public bool IsUsingCustomIcon => _usingCustomIcon;
	public bool UsesNativeRoundedMenu => true;

	public bool TryRestore()
	{
		if (!_isReady && IsWindow(_windowHandle))
		{
			_isReady = AddIcon();
		}
		return _isReady;
	}

	public static bool TryCreate(
		IntPtr windowHandle,
		string executablePath,
		string customIconPath,
		string startupCommand,
		out WindowsSystemTrayIcon? trayIcon,
		out string error)
	{
		trayIcon = null;
		if (windowHandle == IntPtr.Zero)
		{
			error = "Godot window handle is unavailable.";
			return false;
		}

		var candidate = new WindowsSystemTrayIcon(windowHandle, executablePath, customIconPath, startupCommand);
		try
		{
			candidate._previousWindowProcedure = SetWindowLongPtrW(
				windowHandle,
				GwlpWndProc,
				Marshal.GetFunctionPointerForDelegate(candidate._windowProcedure));
			if (candidate._previousWindowProcedure == IntPtr.Zero)
			{
				error = $"Window callback could not be installed (Win32 {Marshal.GetLastWin32Error()}).";
				candidate.Dispose();
				return false;
			}

			candidate.LoadTrayIcon();
			if (!candidate.AddIcon())
			{
				error = $"Notification icon could not be added (Win32 {Marshal.GetLastWin32Error()}).";
				candidate.Dispose();
				return false;
			}

			candidate._isReady = true;
			trayIcon = candidate;
			error = string.Empty;
			return true;
		}
		catch (Exception exception)
		{
			error = exception.Message;
			candidate.Dispose();
			return false;
		}
	}

	public void DrainCommands(Action openToolbar, Action toggleAutoStart, Action exit)
	{
		while (_commands.TryDequeue(out var trayEvent))
		{
			switch (trayEvent.Command)
			{
				case TrayCommand.ShowMenu:
					_menuRequestPending = false;
					_menuTracking = true;
					try
					{
						ShowContextMenu(new Point { X = trayEvent.X, Y = trayEvent.Y });
					}
					finally
					{
						_menuTracking = false;
					}
					break;
				case TrayCommand.OpenToolbar:
					openToolbar();
					break;
				case TrayCommand.ToggleAutoStart:
					toggleAutoStart();
					break;
				case TrayCommand.Exit:
					exit();
					break;
			}
		}
	}

	public void Dispose()
	{
		if (_isReady)
		{
			var data = CreateNotifyIconData();
			ShellNotifyIconW(NimDelete, ref data);
			_isReady = false;
		}

		if (_previousWindowProcedure != IntPtr.Zero && IsWindow(_windowHandle))
		{
			SetWindowLongPtrW(_windowHandle, GwlpWndProc, _previousWindowProcedure);
			_previousWindowProcedure = IntPtr.Zero;
		}

		if (_ownsIconHandle && _iconHandle != IntPtr.Zero)
		{
			DestroyIcon(_iconHandle);
		}
		_iconHandle = IntPtr.Zero;
		_ownsIconHandle = false;
		_usingCustomIcon = false;
	}

	private IntPtr HandleWindowMessage(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam)
	{
		try
		{
			if (_taskbarCreatedMessage != 0 && message == _taskbarCreatedMessage)
			{
				_isReady = false;
				_isReady = AddIcon();
				return IntPtr.Zero;
			}

			if (message == CallbackMessage)
			{
				var mouseMessage = _version4
					? unchecked((uint)lParam.ToInt64()) & 0xFFFF
					: unchecked((uint)lParam.ToInt64());
				if (ShouldQueueMenuRequest(mouseMessage, _menuRequestPending, _menuTracking))
				{
					if (GetCursorPos(out var cursorPosition))
					{
						_menuRequestPending = true;
						_commands.Enqueue(new TrayEvent(
							TrayCommand.ShowMenu,
							cursorPosition.X,
							cursorPosition.Y));
					}
				}
				else if (mouseMessage == WmLeftButtonDoubleClick)
				{
					_commands.Enqueue(new TrayEvent(TrayCommand.OpenToolbar));
				}
				return IntPtr.Zero;
			}
		}
		catch
		{
			// Never allow an exception to escape a native window procedure.
		}

		return _previousWindowProcedure == IntPtr.Zero
			? DefWindowProcW(windowHandle, message, wParam, lParam)
			: CallWindowProcW(_previousWindowProcedure, windowHandle, message, wParam, lParam);
	}

	internal static bool ShouldQueueMenuRequest(uint mouseMessage, bool requestPending, bool menuTracking)
	{
		return (mouseMessage is WmContextMenu or WmRightButtonUp) &&
			!requestPending && !menuTracking;
	}

	private void ShowContextMenu(Point cursorPosition)
	{
		if (!_isReady || !IsWindow(_windowHandle))
		{
			return;
		}

		var menuHandle = CreatePopupMenu();
		if (menuHandle == IntPtr.Zero)
		{
			return;
		}

		try
		{
			AppendMenuW(menuHandle, MfString, new UIntPtr(OpenToolbarMenuId), "打开 Bar 条");
			var autoStartEnabled = false;
			try
			{
				autoStartEnabled = WindowsAutoStart.IsEnabled(_startupCommand);
			}
			catch (Exception exception)
			{
				Godot.GD.PushWarning($"Could not read autostart setting: {exception.Message}");
			}
			AppendMenuW(menuHandle, MfString | (autoStartEnabled ? MfChecked : 0),
				new UIntPtr(AutoStartMenuId), "自启动");
			AppendMenuW(menuHandle, MfSeparator, UIntPtr.Zero, null);
			AppendMenuW(menuHandle, MfString, new UIntPtr(ExitMenuId), "退出");

			SetForegroundWindow(_windowHandle);
			QueueNativeMenuRounding();
			var selectedCommand = TrackPopupMenuEx(
				menuHandle,
				TpmRightButton | TpmNoNotify | TpmReturnCommand,
				cursorPosition.X,
				cursorPosition.Y,
				_windowHandle,
				IntPtr.Zero);
			PostMessageW(_windowHandle, WmNull, IntPtr.Zero, IntPtr.Zero);

			switch (selectedCommand)
			{
				case OpenToolbarMenuId:
					_commands.Enqueue(new TrayEvent(TrayCommand.OpenToolbar));
					break;
				case AutoStartMenuId:
					_commands.Enqueue(new TrayEvent(TrayCommand.ToggleAutoStart));
					break;
				case ExitMenuId:
					_commands.Enqueue(new TrayEvent(TrayCommand.Exit));
					break;
			}
		}
		finally
		{
			DestroyMenu(menuHandle);
		}
	}

	private void QueueNativeMenuRounding()
	{
		if (_windowThreadId == 0)
		{
			return;
		}

		ThreadPool.QueueUserWorkItem(_ =>
		{
			for (var attempt = 0; attempt < 20; attempt++)
			{
				var rounded = false;
				EnumThreadWindows(_windowThreadId, (windowHandle, _) =>
				{
					var className = new StringBuilder(32);
					GetClassNameW(windowHandle, className, className.Capacity);
					if (!string.Equals(className.ToString(), PopupMenuWindowClass, StringComparison.Ordinal) ||
						!IsWindowVisible(windowHandle))
					{
						return true;
					}

					ApplyRoundedCorners(windowHandle);
					rounded = true;
					return false;
				}, IntPtr.Zero);

				if (rounded)
				{
					return;
				}
				Thread.Sleep(8);
			}
		});
	}

	private static void ApplyRoundedCorners(IntPtr menuWindow)
	{
		var cornerPreference = DwmwcpRound;
		DwmSetWindowAttribute(
			menuWindow,
			DwmwaWindowCornerPreference,
			ref cornerPreference,
			Marshal.SizeOf<int>());

		if (!GetWindowRect(menuWindow, out var rectangle))
		{
			return;
		}

		var dpi = 96u;
		try
		{
			dpi = GetDpiForWindow(menuWindow);
		}
		catch (EntryPointNotFoundException)
		{
			// Older Windows versions use the 96-DPI fallback.
		}
		var diameter = Math.Max(12, (int)MathF.Round(22f * dpi / 96f));
		var region = CreateRoundRectRgn(
			0,
			0,
			rectangle.Right - rectangle.Left + 1,
			rectangle.Bottom - rectangle.Top + 1,
			diameter,
			diameter);
		if (region != IntPtr.Zero && SetWindowRgn(menuWindow, region, true) == 0)
		{
			DeleteObject(region);
		}
	}

	private void LoadTrayIcon()
	{
		if (!string.IsNullOrWhiteSpace(_customIconPath) && File.Exists(_customIconPath))
		{
			var customIcon = LoadImageW(
				IntPtr.Zero,
				_customIconPath,
				ImageIcon,
				0,
				0,
				LrLoadFromFile | LrDefaultSize);
			if (customIcon != IntPtr.Zero)
			{
				_iconHandle = customIcon;
				_ownsIconHandle = true;
				_usingCustomIcon = true;
				return;
			}
		}

		if (!string.IsNullOrWhiteSpace(_executablePath))
		{
			var result = SHGetFileInfoW(
				_executablePath,
				0,
				out var fileInfo,
				(uint)Marshal.SizeOf<ShellFileInfo>(),
				ShgfiIcon | ShgfiSmallIcon);
			if (result != IntPtr.Zero && fileInfo.IconHandle != IntPtr.Zero)
			{
				_iconHandle = fileInfo.IconHandle;
				_ownsIconHandle = true;
				return;
			}
		}

		_iconHandle = LoadIconW(IntPtr.Zero, new IntPtr(32512));
		_ownsIconHandle = false;
	}

	private bool AddIcon()
	{
		var data = CreateNotifyIconData();
		if (!ShellNotifyIconW(_isReady ? NimModify : NimAdd, ref data))
		{
			return false;
		}

		data.TimeoutOrVersion = NotifyIconVersion4;
		_version4 = ShellNotifyIconW(NimSetVersion, ref data);
		return true;
	}

	private NotifyIconData CreateNotifyIconData()
	{
		return new NotifyIconData
		{
			Size = (uint)Marshal.SizeOf<NotifyIconData>(),
			WindowHandle = _windowHandle,
			Id = IconId,
			Flags = NifMessage | NifIcon | NifTip | NifShowTip,
			CallbackMessage = CallbackMessage,
			IconHandle = _iconHandle,
			Tip = "果冻键显 JellyKeys",
			Info = string.Empty,
			InfoTitle = string.Empty,
		};
	}

	private enum TrayCommand
	{
		ShowMenu,
		OpenToolbar,
		ToggleAutoStart,
		Exit,
	}

	private readonly record struct TrayEvent(TrayCommand Command, int X = 0, int Y = 0);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NotifyIconData
	{
		public uint Size;
		public IntPtr WindowHandle;
		public uint Id;
		public uint Flags;
		public uint CallbackMessage;
		public IntPtr IconHandle;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string Tip;

		public uint State;
		public uint StateMask;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string Info;

		public uint TimeoutOrVersion;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string InfoTitle;

		public uint InfoFlags;
		public Guid GuidItem;
		public IntPtr BalloonIconHandle;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct ShellFileInfo
	{
		public IntPtr IconHandle;
		public int IconIndex;
		public uint Attributes;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string DisplayName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string TypeName;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Point
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	private delegate IntPtr WindowProcedure(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);
	private delegate bool EnumThreadWindowProcedure(IntPtr windowHandle, IntPtr lParam);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "Shell_NotifyIconW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool ShellNotifyIconW(uint message, ref NotifyIconData data);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
	private static extern IntPtr SHGetFileInfoW(
		string path,
		uint fileAttributes,
		out ShellFileInfo fileInfo,
		uint fileInfoSize,
		uint flags);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetWindowLongPtrW")]
	private static extern IntPtr SetWindowLongPtrW(IntPtr windowHandle, int index, IntPtr newValue);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "CallWindowProcW")]
	private static extern IntPtr CallWindowProcW(
		IntPtr previousWindowProcedure,
		IntPtr windowHandle,
		uint message,
		IntPtr wParam,
		IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "DefWindowProcW")]
	private static extern IntPtr DefWindowProcW(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegisterWindowMessageW")]
	private static extern uint RegisterWindowMessageW(string message);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out Point point);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadIconW")]
	private static extern IntPtr LoadIconW(IntPtr instance, IntPtr iconName);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "LoadImageW")]
	private static extern IntPtr LoadImageW(
		IntPtr instance,
		string imageName,
		uint type,
		int width,
		int height,
		uint loadFlags);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DestroyIcon(IntPtr iconHandle);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "AppendMenuW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AppendMenuW(IntPtr menuHandle, uint flags, UIntPtr itemId, string? text);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint TrackPopupMenuEx(
		IntPtr menuHandle,
		uint flags,
		int x,
		int y,
		IntPtr windowHandle,
		IntPtr parameters);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DestroyMenu(IntPtr menuHandle);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(IntPtr windowHandle);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "PostMessageW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PostMessageW(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumThreadWindows(
		uint threadId,
		EnumThreadWindowProcedure enumProcedure,
		IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
	private static extern int GetClassNameW(IntPtr windowHandle, StringBuilder className, int maxCount);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindowVisible(IntPtr windowHandle);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(IntPtr windowHandle, out Rect rectangle);

	[DllImport("user32.dll")]
	private static extern int SetWindowRgn(IntPtr windowHandle, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(IntPtr windowHandle);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(
		IntPtr windowHandle,
		uint attribute,
		ref int attributeValue,
		int attributeSize);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateRoundRectRgn(
		int left,
		int top,
		int right,
		int bottom,
		int ellipseWidth,
		int ellipseHeight);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DeleteObject(IntPtr gdiObject);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindow(IntPtr windowHandle);
}
