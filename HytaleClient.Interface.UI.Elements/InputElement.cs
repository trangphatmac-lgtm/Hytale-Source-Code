using System;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Elements;

public abstract class InputElement<T> : Element
{
	public Action ValueChanged;

	[UIMarkupProperty]
	public virtual T Value { get; set; }

	public InputElement(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}
}
