using System;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class ReorderableList : Element
{
	public Action<int, int> ElementReordered;

	private int _dropTargetIndex = -1;

	[UIMarkupProperty]
	public Anchor DropIndicatorAnchor;

	[UIMarkupProperty]
	public PatchStyle DropIndicatorBackground;

	private TexturePatch _dropIndicatorTexturePatch;

	private Rectangle _dropIndicatorRectangle;

	[UIMarkupProperty]
	public ScrollbarStyle ScrollbarStyle
	{
		set
		{
			_scrollbarStyle = value;
		}
	}

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

	public ReorderableList(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_layoutMode = LayoutMode.Top;
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_dropIndicatorTexturePatch = ((DropIndicatorBackground != null) ? Desktop.MakeTexturePatch(DropIndicatorBackground) : null);
	}

	protected override void LayoutSelf()
	{
		if (_dropTargetIndex == -1)
		{
			return;
		}
		if (_layoutMode == LayoutMode.Top || _layoutMode == LayoutMode.Bottom || _layoutMode == LayoutMode.Middle || _layoutMode == LayoutMode.MiddleCenter || _layoutMode == LayoutMode.TopScrolling || _layoutMode == LayoutMode.BottomScrolling)
		{
			int num;
			Rectangle anchoredRectangle;
			if (_dropTargetIndex == base.Children.Count)
			{
				Element element = base.Children[base.Children.Count - 1];
				num = element.AnchoredRectangle.Bottom;
				anchoredRectangle = element.AnchoredRectangle;
			}
			else
			{
				Element element2 = base.Children[_dropTargetIndex];
				num = element2.AnchoredRectangle.Top;
				anchoredRectangle = element2.AnchoredRectangle;
			}
			if (_dropTargetIndex > 0 && _dropTargetIndex < base.Children.Count)
			{
				Element element3 = base.Children[_dropTargetIndex - 1];
				float num2 = (float)(anchoredRectangle.Top - element3.AnchoredRectangle.Bottom) / 2f;
				num -= (int)num2;
			}
			if (_dropTargetIndex == base.Children.Count)
			{
				num -= Desktop.ScaleRound(DropIndicatorAnchor.Height.GetValueOrDefault());
			}
			else if (_dropTargetIndex != 0)
			{
				num -= Desktop.ScaleRound((float)DropIndicatorAnchor.Height.GetValueOrDefault() / 2f);
			}
			float num3 = (float)anchoredRectangle.Left + (float)DropIndicatorAnchor.Left.GetValueOrDefault() * Desktop.Scale;
			float num4 = (float)anchoredRectangle.Right - (float)DropIndicatorAnchor.Right.GetValueOrDefault() * Desktop.Scale;
			if (DropIndicatorAnchor.Width.HasValue)
			{
				float num5 = (float)DropIndicatorAnchor.Width.Value * Desktop.Scale;
				if (DropIndicatorAnchor.Left.HasValue)
				{
					if (!DropIndicatorAnchor.Right.HasValue)
					{
						num4 = num3 + num5;
					}
				}
				else if (DropIndicatorAnchor.Right.HasValue)
				{
					num3 = num4 - num5;
				}
				else
				{
					num3 = (float)anchoredRectangle.Center.X - num5 / 2f;
					num4 = num3 + num5;
				}
			}
			_dropIndicatorRectangle = new Rectangle(MathHelper.Round(num3), num, MathHelper.Round(num4 - num3), Desktop.ScaleRound(DropIndicatorAnchor.Height.GetValueOrDefault()));
			return;
		}
		int num6;
		Rectangle anchoredRectangle2;
		if (_dropTargetIndex == base.Children.Count)
		{
			Element element4 = base.Children[base.Children.Count - 1];
			num6 = element4.AnchoredRectangle.Right;
			anchoredRectangle2 = element4.AnchoredRectangle;
		}
		else
		{
			Element element5 = base.Children[_dropTargetIndex];
			num6 = element5.AnchoredRectangle.Left;
			anchoredRectangle2 = element5.AnchoredRectangle;
		}
		if (_dropTargetIndex > 0 && _dropTargetIndex < base.Children.Count)
		{
			Element element6 = base.Children[_dropTargetIndex - 1];
			float num7 = (float)(anchoredRectangle2.Left - element6.AnchoredRectangle.Right) / 2f;
			num6 -= (int)num7;
		}
		if (_dropTargetIndex == base.Children.Count)
		{
			num6 -= Desktop.ScaleRound(DropIndicatorAnchor.Width.GetValueOrDefault());
		}
		else if (_dropTargetIndex != 0)
		{
			num6 -= Desktop.ScaleRound((float)DropIndicatorAnchor.Width.GetValueOrDefault() / 2f);
		}
		float num8 = (float)anchoredRectangle2.Top + (float)DropIndicatorAnchor.Top.GetValueOrDefault() * Desktop.Scale;
		float num9 = (float)anchoredRectangle2.Bottom - (float)DropIndicatorAnchor.Bottom.GetValueOrDefault() * Desktop.Scale;
		if (DropIndicatorAnchor.Height.HasValue)
		{
			float num10 = (float)DropIndicatorAnchor.Height.Value * Desktop.Scale;
			if (DropIndicatorAnchor.Top.HasValue)
			{
				if (!DropIndicatorAnchor.Right.HasValue)
				{
					num9 = num8 + num10;
				}
			}
			else if (DropIndicatorAnchor.Bottom.HasValue)
			{
				num8 = num9 - num10;
			}
			else
			{
				num8 = (float)anchoredRectangle2.Center.X - num10 / 2f;
				num9 = num8 + num10;
			}
		}
		_dropIndicatorRectangle = new Rectangle(num6, MathHelper.Round(num8), Desktop.ScaleRound(DropIndicatorAnchor.Width.GetValueOrDefault()), MathHelper.Round(num9 - num8));
	}

	public void SetDropTargetIndex(int index)
	{
		_dropTargetIndex = index;
		LayoutSelf();
	}

	protected override void PrepareForDrawContent()
	{
		base.PrepareForDrawContent();
		if (_dropTargetIndex != -1 && _dropIndicatorTexturePatch != null)
		{
			Desktop.Graphics.Batcher2D.RequestDrawPatch(_dropIndicatorTexturePatch, _dropIndicatorRectangle, Desktop.Scale);
		}
	}
}
