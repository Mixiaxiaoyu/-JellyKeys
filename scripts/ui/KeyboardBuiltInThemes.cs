using Godot;
using JellyKeyboardOverlay.Layout;

namespace JellyKeyboardOverlay.UI;

public static class KeyboardBuiltInThemes
{
    public static IReadOnlySet<string> DeprecatedIds { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "preset-mango",
        "preset-violet",
        // Two early editor drafts shown in the user's library. Keep their exact
        // identities here so only those records are retired, never later themes
        // that happen to reuse the same display names.
        "be98f656f8314e5a84d063f622c50b5e",
        "6ae9f85d9b95494b93a5ebacc5b1714a",
    };

    public static List<KeyboardDesignData> CreateLibrary()
    {
        var layout = KeyboardLayout.Create108();
        var designs = KeyboardThemes.Presets
            .Take(3)
            .Select((preset, index) =>
            {
                var design = KeyboardDesignData.FromPreset(preset, 108);
                design.DisplayName = $"方案 {index + 1}";
                return design;
            })
            .ToList();

        designs.Add(CreateMechaDawn(layout));
        designs.Add(CreatePortalLab(layout));
        designs.Add(CreateNeonArena(layout));
        designs.Add(CreateSpectrumSilver(layout));
        return designs;
    }

    private static KeyboardDesignData CreateMechaDawn(KeyboardLayoutData layout)
    {
        var design = Create(
            "mecha-dawn",
            "机甲晨光",
            84,
            "#c6d0dd", "#dbe1e8", "#f2f3f5", "#c7ccd3", "#263149", "#ffffff");
        Paint(design, layout, "#f4c431", null,
            "F5", "F6", "F7", "F8", "CapsLock", "Space");
        Paint(design, layout, "#3265b4", "#ffffff",
            "Tab", "ShiftLeft", "ControlLeft", "AltLeft", "ShiftRight", "ControlRight");
        Paint(design, layout, "#e45146", "#ffffff",
            "Escape", "Enter", "MetaLeft", "MetaRight", "ArrowUp", "ArrowLeft", "ArrowDown", "ArrowRight");
        return design;
    }

    private static KeyboardDesignData CreatePortalLab(KeyboardLayoutData layout)
    {
        var design = Create(
            "portal-lab",
            "橙蓝实验室",
            87,
            "#d9d4c8", "#e7e1d4", "#f2f1eb", "#bbb8b0", "#202124", "#ffffff");
        Paint(design, layout, "#3d9bc7", "#ffffff",
            "Tab", "CapsLock", "ShiftLeft", "ControlLeft", "Home", "PageUp", "End");
        Paint(design, layout, "#e88642", "#2a2017",
            "Enter", "ShiftRight", "BracketLeft", "BracketRight", "Quote", "Insert", "Delete");
        Paint(design, layout, "#1b1b1d", "#ffffff",
            "Escape", "Space", "MetaLeft", "AltLeft", "AltRight", "MetaRight", "ContextMenu",
            "ArrowUp", "ArrowLeft", "ArrowDown", "ArrowRight");
        return design;
    }

    private static KeyboardDesignData CreateNeonArena(KeyboardLayoutData layout)
    {
        var design = Create(
            "neon-arena",
            "霓虹竞技场",
            87,
            "#353b40", "#555c60", "#171d28", "#090d14", "#edf4ff", "#ffffff");
        Paint(design, layout, "#78d64d", "#17220e",
            "Tab", "CapsLock", "ShiftLeft", "ControlLeft", "Space", "Enter", "ShiftRight",
            "Home", "PageUp", "End");
        Paint(design, layout, "#db3d35", "#ffffff",
            "Escape", "ArrowUp", "ArrowLeft", "ArrowDown", "ArrowRight");
        Paint(design, layout, "#079975", "#ffffff",
            "AltRight", "MetaRight", "ContextMenu", "ControlRight");
        Paint(design, layout, "#c9d0d2", "#263039", "MetaLeft");
        return design;
    }

    private static KeyboardDesignData CreateSpectrumSilver(KeyboardLayoutData layout)
    {
        var design = Create(
            "spectrum-silver",
            "虹彩银翼",
            61,
            "#d4d1cc", "#b8b3aa", "#e9ebe7", "#b8bcb8", "#24272d", "#ffffff");
        Paint(design, layout, "#f1ca39", "#2b260f", "Escape", "Digit1", "Digit2");
        Paint(design, layout, "#55bfcd", "#18343a", "Digit3", "Digit4");
        Paint(design, layout, "#64c987", "#173622", "Digit5", "Digit6");
        Paint(design, layout, "#e598ae", "#3c2029", "Digit7", "Digit8");
        Paint(design, layout, "#e9515d", "#ffffff", "Digit9", "Digit0");
        Paint(design, layout, "#7476b3", "#ffffff", "Minus", "Equal");
        Paint(design, layout, "#242832", "#ffffff",
            "Backspace", "ShiftLeft", "ShiftRight", "ControlLeft", "ControlRight", "AltLeft", "AltRight",
            "MetaLeft", "MetaRight", "ContextMenu", "Space");
        return design;
    }

    private static KeyboardDesignData Create(
        string id,
        string name,
        int keyboardType,
        string page,
        string deck,
        string keyTop,
        string keyEdge,
        string keyInk,
        string inputInk)
    {
        var palette = new KeyboardThemeData(
            id,
            name,
            Color.FromHtml(page),
            Color.FromHtml(deck),
            Color.FromHtml(keyTop),
            Color.FromHtml(keyEdge),
            Color.FromHtml(keyInk),
            Color.FromHtml(inputInk));
        return new KeyboardDesignData
        {
            Id = $"builtin-{id}",
            DisplayName = name,
            KeyboardType = keyboardType,
            Palette = palette,
        };
    }

    private static void Paint(
        KeyboardDesignData design,
        KeyboardLayoutData layout,
        string topColor,
        string? inkColor,
        params string[] keyIds)
    {
        var top = Color.FromHtml(topColor);
        var ink = inkColor is null ? Colors.Transparent : Color.FromHtml(inkColor);
        foreach (var keyId in keyIds)
        {
            var definition = layout.Keys.FirstOrDefault(key => string.Equals(key.Id, keyId, StringComparison.Ordinal));
            if (definition is null)
            {
                continue;
            }

            var appearance = design.GetOrCreate(definition);
            appearance.TopColor = top;
            appearance.HasTopColor = true;
            if (inkColor is not null)
            {
                appearance.InkColor = ink;
                appearance.HasInkColor = true;
            }
        }
    }
}

public static class KeyboardThemeLibraryCatalog
{
    public static List<KeyboardDesignData> Load()
    {
        var library = new List<KeyboardDesignData>();
        var saved = KeyboardThemeStore.LoadLibrary();
        var deletedThemeIds = KeyboardThemeStore.LoadDeletedThemeIds();
        var removedDeprecated = saved.RemoveAll(item => KeyboardBuiltInThemes.DeprecatedIds.Contains(item.Id)) > 0;
        var consumedIds = new HashSet<string>(StringComparer.Ordinal);
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var namesChanged = false;

        foreach (var builtIn in KeyboardBuiltInThemes.CreateLibrary())
        {
            if (deletedThemeIds.Contains(builtIn.Id))
            {
                continue;
            }

            var savedDesign = saved.FirstOrDefault(item => string.Equals(item.Id, builtIn.Id, StringComparison.Ordinal));
            var design = savedDesign ?? builtIn;
            if (string.IsNullOrWhiteSpace(design.DisplayName))
            {
                design.DisplayName = builtIn.DisplayName;
                namesChanged |= savedDesign is not null;
            }

            library.Add(design);
            consumedIds.Add(builtIn.Id);
            usedNames.Add(design.DisplayName);
        }

        foreach (var design in saved.Where(item =>
                     !consumedIds.Contains(item.Id) && !deletedThemeIds.Contains(item.Id)))
        {
            var normalizedName = NormalizeCustomDisplayName(design.DisplayName);
            if (normalizedName is null || usedNames.Contains(normalizedName))
            {
                design.DisplayName = ThemeNamePolicy.NextAvailable(usedNames, "自定义方案");
                namesChanged = true;
            }
            else if (!string.Equals(design.DisplayName, normalizedName, StringComparison.Ordinal))
            {
                design.DisplayName = normalizedName;
                namesChanged = true;
            }

            usedNames.Add(design.DisplayName);
            library.Add(design);
        }

        if (namesChanged || removedDeprecated)
        {
            KeyboardThemeStore.SaveLibrary(saved);
        }

        return library;
    }

    private static string? NormalizeCustomDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var compact = displayName.Replace(" ", string.Empty);
        if (compact.StartsWith("方案", StringComparison.Ordinal) &&
            int.TryParse(compact[2..], out var presetNumber) &&
            presetNumber >= 1 && presetNumber <= 3)
        {
            return null;
        }

        if (compact.StartsWith("新方案", StringComparison.Ordinal) &&
            int.TryParse(compact[3..], out var newThemeNumber) && newThemeNumber >= 1)
        {
            return $"新方案 {newThemeNumber}";
        }

        return displayName.Trim();
    }
}
