using Godot;
using JellyKeyboardOverlay.Layout;

namespace JellyKeyboardOverlay.UI;

public static class KeyboardTextAtlas
{
    public const float Scale = 2f;
    public const float CanvasPadding = 16f;
    public const float SafeInset = 8f;
    public const float GuideExclusion = 4f;
    private static readonly Lazy<Texture2D?> DarkDefault = new(
        () => GD.Load<Texture2D>("res://assets/default_key_text_dark.png"));
    private static readonly Lazy<Texture2D?> WhiteDefault = new(
        () => GD.Load<Texture2D>("res://assets/default_key_text_white.png"));

    public static Texture2D? GetDefaultTexture(bool lightInk) =>
        lightInk ? WhiteDefault.Value : DarkDefault.Value;

    public static Texture2D? SelectDisplayTexture(
        KeyboardDesignData design, KeyDefinition definition, Texture2D? importedTexture)
    {
        if (design.TextLabelFallbackIds.Contains(definition.Id))
        {
            return null;
        }

        if (importedTexture is not null)
        {
            return importedTexture;
        }

        var appearance = design.Resolve(definition);
        if (HasCustomLabel(appearance, definition))
        {
            return null;
        }

        return GetDefaultTexture(UsesLightInk(design, appearance));
    }

    public static bool HasCustomLabel(KeyAppearanceData appearance, KeyDefinition definition) =>
        !string.Equals(appearance.Label, definition.Label, StringComparison.Ordinal) ||
        appearance.FontSize != KeyCapView.DefaultLabelSizeFor(definition.Rect.Size.X);

    private static bool UsesLightInk(KeyboardDesignData design, KeyAppearanceData appearance)
    {
        var ink = appearance.HasInkColor ? appearance.InkColor : design.Palette.KeyInk;
        var brightness = ink.R * 0.2126f + ink.G * 0.7152f + ink.B * 0.0722f;
        return brightness >= 0.5f;
    }

    public static byte[] BuildDefaultPackageAtlas(KeyboardDesignData design, KeyboardLayoutData layout)
    {
        var atlasSize = GetAtlasSize(layout);
        var dark = GetDefaultTexture(false)?.GetImage()
            ?? throw new InvalidDataException("找不到默认黑色键字贴图。");
        var white = GetDefaultTexture(true)?.GetImage()
            ?? throw new InvalidDataException("找不到默认白色键字贴图。");
        if (dark.GetSize() != atlasSize || white.GetSize() != atlasSize)
        {
            throw new InvalidDataException("默认键字贴图尺寸与导出规范不一致。");
        }

        dark.Convert(Image.Format.Rgba8);
        white.Convert(Image.Format.Rgba8);
        var combined = Image.CreateEmpty(atlasSize.X, atlasSize.Y, false, Image.Format.Rgba8);
        foreach (var definition in layout.Keys)
        {
            var appearance = design.Resolve(definition);
            if (HasCustomLabel(appearance, definition))
            {
                continue;
            }

            var region = GetImportRegion(definition);
            var rect = new Rect2I(
                (int)region.Position.X, (int)region.Position.Y,
                (int)region.Size.X, (int)region.Size.Y);
            combined.BlitRect(UsesLightInk(design, appearance) ? white : dark, rect, rect.Position);
        }

        return combined.SavePngToBuffer();
    }

    public static Vector2I GetAtlasSize(KeyboardLayoutData layout)
    {
        return new Vector2I(
            Mathf.CeilToInt(layout.ContentSize.X * Scale + CanvasPadding * 2f),
            Mathf.CeilToInt(layout.ContentSize.Y * Scale + CanvasPadding * 2f));
    }

    public static Rect2 GetSlot(KeyDefinition definition)
    {
        return new Rect2(
            definition.Rect.Position * Scale + Vector2.One * CanvasPadding,
            definition.Rect.Size * Scale);
    }

    public static Rect2 GetSafeArea(KeyDefinition definition)
    {
        return GetSlot(definition).Grow(-SafeInset);
    }

    public static Rect2 GetImportRegion(KeyDefinition definition)
    {
        return GetSafeArea(definition).Grow(-GuideExclusion);
    }

    public static Texture2D? CreateTexture(byte[]? png)
    {
        if (png is null || png.Length == 0)
        {
            return null;
        }

        var image = new Image();
        if (image.LoadPngFromBuffer(png) != Error.Ok)
        {
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }
}

public partial class KeyboardTextTemplateRenderer : Control
{
    private readonly SystemFont _font = new()
    {
        FontNames = new[] { "Microsoft YaHei UI", "Arial" },
        FontWeight = 700,
    };

    public required KeyboardLayoutData Layout { get; init; }
    public required KeyboardDesignData Design { get; init; }

    public override void _Draw()
    {
        foreach (var definition in Layout.Keys)
        {
            if (!KeyboardTypeCatalog.Includes(definition.Id, Design.KeyboardType, Layout.Keys))
            {
                continue;
            }

            var safe = KeyboardTextAtlas.GetSafeArea(definition);
            var guide = new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                BorderColor = Color.FromHtml("#ff2525"),
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                BorderWidthLeft = 2,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomLeft = 12,
                CornerRadiusBottomRight = 12,
            };
            DrawStyleBox(guide, safe);

            var appearance = Design.Resolve(definition);
            var fontSize = Mathf.Clamp(appearance.FontSize * 2, 18, 38);
            var textSize = _font.GetStringSize(appearance.Label, HorizontalAlignment.Left, -1f, fontSize);
            if (textSize.X > safe.Size.X - 8f)
            {
                fontSize = Mathf.Max(11, Mathf.FloorToInt(fontSize * (safe.Size.X - 8f) / textSize.X));
                textSize = _font.GetStringSize(appearance.Label, HorizontalAlignment.Left, -1f, fontSize);
            }
            var baseline = safe.Position + new Vector2(
                0f,
                (safe.Size.Y - textSize.Y) * 0.5f + _font.GetAscent(fontSize));
            DrawString(
                _font,
                baseline,
                appearance.Label,
                HorizontalAlignment.Center,
                safe.Size.X,
                fontSize,
                Colors.White);
        }
    }

    public static async Task<Error> ExportAsync(Node owner, string path, KeyboardLayoutData layout, KeyboardDesignData design)
    {
        var atlasSize = KeyboardTextAtlas.GetAtlasSize(layout);
        var viewport = new SubViewport
        {
            Name = "TextTemplateViewport",
            Size = atlasSize,
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
        };
        var renderer = new KeyboardTextTemplateRenderer
        {
            Layout = layout,
            Design = design,
            Size = atlasSize,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        viewport.AddChild(renderer);
        owner.AddChild(viewport);
        renderer.QueueRedraw();

        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);

        var image = viewport.GetTexture().GetImage();
        var error = image.SavePng(path);
        viewport.QueueFree();
        return error;
    }
}
