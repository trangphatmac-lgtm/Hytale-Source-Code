using HytaleClient.AssetEditor.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class IconEditor : AssetFileSelectorEditor
{
	public IconEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void Build()
	{
		new Button(Desktop, this)
		{
			Anchor = new Anchor
			{
				Width = 30
			},
			Background = new PatchStyle("AssetEditor/EditIcon.png"),
			Activating = OpenEditModal
		};
		new Group(Desktop, this)
		{
			Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 70)),
			Anchor = new Anchor
			{
				Width = 1
			}
		};
		base.Build();
		_layoutMode = LayoutMode.Left;
		_dropdown.FlexWeight = 1;
	}

	private void OpenEditModal()
	{
		ConfigEditor.IconExporterModal.Open();
	}
}
