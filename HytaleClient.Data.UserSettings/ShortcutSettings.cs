using System.Collections.Generic;
using HytaleClient.InGame.Modules.Shortcuts;
using Newtonsoft.Json;

namespace HytaleClient.Data.UserSettings;

internal class ShortcutSettings
{
	[JsonProperty(PropertyName = "Macros")]
	public Dictionary<string, MacroShortcut> MacroShortcuts = new Dictionary<string, MacroShortcut>();

	[JsonProperty(PropertyName = "Keybinds")]
	public Dictionary<string, KeybindShortcut> KeybindShortcuts = new Dictionary<string, KeybindShortcut>();

	public ShortcutSettings Clone()
	{
		ShortcutSettings shortcutSettings = new ShortcutSettings();
		shortcutSettings.MacroShortcuts = new Dictionary<string, MacroShortcut>(MacroShortcuts);
		shortcutSettings.KeybindShortcuts = new Dictionary<string, KeybindShortcut>(KeybindShortcuts);
		return shortcutSettings;
	}
}
