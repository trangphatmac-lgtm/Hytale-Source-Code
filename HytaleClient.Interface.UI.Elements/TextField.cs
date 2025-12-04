using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class TextField : InputField<string>
{
	public override string Value
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				base.CursorIndex = Value.Length;
			}
		}
	}

	[UIMarkupProperty]
	public string PlaceholderText
	{
		get
		{
			return _placeholderText;
		}
		set
		{
			_placeholderText = value;
		}
	}

	public TextField(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}
}
