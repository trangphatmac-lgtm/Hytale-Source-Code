using System;
using System.Collections.Generic;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
internal class TabNavigation : Element
{
	[UIMarkupElement]
	public class TabButton : Button
	{
		public TabStyle TabStyle;

		public bool IsSelected;

		[UIMarkupProperty]
		public PatchStyle Icon;

		[UIMarkupProperty]
		public PatchStyle IconSelected;

		[UIMarkupProperty]
		public Anchor? IconAnchor;

		[UIMarkupProperty]
		public string Id;

		private Group _overlayElement;

		private Group _iconElement;

		private Label _label;

		public ButtonSounds Sounds
		{
			set
			{
				Style.Sounds = value;
			}
		}

		[UIMarkupProperty]
		public string Text
		{
			set
			{
				_label.Text = value;
				_label.Visible = value != null;
			}
		}

		public TabButton(Desktop desktop, Element parent)
			: base(desktop, parent)
		{
			_iconElement = new Group(desktop, this);
			_overlayElement = new Group(desktop, this);
			_label = new Label(desktop, this)
			{
				Visible = false
			};
		}

		protected override void OnMouseEnter()
		{
			base.OnMouseEnter();
			UpdateStyle();
			Layout();
		}

		protected override void OnMouseLeave()
		{
			base.OnMouseLeave();
			UpdateStyle();
			Layout();
		}

		protected override void OnMouseButtonDown(MouseButtonEvent evt)
		{
			base.OnMouseButtonDown(evt);
			UpdateStyle();
			Layout();
		}

		protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
		{
			UpdateStyle();
			Layout();
			TabNavigation tabNavigation = (TabNavigation)Parent;
			if (activate && (long)evt.Button == 1 && (!IsSelected || tabNavigation.AllowUnselection))
			{
				if (Style.Sounds?.Activate != null)
				{
					Desktop.Provider.PlaySound(Style.Sounds?.Activate);
				}
				Activating?.Invoke();
			}
		}

		public void UpdateStyle()
		{
			TabStyleState tabStyleState = TabStyle.Default;
			if (base.CapturedMouseButton == 1u && TabStyle.Pressed != null)
			{
				tabStyleState = TabStyle.Pressed;
			}
			else if (base.IsHovered && TabStyle.Hovered != null)
			{
				tabStyleState = TabStyle.Hovered;
			}
			Anchor = tabStyleState.Anchor;
			FlexWeight = tabStyleState.FlexWeight;
			Background = tabStyleState.Background;
			Padding = tabStyleState.Padding;
			_overlayElement.Background = tabStyleState.Overlay;
			_iconElement.Background = ((IsSelected && IconSelected != null) ? IconSelected : Icon);
			if (_iconElement.Background != null)
			{
				_iconElement.Visible = true;
				_iconElement.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(int)(tabStyleState.IconOpacity * 255f));
				_iconElement.Anchor = IconAnchor ?? tabStyleState.IconAnchor;
			}
			else
			{
				_iconElement.Visible = false;
			}
			_label.Style = tabStyleState.LabelStyle;
		}
	}

	[UIMarkupData]
	public class TabNavigationStyle
	{
		public TabStyle TabStyle;

		public TabStyle SelectedTabStyle;

		public Anchor SeparatorAnchor;

		public PatchStyle SeparatorBackground;

		public ButtonSounds TabSounds;
	}

	[UIMarkupData]
	public class TabStyle
	{
		public TabStyleState Default;

		public TabStyleState Hovered;

		public TabStyleState Pressed;
	}

	[UIMarkupData]
	public class TabStyleState
	{
		public PatchStyle Background;

		public PatchStyle Overlay;

		public Anchor Anchor;

		public Padding Padding;

		public Anchor IconAnchor;

		public float IconOpacity = 1f;

		public LabelStyle LabelStyle;

		public int FlexWeight;
	}

	[UIMarkupData]
	public class Tab
	{
		public string Id;

		public PatchStyle Icon;

		public PatchStyle IconSelected;

		public Anchor? IconAnchor;

		public string Text;
	}

	[UIMarkupProperty]
	public TabNavigationStyle Style;

	[UIMarkupProperty]
	public string SelectedTab;

	[UIMarkupProperty]
	public bool AllowUnselection;

	public Action SelectedTabChanged;

	private readonly List<TabButton> _tabButtons = new List<TabButton>();

	public Tab[] Tabs
	{
		set
		{
			BuildTabNavigation(value);
		}
	}

	public TabNavigation(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_layoutMode = LayoutMode.Left;
	}

	internal override void AddFromMarkup(Element child)
	{
		TabButton tabButton = child as TabButton;
		if (tabButton == null)
		{
			throw new Exception("Children of TabNavigation must be of type TabButton");
		}
		if (Style.SeparatorBackground != null && _tabButtons.Count > 0)
		{
			new Group(Desktop, this)
			{
				Anchor = Style.SeparatorAnchor,
				Background = Style.SeparatorBackground
			};
		}
		Add(child);
		bool flag = SelectedTab == tabButton.Id;
		tabButton.IsSelected = flag;
		tabButton.TabStyle = (flag ? Style.SelectedTabStyle : Style.TabStyle);
		tabButton.Sounds = Style.TabSounds;
		tabButton.Activating = delegate
		{
			OnActivateTab(tabButton.Id);
		};
		_tabButtons.Add(tabButton);
	}

	private void BuildTabNavigation(Tab[] tabs)
	{
		Clear();
		_tabButtons.Clear();
		for (int i = 0; i < tabs.Length; i++)
		{
			Tab tab = tabs[i];
			bool isSelected = SelectedTab == tab.Id;
			_tabButtons.Add(new TabButton(Desktop, this)
			{
				Id = tab.Id,
				IsSelected = isSelected,
				Icon = tab.Icon,
				IconSelected = tab.IconSelected,
				IconAnchor = tab.IconAnchor,
				Text = tab.Text,
				Activating = delegate
				{
					OnActivateTab(tab.Id);
				}
			});
			if (Style.SeparatorBackground != null && i < tabs.Length - 1)
			{
				new Group(Desktop, this)
				{
					Anchor = Style.SeparatorAnchor,
					Background = Style.SeparatorBackground
				};
			}
		}
	}

	private void OnActivateTab(string tabId)
	{
		if (AllowUnselection && SelectedTab == tabId)
		{
			SelectedTab = null;
		}
		else
		{
			SelectedTab = tabId;
		}
		Layout();
		SelectedTabChanged?.Invoke();
	}

	public override Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		ApplyStyles();
		return base.ComputeScaledMinSize(maxWidth, maxHeight);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		foreach (TabButton tabButton in _tabButtons)
		{
			tabButton.IsSelected = SelectedTab == tabButton.Id;
			tabButton.TabStyle = (tabButton.IsSelected ? Style.SelectedTabStyle : Style.TabStyle);
			tabButton.Sounds = Style.TabSounds;
			tabButton.UpdateStyle();
		}
	}
}
