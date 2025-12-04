using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class MultilineTextField : InputElement<string>
{
	private enum CharType
	{
		AlphaNumeric,
		Whitespace,
		Other
	}

	private struct LineInfo
	{
		public int LineIndex;

		public int StartAt;

		public int EndAt;
	}

	private class SelectionInfo
	{
		public readonly int StartLineNumber;

		public readonly int EndLineNumber;

		public readonly int StartCursorIndex;

		public readonly int EndCursorIndex;

		public readonly int LineCount;

		public SelectionInfo(int selectionLineNumber, int lineNumber, int selectionCursorIndex, int cursorIndex)
		{
			StartLineNumber = System.Math.Min(selectionLineNumber, lineNumber);
			EndLineNumber = System.Math.Max(selectionLineNumber, lineNumber);
			LineCount = EndLineNumber - StartLineNumber + 1;
			if (LineCount == 1)
			{
				StartCursorIndex = System.Math.Min(selectionCursorIndex, cursorIndex);
				EndCursorIndex = System.Math.Max(selectionCursorIndex, cursorIndex);
			}
			else if (EndLineNumber == lineNumber)
			{
				StartCursorIndex = selectionCursorIndex;
				EndCursorIndex = cursorIndex;
			}
			else
			{
				StartCursorIndex = cursorIndex;
				EndCursorIndex = selectionCursorIndex;
			}
		}

		public bool StartsBetween(int left, int right)
		{
			return StartCursorIndex >= left && StartCursorIndex <= right;
		}

		public bool EndsBetween(int left, int right)
		{
			return EndCursorIndex >= left && EndCursorIndex <= right;
		}
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

	[UIMarkupProperty]
	public bool IsReadOnly;

	[UIMarkupProperty]
	public int MaxLength = 255;

	[UIMarkupProperty]
	public int MaxLines = 0;

	[UIMarkupProperty]
	public bool AutoGrow = true;

	[UIMarkupProperty]
	public string PlaceholderText;

	public Action Dismissing;

	public Action Blurred;

	public Action Focused;

	private float _cursorTimer;

	private int _lineNumber;

	private int _cursorIndex;

	private bool _isFocused;

	private Font _font;

	private Font _placeholderFont;

	private float _fontSizeInPixels;

	private float _fontScale;

	private int _textAreaHeight;

	private int _textAreaLeft;

	private int _textAreaTop;

	private int _scaledLineHeight;

	private bool _isLeftMouseButtonHeld;

	private int _numMouseClicks;

	private int _numLineWraps = 0;

	private readonly List<LineInfo> _lineInfo = new List<LineInfo>();

	private int _selectionLineNumber = -1;

	private int _selectionCursorIndex = -1;

	private SelectionInfo _selection;

	private static readonly string[] newlineChars = new string[3] { "\r\n", "\r", "\n" };

	private int LineNumber
	{
		get
		{
			return _lineNumber;
		}
		set
		{
			_cursorTimer = 0f;
			_lineNumber = MathHelper.Clamp(value, 0, _lineInfo.Count - 1);
		}
	}

	private int LineIndex => _lineInfo[LineNumber].LineIndex;

	private int CursorIndex
	{
		get
		{
			return _cursorIndex;
		}
		set
		{
			_cursorTimer = 0f;
			_cursorIndex = MathHelper.Clamp(value, 0, CurrentLine.Length);
		}
	}

	public List<string> Lines { get; private set; } = new List<string> { "" };


	private string CurrentLine
	{
		get
		{
			return Lines[LineIndex];
		}
		set
		{
			Lines[LineIndex] = value;
		}
	}

	public override string Value
	{
		get
		{
			return string.Join(Environment.NewLine, Lines);
		}
		set
		{
			Lines = value.Split(newlineChars, StringSplitOptions.None).ToList();
			_selection = null;
			if (base.IsMounted)
			{
				UpdateLineInfo();
				MoveCursorToEnd();
			}
		}
	}

	public MultilineTextField(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		if (AutoFocus && Desktop.FocusedElement == null)
		{
			Desktop.FocusElement(this);
			if (GetLength() != 0 && AutoSelectAll)
			{
				SelectAll();
			}
		}
		LayoutParentForAutoGrow();
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
			}
			else
			{
				Background = Decoration.Default?.Background;
				OutlineColor = Decoration.Default?.OutlineColor ?? UInt32Color.White;
				OutlineSize = (Decoration.Default?.OutlineSize).GetValueOrDefault();
			}
		}
		_font = Desktop.Provider.GetFontFamily(Style.FontName.Value).RegularFont;
		_placeholderFont = Desktop.Provider.GetFontFamily(PlaceholderStyle.FontName.Value).RegularFont;
		_fontSizeInPixels = Style.FontSize * Desktop.Scale;
		_fontScale = _fontSizeInPixels / (float)_font.BaseSize;
		_scaledLineHeight = Desktop.ScaleRound((float)_font.Height * Style.FontSize / (float)_font.BaseSize);
		base.ApplyStyles();
	}

	protected override void LayoutSelf()
	{
		UpdateLineInfo();
		_textAreaHeight = _scaledLineHeight * GetTotalVisibleLines();
		_textAreaLeft = _rectangleAfterPadding.X;
		_textAreaTop = _rectangleAfterPadding.Center.Y - (int)((float)_textAreaHeight / 2f);
	}

	public override Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		ApplyStyles();
		Point result = base.ComputeScaledMinSize(maxWidth, maxHeight);
		if (!Anchor.Height.HasValue && FlexWeight == 0)
		{
			UpdateLineInfo();
			int num = (Padding.Vertical.HasValue ? Desktop.ScaleRound(Padding.Vertical.Value) : 0);
			result.Y = _scaledLineHeight * GetTotalVisibleLines() + num;
		}
		return result;
	}

	protected override float? GetScaledHeight()
	{
		float? scaledHeight = base.GetScaledHeight();
		if (scaledHeight.HasValue)
		{
			return scaledHeight;
		}
		int num = (Padding.Vertical.HasValue ? Desktop.ScaleRound(Padding.Vertical.Value) : 0);
		return _scaledLineHeight * GetTotalVisibleLines() + num;
	}

	private int GetLength()
	{
		return Lines.Sum((string l) => l.Length);
	}

	private int GetTotalOccupiedLines()
	{
		return _lineInfo.Count;
	}

	private int GetTotalVisibleLines()
	{
		return AutoGrow ? GetTotalOccupiedLines() : (_numLineWraps + MaxLines);
	}

	protected override void OnMouseEnter()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.IBeam);
	}

	protected override void OnMouseLeave()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
		ApplyStyles();
	}

	protected override void OnMouseMove()
	{
		if (_isLeftMouseButtonHeld)
		{
			LineNumber = GetLineNumberAtPosition(Desktop.MousePosition.Y);
			CursorIndex = GetAbsoluteCursorIndexAtPosition(Desktop.MousePosition);
			UpdateSelection();
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		_cursorTimer = 0f;
		_isLeftMouseButtonHeld = (long)evt.Button == 1;
		_numMouseClicks = evt.Clicks;
		if (evt.Clicks == 1)
		{
			Desktop.FocusElement(this);
			LineNumber = GetLineNumberAtPosition(Desktop.MousePosition.Y);
			CursorIndex = GetAbsoluteCursorIndexAtPosition(Desktop.MousePosition);
			if (_isLeftMouseButtonHeld)
			{
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
			}
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if ((long)evt.Button != 1)
		{
			return;
		}
		_isLeftMouseButtonHeld = false;
		switch (_numMouseClicks)
		{
		case 1:
			UpdateSelection();
			break;
		case 2:
			if (_isFocused)
			{
				SelectWordAt(GetAbsoluteCursorIndexAtPosition(Desktop.MousePosition));
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

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Invalid comparison between Unknown and I4
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Invalid comparison between Unknown and I4
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected I4, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Invalid comparison between Unknown and I4
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Invalid comparison between Unknown and I4
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		bool isShortcutKeyDown = Desktop.IsShortcutKeyDown;
		bool isShiftDown = Desktop.IsShiftKeyDown;
		bool isWordSkipDown = Desktop.IsWordSkipDown;
		bool isLineSkipDown = Desktop.IsLineSkipDown;
		if ((int)keycode <= 99)
		{
			if ((int)keycode <= 13)
			{
				if ((int)keycode != 8)
				{
					if ((int)keycode == 13 && (MaxLines <= 0 || Lines.Count != MaxLines) && !IsReadOnly)
					{
						DeleteSelection();
						if (CursorIndex < CurrentLine.Length)
						{
							string item = CurrentLine.Substring(0, CursorIndex);
							string currentLine = CurrentLine.Substring(CursorIndex, CurrentLine.Length - CursorIndex);
							CurrentLine = currentLine;
							Lines.Insert(LineIndex, item);
						}
						else
						{
							Lines.Insert(LineIndex + 1, "");
						}
						OnValueChanged();
						LineNumber++;
						CursorIndex = 0;
					}
				}
				else
				{
					if (IsReadOnly)
					{
						return;
					}
					if (!DeleteSelection())
					{
						if (CursorIndex > 0)
						{
							int cursorIndex = CursorIndex;
							cursorIndex = ((!isWordSkipDown) ? (cursorIndex - 1) : (CurrentLine.LastIndexOf(' ', System.Math.Max(cursorIndex - 2, 0)) + 1));
							if (LineNumber >= _lineInfo.Count || CursorIndex == _lineInfo[LineNumber].StartAt)
							{
								LineNumber--;
							}
							CurrentLine = CurrentLine.Remove(cursorIndex, CursorIndex - cursorIndex);
							CursorIndex = cursorIndex;
						}
						else if (LineNumber > 0)
						{
							string currentLine2 = CurrentLine;
							Lines.RemoveAt(LineIndex);
							LineNumber--;
							CursorIndex = CurrentLine.Length;
							if (currentLine2.Length > 0)
							{
								CurrentLine += currentLine2;
							}
						}
					}
					OnValueChanged();
				}
			}
			else if ((int)keycode != 97)
			{
				if ((int)keycode == 99 && isShortcutKeyDown)
				{
					CopySelectionToClipboard();
				}
			}
			else if (isShortcutKeyDown)
			{
				SelectAll();
			}
		}
		else if ((int)keycode <= 120)
		{
			if ((int)keycode != 118)
			{
				if ((int)keycode == 120 && isShortcutKeyDown)
				{
					CopySelectionToClipboard();
					if (DeleteSelection())
					{
						OnValueChanged();
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
			case 8:
				if (LineNumber == 0)
				{
					CursorIndex = 0;
				}
				else
				{
					MoveCursorVertically(-1);
				}
				if (isShiftDown)
				{
					UpdateSelection();
					break;
				}
				_selection = null;
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
				break;
			case 7:
				if (LineNumber == GetTotalOccupiedLines() - 1)
				{
					CursorIndex = CurrentLine.Length;
				}
				else
				{
					MoveCursorVertically(1);
				}
				if (isShiftDown)
				{
					UpdateSelection();
					break;
				}
				_selection = null;
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
				break;
			case 6:
				if (isLineSkipDown)
				{
					SkipToLineStart();
					break;
				}
				if (CursorIndex == 0 && LineNumber > 0)
				{
					LineNumber--;
					CursorIndex = CurrentLine.Length;
				}
				if (CursorIndex > 0)
				{
					if (isWordSkipDown)
					{
						CursorIndex = CurrentLine.LastIndexOf(' ', System.Math.Max(CursorIndex - 2, 0)) + 1;
					}
					else
					{
						CursorIndex--;
					}
				}
				if (CursorIndex < _lineInfo[LineNumber].StartAt)
				{
					LineNumber--;
				}
				if (isShiftDown)
				{
					UpdateSelection();
					break;
				}
				_selection = null;
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
				break;
			case 5:
				if (isLineSkipDown)
				{
					SkipToLineEnd();
					break;
				}
				if (CursorIndex == CurrentLine.Length && LineNumber < GetTotalOccupiedLines() - 1)
				{
					LineNumber++;
					CursorIndex = 0;
				}
				if (CursorIndex < CurrentLine.Length)
				{
					if (isWordSkipDown)
					{
						CursorIndex = CurrentLine.IndexOf(' ', CursorIndex, CurrentLine.Length - CursorIndex) + 1;
						if (CursorIndex == 0)
						{
							CursorIndex = CurrentLine.Length;
						}
					}
					else
					{
						CursorIndex++;
					}
				}
				if (CursorIndex > _lineInfo[LineNumber].EndAt)
				{
					LineNumber++;
				}
				if (isShiftDown)
				{
					UpdateSelection();
					break;
				}
				_selection = null;
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
				break;
			case 0:
				SkipToLineStart();
				break;
			case 3:
				SkipToLineEnd();
				break;
			case 1:
			case 2:
			case 4:
				break;
			}
		}
		else
		{
			if (IsReadOnly)
			{
				return;
			}
			if (!DeleteSelection() && LineNumber < GetTotalOccupiedLines())
			{
				if (CursorIndex == CurrentLine.Length && Lines.Count > LineIndex + 1)
				{
					string text = Lines[LineIndex + 1];
					Lines.RemoveAt(LineIndex + 1);
					CurrentLine += text;
				}
				else if (CursorIndex < CurrentLine.Length)
				{
					string currentLine3 = CurrentLine;
					int cursorIndex2 = CursorIndex;
					if (isWordSkipDown)
					{
						cursorIndex2 = currentLine3.IndexOf(' ', cursorIndex2, currentLine3.Length - cursorIndex2) + 1;
						if (cursorIndex2 == 0)
						{
							cursorIndex2 = currentLine3.Length;
						}
					}
					else
					{
						cursorIndex2++;
					}
					CurrentLine = currentLine3.Remove(CursorIndex, cursorIndex2 - CursorIndex);
				}
			}
			OnValueChanged();
		}
		void MoveCursorVertically(int offset)
		{
			if (!isShiftDown && _selection != null)
			{
				LineNumber = _selectionLineNumber;
				CursorIndex = _selectionCursorIndex;
				_selection = null;
			}
			else if (isShiftDown && _selectionCursorIndex == -1)
			{
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
			}
			int num = CursorIndex - _lineInfo[LineNumber].StartAt;
			LineNumber += offset;
			CursorIndex = System.Math.Min(_lineInfo[LineNumber].StartAt + num, CurrentLine.Length);
		}
		void SkipToLineEnd()
		{
			if (isShiftDown)
			{
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
			}
			CursorIndex = _lineInfo[LineNumber].EndAt;
			UpdateSelection();
		}
		void SkipToLineStart()
		{
			if (isShiftDown)
			{
				_selectionLineNumber = LineNumber;
				_selectionCursorIndex = CursorIndex;
			}
			CursorIndex = 0;
			UpdateSelection();
		}
	}

	private bool TextOverflowsMaxLength(string text)
	{
		string text2 = string.Join("", Lines);
		return MaxLength > 0 && text2.Length + text.Length > MaxLength && _selection == null;
	}

	protected internal override void OnTextInput(string text)
	{
		if (!IsReadOnly && !TextOverflowsMaxLength(text))
		{
			DeleteSelection();
			_selection = null;
			if (Lines.Count == 0)
			{
				Lines.Add("");
			}
			CurrentLine = CurrentLine.Insert(CursorIndex, text);
			CursorIndex += text.Length;
			OnValueChanged();
		}
	}

	private void OnValueChanged()
	{
		UpdateLineInfo();
		if (LineNumber >= _lineInfo.Count || CursorIndex < _lineInfo[LineNumber].StartAt)
		{
			LineNumber--;
		}
		else if (CursorIndex > _lineInfo[LineNumber].EndAt)
		{
			LineNumber++;
		}
		ValueChanged?.Invoke();
		LayoutParentForAutoGrow();
	}

	private void UpdateLineInfo()
	{
		int x = _rectangleAfterPadding.X + _rectangleAfterPadding.Width;
		_numLineWraps = 0;
		_lineInfo.Clear();
		for (int i = 0; i < Lines.Count; i++)
		{
			string text = Lines[i];
			int num = 0;
			int num2 = GetRelativeCursorIndexAtPosition(text, x);
			while (text.Length > num2)
			{
				int num3 = text.Substring(num, num2 - num).LastIndexOf(' ');
				if (num3 > 0)
				{
					num2 = num + num3 + 1;
				}
				_numLineWraps++;
				_lineInfo.Add(new LineInfo
				{
					LineIndex = i,
					StartAt = num,
					EndAt = num2
				});
				num = num2;
				num2 = num + GetRelativeCursorIndexAtPosition(text.Substring(num), x);
			}
			_lineInfo.Add(new LineInfo
			{
				LineIndex = i,
				StartAt = num,
				EndAt = num2
			});
		}
	}

	private void SelectWordAt(int cursorIndex)
	{
		int selectionCursorIndex = 0;
		int num = 0;
		bool flag = false;
		CharType charType = CharType.Other;
		for (int i = 0; i < CurrentLine.Length; i++)
		{
			char c = CurrentLine[i];
			CharType charType2 = (char.IsWhiteSpace(c) ? CharType.Whitespace : ((!char.IsLetter(c) && !char.IsNumber(c)) ? CharType.Other : CharType.AlphaNumeric));
			if (charType2 != charType)
			{
				if (flag)
				{
					break;
				}
				charType = charType2;
				selectionCursorIndex = i;
			}
			num = i;
			if (i == cursorIndex)
			{
				flag = true;
				if (charType2 == CharType.Other)
				{
					selectionCursorIndex = i;
					break;
				}
			}
		}
		_selectionLineNumber = LineNumber;
		_selectionCursorIndex = selectionCursorIndex;
		CursorIndex = num + 1;
		UpdateSelection();
	}

	private void SelectAll()
	{
		_selectionLineNumber = 0;
		_selectionCursorIndex = 0;
		MoveCursorToEnd();
		UpdateSelection();
	}

	private void MoveCursorToEnd()
	{
		LineNumber = GetTotalOccupiedLines() - 1;
		CursorIndex = CurrentLine.Length;
	}

	private void CopySelectionToClipboard()
	{
		if (_selection == null)
		{
			return;
		}
		string text = "";
		int num = _lineInfo[_selection.StartLineNumber].LineIndex;
		for (int i = _selection.StartLineNumber; i <= _selection.EndLineNumber; i++)
		{
			int lineIndex = _lineInfo[i].LineIndex;
			int startAt = _lineInfo[i].StartAt;
			int endAt = _lineInfo[i].EndAt;
			int num2 = ((i == _selection.StartLineNumber) ? (_selection.StartCursorIndex - startAt) : startAt);
			int num3 = ((i == _selection.EndLineNumber) ? _selection.EndCursorIndex : endAt);
			if (lineIndex != num)
			{
				text += Environment.NewLine;
			}
			text += Lines[lineIndex].Substring(num2, num3 - num2);
			num = lineIndex;
		}
		SDL.SDL_SetClipboardText(text);
	}

	private void PasteFromClipboard()
	{
		string text = SDL.SDL_GetClipboardText();
		if (text == null || (!DeleteSelection() && GetLength() == MaxLength))
		{
			return;
		}
		List<string> list = text.Split(newlineChars, StringSplitOptions.None).ToList();
		int num = ((MaxLines > 0 && Lines.Count + list.Count > MaxLines) ? (MaxLines - (Lines.Count - 1)) : list.Count);
		string text2 = ((CursorIndex == _lineInfo[LineNumber].EndAt) ? "" : CurrentLine.Substring(CursorIndex));
		CurrentLine = CurrentLine.Substring(0, CursorIndex);
		for (int i = 0; i < num; i++)
		{
			string text3 = list[i];
			bool flag = TextOverflowsMaxLength(text3 + text2);
			if (flag)
			{
				int num2 = MaxLength - GetLength() - text2.Length;
				text3 = ((num2 > 0) ? text3.Substring(0, num2) : "");
			}
			if (i == 0)
			{
				CurrentLine = CurrentLine.Insert(CursorIndex, text3);
				OnValueChanged();
				CursorIndex += text3.Length;
			}
			else
			{
				Lines.Insert(LineIndex + 1, text3);
				OnValueChanged();
				LineNumber++;
				CursorIndex = CurrentLine.Length;
			}
			if (i == num - 1)
			{
				CurrentLine += text2;
				OnValueChanged();
			}
			if (flag)
			{
				break;
			}
		}
		_selection = null;
	}

	private bool DeleteSelection()
	{
		if (_selection == null)
		{
			return false;
		}
		int lineIndex = _lineInfo[_selection.StartLineNumber].LineIndex;
		int lineIndex2 = _lineInfo[_selection.EndLineNumber].LineIndex;
		string text = Lines[lineIndex];
		string text2 = Lines[lineIndex2];
		string text3 = ((_selection.StartCursorIndex > 0) ? text.Substring(0, _selection.StartCursorIndex) : "");
		string text4 = ((_selection.EndCursorIndex < text2.Length) ? text2.Substring(_selection.EndCursorIndex) : "");
		Lines[lineIndex] = text3 + text4;
		for (int num = lineIndex2; num > lineIndex; num--)
		{
			Lines.RemoveAt(num);
		}
		LineNumber = _selection.StartLineNumber;
		CursorIndex = _selection.StartCursorIndex;
		UpdateLineInfo();
		_selection = null;
		return true;
	}

	private void UpdateSelection()
	{
		if (_selectionLineNumber == LineNumber && _selectionCursorIndex == CursorIndex)
		{
			_selection = null;
		}
		else
		{
			_selection = new SelectionInfo(_selectionLineNumber, LineNumber, _selectionCursorIndex, CursorIndex);
		}
	}

	private int GetLineNumberAtPosition(int y)
	{
		int num = y - _rectangleAfterPadding.Y;
		return System.Math.Min(System.Math.Max(0, num / _scaledLineHeight), GetTotalOccupiedLines() - 1);
	}

	private int GetAbsoluteCursorIndexAtPosition(Point pos)
	{
		int lineNumberAtPosition = GetLineNumberAtPosition(pos.Y);
		int lineIndex = LineIndex;
		int startAt = _lineInfo[lineNumberAtPosition].StartAt;
		int endAt = _lineInfo[lineNumberAtPosition].EndAt;
		int relativeCursorIndexAtPosition = GetRelativeCursorIndexAtPosition(Lines[lineIndex].Substring(startAt, endAt - startAt), pos.X);
		return startAt + relativeCursorIndexAtPosition;
	}

	private int GetRelativeCursorIndexAtPosition(string line, int x)
	{
		float num = (float)(x - _rectangleAfterPadding.X) / _fontScale;
		float num2 = 0f;
		int num3 = 0;
		foreach (ushort key in line)
		{
			if (_font.GlyphAdvances.TryGetValue(key, out var value))
			{
				if (num <= num2 + value / 2f)
				{
					break;
				}
				num2 += value;
				num3++;
			}
		}
		return num3;
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		Desktop.Batcher2D.PushScissor(_rectangleAfterPadding);
		if (GetLength() == 0 && _lineInfo.Count == 1 && PlaceholderText != null)
		{
			float size = PlaceholderStyle.FontSize * Desktop.Scale;
			Desktop.Batcher2D.RequestDrawText(_placeholderFont, size, PlaceholderStyle.RenderUppercase ? PlaceholderText.ToUpperInvariant() : PlaceholderText, new Vector3(_rectangleAfterPadding.X, _textAreaTop, 0f), PlaceholderStyle.TextColor, PlaceholderStyle.RenderBold, PlaceholderStyle.RenderItalics);
		}
		for (int i = 0; i < _lineInfo.Count; i++)
		{
			int startAt = _lineInfo[i].StartAt;
			int endAt = _lineInfo[i].EndAt;
			string text = Lines[_lineInfo[i].LineIndex].Substring(startAt, endAt - startAt);
			if (text.Length > 0)
			{
				Desktop.Batcher2D.RequestDrawText(_font, _fontSizeInPixels, text, new Vector3(_textAreaLeft, _textAreaTop + i * _scaledLineHeight, 0f), Style.TextColor, Style.RenderBold, Style.RenderItalics);
			}
		}
		if (_selection != null)
		{
			for (int j = _selection.StartLineNumber; j <= _selection.EndLineNumber; j++)
			{
				int startAt2 = _lineInfo[j].StartAt;
				int endAt2 = _lineInfo[j].EndAt;
				string text2 = Lines[_lineInfo[j].LineIndex].Substring(startAt2, endAt2 - startAt2);
				int num = 0;
				if (j == _selection.StartLineNumber && _selection.StartsBetween(startAt2, endAt2))
				{
					num = GetHorizontalPositionAt(text2, _selection.StartCursorIndex - startAt2);
				}
				int num2 = 0;
				num2 = ((j != _selection.EndLineNumber || !_selection.EndsBetween(startAt2, endAt2)) ? text2.Length : (_selection.EndCursorIndex - startAt2));
				int num3 = 0;
				if (text2.Length > 0)
				{
					num3 = GetHorizontalPositionAt(text2, num2);
				}
				else if (j != LineNumber || (j == LineNumber && j != CursorIndex))
				{
					num3 = 5;
				}
				int y = _textAreaTop + j * _scaledLineHeight;
				Desktop.Batcher2D.RequestDrawTexture(Desktop.Provider.WhitePixel.Texture, Desktop.Provider.WhitePixel.Rectangle, new Rectangle(_textAreaLeft + num, y, num3 - num, _scaledLineHeight), UInt32Color.FromRGBA(_isFocused ? 4294967136u : 4294967072u));
			}
		}
		if (_isFocused && Desktop.IsFocused && _cursorTimer % 1f < 0.5f)
		{
			int startAt3 = _lineInfo[LineNumber].StartAt;
			int endAt3 = _lineInfo[LineNumber].EndAt;
			string line2 = Lines[_lineInfo[LineNumber].LineIndex].Substring(startAt3, endAt3 - startAt3);
			int num4 = GetHorizontalPositionAt(line2, CursorIndex - startAt3);
			int y2 = _textAreaTop + LineNumber * _scaledLineHeight;
			Desktop.Batcher2D.RequestDrawTexture(Desktop.Provider.WhitePixel.Texture, Desktop.Provider.WhitePixel.Rectangle, new Rectangle(_textAreaLeft + num4, y2, 1, _scaledLineHeight), Style.TextColor);
		}
		Desktop.Batcher2D.PopScissor();
		int GetHorizontalPositionAt(string line, int charIndex)
		{
			return (int)(_font.CalculateTextWidth(line.Substring(0, charIndex)) * _fontScale);
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

	private void LayoutParentForAutoGrow()
	{
		Element element = this;
		while (element.Parent != null && element.Parent.LayoutMode != 0)
		{
			element = element.Parent;
		}
		element.Layout();
	}
}
