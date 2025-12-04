using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class DropdownEditor : ValueEditor
{
	private DropdownBox _dropdown;

	private bool _isRegistered;

	public DropdownEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void OnMounted()
	{
		base.OnMounted();
		if (!_isRegistered && Schema?.DataSet != null && _dropdown != null)
		{
			_isRegistered = true;
			ConfigEditor.AssetEditorOverlay.RegisterDropdownWithDataset(Schema.DataSet, _dropdown, GetGradientSetValue());
		}
	}

	protected override void OnUnmounted()
	{
		base.OnUnmounted();
		if (_isRegistered && Schema.DataSet != null)
		{
			_isRegistered = false;
			ConfigEditor.AssetEditorOverlay.UnregisterDropdownWithDataset(Schema.DataSet, _dropdown);
		}
	}

	private string GetGradientSetValue()
	{
		if (Schema.DataSet != "GradientIds")
		{
			return null;
		}
		PropertyEditor parentPropertyEditor = ParentPropertyEditor;
		object obj;
		if (parentPropertyEditor == null)
		{
			obj = null;
		}
		else
		{
			ValueEditor parentValueEditor = parentPropertyEditor.ParentValueEditor;
			if (parentValueEditor == null)
			{
				obj = null;
			}
			else
			{
				JToken value = parentValueEditor.Value;
				obj = ((value != null) ? value[(object)"GradientSet"] : null);
			}
		}
		JToken val = (JToken)obj;
		if (val == null)
		{
			return null;
		}
		return (string)val;
	}

	protected override void Build()
	{
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		if (Schema.DataSet == null)
		{
			for (int i = 0; i < Schema.Enum.Length; i++)
			{
				string text = Schema.Enum[i];
				list.Add(new DropdownBox.DropdownEntryInfo(Schema.EnumTitles[i] ?? text, text));
			}
		}
		_dropdown = new DropdownBox(Desktop, this)
		{
			ValueChanged = delegate
			{
				HandleChangeValue(JToken.op_Implicit(_dropdown.Value));
				Validate();
			},
			DropdownToggled = delegate
			{
				if (_dropdown.IsOpen && !(Schema.DataSet != "GradientIds"))
				{
					ConfigEditor.AssetEditorOverlay.UpdateDropdownDataset(Schema.DataSet, _dropdown, GetGradientSetValue());
				}
			},
			DisplayNonExistingValue = true,
			Entries = list,
			Style = ConfigEditor.DropdownBoxStyle,
			ShowSearchInput = true
		};
		_dropdown.Style.PanelScrollbarStyle = ScrollbarStyle.MakeDefault();
		_dropdown.Style.PanelScrollbarStyle.Size = 5;
		if (base.Value != null)
		{
			_dropdown.Value = (string)base.Value;
		}
		else if (Schema.DefaultValue != null)
		{
			_dropdown.Value = (string)Schema.DefaultValue;
		}
		else if (_parentSchema?.DefaultTypeSchema != null)
		{
			_dropdown.Value = _parentSchema.DefaultTypeSchema;
		}
		if (base.IsMounted && Schema.DataSet != null && !_isRegistered)
		{
			_isRegistered = true;
			ConfigEditor.AssetEditorOverlay.RegisterDropdownWithDataset(Schema.DataSet, _dropdown);
		}
	}

	protected override bool IsValueEmptyOrDefault(JToken value)
	{
		return Schema.DefaultValue != null && (string)Schema.DefaultValue == (string)value;
	}

	protected internal override void UpdateDisplayedValue()
	{
		if (base.Value != null)
		{
			_dropdown.Value = (string)base.Value;
		}
		else if (Schema.DefaultValue != null)
		{
			_dropdown.Value = (string)Schema.DefaultValue;
		}
		else if (_parentSchema?.DefaultTypeSchema != null)
		{
			_dropdown.Value = _parentSchema.DefaultTypeSchema;
		}
		else
		{
			_dropdown.Value = null;
		}
		_dropdown.Layout();
	}

	public override void Focus()
	{
		_dropdown.Open();
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 8;
	}
}
