using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI;

[UIMarkupData]
public struct Padding
{
	public int? Left;

	public int? Right;

	public int? Top;

	public int? Bottom;

	public int? Full
	{
		set
		{
			Left = (Right = (Top = (Bottom = value)));
		}
	}

	public int? Horizontal
	{
		get
		{
			return Left + Right;
		}
		set
		{
			Left = (Right = value);
		}
	}

	public int? Vertical
	{
		get
		{
			return Top + Bottom;
		}
		set
		{
			Top = (Bottom = value);
		}
	}

	public Padding(int full)
	{
		Left = full;
		Right = full;
		Top = full;
		Bottom = full;
	}
}
