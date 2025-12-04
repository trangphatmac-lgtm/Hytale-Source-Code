using System;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class ReadOnlyEditor : ValueEditor
{
	private Label _label;

	public ReadOnlyEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
		ContentHeight = 32;
	}

	protected override void Build()
	{
		Label label = new Label(Desktop, this);
		JToken value = base.Value;
		label.Text = ((value != null) ? value.ToString((Formatting)0, Array.Empty<JsonConverter>()) : null) ?? "";
		label.Padding = new Padding
		{
			Left = 5
		};
		label.Anchor = new Anchor
		{
			Height = 32
		};
		label.Style = new LabelStyle
		{
			VerticalAlignment = LabelStyle.LabelAlignment.Center,
			TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125)
		};
		_label = label;
	}

	protected internal override void UpdateDisplayedValue()
	{
		Label label = _label;
		JToken value = base.Value;
		label.Text = ((value != null) ? value.ToString((Formatting)0, Array.Empty<JsonConverter>()) : null) ?? "";
		_label.Layout();
	}
}
