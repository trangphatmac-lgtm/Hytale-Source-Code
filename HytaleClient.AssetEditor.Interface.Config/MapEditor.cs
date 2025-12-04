using System.Collections.Generic;
using System.Linq;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class MapEditor : ValueEditor
{
	private readonly Dictionary<string, PropertyEditor> _properties = new Dictionary<string, PropertyEditor>();

	private ReorderableList _reorderableList;

	public MapEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
		_layoutMode = LayoutMode.Full;
	}

	protected override void Build()
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		_reorderableList?.Clear();
		_properties.Clear();
		_reorderableList = new ReorderableList(Desktop, this)
		{
			ElementReordered = OnListElementReordered,
			DropIndicatorAnchor = new Anchor
			{
				Height = 2,
				Horizontal = -2
			},
			DropIndicatorBackground = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 200))
		};
		if (base.Value == null || ((JContainer)(JObject)base.Value).Count == 0)
		{
			BuildEmptyLabel();
			return;
		}
		foreach (KeyValuePair<string, JToken> item in (JObject)base.Value)
		{
			PropertyEditor propertyEditor = new PropertyEditor(Desktop, _reorderableList, item.Key, GetSchema(), Path.GetChild(item.Key), Schema, ConfigEditor, this);
			propertyEditor.Build(item.Value, filterCategory: false, IsDetachedEditor, CachesToRebuild);
			_properties.Add(item.Key, propertyEditor);
		}
	}

	private void BuildEmptyLabel()
	{
		new Label(Desktop, _reorderableList)
		{
			Text = Desktop.Provider.GetText("ui.assetEditor.mapEditor.empty"),
			Style = new LabelStyle
			{
				FontSize = 12f,
				RenderItalics = true,
				TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 140),
				Alignment = LabelStyle.LabelAlignment.Center
			},
			Anchor = new Anchor
			{
				Height = 32
			},
			Padding = new Padding
			{
				Horizontal = 8
			}
		};
	}

	private SchemaNode GetSchema()
	{
		return ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(Schema.Value);
	}

	public override void SetValueRecursively(JToken value)
	{
		JToken value2 = base.Value;
		base.SetValueRecursively(value);
		if (value2 != base.Value)
		{
			_reorderableList.Clear();
			_properties.Clear();
			Build();
		}
	}

	public void OnItemRemoved(string key)
	{
		PropertyEditor child = _properties[key];
		_properties.Remove(key);
		_reorderableList.Remove(child);
		if (_properties.Count == 0)
		{
			BuildEmptyLabel();
		}
	}

	public void OnItemInserted(string key, JToken value)
	{
		if (_properties.Count == 0)
		{
			_reorderableList.Clear();
		}
		PropertyEditor propertyEditor = new PropertyEditor(Desktop, null, key, GetSchema(), Path.GetChild(key), Schema, ConfigEditor, this);
		propertyEditor.Build(value, filterCategory: false, IsDetachedEditor, CachesToRebuild);
		_reorderableList.Add(propertyEditor);
		_properties.Add(key, propertyEditor);
		ParentPropertyEditor?.UpdateAppearance();
	}

	private void OnListElementReordered(int sourceIndex, int targetIndex)
	{
		HandleMoveKey(((PropertyEditor)_reorderableList.Children[targetIndex]).PropertyName, targetIndex, reorderElement: false);
	}

	internal void HandleInsertKey(string key)
	{
		PropertyPath child = Path.GetChild(key);
		JToken defaultValue = SchemaParser.GetDefaultValue(ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(Schema.Value));
		JToken value = ((defaultValue != null) ? defaultValue.DeepClone() : null);
		if (ParentPropertyEditor != null && ParentPropertyEditor.IsCollapsed)
		{
			ParentPropertyEditor.SetCollapseState(uncollapsed: false);
		}
		ConfigEditor.OnChangeValue(child, value, null, CachesToRebuild?.Caches);
		ConfigEditor.Layout();
		if (_properties[key].IsMounted)
		{
			_properties[key].ValueEditor.Focus();
		}
	}

	internal void HandleRenameKey(string currentKey, string newKey)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		PropertyEditor propertyEditor = _properties[currentKey];
		_properties.Remove(currentKey);
		_properties.Add(newKey, propertyEditor);
		propertyEditor.UpdatePathRecursively(newKey, Path.GetChild(newKey));
		propertyEditor.Layout();
		JToken previousValue = base.Value.DeepClone();
		JObject val = (JObject)base.Value;
		JToken val2 = val[currentKey];
		JToken val3 = ((JToken)val2.Parent).BeforeSelf().LastOrDefault();
		val.Remove(currentKey);
		if (val3 == null)
		{
			((JContainer)val).AddFirst((object)new JProperty(newKey, (object)val2));
		}
		else
		{
			val3.AddAfterSelf((object)new JProperty(newKey, (object)val2));
		}
		ConfigEditor.OnChangeValue(Path, (JToken)(object)val, previousValue, CachesToRebuild?.Caches);
	}

	public void HandleMoveKey(string key, bool backwards)
	{
		PropertyEditor propertyEditor = _properties[key];
		int num = ((!backwards) ? 1 : (-1));
		int num2 = -1;
		for (int i = 0; i < _reorderableList.Children.Count; i++)
		{
			if (_reorderableList.Children[i] == propertyEditor)
			{
				num2 = i;
				break;
			}
		}
		HandleMoveKey(key, num2 + num);
	}

	public void HandleMoveKey(string key, int targetIndex, bool reorderElement = true)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		JToken previousValue = base.Value.DeepClone();
		JObject val = (JObject)base.Value;
		JToken val2 = val[key];
		int num = -1;
		string text = null;
		foreach (KeyValuePair<string, JToken> item in val)
		{
			num++;
			if (item.Key == key)
			{
				break;
			}
		}
		int num2 = 0;
		foreach (KeyValuePair<string, JToken> item2 in val)
		{
			if (num2 == targetIndex)
			{
				text = item2.Key;
				break;
			}
			num2++;
		}
		val.Remove(key);
		if (num > targetIndex)
		{
			((JToken)val[text].Parent).AddBeforeSelf((object)new JProperty(key, (object)val2));
		}
		else
		{
			((JToken)val[text].Parent).AddAfterSelf((object)new JProperty(key, (object)val2));
		}
		if (reorderElement)
		{
			_reorderableList.Reorder(_properties[key], targetIndex);
			_reorderableList.Layout();
		}
		ConfigEditor.OnChangeValue(Path, (JToken)(object)val, previousValue, CachesToRebuild?.Caches);
	}

	public override void UpdatePathRecursively(PropertyPath path)
	{
		base.UpdatePathRecursively(path);
		foreach (KeyValuePair<string, PropertyEditor> property in _properties)
		{
		}
		foreach (KeyValuePair<string, PropertyEditor> property2 in _properties)
		{
			property2.Value.UpdatePathRecursively(property2.Key, Path.GetChild(property2.Key));
		}
	}

	public bool HasItemWithKey(string key)
	{
		key = key.ToLowerInvariant();
		foreach (KeyValuePair<string, PropertyEditor> property in _properties)
		{
			if (property.Key.ToLowerInvariant() == key)
			{
				return true;
			}
		}
		return false;
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 1;
	}

	public void SetCollapseStateForAllItems(bool uncollapsed)
	{
		foreach (PropertyEditor value in _properties.Values)
		{
			value.SetCollapseState(uncollapsed, doDiagnosticsAndLayout: false);
		}
		ConfigEditor.SetupDiagnostics(doLayout: false);
		ConfigEditor.Layout();
	}

	public override bool TryFindPropertyEditor(PropertyPath path, out PropertyEditor propertyEditor)
	{
		string key = path.Elements[Path.Elements.Length];
		if (!_properties.TryGetValue(key, out propertyEditor))
		{
			return false;
		}
		if (path.Elements.Length > Path.Elements.Length + 1)
		{
			return propertyEditor.ValueEditor.TryFindPropertyEditor(path, out propertyEditor);
		}
		return true;
	}

	public override void ClearDiagnosticsOfDescendants()
	{
		foreach (PropertyEditor value in _properties.Values)
		{
			value.ClearDiagnostics();
			value.ValueEditor.ClearDiagnosticsOfDescendants();
		}
	}
}
