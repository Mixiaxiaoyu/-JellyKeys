using Godot;

namespace JellyKeyboardOverlay.UI;

public static class KeyboardVisualMetrics
{
    public static readonly Vector2 DeckPadding = new(14f, 14f);
    public const float DeckDepth = 16f;
    public const int DeckCornerRadius = 24;
    public const int DeckDepthCornerRadius = 25;

    public static Color DeckDepthColor(Color deckColor)
    {
        return new Color(deckColor.R * 0.74f, deckColor.G * 0.74f, deckColor.B * 0.74f, deckColor.A);
    }
}
