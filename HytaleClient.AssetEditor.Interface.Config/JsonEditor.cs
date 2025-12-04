using System;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class JsonEditor : ValueEditor
{
	private TextField _textInput;

	private Label _statusLabel;

	public JsonEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void Build()
	{
		_layoutMode = LayoutMode.Top;
		_statusLabel = new Label(Desktop, this)
		{
			Text = Desktop.Provider.GetText("ui.assetEditor.jsonObjectEditor.json"),
			Style = 
			{
				VerticalAlignment = LabelStyle.LabelAlignment.Center,
				FontSize = 13f
			},
			Anchor = new Anchor
			{
				Height = 30
			},
			Padding = new Padding
			{
				Horizontal = 7
			}
		};
		TextField textField = new TextField(Desktop, this);
		JToken value = base.Value;
		textField.Value = ((value != null) ? value.ToString((Formatting)0, Array.Empty<JsonConverter>()) : null) ?? "";
		textField.Anchor = new Anchor
		{
			Height = 40
		};
		textField.Padding = new Padding
		{
			Left = 5
		};
		textField.PlaceholderText = ((string)Schema.DefaultValue) ?? "";
		textField.PlaceholderStyle = new InputFieldStyle
		{
			TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100)
		};
		textField.Style = new InputFieldStyle
		{
			FontSize = 14f
		};
		textField.MaxLength = Schema.MaxLength;
		textField.Validating = OnTextInputValidating;
		textField.Blurred = OnTextInputBlurred;
		textField.Decoration = new InputFieldDecorationStyle
		{
			Default = new InputFieldDecorationStyleState
			{
				Background = new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 20))
			},
			Focused = new InputFieldDecorationStyleState
			{
				OutlineSize = 1,
				OutlineColor = UInt32Color.FromRGBA(205, 240, 252, 206)
			}
		};
		_textInput = textField;
	}

	protected internal override void UpdateDisplayedValue()
	{
		_statusLabel.Text = Desktop.Provider.GetText("ui.assetEditor.jsonObjectEditor.json");
		_statusLabel.Layout();
		TextField textInput = _textInput;
		JToken value = base.Value;
		textInput.Value = ((value != null) ? value.ToString((Formatting)0, Array.Empty<JsonConverter>()) : null) ?? "";
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 1;
	}

	private void OnTextInputBlurred()
	{
		if (!TryUpdateValue())
		{
			UpdateDisplayedValue();
		}
	}

	private void OnTextInputValidating()
	{
		TryUpdateValue();
	}

	private bool TryUpdateValue()
	{
		JObject value;
		try
		{
			JsonUtils.ValidateJson(_textInput.Value);
			value = JObject.Parse(_textInput.Value);
		}
		catch (JsonReaderException)
		{
			_statusLabel.Text = Desktop.Provider.GetText("ui.assetEditor.jsonObjectEditor.jsonParseError");
			_statusLabel.Layout();
			return false;
		}
		_statusLabel.Text = Desktop.Provider.GetText("ui.assetEditor.jsonObjectEditor.json");
		_statusLabel.Layout();
		HandleChangeValue((JToken)(object)value);
		return true;
	}
}
