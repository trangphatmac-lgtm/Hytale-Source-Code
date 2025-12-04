namespace HytaleClient.InGame.Modules.Shortcuts;

internal class MacroShortcut : Shortcut
{
	private const char CommandSeperator = ';';

	private string[] _commands;

	public MacroShortcut(string name, string command)
		: base(name, command)
	{
		_commands = ParseCommands(command);
	}

	public string[] GetCommands()
	{
		return _commands;
	}

	private static string[] ParseCommands(string command)
	{
		return command.Split(new char[1] { ';' });
	}

	public override string ToString()
	{
		return base.Name + " - " + base.Command;
	}
}
