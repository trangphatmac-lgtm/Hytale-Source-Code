using System;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

public abstract class InputField<T> : InputElement<T>
{
	private enum CharType
	{
		AlphaNumeric,
		Whitespace,
		Other
	}

	[UIMarkupProperty]
	public InputFieldStyle Style = new InputFieldStyle();

	[UIMarkupProperty]
	public InputFieldStyle PlaceholderStyle = new InputFieldStyle();

	[UIMarkupProperty]
	public InputFieldDecorationStyle Decoration;

	[UIMarkupProperty]
	public bool AutoFocus;

	[UIMarkupProperty]
	public bool AutoSelectAll;

	protected string _placeholderText;

	private InputFieldIcon _icon;

	private TexturePatch _iconPatch;

	private Rectangle _iconRectangle;

	private bool _isHoveringClearButton;

	private bool _isPressingClearButton;

	private InputFieldButtonStyle _clearButtonStyle;

	private TexturePatch _clearButtonPatch;

	private Rectangle _clearButtonRectangle;

	[UIMarkupProperty]
	public char? PasswordChar;

	[UIMarkupProperty]
	public bool IsReadOnly;

	[UIMarkupProperty]
	public int MaxLength = 255;

	public Action RightClicking;

	private int _mouseClickCount;

	public Action<SDL_Keycode> KeyDown;

	public Action Validating;

	public Action Dismissing;

	public Action Blurred;

	public Action Focused;

	private int _scrollOffset;

	protected string _text = "";

	private int _cursorIndex;

	private int _relativeSelectionOffset;

	protected bool _isFocused;

	private float _cursorTimer;

	private Font _font;

	private Font _placeholderFont;

	public int CursorIndex
	{
		get
		{
			return _cursorIndex;
		}
		set
		{
			_cursorIndex = MathHelper.Clamp(value, 0, _text.Length);
			_relativeSelectionOffset = 0;
			_cursorTimer = 0f;
		}
	}

	public int RelativeSelectionOffset
	{
		get
		{
			return _relativeSelectionOffset;
		}
		set
		{
			_relativeSelectionOffset = MathHelper.Clamp(value, -_cursorIndex, _text.Length - _cursorIndex);
		}
	}

	private string DisplayedText
	{
		get
		{
			if (PasswordChar.HasValue)
			{
				return new string(PasswordChar.Value, _text.Length);
			}
			return Style.RenderUppercase ? _text.ToUpperInvariant() : _text;
		}
	}

	public InputField(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		_isHoveringClearButton = false;
		Desktop.RegisterAnimationCallback(Animate);
		if (AutoFocus && Desktop.FocusedElement == null)
		{
			Desktop.FocusElement(this);
			if (!string.IsNullOrEmpty(_text) && AutoSelectAll)
			{
				SelectAll();
			}
		}
	}

	protected override void OnUnmounted()
	{
		if (_isFocused)
		{
			Desktop.FocusElement(null);
		}
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected virtual void Animate(float deltaTime)
	{
		_cursorTimer += deltaTime;
	}

	protected internal override void OnFocus()
	{
		_isFocused = true;
		Focused?.Invoke();
		if (base.IsMounted)
		{
			ApplyStyles();
		}
	}

	protected internal override void OnBlur()
	{
		_isFocused = false;
		Blurred?.Invoke();
		if (base.IsMounted)
		{
			ApplyStyles();
		}
	}

	protected override void ApplyParentScroll(Point scaledParentScroll)
	{
		_iconRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_clearButtonRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		base.ApplyParentScroll(scaledParentScroll);
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void ApplyStyles()
	{
		if (Decoration != null)
		{
			if (_isFocused)
			{
				Background = Decoration.Focused?.Background ?? Decoration.Default?.Background;
				OutlineColor = Decoration.Focused?.OutlineColor ?? Decoration.Default?.OutlineColor ?? UInt32Color.White;
				OutlineSize = Decoration.Focused?.OutlineSize ?? (Decoration.Default?.OutlineSize).GetValueOrDefault();
				_icon = Decoration.Focused?.Icon ?? Decoration.Default?.Icon;
				_clearButtonStyle = Decoration.Focused?.ClearButtonStyle ?? Decoration.Default?.ClearButtonStyle;
			}
			else
			{
				Background = Decoration.Default?.Background;
				OutlineColor = Decoration.Default?.OutlineColor ?? UInt32Color.White;
				OutlineSize = (Decoration.Default?.OutlineSize).GetValueOrDefault();
				_icon = Decoration.Default?.Icon;
				_clearButtonStyle = Decoration.Default?.ClearButtonStyle;
			}
		}
		base.ApplyStyles();
		_font = Desktop.Provider.GetFontFamily(Style.FontName.Value).RegularFont;
		_placeholderFont = Desktop.Provider.GetFontFamily(PlaceholderStyle.FontName.Value).RegularFont;
		_iconPatch = ((_icon != null) ? Desktop.MakeTexturePatch(_icon.Texture) : null);
		if (_isPressingClearButton)
		{
			_clearButtonPatch = ((_clearButtonStyle != null) ? Desktop.MakeTexturePatch(_clearButtonStyle.PressedTexture ?? _clearButtonStyle.HoveredTexture ?? _clearButtonStyle.Texture) : null);
		}
		else if (_isHoveringClearButton)
		{
			_clearButtonPatch = ((_clearButtonStyle != null) ? Desktop.MakeTexturePatch(_clearButtonStyle.HoveredTexture ?? _clearButtonStyle.Texture) : null);
		}
		else
		{
			_clearButtonPatch = ((_clearButtonStyle != null) ? Desktop.MakeTexturePatch(_clearButtonStyle.Texture) : null);
		}
	}

	protected override void LayoutSelf()
	{
		if (_iconPatch != null)
		{
			int num = Desktop.ScaleRound(_icon.Height);
			int num2 = Desktop.ScaleRound(_icon.Width);
			int x = ((_icon.Side == InputFieldIcon.InputFieldIconSide.Left) ? (_anchoredRectangle.Left + Desktop.ScaleRound(_icon.Offset)) : (_anchoredRectangle.Right - num2 - Desktop.ScaleRound(_icon.Offset)));
			int y = _anchoredRectangle.Top + (int)((float)_anchoredRectangle.Height / 2f - (float)num / 2f);
			_iconRectangle = new Rectangle(x, y, num2, num);
		}
		if (_clearButtonPatch != null)
		{
			int num3 = Desktop.ScaleRound(_clearButtonStyle.Height);
			int num4 = Desktop.ScaleRound(_clearButtonStyle.Width);
			int x2 = ((_clearButtonStyle.Side == InputFieldButtonStyle.InputFieldButtonSide.Left) ? (_anchoredRectangle.Left + Desktop.ScaleRound(_clearButtonStyle.Offset)) : (_anchoredRectangle.Right - num4 - Desktop.ScaleRound(_clearButtonStyle.Offset)));
			int y2 = _anchoredRectangle.Top + (int)((float)_anchoredRectangle.Height / 2f - (float)num3 / 2f);
			_clearButtonRectangle = new Rectangle(x2, y2, num4, num3);
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		_isPressingClearButton = false;
		if (activate && _clearButtonStyle != null && _clearButtonRectangle.Contains(Desktop.MousePosition) && _text != "")
		{
			_text = "";
			CursorIndex = 0;
			OnTextChanged();
			ValueChanged?.Invoke();
		}
		if (base.IsMounted)
		{
			ApplyStyles();
		}
		if (activate && (long)evt.Button == 3)
		{
			RightClicking?.Invoke();
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (_clearButtonStyle != null && _clearButtonRectangle.Contains(Desktop.MousePosition))
		{
			_isPressingClearButton = true;
			ApplyStyles();
			return;
		}
		_cursorTimer = 0f;
		_mouseClickCount = evt.Clicks;
		switch (evt.Clicks)
		{
		case 1:
			Desktop.FocusElement(this);
			CursorIndex = GetCursorIndexAtPosition(Desktop.MousePosition.X);
			break;
		case 2:
			if (_isFocused)
			{
				SelectWordAt(GetCursorIndexAtPosition(Desktop.MousePosition.X));
			}
			break;
		case 3:
			if (_isFocused)
			{
				SelectAll();
			}
			break;
		}
	}

	protected override void OnMouseEnter()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.IBeam);
	}

	protected override void OnMouseLeave()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
		_isHoveringClearButton = false;
		ApplyStyles();
	}

	protected override void OnMouseMove()
	{
		if (_mouseClickCount == 1 && base.CapturedMouseButton == 1u)
		{
			int num = CursorIndex + RelativeSelectionOffset;
			CursorIndex = GetCursorIndexAtPosition(Desktop.MousePosition.X);
			RelativeSelectionOffset = num - CursorIndex;
		}
		if (_clearButtonStyle != null && _clearButtonRectangle.Contains(Desktop.MousePosition))
		{
			if (!_isHoveringClearButton)
			{
				_isHoveringClearButton = true;
				SDL.SDL_SetCursor(Desktop.Cursors.Hand);
				ApplyStyles();
			}
		}
		else if (_isHoveringClearButton)
		{
			_isHoveringClearButton = false;
			SDL.SDL_SetCursor(Desktop.Cursors.IBeam);
			ApplyStyles();
		}
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Invalid comparison between Unknown and I4
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected I4, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Invalid comparison between Unknown and I4
		//IL_0538: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Invalid comparison between Unknown and I4
		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
		InputField<T> inputField = this;
		bool isShortcutKeyDown = Desktop.IsShortcutKeyDown;
		bool isShiftDown = Desktop.IsShiftKeyDown;
		bool isWordSkipDown = Desktop.IsWordSkipDown;
		bool isLineSkipDown = Desktop.IsLineSkipDown;
		if ((int)keycode <= 99)
		{
			if ((int)keycode != 8)
			{
				if ((int)keycode != 97)
				{
					if ((int)keycode != 99)
					{
						goto IL_0537;
					}
					if (isShortcutKeyDown && !PasswordChar.HasValue)
					{
						CopySelectionToClipboard();
					}
				}
				else if (isShortcutKeyDown)
				{
					SelectAll();
				}
			}
			else if (!IsReadOnly && _text.Length != 0)
			{
				if (RelativeSelectionOffset != 0)
				{
					if (DeleteSelectedText())
					{
						OnTextChanged();
						ValueChanged?.Invoke();
					}
				}
				else if (CursorIndex > 0)
				{
					int cursorIndex = CursorIndex;
					cursorIndex = ((!isWordSkipDown) ? (cursorIndex - 1) : (_text.LastIndexOf(' ', System.Math.Max(cursorIndex - 2, 0)) + 1));
					_text = _text.Remove(cursorIndex, CursorIndex - cursorIndex);
					CursorIndex = cursorIndex;
					OnTextChanged();
					ValueChanged?.Invoke();
				}
			}
		}
		else if ((int)keycode <= 120)
		{
			if ((int)keycode != 118)
			{
				if ((int)keycode != 120)
				{
					goto IL_0537;
				}
				if (isShortcutKeyDown && !PasswordChar.HasValue)
				{
					CopySelectionToClipboard();
					if (DeleteSelectedText())
					{
						OnTextChanged();
						ValueChanged?.Invoke();
					}
				}
			}
			else if (isShortcutKeyDown)
			{
				PasteFromClipboard();
			}
		}
		else if ((int)keycode != 127)
		{
			switch (keycode - 1073741898)
			{
			case 6:
				break;
			case 5:
				goto IL_042f;
			case 0:
				goto IL_0521;
			case 3:
				goto IL_052c;
			default:
				goto IL_0537;
			}
			if (isLineSkipDown)
			{
				SkipToLineStart();
			}
			else if (!isShiftDown && _relativeSelectionOffset != 0)
			{
				CursorIndex = System.Math.Min(_cursorIndex, _cursorIndex + _relativeSelectionOffset);
			}
			else
			{
				int num = _cursorIndex;
				if (isWordSkipDown)
				{
					if (num > 0)
					{
						num = _text.LastIndexOf(' ', System.Math.Max(num - 2, 0)) + 1;
					}
				}
				else
				{
					num--;
				}
				if (isShiftDown)
				{
					int num2 = CursorIndex + RelativeSelectionOffset;
					CursorIndex = num;
					RelativeSelectionOffset = num2 - CursorIndex;
				}
				else
				{
					CursorIndex = num;
				}
			}
		}
		else if (!IsReadOnly && _text.Length != 0)
		{
			if (RelativeSelectionOffset != 0)
			{
				if (DeleteSelectedText())
				{
					ValueChanged?.Invoke();
				}
			}
			else if (CursorIndex < _text.Length)
			{
				int cursorIndex2 = CursorIndex;
				if (isWordSkipDown)
				{
					cursorIndex2 = _text.IndexOf(' ', cursorIndex2, _text.Length - cursorIndex2) + 1;
					if (cursorIndex2 == 0)
					{
						cursorIndex2 = _text.Length;
					}
				}
				else
				{
					cursorIndex2++;
				}
				_text = _text.Remove(CursorIndex, cursorIndex2 - CursorIndex);
				OnTextChanged();
				ValueChanged?.Invoke();
			}
		}
		goto IL_0542;
		IL_0537:
		base.OnKeyDown(keycode, repeat);
		goto IL_0542;
		IL_0542:
		KeyDown?.Invoke(keycode);
		return;
		IL_042f:
		if (isLineSkipDown)
		{
			SkipToLineEnd();
		}
		else if (!isShiftDown && _relativeSelectionOffset != 0)
		{
			CursorIndex = System.Math.Max(_cursorIndex, _cursorIndex + _relativeSelectionOffset);
		}
		else
		{
			int cursorIndex3 = _cursorIndex;
			if (isWordSkipDown)
			{
				cursorIndex3 = _text.IndexOf(' ', cursorIndex3, _text.Length - cursorIndex3) + 1;
				if (cursorIndex3 == 0)
				{
					cursorIndex3 = _text.Length;
				}
			}
			else
			{
				cursorIndex3++;
			}
			if (isShiftDown)
			{
				int num3 = CursorIndex + RelativeSelectionOffset;
				CursorIndex = cursorIndex3;
				RelativeSelectionOffset = num3 - CursorIndex;
			}
			else
			{
				CursorIndex = cursorIndex3;
			}
		}
		goto IL_0542;
		IL_0521:
		SkipToLineStart();
		goto IL_0542;
		IL_052c:
		SkipToLineEnd();
		goto IL_0542;
		void SkipToLineEnd()
		{
			if (isShiftDown)
			{
				int cursorIndex4 = CursorIndex;
				CursorIndex = _text.Length;
				RelativeSelectionOffset = _text.Length - cursorIndex4;
			}
			else
			{
				CursorIndex = _text.Length;
			}
		}
		void SkipToLineStart()
		{
			if (isShiftDown)
			{
				RelativeSelectionOffset = -CursorIndex;
			}
			else
			{
				CursorIndex = 0;
			}
		}
	}

	public void SelectAll()
	{
		CursorIndex = _text.Length;
		RelativeSelectionOffset = -_text.Length;
	}

	private void SelectWordAt(int cursorIndex)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		CharType charType = CharType.Other;
		for (int i = 0; i < _text.Length; i++)
		{
			char c = _text[i];
			CharType charType2 = (char.IsWhiteSpace(c) ? CharType.Whitespace : ((!char.IsLetter(c) && !char.IsNumber(c)) ? CharType.Other : CharType.AlphaNumeric));
			if (charType2 != charType)
			{
				if (flag)
				{
					break;
				}
				charType = charType2;
				num = i;
			}
			num2 = i;
			if (i == cursorIndex)
			{
				flag = true;
				if (charType2 == CharType.Other)
				{
					num = i;
					break;
				}
			}
		}
		CursorIndex = num2 + 1;
		RelativeSelectionOffset = num - num2 - 1;
	}

	public bool DeleteSelectedText()
	{
		if (RelativeSelectionOffset > 0)
		{
			_text = _text.Remove(CursorIndex, RelativeSelectionOffset);
			RelativeSelectionOffset = 0;
			return true;
		}
		if (RelativeSelectionOffset < 0)
		{
			int num = CursorIndex + RelativeSelectionOffset;
			_text = _text.Remove(num, -RelativeSelectionOffset);
			CursorIndex = num;
			return true;
		}
		return false;
	}

	public void CopySelectionToClipboard()
	{
		if (RelativeSelectionOffset != 0)
		{
			string text = ((RelativeSelectionOffset <= 0) ? _text.Substring(CursorIndex + RelativeSelectionOffset, -RelativeSelectionOffset) : _text.Substring(CursorIndex, RelativeSelectionOffset));
			SDL.SDL_SetClipboardText(text);
		}
	}

	private bool TextOverflowsMaxLength(string text)
	{
		return MaxLength > 0 && _text.Length + text.Length > MaxLength && RelativeSelectionOffset == 0;
	}

	public void PasteFromClipboard()
	{
		string text = SDL.SDL_GetClipboardText();
		if (text != null)
		{
			text = text.Replace("\r\n", " ").Replace("\n", " ");
			DeleteSelectedText();
			if (TextOverflowsMaxLength(text))
			{
				text = text.Substring(0, MaxLength - _text.Length);
			}
			_text = _text.Insert(CursorIndex, text);
			CursorIndex += text.Length;
			OnTextChanged();
			ValueChanged?.Invoke();
		}
	}

	public void InsertAtCursor(string text)
	{
		if (!IsReadOnly)
		{
			DeleteSelectedText();
			if (TextOverflowsMaxLength(text))
			{
				text = text.Substring(0, MaxLength - _text.Length);
			}
			_text = _text.Insert(CursorIndex, text);
			CursorIndex += text.Length;
			OnTextChanged();
		}
	}

	protected internal override void OnTextInput(string text)
	{
		if (!IsReadOnly && !TextOverflowsMaxLength(text))
		{
			DeleteSelectedText();
			_text = _text.Insert(CursorIndex, text);
			CursorIndex++;
			OnTextChanged();
			ValueChanged?.Invoke();
		}
	}

	private int GetCursorIndexAtPosition(int x)
	{
		float num = Style.FontSize * Desktop.Scale;
		float num2 = num / (float)_font.BaseSize;
		float num3 = (float)(x - _rectangleAfterPadding.X + _scrollOffset) / num2;
		float num4 = 0f;
		int num5 = 0;
		string displayedText = DisplayedText;
		foreach (ushort key in displayedText)
		{
			if (_font.GlyphAdvances.TryGetValue(key, out var value))
			{
				if (num3 <= num4 + value / 2f)
				{
					break;
				}
				num4 += value;
				num5++;
			}
		}
		return num5;
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (_iconPatch != null)
		{
			Desktop.Batcher2D.RequestDrawPatch(_iconPatch, _iconRectangle, Desktop.Scale);
		}
		if (_clearButtonPatch != null && _text != "")
		{
			Desktop.Batcher2D.RequestDrawPatch(_clearButtonPatch, _clearButtonRectangle, Desktop.Scale);
		}
		Desktop.Batcher2D.PushScissor(_rectangleAfterPadding);
		float num = Style.FontSize * Desktop.Scale;
		float num2 = num / (float)_font.BaseSize;
		string displayedText = DisplayedText;
		int num3 = (int)(_font.CalculateTextWidth(displayedText.Substring(0, CursorIndex)) * num2);
		int width = _rectangleAfterPadding.Width;
		float num4 = _font.CalculateTextWidth(DisplayedText) * num2 + 1f;
		int num5 = (int)System.Math.Max(0f, num4 - (float)width);
		_scrollOffset = MathHelper.Clamp(_scrollOffset, 0, num5);
		int num6 = _rectangleAfterPadding.Width / 3;
		if (_rectangleAfterPadding.Width != 0 && num3 > _scrollOffset + width - 1)
		{
			_scrollOffset = System.Math.Min(num5, num3 - width + 1 + num6);
		}
		else if (num3 < _scrollOffset)
		{
			_scrollOffset = System.Math.Max(0, num3 - num6);
		}
		int num7 = _rectangleAfterPadding.X - _scrollOffset;
		int num8 = (int)((float)_font.Height * num2);
		int num9 = _rectangleAfterPadding.Center.Y - (int)((float)num8 / 2f);
		if (_text.Length == 0 && _placeholderText != null)
		{
			float num10 = PlaceholderStyle.FontSize * Desktop.Scale;
			float num11 = num10 / (float)_placeholderFont.BaseSize;
			Vector3 position = new Vector3(_rectangleAfterPadding.X, _rectangleAfterPadding.Center.Y - (int)((float)_placeholderFont.Height * num11 / 2f), 0f);
			Desktop.Batcher2D.RequestDrawText(_placeholderFont, num10, PlaceholderStyle.RenderUppercase ? _placeholderText.ToUpperInvariant() : _placeholderText, position, PlaceholderStyle.TextColor, PlaceholderStyle.RenderBold, PlaceholderStyle.RenderItalics);
		}
		else
		{
			Desktop.Batcher2D.RequestDrawText(_font, num, displayedText, new Vector3(num7, num9, 0f), Style.TextColor, Style.RenderBold, Style.RenderItalics);
		}
		if (RelativeSelectionOffset != 0)
		{
			int val = (int)(_font.CalculateTextWidth(displayedText.Substring(0, CursorIndex + RelativeSelectionOffset)) * num2);
			int num12 = System.Math.Min(num3, val);
			int num13 = System.Math.Max(num3, val);
			Desktop.Batcher2D.RequestDrawTexture(Desktop.Provider.WhitePixel.Texture, Desktop.Provider.WhitePixel.Rectangle, new Rectangle(num7 + num12, num9, num13 - num12, num8), UInt32Color.FromRGBA(_isFocused ? 4294967136u : 4294967072u));
		}
		if (_isFocused && Desktop.IsFocused && _cursorTimer % 1f < 0.5f)
		{
			Desktop.Batcher2D.RequestDrawTexture(Desktop.Provider.WhitePixel.Texture, Desktop.Provider.WhitePixel.Rectangle, new Rectangle(num7 + num3, num9, 1, num8), Style.TextColor);
		}
		Desktop.Batcher2D.PopScissor();
	}

	protected virtual void OnTextChanged()
	{
	}

	protected internal override void Validate()
	{
		if (_isFocused && Validating != null)
		{
			Validating();
		}
		else
		{
			base.Validate();
		}
	}

	protected internal override void Dismiss()
	{
		if (_isFocused && Dismissing != null)
		{
			Dismissing();
		}
		else
		{
			base.Dismiss();
		}
	}
}
