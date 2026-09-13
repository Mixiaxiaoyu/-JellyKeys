using Godot;

namespace JellyKeyboardOverlay.UI;

public partial class ThemeSettingsView
{
	private void InstallAboutDialog()
	{
		_aboutBackdrop = new Panel
		{
			Name = "AboutBackdrop",
			Position = Vector2.Zero,
			Size = WindowSize,
			ZIndex = 100,
			MouseFilter = MouseFilterEnum.Stop,
			Visible = false,
		};
		_aboutBackdrop.AddThemeStyleboxOverride(
			"panel",
			MakeStyle(Color.FromHtml("#17151f66"), 28));
		_aboutBackdrop.GuiInput += OnAboutBackdropGuiInput;
		AddChild(_aboutBackdrop);

		_aboutCard = new Panel
		{
			Name = "AboutCard",
			Position = new Vector2(200f, 174f),
			Size = new Vector2(540f, 476f),
			MouseFilter = MouseFilterEnum.Stop,
		};
		var cardStyle = MakeStyle(UiDesignTokens.Surface, 24);
		cardStyle.BorderColor = Color.FromHtml("#dedde2");
		cardStyle.SetBorderWidthAll(1);
		cardStyle.ShadowColor = Color.FromHtml("#00000018");
		cardStyle.ShadowSize = 8;
		cardStyle.ShadowOffset = new Vector2(0f, 3f);
		_aboutCard.AddThemeStyleboxOverride("panel", cardStyle);
		_aboutCard.GuiInput += OnAboutCardGuiInput;
		_aboutBackdrop.AddChild(_aboutCard);

		var appIcon = new TextureRect
		{
			Name = "AppIcon",
			Position = new Vector2(32f, 28f),
			Size = new Vector2(80f, 80f),
			Texture = GD.Load<Texture2D>("res://assets/app_icon.png"),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_aboutCard.AddChild(appIcon);

		CreateAboutLabel("Title", "果冻键显 JellyKeys", new Vector2(132f, 34f), new Vector2(360f, 42f), 28, UiDesignTokens.Ink);
		var version = CreateAboutLabel("Version", $"版本 {AppVersion}", new Vector2(132f, 82f), new Vector2(96f, 26f), 13, UiDesignTokens.Ink);
		var versionStyle = MakeStyle(UiDesignTokens.Accent, 8);
		versionStyle.ContentMarginLeft = 10f;
		versionStyle.ContentMarginRight = 10f;
		version.AddThemeStyleboxOverride("normal", versionStyle);

		var divider = new ColorRect
		{
			Name = "Divider",
			Position = new Vector2(32f, 130f),
			Size = new Vector2(476f, 1f),
			Color = Color.FromHtml("#dedde2"),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_aboutCard.AddChild(divider);

		CreateAboutLabel("AuthorCaption", "作者", new Vector2(32f, 146f), new Vector2(100f, 36f), 14, Color.FromHtml("#929098"));
		CreateAboutLabel("AuthorValue", "米夏小雨", new Vector2(150f, 146f), new Vector2(350f, 36f), 19, UiDesignTokens.Ink);
		CreateAboutLabel("TechCaption", "开发技术", new Vector2(32f, 194f), new Vector2(100f, 36f), 14, Color.FromHtml("#929098"));
		CreateAboutLabel("TechValue", "Godot 4.7 · C# / .NET 8 · Win32", new Vector2(150f, 194f), new Vector2(350f, 36f), 17, UiDesignTokens.Ink);
		CreateAboutLabel("WebsiteCaption", "作者网站", new Vector2(32f, 242f), new Vector2(100f, 36f), 14, Color.FromHtml("#929098"));
		CreateAboutLink("WebsiteLink", "mixiaxiaoyu.cc  ↗", AuthorWebsiteUrl, new Vector2(150f, 242f));
		CreateAboutLabel("GitHubCaption", "GitHub", new Vector2(32f, 284f), new Vector2(100f, 36f), 14, Color.FromHtml("#929098"));
		CreateAboutLink("GitHubLink", "github.com/Mixiaxiaoyu/-JellyKeys  ↗", GitHubUrl, new Vector2(150f, 284f));

		CreateAboutLabel("IntroCaption", "项目简介", new Vector2(32f, 338f), new Vector2(120f, 28f), 14, Color.FromHtml("#929098"));
		var intro = CreateAboutLabel(
			"IntroValue",
			"实时呈现键盘操作，支持主题定制与直播画面叠加。",
			new Vector2(32f, 366f),
			new Vector2(476f, 38f),
			17,
			Color.FromHtml("#29282d"));
		intro.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		intro.VerticalAlignment = VerticalAlignment.Top;
		var introEnglish = CreateAboutLabel(
			"IntroEnglish",
			"Real-time keystroke visualization with custom themes for streaming overlays.",
			new Vector2(32f, 405f),
			new Vector2(476f, 42f),
			13,
			Color.FromHtml("#9b99a1"));
		introEnglish.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		introEnglish.VerticalAlignment = VerticalAlignment.Top;

	}

	private Label CreateAboutLabel(
		string name,
		string text,
		Vector2 position,
		Vector2 size,
		int fontSize,
		Color color)
	{
		var label = new Label
		{
			Name = name,
			Text = text,
			Position = position,
			Size = size,
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		label.AddThemeFontOverride("font", _uiFont);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		_aboutCard.AddChild(label);
		return label;
	}

	private Label CreateAboutLink(string name, string text, string url, Vector2 position)
	{
		var hasUrl = !string.IsNullOrWhiteSpace(url);
		var displayText = hasUrl ? text : "待补充";
		var textWidth = _uiFont.GetStringSize(displayText, HorizontalAlignment.Left, -1f, 14).X;
		var link = CreateAboutLabel(name, displayText, position,
			new Vector2(Mathf.Min(358f, textWidth + 8f), 36f), 14,
			Color.FromHtml(hasUrl ? "#5f5d65" : "#aaa8b0"));
		if (hasUrl)
		{
			link.MouseFilter = MouseFilterEnum.Stop;
			link.MouseDefaultCursorShape = CursorShape.PointingHand;
			link.TooltipText = url;
			link.GuiInput += @event =>
			{
				if (@event is InputEventMouseButton mouseButton &&
					mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
				{
					OpenAboutLink(url);
					GetViewport().SetInputAsHandled();
				}
			};
		}
		return link;
	}

	private void ShowAboutDialog()
	{
		HideThemeContextMenu();
		HideKeyContextMenu();
		FinishThemeNameEditing();
		_aboutBackdrop.Visible = true;
		_aboutBackdrop.MoveToFront();
	}

	private void HideAboutDialog()
	{
		_aboutBackdrop.Visible = false;
	}

	private void OnAboutBackdropGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed &&
			!_aboutCard.GetGlobalRect().HasPoint(mouseButton.Position))
		{
			HideAboutDialog();
			AcceptEvent();
		}
	}

	private void OnAboutCardGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
		{
			HideAboutDialog();
			AcceptEvent();
		}
	}

	private static void OpenAboutLink(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return;
		}

		var error = OS.ShellOpen(url);
		if (error != Error.Ok)
		{
			GD.PushWarning($"无法打开链接：{url} ({error})");
		}
	}

}
