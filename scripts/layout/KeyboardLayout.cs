using Godot;

namespace JellyKeyboardOverlay.Layout;

public static class KeyboardLayout
{
    public const float Unit = 46f;
    public const float KeyHeight = 48f;
    public const float KeyGap = 6f;
    public const float RowGap = 8f;
    public const float ClusterGap = 18f;
    public const float MainWidth = 774f;
    public const float NavWidth = Unit * 3f + KeyGap * 2f;
    public const float NumpadWidth = Unit * 4f + KeyGap * 3f;
    public const float BodyY = KeyHeight + ClusterGap;

    public static KeyboardLayoutData Create108()
    {
        var keys = new List<KeyDefinition>(108);

        AddFunctionRow(keys);
        AddMainRows(keys);
        AddNavigation(keys);
        AddNumpad(keys);

        if (keys.Count != 108)
        {
            throw new InvalidOperationException($"Expected 108 keys, generated {keys.Count}.");
        }

        var duplicate = keys.GroupBy(key => key.Id).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate key id: {duplicate.Key}.");
        }

        var contentWidth = MainWidth + ClusterGap + NavWidth + ClusterGap + NumpadWidth;
        var contentHeight = BodyY + KeyHeight * 5f + RowGap * 4f;
        return new KeyboardLayoutData(keys, new Vector2(contentWidth, contentHeight));
    }

    private static void AddFunctionRow(List<KeyDefinition> keys)
    {
        Add(keys, "Escape", "ESC", 0f, 0f);

        var groups = new[]
        {
            (Start: 92f, First: 1),
            (Start: 326f, First: 5),
            (Start: 560f, First: 9),
        };

        foreach (var group in groups)
        {
            for (var index = 0; index < 4; index++)
            {
                var number = group.First + index;
                Add(keys, $"F{number}", $"F{number}", group.Start + index * (Unit + KeyGap), 0f);
            }
        }

        var navX = MainWidth + ClusterGap;
        Add(keys, "PrintScreen", "PRT SC", navX, 0f);
        Add(keys, "ScrollLock", "SCR LK", navX + Unit + KeyGap, 0f);
        Add(keys, "Pause", "PAUSE", navX + (Unit + KeyGap) * 2f, 0f);

        var mediaX = navX + NavWidth + ClusterGap;
        Add(keys, "AudioVolumeMute", "MUTE", mediaX, 0f);
        Add(keys, "AudioVolumeDown", "VOL -", mediaX + Unit + KeyGap, 0f);
        Add(keys, "AudioVolumeUp", "VOL +", mediaX + (Unit + KeyGap) * 2f, 0f);
        Add(keys, "MediaPlayPause", "PLAY", mediaX + (Unit + KeyGap) * 3f, 0f);
    }

    private static void AddMainRows(List<KeyDefinition> keys)
    {
        AddRow(keys, BodyY,
            ("Backquote", "`", 1f),
            ("Digit1", "1", 1f), ("Digit2", "2", 1f), ("Digit3", "3", 1f),
            ("Digit4", "4", 1f), ("Digit5", "5", 1f), ("Digit6", "6", 1f),
            ("Digit7", "7", 1f), ("Digit8", "8", 1f), ("Digit9", "9", 1f),
            ("Digit0", "0", 1f), ("Minus", "-", 1f), ("Equal", "=", 1f),
            ("Backspace", "BACKSPACE", 2f));

        AddRow(keys, RowY(1),
            ("Tab", "TAB", 1.5f),
            ("KeyQ", "Q", 1f), ("KeyW", "W", 1f), ("KeyE", "E", 1f),
            ("KeyR", "R", 1f), ("KeyT", "T", 1f), ("KeyY", "Y", 1f),
            ("KeyU", "U", 1f), ("KeyI", "I", 1f), ("KeyO", "O", 1f),
            ("KeyP", "P", 1f), ("BracketLeft", "[", 1f), ("BracketRight", "]", 1f),
            ("Backslash", "\\", 1.5f));

        AddRow(keys, RowY(2),
            ("CapsLock", "CAPS LOCK", 1.75f),
            ("KeyA", "A", 1f), ("KeyS", "S", 1f), ("KeyD", "D", 1f),
            ("KeyF", "F", 1f), ("KeyG", "G", 1f), ("KeyH", "H", 1f),
            ("KeyJ", "J", 1f), ("KeyK", "K", 1f), ("KeyL", "L", 1f),
            ("Semicolon", ";", 1f), ("Quote", "'", 1f),
            ("Enter", "ENTER", 2.25f));

        AddRow(keys, RowY(3),
            ("ShiftLeft", "SHIFT", 2.25f),
            ("KeyZ", "Z", 1f), ("KeyX", "X", 1f), ("KeyC", "C", 1f),
            ("KeyV", "V", 1f), ("KeyB", "B", 1f), ("KeyN", "N", 1f),
            ("KeyM", "M", 1f), ("Comma", ",", 1f), ("Period", ".", 1f),
            ("Slash", "/", 1f), ("ShiftRight", "SHIFT", 2.75f));

        AddRow(keys, RowY(4),
			("ControlLeft", "CTRL", 1.25f), ("MetaLeft", "WIN", 1.25f),
			("AltLeft", "ALT", 1.25f), ("Space", "", 6.25f),
			("AltRight", "ALT", 1.25f), ("MetaRight", "FN", 1.25f),
			("ContextMenu", "WIN", 1.25f), ("ControlRight", "CTRL", 1.25f));
    }

    private static void AddNavigation(List<KeyDefinition> keys)
    {
        var x = MainWidth + ClusterGap;
        Add(keys, "Insert", "INSERT", x, BodyY);
        Add(keys, "Home", "HOME", x + Unit + KeyGap, BodyY);
        Add(keys, "PageUp", "PG UP", x + (Unit + KeyGap) * 2f, BodyY);
        Add(keys, "Delete", "DELETE", x, RowY(1));
        Add(keys, "End", "END", x + Unit + KeyGap, RowY(1));
        Add(keys, "PageDown", "PG DN", x + (Unit + KeyGap) * 2f, RowY(1));
        Add(keys, "ArrowUp", "UP", x + Unit + KeyGap, RowY(3));
        Add(keys, "ArrowLeft", "LEFT", x, RowY(4));
        Add(keys, "ArrowDown", "DOWN", x + Unit + KeyGap, RowY(4));
        Add(keys, "ArrowRight", "RIGHT", x + (Unit + KeyGap) * 2f, RowY(4));
    }

    private static void AddNumpad(List<KeyDefinition> keys)
    {
        var x = MainWidth + ClusterGap + NavWidth + ClusterGap;
        Add(keys, "NumLock", "NUM", x, BodyY);
        Add(keys, "NumpadDivide", "/", x + Unit + KeyGap, BodyY);
        Add(keys, "NumpadMultiply", "*", x + (Unit + KeyGap) * 2f, BodyY);
        Add(keys, "NumpadSubtract", "-", x + (Unit + KeyGap) * 3f, BodyY);

        Add(keys, "Numpad7", "7", x, RowY(1));
        Add(keys, "Numpad8", "8", x + Unit + KeyGap, RowY(1));
        Add(keys, "Numpad9", "9", x + (Unit + KeyGap) * 2f, RowY(1));
        Add(keys, "NumpadAdd", "+", x + (Unit + KeyGap) * 3f, RowY(1), Unit, KeyHeight * 2f + RowGap);

        Add(keys, "Numpad4", "4", x, RowY(2));
        Add(keys, "Numpad5", "5", x + Unit + KeyGap, RowY(2));
        Add(keys, "Numpad6", "6", x + (Unit + KeyGap) * 2f, RowY(2));

        Add(keys, "Numpad1", "1", x, RowY(3));
        Add(keys, "Numpad2", "2", x + Unit + KeyGap, RowY(3));
        Add(keys, "Numpad3", "3", x + (Unit + KeyGap) * 2f, RowY(3));
        Add(keys, "NumpadEnter", "ENTER", x + (Unit + KeyGap) * 3f, RowY(3), Unit, KeyHeight * 2f + RowGap);

        Add(keys, "Numpad0", "0", x, RowY(4), Unit * 2f + KeyGap, KeyHeight);
        Add(keys, "NumpadDecimal", ".", x + (Unit + KeyGap) * 2f, RowY(4));
    }

    private static void AddRow(List<KeyDefinition> keys, float y, params (string Id, string Label, float Units)[] row)
    {
        var totalUnits = row.Sum(item => item.Units);
        var unitWidth = (MainWidth - KeyGap * (row.Length - 1)) / totalUnits;
        var x = 0f;

        foreach (var item in row)
        {
            var width = unitWidth * item.Units;
            Add(keys, item.Id, item.Label, x, y, width, KeyHeight);
            x += width + KeyGap;
        }
    }

    private static float RowY(int rowIndex) => BodyY + rowIndex * (KeyHeight + RowGap);

    private static void Add(List<KeyDefinition> keys, string id, string label, float x, float y, float width = Unit, float height = KeyHeight)
    {
        keys.Add(new KeyDefinition(id, label, new Rect2(x, y, width, height)));
    }
}
