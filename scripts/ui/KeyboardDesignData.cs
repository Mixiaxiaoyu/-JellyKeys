using Godot;
using JellyKeyboardOverlay.Layout;

namespace JellyKeyboardOverlay.UI;

public sealed class KeyAppearanceData
{
    public string Label { get; set; } = string.Empty;
    public int FontSize { get; set; }
    public Color TopColor { get; set; } = Colors.Transparent;
    public Color InkColor { get; set; } = Colors.Transparent;
    public bool HasTopColor { get; set; }
    public bool HasInkColor { get; set; }

    public KeyAppearanceData Clone()
    {
        return new KeyAppearanceData
        {
            Label = Label,
            FontSize = FontSize,
            TopColor = TopColor,
            InkColor = InkColor,
            HasTopColor = HasTopColor,
            HasInkColor = HasInkColor,
        };
    }
}

public sealed class KeyboardDesignData
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "新方案";
    public int KeyboardType { get; set; } = 108;
    public KeyboardThemeData Palette { get; set; } = KeyboardThemes.Presets[0];
    public byte[]? TextAtlasPng { get; set; }
    public HashSet<string> TextLabelFallbackIds { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, KeyAppearanceData> Keys { get; } = new(StringComparer.Ordinal);

    public static KeyboardDesignData FromPreset(KeyboardThemeData preset, int keyboardType = 108)
    {
        return new KeyboardDesignData
        {
            Id = $"preset-{preset.Id}",
            DisplayName = preset.DisplayName,
            KeyboardType = keyboardType,
            Palette = preset,
        };
    }

    public KeyboardDesignData Clone(bool createNewIdentity = false)
    {
        var clone = new KeyboardDesignData
        {
            Id = createNewIdentity ? Guid.NewGuid().ToString("N") : Id,
            DisplayName = DisplayName,
            KeyboardType = KeyboardType,
            Palette = Palette,
            TextAtlasPng = TextAtlasPng?.ToArray(),
        };

        foreach (var pair in Keys)
        {
            clone.Keys[pair.Key] = pair.Value.Clone();
        }

        clone.TextLabelFallbackIds.UnionWith(TextLabelFallbackIds);

        return clone;
    }

    public KeyAppearanceData GetOrCreate(KeyDefinition definition)
    {
        if (Keys.TryGetValue(definition.Id, out var appearance))
        {
            return appearance;
        }

        appearance = new KeyAppearanceData
        {
            Label = definition.Label,
            FontSize = KeyCapView.DefaultLabelSizeFor(definition.Rect.Size.X),
        };
        Keys[definition.Id] = appearance;
        return appearance;
    }

    public KeyAppearanceData Resolve(KeyDefinition definition)
    {
        return Keys.TryGetValue(definition.Id, out var appearance)
            ? appearance
            : new KeyAppearanceData
            {
                Label = definition.Label,
                FontSize = KeyCapView.DefaultLabelSizeFor(definition.Rect.Size.X),
            };
    }
}
