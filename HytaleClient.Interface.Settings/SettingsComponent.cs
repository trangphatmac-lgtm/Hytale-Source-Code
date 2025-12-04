using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hypixel.ProtoPlus;
using HytaleClient.Application;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Interface.Settings.Options;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.Interface.Settings;

internal class SettingsComponent : InterfaceComponent, ISettingView
{
	private enum SettingsTab
	{
		General,
		Visual,
		Audio,
		Mouse,
		Controls,
		Creative,
		Prototype
	}

	private enum WindowMode
	{
		Window,
		Fullscreen,
		WindowedFullscreen
	}

	private SettingsTab _activeTab = SettingsTab.General;

	private Group _settingInfo;

	private Label _settingInfoLabel;

	private Group _tabContainer;

	private TextButton.TextButtonStyle _buttonStyle;

	private TextButton.TextButtonStyle _buttonSelectedStyle;

	private Dictionary<SettingsTab, TextButton> _tabButtons = new Dictionary<SettingsTab, TextButton>();

	private Dictionary<SettingsTab, Group> _tabs = new Dictionary<SettingsTab, Group>();

	private DropdownSettingComponent _languageSetting;

	private DropdownSettingComponent _fullscreenSetting;

	private LabeledCheckBoxSettingComponent _diagnosticsModeSetting;

	private SliderSettingComponent _maxChatMessagesSetting;

	private LabeledCheckBoxSettingComponent _autoJumpObstacleSetting;

	private LabeledCheckBoxSettingComponent _autoJumpGap;

	private FloatSliderSettingComponent _jumpForceSpeedMultiplierStep;

	private FloatSliderSettingComponent _maxJumpForceSpeedMultiplier;

	private LabeledCheckBoxSettingComponent _builderModeSetting;

	private LabeledCheckBoxSettingComponent _blockHealthSetting;

	private SliderSettingComponent _creativeInteractionDistance;

	private SliderSettingComponent _adventureInteractionDistance;

	private LabeledCheckBoxSettingComponent _mantlingSetting;

	private FloatSliderSettingComponent _minVelocityMantlingSetting;

	private FloatSliderSettingComponent _maxVelocityMantlingSetting;

	private FloatSliderSettingComponent _mantlingCameraOffsetY;

	private FloatSliderSettingComponent _mantleBlockHeight;

	private LabeledCheckBoxSettingComponent _useBlockSubfaces;

	private LabeledCheckBoxSettingComponent _displayBlockSubfaces;

	private LabeledCheckBoxSettingComponent _displayBlockBoundaries;

	private LabeledCheckBoxSettingComponent _placeBlocksAtRange;

	private LabeledCheckBoxSettingComponent _placeBlocksAtRangeInAdventureMode;

	private LabeledCheckBoxSettingComponent _blockPlacementSupportValidation;

	private FloatSliderSettingComponent _resetMouseSensitivityDuration;

	private DropdownSettingComponent _resolutionSetting;

	private LabeledCheckBoxSettingComponent _vsyncSetting;

	private LabeledCheckBoxSettingComponent _unlimitedFpsSetting;

	private SliderSettingComponent _maxFpsSetting;

	private SliderSettingComponent _viewDistanceSetting;

	private SliderSettingComponent _fieldOfViewSetting;

	private LabeledCheckBoxSettingComponent _automaticRenderScaleSetting;

	private SliderSettingComponent _renderScaleSetting;

	private DropdownSettingComponent _placementPreviewSetting;

	private DropdownSettingComponent _outputDeviceSetting;

	private SliderSettingComponent _masterVolumeSetting;

	private SliderSettingComponent _musicVolumeSetting;

	private SliderSettingComponent _ambientVolumeSetting;

	private SliderSettingComponent _uiVolumeSetting;

	private SliderSettingComponent _sfxVolumeSetting;

	private LabeledCheckBoxSettingComponent _mouseRawInputModeSetting;

	private LabeledCheckBoxSettingComponent _mouseInvertedSetting;

	private FloatSliderSettingComponent _mouseXSpeedSetting;

	private FloatSliderSettingComponent _mouseYSpeedSetting;

	private LabeledCheckBoxSettingComponent _builderToolsUseToolReachDistanceSetting;

	private SliderSettingComponent _builderToolsToolReachDistanceSetting;

	private LabeledCheckBoxSettingComponent _builderToolsToolReachLockSetting;

	private SliderSettingComponent _builderToolsToolDelayMinSetting;

	private LabeledCheckBoxSettingComponent _builderToolsEnableBrushShapeRenderingSetting;

	private SliderSettingComponent _builderToolsBrushOpacitySetting;

	private SliderSettingComponent _builderToolsSelectionOpacitySetting;

	private LabeledCheckBoxSettingComponent _builderToolsDisplayLegendSetting;

	private SliderSettingComponent _percentageOfPlaySelectionLengthGizmoShouldRenderSetting;

	private SliderSettingComponent _minPlaySelectGizmoSizeSetting;

	private SliderSettingComponent _maxPlaySelectGizmoSizeSetting;

	private LabeledCheckBoxSettingComponent _sprintFovEffectSetting;

	private FloatSliderSettingComponent _sprintFovIntensitySetting;

	private LabeledCheckBoxSettingComponent _viewBobbingEffectSetting;

	private FloatSliderSettingComponent _viewBobbingIntensitySetting;

	private LabeledCheckBoxSettingComponent _flyCameraMode;

	private LabeledCheckBoxSettingComponent _cameraShakeEffectSetting;

	private FloatSliderSettingComponent _firstPersonCameraShakeIntensitySetting;

	private FloatSliderSettingComponent _thirdPersonCameraShakeIntensitySetting;

	private SliderSettingComponent _builderToolsPaintOperationsIgnoreHistoryLengthSetting;

	private SliderSettingComponent _builderToolsSpacingBlocksOffset;

	private LabeledCheckBoxSettingComponent _builderToolEnableBrushSpacing;

	private LabeledCheckBoxSettingComponent _sprintForceSetting;

	private DropdownSettingComponent _sprintAccelerationEasingTypeSetting;

	private FloatSliderSettingComponent _sprintAccelerationDurationSetting;

	private DropdownSettingComponent _sprintDecelerationEasingTypeSetting;

	private FloatSliderSettingComponent _sprintDecelerationDurationSetting;

	private DropdownSettingComponent _slideDecelerationEasingTypeSetting;

	private FloatSliderSettingComponent _slideDecelerationDurationSetting;

	private SliderSettingComponent _staminaLowAlertPercentSetting;

	private LabeledCheckBoxSettingComponent _staminaDebugInfo;

	private LabeledCheckBoxSettingComponent _weaponPullbackSetting;

	private LabeledCheckBoxSettingComponent _itemAnimationClippingSetting;

	private LabeledCheckBoxSettingComponent _itemClippingSetting;

	private LabeledCheckBoxSettingComponent _useOverrideFirstPersonAnimations;

	private SliderSettingComponent _entityUIMaxEntities;

	private FloatSliderSettingComponent _entityUIMaxDistanceSetting;

	private FloatSliderSettingComponent _entityUIHideDelaySetting;

	private FloatSliderSettingComponent _entityUIFadeInDurationSetting;

	private FloatSliderSettingComponent _entityUIFadeOutDurationSetting;

	private LabeledCheckBoxSettingComponent _showBuilderToolsNotificationsSetting;

	private LabeledCheckBoxSettingComponent _showDebugMarkersSetting;

	private FloatSliderSettingComponent _mountMinTurnRate;

	private FloatSliderSettingComponent _mountMaxTurnRate;

	private FloatSliderSettingComponent _mountSpeedMinTurnRate;

	private FloatSliderSettingComponent _mountSpeedMaxTurnRate;

	private DropdownSettingComponent _mountForwardsAccelerationEasingTypeSetting;

	private DropdownSettingComponent _mountForwardsDecelerationEasingTypeSetting;

	private DropdownSettingComponent _mountBackwardsAccelerationEasingTypeSetting;

	private DropdownSettingComponent _mountBackwardsDecelerationEasingTypeSetting;

	private FloatSliderSettingComponent _mountForwardsAccelerationDurationSetting;

	private FloatSliderSettingComponent _mountForwardsDecelerationDurationSetting;

	private FloatSliderSettingComponent _mountBackwardsAccelerationDurationSetting;

	private FloatSliderSettingComponent _mountBackwardsDecelerationDurationSetting;

	private FloatSliderSettingComponent _mountForcedAccelerationMultiplierSetting;

	private FloatSliderSettingComponent _mountForcedDecelerationMultiplierSetting;

	private LabeledCheckBoxSettingComponent _mountRequireNewInputSetting;

	private Dictionary<string, InputBindingSettingComponent> _inputBindingSettings = new Dictionary<string, InputBindingSettingComponent>();

	private InputBindingPopup _inputBindingPopup;

	private string _editedInputBindingName;

	private float _audioDeviceListRefreshCooldown;

	private List<DropdownBox.DropdownEntryInfo> _easingTypes;

	public SettingsComponent(Interface @interface, Group parent)
		: base(@interface, parent)
	{
		_easingTypes = new List<DropdownBox.DropdownEntryInfo>();
		string[] names = Enum.GetNames(typeof(Easing.EasingType));
		foreach (string text in names)
		{
			_easingTypes.Add(new DropdownBox.DropdownEntryInfo(text, text));
		}
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("Common/Settings/Settings.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Group navigationGroup = uIFragment.Get<Group>("Navigation");
		_buttonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "NavigationButtonStyle");
		_buttonSelectedStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "NavigationButtonSelectedStyle");
		_settingInfo = uIFragment.Get<Group>("SettingInfo");
		_settingInfoLabel = uIFragment.Get<Label>("SettingInfoLabel");
		_tabButtons.Clear();
		AddTabButton(SettingsTab.General, "ui.settings.tabs.general");
		AddTabButton(SettingsTab.Visual, "ui.settings.tabs.visual");
		AddTabButton(SettingsTab.Audio, "ui.settings.tabs.audio");
		AddTabButton(SettingsTab.Mouse, "ui.settings.tabs.mouse");
		AddTabButton(SettingsTab.Controls, "ui.settings.tabs.controls");
		AddTabButton(SettingsTab.Creative, "ui.settings.tabs.creative");
		AddTabButton(SettingsTab.Prototype, "ui.settings.tabs.prototype");
		_tabs.Clear();
		Group container = (_tabContainer = uIFragment.Get<Group>("TabContainer"));
		App app = Interface.App;
		Dictionary<SettingsTab, Group> tabs = _tabs;
		Group obj = new Group(Desktop, container)
		{
			LayoutMode = LayoutMode.Top
		};
		Group group = obj;
		tabs[SettingsTab.General] = obj;
		Group container2 = group;
		AddSectionHeader(container2, "ui.settings.groups.general");
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>
		{
			new DropdownBox.DropdownEntryInfo(Desktop.Provider.GetText("ui.settings.useSystemLanguage"), "")
		};
		foreach (KeyValuePair<string, string> availableLanguage in Language.GetAvailableLanguages())
		{
			list.Add(new DropdownBox.DropdownEntryInfo(availableLanguage.Value, availableLanguage.Key));
		}
		_languageSetting = AddDropdownSetting(container2, "ui.settings.language", list, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings49 = app.Settings.Clone();
			settings49.Language = ((value == "") ? null : value);
			app.ApplyNewSettings(settings49);
		});
		_fullscreenSetting = AddDropdownSetting(container2, "ui.settings.fullscreen", new List<KeyValuePair<string, WindowMode>>
		{
			new KeyValuePair<string, WindowMode>(Desktop.Provider.GetText("ui.settings.fullscreen"), WindowMode.Fullscreen),
			new KeyValuePair<string, WindowMode>(Desktop.Provider.GetText("ui.settings.borderlessFullscreen"), WindowMode.WindowedFullscreen),
			new KeyValuePair<string, WindowMode>(Desktop.Provider.GetText("ui.settings.windowed"), WindowMode.Window)
		}, delegate(WindowMode value)
		{
			bool fullscreen = app.Settings.Fullscreen;
			HytaleClient.Data.UserSettings.Settings settings48 = app.Settings.Clone();
			settings48.Fullscreen = value != WindowMode.Window;
			settings48.UseBorderlessForFullscreen = value == WindowMode.WindowedFullscreen;
			if (fullscreen && !settings48.Fullscreen)
			{
				IReadOnlyList<DropdownBox.DropdownEntryInfo> entries = _resolutionSetting.Dropdown.Entries;
				DropdownBox.DropdownEntryInfo dropdownEntryInfo = entries[entries.Count - 3];
				_resolutionSetting.SetValue(dropdownEntryInfo.Value);
				settings48.ScreenResolution = ScreenResolution.FromString(dropdownEntryInfo.Value);
			}
			else if (!fullscreen && settings48.Fullscreen)
			{
				IReadOnlyList<DropdownBox.DropdownEntryInfo> entries2 = _resolutionSetting.Dropdown.Entries;
				DropdownBox.DropdownEntryInfo dropdownEntryInfo2 = entries2[entries2.Count - 2];
				_resolutionSetting.SetValue(dropdownEntryInfo2.Value);
				settings48.ScreenResolution = ScreenResolution.FromString(dropdownEntryInfo2.Value);
			}
			app.ApplyNewSettings(settings48);
		});
		AddSectionSpacer(container2);
		AddSectionHeader(container2, "ui.settings.groups.ui");
		_maxChatMessagesSetting = AddSliderSetting(container2, "ui.settings.maxChatMessages", 16, 256, 8, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings47 = app.Settings.Clone();
			settings47.MaxChatMessages = value;
			app.ApplyNewSettings(settings47);
		});
		AddSectionSpacer(container2);
		AddSectionHeader(container2, "ui.settings.groups.development");
		_diagnosticsModeSetting = AddCheckBoxSetting(container2, "ui.settings.diagnosticMode", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings46 = app.Settings.Clone();
			settings46.DiagnosticMode = value;
			app.ApplyNewSettings(settings46);
			_inputBindingSettings["OpenDevTools"].Visible = Interface.App.Settings.DiagnosticMode;
		});
		group = (_tabs[SettingsTab.Visual] = new Group(Desktop, container)
		{
			LayoutMode = LayoutMode.Top
		});
		Group container3 = group;
		AddSectionHeader(container3, "ui.settings.groups.rendering");
		List<KeyValuePair<string, string>> availableResolutionOptions = ScreenResolutions.GetAvailableResolutionOptions(app);
		availableResolutionOptions.Add(new KeyValuePair<string, string>(Desktop.Provider.GetText("ui.settings.customResolution"), ScreenResolutions.CustomScreenResolution.ToString()));
		_resolutionSetting = AddDropdownSetting(container3, "ui.settings.resolution", availableResolutionOptions, delegate(string value)
		{
			ScreenResolution screenResolution = ScreenResolution.FromString(value);
			HytaleClient.Data.UserSettings.Settings settings45 = app.Settings.Clone();
			settings45.ScreenResolution = screenResolution;
			settings45.Fullscreen = screenResolution.Fullscreen;
			app.ApplyNewSettings(settings45);
			_fullscreenSetting.SetValue(screenResolution.Fullscreen ? ((!settings45.UseBorderlessForFullscreen) ? WindowMode.Fullscreen : WindowMode.WindowedFullscreen).ToString() : WindowMode.Window.ToString());
			app.ApplyNewSettings(settings45);
		});
		_vsyncSetting = AddCheckBoxSetting(container3, "ui.settings.vsync", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings44 = app.Settings.Clone();
			settings44.VSync = value;
			app.ApplyNewSettings(settings44);
		});
		_unlimitedFpsSetting = AddCheckBoxSetting(container3, "ui.settings.unlimitedFps", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings43 = app.Settings.Clone();
			settings43.UnlimitedFps = value;
			app.ApplyNewSettings(settings43);
			_maxFpsSetting.Visible = !value;
			container.Layout();
		});
		_maxFpsSetting = AddSliderSetting(container3, "ui.settings.fpsLimit", 20, 240, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings42 = app.Settings.Clone();
			settings42.FpsLimit = value;
			app.ApplyNewSettings(settings42);
		});
		_viewDistanceSetting = AddSliderSetting(container3, "ui.settings.viewDistance", 32, 1024, 32, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings41 = app.Settings.Clone();
			settings41.ViewDistance = value;
			app.ApplyNewSettings(settings41);
		});
		_fieldOfViewSetting = AddSliderSetting(container3, "ui.settings.fieldOfView", 30, 120, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings40 = app.Settings.Clone();
			settings40.FieldOfView = value;
			app.ApplyNewSettings(settings40);
		});
		_automaticRenderScaleSetting = AddCheckBoxSetting(container3, "ui.settings.automaticRenderScale", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings39 = app.Settings.Clone();
			settings39.AutomaticRenderScale = value;
			app.ApplyNewSettings(settings39);
			_renderScaleSetting.Visible = !value;
			container.Layout();
		});
		_renderScaleSetting = AddSliderSetting(container3, "ui.settings.renderScale", 50, 200, 5, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings38 = app.Settings.Clone();
			settings38.RenderScale = value;
			app.ApplyNewSettings(settings38);
		});
		_placementPreviewSetting = AddDropdownSetting(container3, "ui.settings.placementPreview", new List<KeyValuePair<string, BlockPlacementPreview.DisplayMode>>
		{
			new KeyValuePair<string, BlockPlacementPreview.DisplayMode>(Desktop.Provider.GetText("ui.settings.placementPreview.none"), BlockPlacementPreview.DisplayMode.None),
			new KeyValuePair<string, BlockPlacementPreview.DisplayMode>(Desktop.Provider.GetText("ui.settings.placementPreview.all"), BlockPlacementPreview.DisplayMode.All),
			new KeyValuePair<string, BlockPlacementPreview.DisplayMode>(Desktop.Provider.GetText("ui.settings.placementPreview.multipart"), BlockPlacementPreview.DisplayMode.Multipart)
		}, delegate(BlockPlacementPreview.DisplayMode value)
		{
			HytaleClient.Data.UserSettings.Settings settings37 = app.Settings.Clone();
			settings37.PlacementPreviewMode = value;
			app.ApplyNewSettings(settings37);
		});
		group = (_tabs[SettingsTab.Mouse] = new Group(Desktop, container)
		{
			LayoutMode = LayoutMode.Top
		});
		Group container4 = group;
		AddSectionHeader(container4, "ui.settings.groups.mouse");
		_mouseRawInputModeSetting = AddCheckBoxSetting(container4, "ui.settings.mouseRawInputMode", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings36 = app.Settings.Clone();
			settings36.MouseSettings.MouseRawInputMode = value;
			app.ApplyNewSettings(settings36);
		});
		_mouseInvertedSetting = AddCheckBoxSetting(container4, "ui.settings.mouseInverted", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings35 = app.Settings.Clone();
			settings35.MouseSettings.MouseInverted = value;
			app.ApplyNewSettings(settings35);
		});
		_mouseXSpeedSetting = AddFloatSliderSetting(container4, "ui.settings.mouseXSpeed", 0f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings34 = app.Settings.Clone();
			settings34.MouseSettings.MouseXSpeed = value;
			app.ApplyNewSettings(settings34);
		});
		_mouseYSpeedSetting = AddFloatSliderSetting(container4, "ui.settings.mouseYSpeed", 0f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings33 = app.Settings.Clone();
			settings33.MouseSettings.MouseYSpeed = value;
			app.ApplyNewSettings(settings33);
		});
		group = (_tabs[SettingsTab.Audio] = new Group(Desktop, container)
		{
			LayoutMode = LayoutMode.Top
		});
		Group container5 = group;
		AddSectionHeader(container5, "ui.settings.groups.audioOutput");
		_outputDeviceSetting = AddDropdownSetting(container5, "ui.settings.outputDevice", new List<DropdownBox.DropdownEntryInfo>(), delegate(string value)
		{
			if (!uint.TryParse(value, out var result))
			{
				result = 0u;
			}
			HytaleClient.Data.UserSettings.Settings settings32 = app.Settings.Clone();
			settings32.AudioSettings.OutputDeviceId = result;
			app.ApplyNewSettings(settings32);
		});
		UpdateAudioOutputDeviceList();
		_masterVolumeSetting = AddSliderSetting(container5, "ui.settings.masterVolume", 0, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings31 = app.Settings.Clone();
			settings31.AudioSettings.MasterVolume = value;
			app.ApplyNewSettings(settings31);
		});
		_musicVolumeSetting = AddSliderSetting(container5, "ui.settings.musicVolume", 0, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings30 = app.Settings.Clone();
			settings30.AudioSettings.CategoryVolumes["MusicVolume"] = value;
			app.ApplyNewSettings(settings30);
		});
		_ambientVolumeSetting = AddSliderSetting(container5, "ui.settings.ambientVolume", 0, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings29 = app.Settings.Clone();
			settings29.AudioSettings.CategoryVolumes["AmbienceVolume"] = value;
			app.ApplyNewSettings(settings29);
		});
		_uiVolumeSetting = AddSliderSetting(container5, "ui.settings.uiVolume", 0, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings28 = app.Settings.Clone();
			settings28.AudioSettings.CategoryVolumes["UIVolume"] = value;
			app.ApplyNewSettings(settings28);
		});
		_sfxVolumeSetting = AddSliderSetting(container5, "ui.settings.sfxVolume", 0, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings27 = app.Settings.Clone();
			settings27.AudioSettings.CategoryVolumes["SFXVolume"] = value;
			app.ApplyNewSettings(settings27);
		});
		group = (_tabs[SettingsTab.Controls] = new Group(Desktop, container)
		{
			LayoutMode = LayoutMode.Top
		});
		Group group6 = group;
		AddSectionHeader(group6, "ui.settings.groups.controls");
		_inputBindingSettings.Clear();
		FieldInfo[] fields = typeof(InputBindings).GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType == typeof(InputBinding))
			{
				_inputBindingSettings[fieldInfo.Name] = AddInputBinding(group6, fieldInfo.Name);
			}
		}
		group6.Children[group6.Children.Count - 1].Children[0].Anchor.Bottom = 0;
		group = (_tabs[SettingsTab.Creative] = new Group(Desktop, container)
		{
			LayoutMode = LayoutMode.Top
		});
		Group container6 = group;
		AddSectionHeader(container6, "ui.settings.groups.builderTools");
		_showBuilderToolsNotificationsSetting = AddCheckBoxSetting(container6, "ui.settings.showToolNotifications", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings26 = app.Settings.Clone();
			settings26.BuilderToolsSettings.ShowBuilderToolsNotifications = value;
			app.ApplyNewSettings(settings26);
		});
		_builderToolEnableBrushSpacing = AddCheckBoxSetting(container6, "ui.settings.enableBrushSpacing", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings25 = app.Settings.Clone();
			settings25.EnableBrushSpacing = value;
			app.ApplyNewSettings(settings25);
		});
		_builderToolsSpacingBlocksOffset = AddSliderSetting(container6, "ui.settings.brushSpacingBlocks", 0, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings24 = app.Settings.Clone();
			settings24.BrushSpacingBlocks = value;
			app.ApplyNewSettings(settings24);
		});
		_builderToolsUseToolReachDistanceSetting = AddCheckBoxSetting(container6, "ui.settings.useToolReachDistance", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings23 = app.Settings.Clone();
			settings23.BuilderToolsSettings.useToolReachDistance = value;
			app.ApplyNewSettings(settings23);
			GatherSettings();
		});
		_builderToolsToolReachDistanceSetting = AddSliderSetting(container6, "ui.settings.toolReachDistance", 1, 256, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings22 = app.Settings.Clone();
			settings22.BuilderToolsSettings.ToolReachDistance = value;
			app.ApplyNewSettings(settings22);
		});
		_builderToolsToolReachLockSetting = AddCheckBoxSetting(container6, "ui.settings.toolReachLock", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings21 = app.Settings.Clone();
			settings21.BuilderToolsSettings.ToolReachLock = value;
			app.ApplyNewSettings(settings21);
		});
		_builderToolsToolDelayMinSetting = AddSliderSetting(container6, "ui.settings.toolDelayMin", 1, 1000, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings20 = app.Settings.Clone();
			settings20.BuilderToolsSettings.ToolDelayMin = value;
			app.ApplyNewSettings(settings20);
		});
		_builderToolsBrushOpacitySetting = AddSliderSetting(container6, "ui.settings.brushOpacity", 1, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings19 = app.Settings.Clone();
			settings19.BuilderToolsSettings.BrushOpacity = value;
			app.ApplyNewSettings(settings19);
		});
		_builderToolsSelectionOpacitySetting = AddSliderSetting(container6, "ui.settings.selectionOpacity", 1, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings18 = app.Settings.Clone();
			settings18.BuilderToolsSettings.SelectionOpacity = value;
			app.ApplyNewSettings(settings18);
		});
		_builderToolsDisplayLegendSetting = AddCheckBoxSetting(container6, "ui.settings.displayLegend", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings17 = app.Settings.Clone();
			settings17.BuilderToolsSettings.DisplayLegend = value;
			app.ApplyNewSettings(settings17);
		});
		group = (_tabs[SettingsTab.Prototype] = new Group(Desktop, container)
		{
			LayoutMode = LayoutMode.Top
		});
		Group group9 = group;
		AddSectionHeader(group9, "ui.settings.groups.general");
		_builderModeSetting = AddCheckBoxSetting(group9, "ui.settings.builderMode", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings16 = app.Settings.Clone();
			settings16.BuilderMode = value;
			app.ApplyNewSettings(settings16);
		});
		_builderModeSetting.TooltipText = Desktop.Provider.GetText("ui.settings.builderMode.tooltip");
		_blockHealthSetting = AddCheckBoxSetting(group9, "ui.settings.blockHealth", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings15 = app.Settings.Clone();
			settings15.BlockHealth = value;
			app.ApplyNewSettings(settings15);
		});
		_blockHealthSetting.TooltipText = Desktop.Provider.GetText("ui.settings.blockHealth.tooltip");
		_creativeInteractionDistance = AddSliderSetting(group9, "ui.settings.creativeInteractionDistance", 1, 128, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings14 = app.Settings.Clone();
			settings14.creativeInteractionDistance = value;
			app.ApplyNewSettings(settings14);
		});
		_adventureInteractionDistance = AddSliderSetting(group9, "ui.settings.adventureInteractionDistance", 1, 128, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings13 = app.Settings.Clone();
			settings13.adventureInteractionDistance = value;
			app.ApplyNewSettings(settings13);
		});
		_builderToolsEnableBrushShapeRenderingSetting = AddCheckBoxSetting(group9, "ui.settings.enableBrushShapeRendering", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings12 = app.Settings.Clone();
			settings12.BuilderToolsSettings.EnableBrushShapeRendering = value;
			app.ApplyNewSettings(settings12);
		});
		_placeBlocksAtRange = AddCheckBoxSetting(group9, "ui.settings.placeBlocksAtRange", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings11 = app.Settings.Clone();
			settings11._placeBlocksAtRange = value;
			if (!value)
			{
				settings11.PlaceBlocksAtRangeInAdventureMode = false;
			}
			app.ApplyNewSettings(settings11);
			GatherSettings();
		});
		_placeBlocksAtRangeInAdventureMode = AddCheckBoxSetting(group9, "ui.settings.placeBlocksAtRangeInAdventureMode", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings10 = app.Settings.Clone();
			settings10.PlaceBlocksAtRangeInAdventureMode = value;
			app.ApplyNewSettings(settings10);
		});
		_useBlockSubfaces = AddCheckBoxSetting(group9, "ui.settings.useBlockSubfaces", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings9 = app.Settings.Clone();
			settings9.UseBlockSubfaces = value;
			settings9.DisplayBlockSubfaces = value;
			app.ApplyNewSettings(settings9);
			GatherSettings();
		});
		_displayBlockSubfaces = AddCheckBoxSetting(group9, "ui.settings.displayBlockSubfaces", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings8 = app.Settings.Clone();
			settings8.DisplayBlockSubfaces = value;
			app.ApplyNewSettings(settings8);
		});
		_displayBlockBoundaries = AddCheckBoxSetting(group9, "ui.settings.displayBlockBoundaries", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings7 = app.Settings.Clone();
			settings7.DisplayBlockBoundaries = value;
			app.ApplyNewSettings(settings7);
		});
		_blockPlacementSupportValidation = AddCheckBoxSetting(group9, "ui.settings.blockPlacementSupportValidation", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings6 = app.Settings.Clone();
			settings6.BlockPlacementSupportValidation = value;
			app.ApplyNewSettings(settings6);
		});
		_resetMouseSensitivityDuration = AddFloatSliderSetting(group9, "ui.settings.resetMouseSensitivityDuration", 0f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings5 = app.Settings.Clone();
			settings5.ResetMouseSensitivityDuration = value;
			app.ApplyNewSettings(settings5);
		});
		AddSectionSpacer(group9);
		AddSectionHeader(group9, "ui.settings.groups.playselect");
		_percentageOfPlaySelectionLengthGizmoShouldRenderSetting = AddSliderSetting(group9, "ui.settings.percentagePlayGizmoLength", 1, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
			settings4.PercentageOfPlaySelectionLengthGizmoShouldRender = (float)value / 100f;
			app.ApplyNewSettings(settings4);
		});
		_minPlaySelectGizmoSizeSetting = AddSliderSetting(group9, "ui.settings.minPlaySelectGizmoSize", 1, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
			settings3.MinPlaySelectGizmoSize = MathHelper.Clamp(value, 1, settings3.MaxPlaySelectGizmoSize - 1);
			app.ApplyNewSettings(settings3);
		});
		_maxPlaySelectGizmoSizeSetting = AddSliderSetting(group9, "ui.settings.maxPlaySelectGizmoSize", 2, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.MaxPlaySelectGizmoSize = MathHelper.Clamp(value, settings2.MinPlaySelectGizmoSize + 1, value);
			app.ApplyNewSettings(settings2);
		});
		AddSectionSpacer(group9);
		AddSectionHeader(group9, "ui.settings.groups.playpaint");
		_builderToolsPaintOperationsIgnoreHistoryLengthSetting = AddSliderSetting(group9, "ui.settings.paintOperationIgnoreHistoryLength", 0, 200, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.PaintOperationsIgnoreHistoryLength = value;
			app.ApplyNewSettings(settings);
		});
		AddMovementSettings(group9, app);
		AddMantlingSettings(group9, app);
		AddSprintForceSettings(group9, app);
		AddSlideForceSettings(group9, app);
		AddCameraSettings(group9, app);
		AddStaminaSettings(group9, app);
		AddWeaponPullbackSettings(group9, app);
		AddEntityUISettings(group9, app);
		AddDebugSettings(group9, app);
		AddMountSettings(group9, app);
		_inputBindingPopup = new InputBindingPopup(this);
		uIFragment.Get<TextButton>("ResetSettings").Activating = delegate
		{
			HytaleClient.Data.UserSettings.Settings newSettings = HytaleClient.Data.UserSettings.Settings.MakeDefaults();
			app.ApplyNewSettings(newSettings);
			GatherSettings();
		};
		uIFragment.Get<TextButton>("SaveChanges").Activating = delegate
		{
			SaveChanges();
			Dismiss();
		};
		UpdateGroup();
		if (base.IsMounted)
		{
			GatherSettings();
		}
		void AddTabButton(SettingsTab tab, string key)
		{
			_tabButtons[tab] = new TextButton(Desktop, navigationGroup)
			{
				Text = Desktop.Provider.GetText(key),
				Style = ((_activeTab == tab) ? _buttonSelectedStyle : _buttonStyle),
				Anchor = new Anchor
				{
					Right = 45
				},
				Activating = delegate
				{
					SelectTab(tab);
				}
			};
		}
	}

	private void SaveChanges()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		Interface.App.Settings.Save();
		(Interface.InGameView.InGame?.Instance?.Connection)?.SendPacket((ProtoPacket)new SyncPlayerPreferences(Interface.App.Settings.DebugSettings.ShowDebugMarkers));
	}

	private void AddDebugSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.debug");
		_showDebugMarkersSetting = AddCheckBoxSetting(group, "ui.settings.showDebugMarkers", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.DebugSettings.ShowDebugMarkers = value;
			app.ApplyNewSettings(settings);
		});
	}

	private void AddMovementSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.movement");
		_autoJumpObstacleSetting = AddCheckBoxSetting(group, "ui.settings.autoJumpObstacle", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
			settings4.AutoJumpObstacle = value;
			app.ApplyNewSettings(settings4);
		});
		_autoJumpGap = AddCheckBoxSetting(group, "ui.settings.autoJumpGap", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
			settings3.AutoJumpGap = value;
			app.ApplyNewSettings(settings3);
		});
		_maxJumpForceSpeedMultiplier = AddFloatSliderSetting(group, "ui.settings.maxJumpForceSpeedMultiplier", 0f, 100f, 1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.MaxJumpForceSpeedMultiplier = value;
			app.ApplyNewSettings(settings2);
		});
		_jumpForceSpeedMultiplierStep = AddFloatSliderSetting(group, "ui.settings.jumpForceSpeedMultiplierStep", 0f, 10f, 1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.JumpForceSpeedMultiplierStep = value;
			app.ApplyNewSettings(settings);
		});
	}

	private void AddMantlingSettings(Group group, App app)
	{
		if (app.InGame.Instance == null || app.InGame.Instance.ClientFeatureModule.IsFeatureEnabled((ClientFeature)2))
		{
			AddSectionSpacer(group);
			AddSectionHeader(group, "ui.settings.groups.mantling");
			_mantlingSetting = AddCheckBoxSetting(group, "ui.settings.mantling", delegate(bool value)
			{
				HytaleClient.Data.UserSettings.Settings settings5 = app.Settings.Clone();
				settings5.Mantling = value;
				app.ApplyNewSettings(settings5);
			});
			_minVelocityMantlingSetting = AddFloatSliderSetting(group, "ui.settings.minVelocityMantling", -200f, 0f, 0.1f, delegate(float value)
			{
				HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
				settings4.MinVelocityMantling = value;
				app.ApplyNewSettings(settings4);
			});
			_maxVelocityMantlingSetting = AddFloatSliderSetting(group, "ui.settings.maxVelocityMantling", 0f, 200f, 0.1f, delegate(float value)
			{
				HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
				settings3.MaxVelocityMantling = value;
				app.ApplyNewSettings(settings3);
			});
			_mantlingCameraOffsetY = AddFloatSliderSetting(group, "ui.settings.mantlingCameraOffsetY", -5f, 0f, 0.1f, delegate(float value)
			{
				HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
				settings2.MantlingCameraOffsetY = value;
				app.ApplyNewSettings(settings2);
			});
			_mantleBlockHeight = AddFloatSliderSetting(group, "ui.settings.mantleBlockHeight", 0f, 1f, 0.01f, delegate(float value)
			{
				HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
				settings.MantleBlockHeight = value;
				app.ApplyNewSettings(settings);
			});
		}
	}

	private void AddCameraSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.camera");
		_flyCameraMode = AddCheckBoxSetting(group, "ui.settings.flyCameraMode", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings8 = app.Settings.Clone();
			settings8.UseNewFlyCamera = value;
			app.ApplyNewSettings(settings8);
		});
		_sprintFovEffectSetting = AddCheckBoxSetting(group, "ui.settings.sprintFovEffect", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings7 = app.Settings.Clone();
			settings7.SprintFovEffect = value;
			app.ApplyNewSettings(settings7);
		});
		_sprintFovIntensitySetting = AddFloatSliderSetting(group, "ui.settings.sprintFovIntensity", 1f, 1.5f, 0.01f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings6 = app.Settings.Clone();
			settings6.SprintFovIntensity = value;
			app.ApplyNewSettings(settings6);
		});
		AddSubSectionSpacer(group);
		_viewBobbingEffectSetting = AddCheckBoxSetting(group, "ui.settings.viewBobbingEffect", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings5 = app.Settings.Clone();
			settings5.ViewBobbingEffect = value;
			app.ApplyNewSettings(settings5);
		});
		_viewBobbingIntensitySetting = AddFloatSliderSetting(group, "ui.settings.viewBobbingIntensity", 0f, 1.5f, 0.01f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
			settings4.ViewBobbingIntensity = value;
			app.ApplyNewSettings(settings4);
		});
		AddSubSectionSpacer(group);
		_cameraShakeEffectSetting = AddCheckBoxSetting(group, "ui.settings.cameraShakeEffect", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
			settings3.CameraShakeEffect = value;
			app.ApplyNewSettings(settings3);
		});
		_firstPersonCameraShakeIntensitySetting = AddFloatSliderSetting(group, "ui.settings.firstPersonCameraShakeIntensity", 0f, 2f, 0.01f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.FirstPersonCameraShakeIntensity = value;
			app.ApplyNewSettings(settings2);
		});
		_thirdPersonCameraShakeIntensitySetting = AddFloatSliderSetting(group, "ui.settings.thirdPersonCameraShakeIntensity", 0f, 2f, 0.01f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.ThirdPersonCameraShakeIntensity = value;
			app.ApplyNewSettings(settings);
		});
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		string[] names = Enum.GetNames(typeof(Easing.EasingType));
		foreach (string text in names)
		{
			list.Add(new DropdownBox.DropdownEntryInfo(text, text));
		}
	}

	private void AddSprintForceSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.sprint");
		_sprintForceSetting = AddCheckBoxSetting(group, "ui.settings.sprintForce", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings5 = app.Settings.Clone();
			settings5.SprintForce = value;
			app.ApplyNewSettings(settings5);
		});
		_sprintAccelerationDurationSetting = AddFloatSliderSetting(group, "ui.settings.sprintAccelerationDuration", 0f, 2f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
			settings4.SprintAccelerationDuration = value;
			app.ApplyNewSettings(settings4);
		});
		_sprintDecelerationDurationSetting = AddFloatSliderSetting(group, "ui.settings.sprintDecelerationDuration", 0f, 2f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
			settings3.SprintDecelerationDuration = value;
			app.ApplyNewSettings(settings3);
		});
		_sprintAccelerationEasingTypeSetting = AddDropdownSetting(group, "ui.settings.sprintAccelerationEasingType", _easingTypes, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.SprintAccelerationEasingType = (Enum.TryParse<Easing.EasingType>(value, out var result2) ? result2 : Easing.EasingType.Linear);
			app.ApplyNewSettings(settings2);
		});
		_sprintDecelerationEasingTypeSetting = AddDropdownSetting(group, "ui.settings.sprintDecelerationEasingType", _easingTypes, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.SprintDecelerationEasingType = (Enum.TryParse<Easing.EasingType>(value, out var result) ? result : Easing.EasingType.Linear);
			app.ApplyNewSettings(settings);
		});
	}

	private void AddSlideForceSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.slide");
		_slideDecelerationDurationSetting = AddFloatSliderSetting(group, "ui.settings.slideDecelerationDuration", 0f, 5f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.SlideDecelerationDuration = value;
			app.ApplyNewSettings(settings2);
		});
		_slideDecelerationEasingTypeSetting = AddDropdownSetting(group, "ui.settings.slideDecelerationEasingType", _easingTypes, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.SlideDecelerationEasingType = (Enum.TryParse<Easing.EasingType>(value, out var result) ? result : Easing.EasingType.Linear);
			app.ApplyNewSettings(settings);
		});
	}

	private void AddStaminaSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.stamina");
		_staminaLowAlertPercentSetting = AddSliderSetting(group, "ui.settings.staminaLowAlertPercent", 0, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.StaminaLowAlertPercent = value;
			app.ApplyNewSettings(settings2);
		});
		_staminaDebugInfo = AddCheckBoxSetting(group, "ui.settings.staminaDebugInfo", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.StaminaDebugInfo = value;
			app.ApplyNewSettings(settings);
		});
	}

	private void AddWeaponPullbackSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.weaponPullback");
		_weaponPullbackSetting = AddCheckBoxSetting(group, "ui.settings.weaponPullback", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
			settings4.WeaponPullback = value;
			app.ApplyNewSettings(settings4);
		});
		_itemAnimationClippingSetting = AddCheckBoxSetting(group, "ui.settings.itemAnimationsClipGeometry", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
			settings3.ItemAnimationsClipGeometry = value;
			app.ApplyNewSettings(settings3);
		});
		_itemClippingSetting = AddCheckBoxSetting(group, "ui.settings.itemsClipGeometry", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.ItemsClipGeometry = value;
			app.ApplyNewSettings(settings2);
		});
		_useOverrideFirstPersonAnimations = AddCheckBoxSetting(group, "ui.settings.overrideFirstPersonAnimations", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.UseOverrideFirstPersonAnimations = value;
			app.ApplyNewSettings(settings);
		});
	}

	private void AddEntityUISettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.entityui");
		_entityUIMaxEntities = AddSliderSetting(group, "ui.settings.entityUIMaxEntities", 1, 100, 1, delegate(int value)
		{
			HytaleClient.Data.UserSettings.Settings settings5 = app.Settings.Clone();
			settings5.EntityUIMaxEntities = value;
			app.ApplyNewSettings(settings5);
		});
		_entityUIMaxDistanceSetting = AddFloatSliderSetting(group, "ui.settings.entityUIMaxDistance", 1f, 64f, 1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
			settings4.EntityUIMaxDistance = value;
			app.ApplyNewSettings(settings4);
		});
		_entityUIHideDelaySetting = AddFloatSliderSetting(group, "ui.settings.entityUIHideDelay", 0f, 6f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
			settings3.EntityUIHideDelay = value;
			app.ApplyNewSettings(settings3);
		});
		_entityUIFadeInDurationSetting = AddFloatSliderSetting(group, "ui.settings.entityUIFadeInDuration", 0f, 4f, 0.01f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.EntityUIFadeInDuration = value;
			app.ApplyNewSettings(settings2);
		});
		_entityUIFadeOutDurationSetting = AddFloatSliderSetting(group, "ui.settings.entityUIFadeOutDuration", 0f, 4f, 0.01f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.EntityUIFadeOutDuration = value;
			app.ApplyNewSettings(settings);
		});
	}

	private void AddMountSettings(Group group, App app)
	{
		AddSectionSpacer(group);
		AddSectionHeader(group, "ui.settings.groups.mounts");
		_mountMinTurnRate = AddFloatSliderSetting(group, "ui.settings.mountMinTurnRate", 0f, 360f, 1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings15 = app.Settings.Clone();
			settings15.MountMinTurnRate = value;
			app.ApplyNewSettings(settings15);
		});
		_mountMinTurnRate.TooltipText = Desktop.Provider.GetText("ui.settings.mountMinTurnRate.tooltip");
		_mountMaxTurnRate = AddFloatSliderSetting(group, "ui.settings.mountMaxTurnRate", 0f, 360f, 1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings14 = app.Settings.Clone();
			settings14.MountMaxTurnRate = value;
			app.ApplyNewSettings(settings14);
		});
		_mountMaxTurnRate.TooltipText = Desktop.Provider.GetText("ui.settings.mountMaxTurnRate.tooltip");
		_mountSpeedMinTurnRate = AddFloatSliderSetting(group, "ui.settings.mountSpeedMinTurnRate", 0f, 20f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings13 = app.Settings.Clone();
			settings13.MountSpeedMinTurnRate = value;
			app.ApplyNewSettings(settings13);
		});
		_mountSpeedMinTurnRate.TooltipText = Desktop.Provider.GetText("ui.settings.mountSpeedMinTurnRate.tooltip");
		_mountSpeedMaxTurnRate = AddFloatSliderSetting(group, "ui.settings.mountSpeedMaxTurnRate", 0f, 20f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings12 = app.Settings.Clone();
			settings12.MountSpeedMaxTurnRate = value;
			app.ApplyNewSettings(settings12);
		});
		_mountSpeedMaxTurnRate.TooltipText = Desktop.Provider.GetText("ui.settings.mountSpeedMaxTurnRate.tooltip");
		_mountForwardsAccelerationEasingTypeSetting = AddDropdownSetting(group, "ui.settings.mountForwardsAccelerationEasingType", _easingTypes, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings11 = app.Settings.Clone();
			settings11.MountForwardsAccelerationEasingType = (Enum.TryParse<Easing.EasingType>(value, out var result4) ? result4 : Easing.EasingType.Linear);
			app.ApplyNewSettings(settings11);
		});
		_mountForwardsAccelerationEasingTypeSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountForwardsAccelerationEasingType.tooltip");
		_mountForwardsDecelerationEasingTypeSetting = AddDropdownSetting(group, "ui.settings.mountForwardsDecelerationEasingType", _easingTypes, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings10 = app.Settings.Clone();
			settings10.MountForwardsDecelerationEasingType = (Enum.TryParse<Easing.EasingType>(value, out var result3) ? result3 : Easing.EasingType.Linear);
			app.ApplyNewSettings(settings10);
		});
		_mountForwardsDecelerationEasingTypeSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountForwardsDecelerationEasingType.tooltip");
		_mountBackwardsAccelerationEasingTypeSetting = AddDropdownSetting(group, "ui.settings.mountBackwardsAccelerationEasingType", _easingTypes, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings9 = app.Settings.Clone();
			settings9.MountBackwardsAccelerationEasingType = (Enum.TryParse<Easing.EasingType>(value, out var result2) ? result2 : Easing.EasingType.Linear);
			app.ApplyNewSettings(settings9);
		});
		_mountBackwardsAccelerationEasingTypeSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountBackwardsAccelerationEasingType.tooltip");
		_mountBackwardsDecelerationEasingTypeSetting = AddDropdownSetting(group, "ui.settings.mountBackwardsDecelerationEasingType", _easingTypes, delegate(string value)
		{
			HytaleClient.Data.UserSettings.Settings settings8 = app.Settings.Clone();
			settings8.MountBackwardsDecelerationEasingType = (Enum.TryParse<Easing.EasingType>(value, out var result) ? result : Easing.EasingType.Linear);
			app.ApplyNewSettings(settings8);
		});
		_mountBackwardsDecelerationEasingTypeSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountBackwardsDecelerationEasingType.tooltip");
		_mountForwardsAccelerationDurationSetting = AddFloatSliderSetting(group, "ui.settings.mountForwardsAccelerationDuration", 0f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings7 = app.Settings.Clone();
			settings7.MountForwardsAccelerationDuration = value;
			app.ApplyNewSettings(settings7);
		});
		_mountForwardsAccelerationDurationSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountForwardsAccelerationDuration.tooltip");
		_mountForwardsDecelerationDurationSetting = AddFloatSliderSetting(group, "ui.settings.mountForwardsDecelerationDuration", 0f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings6 = app.Settings.Clone();
			settings6.MountForwardsDecelerationDuration = value;
			app.ApplyNewSettings(settings6);
		});
		_mountForwardsDecelerationDurationSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountForwardsDecelerationDuration.tooltip");
		_mountBackwardsAccelerationDurationSetting = AddFloatSliderSetting(group, "ui.settings.mountBackwardsAccelerationDuration", 0f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings5 = app.Settings.Clone();
			settings5.MountBackwardsAccelerationDuration = value;
			app.ApplyNewSettings(settings5);
		});
		_mountBackwardsAccelerationDurationSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountBackwardsAccelerationDuration.tooltip");
		_mountBackwardsDecelerationDurationSetting = AddFloatSliderSetting(group, "ui.settings.mountBackwardsDecelerationDuration", 0f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings4 = app.Settings.Clone();
			settings4.MountBackwardsDecelerationDuration = value;
			app.ApplyNewSettings(settings4);
		});
		_mountBackwardsDecelerationDurationSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountBackwardsDecelerationDuration.tooltip");
		_mountForcedAccelerationMultiplierSetting = AddFloatSliderSetting(group, "ui.settings.mountForcedAccelerationMultiplierSetting", 1f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings3 = app.Settings.Clone();
			settings3.MountForcedAccelerationMultiplier = value;
			app.ApplyNewSettings(settings3);
		});
		_mountForcedAccelerationMultiplierSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountForcedAccelerationMultiplierSetting.tooltip");
		_mountForcedDecelerationMultiplierSetting = AddFloatSliderSetting(group, "ui.settings.mountForcedDecelerationMultiplierSetting", 1f, 10f, 0.1f, delegate(float value)
		{
			HytaleClient.Data.UserSettings.Settings settings2 = app.Settings.Clone();
			settings2.MountForcedDecelerationMultiplier = value;
			app.ApplyNewSettings(settings2);
		});
		_mountForcedDecelerationMultiplierSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountForcedDecelerationMultiplierSetting.tooltip");
		_mountRequireNewInputSetting = AddCheckBoxSetting(group, "ui.settings.mountRequireNewInput", delegate(bool value)
		{
			HytaleClient.Data.UserSettings.Settings settings = app.Settings.Clone();
			settings.MountRequireNewInput = value;
			app.ApplyNewSettings(settings);
		});
		_mountRequireNewInputSetting.TooltipText = Desktop.Provider.GetText("ui.settings.mountRequireNewInput.tooltip");
	}

	protected override void OnMounted()
	{
		GatherSettings();
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (_activeTab == SettingsTab.Audio)
		{
			_audioDeviceListRefreshCooldown += deltaTime;
			if (_audioDeviceListRefreshCooldown > 1f)
			{
				_audioDeviceListRefreshCooldown = 0f;
				UpdateAudioOutputDeviceList();
			}
		}
	}

	public void StopEditingInputBinding()
	{
		Desktop.SetTransientLayer(null);
		_editedInputBindingName = null;
	}

	public void OnInputBindingKeyPress(SDL_Keycode keycode)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (_editedInputBindingName != null)
		{
			_inputBindingSettings[_editedInputBindingName].SetValue(SDL.SDL_GetKeyName(keycode));
			_inputBindingSettings[_editedInputBindingName].Layout();
			HytaleClient.Data.UserSettings.Settings settings = Interface.App.Settings.Clone();
			InputBinding inputBinding = (InputBinding)typeof(InputBindings).GetField(_editedInputBindingName).GetValue(settings.InputBindings);
			inputBinding.Keycode = keycode;
			Interface.App.ApplyNewSettings(settings);
			Interface.TriggerEvent("settings.inputBindingsUpdated");
			StopEditingInputBinding();
		}
	}

	public void OnInputBindingMousePress(Input.MouseButton button)
	{
		if (_editedInputBindingName != null)
		{
			_inputBindingSettings[_editedInputBindingName].SetValue(InputBinding.GetMouseBoundInputLabel(button));
			_inputBindingSettings[_editedInputBindingName].Layout();
			HytaleClient.Data.UserSettings.Settings settings = Interface.App.Settings.Clone();
			InputBinding inputBinding = (InputBinding)typeof(InputBindings).GetField(_editedInputBindingName).GetValue(settings.InputBindings);
			inputBinding.MouseButton = button;
			Interface.App.ApplyNewSettings(settings);
			Interface.TriggerEvent("settings.inputBindingsUpdated");
			StopEditingInputBinding();
		}
	}

	private void SelectTab(SettingsTab tab)
	{
		_activeTab = tab;
		foreach (TextButton value in _tabButtons.Values)
		{
			value.Style = _buttonStyle;
		}
		_tabButtons[_activeTab].Style = _buttonSelectedStyle;
		UpdateGroup();
		Layout();
	}

	private void UpdateAudioOutputDeviceList()
	{
		List<DropdownBox.DropdownEntryInfo> audioDevices = new List<DropdownBox.DropdownEntryInfo>
		{
			new DropdownBox.DropdownEntryInfo(Desktop.Provider.GetText("ui.settings.defaultAudioDevice"), "0")
		};
		AudioDevice audio = Interface.Engine.Audio;
		AudioDevice.OutputDevice[] outputDevices = audio.GetOutputDevices();
		for (int i = 0; i < audio.OutputDeviceCount; i++)
		{
			ref AudioDevice.OutputDevice reference = ref outputDevices[i];
			audioDevices.Add(new DropdownBox.DropdownEntryInfo(reference.Name, reference.Id.ToString()));
		}
		bool flag = false;
		List<string> next = audioDevices.Select((DropdownBox.DropdownEntryInfo info) => info.Value).ToList();
		string value = Interface.App.Settings.AudioSettings.OutputDeviceId.ToString();
		if (!next.Contains(_outputDeviceSetting.Dropdown.Value))
		{
			value = 0u.ToString();
			flag = true;
		}
		if (flag || HasListChanged())
		{
			_outputDeviceSetting.SetEntries(audioDevices);
			_outputDeviceSetting.SetValue(value);
			_outputDeviceSetting.Layout();
		}
		bool HasListChanged()
		{
			IReadOnlyList<DropdownBox.DropdownEntryInfo> entries = _outputDeviceSetting.Dropdown.Entries;
			if (audioDevices.Count != entries.Count)
			{
				return true;
			}
			foreach (DropdownBox.DropdownEntryInfo item in entries)
			{
				if (!next.Contains(item.Value))
				{
					return true;
				}
			}
			return false;
		}
	}

	private void UpdateGroup()
	{
		foreach (Group value2 in _tabs.Values)
		{
			value2.Visible = false;
		}
		if (_tabs.TryGetValue(_activeTab, out var value))
		{
			value.Visible = true;
		}
		if (_activeTab == SettingsTab.Controls || _activeTab == SettingsTab.Prototype)
		{
			_tabContainer.LayoutMode = LayoutMode.TopScrolling;
		}
		else
		{
			_tabContainer.LayoutMode = LayoutMode.Top;
		}
		_tabContainer.SetScroll(0, 0);
		if (_tabContainer.IsMounted)
		{
			_tabContainer.Layout();
		}
	}

	private void AddSectionSpacer(Group container)
	{
		new Element(Desktop, container).Anchor = new Anchor
		{
			Height = 50
		};
	}

	private void AddSubSectionSpacer(Group container)
	{
		new Element(Desktop, container).Anchor = new Anchor
		{
			Height = 15
		};
	}

	private void AddSectionHeader(Group container, string name)
	{
		Interface.TryGetDocument("Common/Settings/SectionHeader.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, container);
		uIFragment.Get<Label>("Name").Text = Desktop.Provider.GetText(name);
	}

	private InputBindingSettingComponent AddInputBinding(Group container, string name)
	{
		return new InputBindingSettingComponent(Desktop, container, name, this)
		{
			OnChange = delegate
			{
				_editedInputBindingName = name;
				_inputBindingPopup.Setup(name);
				Desktop.SetTransientLayer(_inputBindingPopup);
			}
		};
	}

	private LabeledCheckBoxSettingComponent AddCheckBoxSetting(Group container, string name, Action<bool> onChange)
	{
		return new LabeledCheckBoxSettingComponent(Desktop, container, name, this)
		{
			OnChange = onChange
		};
	}

	private SliderSettingComponent AddSliderSetting(Group container, string name, int min, int max, int step, Action<int> onChange)
	{
		return new SliderSettingComponent(Desktop, container, name, this, min, max, step)
		{
			OnChange = onChange
		};
	}

	private FloatSliderSettingComponent AddFloatSliderSetting(Group container, string name, float min, float max, float step, Action<float> onChange)
	{
		return new FloatSliderSettingComponent(Desktop, container, name, this, min, max, step)
		{
			OnChange = onChange
		};
	}

	private DropdownSettingComponent AddDropdownSetting<T>(Group container, string name, List<KeyValuePair<string, T>> values, Action<T> onChange) where T : struct, IConvertible
	{
		return new DropdownSettingComponent(Desktop, container, name, this, values.Select((KeyValuePair<string, T> e) => new DropdownBox.DropdownEntryInfo(e.Key, e.Value.ToString())).ToList())
		{
			OnChange = delegate(string v)
			{
				onChange((T)Enum.Parse(typeof(T), v));
			}
		};
	}

	private DropdownSettingComponent AddDropdownSetting(Group container, string name, List<KeyValuePair<string, string>> values, Action<string> onChange)
	{
		return new DropdownSettingComponent(Desktop, container, name, this, values.Select((KeyValuePair<string, string> e) => new DropdownBox.DropdownEntryInfo(e.Key, e.Value.ToString())).ToList())
		{
			OnChange = onChange
		};
	}

	private DropdownSettingComponent AddDropdownSetting(Group container, string name, List<DropdownBox.DropdownEntryInfo> values, Action<string> onChange)
	{
		return new DropdownSettingComponent(Desktop, container, name, this, values)
		{
			OnChange = onChange
		};
	}

	public void SetHoveredSetting<T>(string setting, SettingComponent<T> component)
	{
		if (setting == null)
		{
			_settingInfo.Visible = false;
			return;
		}
		_settingInfoLabel.Text = setting;
		_settingInfo.Anchor.Left = Desktop.UnscaleRound(component.Children[0].ContainerRectangle.Right);
		_settingInfo.Anchor.Top = Desktop.UnscaleRound(component.Children[0].ContainerRectangle.Top);
		_settingInfo.Visible = true;
		_settingInfo.Layout(Desktop.RootLayoutRectangle);
	}

	public bool TryGetDocument(string path, out Document document)
	{
		return Desktop.Provider.TryGetDocument("Common/Settings/" + path, out document);
	}

	public void GatherSettings()
	{
		if (Interface.HasMarkupError)
		{
			return;
		}
		HytaleClient.Data.UserSettings.Settings settings = Interface.App.Settings;
		_fullscreenSetting.SetValue(settings.Fullscreen ? ((!settings.UseBorderlessForFullscreen) ? WindowMode.Fullscreen : WindowMode.WindowedFullscreen).ToString() : WindowMode.Window.ToString());
		_maxChatMessagesSetting.SetValue(settings.MaxChatMessages);
		_diagnosticsModeSetting.SetValue(settings.DiagnosticMode);
		_vsyncSetting.SetValue(settings.VSync);
		_unlimitedFpsSetting.SetValue(settings.UnlimitedFps);
		_maxFpsSetting.Visible = !settings.UnlimitedFps;
		_maxFpsSetting.SetValue(settings.FpsLimit);
		_viewDistanceSetting.SetValue(settings.ViewDistance);
		_fieldOfViewSetting.SetValue(settings.FieldOfView);
		_automaticRenderScaleSetting.SetValue(settings.AutomaticRenderScale);
		_renderScaleSetting.Visible = !settings.AutomaticRenderScale;
		_renderScaleSetting.SetValue(settings.RenderScale);
		_placementPreviewSetting.SetValue(settings.PlacementPreviewMode.ToString());
		_resolutionSetting.SetValue(settings.ScreenResolution.ToString());
		_outputDeviceSetting.SetValue(settings.AudioSettings.OutputDeviceId.ToString());
		_masterVolumeSetting.SetValue((int)settings.AudioSettings.MasterVolume);
		_musicVolumeSetting.SetValue((int)settings.AudioSettings.CategoryVolumes["MusicVolume"]);
		_ambientVolumeSetting.SetValue((int)settings.AudioSettings.CategoryVolumes["AmbienceVolume"]);
		_uiVolumeSetting.SetValue((int)settings.AudioSettings.CategoryVolumes["UIVolume"]);
		_sfxVolumeSetting.SetValue((int)settings.AudioSettings.CategoryVolumes["SFXVolume"]);
		_mouseRawInputModeSetting.SetValue(settings.MouseSettings.MouseRawInputMode);
		_mouseInvertedSetting.SetValue(settings.MouseSettings.MouseInverted);
		_mouseXSpeedSetting.SetValue(settings.MouseSettings.MouseXSpeed);
		_mouseYSpeedSetting.SetValue(settings.MouseSettings.MouseYSpeed);
		FieldInfo[] fields = typeof(InputBindings).GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType == typeof(InputBinding))
			{
				_inputBindingSettings[fieldInfo.Name].SetValue(((InputBinding)fieldInfo.GetValue(settings.InputBindings))?.BoundInputLabel);
			}
		}
		_builderToolsUseToolReachDistanceSetting.SetValue(settings.BuilderToolsSettings.useToolReachDistance);
		_builderToolsToolReachDistanceSetting.SetValue(settings.BuilderToolsSettings.ToolReachDistance);
		_builderToolsToolReachDistanceSetting.Visible = settings.BuilderToolsSettings.useToolReachDistance;
		_builderToolsToolReachLockSetting.SetValue(settings.BuilderToolsSettings.ToolReachLock);
		_builderToolsToolDelayMinSetting.SetValue(settings.BuilderToolsSettings.ToolDelayMin);
		_builderToolsEnableBrushShapeRenderingSetting.SetValue(settings.BuilderToolsSettings.EnableBrushShapeRendering);
		_builderToolsBrushOpacitySetting.SetValue(settings.BuilderToolsSettings.BrushOpacity);
		_builderToolsSelectionOpacitySetting.SetValue(settings.BuilderToolsSettings.SelectionOpacity);
		_builderToolsDisplayLegendSetting.SetValue(settings.BuilderToolsSettings.DisplayLegend);
		_showBuilderToolsNotificationsSetting.SetValue(settings.BuilderToolsSettings.ShowBuilderToolsNotifications);
		_showDebugMarkersSetting.SetValue(settings.DebugSettings.ShowDebugMarkers);
		_languageSetting.SetValue(settings.Language ?? "");
		_inputBindingSettings["OpenDevTools"].Visible = settings.DiagnosticMode;
		_autoJumpObstacleSetting.SetValue(settings.AutoJumpObstacle);
		_autoJumpGap.SetValue(settings.AutoJumpGap);
		_builderModeSetting.SetValue(settings.BuilderMode);
		_blockHealthSetting.SetValue(settings.BlockHealth);
		_creativeInteractionDistance.SetValue(settings.creativeInteractionDistance);
		_adventureInteractionDistance.SetValue(settings.adventureInteractionDistance);
		_mantlingSetting.SetValue(settings.Mantling);
		_minVelocityMantlingSetting.SetValue(settings.MinVelocityMantling);
		_maxVelocityMantlingSetting.SetValue(settings.MaxVelocityMantling);
		_jumpForceSpeedMultiplierStep.SetValue(settings.JumpForceSpeedMultiplierStep);
		_maxJumpForceSpeedMultiplier.SetValue(settings.MaxJumpForceSpeedMultiplier);
		_mantlingCameraOffsetY.SetValue(settings.MantlingCameraOffsetY);
		_mantleBlockHeight.SetValue(settings.MantleBlockHeight);
		_sprintFovEffectSetting.SetValue(settings.SprintFovEffect);
		_sprintFovIntensitySetting.SetValue(settings.SprintFovIntensity);
		_viewBobbingEffectSetting.SetValue(settings.ViewBobbingEffect);
		_viewBobbingIntensitySetting.SetValue(settings.ViewBobbingIntensity);
		_cameraShakeEffectSetting.SetValue(settings.CameraShakeEffect);
		_firstPersonCameraShakeIntensitySetting.SetValue(settings.FirstPersonCameraShakeIntensity);
		_thirdPersonCameraShakeIntensitySetting.SetValue(settings.ThirdPersonCameraShakeIntensity);
		_sprintForceSetting.SetValue(settings.SprintForce);
		_sprintAccelerationEasingTypeSetting.SetValue(settings.SprintAccelerationEasingType.ToString());
		_sprintAccelerationDurationSetting.SetValue(settings.SprintAccelerationDuration);
		_sprintDecelerationEasingTypeSetting.SetValue(settings.SprintDecelerationEasingType.ToString());
		_sprintDecelerationDurationSetting.SetValue(settings.SprintDecelerationDuration);
		_flyCameraMode.SetValue(settings.UseNewFlyCamera);
		_slideDecelerationDurationSetting.SetValue(settings.SlideDecelerationDuration);
		_slideDecelerationEasingTypeSetting.SetValue(settings.SlideDecelerationEasingType.ToString());
		_staminaLowAlertPercentSetting.SetValue(settings.StaminaLowAlertPercent);
		_staminaDebugInfo.SetValue(settings.StaminaDebugInfo);
		_weaponPullbackSetting.SetValue(settings.WeaponPullback);
		_itemAnimationClippingSetting.SetValue(settings.ItemAnimationsClipGeometry);
		_itemClippingSetting.SetValue(settings.ItemsClipGeometry);
		_useOverrideFirstPersonAnimations.SetValue(settings.UseOverrideFirstPersonAnimations);
		_entityUIMaxEntities.SetValue(settings.EntityUIMaxEntities);
		_entityUIMaxDistanceSetting.SetValue(settings.EntityUIMaxDistance);
		_entityUIHideDelaySetting.SetValue(settings.EntityUIHideDelay);
		_entityUIFadeInDurationSetting.SetValue(settings.EntityUIFadeInDuration);
		_entityUIFadeOutDurationSetting.SetValue(settings.EntityUIFadeOutDuration);
		_useBlockSubfaces.SetValue(settings.UseBlockSubfaces);
		_displayBlockSubfaces.SetValue(settings.DisplayBlockSubfaces);
		_displayBlockSubfaces.Visible = settings.UseBlockSubfaces;
		_displayBlockBoundaries.SetValue(settings.DisplayBlockBoundaries);
		_placeBlocksAtRange.SetValue(settings._placeBlocksAtRange);
		_placeBlocksAtRangeInAdventureMode.SetValue(settings.PlaceBlocksAtRangeInAdventureMode);
		_placeBlocksAtRangeInAdventureMode.Visible = settings._placeBlocksAtRange;
		_blockPlacementSupportValidation.SetValue(settings.BlockPlacementSupportValidation);
		_resetMouseSensitivityDuration.SetValue(settings.ResetMouseSensitivityDuration);
		_percentageOfPlaySelectionLengthGizmoShouldRenderSetting.SetValue((int)(settings.PercentageOfPlaySelectionLengthGizmoShouldRender * 100f));
		_minPlaySelectGizmoSizeSetting.SetValue(settings.MinPlaySelectGizmoSize);
		_maxPlaySelectGizmoSizeSetting.SetValue(settings.MaxPlaySelectGizmoSize);
		_builderToolsPaintOperationsIgnoreHistoryLengthSetting.SetValue(settings.PaintOperationsIgnoreHistoryLength);
		_builderToolsSpacingBlocksOffset.SetValue(settings.BrushSpacingBlocks);
		_builderToolEnableBrushSpacing.SetValue(settings.EnableBrushSpacing);
		_mountMinTurnRate.SetValue(settings.MountMinTurnRate);
		_mountMaxTurnRate.SetValue(settings.MountMaxTurnRate);
		_mountSpeedMinTurnRate.SetValue(settings.MountSpeedMinTurnRate);
		_mountSpeedMaxTurnRate.SetValue(settings.MountSpeedMaxTurnRate);
		_mountForwardsAccelerationEasingTypeSetting.SetValue(settings.MountForwardsAccelerationEasingType.ToString());
		_mountForwardsDecelerationEasingTypeSetting.SetValue(settings.MountForwardsDecelerationEasingType.ToString());
		_mountBackwardsAccelerationEasingTypeSetting.SetValue(settings.MountBackwardsAccelerationEasingType.ToString());
		_mountBackwardsDecelerationEasingTypeSetting.SetValue(settings.MountBackwardsDecelerationEasingType.ToString());
		_mountForwardsAccelerationDurationSetting.SetValue(settings.MountForwardsAccelerationDuration);
		_mountForwardsDecelerationDurationSetting.SetValue(settings.MountForwardsDecelerationDuration);
		_mountBackwardsAccelerationDurationSetting.SetValue(settings.MountBackwardsAccelerationDuration);
		_mountBackwardsDecelerationDurationSetting.SetValue(settings.MountBackwardsDecelerationDuration);
		_mountForcedAccelerationMultiplierSetting.SetValue(settings.MountForcedAccelerationMultiplier);
		_mountForcedDecelerationMultiplierSetting.SetValue(settings.MountForcedDecelerationMultiplier);
		_mountRequireNewInputSetting.SetValue(settings.MountRequireNewInput);
		Layout();
	}

	public void OnWindowSizeChanged()
	{
		if (_resolutionSetting != null)
		{
			List<ScreenResolution> availableResolutions = ScreenResolutions.GetAvailableResolutions(Interface.App);
			Point size = Interface.Engine.Window.GetSize();
			if (!availableResolutions.Contains(new ScreenResolution(size.X, size.Y, Interface.Engine.Window.GetState() == Window.WindowState.Fullscreen)))
			{
				_resolutionSetting.SetValue(ScreenResolutions.CustomScreenResolution.ToString());
			}
		}
	}
}
