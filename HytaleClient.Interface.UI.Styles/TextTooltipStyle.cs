using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class TextTooltipStyle
{
	public enum TooltipAlignment
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public LabelStyle LabelStyle = new LabelStyle();

	public Padding Padding = new Padding
	{
		Full = 10
	};

	public PatchStyle Background = new PatchStyle(221u);

	public int? MaxWidth;

	public TooltipAlignment? Alignment;
}
