#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class Label : Element
{
	[UIMarkupData]
	public class LabelSpan
	{
		public string Text;

		public bool IsUppercase;

		public bool IsBold;

		public bool IsItalics;

		public bool IsUnderlined;

		public UInt32Color? Color;

		public string Link;

		public Dictionary<string, object> Params;
	}

	public class LabelSpanPortion
	{
		public LabelSpan Span;

		public string Text;

		public float X;

		public float Y;

		public float Width;

		public Point CenterPoint;
	}

	public Action<string> LinkActivating;

	public Action<LabelSpanPortion> TagMouseEntered;

	private LabelSpanPortion _currentlyHoveredTag;

	protected readonly List<LabelSpan> _textSpans = new List<LabelSpan>();

	protected readonly List<LabelSpanPortion> _textSpanPortions = new List<LabelSpanPortion>();

	protected readonly List<LabelSpanPortion> _linkSpanPortions = new List<LabelSpanPortion>();

	protected readonly List<LabelSpanPortion> _tagSpanPortions = new List<LabelSpanPortion>();

	[UIMarkupProperty]
	public LabelStyle Style = new LabelStyle();

	private FontFamily _fontFamily;

	private int _scaledLineHeight;

	[UIMarkupProperty]
	public string Text
	{
		set
		{
			_textSpanPortions.Clear();
			_textSpans.Clear();
			if (!string.IsNullOrEmpty(value))
			{
				_textSpans.Add(new LabelSpan
				{
					Text = value
				});
			}
		}
	}

	[UIMarkupProperty]
	public IList<LabelSpan> TextSpans
	{
		get
		{
			return _textSpans.AsReadOnly();
		}
		set
		{
			_textSpans.Clear();
			_textSpans.AddRange(value);
			_textSpanPortions.Clear();
		}
	}

	public Label(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position) || (_linkSpanPortions.Count == 0 && (_tagSpanPortions.Count == 0 || TagMouseEntered == null) && !_hasTooltipText))
		{
			return null;
		}
		return this;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (_linkSpanPortions.Count == 0)
		{
			return;
		}
		Debug.Assert(LinkActivating != null);
		int x = Desktop.MousePosition.X - _rectangleAfterPadding.X;
		int y = Desktop.MousePosition.Y - _rectangleAfterPadding.Y;
		foreach (LabelSpanPortion linkSpanPortion in _linkSpanPortions)
		{
			if (IsMouseInSpanPortion(x, y, linkSpanPortion))
			{
				LinkActivating(linkSpanPortion.Span.Link);
				break;
			}
		}
	}

	protected override void OnMouseMove()
	{
		if (_tagSpanPortions.Count == 0)
		{
			return;
		}
		Debug.Assert(TagMouseEntered != null);
		int x = Desktop.MousePosition.X - _rectangleAfterPadding.X;
		int y = Desktop.MousePosition.Y - _rectangleAfterPadding.Y;
		foreach (LabelSpanPortion tagSpanPortion in _tagSpanPortions)
		{
			if (IsMouseInSpanPortion(x, y, tagSpanPortion))
			{
				if (_currentlyHoveredTag != tagSpanPortion)
				{
					tagSpanPortion.CenterPoint = new Point((int)((float)_rectangleAfterPadding.X + tagSpanPortion.X + tagSpanPortion.Width / 2f), (int)((float)_rectangleAfterPadding.Y + tagSpanPortion.Y + (float)(GetSpanFont(tagSpanPortion.Span).Height / 2)));
					_currentlyHoveredTag = tagSpanPortion;
					TagMouseEntered(tagSpanPortion);
				}
				return;
			}
		}
		_currentlyHoveredTag = null;
		TagMouseEntered(null);
	}

	protected override void OnMouseLeave()
	{
		_currentlyHoveredTag = null;
		TagMouseEntered?.Invoke(null);
	}

	private bool IsMouseInSpanPortion(int x, int y, LabelSpanPortion portion)
	{
		return (float)x >= portion.X && (float)x < portion.X + portion.Width && (float)y >= portion.Y && (float)y < portion.Y + (float)_scaledLineHeight;
	}

	private Font GetSpanFont(LabelSpan span)
	{
		return (span.IsBold || Style.RenderBold) ? _fontFamily.BoldFont : _fontFamily.RegularFont;
	}

	public override Point ComputeScaledMinSize(int? containerMaxWidth, int? containerMaxHeight)
	{
		ApplyStyles();
		int num = Padding.Left.GetValueOrDefault() + Padding.Right.GetValueOrDefault();
		int? maxWidth = containerMaxWidth - Desktop.ScaleRound(num);
		if (Anchor.MaxWidth.HasValue || Anchor.Width.HasValue)
		{
			int num2 = Desktop.ScaleRound((Anchor.MaxWidth ?? Anchor.Width.Value) - num);
			if (!maxWidth.HasValue || maxWidth > num2)
			{
				maxWidth = num2;
			}
		}
		if (Anchor.MinWidth.HasValue)
		{
			maxWidth = System.Math.Max(Desktop.ScaleRound(Anchor.MinWidth.Value), maxWidth.GetValueOrDefault());
		}
		float x = 0f;
		float y = 0f;
		float longestLineWidth = 0f;
		int lineCount = 0;
		foreach (LabelSpan textSpan in _textSpans)
		{
			if (textSpan.Text.Length == 0)
			{
				continue;
			}
			Font font = GetSpanFont(textSpan);
			string spanText = (Style.RenderUppercase ? textSpan.Text.ToUpperInvariant() : textSpan.Text);
			int startIndex = 0;
			while (true)
			{
				if (x > 0f)
				{
					x += Desktop.Scale * Style.LetterSpacing;
				}
				GrowPortion(out var breakIndex2, out var portionWidth2, out var ellipsize2);
				if (breakIndex2 > startIndex)
				{
					x += portionWidth2;
				}
				if (ellipsize2)
				{
					int num3 = textSpan.Text.IndexOf('\n', breakIndex2);
					if (num3 == -1)
					{
						break;
					}
					FinishLine();
					startIndex = num3 + 1;
					continue;
				}
				if (breakIndex2 < spanText.Length && spanText[breakIndex2] == '\n')
				{
					breakIndex2++;
				}
				startIndex = breakIndex2;
				if (breakIndex2 < spanText.Length || spanText[spanText.Length - 1] == '\n')
				{
					FinishLine();
				}
				if (breakIndex2 != spanText.Length)
				{
					continue;
				}
				break;
			}
			void GrowPortion(out int breakIndex, out float portionWidth, out bool ellipsize)
			{
				breakIndex = spanText.Length;
				ellipsize = false;
				float num4 = 0f;
				float num5 = 0f;
				int num6 = 0;
				for (int i = startIndex; i < spanText.Length; i++)
				{
					if (spanText[i] == '\n')
					{
						num4 -= Style.LetterSpacing;
						breakIndex = i;
						break;
					}
					if (spanText[i] == ' ')
					{
						num5 = num4 - Style.LetterSpacing;
						num6 = i + 1;
					}
					float num7 = font.GetCharacterAdvance(spanText[i]) * Style.FontSize / (float)font.BaseSize;
					if (x + Desktop.Scale * (num4 + num7) > (float?)(maxWidth + 1) && num4 > 0f)
					{
						if (!Style.Wrap)
						{
							float num8 = font.GetCharacterAdvance(8230) * Style.FontSize / (float)font.BaseSize;
							while (i > 0 && x + Desktop.Scale * (num4 + num8) > (float?)(maxWidth + 1))
							{
								i--;
								num4 -= font.GetCharacterAdvance(spanText[i]) * Style.FontSize / (float)font.BaseSize;
							}
							num4 += num8;
							breakIndex = i;
							ellipsize = true;
						}
						else if (num5 > 0f)
						{
							num4 = num5;
							breakIndex = num6;
						}
						else
						{
							breakIndex = i;
						}
						break;
					}
					num4 += num7 + ((i < spanText.Length) ? Style.LetterSpacing : 0f);
				}
				portionWidth = Desktop.Scale * num4;
			}
		}
		FinishLine();
		Point result = ComputeScaledAnchorAndPaddingSize(containerMaxWidth);
		if (!Anchor.Width.HasValue && !Anchor.MaxWidth.HasValue)
		{
			result.X = ((lineCount > 1) ? containerMaxWidth : null) ?? (result.X + (int)System.Math.Ceiling(longestLineWidth));
		}
		if (Anchor.MinWidth.HasValue)
		{
			result.X = System.Math.Max(Desktop.ScaleRound(Anchor.MinWidth.Value), result.X);
		}
		if (!Anchor.Height.HasValue)
		{
			result.Y += _scaledLineHeight * lineCount;
		}
		return result;
		void FinishLine()
		{
			longestLineWidth = System.Math.Max(longestLineWidth, x);
			x = 0f;
			y += _scaledLineHeight;
			lineCount++;
		}
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_fontFamily = Desktop.Provider.GetFontFamily(Style.FontName.Value);
		_scaledLineHeight = Desktop.ScaleRound((float)_fontFamily.RegularFont.Height * Style.FontSize / (float)_fontFamily.RegularFont.BaseSize);
	}

	protected override void LayoutSelf()
	{
		_textSpanPortions.Clear();
		_linkSpanPortions.Clear();
		_tagSpanPortions.Clear();
		int maxWidth = _rectangleAfterPadding.Width;
		float x = 0f;
		float y = 0f;
		int lineCount = 0;
		List<LabelSpanPortion> lineTextPortions = new List<LabelSpanPortion>();
		List<LabelSpanPortion> lineLinkPortions = new List<LabelSpanPortion>();
		List<LabelSpanPortion> lineTagPortions = new List<LabelSpanPortion>();
		foreach (LabelSpan textSpan in _textSpans)
		{
			if (textSpan.Text.Length == 0)
			{
				continue;
			}
			Font font = GetSpanFont(textSpan);
			string spanText = (Style.RenderUppercase ? textSpan.Text.ToUpperInvariant() : textSpan.Text);
			int startIndex = 0;
			while (true)
			{
				if (x > 0f)
				{
					x += Desktop.Scale * Style.LetterSpacing;
				}
				GrowPortion(out var breakIndex2, out var portionWidth2, out var ellipsize2);
				if (breakIndex2 > startIndex || breakIndex2 == 0)
				{
					string text = spanText.Substring(startIndex, breakIndex2 - startIndex);
					if (ellipsize2)
					{
						text += "…";
					}
					LabelSpanPortion item = new LabelSpanPortion
					{
						Span = textSpan,
						Text = text,
						X = x,
						Y = y,
						Width = portionWidth2
					};
					if (textSpan.Link != null)
					{
						lineLinkPortions.Add(item);
					}
					else if (textSpan.Params != null && textSpan.Params.ContainsKey("tagType"))
					{
						lineTagPortions.Add(item);
					}
					else
					{
						lineTextPortions.Add(item);
					}
					x += portionWidth2;
				}
				if (ellipsize2)
				{
					int num = textSpan.Text.IndexOf('\n', breakIndex2);
					if (num == -1)
					{
						break;
					}
					FinishLine();
					startIndex = num + 1;
					continue;
				}
				if (breakIndex2 < spanText.Length && spanText[breakIndex2] == '\n')
				{
					breakIndex2++;
				}
				startIndex = breakIndex2;
				if (breakIndex2 < spanText.Length || spanText[spanText.Length - 1] == '\n')
				{
					FinishLine();
				}
				if (breakIndex2 != spanText.Length)
				{
					continue;
				}
				break;
			}
			void GrowPortion(out int breakIndex, out float portionWidth, out bool ellipsize)
			{
				breakIndex = spanText.Length;
				ellipsize = false;
				float num4 = 0f;
				float num5 = 0f;
				int num6 = 0;
				for (int i = startIndex; i < spanText.Length; i++)
				{
					if (spanText[i] == '\n')
					{
						num4 -= Style.LetterSpacing;
						breakIndex = i;
						break;
					}
					if (spanText[i] == ' ')
					{
						num5 = num4 - Style.LetterSpacing;
						num6 = i + 1;
					}
					float num7 = font.GetCharacterAdvance(spanText[i]) * Style.FontSize / (float)font.BaseSize;
					if (x + Desktop.Scale * (num4 + num7) > (float)(maxWidth + 1) && num4 > 0f)
					{
						if (!Style.Wrap)
						{
							float num8 = font.GetCharacterAdvance(8230) * Style.FontSize / (float)font.BaseSize;
							while (i > 0 && x + Desktop.Scale * (num4 + num8) > (float)(maxWidth + 1))
							{
								i--;
								num4 -= font.GetCharacterAdvance(spanText[i]) * Style.FontSize / (float)font.BaseSize;
							}
							num4 += num8;
							breakIndex = i;
							ellipsize = true;
						}
						else if (num5 > 0f)
						{
							num4 = num5;
							breakIndex = num6;
						}
						else
						{
							breakIndex = i;
						}
						break;
					}
					num4 += num7 + ((i < spanText.Length) ? Style.LetterSpacing : 0f);
				}
				portionWidth = Desktop.Scale * num4;
			}
		}
		FinishLine();
		if (Style.VerticalAlignment == LabelStyle.LabelAlignment.Start)
		{
			return;
		}
		int num2 = _rectangleAfterPadding.Height - _scaledLineHeight * lineCount;
		if (Style.VerticalAlignment == LabelStyle.LabelAlignment.Center)
		{
			num2 /= 2;
		}
		foreach (LabelSpanPortion textSpanPortion in _textSpanPortions)
		{
			textSpanPortion.Y += num2;
		}
		foreach (LabelSpanPortion linkSpanPortion in _linkSpanPortions)
		{
			linkSpanPortion.Y += num2;
		}
		foreach (LabelSpanPortion tagSpanPortion in _tagSpanPortions)
		{
			tagSpanPortion.Y += num2;
		}
		void FinishLine()
		{
			if (Style.HorizontalAlignment != 0)
			{
				float num3 = (float)maxWidth - x;
				if (Style.HorizontalAlignment == LabelStyle.LabelAlignment.Center)
				{
					num3 /= 2f;
				}
				foreach (LabelSpanPortion item2 in lineTextPortions)
				{
					item2.X += num3;
				}
				foreach (LabelSpanPortion item3 in lineLinkPortions)
				{
					item3.X += num3;
				}
				foreach (LabelSpanPortion item4 in lineTagPortions)
				{
					item4.X += num3;
				}
			}
			_textSpanPortions.AddRange(lineTextPortions);
			lineTextPortions.Clear();
			_linkSpanPortions.AddRange(lineLinkPortions);
			lineLinkPortions.Clear();
			_tagSpanPortions.AddRange(lineTagPortions);
			lineTagPortions.Clear();
			x = 0f;
			y += _scaledLineHeight;
			lineCount++;
		}
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		Debug.Assert(_textSpanPortions.Count > 0 || _textSpans.Count == 0, "Label hasn't been laid out since text was set.", "{0}", this);
		int underlineHeight = System.Math.Max(1, Desktop.ScaleRound(1f));
		int refX = _rectangleAfterPadding.X;
		int refY = _rectangleAfterPadding.Y;
		foreach (LabelSpanPortion textSpanPortion in _textSpanPortions)
		{
			DrawPortion(textSpanPortion);
		}
		foreach (LabelSpanPortion linkSpanPortion in _linkSpanPortions)
		{
			DrawPortion(linkSpanPortion);
		}
		foreach (LabelSpanPortion tagSpanPortion in _tagSpanPortions)
		{
			DrawPortion(tagSpanPortion);
		}
		void DrawPortion(LabelSpanPortion portion)
		{
			Font spanFont = GetSpanFont(portion.Span);
			UInt32Color color = portion.Span.Color ?? Style.TextColor;
			Desktop.Batcher2D.RequestDrawText(spanFont, Style.FontSize * Desktop.Scale, portion.Text, new Vector3((float)refX + portion.X, (float)refY + portion.Y, 0f), color, isBold: false, Style.RenderItalics || portion.Span.IsItalics, Style.LetterSpacing * Desktop.Scale);
			if (Style.RenderUnderlined || portion.Span.IsUnderlined)
			{
				int num = Desktop.ScaleRound((float)spanFont.Height * Style.FontSize / (float)spanFont.BaseSize);
				Desktop.Batcher2D.RequestDrawTexture(Desktop.Provider.WhitePixel.Texture, Desktop.Provider.WhitePixel.Rectangle, new Rectangle((int)((float)refX + portion.X), (int)((float)refY + portion.Y + (float)num), (int)portion.Width, underlineHeight), color);
			}
		}
	}
}
