using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using JellyKeyboardOverlay.Layout;

namespace JellyKeyboardOverlay.UI;

public static class KeyboardThemeStore
{
    public const string ThemeSchema = "jelly-keyboard-theme/v1";
    public const string PackageSchema = "jelly-keyboard-package/v1";
    public const string PackageExtension = ".jellytheme";

    private const int MaxThemeJsonBytes = 2 * 1024 * 1024;
    private const int MaxTextureBytes = 24 * 1024 * 1024;
    private const int MaxPackageBytes = 32 * 1024 * 1024;
    private const string UserThemeDirectory = "user://themes/user";
    private const string ActivePath = "user://state/active_theme.json";
    private const string LibraryPath = UserThemeDirectory + "/theme_library.json";
    private const string DeletedThemeIdsPath = UserThemeDirectory + "/deleted_theme_ids.json";
    private const string PreviousUserActivePath = UserThemeDirectory + "/active_theme.json";
    private const string LegacyActivePath = "user://active_theme.json";
    private const string LegacyLibraryPath = "user://theme_library.json";
    private const string LegacyDeletedThemeIdsPath = "user://deleted_theme_ids.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static KeyboardDesignData LoadActive(KeyboardDesignData fallback)
    {
        try
        {
            var path = MigrateLegacyFileIfNeeded(
                ProjectSettings.GlobalizePath(ActivePath),
                ProjectSettings.GlobalizePath(PreviousUserActivePath));
            path = MigrateLegacyFileIfNeeded(
                path,
                ProjectSettings.GlobalizePath(LegacyActivePath));
            return File.Exists(path) ? ReadTheme(path) : fallback;
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Active theme could not be loaded: {exception.Message}");
            return fallback;
        }
    }

    public static void SaveActive(KeyboardDesignData design)
    {
        try
        {
            WriteTheme(ProjectSettings.GlobalizePath(ActivePath), design);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Active theme could not be saved: {exception.Message}");
        }
    }

    public static List<KeyboardDesignData> LoadLibrary()
    {
        try
        {
            var path = MigrateLegacyFileIfNeeded(
                ProjectSettings.GlobalizePath(LibraryPath),
                ProjectSettings.GlobalizePath(LegacyLibraryPath));
            if (!File.Exists(path))
            {
                return new List<KeyboardDesignData>();
            }

            var file = JsonSerializer.Deserialize<ThemeLibraryDto>(File.ReadAllText(path), JsonOptions);
            return NormalizeLibrary(file?.Themes.Select(FromDto) ?? Array.Empty<KeyboardDesignData>());
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Theme library could not be loaded: {exception.Message}");
            return new List<KeyboardDesignData>();
        }
    }

    public static void SaveLibrary(IEnumerable<KeyboardDesignData> designs)
    {
        var path = ProjectSettings.GlobalizePath(LibraryPath);
        EnsureParent(path);
        var file = new ThemeLibraryDto
        {
            Schema = ThemeSchema,
            Themes = NormalizeLibrary(designs).Select(ToDto).ToList(),
        };
        WriteTextAtomically(path, JsonSerializer.Serialize(file, JsonOptions));
    }

    public static void UpsertLibrary(KeyboardDesignData design)
    {
        var designs = LoadLibrary();
        UpsertByIdentity(designs, design);
        SaveLibrary(designs);
        RestoreDeletedTheme(design.Id);
    }

    internal static void UpsertByIdentity(List<KeyboardDesignData> designs, KeyboardDesignData design)
    {
        var index = designs.FindIndex(item => string.Equals(item.Id, design.Id, StringComparison.Ordinal));
        if (index >= 0)
        {
            designs[index] = design.Clone();
        }
        else
        {
            designs.Add(design.Clone());
        }
    }

    public static bool DeleteTheme(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId))
        {
            return false;
        }

        var designs = LoadLibrary();
        var removedFromLibrary = RemoveByIdentity(designs, themeId);
        if (removedFromLibrary)
        {
            SaveLibrary(designs);
        }

        var deletedThemeIds = LoadDeletedThemeIds();
        var markedDeleted = deletedThemeIds.Add(themeId);
        if (markedDeleted)
        {
            SaveDeletedThemeIds(deletedThemeIds);
        }

        return removedFromLibrary || markedDeleted;
    }

    internal static bool RemoveByIdentity(List<KeyboardDesignData> designs, string themeId)
    {
        return designs.RemoveAll(item => string.Equals(item.Id, themeId, StringComparison.Ordinal)) > 0;
    }

    public static HashSet<string> LoadDeletedThemeIds()
    {
        try
        {
            var path = MigrateLegacyFileIfNeeded(
                ProjectSettings.GlobalizePath(DeletedThemeIdsPath),
                ProjectSettings.GlobalizePath(LegacyDeletedThemeIdsPath));
            if (!File.Exists(path))
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var file = JsonSerializer.Deserialize<DeletedThemeIdsDto>(File.ReadAllText(path), JsonOptions);
            return new HashSet<string>(
                file?.Ids.Where(id => !string.IsNullOrWhiteSpace(id)) ?? Array.Empty<string>(),
                StringComparer.Ordinal);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Deleted theme list could not be loaded: {exception.Message}");
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private static void RestoreDeletedTheme(string themeId)
    {
        var deletedThemeIds = LoadDeletedThemeIds();
        if (!deletedThemeIds.Remove(themeId))
        {
            return;
        }

        SaveDeletedThemeIds(deletedThemeIds);
    }

    private static void SaveDeletedThemeIds(IEnumerable<string> themeIds)
    {
        var path = ProjectSettings.GlobalizePath(DeletedThemeIdsPath);
        EnsureParent(path);
        var file = new DeletedThemeIdsDto
        {
            Ids = themeIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList(),
        };
        WriteTextAtomically(path, JsonSerializer.Serialize(file, JsonOptions));
    }

    public static void ExportTheme(string path, KeyboardDesignData design, KeyboardLayoutData layout)
    {
        var packaged = design.Clone();
        byte[] textureBytes;
        if (packaged.TextAtlasPng is null)
        {
            foreach (var definition in layout.Keys)
            {
                if (KeyboardTextAtlas.HasCustomLabel(packaged.Resolve(definition), definition))
                {
                    packaged.TextLabelFallbackIds.Add(definition.Id);
                }
            }
            textureBytes = KeyboardTextAtlas.BuildDefaultPackageAtlas(packaged, layout);
        }
        else
        {
            textureBytes = packaged.TextAtlasPng;
        }
        KeyboardTextAtlasValidator.Validate(textureBytes, layout);

        var theme = ToDto(packaged);
        theme.Schema = PackageSchema;
        theme.TextureFile = ThemePackageArchive.TextureEntryName;
        theme.TextAtlasPng = null;
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(theme, JsonOptions));
        if (jsonBytes.Length > MaxThemeJsonBytes || textureBytes.Length > MaxTextureBytes)
        {
            throw new InvalidDataException("主题文件过大，无法导出。");
        }

        ThemePackageArchive.Write(path, jsonBytes, textureBytes);
    }

    public static KeyboardDesignData ImportTheme(string path, KeyboardLayoutData layout)
    {
        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            if (new FileInfo(path).Length > MaxPackageBytes)
            {
                throw new InvalidDataException("主题文件过大，无法导入。");
            }
            var legacy = ReadTheme(path);
            if (legacy.TextAtlasPng is not null)
            {
                KeyboardTextAtlasValidator.Validate(legacy.TextAtlasPng, layout);
            }
            return legacy;
        }
        if (!path.EndsWith(PackageExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("请选择 .jellytheme 主题包或旧版 .json 文件。");
        }
        if (new FileInfo(path).Length > MaxPackageBytes)
        {
            throw new InvalidDataException("主题包过大，无法导入。");
        }

        var (themeBytes, textureBytes) = ThemePackageArchive.Read(path, MaxThemeJsonBytes, MaxTextureBytes);
        var theme = JsonSerializer.Deserialize<ThemeFileDto>(themeBytes, JsonOptions)
            ?? throw new InvalidDataException("主题数据为空。");
        if (!string.Equals(theme.Schema, PackageSchema, StringComparison.Ordinal) ||
            !string.Equals(theme.TextureFile, ThemePackageArchive.TextureEntryName, StringComparison.Ordinal) ||
            theme.TextAtlasPng is not null)
        {
            throw new InvalidDataException("主题包格式不受支持。");
        }

        if (theme.Keys is null || theme.TextLabelFallbackIds is { Count: > 108 } ||
            theme.TextLabelFallbackIds?.Any(id => !layout.Keys.Any(key => key.Id == id)) == true)
        {
            throw new InvalidDataException("主题数据不完整或键位无效。");
        }

        KeyboardTextAtlasValidator.Validate(textureBytes, layout);
        var design = FromDto(theme);
        design.TextAtlasPng = textureBytes;
        return design;
    }

    public static void ImportTextAtlas(string path, KeyboardDesignData design, KeyboardLayoutData layout)
    {
        if (new FileInfo(path).Length > MaxTextureBytes)
        {
            throw new InvalidDataException("贴图文件过大。");
        }
        var png = File.ReadAllBytes(path);
        KeyboardTextAtlasValidator.Validate(png, layout);
        design.TextAtlasPng = png;
        design.TextLabelFallbackIds.Clear();
    }

    private static KeyboardDesignData ReadTheme(string path)
    {
        var file = JsonSerializer.Deserialize<ThemeFileDto>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("主题文件为空。");
        if (!string.Equals(file.Schema, ThemeSchema, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"不支持的主题格式：{file.Schema}");
        }

        return FromDto(file);
    }

    private static void WriteTheme(string path, KeyboardDesignData design)
    {
        WriteTextAtomically(path, JsonSerializer.Serialize(ToDto(design), JsonOptions));
    }

    private static void WriteTextAtomically(string path, string contents)
    {
        EnsureParent(path);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, contents);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static ThemeFileDto ToDto(KeyboardDesignData design)
    {
        return new ThemeFileDto
        {
            Schema = ThemeSchema,
            Id = design.Id,
            Name = design.DisplayName,
            KeyboardType = design.KeyboardType,
            TextAtlasPng = design.TextAtlasPng,
            TextLabelFallbackIds = design.TextLabelFallbackIds.Count == 0
                ? null : design.TextLabelFallbackIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            Palette = new PaletteDto
            {
                Id = design.Palette.Id,
                Name = design.Palette.DisplayName,
                Page = design.Palette.Page.ToHtml(false),
                Deck = design.Palette.Deck.ToHtml(false),
                KeyTop = design.Palette.KeyTop.ToHtml(false),
                KeyEdge = design.Palette.KeyEdge.ToHtml(false),
                KeyInk = design.Palette.KeyInk.ToHtml(false),
                InputInk = design.Palette.InputInk.ToHtml(false),
            },
            Keys = design.Keys.ToDictionary(
                pair => pair.Key,
                pair => new KeyAppearanceDto
                {
                    Label = pair.Value.Label,
                    FontSize = pair.Value.FontSize,
                    TopColor = pair.Value.TopColor.ToHtml(false),
                    InkColor = pair.Value.InkColor.ToHtml(false),
                    HasTopColor = pair.Value.HasTopColor,
                    HasInkColor = pair.Value.HasInkColor,
                },
                StringComparer.Ordinal),
        };
    }

    private static KeyboardDesignData FromDto(ThemeFileDto file)
    {
        var palette = file.Palette ?? new PaletteDto();
        var design = new KeyboardDesignData
        {
            Id = string.IsNullOrWhiteSpace(file.Id) ? Guid.NewGuid().ToString("N") : file.Id,
            DisplayName = string.IsNullOrWhiteSpace(file.Name) ? "导入方案" : file.Name,
            KeyboardType = KeyboardTypeCatalog.IsSupported(file.KeyboardType) ? file.KeyboardType : 108,
            TextAtlasPng = file.TextAtlasPng,
            Palette = new KeyboardThemeData(
                palette.Id,
                palette.Name,
                ParseColor(palette.Page, "#3455e9"),
                ParseColor(palette.Deck, "#243fc4"),
                ParseColor(palette.KeyTop, "#d9f871"),
                ParseColor(palette.KeyEdge, "#aabd51"),
                ParseColor(palette.KeyInk, "#1f2937"),
                ParseColor(palette.InputInk, "#f7f4ff")),
        };

        foreach (var pair in file.Keys)
        {
            design.Keys[pair.Key] = new KeyAppearanceData
            {
                Label = pair.Value.Label ?? string.Empty,
                FontSize = pair.Value.FontSize,
                TopColor = ParseColor(pair.Value.TopColor, "#000000"),
                InkColor = ParseColor(pair.Value.InkColor, "#000000"),
                HasTopColor = pair.Value.HasTopColor,
                HasInkColor = pair.Value.HasInkColor,
            };
        }

        if (file.TextLabelFallbackIds is not null)
        {
            design.TextLabelFallbackIds.UnionWith(file.TextLabelFallbackIds);
        }

        return design;
    }

    private static Color ParseColor(string? html, string fallback)
    {
        return Color.FromHtml(string.IsNullOrWhiteSpace(html) ? fallback : html);
    }

    private static List<KeyboardDesignData> NormalizeLibrary(IEnumerable<KeyboardDesignData> designs)
    {
        var normalized = new List<KeyboardDesignData>();
        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var design in designs)
        {
            if (string.IsNullOrWhiteSpace(design.Id))
            {
                design.Id = Guid.NewGuid().ToString("N");
            }

            if (positions.TryGetValue(design.Id, out var existingIndex))
            {
                normalized[existingIndex] = design.Clone();
                continue;
            }

            positions[design.Id] = normalized.Count;
            normalized.Add(design.Clone());
        }

        return normalized;
    }

    private static void EnsureParent(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    internal static string MigrateLegacyFileIfNeeded(string destination, string legacy)
    {
        if (File.Exists(destination) || !File.Exists(legacy))
        {
            return destination;
        }

        EnsureParent(destination);
        var temporaryPath = $"{destination}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.Copy(legacy, temporaryPath);
            try
            {
                File.Move(temporaryPath, destination);
            }
            catch (IOException) when (File.Exists(destination))
            {
                // Another instance completed the same one-time migration.
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return destination;
    }

    private sealed class ThemeLibraryDto
    {
        [JsonPropertyName("schema")]
        public string Schema { get; set; } = ThemeSchema;

        [JsonPropertyName("themes")]
        public List<ThemeFileDto> Themes { get; set; } = new();
    }

    private sealed class DeletedThemeIdsDto
    {
        [JsonPropertyName("schema")]
        public string Schema { get; set; } = ThemeSchema;

        [JsonPropertyName("ids")]
        public List<string> Ids { get; set; } = new();
    }

    private sealed class ThemeFileDto
    {
        [JsonPropertyName("schema")]
        public string Schema { get; set; } = ThemeSchema;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("keyboard_type")]
        public int KeyboardType { get; set; } = 108;

        [JsonPropertyName("palette")]
        public PaletteDto? Palette { get; set; } = new();

        [JsonPropertyName("text_atlas_png")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public byte[]? TextAtlasPng { get; set; }

        [JsonPropertyName("texture_file")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TextureFile { get; set; }

        [JsonPropertyName("text_label_fallback_ids")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? TextLabelFallbackIds { get; set; }

        [JsonPropertyName("keys")]
        public Dictionary<string, KeyAppearanceDto> Keys { get; set; } = new(StringComparer.Ordinal);
    }

    private sealed class PaletteDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "custom";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "自定义方案";

        [JsonPropertyName("page")]
        public string Page { get; set; } = "#3455e9";

        [JsonPropertyName("deck")]
        public string Deck { get; set; } = "#243fc4";

        [JsonPropertyName("key_top")]
        public string KeyTop { get; set; } = "#d9f871";

        [JsonPropertyName("key_edge")]
        public string KeyEdge { get; set; } = "#aabd51";

        [JsonPropertyName("key_ink")]
        public string KeyInk { get; set; } = "#1f2937";

        [JsonPropertyName("input_ink")]
        public string InputInk { get; set; } = "#f7f4ff";
    }

    private sealed class KeyAppearanceDto
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("font_size")]
        public int FontSize { get; set; }

        [JsonPropertyName("top_color")]
        public string TopColor { get; set; } = "#000000";

        [JsonPropertyName("ink_color")]
        public string InkColor { get; set; } = "#000000";

        [JsonPropertyName("has_top_color")]
        public bool HasTopColor { get; set; }

        [JsonPropertyName("has_ink_color")]
        public bool HasInkColor { get; set; }
    }

}
