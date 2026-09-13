using Godot;
using JellyKeyboardOverlay.Layout;
using JellyKeyboardOverlay.Overlay;
using JellyKeyboardOverlay.Settings;

namespace JellyKeyboardOverlay.UI;

public partial class ThemeSettingsView : Control
{
	private enum SettingsTab { Layout, Design, Themes }

	public static readonly Vector2I WindowSize = new(940, 824);
	private const string AppVersion = "1.0.0";
	private const string AuthorWebsiteUrl = "https://mixiaxiaoyu.cc";
	private const string GitHubUrl = "https://github.com/Mixiaxiaoyu/-JellyKeys";
	private readonly Dictionary<string, KeyCapView> _previewKeys = new(StringComparer.Ordinal);
	private readonly Dictionary<int, Button> _typeButtons = new();
	private readonly List<KeyboardDesignData> _library = new();
	private readonly List<Button> _themeCards = new();
	private Label _deviceLayoutStatus = null!;
	private Label _deviceLayoutHint = null!;
	private Panel _deviceLayoutPreviewPanel = null!;
	private KeyboardThumbnailView _deviceLayoutPreview = null!;
	private OptionButton _deviceLayoutSelect = null!;
	private Button _deviceLayoutRescan = null!;
	private KeyboardLayoutMode _deviceLayoutMode;
	private int _manualKeyboardType = 108;
	private KeyboardLayoutSuggestion _deviceLayoutSuggestion;
	private bool _syncingDeviceLayout;
	private UiMotionScope _motionScope = null!;
	private bool _animationsEnabled;
	private SettingsTab _activeTab;
	private readonly SystemFont _uiFont = new()
	{
		FontNames = new[] { "HONOR Sans CN", "Microsoft YaHei UI", "Segoe UI" },
		FontWeight = 500,
	};

	private Panel _background = null!;
	private Button _closeButton = null!;
	private Button _aboutButton = null!;
	private Panel _aboutBackdrop = null!;
	private Panel _aboutCard = null!;
	private Panel _notice = null!;
	private Label _noticeLabel = null!;
	private Godot.Timer _noticeTimer = null!;
	private Button _layoutTab = null!;
	private Button _designTab = null!;
	private Button _themesTab = null!;
	private Control _layoutContent = null!;
	private Control _designContent = null!;
	private Control _themeContent = null!;
	private Label _themeNameDisplay = null!;
	private LineEdit _themeNameEdit = null!;
	private Panel _previewPanel = null!;
	private Control _previewHost = null!;
	private Panel? _previewDeckDepth;
	private Panel? _previewDeck;
	private GridContainer _themeGrid = null!;
	private HSlider _hueSlider = null!;
	private HSlider _saturationSlider = null!;
	private HSlider _valueSlider = null!;
	private ColorGradientTrack _hueTrack = null!;
	private ColorGradientTrack _saturationTrack = null!;
	private ColorGradientTrack _valueTrack = null!;
	private Button _exportSpecificationButton = null!;
	private Button _importDesignButton = null!;
	private Button _newThemeButton = null!;
	private Button _saveThemeButton = null!;
	private Button _importThemeButton = null!;
	private Button _exportThemeButton = null!;
	private Button _applyThemeButton = null!;
	private Panel _themeContextMenu = null!;
	private Button _deleteThemeButton = null!;
	private Panel _keyContextMenu = null!;
	private Button _copyKeyColorButton = null!;
	private Button _pasteKeyColorButton = null!;
	private FileDialog _exportSpecificationDialog = null!;
	private FileDialog _importDesignDialog = null!;
	private FileDialog _importThemeDialog = null!;
	private FileDialog _exportThemeDialog = null!;

	private KeyboardLayoutData _layout = null!;
	private KeyboardDesignData _working = KeyboardDesignData.FromPreset(KeyboardThemes.Presets[0]);
	private Texture2D? _textAtlas;
	private readonly HashSet<string> _selectedKeyIds = new(StringComparer.Ordinal);
	private string _primarySelectedKeyId = "KeyA";
	private bool _deckSelected;
	private int _selectedLibraryIndex = -1;
	private bool _syncingSliders;
	private bool _syncingThemeName;
	private bool _editingThemeName;
	private string? _contextThemeId;
	private string? _contextKeyId;
	private Color? _copiedKeyColor;

	public event Action? CloseRequested;
	public event Action<KeyboardDesignData>? DesignChanged;
	public event Action<KeyboardLayoutMode, int>? DeviceLayoutChanged;
	public event Action? DeviceLayoutRescanRequested;

	public override void _Ready()
	{
		_motionScope = new UiMotionScope(this);
		_background = GetNode<Panel>("Background");
		_closeButton = GetNode<Button>("CloseButton");
		_aboutButton = GetNode<Button>("AboutButton");
		_aboutButton.Text = $"关于  {AppVersion}";
		_layoutTab = GetNode<Button>("LayoutTab");
		_designTab = GetNode<Button>("DesignTab");
		_themesTab = GetNode<Button>("ThemesTab");
		_layoutContent = GetNode<Control>("LayoutContent");
		_designContent = GetNode<Control>("DesignContent");
		_themeContent = GetNode<Control>("ThemeContent");
		_themeNameDisplay = GetNode<Label>("DesignContent/PreviewPanel/ThemeNameDisplay");
		_themeNameEdit = GetNode<LineEdit>("DesignContent/PreviewPanel/ThemeNameEdit");
		_previewPanel = GetNode<Panel>("DesignContent/PreviewPanel");
		_previewHost = GetNode<Control>("DesignContent/PreviewPanel/PreviewKeys");
		_deviceLayoutStatus = GetNode<Label>("LayoutContent/DeviceLayoutInfo/DeviceLayoutStatus");
		_deviceLayoutHint = GetNode<Label>("LayoutContent/DeviceLayoutInfo/DeviceLayoutHint");
		_deviceLayoutPreviewPanel = GetNode<Panel>("LayoutContent/DeviceLayoutPreviewPanel");
		_deviceLayoutPreview = GetNode<KeyboardThumbnailView>("LayoutContent/DeviceLayoutPreviewPanel/DeviceLayoutPreview");
		_deviceLayoutSelect = GetNode<OptionButton>("LayoutContent/DeviceLayoutSelect");
		_deviceLayoutRescan = GetNode<Button>("LayoutContent/DeviceLayoutRescan");
		_themeGrid = GetNode<GridContainer>("ThemeContent/ThemeScroll/ThemeGrid");
		_hueSlider = GetNode<HSlider>("DesignContent/HueSlider");
		_saturationSlider = GetNode<HSlider>("DesignContent/SaturationSlider");
		_valueSlider = GetNode<HSlider>("DesignContent/ValueSlider");
		_exportSpecificationButton = GetNode<Button>("DesignContent/ExportSpecificationButton");
		_importDesignButton = GetNode<Button>("DesignContent/ImportDesignButton");
		_newThemeButton = GetNode<Button>("DesignContent/NewThemeButton");
		_saveThemeButton = GetNode<Button>("DesignContent/SaveThemeButton");
		_importThemeButton = GetNode<Button>("ThemeContent/ImportThemeButton");
		_exportThemeButton = GetNode<Button>("ThemeContent/ExportThemeButton");
		_applyThemeButton = GetNode<Button>("ThemeContent/ApplyThemeButton");
		_exportSpecificationDialog = GetNode<FileDialog>("ExportSpecificationDialog");
		_importDesignDialog = GetNode<FileDialog>("ImportDesignDialog");
		_importThemeDialog = GetNode<FileDialog>("ImportThemeDialog");
		_exportThemeDialog = GetNode<FileDialog>("ExportThemeDialog");

		foreach (var type in KeyboardTypeCatalog.Supported)
		{
			_typeButtons[type] = GetNode<Button>($"DesignContent/Type{type}");
			var capturedType = type;
			_typeButtons[type].Pressed += () => SelectKeyboardType(capturedType);
		}

		_closeButton.ButtonDown += RequestClose;
		_aboutButton.Pressed += ShowAboutDialog;
		_layoutTab.ButtonDown += ShowLayoutTab;
		_designTab.ButtonDown += ShowDesignTab;
		_themesTab.ButtonDown += ShowThemesTab;
		_hueSlider.ValueChanged += _ => OnColorSliderChanged();
		_saturationSlider.ValueChanged += _ => OnColorSliderChanged();
		_valueSlider.ValueChanged += _ => OnColorSliderChanged();
		_themeNameDisplay.GuiInput += OnThemeNameDisplayGuiInput;
		_themeNameEdit.TextChanged += OnThemeNameChanged;
		_themeNameEdit.TextSubmitted += _ => FinishThemeNameEditing();
		_themeNameEdit.FocusExited += FinishThemeNameEditing;
		_exportSpecificationButton.Pressed += () => _exportSpecificationDialog.PopupCentered(new Vector2I(760, 520));
		_importDesignButton.Pressed += () => _importDesignDialog.PopupCentered(new Vector2I(760, 520));
		_newThemeButton.Pressed += CreateNewTheme;
		_saveThemeButton.Pressed += SaveCurrentTheme;
		_importThemeButton.Pressed += () => _importThemeDialog.PopupCentered(new Vector2I(760, 520));
		_exportThemeButton.Pressed += () => _exportThemeDialog.PopupCentered(new Vector2I(760, 520));
		_applyThemeButton.Pressed += ApplySelectedTheme;
		_exportSpecificationDialog.FileSelected += OnExportSpecificationSelected;
		_importDesignDialog.FileSelected += OnImportDesignSelected;
		_importThemeDialog.FileSelected += OnImportThemeSelected;
		_exportThemeDialog.FileSelected += OnExportThemeSelected;
		_deviceLayoutSelect.AddItem("跟随主题", 0);
		_deviceLayoutSelect.AddItem("自动识别", 1);
		_deviceLayoutSelect.AddSeparator("固定键数");
		foreach (var keyboardType in KeyboardTypeCatalog.Supported.OrderBy(type => type))
		{
			_deviceLayoutSelect.AddItem($"固定 · {keyboardType} 键", keyboardType);
		}
		_deviceLayoutSelect.ItemSelected += OnDeviceLayoutSelected;
		_deviceLayoutRescan.Pressed += () => DeviceLayoutRescanRequested?.Invoke();

		InstallAboutDialog();
		ConfigureFileDialogs();
		ApplyFigmaStyle();
		InstallNotice();
		InstallSliderTracks();
		InstallWindowDragRegion();
		InstallThemeContextMenu();
		InstallKeyContextMenu();

		_closeButton.MoveToFront();
		_layoutTab.MoveToFront();
		_designTab.MoveToFront();
		_themesTab.MoveToFront();
		_aboutButton.MoveToFront();
	}

	public void Open(
		KeyboardDesignData design, KeyboardLayoutData layout,
		KeyboardLayoutMode layoutMode, int manualKeyboardType, KeyboardLayoutSuggestion suggestion,
		bool animate = true)
	{
		_motionScope.CancelAll();
		Modulate = Colors.White;
		_animationsEnabled = false;
		_layout = layout;
		_working = design.Clone();
		_deviceLayoutMode = layoutMode;
		_manualKeyboardType = manualKeyboardType;
		_deviceLayoutSuggestion = suggestion;
		_textAtlas = KeyboardTextAtlas.CreateTexture(_working.TextAtlasPng);
		_primarySelectedKeyId = layout.Keys.Any(key => key.Id == "KeyA") ? "KeyA" : layout.Keys[0].Id;
		_selectedKeyIds.Clear();
		_selectedKeyIds.Add(_primarySelectedKeyId);
		_deckSelected = false;
		_editingThemeName = false;
		_aboutBackdrop.Visible = false;
		_notice.Visible = false;
		_noticeTimer.Stop();
		Visible = true;
		ShowLayoutTab();
		BuildPreview();
		RefreshPreview();
		SyncSlidersFromSelection();
		SyncThemeName();
		RefreshTypeButtons();
		RefreshDeviceLayoutControls();
		AnimateOpen(animate);
	}

	public KeyboardDesignData CurrentDesign => _working.Clone();

	public void SetDeviceLayoutState(
		KeyboardLayoutMode mode, int manualKeyboardType, KeyboardLayoutSuggestion suggestion)
	{
		_deviceLayoutMode = mode;
		_manualKeyboardType = manualKeyboardType;
		_deviceLayoutSuggestion = suggestion;
		RefreshDeviceLayoutControls();
	}

	private void OnDeviceLayoutSelected(long index)
	{
		if (_syncingDeviceLayout)
		{
			return;
		}

		var id = _deviceLayoutSelect.GetItemId((int)index);
		_deviceLayoutMode = id switch
		{
			0 => KeyboardLayoutMode.FollowTheme,
			1 => KeyboardLayoutMode.Automatic,
			_ => KeyboardLayoutMode.Manual,
		};
		if (_deviceLayoutMode == KeyboardLayoutMode.Manual)
		{
			_manualKeyboardType = id;
		}
		RefreshDeviceLayoutControls();
		DeviceLayoutChanged?.Invoke(_deviceLayoutMode, _manualKeyboardType);
	}

	private void RefreshDeviceLayoutControls()
	{
		if (_layout is null)
		{
			return;
		}

		_deviceLayoutStatus.Text = _deviceLayoutSuggestion.KeyboardType is { } detected
			? $"系统建议 {detected} 键" : "自动识别暂不可用";
		_deviceLayoutStatus.TooltipText = _deviceLayoutSuggestion.Message;
		_deviceLayoutSelect.SetItemText(0, $"跟随主题 · {_working.KeyboardType} 键");
		_deviceLayoutSelect.SetItemText(1, _deviceLayoutSuggestion.KeyboardType is { } suggested
			? $"自动识别 · {suggested} 键" : "自动识别 · 无结果");
		_deviceLayoutSelect.SetItemDisabled(1,
			_deviceLayoutSuggestion.KeyboardType is null && _deviceLayoutMode != KeyboardLayoutMode.Automatic);
		var selectedId = _deviceLayoutMode switch
		{
			KeyboardLayoutMode.Automatic => 1,
			KeyboardLayoutMode.Manual => _manualKeyboardType,
			_ => 0,
		};
		_syncingDeviceLayout = true;
		for (var index = 0; index < _deviceLayoutSelect.ItemCount; index++)
		{
			if (_deviceLayoutSelect.GetItemId(index) == selectedId)
			{
				_deviceLayoutSelect.Select(index);
				break;
			}
		}
		_syncingDeviceLayout = false;
		RefreshDeviceLayoutThumbnail();
	}

	private void RefreshDeviceLayoutThumbnail()
	{
		if (_layout is null)
		{
			return;
		}

		var displayType = OverlaySettings.ResolveKeyboardType(
			_deviceLayoutMode, _manualKeyboardType,
			_working.KeyboardType, _deviceLayoutSuggestion.KeyboardType);
		_deviceLayoutSelect.SetItemText(0, $"跟随主题 · {_working.KeyboardType} 键");
		_deviceLayoutHint.Text = _deviceLayoutMode switch
		{
			KeyboardLayoutMode.Manual => $"换主题后仍显示 {displayType} 键",
			KeyboardLayoutMode.Automatic when _deviceLayoutSuggestion.KeyboardType is null =>
				"暂按主题布局显示",
			KeyboardLayoutMode.Automatic => "重新检测可更新建议",
			_ => "换主题时自动切换",
		};
		var thumbnailDesign = new KeyboardDesignData
		{
			Id = _working.Id,
			DisplayName = _working.DisplayName,
			KeyboardType = displayType,
			Palette = _working.Palette,
		};
		foreach (var pair in _working.Keys)
		{
			thumbnailDesign.Keys[pair.Key] = pair.Value;
		}
		_deviceLayoutPreview.Configure(thumbnailDesign, _layout);
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible)
		{
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			if (_aboutBackdrop.Visible)
			{
				HideAboutDialog();
				GetViewport().SetInputAsHandled();
				return;
			}

			HideThemeContextMenu();
			HideKeyContextMenu();
			return;
		}

		if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
		{
			return;
		}

		if (_themeContextMenu.Visible && !_themeContextMenu.GetGlobalRect().HasPoint(mouseButton.Position))
		{
			HideThemeContextMenu();
		}

		if (_keyContextMenu.Visible && !_keyContextMenu.GetGlobalRect().HasPoint(mouseButton.Position))
		{
			HideKeyContextMenu();
		}

		if (_editingThemeName && mouseButton.ButtonIndex == MouseButton.Left &&
			!_themeNameEdit.GetGlobalRect().HasPoint(mouseButton.Position))
		{
			FinishThemeNameEditing();
		}
	}

	public void ShowThemeLibrary()
	{
		ShowThemesTab();
	}

	private void OnThemeNameDisplayGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton ||
			mouseButton.ButtonIndex != MouseButton.Left || !mouseButton.Pressed || !mouseButton.DoubleClick)
		{
			return;
		}

		BeginThemeNameEditing();
		AcceptEvent();
	}

	private void BeginThemeNameEditing()
	{
		if (_editingThemeName)
		{
			return;
		}

		_editingThemeName = true;
		SyncThemeName();
		_themeNameEdit.GrabFocus();
		_themeNameEdit.SelectAll();
	}

	private void FinishThemeNameEditing()
	{
		if (!_editingThemeName)
		{
			return;
		}

		CommitThemeName();
		_editingThemeName = false;
		SyncThemeName();
	}

	private void OnThemeNameChanged(string value)
	{
		if (_syncingThemeName)
		{
			return;
		}

		_working.DisplayName = value;
		_saveThemeButton.Text = "保存方案";
	}

	private void CommitThemeName()
	{
		if (_syncingThemeName)
		{
			return;
		}

		_working.DisplayName = string.IsNullOrWhiteSpace(_themeNameEdit.Text)
			? "未命名方案"
			: _themeNameEdit.Text.Trim();
		SyncThemeName();
		DesignChanged?.Invoke(_working.Clone());
	}

	private void SyncThemeName()
	{
		_syncingThemeName = true;
		_themeNameDisplay.Text = _working.DisplayName;
		_themeNameEdit.Text = _working.DisplayName;
		_themeNameDisplay.Visible = !_editingThemeName;
		_themeNameEdit.Visible = _editingThemeName;
		_syncingThemeName = false;
	}

	private void BuildPreview()
	{
		foreach (var child in _previewHost.GetChildren())
		{
			child.Free();
		}
		_previewKeys.Clear();
		_previewDeckDepth = null;
		_previewDeck = null;

		var visibleBounds = KeyboardTypeCatalog.VisibleBounds(_working.KeyboardType, _layout.Keys);
		var deckSize = visibleBounds.Size + KeyboardVisualMetrics.DeckPadding * 2f;
		var visualSize = deckSize + new Vector2(0f, KeyboardVisualMetrics.DeckDepth);
		var scale = Mathf.Min(
			_previewHost.Size.X / visualSize.X,
			_previewHost.Size.Y / visualSize.Y) * 0.96f;
		var previewRoot = new Control
		{
			Name = "KeyboardPreview",
			Size = visualSize,
			Scale = Vector2.One * scale,
			Position = (_previewHost.Size - visualSize * scale) * 0.5f,
			MouseFilter = MouseFilterEnum.Pass,
		};
		_previewHost.AddChild(previewRoot);

		_previewDeckDepth = new Panel
		{
			Name = "KeyboardDeckDepth",
			Position = new Vector2(0f, KeyboardVisualMetrics.DeckDepth),
			Size = deckSize,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		previewRoot.AddChild(_previewDeckDepth);

		_previewDeck = new Panel
		{
			Name = "KeyboardDeck",
			Size = deckSize,
			MouseFilter = MouseFilterEnum.Stop,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			TooltipText = "点击底板修改底板颜色；按键支持 Ctrl / Shift 多选与右键复制、粘贴颜色",
		};
		_previewDeck.GuiInput += OnPreviewDeckGuiInput;
		previewRoot.AddChild(_previewDeck);

		var keyLayer = new Control
		{
			Name = "Keys",
			Position = KeyboardVisualMetrics.DeckPadding - visibleBounds.Position,
			Size = visibleBounds.Size,
			MouseFilter = MouseFilterEnum.Pass,
		};
		_previewDeck.AddChild(keyLayer);

		foreach (var definition in _layout.Keys)
		{
			var key = new KeyCapView(definition.Id, definition.Label);
			keyLayer.AddChild(key);
			key.Configure(KeyboardTypeCatalog.DisplayRect(definition, _working.KeyboardType, _layout.Keys));
			key.SetPreviewSelectable(true);
			key.SelectionRequested += SelectKey;
			key.ContextMenuRequested += ShowKeyContextMenu;
			_previewKeys[definition.Id] = key;
		}

		NormalizeKeySelection();
	}

	private void SelectKey(string keyId, bool additive)
	{
		HideKeyContextMenu();
		_deckSelected = false;

		if (!additive)
		{
			_selectedKeyIds.Clear();
			_selectedKeyIds.Add(keyId);
			_primarySelectedKeyId = keyId;
		}
		else if (_selectedKeyIds.Contains(keyId))
		{
			if (_selectedKeyIds.Count > 1)
			{
				_selectedKeyIds.Remove(keyId);
				if (string.Equals(_primarySelectedKeyId, keyId, StringComparison.Ordinal))
				{
					_primarySelectedKeyId = _selectedKeyIds.First();
				}
			}
		}
		else
		{
			_selectedKeyIds.Add(keyId);
			_primarySelectedKeyId = keyId;
		}

		RefreshPreview();
		SyncSlidersFromSelection();
	}

	private void NormalizeKeySelection()
	{
		var visibleKeyIds = _layout.Keys
			.Where(key => KeyboardTypeCatalog.Includes(key.Id, _working.KeyboardType, _layout.Keys))
			.Select(key => key.Id)
			.ToHashSet(StringComparer.Ordinal);

		_selectedKeyIds.RemoveWhere(keyId => !visibleKeyIds.Contains(keyId));
		if (_deckSelected)
		{
			return;
		}

		if (_selectedKeyIds.Count == 0)
		{
			_primarySelectedKeyId = visibleKeyIds.First();
			_selectedKeyIds.Add(_primarySelectedKeyId);
		}
		else if (!_selectedKeyIds.Contains(_primarySelectedKeyId))
		{
			_primarySelectedKeyId = _selectedKeyIds.First();
		}
	}

	private void SelectDeck()
	{
		HideKeyContextMenu();
		_deckSelected = true;
		_selectedKeyIds.Clear();
		RefreshPreview();
		SyncSlidersFromSelection();
	}

	private void OnPreviewDeckGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton ||
			mouseButton.ButtonIndex != MouseButton.Left || !mouseButton.Pressed)
		{
			return;
		}

		SelectDeck();
		AcceptEvent();
	}

	private void SelectKeyboardType(int keyboardType)
	{
		_working.KeyboardType = keyboardType;
		_saveThemeButton.Text = "保存方案";
		RefreshTypeButtons();
		BuildPreview();
		RefreshPreview();
		SyncSlidersFromSelection();
		RefreshDeviceLayoutThumbnail();
		DesignChanged?.Invoke(_working.Clone());
	}

	private void SyncSlidersFromSelection()
	{
		var color = GetSelectedColor();
		_syncingSliders = true;
		_hueSlider.SetValueNoSignal(color.H * 360.0);
		_saturationSlider.SetValueNoSignal(color.S * 100.0);
		_valueSlider.SetValueNoSignal(color.V * 100.0);
		_syncingSliders = false;
		UpdateSliderTracks();
	}

	private void OnColorSliderChanged()
	{
		if (_syncingSliders || _layout is null)
		{
			return;
		}

		var color = Color.FromHsv(
			(float)(_hueSlider.Value / 360.0),
			(float)(_saturationSlider.Value / 100.0),
			(float)(_valueSlider.Value / 100.0));

		if (_deckSelected)
		{
			_working.Palette = _working.Palette with { Deck = color };
		}
		else
		{
			foreach (var definition in _layout.Keys.Where(key => _selectedKeyIds.Contains(key.Id)))
			{
				var appearance = _working.GetOrCreate(definition);
				appearance.TopColor = color;
				appearance.HasTopColor = true;
			}
		}

		UpdateSliderTracks();
		_saveThemeButton.Text = "保存方案";
		RefreshPreview();
		RefreshDeviceLayoutThumbnail();
		DesignChanged?.Invoke(_working.Clone());
	}

	private Color GetSelectedColor()
	{
		if (_deckSelected)
		{
			return _working.Palette.Deck;
		}

		var definition = _layout.Keys.First(key => key.Id == _primarySelectedKeyId);
		var appearance = _working.Resolve(definition);
		return appearance.HasTopColor ? appearance.TopColor : _working.Palette.KeyTop;
	}

	private void RefreshPreview()
	{
		ApplyPreviewDeckStyle();
		foreach (var definition in _layout.Keys)
		{
			var key = _previewKeys[definition.Id];
			key.Visible = KeyboardTypeCatalog.Includes(definition.Id, _working.KeyboardType, _layout.Keys);
			key.ApplyTheme(_working.Palette, _working.Resolve(definition), _selectedKeyIds.Contains(definition.Id));
			key.ApplyTextAtlas(
				KeyboardTypeCatalog.Includes(definition.Id, _working.KeyboardType, _layout.Keys)
					? KeyboardTextAtlas.SelectDisplayTexture(_working, definition, _textAtlas)
					: null,
				KeyboardTextAtlas.GetImportRegion(definition));
		}
	}

	private void RefreshTypeButtons()
	{
		foreach (var pair in _typeButtons)
		{
			pair.Value.AddThemeColorOverride("font_color", pair.Key == _working.KeyboardType ? Colors.Black : Color.FromHtml("#b7b7b7"));
			pair.Value.AddThemeColorOverride("font_hover_color", Colors.Black);
			pair.Value.AddThemeFontOverride("font", _uiFont);
			pair.Value.AddThemeFontSizeOverride("font_size", 24);
		}
	}

	private Control ActiveTabContent => _activeTab switch
	{
		SettingsTab.Design => _designContent,
		SettingsTab.Themes => _themeContent,
		_ => _layoutContent,
	};

	private void ShowLayoutTab() => SelectTab(SettingsTab.Layout);
	private void ShowDesignTab() => SelectTab(SettingsTab.Design);
	private void ShowThemesTab() => SelectTab(SettingsTab.Themes);

	private void SelectTab(SettingsTab selected)
	{
		if (selected != SettingsTab.Design)
		{
			HideKeyContextMenu();
		}
		if (selected != SettingsTab.Themes)
		{
			HideThemeContextMenu();
		}

		_activeTab = selected;
		_layoutContent.Visible = selected == SettingsTab.Layout;
		_designContent.Visible = selected == SettingsTab.Design;
		_themeContent.Visible = selected == SettingsTab.Themes;
		ApplyTabStyle(_layoutTab, selected == SettingsTab.Layout);
		ApplyTabStyle(_designTab, selected == SettingsTab.Design);
		ApplyTabStyle(_themesTab, selected == SettingsTab.Themes);
		if (selected == SettingsTab.Themes)
		{
			LoadThemeLibrary();
		}
		if (_animationsEnabled)
		{
			AnimateItemsIn(ActiveTabContent.GetChildren().OfType<Control>().Where(item => item.Visible));
		}
	}

	private void RequestClose()
	{
		CloseRequested?.Invoke();
	}

	private void LoadThemeLibrary()
	{
		_library.Clear();
		_library.AddRange(KeyboardThemeLibraryCatalog.Load());

		_selectedLibraryIndex = _library.FindIndex(item => item.Id == _working.Id);
		if (_selectedLibraryIndex < 0 && _library.Count > 0)
		{
			_selectedLibraryIndex = 0;
		}
		RefreshThemeCards();
		UpdateThemeLibraryActions();
	}

	private void RefreshThemeCards()
	{
		foreach (var child in _themeGrid.GetChildren())
		{
			child.Free();
		}
		_themeCards.Clear();

		for (var index = 0; index < _library.Count; index++)
		{
			var capturedIndex = index;
			var design = _library[index];
			var card = new Button
			{
				Name = $"ThemeCard{index}",
				CustomMinimumSize = new Vector2(188f, 148f),
				FocusMode = FocusModeEnum.None,
				MouseDefaultCursorShape = CursorShape.PointingHand,
				TooltipText = "左键选择 · 右键删除",
			};
			card.Pressed += () =>
			{
				_selectedLibraryIndex = capturedIndex;
				RefreshThemeCardSelection();
			};
			var capturedThemeId = design.Id;
			card.GuiInput += @event => OnThemeCardGuiInput(@event, capturedThemeId);
			ApplyThemeCardStyle(card, index == _selectedLibraryIndex);

			var thumbnail = new KeyboardThumbnailView
			{
				Name = "KeyboardThumbnail",
				Position = new Vector2(8f, 30f),
				Size = new Vector2(172f, 70f),
			};
			thumbnail.Configure(design, _layout);
			card.AddChild(thumbnail);

			var label = new Label
			{
				Text = design.DisplayName,
				Position = new Vector2(4f, 101f),
				Size = new Vector2(180f, 27f),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			label.AddThemeFontOverride("font", _uiFont);
			label.AddThemeFontSizeOverride("font_size", 17);
			label.AddThemeColorOverride("font_color", UiDesignTokens.Ink);
			card.AddChild(label);

			var typeLabel = new Label
			{
				Text = $"{design.KeyboardType} 键",
				Position = new Vector2(4f, 126f),
				Size = new Vector2(180f, 18f),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			typeLabel.AddThemeFontOverride("font", _uiFont);
			typeLabel.AddThemeFontSizeOverride("font_size", 13);
			typeLabel.AddThemeColorOverride("font_color", Color.FromHtml("#888888"));
			card.AddChild(typeLabel);

			_themeGrid.AddChild(card);
			_themeCards.Add(card);
		}
	}

	private void RefreshThemeCardSelection()
	{
		for (var index = 0; index < _themeCards.Count; index++)
		{
			ApplyThemeCardStyle(_themeCards[index], index == _selectedLibraryIndex);
		}
	}

	private void OnThemeCardGuiInput(InputEvent @event, string themeId)
	{
		if (@event is not InputEventMouseButton mouseButton ||
			!mouseButton.Pressed || mouseButton.ButtonIndex != MouseButton.Right)
		{
			return;
		}

		var index = _library.FindIndex(theme => string.Equals(theme.Id, themeId, StringComparison.Ordinal));
		if (index < 0)
		{
			return;
		}

		_selectedLibraryIndex = index;
		RefreshThemeCardSelection();
		ShowThemeContextMenu(themeId, GetViewport().GetMousePosition());
		AcceptEvent();
	}

	private void InstallThemeContextMenu()
	{
		_themeContextMenu = new Panel
		{
			Name = "ThemeContextMenu",
			Size = new Vector2(116f, 42f),
			ZIndex = 40,
			MouseFilter = MouseFilterEnum.Stop,
			Visible = false,
		};
		var menuStyle = MakeStyle(UiDesignTokens.Surface, 12);
		menuStyle.BorderColor = Color.FromHtml("#dedde2");
		menuStyle.BorderWidthTop = 1;
		menuStyle.BorderWidthRight = 1;
		menuStyle.BorderWidthBottom = 1;
		menuStyle.BorderWidthLeft = 1;
		_themeContextMenu.AddThemeStyleboxOverride("panel", menuStyle);
		AddChild(_themeContextMenu);

		_deleteThemeButton = new Button
		{
			Name = "DeleteThemeButton",
			Position = new Vector2(3f, 3f),
			Size = new Vector2(110f, 36f),
			Text = "删除方案",
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand,
		};
		_deleteThemeButton.Pressed += DeleteContextTheme;
		ApplyCompactDarkMenuButtonStyle(_deleteThemeButton);
		_themeContextMenu.AddChild(_deleteThemeButton);
	}

	private void ShowThemeContextMenu(string themeId, Vector2 viewportPosition)
	{
		HideKeyContextMenu();
		_contextThemeId = themeId;
		_deleteThemeButton.Disabled = _library.Count <= 1;
		_deleteThemeButton.TooltipText = _deleteThemeButton.Disabled ? "至少保留一个主题方案" : string.Empty;
		var menuSize = _themeContextMenu.Size;
		_themeContextMenu.Position = new Vector2(
			Mathf.Clamp(viewportPosition.X, 12f, Size.X - menuSize.X - 12f),
			Mathf.Clamp(viewportPosition.Y, 12f, Size.Y - menuSize.Y - 12f));
		_themeContextMenu.Visible = true;
		_themeContextMenu.MoveToFront();
	}

	private void HideThemeContextMenu()
	{
		if (_themeContextMenu is null)
		{
			return;
		}

		_themeContextMenu.Visible = false;
		_contextThemeId = null;
	}

	private void InstallKeyContextMenu()
	{
		_keyContextMenu = new Panel
		{
			Name = "KeyColorContextMenu",
			Size = new Vector2(132f, 76f),
			ZIndex = 41,
			MouseFilter = MouseFilterEnum.Stop,
			Visible = false,
		};
		var menuStyle = MakeStyle(UiDesignTokens.Surface, 12);
		menuStyle.BorderColor = Color.FromHtml("#dedde2");
		menuStyle.SetBorderWidthAll(1);
		_keyContextMenu.AddThemeStyleboxOverride("panel", menuStyle);
		AddChild(_keyContextMenu);

		_copyKeyColorButton = CreateCompactKeyMenuButton("CopyKeyColorButton", "复制颜色", 4f);
		_copyKeyColorButton.Pressed += CopyContextKeyColor;
		_keyContextMenu.AddChild(_copyKeyColorButton);

		_pasteKeyColorButton = CreateCompactKeyMenuButton("PasteKeyColorButton", "粘贴颜色", 38f);
		_pasteKeyColorButton.Pressed += PasteKeyColorToSelection;
		_keyContextMenu.AddChild(_pasteKeyColorButton);
	}

	private Button CreateCompactKeyMenuButton(string name, string text, float top)
	{
		var button = new Button
		{
			Name = name,
			Text = text,
			Position = new Vector2(4f, top),
			Size = new Vector2(124f, 34f),
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand,
		};
		ApplyCompactLightMenuButtonStyle(button);
		return button;
	}

	private void ShowKeyContextMenu(string keyId)
	{
		if (!_selectedKeyIds.Contains(keyId) || _deckSelected)
		{
			_selectedKeyIds.Clear();
			_selectedKeyIds.Add(keyId);
			_deckSelected = false;
			RefreshPreview();
		}

		_primarySelectedKeyId = keyId;
		_contextKeyId = keyId;
		SyncSlidersFromSelection();
		HideThemeContextMenu();

		var sourceColor = ResolveKeyTopColor(keyId);
		_copyKeyColorButton.TooltipText = $"复制 #{sourceColor.ToHtml(false).ToUpperInvariant()}";
		_pasteKeyColorButton.Disabled = !_copiedKeyColor.HasValue;
		_pasteKeyColorButton.TooltipText = _copiedKeyColor.HasValue
			? $"粘贴到 {_selectedKeyIds.Count} 个按键"
			: "请先复制一个按键颜色";
		PositionContextMenu(_keyContextMenu, GetViewport().GetMousePosition());
		_keyContextMenu.Visible = true;
		_keyContextMenu.MoveToFront();
	}

	private void HideKeyContextMenu()
	{
		if (_keyContextMenu is null)
		{
			return;
		}

		_keyContextMenu.Visible = false;
		_contextKeyId = null;
	}

	private void CopyContextKeyColor()
	{
		if (!string.IsNullOrWhiteSpace(_contextKeyId))
		{
			_copiedKeyColor = ResolveKeyTopColor(_contextKeyId);
		}

		HideKeyContextMenu();
	}

	private void PasteKeyColorToSelection()
	{
		if (!_copiedKeyColor.HasValue || _selectedKeyIds.Count == 0)
		{
			return;
		}

		foreach (var definition in _layout.Keys.Where(key => _selectedKeyIds.Contains(key.Id)))
		{
			var appearance = _working.GetOrCreate(definition);
			appearance.TopColor = _copiedKeyColor.Value;
			appearance.HasTopColor = true;
		}

		HideKeyContextMenu();
		_saveThemeButton.Text = "保存方案";
		RefreshPreview();
		SyncSlidersFromSelection();
		DesignChanged?.Invoke(_working.Clone());
	}

	private Color ResolveKeyTopColor(string keyId)
	{
		var definition = _layout.Keys.First(key => key.Id == keyId);
		var appearance = _working.Resolve(definition);
		return appearance.HasTopColor ? appearance.TopColor : _working.Palette.KeyTop;
	}

	private void PositionContextMenu(Control menu, Vector2 viewportPosition)
	{
		menu.Position = new Vector2(
			Mathf.Clamp(viewportPosition.X, 12f, Size.X - menu.Size.X - 12f),
			Mathf.Clamp(viewportPosition.Y, 12f, Size.Y - menu.Size.Y - 12f));
	}

	private void DeleteContextTheme()
	{
		if (_library.Count <= 1 || string.IsNullOrWhiteSpace(_contextThemeId))
		{
			return;
		}

		var deletedIndex = _library.FindIndex(theme =>
			string.Equals(theme.Id, _contextThemeId, StringComparison.Ordinal));
		if (deletedIndex < 0)
		{
			HideThemeContextMenu();
			return;
		}

		var deletedTheme = _library[deletedIndex];
		try
		{
			KeyboardThemeStore.DeleteTheme(deletedTheme.Id);
		}
		catch (Exception exception)
		{
			GD.PushWarning($"主题删除失败：{exception.Message}");
			return;
		}

		_library.RemoveAt(deletedIndex);
		_selectedLibraryIndex = Mathf.Clamp(deletedIndex, 0, _library.Count - 1);
		HideThemeContextMenu();

		if (string.Equals(deletedTheme.Id, _working.Id, StringComparison.Ordinal))
		{
			_working = _library[_selectedLibraryIndex].Clone();
			_textAtlas = KeyboardTextAtlas.CreateTexture(_working.TextAtlasPng);
			KeyboardThemeStore.SaveActive(_working);
			BuildPreview();
			RefreshPreview();
			SyncSlidersFromSelection();
			SyncThemeName();
			RefreshTypeButtons();
			RefreshDeviceLayoutThumbnail();
			DesignChanged?.Invoke(_working.Clone());
		}

		RefreshThemeCards();
		UpdateThemeLibraryActions();
	}

	private void UpdateThemeLibraryActions()
	{
		var hasSelection = _selectedLibraryIndex >= 0 && _selectedLibraryIndex < _library.Count;
		_exportThemeButton.Disabled = !hasSelection;
		_applyThemeButton.Disabled = !hasSelection;
	}

	private void CreateNewTheme()
	{
		CommitThemeName();
		PersistCurrentTheme();
		var saved = KeyboardThemeStore.LoadLibrary();
		_working = _working.Clone(createNewIdentity: true);
		_working.DisplayName = ThemeNamePolicy.NextAvailable(
			saved.Select(item => item.DisplayName).ToHashSet(StringComparer.Ordinal),
			"新方案");
		_working.Keys.Clear();
		_working.TextAtlasPng = null;
		_textAtlas = null;
		KeyboardThemeStore.UpsertLibrary(_working);
		KeyboardThemeStore.SaveActive(_working);
		_saveThemeButton.Text = "保存方案";
		SyncThemeName();
		RefreshPreview();
		SyncSlidersFromSelection();
		RefreshDeviceLayoutThumbnail();
		DesignChanged?.Invoke(_working.Clone());
	}

	private void SaveCurrentTheme()
	{
		CommitThemeName();
		PersistCurrentTheme();
		_saveThemeButton.Text = "已保存";
	}

	private void PersistCurrentTheme()
	{
		if (string.IsNullOrWhiteSpace(_working.DisplayName))
		{
			_working.DisplayName = "未命名方案";
		}

		KeyboardThemeStore.UpsertLibrary(_working);
		KeyboardThemeStore.SaveActive(_working);
	}

	private void ApplySelectedTheme()
	{
		if (_selectedLibraryIndex < 0 || _selectedLibraryIndex >= _library.Count)
		{
			return;
		}

		_working = _library[_selectedLibraryIndex].Clone();
		_textAtlas = KeyboardTextAtlas.CreateTexture(_working.TextAtlasPng);
		_saveThemeButton.Text = "保存方案";
		KeyboardThemeStore.SaveActive(_working);
		DesignChanged?.Invoke(_working.Clone());
		ShowDesignTab();
		BuildPreview();
		RefreshPreview();
		SyncSlidersFromSelection();
		SyncThemeName();
		RefreshTypeButtons();
		RefreshDeviceLayoutThumbnail();
	}

	private async void OnExportSpecificationSelected(string path)
	{
		try
		{
			if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
			{
				path += ".png";
			}
			var error = await KeyboardTextTemplateRenderer.ExportAsync(this, path, _layout, _working);
			if (error != Error.Ok)
			{
				throw new IOException($"PNG 导出错误：{error}");
			}
			ShowNotice("文字贴图模板已导出");
		}
		catch (Exception exception)
		{
			GD.PushWarning($"文字贴图模板导出失败：{exception.Message}");
			ShowNotice("模板导出失败，请检查保存位置。");
		}
	}

	private void OnImportDesignSelected(string path)
	{
		try
		{
			KeyboardThemeStore.ImportTextAtlas(path, _working, _layout);
			_textAtlas = KeyboardTextAtlas.CreateTexture(_working.TextAtlasPng);
			_saveThemeButton.Text = "保存方案";
			RefreshPreview();
			DesignChanged?.Invoke(_working.Clone());
			ShowNotice("文字贴图已导入");
		}
		catch (Exception exception)
		{
			GD.PushWarning($"文字贴图导入失败：{exception.Message}");
			ShowNotice(FriendlyFileError(exception, "贴图导入失败，请检查文件。"));
		}
	}

	private void OnImportThemeSelected(string path)
	{
		try
		{
			var imported = KeyboardThemeStore.ImportTheme(path, _layout);
			KeyboardThemeStore.UpsertLibrary(imported);
			LoadThemeLibrary();
			_selectedLibraryIndex = _library.FindIndex(item => item.Id == imported.Id);
			RefreshThemeCardSelection();
			ShowNotice("主题已导入，可点击应用方案");
		}
		catch (Exception exception)
		{
			GD.PushWarning($"主题导入失败：{exception.Message}");
			ShowNotice(FriendlyFileError(exception, "主题导入失败，请检查文件。"));
		}
	}

	private void OnExportThemeSelected(string path)
	{
		if (_selectedLibraryIndex < 0 || _selectedLibraryIndex >= _library.Count)
		{
			return;
		}
		try
		{
			if (!path.EndsWith(KeyboardThemeStore.PackageExtension, StringComparison.OrdinalIgnoreCase))
			{
				path += KeyboardThemeStore.PackageExtension;
			}
			KeyboardThemeStore.ExportTheme(path, _library[_selectedLibraryIndex], _layout);
			ShowNotice("主题已导出");
		}
		catch (Exception exception)
		{
			GD.PushWarning($"主题导出失败：{exception.Message}");
			ShowNotice(FriendlyFileError(exception, "主题导出失败，请检查保存位置。"));
		}
	}

	private void ConfigureFileDialogs()
	{
		_exportSpecificationDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		_exportSpecificationDialog.Access = FileDialog.AccessEnum.Filesystem;
		_exportSpecificationDialog.Filters = new[] { "*.png ; PNG 文字贴图模板" };
		_exportSpecificationDialog.CurrentFile = "keyboard-text-safe-area.png";

		_importDesignDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_importDesignDialog.Access = FileDialog.AccessEnum.Filesystem;
		_importDesignDialog.Filters = new[] { "*.png ; PNG 文字贴图" };

		_importThemeDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_importThemeDialog.Access = FileDialog.AccessEnum.Filesystem;
		_importThemeDialog.Filters = new[]
		{
			"*.jellytheme ; 果冻键显主题包",
			"*.json ; 旧版主题 JSON",
		};

		_exportThemeDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		_exportThemeDialog.Access = FileDialog.AccessEnum.Filesystem;
		_exportThemeDialog.Filters = new[] { "*.jellytheme ; 果冻键显主题包" };
		_exportThemeDialog.CurrentFile = "jelly-keyboard-theme.jellytheme";
	}

	private void InstallNotice()
	{
		_notice = new Panel
		{
			Name = "FileNotice",
			Position = new Vector2(240f, 24f),
			Size = new Vector2(460f, 44f),
			ZIndex = 200,
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_notice.AddThemeStyleboxOverride("panel", MakeStyle(UiDesignTokens.Ink, 12));
		_noticeLabel = new Label
		{
			Name = "Message",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_noticeLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_noticeLabel.AddThemeFontOverride("font", _uiFont);
		_noticeLabel.AddThemeFontSizeOverride("font_size", 15);
		_noticeLabel.AddThemeColorOverride("font_color", Colors.White);
		_notice.AddChild(_noticeLabel);
		AddChild(_notice);

		_noticeTimer = new Godot.Timer { OneShot = true, WaitTime = 3.5 };
		_noticeTimer.Timeout += () => _notice.Visible = false;
		AddChild(_noticeTimer);
	}

	private void ShowNotice(string message)
	{
		_noticeLabel.Text = message;
		var width = Mathf.Clamp(_uiFont.GetStringSize(message, HorizontalAlignment.Left, -1f, 15).X + 44f,
			220f, 720f);
		_notice.Position = new Vector2((WindowSize.X - width) * 0.5f, 24f);
		_notice.Size = new Vector2(width, 44f);
		_notice.Visible = true;
		_notice.MoveToFront();
		_noticeTimer.Start();
	}

	private static string FriendlyFileError(Exception exception, string fallback)
	{
		return exception is InvalidDataException &&
			(exception.Message.StartsWith("贴图", StringComparison.Ordinal) ||
			 exception.Message.StartsWith("主题", StringComparison.Ordinal))
			? exception.Message : fallback;
	}


}
