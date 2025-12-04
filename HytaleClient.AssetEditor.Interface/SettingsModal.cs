using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.Data.UserSettings;
using HytaleClient.Interface.Settings;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Utils;

namespace HytaleClient.AssetEditor.Interface;

internal class SettingsModal : BaseModal, ISettingView
{
	private enum WindowMode
	{
		Window,
		Fullscreen,
		WindowedFullscreen
	}

	private DropdownSettingComponent _languageSetting;

	private DropdownSettingComponent _fullscreenSetting;

	private CheckBoxSettingComponent _diagnosticsModeSetting;

	private FolderSelectorDropdownComponent _assetsPathSetting;

	public SettingsModal(AssetEditorInterface @interface)
		: base(@interface, "Settings/SettingsModal.ui")
	{
	}

	protected override void BuildModal(Document doc, UIFragment fragment)
	{
		BuildSettings();
		ApplySettings();
	}

	private void BuildSettings()
	{
		_content.Clear();
		AssetEditorApp app = _interface.App;
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>
		{
			new DropdownBox.DropdownEntryInfo(Desktop.Provider.GetText("ui.assetEditor.settings.setting.useSystemLanguage"), "")
		};
		foreach (KeyValuePair<string, string> availableLanguage in Language.GetAvailableLanguages())
		{
			list.Add(new DropdownBox.DropdownEntryInfo(availableLanguage.Value, availableLanguage.Key));
		}
		_languageSetting = AddDropdownSetting(_content, "ui.assetEditor.settings.setting.language", list, delegate(string value)
		{
			AssetEditorSettings assetEditorSettings4 = app.Settings.Clone();
			assetEditorSettings4.Language = ((value == "") ? null : value);
			app.ApplySettings(assetEditorSettings4);
		});
		_fullscreenSetting = AddDropdownSetting(_content, "ui.assetEditor.settings.setting.fullscreen", new List<KeyValuePair<string, WindowMode>>
		{
			new KeyValuePair<string, WindowMode>(Desktop.Provider.GetText("ui.assetEditor.settings.setting.fullscreen"), WindowMode.Fullscreen),
			new KeyValuePair<string, WindowMode>(Desktop.Provider.GetText("ui.assetEditor.settings.setting.borderlessFullscreen"), WindowMode.WindowedFullscreen),
			new KeyValuePair<string, WindowMode>(Desktop.Provider.GetText("ui.assetEditor.settings.setting.windowed"), WindowMode.Window)
		}, delegate(WindowMode value)
		{
			AssetEditorSettings assetEditorSettings3 = app.Settings.Clone();
			assetEditorSettings3.Fullscreen = value != WindowMode.Window;
			assetEditorSettings3.UseBorderlessForFullscreen = value == WindowMode.WindowedFullscreen;
			app.ApplySettings(assetEditorSettings3);
		});
		_diagnosticsModeSetting = AddCheckBoxSetting(_content, "ui.assetEditor.settings.setting.diagnosticMode", delegate(bool value)
		{
			AssetEditorSettings assetEditorSettings2 = app.Settings.Clone();
			assetEditorSettings2.DiagnosticMode = value;
			app.ApplySettings(assetEditorSettings2);
		});
		_assetsPathSetting = AddFolderSelectorDropdownSetting(_content, "ui.assetEditor.settings.setting.assetsPath", delegate(string value)
		{
			if (!(value == app.Settings.AssetsPath))
			{
				AssetEditorSettings assetEditorSettings = app.Settings.Clone();
				assetEditorSettings.AssetsPath = value;
				assetEditorSettings.DisplayDefaultAssetPathWarning = false;
				app.ApplySettings(assetEditorSettings);
			}
		});
	}

	private void ApplySettings()
	{
		AssetEditorSettings settings = _interface.App.Settings;
		_languageSetting.SetValue(settings.Language ?? "");
		_fullscreenSetting.SetValue(settings.Fullscreen ? ((!settings.UseBorderlessForFullscreen) ? WindowMode.Fullscreen : WindowMode.WindowedFullscreen).ToString() : WindowMode.Window.ToString());
		_diagnosticsModeSetting.SetValue(settings.DiagnosticMode);
		_assetsPathSetting.SetValue(settings.AssetsPath);
	}

	private CheckBoxSettingComponent AddCheckBoxSetting(Group container, string name, Action<bool> onChange)
	{
		return new CheckBoxSettingComponent(Desktop, container, name, this)
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

	private DropdownSettingComponent AddDropdownSetting(Group container, string name, List<DropdownBox.DropdownEntryInfo> values, Action<string> onChange)
	{
		return new DropdownSettingComponent(Desktop, container, name, this, values)
		{
			OnChange = onChange
		};
	}

	private FolderSelectorDropdownComponent AddFolderSelectorDropdownSetting(Group container, string name, Action<string> onChange)
	{
		return new FolderSelectorDropdownComponent(Desktop, container, name, this)
		{
			OnChange = onChange
		};
	}

	protected internal override void Validate()
	{
		Dismiss();
	}

	public void SetHoveredSetting<T>(string setting, SettingComponent<T> component)
	{
	}

	public bool TryGetDocument(string path, out Document document)
	{
		return Desktop.Provider.TryGetDocument("Settings/" + path, out document);
	}

	public void Open()
	{
		OpenInLayer();
	}
}
