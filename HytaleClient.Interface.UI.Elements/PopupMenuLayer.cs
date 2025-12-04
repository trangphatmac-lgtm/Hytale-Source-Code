using System.Collections.Generic;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

internal class PopupMenuLayer : Element
{
	[UIMarkupProperty]
	public PopupMenuLayerStyle Style = new PopupMenuLayerStyle();

	private readonly Element _parent;

	private readonly Group _itemsContainer;

	private readonly Label _title;

	public bool HasIcons = false;

	public bool CloseOnActivate = true;

	public IReadOnlyList<PopupMenuItem> Items { get; private set; }

	public PopupMenuLayer(Desktop uiManager, Element parent)
		: base(uiManager, null)
	{
		_parent = parent;
		_title = new Label(uiManager, this);
		_title.Visible = false;
		_itemsContainer = new Group(uiManager, this);
	}

	public void SetTitle(string title)
	{
		_title.Text = title;
		_title.Visible = title != null;
	}

	public void SetItems(IReadOnlyList<PopupMenuItem> items)
	{
		Items = items ?? new List<PopupMenuItem>();
		_itemsContainer.Clear();
		foreach (PopupMenuItem item in Items)
		{
			if (item.IconTexturePath != null)
			{
				HasIcons = true;
			}
			_itemsContainer.Add(new PopupMenuRow(this, item));
		}
		ApplyStyles();
	}

	private int ComputeHeight()
	{
		int num = Style.BaseHeight + _itemsContainer.Children.Count * Style.RowHeight;
		if (_title.Visible)
		{
			num += Style.RowHeight + 3;
		}
		return num;
	}

	protected override void ApplyStyles()
	{
		_layoutMode = LayoutMode.Top;
		Padding = new Padding(Style.Padding);
		Background = Style.Background;
		Anchor.MaxWidth = Style.MaxWidth;
		Anchor.Height = ComputeHeight();
		_title.Style = Style.TitleStyle;
		_title.Padding = Style.ItemPadding;
		_title.Background = Style.TitleBackground;
		if (HasIcons)
		{
			_title.Padding.Left = Desktop.UnscaleRound(Style.ItemIconSize) + Style.ItemPadding.Left * 2;
		}
		_itemsContainer.LayoutMode = LayoutMode.Top;
		base.ApplyStyles();
	}

	public override Element HitTest(Point position)
	{
		if (!_anchoredRectangle.Contains(position))
		{
			return this;
		}
		return base.HitTest(position);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		Close();
		Desktop.RefreshHover();
		Desktop.OnMouseDown(evt.Button, evt.Clicks);
	}

	protected internal override void Dismiss()
	{
		Close();
	}

	public void Open()
	{
		Anchor.Left = Desktop.UnscaleRound(Desktop.MousePosition.X);
		Anchor.Top = Desktop.UnscaleRound(Desktop.MousePosition.Y);
		float num = Desktop.Scale * (float)Anchor.MaxWidth.Value;
		if ((float)Desktop.MousePosition.X + num > (float)Desktop.ViewportRectangle.Width)
		{
			Anchor.Left -= Anchor.MaxWidth;
		}
		int num2 = ComputeHeight();
		float num3 = (float)num2 * Desktop.Scale;
		if ((float)Desktop.MousePosition.Y + num3 > (float)Desktop.ViewportRectangle.Height)
		{
			Anchor.Top -= num2;
		}
		Desktop.SetTransientLayer(this);
	}

	public void Close()
	{
		Desktop.SetTransientLayer(null);
	}
}
