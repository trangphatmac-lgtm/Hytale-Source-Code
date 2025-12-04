using System;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI;

[UIMarkupData]
public struct Anchor
{
	public int? Left;

	public int? Right;

	public int? Top;

	public int? Bottom;

	private int? _width;

	private int? _minWidth;

	private int? _maxWidth;

	public int? Height;

	public int? Full
	{
		set
		{
			Left = (Right = (Top = (Bottom = value)));
		}
	}

	public int? Horizontal
	{
		set
		{
			Left = (Right = value);
		}
	}

	public int? Vertical
	{
		set
		{
			Top = (Bottom = value);
		}
	}

	public int? Width
	{
		get
		{
			return _width;
		}
		set
		{
			if (_maxWidth.HasValue)
			{
				throw new Exception("Can't set Width, MaxWidth has been set.");
			}
			_width = value;
		}
	}

	public int? MinWidth
	{
		get
		{
			return _minWidth;
		}
		set
		{
			if (_width.HasValue)
			{
				throw new Exception("Can't set MinWidth, Width has been set.");
			}
			_minWidth = value;
		}
	}

	public int? MaxWidth
	{
		get
		{
			return _maxWidth;
		}
		set
		{
			if (_width.HasValue)
			{
				throw new Exception("Can't set MaxWidth, Width has been set.");
			}
			_maxWidth = value;
		}
	}
}
