using System.Runtime.InteropServices;

namespace JellyKeyboardOverlay.Overlay;

/// <summary>
/// Applies true cross-process click-through to the transparent overlay and
/// clips the one-pixel frame Windows can add to layered OpenGL windows.
/// </summary>
internal static class WindowsWindowInputMode
{
	private const int GwlExStyle = -20;
	private const long WsExTransparent = 0x00000020L;
	private const long WsExLayered = 0x00080000L;
	private const long WsExNoActivate = 0x08000000L;
	private const uint SwpNoSize = 0x0001;
	private const uint SwpNoMove = 0x0002;
	private const uint SwpNoZOrder = 0x0004;
	private const uint SwpNoActivate = 0x0010;
	private const uint SwpFrameChanged = 0x0020;
	private const int WindowEdgeInset = 1;

	public static bool Apply(IntPtr windowHandle, bool clickThrough, bool clipWindowEdge, out string error)
	{
		error = string.Empty;
		if (!OperatingSystem.IsWindows() || windowHandle == IntPtr.Zero)
		{
			return true;
		}

		Marshal.SetLastPInvokeError(0);
		var currentStyle = GetWindowLongPtrW(windowHandle, GwlExStyle);
		var getError = Marshal.GetLastPInvokeError();
		if (currentStyle == IntPtr.Zero && getError != 0)
		{
			error = $"GetWindowLongPtrW failed ({getError}).";
			return false;
		}

		var currentBits = currentStyle.ToInt64();
		var requestedBits = clickThrough
			? currentBits | WsExTransparent | WsExLayered | WsExNoActivate
			: currentBits & ~WsExTransparent & ~WsExLayered & ~WsExNoActivate;

		if (requestedBits != currentBits)
		{
			Marshal.SetLastPInvokeError(0);
			var previousStyle = SetWindowLongPtrW(windowHandle, GwlExStyle, new IntPtr(requestedBits));
			var setError = Marshal.GetLastPInvokeError();
			if (previousStyle == IntPtr.Zero && setError != 0)
			{
				error = $"SetWindowLongPtrW failed ({setError}).";
				return false;
			}
		}

		if (!ApplyWindowRegion(windowHandle, clipWindowEdge, out error))
		{
			return false;
		}

		if (!SetWindowPos(
			windowHandle,
			IntPtr.Zero,
			0,
			0,
			0,
			0,
			SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged))
		{
			error = $"SetWindowPos failed ({Marshal.GetLastPInvokeError()}).";
			return false;
		}

		return true;
	}

	private static bool ApplyWindowRegion(IntPtr windowHandle, bool clipEdge, out string error)
	{
		error = string.Empty;
		if (!clipEdge)
		{
			if (SetWindowRgn(windowHandle, IntPtr.Zero, true) == 0)
			{
				error = $"SetWindowRgn reset failed ({Marshal.GetLastPInvokeError()}).";
				return false;
			}
			return true;
		}

		if (!GetClientRect(windowHandle, out var clientRect))
		{
			error = $"GetClientRect failed ({Marshal.GetLastPInvokeError()}).";
			return false;
		}

		var right = Math.Max(WindowEdgeInset + 1, clientRect.Right - WindowEdgeInset);
		var bottom = Math.Max(WindowEdgeInset + 1, clientRect.Bottom - WindowEdgeInset);
		var region = CreateRectRgn(WindowEdgeInset, WindowEdgeInset, right, bottom);
		if (region == IntPtr.Zero)
		{
			error = "CreateRectRgn failed.";
			return false;
		}

		if (SetWindowRgn(windowHandle, region, true) == 0)
		{
			DeleteObject(region);
			error = $"SetWindowRgn failed ({Marshal.GetLastPInvokeError()}).";
			return false;
		}

		// After a successful SetWindowRgn call, Windows owns the region handle.
		return true;
	}

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
	private static extern IntPtr GetWindowLongPtrW(IntPtr windowHandle, int index);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static extern IntPtr SetWindowLongPtrW(IntPtr windowHandle, int index, IntPtr newValue);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetWindowPos(
		IntPtr windowHandle,
		IntPtr insertAfter,
		int x,
		int y,
		int width,
		int height,
		uint flags);

	[StructLayout(LayoutKind.Sequential)]
	private struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetClientRect(IntPtr windowHandle, out Rect rect);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int SetWindowRgn(IntPtr windowHandle, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DeleteObject(IntPtr value);
}
