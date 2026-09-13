using Godot;
using JellyKeyboardOverlay.Layout;

namespace JellyKeyboardOverlay.UI;

public partial class KeyboardThumbnailView : Control
{
    private KeyboardDesignData? _design;
    private KeyboardLayoutData? _layout;

    public KeyboardThumbnailView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Configure(KeyboardDesignData design, KeyboardLayoutData layout)
    {
        _design = design.Clone();
        _layout = layout;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_design is null || _layout is null || Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        const float outerPadding = 2f;
        var visibleBounds = KeyboardTypeCatalog.VisibleBounds(_design.KeyboardType, _layout.Keys);
        var deckSize = visibleBounds.Size + KeyboardVisualMetrics.DeckPadding * 2f;
        var visualSize = deckSize + new Vector2(0f, KeyboardVisualMetrics.DeckDepth);
        var available = Size - Vector2.One * outerPadding * 2f;
        var scale = Mathf.Min(available.X / visualSize.X, available.Y / visualSize.Y);
        var origin = (Size - visualSize * scale) * 0.5f;
        var deckRect = new Rect2(origin, deckSize * scale);

        var depthRect = new Rect2(
            origin + new Vector2(0f, KeyboardVisualMetrics.DeckDepth * scale),
            deckSize * scale);
        var radius = Mathf.Max(2, Mathf.RoundToInt(KeyboardVisualMetrics.DeckCornerRadius * scale));
        DrawStyleBox(MakeStyle(KeyboardVisualMetrics.DeckDepthColor(_design.Palette.Deck), radius), depthRect);

        var deck = MakeStyle(_design.Palette.Deck, radius);
        DrawStyleBox(deck, deckRect);

        var keyOrigin = origin + (KeyboardVisualMetrics.DeckPadding - visibleBounds.Position) * scale;

        foreach (var definition in _layout.Keys)
        {
            if (!KeyboardTypeCatalog.Includes(definition.Id, _design.KeyboardType, _layout.Keys))
            {
                continue;
            }

            var appearance = _design.Resolve(definition);
            var topColor = appearance.HasTopColor ? appearance.TopColor : _design.Palette.KeyTop;
            var edgeColor = appearance.HasTopColor
                ? KeyCapView.EdgeColorFor(topColor)
                : _design.Palette.KeyEdge;
            var displayRect = KeyboardTypeCatalog.DisplayRect(definition, _design.KeyboardType, _layout.Keys);
            var keyRect = new Rect2(
                keyOrigin + displayRect.Position * scale,
                displayRect.Size * scale);
            var keyRadius = Mathf.Clamp(Mathf.Min(keyRect.Size.X, keyRect.Size.Y) * 0.22f, 1f, 3f);

            DrawRect(new Rect2(keyRect.Position + new Vector2(0f, 1.5f), keyRect.Size), edgeColor, true);
            var topRect = new Rect2(keyRect.Position, new Vector2(keyRect.Size.X, Mathf.Max(1f, keyRect.Size.Y - 1.5f)));
            DrawStyleBox(MakeStyle(topColor, Mathf.RoundToInt(keyRadius)), topRect);
        }
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
}
