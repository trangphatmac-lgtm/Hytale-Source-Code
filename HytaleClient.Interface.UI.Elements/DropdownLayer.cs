using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

public class DropdownLayer : Group
{
	public readonly DropdownBox DropdownBox;

	public readonly Label Label;

	public readonly Group EntriesContainer;

	private readonly Group _container;

	private readonly Label _noItemsLabel;

	private readonly List<DropdownEntry> _entryComponents = new List<DropdownEntry>();

	private TextField _searchInput;

	private DropdownEntry _focusedEntry;

	public Action DropdownToggled;

	public DropdownLayer(DropdownBox box)
		: base(box.Desktop, null)
	{
		DropdownBox = box;
		_container = new Group(Desktop, this)
		{
			LayoutMode = LayoutMode.Top
		};
		Label = new Label(Desktop, _container)
		{
			Visible = false
		};
		_noItemsLabel = new Label(Desktop, _container);
		EntriesContainer = new Group(Desktop, _container)
		{
			LayoutMode = LayoutMode.TopScrolling,
			FlexWeight = 1
		};
	}

	protected override void OnMounted()
	{
		if (_searchInput != null)
		{
			Desktop.FocusElement(_searchInput);
		}
		DropdownToggled?.Invoke();
	}

	protected override void OnUnmounted()
	{
		if (_focusedEntry != null)
		{
			_focusedEntry = null;
			UpdateFocusedDropdownEntry();
		}
		if (_searchInput != null)
		{
			_searchInput.Value = "";
			OnEntriesChanged();
		}
		DropdownToggled?.Invoke();
	}

	public override Element HitTest(Point position)
	{
		return base.HitTest(position) ?? this;
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		if (DropdownBox.ShowSearchInput)
		{
			DropdownBoxStyle.DropdownBoxSearchInputStyle searchInputStyle = DropdownBox.Style.SearchInputStyle;
			if (searchInputStyle == null)
			{
				throw new Exception($"Search input enabled in dropdown box but SearchInputStyle not provided: {DropdownBox}");
			}
			if (_searchInput == null)
			{
				_searchInput = new TextField(Desktop, null)
				{
					ValueChanged = OnEntriesChanged,
					KeyDown = delegate(SDL_Keycode keycode)
					{
						//IL_0006: Unknown result type (might be due to invalid IL or missing references)
						OnKeyDown(keycode, 0);
					}
				};
				_container.Add(_searchInput, 0);
				if (base.IsMounted)
				{
					Desktop.FocusElement(_searchInput);
				}
			}
			_searchInput.Style = searchInputStyle.Style;
			_searchInput.PlaceholderStyle = searchInputStyle.PlaceholderStyle;
			_searchInput.PlaceholderText = searchInputStyle.PlaceholderText;
			_searchInput.Anchor = searchInputStyle.Anchor;
			_searchInput.Padding = searchInputStyle.Padding;
			_searchInput.Decoration = new InputFieldDecorationStyle
			{
				Default = new InputFieldDecorationStyleState
				{
					Icon = searchInputStyle.Icon,
					Background = searchInputStyle.Background,
					ClearButtonStyle = searchInputStyle.ClearButtonStyle
				}
			};
		}
		else if (_searchInput != null)
		{
			_container.Remove(_searchInput);
			_searchInput = null;
		}
		_container.Background = DropdownBox.Style.PanelBackground;
		_container.Padding = new Padding(DropdownBox.Style.PanelPadding);
		if (DropdownBox.PanelTitleText != null)
		{
			Label.Anchor = new Anchor
			{
				Left = DropdownBox.Style.HorizontalEntryPadding,
				Right = DropdownBox.Style.HorizontalEntryPadding,
				Height = DropdownBox.Style.EntryHeight
			};
			Label.Style = DropdownBox.Style.PanelTitleLabelStyle;
			Label.Text = DropdownBox.PanelTitleText;
			Label.Visible = true;
		}
		EntriesContainer.ScrollbarStyle = DropdownBox.Style.PanelScrollbarStyle ?? ScrollbarStyle.MakeDefault();
		if (_entryComponents.Count == 0 && DropdownBox.NoItemsText != null)
		{
			_noItemsLabel.Visible = true;
			_noItemsLabel.Anchor = new Anchor
			{
				Left = DropdownBox.Style.HorizontalEntryPadding,
				Right = DropdownBox.Style.HorizontalEntryPadding,
				Height = DropdownBox.Style.EntryHeight
			};
			_noItemsLabel.Style = DropdownBox.Style.NoItemsLabelStyle;
			_noItemsLabel.Text = DropdownBox.NoItemsText;
		}
		else
		{
			_noItemsLabel.Visible = false;
			foreach (DropdownEntry entryComponent in _entryComponents)
			{
				entryComponent.ApplyStylesFromDropdownBox();
			}
		}
		int listHeight = DropdownBox.Style.EntryHeight * System.Math.Min(DropdownBox.Style.EntriesInViewport, _entryComponents.Count) + DropdownBox.Style.PanelPadding * 2 + 2;
		if (_searchInput != null)
		{
			listHeight += _searchInput.Anchor.Height.GetValueOrDefault();
		}
		int num = DropdownBox.Style.PanelPadding - 2;
		int dropdownLeft = Desktop.UnscaleRound(DropdownBox.AnchoredRectangle.X) - num;
		int dropdownTop = Desktop.UnscaleRound(DropdownBox.AnchoredRectangle.Top) - num;
		int dropdownWidth = Desktop.UnscaleRound(DropdownBox.AnchoredRectangle.Width);
		int dropdownHeight = Desktop.UnscaleRound(DropdownBox.AnchoredRectangle.Height);
		int valueOrDefault = DropdownBox.Style.PanelWidth.GetValueOrDefault(dropdownWidth);
		if (Label.Visible)
		{
			listHeight += DropdownBox.Style.EntryHeight;
		}
		if (_noItemsLabel.Visible)
		{
			listHeight += DropdownBox.Style.EntryHeight;
		}
		_container.Anchor = new Anchor
		{
			Width = valueOrDefault,
			Height = listHeight
		};
		int windowHeight = Desktop.UnscaleRound(Desktop.ViewportRectangle.Height);
		int windowWidth = Desktop.UnscaleRound(Desktop.ViewportRectangle.Width);
		ApplyAlignment(DropdownBox.Style.PanelAlign, checkViewport: true);
		void ApplyAlignment(DropdownBoxStyle.DropdownBoxAlign alignment, bool checkViewport)
		{
			switch (alignment)
			{
			case DropdownBoxStyle.DropdownBoxAlign.Bottom:
				_container.Anchor.Left = dropdownLeft;
				_container.Anchor.Top = dropdownTop + dropdownHeight + DropdownBox.Style.PanelOffset;
				if (checkViewport && _container.Anchor.Top + _container.Anchor.Height > windowHeight)
				{
					ApplyAlignment(DropdownBoxStyle.DropdownBoxAlign.Top, checkViewport: false);
				}
				break;
			case DropdownBoxStyle.DropdownBoxAlign.Top:
				_container.Anchor.Left = dropdownLeft;
				_container.Anchor.Top = dropdownTop - listHeight - DropdownBox.Style.PanelOffset;
				if (checkViewport && _container.Anchor.Top < 0)
				{
					ApplyAlignment(DropdownBoxStyle.DropdownBoxAlign.Bottom, checkViewport: false);
				}
				break;
			case DropdownBoxStyle.DropdownBoxAlign.Left:
				_container.Anchor.Left = dropdownLeft - dropdownWidth - DropdownBox.Style.PanelOffset;
				_container.Anchor.Top = dropdownTop;
				if (checkViewport && _container.Anchor.Left < 0)
				{
					ApplyAlignment(DropdownBoxStyle.DropdownBoxAlign.Right, checkViewport: false);
				}
				break;
			case DropdownBoxStyle.DropdownBoxAlign.Right:
				_container.Anchor.Left = dropdownLeft + dropdownWidth + DropdownBox.Style.PanelOffset;
				_container.Anchor.Top = dropdownTop;
				if (checkViewport && _container.Anchor.Left + _container.Anchor.Width > windowWidth)
				{
					ApplyAlignment(DropdownBoxStyle.DropdownBoxAlign.Left, checkViewport: false);
				}
				break;
			}
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if ((_searchInput == null || !_searchInput.AnchoredRectangle.Contains(Desktop.MousePosition)) && (long)evt.Button == 1)
		{
			DropdownBox.CloseDropdown();
		}
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Invalid comparison between Unknown and I4
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Invalid comparison between Unknown and I4
		base.OnKeyDown(keycode, repeat);
		if (_searchInput != null && Desktop.IsShortcutKeyDown && (int)keycode == 102)
		{
			Desktop.FocusElement(_searchInput);
			_focusedEntry = null;
		}
		else if ((int)keycode == 1073741906 || (int)keycode == 1073741905)
		{
			if (_entryComponents.Count == 0)
			{
				return;
			}
			Desktop.FocusElement(null);
			if (_focusedEntry != null)
			{
				int num = _entryComponents.FindIndex((DropdownEntry e) => e.Value == _focusedEntry.Value) + (((int)keycode != 1073741906) ? 1 : (-1));
				if (num < 0)
				{
					if (_searchInput != null)
					{
						Desktop.FocusElement(_searchInput);
						_focusedEntry = null;
					}
					else
					{
						_focusedEntry = _entryComponents[_entryComponents.Count - 1];
					}
				}
				else
				{
					_focusedEntry = _entryComponents[(num < _entryComponents.Count) ? num : 0];
				}
			}
			else
			{
				_focusedEntry = (((int)keycode == 1073741906) ? _entryComponents.Last() : _entryComponents.First());
			}
		}
		UpdateFocusedDropdownEntry();
	}

	private void UpdateFocusedDropdownEntry()
	{
		foreach (DropdownEntry entryComponent in _entryComponents)
		{
			entryComponent.OutlineSize = ((_focusedEntry != null && entryComponent == _focusedEntry) ? DropdownBox.Style.FocusOutlineSize : 0);
		}
	}

	protected internal override void Dismiss()
	{
		DropdownBox.CloseDropdown();
	}

	internal void OnActivateEntry(DropdownEntry entry)
	{
		DropdownBox.SelectEntry(entry);
	}

	public void OnSelectedValuesChanged()
	{
		foreach (DropdownEntry entryComponent in _entryComponents)
		{
			entryComponent.Selected = DropdownBox.SelectedValues.Contains(entryComponent.Value);
		}
		if (base.IsMounted)
		{
			Layout();
		}
	}

	public void OnEntriesChanged()
	{
		EntriesContainer.Clear();
		_entryComponents.Clear();
		string text = ((_searchInput == null) ? "" : _searchInput.Value.Trim().ToLowerInvariant());
		bool flag = _focusedEntry != null;
		foreach (DropdownBox.DropdownEntryInfo entry in DropdownBox.Entries)
		{
			if (!(text != "") || entry.Label.ToLowerInvariant().Contains(text))
			{
				DropdownEntry dropdownEntry = new DropdownEntry(this, entry.Value, entry.Label, entry.Selected);
				_entryComponents.Add(dropdownEntry);
				if (_focusedEntry != null && entry.Value == _focusedEntry.Value)
				{
					dropdownEntry.OutlineSize = ((_focusedEntry != null && entry.Value == _focusedEntry.Value) ? DropdownBox.Style.FocusOutlineSize : 0);
					flag = false;
				}
				if (DropdownBox.SelectedValues.Contains(entry.Value))
				{
					entry.Selected = true;
					dropdownEntry.Selected = true;
				}
			}
		}
		if (flag)
		{
			_focusedEntry = null;
		}
		if (EntriesContainer.IsMounted)
		{
			Layout();
		}
	}

	public void AddEntry(DropdownEntry entry)
	{
		entry.Layer = this;
		entry.Activating = delegate
		{
			OnActivateEntry(entry);
		};
		_entryComponents.Add(entry);
		EntriesContainer.Add(entry);
	}

	public void DeselectEntries()
	{
		foreach (DropdownEntry entryComponent in _entryComponents)
		{
			entryComponent.Selected = false;
		}
	}

	public DropdownEntry GetEntryByValue(string value)
	{
		return _entryComponents.Find((DropdownEntry e) => e.Value.Equals(value));
	}

	protected internal override void Validate()
	{
		OnActivateEntry(_focusedEntry);
	}
}
