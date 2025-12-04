using System;

namespace HytaleClient.InGame.Commands;

public class DescriptionAttribute : Attribute
{
	public readonly string Description;

	public DescriptionAttribute(string description)
	{
		Description = description;
	}
}
