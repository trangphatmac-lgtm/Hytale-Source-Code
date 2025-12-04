#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class AutoCompleteMenu : Element
{
	public class AutoCompleteMenuButton : TextButton
	{
		private readonly string _tag;

		private readonly AutoCompleteMenu _menu;

		public AutoCompleteMenuButton(AutoCompleteMenu parent, string tag)
			: base(parent.Desktop, parent)
		{
			_tag = tag;
			_menu = parent;
		}

		protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
		{
			base.OnMouseButtonUp(evt, activate);
			if (activate && (long)evt.Button == 1 && !Disabled)
			{
				_menu.TextEditor.OnAutoCompleteSelectedValue(_tag);
			}
			if (base.IsMounted)
			{
				_menu.Close();
			}
		}
	}

	private HashSet<string> _strings;

	private string _focusedTag;

	private TextButton.TextButtonStyle _buttonStyle;

	private TextButton.TextButtonStyle _buttonFocusedStyle;

	private Padding _buttonPadding;

	private Anchor _buttonAnchor;

	public TextEditor TextEditor { get; private set; }

	public AutoCompleteMenu(Desktop desktop)
		: base(desktop, null)
	{
		Anchor = new Anchor
		{
			Height = 80
		};
		Padding = new Padding
		{
			Full = 5
		};
		Background = new PatchStyle(UInt32Color.Black);
		base.Visible = false;
		_layoutMode = LayoutMode.TopScrolling;
		_scrollbarStyle.Size = 8;
		_scrollbarStyle.Spacing = 0;
	}

	public void Build()
	{
		Desktop.Provider.TryGetDocument("AssetEditor/AutoCompleteMenu.ui", out var document);
		_buttonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "ButtonStyle");
		_buttonFocusedStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "ButtonFocusedStyle");
		_buttonPadding = document.ResolveNamedValue<Padding>(Desktop.Provider, "ButtonPadding");
		_buttonAnchor = document.ResolveNamedValue<Anchor>(Desktop.Provider, "ButtonAnchor");
	}

	public void Open(TextEditor textEditor, int x, int y, int width)
	{
		TextEditor = textEditor;
		Anchor.Left = x;
		Anchor.Top = y;
		Anchor.Width = width;
		base.Visible = false;
		Clear();
		TextEditor.ConfigEditor.AssetEditorOverlay.Add(this);
		Layout(Parent.AnchoredRectangle);
	}

	public void Close()
	{
		TextEditor.ConfigEditor.AssetEditorOverlay.Remove(this);
		TextEditor = null;
	}

	protected override void OnUnmounted()
	{
		_focusedTag = null;
		_strings = null;
	}

	public void SetupResults(HashSet<string> strings)
	{
		Debug.Assert(strings != null);
		Clear();
		_focusedTag = null;
		_strings = strings;
		foreach (string @string in strings)
		{
			new AutoCompleteMenuButton(this, @string)
			{
				Name = @string,
				Text = @string,
				Anchor = _buttonAnchor,
				Padding = _buttonPadding,
				Style = _buttonStyle
			};
		}
		Anchor.Height = strings.Count * 20 + Padding.Vertical;
		base.Visible = strings.Count > 0;
		Layout(Parent.AnchoredRectangle);
	}

	protected internal override void OnKeyDown(SDL_Keycode keyCode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		base.OnKeyDown(keyCode, repeat);
		if ((int)keyCode != 1073741905)
		{
			if ((int)keyCode == 1073741906 && _strings.Count != 0)
			{
				SelectNext(invert: true);
			}
		}
		else if (_strings.Count != 0)
		{
			SelectNext(invert: false);
		}
	}

	private void SelectNext(bool invert)
	{
		if (_focusedTag != null)
		{
			TextButton textButton = Find<TextButton>(_focusedTag);
			textButton.Style = _buttonStyle;
			textButton.Layout();
		}
		List<string> list = _strings.ToList();
		int num = ((_focusedTag != null) ? (list.IndexOf(_focusedTag) + ((!invert) ? 1 : (-1))) : 0);
		if (num >= _strings.Count)
		{
			num = 0;
		}
		else if (num < 0)
		{
			num = _strings.Count - 1;
		}
		_focusedTag = list[num];
		TextButton textButton2 = Find<TextButton>(_focusedTag);
		textButton2.Style = _buttonFocusedStyle;
		textButton2.Layout();
	}

	protected internal override void Validate()
	{
		if (_focusedTag != null)
		{
			TextEditor.OnAutoCompleteSelectedValue(_focusedTag);
			Desktop.FocusElement(null);
		}
	}
}
