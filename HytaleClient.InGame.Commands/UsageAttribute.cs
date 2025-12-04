using System;

namespace HytaleClient.InGame.Commands;

internal class UsageAttribute : Attribute
{
	public readonly string Command;

	public readonly string[] Usage;

	public UsageAttribute(string command, params string[] usage)
	{
		Command = command;
		Usage = usage;
	}

	public override string ToString()
	{
		string text = "Usage:";
		if (Usage.Length == 0)
		{
			text = text + "\n  ." + Command;
		}
		else
		{
			string[] usage = Usage;
			foreach (string text2 in usage)
			{
				text = text + "\n  ." + Command + " " + text2;
			}
		}
		return text;
	}
}
