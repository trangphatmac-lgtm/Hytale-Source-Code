using System;
using System.Collections.Generic;
using System.Globalization;

namespace HytaleClient.Interface.UI.Markup;

public class TextParser
{
	public class TextParserException : Exception
	{
		public readonly TextParserSpan Span;

		public readonly string RawMessage;

		public override string Message
		{
			get
			{
				Span.GetContext(3, out var startLine, out var startColumn, out var _, out var _, out var _);
				return $"Failed to parse file {Span.Parser.SourcePath} ({startLine + 1}:{startColumn + 1}) – {RawMessage}";
			}
		}

		public TextParserException(string message, TextParserSpan span)
			: base(message)
		{
			Span = span;
			RawMessage = message;
		}

		public TextParserException(string message, TextParserSpan span, Exception inner)
			: base(message, inner)
		{
			Span = span;
			RawMessage = message;
		}
	}

	public readonly string Data;

	public int Cursor;

	protected readonly Stack<int> _spanStack = new Stack<int>();

	public readonly string SourcePath;

	protected int PushSpan()
	{
		_spanStack.Push(Cursor);
		return Cursor;
	}

	protected TextParserSpan PopSpan(int startCursor)
	{
		if (startCursor != _spanStack.Pop())
		{
			throw new Exception("Invalid span stacking");
		}
		return new TextParserSpan(startCursor, Cursor, this);
	}

	public TextParser(string data, string sourcePath)
	{
		Data = data.Replace("\r\n", "\n");
		SourcePath = sourcePath;
	}

	public bool IsEOF()
	{
		return Cursor >= Data.Length;
	}

	public void GetCursorLocation(int cursor, out int line, out int column)
	{
		line = 0;
		column = 0;
		for (int i = 0; i < cursor; i++)
		{
			if (Data[i] == '\n')
			{
				line++;
				column = 0;
			}
			else
			{
				column++;
			}
		}
	}

	public void GetCursorInfo(out int line, out int column, out string context)
	{
		GetCursorLocation(Cursor, out line, out column);
		int num = Cursor - column;
		int num2 = Data.IndexOf('\n', num);
		if (num2 == -1)
		{
			num2 = Data.Length;
		}
		context = Data.Substring(num, num2 - num);
	}

	public bool Peek(char c)
	{
		return !IsEOF() && Data[Cursor] == c;
	}

	public bool Peek(string text)
	{
		return Cursor + text.Length < Data.Length && Data.Substring(Cursor, text.Length) == text;
	}

	public void Eat(char c)
	{
		int startCursor = PushSpan();
		if (IsEOF())
		{
			throw new TextParserException($"Expected {c}, found end of file", PopSpan(startCursor));
		}
		if (Data[Cursor] != c)
		{
			throw new TextParserException($"Expected {c}, found {Data[Cursor]}", PopSpan(startCursor));
		}
		Cursor++;
		PopSpan(startCursor);
	}

	public bool TryEat(char c)
	{
		if (IsEOF() || Data[Cursor] != c)
		{
			return false;
		}
		Cursor++;
		return true;
	}

	public void Eat(string text)
	{
		int startCursor = PushSpan();
		if (IsEOF())
		{
			throw new TextParserException("Expected " + text + ", found end of file", PopSpan(startCursor));
		}
		string text2 = Data.Substring(Cursor, text.Length);
		if (text2 != text)
		{
			throw new TextParserException("Expected " + text + ", found " + text2, PopSpan(startCursor));
		}
		Cursor += text.Length;
		PopSpan(startCursor);
	}

	public bool TryEat(string text)
	{
		if (Cursor + text.Length > Data.Length || Data.Substring(Cursor, text.Length) != text)
		{
			return false;
		}
		Cursor += text.Length;
		return true;
	}

	public void SkipUntil(char c)
	{
		int startCursor = PushSpan();
		while (true)
		{
			if (IsEOF())
			{
				throw new TextParserException($"Encountered end of file while looking for {c}", PopSpan(startCursor));
			}
			if (Data[Cursor] == c)
			{
				break;
			}
			Cursor++;
		}
		PopSpan(startCursor);
	}

	public int EatInteger()
	{
		int startCursor = PushSpan();
		if (IsEOF())
		{
			throw new TextParserException("Expected integer, found end of file", PopSpan(startCursor));
		}
		string text = "";
		while (!IsEOF())
		{
			char c = Data[Cursor];
			if (c >= '0' && c <= '9')
			{
				text += c;
				Cursor++;
				continue;
			}
			if (c == '-' && text.Length == 0)
			{
				text += c;
				Cursor++;
				continue;
			}
			if (text.Length == 0)
			{
				throw new TextParserException($"Expected integer, found {c}", PopSpan(startCursor));
			}
			break;
		}
		PopSpan(startCursor);
		return int.Parse(text);
	}

	public decimal EatDecimal()
	{
		int startCursor = PushSpan();
		if (IsEOF())
		{
			throw new TextParserException("Expected decimal, found end of file", PopSpan(startCursor));
		}
		bool flag = false;
		string text = "";
		while (!IsEOF())
		{
			char c = Data[Cursor];
			if (c >= '0' && c <= '9')
			{
				text += c;
				Cursor++;
				continue;
			}
			if (c == '-' && text.Length == 0)
			{
				text += c;
				Cursor++;
				continue;
			}
			if (c == '.' && text.Length > 0 && !flag)
			{
				text += c;
				flag = true;
				Cursor++;
				continue;
			}
			if (text.Length == 0 || text == "-")
			{
				throw new TextParserException($"Expected decimal, found {c}", PopSpan(startCursor));
			}
			break;
		}
		PopSpan(startCursor);
		return decimal.Parse(text, CultureInfo.InvariantCulture);
	}
}
