using System;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.AssetEditor.Interface.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class DynamicPaneContainer : Group
{
	public const int FlexMinSize = 50;

	public DynamicPaneContainer(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void AfterChildrenLayout()
	{
		if (_layoutMode == LayoutMode.Left || _layoutMode == LayoutMode.Right || _layoutMode == LayoutMode.Top || _layoutMode == LayoutMode.Bottom)
		{
			EnsureChildrenMinMaxSize();
		}
	}

	private void EnsureChildrenMinMaxSize()
	{
		bool flag = _layoutMode == LayoutMode.Top || _layoutMode == LayoutMode.Bottom;
		int num = (flag ? _anchoredRectangle.Height : _anchoredRectangle.Width);
		int num2 = 0;
		int num3 = 0;
		foreach (Element child in base.Children)
		{
			if (!child.IsMounted)
			{
				continue;
			}
			if (child is DynamicPane dynamicPane)
			{
				if (child.FlexWeight > 0)
				{
					num -= Desktop.ScaleRound(dynamicPane.MinSize);
					continue;
				}
				num2 = (flag ? child.AnchoredRectangle.Height : child.AnchoredRectangle.Width);
				num3++;
			}
			else if (flag && child.Anchor.Height.HasValue)
			{
				num -= Desktop.ScaleRound(child.Anchor.Height.Value);
			}
			else if (!flag && child.Anchor.Width.HasValue)
			{
				num -= Desktop.ScaleRound(child.Anchor.Width.Value);
			}
			else if (child.FlexWeight > 0)
			{
				num -= Desktop.ScaleRound(50f);
			}
		}
		if (num3 <= 0 || num2 <= num)
		{
			return;
		}
		int num4 = (int)System.Math.Round((float)(num2 - num) / (float)num3, MidpointRounding.ToEven);
		foreach (Element child2 in base.Children)
		{
			if (!(child2 is DynamicPane dynamicPane2) || child2.FlexWeight > 0 || !child2.IsMounted)
			{
				continue;
			}
			if (flag)
			{
				child2.Anchor.Height -= num4;
				if (child2.Anchor.Height < dynamicPane2.MinSize)
				{
					child2.Anchor.Height = dynamicPane2.MinSize;
				}
			}
			else
			{
				child2.Anchor.Width -= num4;
				if (child2.Anchor.Width < dynamicPane2.MinSize)
				{
					child2.Anchor.Width = dynamicPane2.MinSize;
				}
			}
		}
		LayoutChildren();
	}
}
