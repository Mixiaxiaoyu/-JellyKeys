using Godot;

namespace JellyKeyboardOverlay.UI;

public partial class ThemeSettingsView
{
	private void ApplyFigmaStyle()
	{
		_background.AddThemeStyleboxOverride("panel", MakeStyle(UiDesignTokens.Surface, 28));
		_previewPanel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		_previewPanel.MouseDefaultCursorShape = CursorShape.Arrow;
		_themeNameDisplay.AddThemeFontOverride("font", _uiFont);
		_themeNameDisplay.AddThemeFontSizeOverride("font_size", 21);
		_themeNameDisplay.AddThemeColorOverride("font_color", UiDesignTokens.Ink);
		_themeNameEdit.AddThemeFontOverride("font", _uiFont);
		_themeNameEdit.AddThemeFontSizeOverride("font_size", 20);
		_themeNameEdit.AddThemeColorOverride("font_color", UiDesignTokens.Ink);
		_themeNameEdit.AddThemeColorOverride("font_placeholder_color", Color.FromHtml("#999999"));
		_themeNameEdit.AddThemeStyleboxOverride("normal", MakeInputStyle(Color.FromHtml("#e5e5e7")));
		_themeNameEdit.AddThemeStyleboxOverride("focus", MakeInputStyle(Color.FromHtml("#a5a5a8")));

		foreach (var path in new[]
				 {
					 "DesignContent/HueLabel", "DesignContent/SaturationLabel", "DesignContent/ValueLabel",
				 })
		{
			var label = GetNode<Label>(path);
			label.AddThemeFontOverride("font", _uiFont);
			label.AddThemeFontSizeOverride("font_size", 28);
			label.AddThemeColorOverride("font_color", Colors.Black);
		}

		_aboutButton.AddThemeFontOverride("font", _uiFont);
		_aboutButton.AddThemeFontSizeOverride("font_size", 20);
		_aboutButton.AddThemeColorOverride("font_color", Color.FromHtml("#999999"));
		_aboutButton.AddThemeColorOverride("font_hover_color", UiDesignTokens.Ink);
		_aboutButton.AddThemeColorOverride("font_pressed_color", UiDesignTokens.Ink);
		_aboutButton.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		_aboutButton.AddThemeStyleboxOverride("hover", MakeStyle(Color.FromHtml("#e9e8ed"), 9));
		_aboutButton.AddThemeStyleboxOverride("pressed", MakeStyle(Color.FromHtml("#dad9df"), 9));
		_aboutButton.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

		var selectionHint = GetNode<Label>("DesignContent/PreviewPanel/SelectionHint");
		selectionHint.AddThemeFontOverride("font", _uiFont);
		selectionHint.AddThemeFontSizeOverride("font_size", 13);
		selectionHint.AddThemeColorOverride("font_color", Color.FromHtml("#99979f"));

		var deviceTitle = GetNode<Label>("LayoutContent/DeviceLayoutTitle");
		deviceTitle.AddThemeFontOverride("font", _uiFont);
		deviceTitle.AddThemeFontSizeOverride("font_size", 30);
		deviceTitle.AddThemeColorOverride("font_color", UiDesignTokens.Ink);
		var deviceSubtitle = GetNode<Label>("LayoutContent/DeviceLayoutSubtitle");
		deviceSubtitle.AddThemeFontOverride("font", _uiFont);
		deviceSubtitle.AddThemeFontSizeOverride("font_size", 16);
		deviceSubtitle.AddThemeColorOverride("font_color", Color.FromHtml("#88888f"));
		var deviceFieldLabel = GetNode<Label>("LayoutContent/DeviceLayoutFieldLabel");
		deviceFieldLabel.AddThemeFontOverride("font", _uiFont);
		deviceFieldLabel.AddThemeFontSizeOverride("font_size", 25);
		deviceFieldLabel.AddThemeColorOverride("font_color", UiDesignTokens.Ink);
		_deviceLayoutStatus.AddThemeFontOverride("font", _uiFont);
		_deviceLayoutStatus.AddThemeFontSizeOverride("font_size", 15);
		_deviceLayoutStatus.AddThemeColorOverride("font_color", Color.FromHtml("#88888f"));
		var previewStyle = MakeStyle(Color.FromHtml("#f0eff4"), 18);
		previewStyle.BorderColor = Color.FromHtml("#e9e8ee");
		previewStyle.BorderWidthLeft = 1;
		previewStyle.BorderWidthTop = 1;
		previewStyle.BorderWidthRight = 1;
		previewStyle.BorderWidthBottom = 1;
		_deviceLayoutPreviewPanel.AddThemeStyleboxOverride("panel", previewStyle);
		_deviceLayoutSelect.AddThemeStyleboxOverride("normal",
			MakeDeviceLayoutSelectStyle(Color.FromHtml("#ffffff")));
		_deviceLayoutSelect.AddThemeStyleboxOverride("hover",
			MakeDeviceLayoutSelectStyle(Color.FromHtml("#f4f3f7")));
		_deviceLayoutSelect.AddThemeStyleboxOverride("pressed",
			MakeDeviceLayoutSelectStyle(Color.FromHtml("#ecebf1")));
		_deviceLayoutSelect.AddThemeStyleboxOverride("hover_pressed",
			MakeDeviceLayoutSelectStyle(Color.FromHtml("#ecebf1")));
		_deviceLayoutSelect.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		_deviceLayoutSelect.AddThemeFontOverride("font", _uiFont);
		_deviceLayoutSelect.AddThemeFontSizeOverride("font_size", 15);
		_deviceLayoutSelect.AddThemeColorOverride("font_color", UiDesignTokens.Ink);
		_deviceLayoutSelect.AddThemeColorOverride("font_hover_color", UiDesignTokens.Ink);
		_deviceLayoutSelect.AddThemeColorOverride("font_pressed_color", UiDesignTokens.Ink);
		var layoutMenu = _deviceLayoutSelect.GetPopup();
		var menuStyle = MakeStyle(Color.FromHtml("#ffffff"), 11);
		menuStyle.BorderColor = Color.FromHtml("#e4e3ea");
		menuStyle.BorderWidthLeft = 1;
		menuStyle.BorderWidthTop = 1;
		menuStyle.BorderWidthRight = 1;
		menuStyle.BorderWidthBottom = 1;
		menuStyle.ContentMarginLeft = 6f;
		menuStyle.ContentMarginRight = 6f;
		menuStyle.ContentMarginTop = 6f;
		menuStyle.ContentMarginBottom = 6f;
		layoutMenu.AddThemeStyleboxOverride("panel", menuStyle);
		layoutMenu.AddThemeStyleboxOverride("hover", MakeStyle(Color.FromHtml("#eef8d5"), 7));
		layoutMenu.AddThemeFontOverride("font", _uiFont);
		layoutMenu.AddThemeFontSizeOverride("font_size", 15);
		layoutMenu.AddThemeColorOverride("font_color", UiDesignTokens.Ink);
		layoutMenu.AddThemeColorOverride("font_hover_color", UiDesignTokens.Ink);
		layoutMenu.AddThemeColorOverride("font_disabled_color", Color.FromHtml("#aaaab0"));
		ApplyCompactLightMenuButtonStyle(_deviceLayoutRescan);
		_deviceLayoutRescan.AddThemeFontSizeOverride("font_size", 13);
		_deviceLayoutRescan.AddThemeColorOverride("font_color", Color.FromHtml("#55555d"));
		_deviceLayoutRescan.AddThemeColorOverride("font_hover_color", UiDesignTokens.Ink);
		_deviceLayoutHint.AddThemeFontOverride("font", _uiFont);
		_deviceLayoutHint.AddThemeFontSizeOverride("font_size", 15);
		_deviceLayoutHint.AddThemeColorOverride("font_color", Color.FromHtml("#99979f"));

		var bottomButtons = new[]
		{
			_exportSpecificationButton, _importDesignButton, _newThemeButton, _saveThemeButton,
			_importThemeButton, _exportThemeButton, _applyThemeButton,
		};
		foreach (var button in bottomButtons)
		{
			ApplyBlackButtonStyle(button);
		}

		foreach (var slider in new[] { _hueSlider, _saturationSlider, _valueSlider })
		{
			ApplySliderStyle(slider);
		}

		var transparent = new StyleBoxEmpty();
		_closeButton.AddThemeStyleboxOverride("normal", transparent);
		_closeButton.AddThemeStyleboxOverride("hover", transparent);
		_closeButton.AddThemeStyleboxOverride("pressed", transparent);
		_closeButton.AddThemeStyleboxOverride("focus", transparent);
		_closeButton.MouseFilter = MouseFilterEnum.Stop;
		_closeButton.MouseDefaultCursorShape = CursorShape.PointingHand;
		_layoutTab.MouseFilter = MouseFilterEnum.Stop;
		_designTab.MouseFilter = MouseFilterEnum.Stop;
		_themesTab.MouseFilter = MouseFilterEnum.Stop;
		_layoutTab.MouseDefaultCursorShape = CursorShape.PointingHand;
		_designTab.MouseDefaultCursorShape = CursorShape.PointingHand;
		_themesTab.MouseDefaultCursorShape = CursorShape.PointingHand;
		ApplyTabStyle(_layoutTab, true);
		ApplyTabStyle(_designTab, false);
		ApplyTabStyle(_themesTab, false);
	}

	private void ApplyPreviewDeckStyle()
	{
		if (_previewDeck is null || _previewDeckDepth is null)
		{
			return;
		}

		var deckStyle = MakeStyle(_working.Palette.Deck, KeyboardVisualMetrics.DeckCornerRadius);
		if (_deckSelected)
		{
			deckStyle.BorderColor = Colors.Black;
			deckStyle.BorderWidthTop = 2;
			deckStyle.BorderWidthRight = 2;
			deckStyle.BorderWidthBottom = 2;
			deckStyle.BorderWidthLeft = 2;
		}

		_previewDeck.AddThemeStyleboxOverride("panel", deckStyle);
		_previewDeckDepth.AddThemeStyleboxOverride(
			"panel",
			MakeStyle(
				KeyboardVisualMetrics.DeckDepthColor(_working.Palette.Deck),
				KeyboardVisualMetrics.DeckDepthCornerRadius));
	}

	private void ApplyTabStyle(Button button, bool selected)
	{
		var style = MakeStyle(selected ? UiDesignTokens.Accent : Colors.Transparent, 12);
		button.AddThemeStyleboxOverride("normal", style);
		button.AddThemeStyleboxOverride("hover", MakeStyle(selected ? Color.FromHtml("#e4ff91") : Color.FromHtml("#e8e8e8"), 12));
		button.AddThemeStyleboxOverride("pressed", MakeStyle(selected ? Color.FromHtml("#c8ee52") : Color.FromHtml("#d4d4d4"), 12));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeFontOverride("font", _uiFont);
		button.AddThemeFontSizeOverride("font_size", 28);
		var contentColor = Colors.Black;
		button.AddThemeColorOverride("font_color", contentColor);
		button.AddThemeColorOverride("font_hover_color", contentColor);
		button.AddThemeColorOverride("font_pressed_color", contentColor);

		var label = button.GetNodeOrNull<Label>("Label");
		if (label is not null)
		{
			label.AddThemeFontOverride("font", _uiFont);
			label.AddThemeFontSizeOverride("font_size", 28);
			label.AddThemeColorOverride("font_color", contentColor);
		}

		var icon = button.GetNodeOrNull<TextureRect>("Icon");
		if (icon is not null)
		{
			var iconPath = ReferenceEquals(button, _layoutTab)
				? "res://assets/figma/settings_layout.svg"
				: ReferenceEquals(button, _designTab)
					? "res://assets/figma/settings_palette.svg"
					: "res://assets/figma/settings_knob.svg";
			icon.Texture = GD.Load<Texture2D>(iconPath);
			icon.Modulate = Colors.White;
		}
	}

	private void ApplyBlackButtonStyle(Button button)
	{
		button.AddThemeStyleboxOverride("normal", MakeStyle(Colors.Black, 16));
		button.AddThemeStyleboxOverride("hover", MakeStyle(Color.FromHtml("#252525"), 16));
		button.AddThemeStyleboxOverride("pressed", MakeStyle(Color.FromHtml("#404040"), 16));
		button.AddThemeStyleboxOverride("disabled", MakeStyle(Color.FromHtml("#b8b8bc"), 16));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeFontOverride("font", _uiFont);
		button.AddThemeFontSizeOverride("font_size", 24);
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", Colors.White);
	}

	private void ApplyCompactDarkMenuButtonStyle(Button button)
	{
		button.AddThemeStyleboxOverride("normal", MakeStyle(UiDesignTokens.Ink, 9));
		button.AddThemeStyleboxOverride("hover", MakeStyle(Color.FromHtml("#2b2b2b"), 9));
		button.AddThemeStyleboxOverride("pressed", MakeStyle(Color.FromHtml("#444444"), 9));
		button.AddThemeStyleboxOverride("disabled", MakeStyle(Color.FromHtml("#b8b8bc"), 9));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeFontOverride("font", _uiFont);
		button.AddThemeFontSizeOverride("font_size", 15);
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", Colors.White);
	}

	private void ApplyCompactLightMenuButtonStyle(Button button)
	{
		button.AddThemeStyleboxOverride("normal", MakeStyle(Colors.Transparent, 8));
		button.AddThemeStyleboxOverride("hover", MakeStyle(Color.FromHtml("#e9e8ed"), 8));
		button.AddThemeStyleboxOverride("pressed", MakeStyle(Color.FromHtml("#dad9df"), 8));
		button.AddThemeStyleboxOverride("disabled", MakeStyle(Colors.Transparent, 8));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeFontOverride("font", _uiFont);
		button.AddThemeFontSizeOverride("font_size", 15);
		button.AddThemeColorOverride("font_color", Color.FromHtml("#181818"));
		button.AddThemeColorOverride("font_hover_color", Colors.Black);
		button.AddThemeColorOverride("font_pressed_color", Colors.Black);
		button.AddThemeColorOverride("font_disabled_color", Color.FromHtml("#aaa9ae"));
	}

	private static void ApplyThemeCardStyle(Button button, bool selected)
	{
		var style = MakeStyle(selected ? Color.FromHtml("#ffffff") : Colors.Transparent, 12);
		style.BorderColor = Color.FromHtml("#c9c9c9");
		style.BorderWidthTop = selected ? 2 : 0;
		style.BorderWidthRight = selected ? 2 : 0;
		style.BorderWidthBottom = selected ? 2 : 0;
		style.BorderWidthLeft = selected ? 2 : 0;
		button.AddThemeStyleboxOverride("normal", style);
		var hover = (StyleBoxFlat)style.Duplicate();
		hover.BgColor = Color.FromHtml("#eeeeef");
		var pressed = (StyleBoxFlat)hover.Duplicate();
		pressed.BgColor = Color.FromHtml("#e1e1e3");
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
	}

	private static void ApplySliderStyle(HSlider slider)
	{
		var transparent = MakeStyle(Colors.Transparent, 8);
		transparent.ContentMarginTop = 8f;
		transparent.ContentMarginBottom = 8f;
		slider.AddThemeStyleboxOverride("slider", transparent);
		slider.AddThemeStyleboxOverride("grabber_area", MakeStyle(Colors.Transparent, 8));
		slider.AddThemeStyleboxOverride("grabber_area_highlight", MakeStyle(Colors.Transparent, 8));
		var knob = GD.Load<Texture2D>("res://assets/figma/settings_layers.svg");
		slider.AddThemeIconOverride("grabber", knob);
		slider.AddThemeIconOverride("grabber_highlight", knob);
	}

	private void InstallSliderTracks()
	{
		_hueTrack = AddSliderTrack(_hueSlider, "HueTrack");
		_saturationTrack = AddSliderTrack(_saturationSlider, "SaturationTrack");
		_valueTrack = AddSliderTrack(_valueSlider, "ValueTrack");

		_hueTrack.SetStops(
			(0f, Color.FromHsv(0f, 1f, 1f)),
			(1f / 6f, Color.FromHsv(1f / 6f, 1f, 1f)),
			(2f / 6f, Color.FromHsv(2f / 6f, 1f, 1f)),
			(3f / 6f, Color.FromHsv(3f / 6f, 1f, 1f)),
			(4f / 6f, Color.FromHsv(4f / 6f, 1f, 1f)),
			(5f / 6f, Color.FromHsv(5f / 6f, 1f, 1f)),
			(1f, Color.FromHsv(1f, 1f, 1f)));
		UpdateSliderTracks();
	}

	private static ColorGradientTrack AddSliderTrack(HSlider slider, string name)
	{
		var track = new ColorGradientTrack
		{
			Name = name,
			Position = slider.Position + new Vector2(0f, 8f),
			Size = new Vector2(slider.Size.X, 16f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		var parent = slider.GetParent();
		parent.AddChild(track);
		parent.MoveChild(track, slider.GetIndex());
		return track;
	}

	private void UpdateSliderTracks()
	{
		if (_saturationTrack is null || _valueTrack is null)
		{
			return;
		}

		var hue = (float)(_hueSlider.Value / 360.0);
		_saturationTrack.SetStops(
			(0f, Color.FromHsv(hue, 0f, 0.62f)),
			(1f, Color.FromHsv(hue, 1f, 1f)));
		_valueTrack.SetStops(
			(0f, Colors.Black),
			(1f, Colors.White));
	}

	private void InstallWindowDragRegion()
	{
		GuiInput += OnWindowDragInput;
		var dragRegion = new Control
		{
			Name = "WindowDragRegion",
			Position = Vector2.Zero,
			Size = new Vector2(850f, 96f),
			ZIndex = 9,
			MouseFilter = MouseFilterEnum.Stop,
			MouseDefaultCursorShape = CursorShape.Move,
			TooltipText = "拖动设置面板",
		};
		dragRegion.GuiInput += OnWindowDragInput;
		AddChild(dragRegion);
	}

	private void OnWindowDragInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton ||
			mouseButton.ButtonIndex != MouseButton.Left || !mouseButton.Pressed)
		{
			return;
		}

		if (Engine.IsEmbeddedInEditor())
		{
			return;
		}

		DisplayServer.WindowStartDrag();
		AcceptEvent();
	}

	private static StyleBoxFlat MakeDeviceLayoutSelectStyle(Color color)
	{
		var style = MakeStyle(color, 11);
		style.BorderColor = Color.FromHtml("#e4e3ea");
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.ContentMarginLeft = 12f;
		style.ContentMarginRight = 12f;
		return style;
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

	private static StyleBoxFlat MakeInputStyle(Color borderColor)
	{
		var style = MakeStyle(Colors.White, 12);
		style.BorderColor = borderColor;
		style.BorderWidthTop = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthBottom = 2;
		style.BorderWidthLeft = 2;
		style.ContentMarginLeft = 14f;
		style.ContentMarginRight = 14f;
		return style;
	}}
