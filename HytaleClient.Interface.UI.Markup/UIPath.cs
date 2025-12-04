using System;

namespace HytaleClient.Interface.UI.Markup;

public class UIPath
{
	public readonly string Value;

	public UIPath(string value)
	{
		Value = value ?? throw new ArgumentNullException("value");
	}
}
