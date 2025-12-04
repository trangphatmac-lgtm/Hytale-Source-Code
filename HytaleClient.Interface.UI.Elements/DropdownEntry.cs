using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class DropdownEntry : Button
{
	public DropdownLayer Layer;

	private readonly Group _icon;

	private readonly Label _label;

	private string _text;

	[UIMarkupProperty]
	public string Value;

	[UIMarkupProperty]
	public bool Selected;

	[UIMarkupProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value;
			_label.Text = value;
		}
	}

	public DropdownEntry(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_label = new Label(Desktop, this);
	}

	public DropdownEntry(DropdownLayer layer, string value, string text, bool selected = false)
		: base(layer.Desktop, layer.EntriesContainer)
	{
		Layer = layer;
		Value = value;
		Selected = selected;
		_icon = new Group(Desktop, this)
		{
			Visible = false
		};
		_label = new Label(Desktop, this)
		{
			Text = text
		};
	}

	public void ApplyStylesFromDropdownBox()
	{
		DropdownBoxStyle style = Layer.DropdownBox.Style;
		int num = 0;
		Style.Sounds = style.EntrySounds;
		Anchor = new Anchor
		{
			Height = Layer.DropdownBox.Style.EntryHeight
		};
		if (style.EntryIconBackground != null || style.SelectedEntryIconBackground != null)
		{
			_icon.Anchor = new Anchor
			{
				Width = style.EntryIconWidth,
				Height = style.EntryIconHeight,
				Left = style.HorizontalEntryPadding
			};
			_icon.Visible = true;
			num = style.EntryIconWidth + style.HorizontalEntryPadding;
			if (Selected)
			{
				if (style.SelectedEntryIconBackground != null)
				{
					_icon.Background = style.SelectedEntryIconBackground;
				}
				else
				{
					_icon.Visible = false;
					num = 0;
				}
			}
			else if (style.EntryIconBackground != null)
			{
				_icon.Background = style.EntryIconBackground;
			}
			else
			{
				_icon.Visible = false;
				num = 0;
			}
		}
		_label.Style = ((Selected && style.SelectedEntryLabelStyle != null) ? style.SelectedEntryLabelStyle : (style.EntryLabelStyle ?? new LabelStyle()));
		_label.Anchor = new Anchor
		{
			Left = style.HorizontalEntryPadding + num,
			Right = style.HorizontalEntryPadding
		};
		OutlineColor = style.FocusOutlineColor;
	}

	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();
		Background = Layer.DropdownBox.Style.HoveredEntryBackground;
		ApplyStyles();
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();
		Background = null;
		ApplyStyles();
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if ((long)evt.Button == 1)
		{
			Background = Layer.DropdownBox.Style.PressedEntryBackground;
			ApplyStyles();
		}
		base.OnMouseButtonDown(evt);
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if ((long)evt.Button == 1)
		{
			Background = (activate ? Layer.DropdownBox.Style.HoveredEntryBackground : null);
			ApplyStyles();
		}
		base.OnMouseButtonUp(evt, activate);
		if ((long)evt.Button == 1 && activate)
		{
			Layer.OnActivateEntry(this);
		}
	}
}
