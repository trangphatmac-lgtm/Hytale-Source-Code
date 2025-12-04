using System;
using System.Collections.Generic;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Utils;

namespace HytaleClient.Interface.Messages;

[UIMarkupData]
public class FormattedMessage
{
	public List<FormattedMessage> Children;

	public string RawText;

	public string MessageId;

	public Dictionary<string, object> Params;

	public Dictionary<string, FormattedMessage> MessageParams;

	public string Color;

	public bool? Bold;

	public bool? Italic;

	public bool? Underlined;

	public string Link;

	public bool MarkupEnabled;

	public static FormattedMessage FromMessageId(string messageId, Dictionary<string, object> messageParams = null)
	{
		return new FormattedMessage
		{
			MessageId = messageId,
			Params = messageParams
		};
	}

	public static bool TryParseFromBson(sbyte[] bytes, out FormattedMessage message)
	{
		try
		{
			message = BsonHelper.ObjectFromBson<FormattedMessage>(bytes);
			return true;
		}
		catch (Exception)
		{
			message = null;
			return false;
		}
	}
}
