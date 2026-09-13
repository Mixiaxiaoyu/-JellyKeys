using Godot;
using JellyKeyboardOverlay.Layout;

namespace JellyKeyboardOverlay.UI;

public static class KeyboardTypeCatalog
{
    public static IReadOnlyList<int> Supported { get; } = new[] { 61, 68, 87, 84, 98, 100, 104, 108 };
    private static readonly IReadOnlyDictionary<int, HashSet<string>> RemovedKeys = CreateRemovedKeys();

    public static bool IsSupported(int value) => Supported.Contains(value);

    public static Rect2 VisibleBounds(int keyboardType, IReadOnlyList<KeyDefinition> allKeys)
    {
        var visible = allKeys.Where(key => Includes(key.Id, keyboardType, allKeys))
            .Select(key => DisplayRect(key, keyboardType, allKeys)).ToArray();
        if (visible.Length == 0)
        {
            return new Rect2(Vector2.Zero, Vector2.One);
        }

        var minimum = new Vector2(
            visible.Min(rect => rect.Position.X),
            visible.Min(rect => rect.Position.Y));
        var maximum = new Vector2(
            visible.Max(rect => rect.End.X),
            visible.Max(rect => rect.End.Y));
        return new Rect2(minimum, maximum - minimum);
    }

    public static Rect2 DisplayRect(KeyDefinition key, int keyboardType, IReadOnlyList<KeyDefinition> allKeys)
    {
        if (keyboardType == 61 && key.Id == "Escape")
        {
            return allKeys.FirstOrDefault(definition => definition.Id == "Backquote")?.Rect ?? key.Rect;
        }
        return key.Rect;
    }

    public static bool Includes(string keyId, int keyboardType, IReadOnlyList<KeyDefinition> allKeys)
    {
        if (keyboardType >= 108)
        {
            return true;
        }

        return !RemovedKeys.TryGetValue(keyboardType, out var removed) || !removed.Contains(keyId);
    }

    private static IReadOnlyDictionary<int, HashSet<string>> CreateRemovedKeys()
    {
        var media = new[] { "MediaPlayPause", "AudioVolumeUp", "AudioVolumeDown", "AudioVolumeMute" };
        var numpad = new[]
        {
            "NumpadDecimal", "NumpadEnter", "Numpad0", "Numpad3", "Numpad2", "Numpad1",
            "NumpadAdd", "Numpad6", "Numpad5", "Numpad4", "NumpadSubtract", "NumpadMultiply",
            "NumpadDivide", "Numpad9", "Numpad8", "Numpad7", "NumLock",
        };
        var system = new[] { "PrintScreen", "ScrollLock", "Pause", "ContextMenu" };
        var navigation = new[] { "Insert", "Home", "PageUp", "Delete", "End", "PageDown" };
        var arrows = new[] { "ArrowRight", "ArrowDown", "ArrowLeft", "ArrowUp" };
        var functionKeys = Enumerable.Range(1, 12).Select(number => $"F{number}");

        var removed = new Dictionary<int, HashSet<string>>
        {
            [104] = new(media, StringComparer.Ordinal),
            [100] = new(media.Concat(system), StringComparer.Ordinal),
            [98] = new(media.Concat(navigation), StringComparer.Ordinal),
            [87] = new(media.Concat(numpad), StringComparer.Ordinal),
            [84] = new(media.Concat(numpad).Concat(system.Take(3)), StringComparer.Ordinal),
            [68] = new(media.Concat(numpad).Concat(system.Take(3)).Concat(functionKeys).Concat(arrows), StringComparer.Ordinal),
            [61] = new(media.Concat(numpad).Concat(system.Take(3)).Concat(functionKeys).Concat(arrows).Concat(navigation).Append("Backquote"), StringComparer.Ordinal),
        };
        return removed;
    }
}
