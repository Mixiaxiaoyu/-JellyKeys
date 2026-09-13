using Godot;

namespace JellyKeyboardOverlay.UI;

public partial class EditToolbarView : Control
{
	private static readonly Vector2 NominalRailSize = new(940f, 80f);
	private const float MinimumRailWidth = 800f;
	private static readonly Color Surface = UiDesignTokens.Surface;
	private static readonly Color Tray = Color.FromHtml("#dddddd");
	private const int VisibleThemeCount = 5;

	private Panel _rail = null!;
	private Control _themeScrollArea = null!;
	private Panel _scaleTray = null!;
	private Button _dragButton = null!;
	private Button _confirmButton = null!;
	private Button _lockButton = null!;
	private Button _settingsButton = null!;
	private Button _collapseButton = null!;
	private Button _smallerButton = null!;
	private Button _largerButton = null!;
	private Label _scaleLabel = null!;
	private readonly List<Button> _swatches = new();
	private readonly List<KeyboardDesignData> _themes = new();
	private readonly int[] _visibleThemeIndices = Enumerable.Repeat(-1, VisibleThemeCount).ToArray();
	private readonly List<CanvasItem> _collapsibleItems = new();
	private UiMotionScope _motionScope = null!;
	private bool _collapsed;
	private int _themeWindowStart;
	private string _selectedThemeId = string.Empty;
	private Vector2 _railSize = NominalRailSize;

	public event Action<KeyboardDesignData>? ThemeSelected;
	public event Action? ConfirmRequested;
	public event Action<float>? ScaleStepRequested;
	public event Action<bool>? LockChanged;
	public event Action? SettingsRequested;
	public event Action<InputEvent>? DragSurfaceInput;

	public override void _Ready()
	{
		_motionScope = new UiMotionScope(this);
		_rail = GetNode<Panel>("Rail");
		_themeScrollArea = GetNode<Control>("ThemeScrollArea");
		_scaleTray = GetNode<Panel>("ScaleTray");
		_dragButton = GetNode<Button>("DragButton");
		_confirmButton = GetNode<Button>("ConfirmButton");
		_lockButton = GetNode<Button>("LockButton");
		_settingsButton = GetNode<Button>("SettingsButton");
		_collapseButton = GetNode<Button>("CollapseButton");
		_smallerButton = GetNode<Button>("ScaleTray/SmallerButton");
		_largerButton = GetNode<Button>("ScaleTray/LargerButton");
		_scaleLabel = GetNode<Label>("ScaleTray/ScaleLabel");

		for (var index = 0; index < VisibleThemeCount; index++)
		{
			var button = GetNode<Button>($"Theme{index}");
			var capturedSlot = index;
			button.Pressed += () => SelectThemeSlot(capturedSlot);
			button.GuiInput += OnThemeScrollInput;
			_swatches.Add(button);
		}
		_themeScrollArea.GuiInput += OnThemeScrollInput;

		_dragButton.GuiInput += @event => DragSurfaceInput?.Invoke(@event);
		_rail.GuiInput += @event => DragSurfaceInput?.Invoke(@event);
		_confirmButton.Pressed += () => ConfirmRequested?.Invoke();
		_smallerButton.Pressed += () => ScaleStepRequested?.Invoke(-0.05f);
		_largerButton.Pressed += () => ScaleStepRequested?.Invoke(0.05f);
		_lockButton.Toggled += locked =>
		{
			UpdateLockVisual(locked);
			LockChanged?.Invoke(locked);
		};
		_settingsButton.Pressed += () => SettingsRequested?.Invoke();
		_collapseButton.Pressed += ToggleCollapsed;

		_collapsibleItems.Add(_rail);
		_collapsibleItems.Add(_dragButton);
		_collapsibleItems.Add(_themeScrollArea);
		_collapsibleItems.AddRange(_swatches);
		_collapsibleItems.Add(_scaleTray);
		_collapsibleItems.Add(_confirmButton);
		_collapsibleItems.Add(_lockButton);
		_collapsibleItems.Add(_settingsButton);

		foreach (var button in GetChildren().OfType<Button>().Concat(_scaleTray.GetChildren().OfType<Button>()))
		{
			AddButtonMotion(button);
		}

		ApplyFigmaStyle();
	}

	public void ApplyThemes(IReadOnlyList<KeyboardDesignData> themes, string activeThemeId, float scale)
	{
		_themes.Clear();
		_themes.AddRange(themes.Select(theme => theme.Clone()));
		_selectedThemeId = activeThemeId;

		var activeIndex = _themes.FindIndex(theme => string.Equals(theme.Id, activeThemeId, StringComparison.Ordinal));
		var maximumStart = Mathf.Max(0, _themes.Count - VisibleThemeCount);
		if (activeIndex >= 0 && (activeIndex < _themeWindowStart || activeIndex >= _themeWindowStart + VisibleThemeCount))
		{
			_themeWindowStart = Mathf.Clamp(activeIndex - VisibleThemeCount / 2, 0, maximumStart);
		}
		else
		{
			_themeWindowStart = Mathf.Clamp(_themeWindowStart, 0, maximumStart);
		}

		RefreshThemeSwatches();
		SetScale(scale);
	}

	public void SetScale(float scale)
	{
		_scaleLabel.Text = $"{Mathf.RoundToInt(scale * 100f)}%";
	}

	public void SetLockState(bool locked)
	{
		_lockButton.SetPressedNoSignal(locked);
		UpdateLockVisual(locked);
	}

	public void SetStatus(string text)
	{
		// The Figma bar intentionally has no visible status field.
	}

	public void FitToKeyboard(float keyboardWidth)
	{
		var width = Mathf.Clamp(keyboardWidth, MinimumRailWidth, NominalRailSize.X);
		_railSize = new Vector2(width, NominalRailSize.Y);
		CustomMinimumSize = _railSize;
		Size = _railSize;
		_rail.Size = _railSize;

		// Preserve the current control sizes and compress only the horizontal gaps.
		// This keeps every icon easy to hit when a compact keyboard is selected.
		var positionScale = (width - 40f) / (NominalRailSize.X - 40f);
		float RemapX(float nominalX) => 20f + (nominalX - 20f) * positionScale;

		_dragButton.Position = new Vector2(RemapX(20f), 22f);
		var themeAreaLeft = RemapX(70f);
		var themeAreaRight = RemapX(414f);
		_themeScrollArea.Position = new Vector2(themeAreaLeft, 10f);
		_themeScrollArea.Size = new Vector2(themeAreaRight - themeAreaLeft, 60f);
		for (var index = 0; index < _swatches.Count; index++)
		{
			_swatches[index].Position = new Vector2(RemapX(82f + 68f * index), 18f);
		}

		var trayLeft = RemapX(432f);
		var trayRight = RemapX(628f);
		_scaleTray.Position = new Vector2(trayLeft, 16f);
		_scaleTray.Size = new Vector2(trayRight - trayLeft, 48f);
		_smallerButton.Position = Vector2.Zero;
		_smallerButton.Size = new Vector2(48f, 48f);
		_largerButton.Position = new Vector2(_scaleTray.Size.X - 48f, 0f);
		_largerButton.Size = new Vector2(48f, 48f);
		_scaleLabel.Position = new Vector2(48f, 0f);
		_scaleLabel.Size = new Vector2(_scaleTray.Size.X - 96f, 48f);

		_confirmButton.Position = new Vector2(RemapX(658f), 16f);
		_lockButton.Position = new Vector2(RemapX(726f), 16f);
		_settingsButton.Position = new Vector2(RemapX(794f), 16f);
		_collapseButton.Position = _collapsed
			? new Vector2((width - _collapseButton.Size.X) * 0.5f, 8f)
			: new Vector2(RemapX(862f), 16f);
		PivotOffset = _railSize * 0.5f;
	}

	public void ShowForEdit(bool animate = true)
	{
		Visible = true;
		_motionScope.CancelAll();
		if (!animate)
		{
			Modulate = Colors.White;
			Scale = Vector2.One;
			return;
		}

		PivotOffset = _railSize * 0.5f;
		Modulate = new Color(1f, 1f, 1f, 0f);
		Scale = new Vector2(0.985f, 0.88f);
		var motion = _motionScope.StartRoot();
		motion.TweenProperty(this, "modulate:a", 1f, 0.18)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		motion.TweenProperty(this, "scale", Vector2.One, 0.28)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		AnimateItemsIn();
	}

	public void HideForLocked(bool animate = true)
	{
		HideWithMotion(null, animate);
	}

	public void HideForSettings(Action onHidden, bool animate = true)
	{
		HideWithMotion(onHidden, animate);
	}

	private void HideWithMotion(Action? onHidden, bool animate)
	{
		_motionScope.CancelAll();
		if (!animate || !Visible)
		{
			Visible = false;
			Modulate = Colors.White;
			Scale = Vector2.One;
			onHidden?.Invoke();
			return;
		}

		var motion = _motionScope.StartRoot();
		motion.TweenProperty(this, "modulate:a", 0f, 0.16)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.In);
		motion.TweenProperty(this, "scale", new Vector2(0.985f, 0.9f), 0.20)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.In);
		var items = GetMotionItems().Reverse().ToArray();
		var itemMotion = _motionScope.StartItems();
		for (var index = 0; index < items.Length; index++)
		{
			var item = items[index];
			_motionScope.Remember(item);
			itemMotion.TweenProperty(item, "modulate:a", 0f, 0.12)
				.SetDelay(index * 0.006)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
		}
		motion.Finished += () =>
		{
			_motionScope.CancelItems();
			Visible = false;
			Modulate = Colors.White;
			Scale = Vector2.One;
			_motionScope.CompleteRoot(motion);
			onHidden?.Invoke();
		};
	}

	private void AnimateItemsIn()
	{
		var items = GetMotionItems();
		var tween = _motionScope.StartItems();
		var index = 0;
		foreach (var item in items)
		{
			var target = item.Modulate;
			_motionScope.Remember(item);
			item.Modulate = new Color(target.R, target.G, target.B, 0f);
			tween.TweenProperty(item, "modulate:a", target.A, 0.16)
				.SetDelay(0.025 + index * 0.018)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
			index++;
		}
		tween.Finished += () => _motionScope.CompleteItems(tween);
	}

	private CanvasItem[] GetMotionItems()
	{
		return new CanvasItem[]
		{
			_dragButton, _swatches[0], _swatches[1], _swatches[2], _swatches[3], _swatches[4],
			_scaleTray, _confirmButton, _lockButton, _settingsButton, _collapseButton,
		}.Where(item => item.Visible).ToArray();
	}

	private void ToggleCollapsed()
	{
		_collapsed = !_collapsed;
		foreach (var item in _collapsibleItems)
		{
			item.Visible = !_collapsed;
		}

		_collapseButton.PivotOffset = _collapseButton.Size * 0.5f;
		if (_collapsed)
		{
			_collapseButton.Position = new Vector2((_railSize.X - _collapseButton.Size.X) * 0.5f, 8f);
			_collapseButton.Rotation = Mathf.Pi;
		}
		else
		{
			var positionScale = (_railSize.X - 40f) / (NominalRailSize.X - 40f);
			_collapseButton.Position = new Vector2(20f + (862f - 20f) * positionScale, 16f);
			_collapseButton.Rotation = 0f;
		}
	}

	private void ApplyFigmaStyle()
	{
		_rail.AddThemeStyleboxOverride("panel", MakeStyle(Surface, 28));
		_scaleTray.AddThemeStyleboxOverride("panel", MakeStyle(Tray, 12));

		var transparent = new StyleBoxEmpty();
		foreach (var button in GetChildren().OfType<Button>().Concat(_scaleTray.GetChildren().OfType<Button>()))
		{
			button.AddThemeStyleboxOverride("normal", transparent);
			button.AddThemeStyleboxOverride("hover", transparent);
			button.AddThemeStyleboxOverride("pressed", transparent);
			button.AddThemeStyleboxOverride("focus", transparent);
		}

		var font = new SystemFont
		{
			FontNames = new[] { "Baloo Chettan 2", "Microsoft YaHei UI", "Segoe UI" },
			FontWeight = 700,
		};
		_scaleLabel.AddThemeFontOverride("font", font);
		_scaleLabel.AddThemeFontSizeOverride("font_size", 24);
		_scaleLabel.AddThemeColorOverride("font_color", Colors.Black);

		RefreshThemeSwatches();
	}

	private void OnThemeScrollInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
		{
			return;
		}

		var direction = mouseButton.ButtonIndex switch
		{
			MouseButton.WheelDown => 1,
			MouseButton.WheelUp => -1,
			_ => 0,
		};
		if (direction == 0)
		{
			return;
		}

		var maximumStart = Mathf.Max(0, _themes.Count - VisibleThemeCount);
		var nextStart = Mathf.Clamp(_themeWindowStart + direction, 0, maximumStart);
		if (nextStart != _themeWindowStart)
		{
			_themeWindowStart = nextStart;
			RefreshThemeSwatches();
		}
		AcceptEvent();
	}

	private void SelectThemeSlot(int slot)
	{
		if (slot < 0 || slot >= _visibleThemeIndices.Length)
		{
			return;
		}

		var themeIndex = _visibleThemeIndices[slot];
		if (themeIndex < 0 || themeIndex >= _themes.Count)
		{
			return;
		}

		ThemeSelected?.Invoke(_themes[themeIndex].Clone());
	}

	private void RefreshThemeSwatches()
	{
		for (var slot = 0; slot < _swatches.Count; slot++)
		{
			var themeIndex = _themeWindowStart + slot;
			var button = _swatches[slot];
			if (themeIndex >= _themes.Count)
			{
				_visibleThemeIndices[slot] = -1;
				button.Visible = false;
				continue;
			}

			var design = _themes[themeIndex];
			_visibleThemeIndices[slot] = themeIndex;
			button.Visible = true;
			button.TooltipText = string.Empty;
			ApplySwatchStyle(
				button,
				RepresentativeColor(design),
				string.Equals(design.Id, _selectedThemeId, StringComparison.Ordinal));
		}
	}

	private static Color RepresentativeColor(KeyboardDesignData design)
	{
		return design.Keys.Values.FirstOrDefault(appearance => appearance.HasTopColor)?.TopColor
			?? design.Palette.KeyTop;
	}

	private static void ApplySwatchStyle(Button button, Color color, bool selected)
	{
		var normal = MakeStyle(color, 12);
		normal.BorderColor = selected ? Color.FromHtml("#3f5500") : Colors.Transparent;
		normal.BorderWidthTop = selected ? 3 : 0;
		normal.BorderWidthRight = selected ? 3 : 0;
		normal.BorderWidthBottom = selected ? 3 : 0;
		normal.BorderWidthLeft = selected ? 3 : 0;

		var hover = (StyleBoxFlat)normal.Duplicate();
		hover.BgColor = color.Lightened(0.08f);
		var pressed = (StyleBoxFlat)normal.Duplicate();
		pressed.BgColor = color.Darkened(0.05f);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
	}

	private static StyleBoxFlat MakeStyle(Color color, int radius)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius,
		};
	}

	private static void AddButtonMotion(BaseButton button)
	{
		button.MouseEntered += () => AnimateButton(button, new Vector2(1.035f, 1.035f), 0.10);
		button.MouseExited += () => AnimateButton(button, Vector2.One, 0.12);
		button.ButtonDown += () => AnimateButton(button, new Vector2(0.94f, 0.92f), 0.06);
		button.ButtonUp += () => AnimateButton(button, button.IsHovered() ? new Vector2(1.035f, 1.035f) : Vector2.One, 0.10);
	}

	private void UpdateLockVisual(bool locked)
	{
		_lockButton.Modulate = locked
			? Colors.White
			: new Color(1f, 1f, 1f, 0.58f);
	}

	private static void AnimateButton(Control button, Vector2 target, double duration)
	{
		button.PivotOffset = button.Size * 0.5f;
		button.CreateTween()
			.TweenProperty(button, "scale", target, duration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}
}
