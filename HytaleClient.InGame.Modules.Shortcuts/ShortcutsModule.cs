using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HytaleClient.Data.Items;
using HytaleClient.InGame.Commands;

namespace HytaleClient.InGame.Modules.Shortcuts;

internal class ShortcutsModule : Module
{
	private readonly Dictionary<string, MacroShortcut> _macros;

	private readonly Dictionary<string, KeybindShortcut> _keybinds;

	private List<string> _commandQueue = new List<string>();

	private const int MaxTickExecution = 50;

	private int _executionCount = 0;

	public ShortcutsModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_macros = _gameInstance.App.Settings.ShortcutSettings.MacroShortcuts;
		_keybinds = _gameInstance.App.Settings.ShortcutSettings.KeybindShortcuts;
		foreach (string key in _keybinds.Keys)
		{
			if (_keybinds[key] == null)
			{
				_keybinds.Remove(key);
			}
		}
		_gameInstance.RegisterCommand("macro", MacroCommand);
		_gameInstance.RegisterCommand("keybind", KeybindCommand);
	}

	public void Update()
	{
		foreach (KeyValuePair<string, KeybindShortcut> keybind in _keybinds)
		{
			if (keybind.Value.IsActive(_gameInstance.Input) && keybind.Value.ConsumeKeybinds(_gameInstance.Input))
			{
				ExecuteCommand(keybind.Value.Command);
			}
		}
		if (_commandQueue.Count > 0)
		{
			string command = _commandQueue[0];
			_commandQueue.RemoveAt(0);
			_gameInstance.Chat.SendCommand(command);
		}
		_executionCount = 0;
	}

	[Usage("macro", new string[] { "[name] OR ..[name]", "add [name] [command]", "remove [name]", "clear", "list" })]
	[Description("Manage macros")]
	public void MacroCommand(string[] args)
	{
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "add":
		{
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			string text = ParseCommandText(args, 2).Trim(new char[1] { ';' });
			if (text.Length == 0)
			{
				throw new InvalidCommandUsage();
			}
			string name = args[1];
			AddMacro(name, text);
			break;
		}
		case "remove":
		{
			if (args.Length != 2)
			{
				throw new InvalidCommandUsage();
			}
			string text2 = args[1];
			if (!_macros.ContainsKey(text2))
			{
				_gameInstance.Chat.Error("Unable to find macro '" + text2 + "'");
				break;
			}
			_macros.Remove(text2);
			_gameInstance.Chat.Log("Removed macro shortcut '" + text2 + "'");
			break;
		}
		case "clear":
			if (args.Length != 1)
			{
				throw new InvalidCommandUsage();
			}
			_macros.Clear();
			_gameInstance.Chat.Log("Cleared all macros.");
			break;
		case "list":
			if (args.Length != 1)
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.Chat.Log("Saved Macros:");
			if (_macros.Count > 0)
			{
				foreach (KeyValuePair<string, MacroShortcut> macro in _macros)
				{
					_gameInstance.Chat.Log(macro.Value.ToString());
				}
				break;
			}
			_gameInstance.Chat.Log("None");
			break;
		default:
			ExecuteMacro(args);
			break;
		}
	}

	[Usage("keybind", new string[] { "add [keys] [command]", "remove [keys]", "clear", "list" })]
	[Description("Modify keybinds")]
	public void KeybindCommand(string[] args)
	{
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "add":
		{
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			string text = ParseCommandText(args, 2);
			if (text.Length == 0)
			{
				throw new InvalidCommandUsage();
			}
			string text2 = string.Join("", Regex.Replace(args[1], "\\s+", ""));
			KeybindShortcut value;
			try
			{
				value = new KeybindShortcut(text2, text);
			}
			catch (Exception ex)
			{
				_gameInstance.Chat.Error("Unable to find matching key with name '" + ex.Message + "'");
				break;
			}
			if (_keybinds.ContainsKey(text2))
			{
				_gameInstance.Chat.Error("A keybind already exists for the keys [ " + text2 + " ]");
				break;
			}
			_keybinds.Add(text2, value);
			_gameInstance.Chat.Log("Keybind [ " + text2 + " ] set to command '" + text + "'");
			break;
		}
		case "remove":
		{
			if (args.Length != 2)
			{
				throw new InvalidCommandUsage();
			}
			string text3 = string.Join("", Regex.Replace(args[1], "\\s+", ""));
			string text4 = "";
			try
			{
				text4 = KeybindShortcut.FixKeyList(text3);
			}
			catch (Exception ex2)
			{
				_gameInstance.Chat.Error("Unable to find matching key with name '" + ex2.Message + "'");
				break;
			}
			if (!_keybinds.ContainsKey(text3))
			{
				_gameInstance.Chat.Error("Unable to find keybind with keys [ " + text4 + " ]");
				break;
			}
			_keybinds.Remove(text3);
			_gameInstance.Chat.Log("Removed keybind shortcut for keys [ " + text4 + " ]");
			break;
		}
		case "clear":
			if (args.Length != 1)
			{
				throw new InvalidCommandUsage();
			}
			_keybinds.Clear();
			_gameInstance.Chat.Log("Cleared all keybinds.");
			break;
		case "list":
			if (args.Length != 1)
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.Chat.Log("Saved Keybinds:");
			if (_keybinds.Count > 0)
			{
				foreach (KeyValuePair<string, KeybindShortcut> keybind in _keybinds)
				{
					_gameInstance.Chat.Log(keybind.Value.ToString());
				}
				break;
			}
			_gameInstance.Chat.Log("None");
			break;
		default:
			throw new InvalidCommandUsage();
		}
	}

	public void AddMacro(string name, string command)
	{
		if (_macros.ContainsKey(name))
		{
			_gameInstance.Chat.Error("A macro already exists with the name '" + name + "'");
			return;
		}
		_macros.Add(name, new MacroShortcut(name, command));
		_gameInstance.Chat.Log("Macro '" + name + "' successfully set to command '" + command + "'");
	}

	public void ExecuteMacro(string[] args)
	{
		if (!_macros.TryGetValue(args[0], out var value))
		{
			_gameInstance.Chat.Error("Unable to find macro with name: " + args[0]);
			return;
		}
		if (_executionCount >= 50)
		{
			_gameInstance.Chat.Error("Shortcut execution stopped after " + 50 + " cycles!");
			_gameInstance.Chat.Error("This may be because you have an infinite loop in the commands run. Please check shortcuts and try again.");
			return;
		}
		_executionCount++;
		string[] commands = value.GetCommands();
		string[] args2 = args.Skip(1).ToArray();
		for (int i = 0; i < commands.Length; i++)
		{
			ExecuteCommand(commands[i], args2);
		}
	}

	private void ExecuteCommand(string command, string[] args = null)
	{
		command = command.Trim();
		if (ReplaceCommandArgs(ref command, args))
		{
			if (command.StartsWith("."))
			{
				_gameInstance.ExecuteCommand(command);
			}
			else
			{
				_commandQueue.Add(command);
			}
		}
	}

	private bool ReplaceCommandArgs(ref string command, string[] args)
	{
		if (command.IndexOf("%x") > -1 || command.IndexOf("%y") > -1 || command.IndexOf("%z") > -1)
		{
			ReplacePositionArg(ref command, "%x", (int)_gameInstance.LocalPlayer.Position.X);
			ReplacePositionArg(ref command, "%y", (int)_gameInstance.LocalPlayer.Position.Y);
			ReplacePositionArg(ref command, "%z", (int)_gameInstance.LocalPlayer.Position.Z);
		}
		if (command.IndexOf("%chunkx") > -1 || command.IndexOf("%chunky") > -1 || command.IndexOf("%chunkz") > -1)
		{
			ReplacePositionArg(ref command, "%chunkx", (int)_gameInstance.LocalPlayer.Position.X >> 5);
			ReplacePositionArg(ref command, "%chunky", (int)_gameInstance.LocalPlayer.Position.Y >> 5);
			ReplacePositionArg(ref command, "%chunkz", (int)_gameInstance.LocalPlayer.Position.Z >> 5);
		}
		if (command.IndexOf("%hitx") > -1 || command.IndexOf("%hity") > -1 || command.IndexOf("%hitz") > -1 || command.IndexOf("%hitblock") > -1)
		{
			if (!_gameInstance.InteractionModule.HasFoundTargetBlock)
			{
				_gameInstance.Chat.Error("Unable to replace all parameters - no blocks within range.");
				return false;
			}
			HitDetection.RaycastHit targetBlockHit = _gameInstance.InteractionModule.TargetBlockHit;
			int num = (int)System.Math.Floor(targetBlockHit.BlockPosition.X);
			int num2 = (int)System.Math.Floor(targetBlockHit.BlockPosition.Y);
			int num3 = (int)System.Math.Floor(targetBlockHit.BlockPosition.Z);
			ReplacePositionArg(ref command, "%hitx", num);
			ReplacePositionArg(ref command, "%hity", num2);
			ReplacePositionArg(ref command, "%hitz", num3);
			int block = _gameInstance.MapModule.GetBlock(num, num2, num3, int.MaxValue);
			string name = _gameInstance.MapModule.ClientBlockTypes[block].Name;
			ReplaceParameterArg(ref command, "%hitblock", name);
		}
		if (command.IndexOf("%activeitem") > -1)
		{
			ClientItemStack activeItem = _gameInstance.InventoryModule.GetActiveItem();
			string val = ((activeItem == null) ? "Empty" : activeItem.Id.Split(new char[1] { '.' })[1]);
			ReplaceParameterArg(ref command, "%activeitem", val);
		}
		ReplaceParameterArg(ref command, "%name", _gameInstance.LocalPlayer.Name);
		if (args != null)
		{
			for (int i = 0; i < args.Length; i++)
			{
				ReplaceParameterArg(ref command, "%" + (i + 1), args[i]);
			}
		}
		return true;
	}

	private static void ReplacePositionArg(ref string command, string arg, int val)
	{
		int num = command.IndexOf(arg);
		if (num > -1)
		{
			int length = arg.Length;
			int num2 = command.IndexOf(' ', num);
			int num3 = ((num2 - num - length > 0 && num2 > -1) ? int.Parse(command.Substring(num + length, num2 - num - length), CultureInfo.InvariantCulture) : 0);
			command = command.Substring(0, num) + (num3 + val) + ((num2 > -1) ? command.Substring(num2) : "");
		}
	}

	private static void ReplaceParameterArg(ref string command, string arg, string val)
	{
		command = command.Replace(arg, val);
	}

	private static string ParseCommandText(string[] args, int startIndex)
	{
		return string.Join(" ", args.Skip(startIndex));
	}
}
