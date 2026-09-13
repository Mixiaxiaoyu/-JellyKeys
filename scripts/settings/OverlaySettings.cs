using Godot;
using JellyKeyboardOverlay.UI;

namespace JellyKeyboardOverlay.Settings;

public enum KeyboardLayoutMode
{
    FollowTheme,
    Automatic,
    Manual,
}

public sealed class OverlaySettings
{
    public const float MinScale = 0.25f;
    public const float MaxScale = 1.5f;
    private const string SettingsPath = "user://settings.cfg";
    private const string Section = "overlay";

    public int ThemeIndex { get; set; }
    public float Scale { get; set; } = 1f;
    public bool Locked { get; set; } = true;
    public Vector2I WindowPosition { get; set; } = new(-1, -1);
    public KeyboardLayoutMode LayoutMode { get; set; } = KeyboardLayoutMode.FollowTheme;
    public int ManualKeyboardType { get; set; } = 108;

    public int ResolveKeyboardType(int themeType, int? detectedType) =>
        ResolveKeyboardType(LayoutMode, ManualKeyboardType, themeType, detectedType);

    public static int ResolveKeyboardType(
        KeyboardLayoutMode mode, int manualType, int themeType, int? detectedType)
    {
        if (mode == KeyboardLayoutMode.Manual && KeyboardTypeCatalog.IsSupported(manualType))
        {
            return manualType;
        }
        if (mode == KeyboardLayoutMode.Automatic && detectedType.HasValue &&
            KeyboardTypeCatalog.IsSupported(detectedType.Value))
        {
            return detectedType.Value;
        }
        return KeyboardTypeCatalog.IsSupported(themeType) ? themeType : 108;
    }

    public static OverlaySettings Load()
    {
        var settings = new OverlaySettings();
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
        {
            return settings;
        }

        settings.ThemeIndex = Mathf.Clamp((int)config.GetValue(Section, "theme_index", 0), 0, 4);
        settings.Scale = Mathf.Clamp((float)config.GetValue(Section, "scale", 1f), MinScale, MaxScale);
        settings.Locked = (bool)config.GetValue(Section, "locked", true);
        settings.WindowPosition = (Vector2I)config.GetValue(Section, "window_position", new Vector2I(-1, -1));
        var layoutMode = (int)config.GetValue(Section, "keyboard_layout_mode", 0);
        settings.LayoutMode = Enum.IsDefined(typeof(KeyboardLayoutMode), layoutMode)
            ? (KeyboardLayoutMode)layoutMode : KeyboardLayoutMode.FollowTheme;
        var manualType = (int)config.GetValue(Section, "manual_keyboard_type", 108);
        settings.ManualKeyboardType = KeyboardTypeCatalog.IsSupported(manualType) ? manualType : 108;
        return settings;
    }

    public void Save()
    {
        var config = new ConfigFile();
        config.SetValue(Section, "theme_index", ThemeIndex);
        config.SetValue(Section, "scale", Scale);
        config.SetValue(Section, "locked", Locked);
        config.SetValue(Section, "window_position", WindowPosition);
        config.SetValue(Section, "keyboard_layout_mode", (int)LayoutMode);
        config.SetValue(Section, "manual_keyboard_type", ManualKeyboardType);
        config.Save(SettingsPath);
    }

    public void SaveKeyboardLayout()
    {
        var config = new ConfigFile();
        config.Load(SettingsPath);
        config.SetValue(Section, "keyboard_layout_mode", (int)LayoutMode);
        config.SetValue(Section, "manual_keyboard_type", ManualKeyboardType);
        config.Save(SettingsPath);
    }
}
