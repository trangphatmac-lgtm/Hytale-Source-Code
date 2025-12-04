using System;
using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.InGame;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.Messages;

internal static class FormattedMessageConverter
{
	public static string GetString(FormattedMessage message, IUIProvider provider)
	{
		string text = "";
		if (message.MessageId != null)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (message.Params != null)
			{
				foreach (KeyValuePair<string, object> param in message.Params)
				{
					dictionary[param.Key] = param.Value.ToString();
				}
			}
			if (message.MessageParams != null)
			{
				foreach (KeyValuePair<string, FormattedMessage> messageParam in message.MessageParams)
				{
					dictionary[messageParam.Key] = GetString(messageParam.Value, provider);
				}
			}
			text = provider.GetText(message.MessageId, dictionary);
		}
		else if (message.RawText != null)
		{
			text = message.RawText;
		}
		text = text.Replace("\t", "        ");
		if (message.Children != null)
		{
			foreach (FormattedMessage child in message.Children)
			{
				text += GetString(child, provider);
			}
		}
		return text;
	}

	public static List<Label.LabelSpan> GetLabelSpans(FormattedMessage message, IUIProvider uiProvider, SpanStyle style = default(SpanStyle), bool allowFormatting = true)
	{
		List<Label.LabelSpan> list = new List<Label.LabelSpan>();
		AppendLabelSpans(message, list, style, uiProvider, allowFormatting);
		return list;
	}

	public static List<Label.LabelSpan> GetLabelSpansFromMarkup(string message, SpanStyle style)
	{
		List<Label.LabelSpan> list = new List<Label.LabelSpan>();
		AppendLabelSpansFromMarkup(message, list, style);
		return list;
	}

	public static void AppendLabelSpansFromMarkup(string text, List<Label.LabelSpan> textSpans, SpanStyle style)
	{
		SpanStyle spanStyle = style;
		string text2 = "";
		Stack<SpanStyle> stack = new Stack<SpanStyle>();
		TextParser textParser = new TextParser(text, null);
		while (!textParser.IsEOF())
		{
			if (textParser.Data[textParser.Cursor] == '<')
			{
				AddSpan(textSpans, text2, spanStyle);
				text2 = "";
				int cursor = textParser.Cursor;
				if (textParser.TryEat("<b>"))
				{
					stack.Push(spanStyle);
					spanStyle.IsBold = true;
					spanStyle.LastTag = "b";
				}
				else if (textParser.TryEat("</b>"))
				{
					if (spanStyle.LastTag != "b")
					{
						break;
					}
					spanStyle = stack.Pop();
				}
				else if (textParser.TryEat("<i>"))
				{
					stack.Push(spanStyle);
					spanStyle.IsItalics = true;
					spanStyle.LastTag = "i";
				}
				else if (textParser.TryEat("</i>"))
				{
					if (spanStyle.LastTag != "i")
					{
						break;
					}
					spanStyle = stack.Pop();
				}
				else if (textParser.TryEat("<u>"))
				{
					stack.Push(spanStyle);
					spanStyle.IsUnderlined = true;
					spanStyle.LastTag = "u";
				}
				else if (textParser.TryEat("</u>"))
				{
					if (spanStyle.LastTag != "u")
					{
						break;
					}
					spanStyle = stack.Pop();
				}
				else if (textParser.TryEat("<color is=\"#"))
				{
					string text3 = textParser.Data.Substring(textParser.Cursor, 6);
					textParser.Cursor += 6;
					if (!textParser.TryEat("\">"))
					{
						textParser.Cursor = cursor;
						break;
					}
					byte r = byte.MaxValue;
					byte g = byte.MaxValue;
					byte b = byte.MaxValue;
					try
					{
						r = Convert.ToByte(text3.Substring(0, 2), 16);
						g = Convert.ToByte(text3.Substring(2, 2), 16);
						b = Convert.ToByte(text3.Substring(4, 2), 16);
					}
					catch
					{
					}
					stack.Push(spanStyle);
					spanStyle.Color = UInt32Color.FromRGBA(r, g, b, byte.MaxValue);
					spanStyle.LastTag = "color";
				}
				else if (textParser.TryEat("</color>"))
				{
					if (spanStyle.LastTag != "color")
					{
						break;
					}
					spanStyle = stack.Pop();
				}
				else if (textParser.TryEat("<a href=\""))
				{
					string text4 = "";
					while (!textParser.TryEat("\">"))
					{
						text4 += textParser.Data[textParser.Cursor];
						textParser.Cursor++;
					}
					stack.Push(spanStyle);
					spanStyle.Link = text4;
					spanStyle.LastTag = "a";
				}
				else
				{
					if (!textParser.TryEat("</a>") || spanStyle.LastTag != "a")
					{
						break;
					}
					spanStyle = stack.Pop();
				}
			}
			else
			{
				text2 += textParser.Data[textParser.Cursor];
				textParser.Cursor++;
			}
		}
		if (!textParser.IsEOF() || stack.Count > 0)
		{
			text2 += textParser.Data.Substring(textParser.Cursor, textParser.Data.Length - textParser.Cursor);
		}
		AddSpan(textSpans, text2, spanStyle);
	}

	private static void AppendLabelSpans(FormattedMessage message, List<Label.LabelSpan> textSpans, SpanStyle style, IUIProvider uiProvider, bool allowFormatting = true)
	{
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		if (message.Link != null)
		{
			style.Link = message.Link;
		}
		if (allowFormatting)
		{
			if (message.Color != null && message.Color.StartsWith("#"))
			{
				string color = message.Color;
				switch (color.Length)
				{
				case 4:
					try
					{
						byte r2 = Convert.ToByte(color.Substring(1, 1) + color.Substring(1, 1), 16);
						byte g2 = Convert.ToByte(color.Substring(2, 1) + color.Substring(2, 1), 16);
						byte b2 = Convert.ToByte(color.Substring(3, 1) + color.Substring(3, 1), 16);
						style.Color = UInt32Color.FromRGBA(r2, g2, b2, byte.MaxValue);
					}
					catch
					{
					}
					break;
				case 7:
					try
					{
						byte r = Convert.ToByte(color.Substring(1, 2), 16);
						byte g = Convert.ToByte(color.Substring(3, 2), 16);
						byte b = Convert.ToByte(color.Substring(5, 2), 16);
						style.Color = UInt32Color.FromRGBA(r, g, b, byte.MaxValue);
					}
					catch
					{
					}
					break;
				}
			}
			if (message.Bold.HasValue)
			{
				style.IsBold = message.Bold.Value;
			}
			if (message.Italic.HasValue)
			{
				style.IsItalics = message.Italic.Value;
			}
			if (message.Underlined.HasValue)
			{
				style.IsUnderlined = message.Underlined.Value;
			}
		}
		string text = null;
		if (message.MessageId != null)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (message.Params != null)
			{
				foreach (KeyValuePair<string, object> param in message.Params)
				{
					dictionary[param.Key] = param.Value.ToString();
				}
			}
			if (message.MessageParams != null)
			{
				foreach (KeyValuePair<string, FormattedMessage> messageParam in message.MessageParams)
				{
					dictionary[messageParam.Key] = GetString(messageParam.Value, uiProvider);
				}
			}
			text = uiProvider.GetText(message.MessageId, dictionary);
		}
		else if (message.RawText != null)
		{
			text = message.RawText;
		}
		if (text != null)
		{
			text = text.Replace("\t", "        ");
			ChatTagType result;
			if (allowFormatting && message.MarkupEnabled)
			{
				AppendLabelSpansFromMarkup(text, textSpans, style);
			}
			else if (message.Params != null && message.Params.ContainsKey("tagType") && Enum.TryParse<ChatTagType>(message.Params["tagType"].ToString(), out result))
			{
				if (uiProvider is Interface @interface)
				{
					SpanStyle style2 = style;
					ChatTagType val = result;
					ChatTagType val2 = val;
					if ((int)val2 == 0)
					{
						GameInstance instance = @interface.InGameView.InGame.Instance;
						object arg = message.Params["id"];
						ClientItemBase item = instance.ItemLibraryModule.GetItem(message.Params["id"].ToString());
						string text2 = @interface.GetText($"items.{arg}.name");
						ClientItemQuality[] itemQualities = instance.ServerSettings.ItemQualities;
						style2.Color = itemQualities[item.QualityIndex].TextColor;
						AddSpan(textSpans, "[" + text2 + "]", style2, message.Params);
					}
					else
					{
						AddSpan(textSpans, $"[Unrecognized tag type: {result}]", style2);
					}
				}
				else
				{
					AddSpan(textSpans, $"[Unrecognized tag type: {result}]", style);
				}
			}
			else
			{
				AddSpan(textSpans, text, style);
			}
		}
		if (message.Children == null)
		{
			return;
		}
		foreach (FormattedMessage child in message.Children)
		{
			AppendLabelSpans(child, textSpans, style, uiProvider);
		}
	}

	private static void AddSpan(List<Label.LabelSpan> spans, string text, SpanStyle style)
	{
		AddSpan(spans, text, style, null);
	}

	private static void AddSpan(List<Label.LabelSpan> spans, string text, SpanStyle style, Dictionary<string, object> parameters)
	{
		if (text.Length != 0)
		{
			spans.Add(new Label.LabelSpan
			{
				Text = text,
				Color = style.Color,
				IsBold = style.IsBold,
				IsItalics = style.IsItalics,
				IsUnderlined = style.IsUnderlined,
				IsUppercase = style.IsUppercase,
				Link = style.Link,
				Params = parameters
			});
		}
	}

	public static string GetString(FormattedMessage message, Interface @interface)
	{
		if (message.MessageId == null)
		{
			return message.RawText;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (message.Params != null)
		{
			foreach (KeyValuePair<string, object> param in message.Params)
			{
				dictionary[param.Key] = param.Value.ToString();
			}
		}
		if (message.MessageParams != null)
		{
			foreach (KeyValuePair<string, FormattedMessage> messageParam in message.MessageParams)
			{
				dictionary[messageParam.Key] = GetString(messageParam.Value, @interface);
			}
		}
		string text = @interface.GetText(message.MessageId, dictionary);
		if (message.Children != null)
		{
			foreach (FormattedMessage child in message.Children)
			{
				text += GetString(child, @interface);
			}
		}
		return text;
	}
}
