using System;

namespace HytaleClient.Interface.UI.Markup;

public class TextParserSpan
{
	public readonly TextParser Parser;

	public readonly int Start;

	public readonly int End;

	public TextParserSpan(int start, int end, TextParser parser)
	{
		Parser = parser;
		Start = start;
		End = end;
	}

	public void GetContext(int linesOfContext, out int startLine, out int startColumn, out string before, out string inside, out string after)
	{
		Parser.GetCursorLocation(Start, out startLine, out startColumn);
		int num = Start - startColumn;
		if (num > 0)
		{
			for (int i = 0; i < linesOfContext; i++)
			{
				int num2 = Parser.Data.LastIndexOf("\n", num - 1);
				if (num2 == -1)
				{
					break;
				}
				num = num2;
			}
		}
		int num3 = Parser.Data.IndexOf("\n", End);
		if (num3 == -1)
		{
			num3 = Parser.Data.Length;
		}
		else
		{
			for (int j = 0; j < linesOfContext; j++)
			{
				int num4 = Parser.Data.IndexOf("\n", num3 + 1);
				if (num4 == -1)
				{
					break;
				}
				num3 = num4;
			}
		}
		before = Parser.Data.Substring(num, Start - num);
		while (before.StartsWith("\n"))
		{
			before = before.Substring(1);
		}
		inside = Parser.Data.Substring(Start, End - Start);
		after = Parser.Data.Substring(End, num3 - End).TrimEnd(Array.Empty<char>());
	}
}
