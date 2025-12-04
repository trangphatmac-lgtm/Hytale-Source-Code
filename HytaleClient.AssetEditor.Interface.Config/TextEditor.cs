using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class TextEditor : ValueEditor
{
	private TextField _textInput;

	public TextEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void OnUnmounted()
	{
		if (ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.TextEditor == this)
		{
			ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.Close();
		}
		base.OnUnmounted();
		SubmitUpdateCommand();
	}

	protected override void Build()
	{
		_textInput = new TextField(Desktop, this)
		{
			Value = (((string)base.Value) ?? ""),
			Padding = new Padding
			{
				Left = 5
			},
			PlaceholderText = (((string)Schema.DefaultValue) ?? ""),
			PlaceholderStyle = new InputFieldStyle
			{
				TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100),
				FontSize = 14f
			},
			Style = new InputFieldStyle
			{
				FontSize = 14f
			},
			KeyDown = OnTextInputKeyPress,
			Focused = OnTextInputFocus,
			Blurred = OnTextInputBlur,
			Validating = OnTextInputValidate,
			Dismissing = OnTextInputDismissing,
			ValueChanged = OnTextInputChange,
			MaxLength = Schema.MaxLength,
			Decoration = new InputFieldDecorationStyle
			{
				Focused = new InputFieldDecorationStyleState
				{
					OutlineSize = 1,
					OutlineColor = UInt32Color.FromRGBA(244, 188, 81, 153)
				}
			}
		};
	}

	private void OnTextInputChange()
	{
		if (Schema.DataSet != null)
		{
			if (ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.TextEditor == null)
			{
				ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.Open(this, Desktop.UnscaleRound(_textInput.AnchoredRectangle.X), Desktop.UnscaleRound(_rectangleAfterPadding.Bottom), Desktop.UnscaleRound(_textInput.AnchoredRectangle.Width));
			}
			UpdateAutoComplete();
		}
		HandleChangeValue(JToken.op_Implicit(_textInput.Value), withheldCommand: true);
	}

	private void OnTextInputValidate()
	{
		Validate();
		SubmitUpdateCommand();
	}

	private void OnTextInputDismissing()
	{
		if (ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.TextEditor == this)
		{
			ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.Close();
		}
		else
		{
			Dismiss();
		}
	}

	private void OnTextInputBlur()
	{
		if (ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.TextEditor == this && !(Desktop.CapturedElement is AutoCompleteMenu.AutoCompleteMenuButton))
		{
			ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.Close();
		}
		SubmitUpdateCommand();
	}

	private void OnTextInputFocus()
	{
		if (Schema.DataSet != null)
		{
			ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.Open(this, Desktop.UnscaleRound(_textInput.AnchoredRectangle.X), Desktop.UnscaleRound(_rectangleAfterPadding.Bottom), Desktop.UnscaleRound(_textInput.AnchoredRectangle.Width));
			UpdateAutoComplete();
		}
	}

	private void OnTextInputKeyPress(SDL_Keycode keycode)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.IsMounted)
		{
			ConfigEditor.AssetEditorOverlay.AutoCompleteMenu.OnKeyDown(keycode, 0);
		}
	}

	internal void OnAutoCompleteSelectedValue(string text)
	{
		_textInput.Value = text;
		HandleChangeValue(JToken.op_Implicit(_textInput.Value));
	}

	private void UpdateAutoComplete()
	{
		AutoCompleteMenu autoCompleteMenu = ConfigEditor.AssetEditorOverlay.AutoCompleteMenu;
		if (autoCompleteMenu.Parent == null)
		{
			return;
		}
		ConfigEditor.AssetEditorOverlay.Backend.FetchAutoCompleteData(Schema.DataSet, _textInput.Value.Trim(), delegate(HashSet<string> results, FormattedMessage error)
		{
			if (autoCompleteMenu.Parent != null)
			{
				autoCompleteMenu.SetupResults(results ?? new HashSet<string>());
			}
		});
	}

	protected override bool IsValueEmptyOrDefault(JToken value)
	{
		if ((string)value == "")
		{
			return true;
		}
		if (Schema.DefaultValue != null && (string)Schema.DefaultValue == (string)value)
		{
			return true;
		}
		return false;
	}

	public override void Focus()
	{
		Desktop.FocusElement(_textInput);
	}

	public void SelectAll()
	{
		_textInput.SelectAll();
	}

	protected internal override void UpdateDisplayedValue()
	{
		_textInput.Value = ((string)base.Value) ?? "";
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 8;
	}

	protected override JToken SanitizeValue(JToken value)
	{
		string text = (string)value;
		if (Schema.MaxLength > 0 && text.Length > Schema.MaxLength)
		{
			value = JToken.op_Implicit(text.Substring(0, Schema.MaxLength));
		}
		return value;
	}

	public override void PasteValue(string text)
	{
		JToken value = SanitizeValue(JToken.op_Implicit(text));
		HandleChangeValue(value, withheldCommand: false, confirmed: false, updateDisplayedValue: true);
		ConfigEditor.Layout();
	}
}
