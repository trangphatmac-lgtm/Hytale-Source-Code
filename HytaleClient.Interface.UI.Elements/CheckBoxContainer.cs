#define DEBUG
using System.Diagnostics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class CheckBoxContainer : Group
{
	public CheckBoxContainer(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	private InputElement<bool> FindCheckBox()
	{
		return TraverseChildren(this);
		static InputElement<bool> TraverseChildren(Element element)
		{
			foreach (Element child in element.Children)
			{
				if (child is CheckBox || child is LabeledCheckBox)
				{
					return (InputElement<bool>)child;
				}
				InputElement<bool> inputElement = TraverseChildren(child);
				if (inputElement != null)
				{
					return inputElement;
				}
			}
			return null;
		}
	}

	private bool IsCheckBoxDisabled(Element element)
	{
		if (!(element is CheckBox { Disabled: var disabled }))
		{
			if (!(element is LabeledCheckBox { Disabled: var disabled2 }))
			{
				return true;
			}
			return disabled2;
		}
		return disabled;
	}

	protected override void OnMouseEnter()
	{
		InputElement<bool> inputElement = FindCheckBox();
		if (inputElement != null && !IsCheckBoxDisabled(inputElement))
		{
			SDL.SDL_SetCursor(Desktop.Cursors.Hand);
		}
	}

	protected override void OnMouseLeave()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		InputElement<bool> inputElement = FindCheckBox();
		if (inputElement != null && !IsCheckBoxDisabled(inputElement))
		{
			inputElement.Value = !inputElement.Value;
			inputElement.Layout();
		}
	}
}
