using System.Collections.Generic;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface;

public class MarkupErrorOverlay : Element
{
	private readonly string _title;

	public MarkupErrorOverlay(Desktop desktop, Element parent, string title)
		: base(desktop, parent)
	{
		_title = title;
		_layoutMode = LayoutMode.Top;
		Background = new PatchStyle(170u);
	}

	public void Setup(string message, TextParserSpan span)
	{
		Clear();
		LabelStyle labelStyle = new LabelStyle
		{
			FontSize = 20f,
			TextColor = UInt32Color.FromRGBA(3722305023u)
		};
		LabelStyle style = new LabelStyle
		{
			FontSize = 20f
		};
		LabelStyle labelStyle2 = new LabelStyle
		{
			FontSize = 20f,
			FontName = new UIFontName("Mono"),
			TextColor = UInt32Color.FromRGBA(2863311615u),
			Wrap = true,
			HorizontalAlignment = LabelStyle.LabelAlignment.End
		};
		LabelStyle style2 = new LabelStyle
		{
			FontSize = 20f,
			FontName = new UIFontName("Mono"),
			Wrap = true
		};
		span.GetContext(3, out var startLine, out var startColumn, out var before, out var inside, out var after);
		new Label(Desktop, this)
		{
			Anchor = new Anchor
			{
				Full = 10
			},
			Style = new LabelStyle
			{
				FontSize = 30f,
				RenderBold = true
			},
			Text = _title
		};
		new Label(Desktop, this)
		{
			Style = style,
			Anchor = new Anchor
			{
				Full = 10
			},
			TextSpans = new List<Label.LabelSpan>
			{
				new Label.LabelSpan
				{
					Text = $"{span.Parser.SourcePath} ({startLine + 1}:{startColumn + 1})",
					Color = UInt32Color.FromRGBA(3435973887u)
				},
				new Label.LabelSpan
				{
					Text = " — "
				},
				new Label.LabelSpan
				{
					Text = message,
					IsBold = true
				}
			}
		};
		Group parent = new Group(Desktop, this)
		{
			Anchor = new Anchor
			{
				Full = 10
			},
			Background = new PatchStyle(170u),
			LayoutMode = LayoutMode.Top
		};
		Label label = new Label(Desktop, parent)
		{
			Style = style2,
			Anchor = new Anchor
			{
				Full = 20
			}
		};
		if (inside.Length == 0)
		{
			if (after.Length > 0)
			{
				inside = after.Substring(0, 1);
				after = after.Substring(1);
			}
			else
			{
				inside = " ";
			}
		}
		label.TextSpans = new List<Label.LabelSpan>
		{
			new Label.LabelSpan
			{
				Text = before
			},
			new Label.LabelSpan
			{
				Text = inside,
				IsUnderlined = true,
				Color = UInt32Color.FromRGBA(4282664191u)
			},
			new Label.LabelSpan
			{
				Text = after
			}
		};
	}
}
