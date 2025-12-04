using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

internal class PopupMenuRow : Button
{
	private readonly PopupMenuLayer _layer;

	private readonly PopupMenuItem _menuItem;

	private readonly Element _icon;

	private readonly Label _label;

	public PopupMenuRow(PopupMenuLayer layer, PopupMenuItem menuItem)
		: base(layer.Desktop, null)
	{
		_layer = layer;
		_menuItem = menuItem;
		if (menuItem.IconTexturePath != null)
		{
			_icon = new Element(Desktop, this);
		}
		_label = new Label(Desktop, this)
		{
			Text = menuItem.Label
		};
		Activating = delegate
		{
			if (layer.CloseOnActivate)
			{
				layer.Close();
			}
			menuItem.Activating?.Invoke();
		};
		Background = _layer.Style.ItemBackground;
		Style.Sounds = _layer.Style.ItemSounds?.Clone() ?? new ButtonSounds();
		if (menuItem.ActivateSound != null)
		{
			Style.Sounds.Activate = menuItem.ActivateSound;
		}
	}

	protected override void ApplyStyles()
	{
		Anchor.Height = _layer.Style.RowHeight;
		_label.Padding = _layer.Style.ItemPadding;
		if (_layer.HasIcons)
		{
			if (_icon == null)
			{
				_label.Padding.Left = Desktop.UnscaleRound(_layer.Style.ItemIconSize) + _layer.Style.ItemPadding.Left * 2;
			}
			else
			{
				base.LayoutMode = LayoutMode.Left;
				_icon.Background = new PatchStyle
				{
					TexturePath = _menuItem.IconTexturePath
				};
				_icon.Anchor.Left = _layer.Style.ItemPadding.Left;
				_icon.Anchor.Top = (_layer.Style.RowHeight - Desktop.UnscaleRound(_layer.Style.ItemIconSize)) / 2;
				_icon.Anchor.Width = Desktop.UnscaleRound(_layer.Style.ItemIconSize);
				_icon.Anchor.Height = Desktop.UnscaleRound(_layer.Style.ItemIconSize);
			}
		}
		_label.Style = _layer.Style.ItemLabelStyle;
		base.ApplyStyles();
	}

	protected override void OnMouseEnter()
	{
		Background = _layer.Style.HoveredItemBackground;
		ApplyStyles();
	}

	protected override void OnMouseLeave()
	{
		Background = _layer.Style.ItemBackground;
		ApplyStyles();
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if ((long)evt.Button == 1)
		{
			Background = _layer.Style.PressedItemBackground;
			ApplyStyles();
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if ((long)evt.Button == 1)
		{
			Background = (activate ? _layer.Style.HoveredItemBackground : _layer.Style.ItemBackground);
			ApplyStyles();
			base.OnMouseButtonUp(evt, activate);
		}
	}
}
