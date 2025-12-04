using System;

namespace HytaleClient.Interface.UI.Markup;

public class UIFontName
{
	public readonly string Value;

	public UIFontName(string value)
	{
		Value = value ?? throw new ArgumentNullException("value");
	}
}
