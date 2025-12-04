using System.Collections.Generic;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

internal class TextTooltipLayer : BaseTooltipLayer
{
	[UIMarkupProperty]
	public TextTooltipStyle Style = new TextTooltipStyle();

	private readonly Group _wrapper;

	private readonly Group _group;

	private readonly Label _label;

	[UIMarkupProperty]
	public string Text
	{
		set
		{
			_label.Text = value;
		}
	}

	[UIMarkupProperty]
	public IList<Label.LabelSpan> TextSpans
	{
		set
		{
			_label.TextSpans = value;
		}
	}

	public TextTooltipLayer(Desktop desktop)
		: base(desktop)
	{
		_layoutMode = LayoutMode.Top;
		_wrapper = new Group(Desktop, this)
		{
			LayoutMode = LayoutMode.Left,
			Anchor = 
			{
				Left = 0
			}
		};
		_group = new Group(Desktop, _wrapper);
		_label = new Label(Desktop, _group);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_label.Style = Style.LabelStyle;
		_group.Background = Style.Background;
		_group.Padding = Style.Padding;
		_wrapper.Anchor.Width = Style.MaxWidth;
	}

	protected override void UpdatePosition()
	{
		Anchor.Left = Desktop.UnscaleRound(Desktop.MousePosition.X);
		Anchor.Top = Desktop.UnscaleRound(Desktop.MousePosition.Y) + 30;
		Point point = _group.ComputeScaledMinSize(Style.MaxWidth.HasValue ? new int?(Desktop.ScaleRound(Style.MaxWidth.Value)) : null, null);
		if (Desktop.MousePosition.X + point.X > Desktop.RootLayoutRectangle.Width || Style.Alignment.GetValueOrDefault() == TextTooltipStyle.TooltipAlignment.BottomLeft || Style.Alignment == TextTooltipStyle.TooltipAlignment.TopLeft)
		{
			Anchor.Left -= Desktop.UnscaleRound(point.X);
		}
		if (Desktop.MousePosition.Y + point.Y + Desktop.ScaleRound(30f) > Desktop.RootLayoutRectangle.Height || Style.Alignment == TextTooltipStyle.TooltipAlignment.TopLeft || Style.Alignment.GetValueOrDefault() == TextTooltipStyle.TooltipAlignment.TopRight)
		{
			Anchor.Top -= Desktop.UnscaleRound(point.Y) + 60;
		}
	}
}
