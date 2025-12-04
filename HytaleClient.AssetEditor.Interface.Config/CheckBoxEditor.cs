using HytaleClient.AssetEditor.Data;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class CheckBoxEditor : ValueEditor
{
	private CheckBox _checkBox;

	public CheckBoxEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void Build()
	{
		_checkBox = new CheckBox(Desktop, this)
		{
			Anchor = new Anchor
			{
				Width = 24,
				Height = 24,
				Left = 4,
				Top = 4
			},
			ValueChanged = delegate
			{
				HandleChangeValue(JToken.op_Implicit(_checkBox.Value));
				Validate();
			},
			Value = (bool)(base.Value ?? JToken.op_Implicit((bool)Schema.DefaultValue)),
			Style = ConfigEditor.CheckBoxStyle
		};
	}

	protected internal override void UpdateDisplayedValue()
	{
		_checkBox.Value = (bool)(base.Value ?? JToken.op_Implicit((bool)Schema.DefaultValue));
		_checkBox.Layout();
	}

	protected override bool IsValueEmptyOrDefault(JToken value)
	{
		return (bool)value == (bool)Schema.DefaultValue;
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		return (int)value.Type == 9;
	}
}
