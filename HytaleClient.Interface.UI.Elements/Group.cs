using System;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class Group : Element
{
	public Action Validating;

	public Action Dismissing;

	[UIMarkupProperty]
	public new LayoutMode LayoutMode
	{
		get
		{
			return _layoutMode;
		}
		set
		{
			_layoutMode = value;
		}
	}

	[UIMarkupProperty]
	public ScrollbarStyle ScrollbarStyle
	{
		get
		{
			return _scrollbarStyle;
		}
		set
		{
			_scrollbarStyle = value;
		}
	}

	public Action Scrolled
	{
		set
		{
			_scrolled = value;
		}
	}

	public Group(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected internal override void Validate()
	{
		if (Validating != null)
		{
			Validating();
		}
		else
		{
			base.Validate();
		}
	}

	protected internal override void Dismiss()
	{
		if (Dismissing != null)
		{
			Dismissing();
		}
		else
		{
			base.Dismiss();
		}
	}
}
