using Godot;

namespace JellyKeyboardOverlay.UI;

public sealed record KeyboardThemeData(
    string Id,
    string DisplayName,
    Color Page,
    Color Deck,
    Color KeyTop,
    Color KeyEdge,
    Color KeyInk,
    Color InputInk);

public static class KeyboardThemes
{
    public static IReadOnlyList<KeyboardThemeData> Presets { get; } = new[]
    {
        Create("lime", "方案 1", "#3455e9", "#243fc4", "#d9f871", "#aabd51", "#1f2937", "#f7f4ff"),
        Create("coral", "方案 2", "#762d69", "#571d50", "#ff987f", "#c96757", "#351824", "#fff7f4"),
        Create("aqua", "方案 3", "#087a98", "#075c75", "#9cecf3", "#58b7c5", "#0b3340", "#f4feff"),
        Create("mango", "Clay / Mango", "#a94325", "#7e2e1a", "#ffd15c", "#c69331", "#3b2a0e", "#fffaf0"),
        Create("violet", "Midnight / Violet", "#24223e", "#17162c", "#c8b7ff", "#8f7dce", "#282040", "#f8f5ff"),
    };

    private static KeyboardThemeData Create(string id, string name, string page, string deck, string top, string edge, string ink, string input)
    {
        return new KeyboardThemeData(id, name, Color.FromHtml(page), Color.FromHtml(deck), Color.FromHtml(top), Color.FromHtml(edge), Color.FromHtml(ink), Color.FromHtml(input));
    }
}
