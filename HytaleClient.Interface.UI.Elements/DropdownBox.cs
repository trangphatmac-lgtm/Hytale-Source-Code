using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class DropdownBox : InputElement<string>
{
	[UIMarkupData]
	public class DropdownEntryInfo
	{
		public string Label;

		public string Value;

		public bool Selected;

		public DropdownEntryInfo()
		{
		}

		public DropdownEntryInfo(string text, string value, bool selected = false)
		{
			Label = text;
			Value = value;
			Selected = selected;
		}
	}

	[UIMarkupProperty]
	public bool Disabled;

	[UIMarkupProperty]
	public DropdownBoxStyle Style = new DropdownBoxStyle();

	private List<DropdownEntryInfo> _entries;

	private List<string> _selectedValues;

	[UIMarkupProperty]
	public string PanelTitleText;

	[UIMarkupProperty]
	public int MaxSelection = 1;

	[UIMarkupProperty]
	public bool ShowSearchInput;

	[UIMarkupProperty]
	public bool ShowLabel = true;

	[UIMarkupProperty]
	public string NoItemsText;

	public bool DisplayNonExistingValue;

	private readonly Group _icon;

	private readonly Label _label;

	private readonly Element _arrow;

	private readonly DropdownLayer _dropdownLayer;

	[UIMarkupProperty]
	public IReadOnlyList<DropdownEntryInfo> Entries
	{
		get
		{
			return _entries;
		}
		set
		{
			if (value == null)
			{
				_entries = new List<DropdownEntryInfo>();
			}
			else
			{
				_entries = ((value is List<DropdownEntryInfo> list) ? list : new List<DropdownEntryInfo>(value));
			}
			_dropdownLayer.OnEntriesChanged();
		}
	}

	[UIMarkupProperty]
	public List<string> SelectedValues
	{
		get
		{
			return _selectedValues;
		}
		set
		{
			_selectedValues = value ?? new List<string>();
			_dropdownLayer.OnSelectedValuesChanged();
		}
	}

	public override string Value
	{
		get
		{
			return _selectedValues.FirstOrDefault();
		}
		set
		{
			if (_selectedValues.Count == 0)
			{
				_selectedValues.Add(value ?? "");
			}
			else
			{
				_selectedValues[0] = value ?? "";
			}
			_dropdownLayer.OnSelectedValuesChanged();
		}
	}

	public Action DropdownToggled
	{
		set
		{
			_dropdownLayer.DropdownToggled = value;
		}
	}

	public bool IsOpen => _dropdownLayer.IsMounted;

	public List<int> selectedIndexes()
	{
		List<int> list = new List<int>();
		int num = 0;
		foreach (DropdownEntryInfo entry in _entries)
		{
			if (_selectedValues.Contains(entry.Value))
			{
				list.Add(num);
			}
			num++;
		}
		return list;
	}

	public DropdownBox(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_entries = new List<DropdownEntryInfo>();
		_selectedValues = new List<string>();
		_icon = new Group(Desktop, this);
		_label = new Label(Desktop, this);
		_arrow = new Element(Desktop, this);
		_dropdownLayer = new DropdownLayer(this);
	}

	internal override void AddFromMarkup(Element child)
	{
		if (!(child is DropdownEntry dropdownEntry))
		{
			throw new Exception("Children of DropdownBox must be of type DropdownEntry");
		}
		_dropdownLayer.AddEntry(dropdownEntry);
		_entries.Add(new DropdownEntryInfo(dropdownEntry.Text, dropdownEntry.Value));
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void OnUnmounted()
	{
		if (_dropdownLayer.IsMounted)
		{
			CloseDropdown(playSound: false);
		}
	}

	protected override void ApplyStyles()
	{
		if (Disabled)
		{
			Background = Style.DisabledBackground ?? Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = (Style.DisabledArrowTexturePath ?? Style.DefaultArrowTexturePath)
			};
		}
		else if (_dropdownLayer.IsMounted || (base.CapturedMouseButton.HasValue && (long)base.CapturedMouseButton.Value == 1))
		{
			Background = Style.PressedBackground ?? Style.HoveredBackground ?? Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = (Style.PressedArrowTexturePath ?? Style.HoveredArrowTexturePath ?? Style.DefaultArrowTexturePath)
			};
		}
		else if (base.IsHovered)
		{
			Background = Style.HoveredBackground ?? Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = (Style.HoveredArrowTexturePath ?? Style.DefaultArrowTexturePath)
			};
		}
		else
		{
			Background = Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = Style.DefaultArrowTexturePath
			};
		}
		base.ApplyStyles();
		if (Style.IconTexturePath != null)
		{
			_icon.Anchor.Width = Style.IconWidth;
			_icon.Anchor.Height = Style.IconHeight;
			_icon.Anchor.Left = Style.HorizontalPadding;
			_icon.Background = new PatchStyle
			{
				TexturePath = Style.IconTexturePath
			};
		}
		_arrow.Anchor.Width = Style.ArrowWidth;
		_arrow.Anchor.Height = Style.ArrowHeight;
		_arrow.Anchor.Right = Style.HorizontalPadding;
		if (MaxSelection == 1 && ShowLabel)
		{
			LabelStyle labelStyle = ((Disabled && Style.DisabledLabelStyle != null) ? Style.DisabledLabelStyle : Style.LabelStyle);
			if (labelStyle != null)
			{
				_label.Style = labelStyle;
			}
			string firstValue = SelectedValues.FirstOrDefault();
			string text = (DisplayNonExistingValue ? firstValue : "");
			_label.Text = ((_entries.Count <= 0) ? text : (_entries.FirstOrDefault((DropdownEntryInfo e) => e.Value.Equals(firstValue))?.Label ?? text));
			_label.Anchor.Left = (_label.Anchor.Right = Style.HorizontalPadding);
		}
	}

	protected override void OnMouseEnter()
	{
		if (Disabled)
		{
			SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
		}
		else
		{
			SDL.SDL_SetCursor(Desktop.Cursors.Hand);
			if (Style.Sounds?.MouseHover != null)
			{
				Desktop.Provider.PlaySound(Style.Sounds.MouseHover);
			}
		}
		Layout();
	}

	protected override void OnMouseLeave()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
		Layout();
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (!Disabled && (long)evt.Button == 1)
		{
			if (Style.Sounds?.Activate != null)
			{
				Desktop.Provider.PlaySound(Style.Sounds.Activate);
			}
			Layout();
			Open();
		}
	}

	internal void SelectEntry(DropdownEntry entry)
	{
		if (entry != null)
		{
			if (MaxSelection != 1 && SelectedValues.Contains(entry.Value))
			{
				entry.Selected = false;
				SelectedValues.Remove(entry.Value);
			}
			else if (MaxSelection < 1 || SelectedValues.Count() + 1 < MaxSelection)
			{
				SelectedValues.Add(entry.Value);
				entry.Selected = true;
			}
			else if (MaxSelection == 1)
			{
				_dropdownLayer.DeselectEntries();
				SelectedValues.Clear();
				SelectedValues.Add(entry.Value);
				entry.Selected = true;
				CloseDropdown(playSound: false);
			}
			Layout();
			ValueChanged?.Invoke();
		}
	}

	internal void CloseDropdown(bool playSound = true)
	{
		if (playSound && Style.Sounds?.Close != null)
		{
			Desktop.Provider.PlaySound(Style.Sounds.Close);
		}
		Desktop.SetTransientLayer(null);
		if (base.IsMounted)
		{
			Layout();
		}
	}

	protected override void LayoutSelf()
	{
		if (_dropdownLayer.IsMounted)
		{
			_dropdownLayer.Layout();
		}
	}

	public T GetEnumValue<T>() where T : struct, IConvertible
	{
		if (!typeof(T).IsEnum)
		{
			throw new ArgumentException("T must be an enum");
		}
		return (T)Enum.Parse(typeof(T), SelectedValues.FirstOrDefault());
	}

	public void Open()
	{
		Desktop.SetTransientLayer(_dropdownLayer);
	}
}
