using System;
using System.Collections.Generic;
using HytaleClient.Core;
using SDL2;

namespace HytaleClient.InGame.Modules.Shortcuts;

internal class KeybindShortcut : Shortcut
{
	private static readonly string[] _fixedNames = new string[4] { "PageUp", "PageDown", "ScrollLock", "CapsLock" };

	private static readonly Dictionary<string, string> _replaceKeys = new Dictionary<string, string>
	{
		{ "kp1", "keypad 1" },
		{ "kp2", "keypad 2" },
		{ "kp3", "keypad 3" },
		{ "kp4", "keypad 4" },
		{ "kp5", "keypad 5" },
		{ "kp6", "keypad 6" },
		{ "kp7", "keypad 7" },
		{ "kp8", "keypad 8" },
		{ "kp9", "keypad 9" },
		{ "kp0", "keypad 0" },
		{ "kp/", "keypad /" },
		{ "kp*", "keypad *" },
		{ "kp-", "keypad -" },
		{ "kpplus", "keypad +" },
		{ "kpenter", "keypad enter" },
		{ "kp.", "keypad ." }
	};

	private const char KeybindSeperator = '+';

	private SDL_Keycode[] _keycodes;

	private bool _shiftMod = false;

	private bool _ctrlMod = false;

	private bool _altMod = false;

	public KeybindShortcut(string keys, string command)
		: base(keys, command)
	{
		_keycodes = ParseKeybindings(keys);
		base.Name = GetKeyNames();
	}

	private SDL_Keycode[] ParseKeybindings(string keyStr)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Invalid comparison between Unknown and I4
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		keyStr = keyStr.Trim().ToLower();
		string[] array = keyStr.Split(new char[1] { '+' });
		List<SDL_Keycode> list = new List<SDL_Keycode>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!ParseModifierKey(text))
			{
				string text2 = (_replaceKeys.ContainsKey(text) ? _replaceKeys[text] : text);
				SDL_Keycode val = SDL.SDL_GetKeyFromName(text2);
				if ((int)val <= 0)
				{
					throw new Exception(text);
				}
				list.Add(val);
			}
		}
		return list.ToArray();
	}

	private bool ParseModifierKey(string str)
	{
		if (str.IndexOf("shift") > -1)
		{
			_shiftMod = true;
			return true;
		}
		if (str.IndexOf("ctrl") > -1)
		{
			_ctrlMod = true;
			return true;
		}
		if (str.IndexOf("alt") > -1)
		{
			_altMod = true;
			return true;
		}
		return false;
	}

	public bool IsActive(Input input)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (!CheckModifiers(input))
		{
			return false;
		}
		for (int i = 0; i < _keycodes.Length; i++)
		{
			if (!input.IsKeyHeld(SDL.SDL_GetScancodeFromKey(_keycodes[i])))
			{
				return false;
			}
		}
		return true;
	}

	private bool CheckModifiers(Input input)
	{
		if ((_shiftMod && !input.IsShiftHeld()) || (!_shiftMod && input.IsShiftHeld()))
		{
			return false;
		}
		if ((_ctrlMod && !input.IsCtrlHeld()) || (!_ctrlMod && input.IsCtrlHeld()))
		{
			return false;
		}
		if ((_altMod && !input.IsAltHeld()) || (!_altMod && input.IsAltHeld()))
		{
			return false;
		}
		return true;
	}

	public bool ConsumeKeybinds(Input input)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < _keycodes.Length; i++)
		{
			if (!input.ConsumeKey(SDL.SDL_GetScancodeFromKey(_keycodes[i])))
			{
				return false;
			}
		}
		return true;
	}

	private string GetKeyNames()
	{
		string text = "";
		for (int i = 0; i < _keycodes.Length; i++)
		{
			if (i > 0)
			{
				text += "+";
			}
			text += GetKeyName(_keycodes[i]);
		}
		string text2 = "";
		if (_shiftMod)
		{
			text2 = "Shift+";
		}
		if (_ctrlMod)
		{
			text2 += "Ctrl+";
		}
		if (_altMod)
		{
			text2 += "Alt+";
		}
		return text2 + text;
	}

	public static string FixKeyList(string keyStr)
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Invalid comparison between Unknown and I4
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		keyStr = keyStr.Trim().ToLower();
		string[] array = keyStr.Split(new char[1] { '+' });
		List<SDL_Keycode> list = new List<SDL_Keycode>();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.IndexOf("shift") > -1)
			{
				flag = true;
				continue;
			}
			if (text.IndexOf("ctrl") > -1)
			{
				flag2 = true;
				continue;
			}
			if (text.IndexOf("alt") > -1)
			{
				flag3 = true;
				continue;
			}
			string text2 = (_replaceKeys.ContainsKey(text) ? _replaceKeys[text] : text);
			SDL_Keycode val = SDL.SDL_GetKeyFromName(text2);
			if ((int)val > 0)
			{
				list.Add(val);
				continue;
			}
			throw new Exception(text);
		}
		SDL_Keycode[] array3 = list.ToArray();
		string text3 = "";
		for (int j = 0; j < array3.Length; j++)
		{
			if (j > 0)
			{
				text3 += "+";
			}
			text3 += GetKeyName(array3[j]);
		}
		string text4 = "";
		if (flag)
		{
			text4 = "Shift+";
		}
		if (flag2)
		{
			text4 += "Ctrl+";
		}
		if (flag3)
		{
			text4 += "Alt+";
		}
		return text4 + text3;
	}

	private static string GetKeyName(SDL_Keycode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		string text = SDL.SDL_GetKeyName(key);
		text = text.Substring(text.IndexOf("_") + 1).ToLower();
		for (int i = 0; i < _fixedNames.Length; i++)
		{
			if (text == _fixedNames[i].ToLower())
			{
				return _fixedNames[i];
			}
		}
		return (text.Length <= 1) ? text.ToUpper() : (char.ToUpper(text[0]) + text.Substring(1));
	}

	public override string ToString()
	{
		return GetKeyNames() + " - " + base.Command;
	}
}
