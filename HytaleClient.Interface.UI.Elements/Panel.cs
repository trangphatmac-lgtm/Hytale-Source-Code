using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class Panel : Group
{
	public Panel(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	public override Element HitTest(Point position)
	{
		if (_waitingForLayoutAfterMount || !_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}
}
