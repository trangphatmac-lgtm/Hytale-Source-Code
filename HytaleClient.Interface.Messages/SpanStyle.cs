using HytaleClient.Graphics;

namespace HytaleClient.Interface.Messages;

public struct SpanStyle
{
	public bool IsUppercase;

	public bool IsBold;

	public bool IsItalics;

	public bool IsUnderlined;

	public UInt32Color? Color;

	public string Link;

	public string LastTag;
}
