using Godot;

namespace JellyKeyboardOverlay.UI;

public partial class KeyCapView : Control
{
    private readonly Label _label;
    private readonly TextureRect _textTexture;
    private Texture2D? _currentTextAtlas;
    private Rect2 _currentTextRegion;
    private KeyboardThemeData _theme = KeyboardThemes.Presets[0];
    private Color _topColor;
    private Color _edgeColor;
    private Color _inkColor;
    private Vector2 _restPosition;
    private float _motionTime;
    private bool _held;
    private bool _releasing;
    private bool _selected;
    private bool _selectable;
    private readonly float _lean;

    public string KeyId { get; }
    public event Action<string, bool>? SelectionRequested;
    public event Action<string>? ContextMenuRequested;

    public KeyCapView(string keyId, string label)
    {
        KeyId = keyId;
        Name = keyId;
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = false;
        SetProcess(false);

        _lean = StableLean(keyId);
        _topColor = _theme.KeyTop;
        _edgeColor = _theme.KeyEdge;
        _inkColor = _theme.KeyInk;

        _textTexture = new TextureRect
        {
            Name = "TextArtwork",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            // The supplied key lettering is pixel art; linear filtering softens it at fractional scales.
            TextureFilter = TextureFilterEnum.Nearest,
            Visible = false,
        };
        _textTexture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_textTexture);

        _label = new Label
        {
            Text = label,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_label);
    }

    public void Configure(Rect2 rect)
    {
        Position = rect.Position;
        Size = rect.Size;
        _restPosition = rect.Position;
        PivotOffset = new Vector2(Size.X * 0.5f, Size.Y * 0.92f);
        _label.OffsetBottom = 0f;
        _textTexture.OffsetLeft = 5f;
        _textTexture.OffsetTop = 5f;
        _textTexture.OffsetRight = -5f;
        _textTexture.OffsetBottom = -7f;
        _label.AddThemeFontSizeOverride("font_size", LabelSizeFor(rect.Size.X));
        QueueRedraw();
    }

    public void ApplyTheme(KeyboardThemeData theme)
    {
        ApplyTheme(theme, null, false);
    }

    public void ApplyTheme(KeyboardThemeData theme, KeyAppearanceData? appearance, bool selected)
    {
        _theme = theme;
        _topColor = appearance?.HasTopColor == true ? appearance.TopColor : theme.KeyTop;
        _edgeColor = appearance?.HasTopColor == true ? EdgeColorFor(_topColor) : theme.KeyEdge;
        _inkColor = appearance?.HasInkColor == true ? appearance.InkColor : theme.KeyInk;
        _selected = selected;
        if (appearance is not null)
        {
            _label.Text = appearance.Label;
            _label.AddThemeFontSizeOverride("font_size", Mathf.Clamp(appearance.FontSize, 7, 24));
        }

        _label.AddThemeColorOverride("font_color", _inkColor);
        QueueRedraw();
    }

    public void ApplyTextAtlas(Texture2D? atlas, Rect2 region)
    {
        if (atlas is null)
        {
            _currentTextAtlas = null;
            _textTexture.Texture = null;
            _textTexture.Visible = false;
            _label.Visible = true;
            return;
        }

        if (_currentTextAtlas != atlas || _currentTextRegion != region)
        {
            _textTexture.Texture = new AtlasTexture
            {
                Atlas = atlas,
                Region = region,
                FilterClip = true,
            };
            _currentTextAtlas = atlas;
            _currentTextRegion = region;
        }
        _textTexture.Visible = true;
        _label.Visible = false;
    }

    public void SetPreviewSelectable(bool selectable)
    {
        _selectable = selectable;
        MouseFilter = selectable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        MouseDefaultCursorShape = selectable ? CursorShape.PointingHand : CursorShape.Arrow;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!_selectable || @event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            SelectionRequested?.Invoke(KeyId, mouseButton.CtrlPressed || mouseButton.ShiftPressed);
            AcceptEvent();
        }
        else if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            ContextMenuRequested?.Invoke(KeyId);
            AcceptEvent();
        }
    }

    public void PressVisual(bool repeat = false)
    {
        if (_held && !repeat)
        {
            return;
        }

        _held = true;
        _releasing = false;
        _motionTime = repeat ? 0.055f : 0f;
        SetProcess(true);
        QueueRedraw();
    }

    public void ReleaseVisual()
    {
        if (!_held && !_releasing)
        {
            return;
        }

        _held = false;
        _releasing = true;
        _motionTime = 0f;
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _motionTime += (float)delta;

        if (_held)
        {
            AnimateHeld((float)delta);
            return;
        }

        if (_releasing)
        {
            AnimateRelease();
        }
    }

    public override void _Draw()
    {
        var width = Mathf.Max(4f, Size.X);
        var height = Mathf.Max(4f, Size.Y);
        var radius = (int)Mathf.Clamp(Mathf.Min(width, height) * 0.24f, 8f, 13f);
        const float antiAliasingSize = 0.68f;

		var shadow = MakeBox(new Color(0.02f, 0.03f, 0.08f, 0.10f), radius + 1, antiAliasingSize);
		DrawStyleBox(shadow, new Rect2(2f, 5.25f, width - 4f, height - 1f));

		var edge = MakeBox(_edgeColor, radius + 1, antiAliasingSize);
		edge.BorderColor = _edgeColor.Darkened(0.12f);
		edge.BorderWidthLeft = 1;
		edge.BorderWidthRight = 1;
		edge.BorderWidthBottom = 1;
		DrawStyleBox(edge, new Rect2(0f, 3.5f, width, height - 1f));

        var topColor = _held ? _topColor.Lightened(0.07f) : _topColor;
        var top = MakeBox(topColor, radius, antiAliasingSize);
        top.BorderColor = MaterialHighlightFor(topColor, _held);
        top.BorderWidthTop = 1;
        top.BorderWidthLeft = 1;
        DrawStyleBox(top, new Rect2(1.25f, 0f, width - 2.5f, height - 2.25f));

		if (_selected)
		{
			var selection = MakeBox(Colors.Transparent, radius + 1, antiAliasingSize);
			selection.BorderColor = SelectionOutlineFor(_topColor);
			selection.SetBorderWidthAll(2);
			DrawStyleBox(selection, new Rect2(0f, -1f, width, height - 1.25f));
		}
    }

    private void AnimateHeld(float delta)
    {
        if (_motionTime < 0.24f)
        {
            var envelope = Mathf.Exp(-_motionTime * 8.2f);
            var wave = Mathf.Cos(_motionTime * 30f);
            var impact = Mathf.Clamp(_motionTime / 0.045f, 0f, 1f);
            var settleX = 1.035f;
            var settleY = 0.855f;

            Scale = new Vector2(
                settleX + envelope * wave * 0.075f * impact,
                settleY - envelope * wave * 0.075f * impact);
            Position = _restPosition + new Vector2(_lean * envelope * wave * 2.2f, 5.5f + envelope * wave * 1.5f);
            Rotation = Mathf.DegToRad(_lean * envelope * wave * 2.4f);
        }
        else
        {
            var weight = Mathf.Clamp(delta * 17f, 0f, 1f);
            Scale = Scale.Lerp(new Vector2(1.035f, 0.855f), weight);
            Position = Position.Lerp(_restPosition + new Vector2(0f, 5.5f), weight);
            Rotation = Mathf.Lerp(Rotation, 0f, weight);
            SetProcess(false);
        }
    }

    private void AnimateRelease()
    {
        var envelope = Mathf.Exp(-_motionTime * 5.1f);
        var wave = Mathf.Cos(_motionTime * 28f + Mathf.Pi);
        var stretch = envelope * wave;

        Scale = new Vector2(1f - stretch * 0.10f, 1f + stretch * 0.16f);
        Position = _restPosition + new Vector2(_lean * envelope * wave * 3.1f, -stretch * 4.5f);
        Rotation = Mathf.DegToRad(_lean * envelope * wave * 3.2f);

        if (_motionTime < 0.95f)
        {
            return;
        }

        _releasing = false;
        Scale = Vector2.One;
        Position = _restPosition;
        Rotation = 0f;
        SetProcess(false);
        QueueRedraw();
    }

    private static Color MaterialHighlightFor(Color color, bool held)
    {
        var luminance = color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
        var amount = Mathf.Lerp(held ? 0.10f : 0.16f, held ? 0.035f : 0.055f, luminance);
        return color.Lightened(amount);
    }

	private static Color SelectionOutlineFor(Color color)
	{
		var luminance = color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
		return luminance >= 0.52f ? Color.FromHtml("#24222a") : Color.FromHtml("#d7fb6f");
	}

    private static StyleBoxFlat MakeBox(Color color, int radius, float antiAliasingSize)
    {
        return new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            AntiAliasing = true,
            AntiAliasingSize = antiAliasingSize,
        };
    }

    public static Color EdgeColorFor(Color topColor)
    {
        return topColor.Darkened(0.24f);
    }

    public static int DefaultLabelSizeFor(float width)
    {
        return LabelSizeFor(width);
    }

    private static int LabelSizeFor(float width)
    {
        if (width < 55f)
        {
            return 11;
        }

        return width < 88f ? 10 : 9;
    }

    private static float StableLean(string value)
    {
        var hash = 17;
        foreach (var character in value)
        {
            hash = unchecked(hash * 31 + character);
        }

        return ((Math.Abs(hash) % 201) / 100f - 1f) * 0.85f;
    }
}
