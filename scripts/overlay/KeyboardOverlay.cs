using Godot;
using JellyKeyboardOverlay.Input;
using JellyKeyboardOverlay.Layout;
using JellyKeyboardOverlay.Settings;
using JellyKeyboardOverlay.UI;

namespace JellyKeyboardOverlay.Overlay;

public partial class KeyboardOverlay : Control
{
	private enum SettingsStage { Hidden, Opening, Visible, Closing }

	private static readonly Vector2I BaseWindowSize = new(1218, 482);
	private static readonly Vector2 DeckPosition = new(14f, 88f);
	private const float CursorOverlapAlpha = 0.18f;
	private const double TrayRetryIntervalSeconds = 10;

	private readonly Dictionary<string, KeyCapView> _keyCaps = new(StringComparer.Ordinal);
	private readonly HashSet<string> _pressedKeys = new(StringComparer.Ordinal);
	private readonly List<KeyboardDesignData> _toolbarThemes = new();
	private byte[]? _cachedTextAtlasPng;
	private Texture2D? _cachedTextAtlasTexture;

	private OverlaySettings _settings = new();
	private KeyboardLayoutSuggestion _deviceLayoutSuggestion;
	private GlobalKeyboardHook? _keyboardHook;
	private WindowsSystemTrayIcon? _systemTrayIcon;
	private Panel? _deckLip;
	private Panel? _deck;
	private Control? _keyLayer;
	private EditToolbarView? _toolbar;
	private ThemeSettingsView? _settingsPanel;
	private KeyboardLayoutData? _layout;
	private KeyboardDesignData _activeDesign = KeyboardDesignData.FromPreset(KeyboardThemes.Presets[0]);
	private bool _embeddedInEditor;
	private SettingsStage _settingsStage;
	private bool _settingsOpen => _settingsStage != SettingsStage.Hidden;
	private bool _wakeAfterSettingsClose;
	private double _trayRetryElapsed;
	private string? _lastTrayError;
	private bool _windowLocked;
	private bool _pendingLocked;
	private float _visualAlpha = 1f;

	public override void _Ready()
	{
		var arguments = OS.GetCmdlineUserArgs();
		_embeddedInEditor = Engine.IsEmbeddedInEditor();
		_settings = OverlaySettings.Load();
		_pendingLocked = _settings.Locked;
		_deviceLayoutSuggestion = WindowsKeyboardLayoutDetector.Detect();

		_layout = KeyboardLayout.Create108();
		BuildInterface(_layout);
		_activeDesign = KeyboardThemeStore.LoadActive(
			KeyboardDesignData.FromPreset(KeyboardThemes.Presets[_settings.ThemeIndex]));
		ApplyDesign(_activeDesign);
		ApplyScale(_settings.Scale, reposition: _settings.WindowPosition.X < 0);
		var initialLocked = arguments.Contains("--locked-mode")
			? true
			: arguments.Contains("--edit-mode") || _embeddedInEditor
				? false
				: _settings.Locked;
		ApplyWindowMode(initialLocked);

		if (!_embeddedInEditor)
		{
			StartKeyboardHook();
			StartSystemTray();
		}
		else
		{
			SetStatus("嵌入预览：点击画面后可测试按键；窗口移动请使用外部启动文件");
		}
	}

	public override void _Process(double delta)
	{
		if (!_embeddedInEditor && _systemTrayIcon?.IsReady != true)
		{
			_trayRetryElapsed += delta;
			if (_trayRetryElapsed >= TrayRetryIntervalSeconds)
			{
				_trayRetryElapsed = 0;
				StartSystemTray();
			}
		}
		_systemTrayIcon?.DrainCommands(WakeToolbar, ToggleAutoStart, () => GetTree().Quit());
		DrainGlobalInput();
		UpdateCursorOverlapFade((float)delta);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (_keyboardHook?.IsRunning == true || @event is not InputEventKey keyEvent || keyEvent.Echo)
		{
			return;
		}

		var keyId = MapFocusedKey(keyEvent);
		if (keyId is null)
		{
			return;
		}

		HandleMappedKey(keyId, keyEvent.Pressed);
	}

	public override void _ExitTree()
	{
		_systemTrayIcon?.Dispose();
		_keyboardHook?.Dispose();
		KeyboardThemeStore.SaveActive(_activeDesign);
		if (!_embeddedInEditor)
		{
			_settings.WindowPosition = DisplayServer.WindowGetPosition();
			_settings.Save();
		}
	}

	private void BuildInterface(KeyboardLayoutData layout)
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Pass;
		GetViewport().TransparentBg = true;
		GetViewport().Snap2DTransformsToPixel = true;
		GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;

		_deckLip = GetNode<Panel>("KeyboardDeckDepth");
		_deck = GetNode<Panel>("KeyboardDeck");
		_toolbar = GetNode<EditToolbarView>("EditToolbar");
		_settingsPanel = GetNode<ThemeSettingsView>("ThemeSettings");

		var deckSize = layout.ContentSize + KeyboardVisualMetrics.DeckPadding * 2f;
		_deckLip.Position = DeckPosition + new Vector2(0f, KeyboardVisualMetrics.DeckDepth);
		_deckLip.Size = deckSize;
		_deck.Position = DeckPosition;
		_deck.Size = deckSize;
		_deck.MouseDefaultCursorShape = CursorShape.Move;
		_deck.GuiInput += OnEditSurfaceGuiInput;

		_keyLayer = _deck.GetNode<Control>("Keys");
		_keyLayer.Position = KeyboardVisualMetrics.DeckPadding;
		_keyLayer.Size = layout.ContentSize;
		foreach (var child in _keyLayer.GetChildren())
		{
			child.Free();
		}

		foreach (var definition in layout.Keys)
		{
			var keyCap = new KeyCapView(definition.Id, definition.Label);
			_keyLayer.AddChild(keyCap);
			keyCap.Configure(definition.Rect);
			_keyCaps.Add(definition.Id, keyCap);
		}

		BuildToolbar();
		_settingsPanel.CloseRequested += CloseSettings;
		_settingsPanel.DesignChanged += ApplyDesign;
		_settingsPanel.DeviceLayoutChanged += ChangeDeviceLayout;
		_settingsPanel.DeviceLayoutRescanRequested += RedetectDeviceLayout;
	}

	private void BuildToolbar()
	{
		_toolbar!.ThemeSelected += ApplyToolbarTheme;
		_toolbar.ConfirmRequested += ConfirmEdits;
		_toolbar.ScaleStepRequested += ChangeScale;
		_toolbar.LockChanged += SetPendingLock;
		_toolbar.SettingsRequested += OpenSettings;
		_toolbar.DragSurfaceInput += OnEditSurfaceGuiInput;
	}

	private void StartKeyboardHook()
	{
		_keyboardHook = new GlobalKeyboardHook();
		if (!_keyboardHook.Start())
		{
			GD.PushWarning($"Global keyboard input unavailable: {_keyboardHook.StartupError}. Focused-window input is still active.");
			ApplyWindowMode(false);
			SetStatus("全局监听不可用；当前仅响应窗口内按键");
		}
	}

	private void StartSystemTray()
	{
		if (!System.OperatingSystem.IsWindows())
		{
			return;
		}
		if (_systemTrayIcon is not null)
		{
			if (_systemTrayIcon.TryRestore())
			{
				_lastTrayError = null;
			}
			return;
		}

		var nativeHandle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
		var adjacentIconPath = Path.Combine(
			Path.GetDirectoryName(OS.GetExecutablePath()) ?? ".", "JellyKeys.ico");
		var iconPath = File.Exists(adjacentIconPath)
			? adjacentIconPath
			: ProjectSettings.GlobalizePath("res://assets/app_icon.ico");
		if (WindowsSystemTrayIcon.TryCreate(
			new IntPtr(unchecked((long)nativeHandle)),
			OS.GetExecutablePath(),
			iconPath,
			WindowsAutoStart.BuildCommand(OS.GetExecutablePath(), ProjectSettings.GlobalizePath("res://")),
			out var trayIcon,
			out var error))
		{
			_systemTrayIcon = trayIcon;
			_lastTrayError = null;
			return;
		}

		if (!string.Equals(_lastTrayError, error, StringComparison.Ordinal))
		{
			GD.PushWarning($"System tray unavailable: {error}");
			_lastTrayError = error;
		}
	}

	private void ToggleAutoStart()
	{
		try
		{
			WindowsAutoStart.Toggle(WindowsAutoStart.BuildCommand(
				OS.GetExecutablePath(), ProjectSettings.GlobalizePath("res://")));
		}
		catch (Exception exception)
		{
			GD.PushWarning($"自启动设置失败：{exception.Message}");
		}
	}

	private void DrainGlobalInput()
	{
		if (_keyboardHook is null)
		{
			return;
		}

		while (_keyboardHook.TryDequeue(out var rawEvent))
		{
			if (KeyMapper.TryMap(rawEvent, out var keyId))
			{
				HandleMappedKey(keyId, rawEvent.Pressed);
			}
		}
	}

	private void HandleMappedKey(string keyId, bool pressed)
	{
		if (pressed)
		{
			var firstPress = _pressedKeys.Add(keyId);
			if (_keyCaps.TryGetValue(keyId, out var keyCap))
			{
				keyCap.PressVisual(repeat: !firstPress);
			}

			if (firstPress)
			{
				HandleShortcut(keyId);
			}
		}
		else if (_pressedKeys.Remove(keyId) && _keyCaps.TryGetValue(keyId, out var keyCap))
		{
			keyCap.ReleaseVisual();
		}
	}

	private void HandleShortcut(string keyId)
	{
		var control = _pressedKeys.Contains("ControlLeft") || _pressedKeys.Contains("ControlRight");
		var shift = _pressedKeys.Contains("ShiftLeft") || _pressedKeys.Contains("ShiftRight");
		if (!control || !shift)
		{
			return;
		}

		if (keyId == "F10")
		{
			WakeToolbar();
		}
	}

	private void WakeToolbar()
	{
		if (_settingsOpen)
		{
			_wakeAfterSettingsClose = true;
			CloseSettings();
			return;
		}

		_deck!.Visible = true;
		_deckLip!.Visible = true;
		ApplyWindowMode(false);
		if (!_embeddedInEditor && !string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
		{
			CallDeferred(nameof(ActivateToolbarWindow));
		}
	}

	private void ActivateToolbarWindow()
	{
		if (_settingsOpen || _embeddedInEditor)
		{
			return;
		}

		SetDesktopClickThrough(false);
		DisplayServer.WindowMoveToForeground();
	}

	private void ApplyToolbarTheme(KeyboardDesignData design)
	{
		ApplyDesign(design);
		KeyboardThemeStore.SaveActive(_activeDesign);
	}

	private void ApplyDesign(KeyboardDesignData design)
	{
		_activeDesign = design.Clone();
		var displayKeyboardType = _settings.ResolveKeyboardType(
			_activeDesign.KeyboardType, _deviceLayoutSuggestion.KeyboardType);
		var theme = _activeDesign.Palette;
		var matchedThemeIndex = KeyboardThemes.Presets
			.Select((preset, index) => (preset, index))
			.Where(item => item.preset.Id == theme.Id)
			.Select(item => item.index)
			.DefaultIfEmpty(-1)
			.First();
		if (matchedThemeIndex >= 0)
		{
			_settings.ThemeIndex = matchedThemeIndex;
		}

		ApplyKeyboardGeometry(displayKeyboardType);

		var lipStyle = new StyleBoxFlat
		{
			BgColor = KeyboardVisualMetrics.DeckDepthColor(theme.Deck),
			CornerRadiusTopLeft = KeyboardVisualMetrics.DeckDepthCornerRadius,
			CornerRadiusTopRight = KeyboardVisualMetrics.DeckDepthCornerRadius,
			CornerRadiusBottomLeft = KeyboardVisualMetrics.DeckDepthCornerRadius,
			CornerRadiusBottomRight = KeyboardVisualMetrics.DeckDepthCornerRadius,
		};
		_deckLip!.AddThemeStyleboxOverride("panel", lipStyle);

		var deckStyle = new StyleBoxFlat
		{
			BgColor = theme.Deck,
			CornerRadiusTopLeft = KeyboardVisualMetrics.DeckCornerRadius,
			CornerRadiusTopRight = KeyboardVisualMetrics.DeckCornerRadius,
			CornerRadiusBottomLeft = KeyboardVisualMetrics.DeckCornerRadius,
			CornerRadiusBottomRight = KeyboardVisualMetrics.DeckCornerRadius,
		};
		_deck!.AddThemeStyleboxOverride("panel", deckStyle);

		var textAtlas = GetTextAtlasTexture(_activeDesign.TextAtlasPng);
		foreach (var definition in _layout!.Keys)
		{
			var keyCap = _keyCaps[definition.Id];
			var displayRect = KeyboardTypeCatalog.DisplayRect(definition, displayKeyboardType, _layout.Keys);
			if (keyCap.Position != displayRect.Position || keyCap.Size != displayRect.Size)
			{
				keyCap.Configure(displayRect);
			}
			keyCap.Visible = KeyboardTypeCatalog.Includes(definition.Id, displayKeyboardType, _layout.Keys);
			keyCap.ApplyTheme(theme, _activeDesign.Resolve(definition), false);
			keyCap.ApplyTextAtlas(
				KeyboardTypeCatalog.Includes(definition.Id, displayKeyboardType, _layout.Keys)
					? KeyboardTextAtlas.SelectDisplayTexture(_activeDesign, definition, textAtlas)
					: null,
				KeyboardTextAtlas.GetImportRegion(definition));
		}

		if (_toolbarThemes.Count == 0 || !_settingsOpen)
		{
			RefreshToolbarThemes();
		}
		else
		{
			_toolbar?.ApplyThemes(_toolbarThemes, _activeDesign.Id, _settings.Scale);
		}
		SetStatus($"主题：{_activeDesign.DisplayName} · {displayKeyboardType} 键");
	}

	private Texture2D? GetTextAtlasTexture(byte[]? png)
	{
		if (png is null)
		{
			_cachedTextAtlasPng = null;
			_cachedTextAtlasTexture = null;
			return null;
		}

		if (_cachedTextAtlasPng is not null && _cachedTextAtlasTexture is not null &&
			(_cachedTextAtlasPng == png || _cachedTextAtlasPng.AsSpan().SequenceEqual(png)))
		{
			return _cachedTextAtlasTexture;
		}

		_cachedTextAtlasTexture = KeyboardTextAtlas.CreateTexture(png);
		_cachedTextAtlasPng = _cachedTextAtlasTexture is null ? null : png;
		return _cachedTextAtlasTexture;
	}

	private void ChangeDeviceLayout(KeyboardLayoutMode mode, int manualKeyboardType)
	{
		_settings.LayoutMode = mode;
		_settings.ManualKeyboardType = manualKeyboardType;
		ApplyDesign(_activeDesign);
		if (!_embeddedInEditor)
		{
			_settings.SaveKeyboardLayout();
		}
	}

	private void RedetectDeviceLayout()
	{
		_deviceLayoutSuggestion = WindowsKeyboardLayoutDetector.Detect();
		if (_settings.LayoutMode == KeyboardLayoutMode.Automatic)
		{
			ApplyDesign(_activeDesign);
		}
		_settingsPanel?.SetDeviceLayoutState(
			_settings.LayoutMode, _settings.ManualKeyboardType, _deviceLayoutSuggestion);
	}

	private void RefreshToolbarThemes()
	{
		_toolbarThemes.Clear();
		_toolbarThemes.AddRange(KeyboardThemeLibraryCatalog.Load());
		_toolbar?.ApplyThemes(_toolbarThemes, _activeDesign.Id, _settings.Scale);
	}

	private void ApplyKeyboardGeometry(int keyboardType)
	{
		if (_layout is null || _deck is null || _deckLip is null || _keyLayer is null)
		{
			return;
		}

		var visibleBounds = KeyboardTypeCatalog.VisibleBounds(keyboardType, _layout.Keys);
		var deckSize = visibleBounds.Size + KeyboardVisualMetrics.DeckPadding * 2f;
		_deck.Size = deckSize;
		_deckLip.Size = deckSize;
		_keyLayer.Position = KeyboardVisualMetrics.DeckPadding - visibleBounds.Position;
		_keyLayer.Size = visibleBounds.Size;
		if (_toolbar is not null)
		{
			_toolbar.FitToKeyboard(deckSize.X);
			_toolbar.Position = new Vector2(
				DeckPosition.X + (deckSize.X - _toolbar.Size.X) * 0.5f,
				4f);
		}
	}

	private void SetPendingLock(bool locked)
	{
		if (_embeddedInEditor)
		{
			_pendingLocked = false;
			_toolbar?.SetLockState(false);
			SetStatus("嵌入预览不能启用桌面穿透锁定");
			return;
		}

		_pendingLocked = locked;
		_toolbar?.SetLockState(locked);
		SetStatus(locked ? "按 OK 后锁定键盘位置" : "按 OK 后保留键盘拖动");
	}

	private void ApplyWindowMode(bool locked, bool showToolbarWhenUnlocked = true)
	{
		if (locked && _settingsOpen)
		{
			_windowLocked = true;
			CloseSettings();
			return;
		}
		_windowLocked = _embeddedInEditor ? false : locked;
		_toolbar?.SetLockState(_embeddedInEditor ? false : _pendingLocked);

		if (_embeddedInEditor)
		{
			if (!_settingsOpen && showToolbarWhenUnlocked)
			{
				_toolbar!.ShowForEdit(animate: true);
			}
			else if (!_settingsOpen)
			{
				_toolbar!.HideForLocked(animate: true);
			}
			_deck!.MouseDefaultCursorShape = CursorShape.Arrow;
			SetStatus("嵌入预览不支持桌面窗口拖动和穿透；请使用外部启动文件");
			return;
		}

		_deck!.MouseDefaultCursorShape = _windowLocked ? CursorShape.Arrow : CursorShape.Move;
		if (_windowLocked || !showToolbarWhenUnlocked)
		{
			_toolbar!.HideForLocked(animate: true);
		}
		else
		{
			if (!_settingsOpen)
			{
				_toolbar!.ShowForEdit(animate: true);
			}
		}
		DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
		DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, !_settingsOpen);
		DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Transparent, true);
		SetDesktopClickThrough(_windowLocked);
		SetStatus(_windowLocked ? "已锁定 · Ctrl+Shift+F10 唤醒 Bar 条" : "可拖动 · 滚轮缩放");
	}

	private void SetDesktopClickThrough(bool enabled)
	{
		if (_embeddedInEditor || string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		// Apply NoFocus before MousePassthrough. On Windows these operations both
		// refresh extended window styles, so pass-through must be the final Godot flag.
		DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.NoFocus, enabled);
		DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.MousePassthrough, enabled);

		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		var nativeHandle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
		if (!WindowsWindowInputMode.Apply(
			new IntPtr(unchecked((long)nativeHandle)),
			enabled,
			clipWindowEdge: !_settingsOpen,
			out var error))
		{
			GD.PushWarning($"Windows desktop click-through could not be updated: {error}");
		}
	}

	private void RefreshDesktopWindowInputModeAfterResize()
	{
		if (_embeddedInEditor || _settingsOpen ||
			string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		SetDesktopClickThrough(_windowLocked);
	}

	private void UpdateCursorOverlapFade(float delta)
	{
		if (_deck is null || _deckLip is null)
		{
			return;
		}

		var targetAlpha = 1f;
		if (_windowLocked)
		{
			var windowPosition = DisplayServer.WindowGetPosition();
			var windowSize = DisplayServer.WindowGetSize();
			var scaleX = windowSize.X / (float)BaseWindowSize.X;
			var scaleY = windowSize.Y / (float)BaseWindowSize.Y;
			var keyboardScreenRect = new Rect2(
				windowPosition.X + DeckPosition.X * scaleX,
				windowPosition.Y + DeckPosition.Y * scaleY,
				_deck.Size.X * scaleX,
				(_deck.Size.Y + KeyboardVisualMetrics.DeckDepth) * scaleY);

			if (keyboardScreenRect.HasPoint(DisplayServer.MouseGetPosition()))
			{
				targetAlpha = CursorOverlapAlpha;
			}
		}

		var blend = 1f - Mathf.Exp(-delta * (targetAlpha < _visualAlpha ? 18f : 11f));
		_visualAlpha = Mathf.Lerp(_visualAlpha, targetAlpha, blend);
		if (Mathf.Abs(_visualAlpha - targetAlpha) < 0.002f)
		{
			_visualAlpha = targetAlpha;
		}

		var modulation = new Color(1f, 1f, 1f, _visualAlpha);
		_deck.Modulate = modulation;
		_deckLip.Modulate = modulation;
	}

	private void ConfirmEdits()
	{
		KeyboardThemeStore.SaveActive(_activeDesign);
		if (!_embeddedInEditor)
		{
			_settings.WindowPosition = DisplayServer.WindowGetPosition();
		}
		_settings.Locked = _pendingLocked;
		ApplyWindowMode(_settings.Locked, showToolbarWhenUnlocked: false);
		if (!_embeddedInEditor)
		{
			_settings.Save();
		}
		SetStatus("当前设置已保存");
	}

	private void ChangeScale(float amount)
	{
		_settings.WindowPosition = DisplayServer.WindowGetPosition();
		ApplyScale(Mathf.Clamp(_settings.Scale + amount, OverlaySettings.MinScale, OverlaySettings.MaxScale), reposition: false);
		_toolbar?.SetScale(_settings.Scale);
		SetStatus($"缩放：{Mathf.RoundToInt(_settings.Scale * 100f)}%");
	}

	private void ApplyScale(float scale, bool reposition)
	{
		_settings.Scale = Mathf.Clamp(scale, OverlaySettings.MinScale, OverlaySettings.MaxScale);
		if (_embeddedInEditor || _settingsOpen)
		{
			if (_embeddedInEditor)
			{
				SetStatus("嵌入预览不能调整系统窗口大小；请使用“调整键盘位置”启动文件");
			}
			return;
		}

		var windowSize = new Vector2I(
			Mathf.RoundToInt(BaseWindowSize.X * _settings.Scale),
			Mathf.RoundToInt(BaseWindowSize.Y * _settings.Scale));
		DisplayServer.WindowSetSize(windowSize);
		CallDeferred(nameof(RefreshDesktopWindowInputModeAfterResize));
		foreach (var keyCap in _keyCaps.Values)
		{
			keyCap.QueueRedraw();
		}

		if (reposition)
		{
			var usable = DisplayServer.ScreenGetUsableRect();
			DisplayServer.WindowSetPosition(new Vector2I(
				usable.End.X - windowSize.X - 20,
				usable.End.Y - windowSize.Y - 20));
		}
		else if (_settings.WindowPosition.X >= 0)
		{
			DisplayServer.WindowSetPosition(_settings.WindowPosition);
		}
	}

	private void OnEditSurfaceGuiInput(InputEvent @event)
	{
		if (_windowLocked || _settingsOpen)
		{
			return;
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
			{
				if (_embeddedInEditor)
				{
					SetStatus("嵌入预览无法移动系统窗口；请双击“调整键盘位置.cmd”");
					AcceptEvent();
					return;
				}

				DisplayServer.WindowStartDrag();
				AcceptEvent();
			}
			else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				ChangeScale(0.05f);
				AcceptEvent();
			}
			else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				ChangeScale(-0.05f);
				AcceptEvent();
			}
		}
	}

	private void OpenSettings()
	{
		if (_settingsStage != SettingsStage.Hidden || _layout is null)
		{
			return;
		}

		_settingsStage = SettingsStage.Opening;
		_settings.WindowPosition = DisplayServer.WindowGetPosition();
		var animate = !_embeddedInEditor;
		_toolbar!.HideForSettings(() => FinishOpenSettings(animate), animate && _toolbar.Visible);
	}

	private void FinishOpenSettings(bool animate)
	{
		if (_settingsStage != SettingsStage.Opening)
		{
			return;
		}

		_deck!.Visible = false;
		_deckLip!.Visible = false;

		if (!_embeddedInEditor)
		{
			SetDesktopClickThrough(false);
			DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, false);
			GetWindow().ContentScaleSize = ThemeSettingsView.WindowSize;
			DisplayServer.WindowSetSize(ThemeSettingsView.WindowSize);
			if (!string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
			{
				var usable = DisplayServer.ScreenGetUsableRect();
				var current = DisplayServer.WindowGetPosition();
				DisplayServer.WindowSetPosition(new Vector2I(
					Mathf.Clamp(current.X, usable.Position.X, usable.End.X - ThemeSettingsView.WindowSize.X),
					Mathf.Clamp(current.Y, usable.Position.Y, usable.End.Y - ThemeSettingsView.WindowSize.Y)));
			}
		}

		_settingsPanel!.Open(
			_activeDesign, _layout!,
			_settings.LayoutMode, _settings.ManualKeyboardType, _deviceLayoutSuggestion,
			animate);
		_settingsStage = SettingsStage.Visible;

		if (!_embeddedInEditor)
		{
			CallDeferred(nameof(ActivateSettingsWindow));
		}
	}

	private void ActivateSettingsWindow()
	{
		if (!_settingsOpen || _embeddedInEditor)
		{
			return;
		}

		// NoFocus and mouse pass-through are asynchronous on Windows. Reapply them
		// after the resize so the real Godot controls receive pointer input at once.
		SetDesktopClickThrough(false);
		if (!string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
		{
			DisplayServer.WindowMoveToForeground();
		}
	}

	private void CloseSettings()
	{
		if (_settingsStage is SettingsStage.Hidden or SettingsStage.Closing)
		{
			return;
		}

		var persistDesign = _settingsStage == SettingsStage.Visible;
		_settingsStage = SettingsStage.Closing;
		_settingsPanel!.AnimateClose(() => CompleteCloseSettings(persistDesign),
			animate: !_embeddedInEditor);
	}

	private void CompleteCloseSettings(bool persistDesign)
	{
		if (persistDesign)
		{
			_activeDesign = _settingsPanel!.CurrentDesign;
			KeyboardThemeStore.SaveActive(_activeDesign);
			RefreshToolbarThemes();
		}
		_settingsStage = SettingsStage.Hidden;
		_deck!.Visible = true;
		_deckLip!.Visible = true;

		if (!_embeddedInEditor)
		{
			GetWindow().ContentScaleSize = BaseWindowSize;
			ApplyScale(_settings.Scale, reposition: false);
		}

		var wakeToolbar = _wakeAfterSettingsClose;
		_wakeAfterSettingsClose = false;
		ApplyWindowMode(wakeToolbar ? false : _windowLocked);
		if (wakeToolbar && !_embeddedInEditor)
		{
			CallDeferred(nameof(ActivateToolbarWindow));
		}
	}

	private void SetStatus(string text)
	{
		_toolbar?.SetStatus(text);
	}

	private static string? MapFocusedKey(InputEventKey keyEvent)
	{
		var physical = keyEvent.PhysicalKeycode;
		if (physical is >= Key.A and <= Key.Z)
		{
			return $"Key{physical.ToString().ToUpperInvariant()}";
		}

		if (physical is >= Key.Key0 and <= Key.Key9)
		{
			return $"Digit{(int)(physical - Key.Key0)}";
		}

		if (physical is >= Key.F1 and <= Key.F12)
		{
			return $"F{(int)(physical - Key.F1) + 1}";
		}

		return physical switch
		{
			Key.Escape => "Escape",
			Key.Space => "Space",
			Key.Enter => "Enter",
			Key.Backspace => "Backspace",
			Key.Tab => "Tab",
			Key.Shift => keyEvent.Location == KeyLocation.Right ? "ShiftRight" : "ShiftLeft",
			Key.Ctrl => keyEvent.Location == KeyLocation.Right ? "ControlRight" : "ControlLeft",
			Key.Alt => keyEvent.Location == KeyLocation.Right ? "AltRight" : "AltLeft",
			Key.Up => "ArrowUp",
			Key.Down => "ArrowDown",
			Key.Left => "ArrowLeft",
			Key.Right => "ArrowRight",
			_ => null,
		};
	}
}
