using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class ListEditor : ValueEditor
{
	private readonly List<PropertyEditor> _items = new List<PropertyEditor>();

	private ReorderableList _reorderableList;

	public ListEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
		_layoutMode = LayoutMode.Full;
	}

	private SchemaNode GetSchema()
	{
		return ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(Schema.Value);
	}

	protected override void Build()
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
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
		if (base.Value == null || ((JContainer)(JArray)base.Value).Count == 0)
		{
			BuildEmptyLabel();
			return;
		}
		JArray val = (JArray)base.Value;
		for (int i = 0; i < ((JContainer)val).Count; i++)
		{
			PropertyPath child = Path.GetChild(i.ToString());
			PropertyEditor propertyEditor = new PropertyEditor(Desktop, _reorderableList, i.ToString(), GetSchema(), child, Schema, ConfigEditor, this);
			propertyEditor.Build(val[i], filterCategory: false, isDetached: false, CachesToRebuild);
			_items.Add(propertyEditor);
		}
	}

	private void BuildEmptyLabel()
	{
		new Label(Desktop, _reorderableList)
		{
			Text = Desktop.Provider.GetText("ui.assetEditor.listEditor.empty"),
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

	public void HandleInsertItem(int index = -1)
	{
		if (index < 0)
		{
			index = _items.Count;
		}
		PropertyPath child = Path.GetChild(index.ToString());
		JToken defaultValue = SchemaParser.GetDefaultValue(ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(Schema.Value));
		JToken value = ((defaultValue != null) ? defaultValue.DeepClone() : null);
		if (ParentPropertyEditor != null && ParentPropertyEditor.IsCollapsed)
		{
			ParentPropertyEditor.SetCollapseState(uncollapsed: false);
		}
		ConfigEditor.OnChangeValue(child, value, null, CachesToRebuild?.Caches, withheldCommand: false, insertItem: true);
		ConfigEditor.Layout();
		if (_items[index].IsMounted)
		{
			_items[index].ValueEditor.Focus();
		}
	}

	public override void SetValueRecursively(JToken value)
	{
		JToken value2 = base.Value;
		base.SetValueRecursively(value);
		if (value2 != base.Value)
		{
			_reorderableList.Clear();
			_items.Clear();
			Build();
		}
	}

	public void OnItemRemoved(int index)
	{
		PropertyEditor child = _items[index];
		_items.RemoveAt(index);
		_reorderableList.Remove(child);
		UpdatePathRecursively(Path);
		if (_items.Count == 0)
		{
			BuildEmptyLabel();
		}
	}

	public void OnItemInserted(JToken value, int index)
	{
		if (_items.Count == 0)
		{
			_reorderableList.Clear();
		}
		PropertyEditor propertyEditor = new PropertyEditor(Desktop, null, index.ToString(), GetSchema(), Path.GetChild(index.ToString()), Schema, ConfigEditor, this);
		propertyEditor.Build(value, filterCategory: false, IsDetachedEditor, CachesToRebuild);
		_items.Insert(index, propertyEditor);
		UpdatePathRecursively(Path);
		_reorderableList.Add(propertyEditor, index);
		ParentPropertyEditor?.UpdateAppearance();
	}

	public void OnListElementReordered(int sourceIndex, int targetIndex)
	{
		HandleMoveItem(sourceIndex, targetIndex, reorderElement: false);
	}

	public void HandleMoveItem(int sourceIndex, int targetIndex, bool reorderElement = true)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		PropertyEditor propertyEditor = _items[sourceIndex];
		_items.RemoveAt(sourceIndex);
		_items.Insert(targetIndex, propertyEditor);
		if (reorderElement)
		{
			_reorderableList.Reorder(propertyEditor, targetIndex);
		}
		JArray val = (JArray)base.Value.DeepClone();
		JToken val2 = val[sourceIndex];
		val.RemoveAt(sourceIndex);
		val.Insert(targetIndex, val2);
		Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
		if (sourceIndex > targetIndex)
		{
			PropertyPath child = Path.GetChild(sourceIndex.ToString());
			if (ConfigEditor.State.UncollapsedProperties.TryGetValue(child, out var value))
			{
				ConfigEditor.State.UncollapsedProperties.Remove(child);
				dictionary[targetIndex] = value;
			}
			for (int i = targetIndex; i < sourceIndex; i++)
			{
				PropertyPath child2 = Path.GetChild(i.ToString());
				if (ConfigEditor.State.UncollapsedProperties.TryGetValue(child2, out var value2))
				{
					ConfigEditor.State.UncollapsedProperties.Remove(child2);
					dictionary[i + 1] = value2;
				}
			}
		}
		else
		{
			PropertyPath child3 = Path.GetChild(sourceIndex.ToString());
			if (ConfigEditor.State.UncollapsedProperties.TryGetValue(child3, out var value3))
			{
				ConfigEditor.State.UncollapsedProperties.Remove(child3);
				dictionary[targetIndex] = value3;
			}
			for (int j = sourceIndex + 1; j < targetIndex + 1; j++)
			{
				PropertyPath child4 = Path.GetChild(j.ToString());
				if (ConfigEditor.State.UncollapsedProperties.TryGetValue(child4, out var value4))
				{
					ConfigEditor.State.UncollapsedProperties.Remove(child4);
					dictionary[j - 1] = value4;
				}
			}
		}
		foreach (KeyValuePair<int, bool> item in dictionary)
		{
			ConfigEditor.State.UncollapsedProperties.Add(Path.GetChild(item.Key.ToString()), item.Value);
		}
		ConfigEditor.OnChangeValue(Path, (JToken)(object)val, base.Value, CachesToRebuild?.Caches);
		ConfigEditor.Layout();
	}

	public override void UpdatePathRecursively(PropertyPath path)
	{
		base.UpdatePathRecursively(path);
		for (int i = 0; i < _items.Count; i++)
		{
			_items[i].UpdatePathRecursively(i.ToString(), Path.GetChild(i.ToString()));
		}
	}

	public int Count()
	{
		return _items.Count;
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 2;
	}

	public void SetCollapseStateForAllItems(bool uncollapsed)
	{
		foreach (PropertyEditor item in _items)
		{
			item.SetCollapseState(uncollapsed, doDiagnosticsAndLayout: false);
		}
		ConfigEditor.SetupDiagnostics(doLayout: false);
		ConfigEditor.Layout();
	}

	public override bool TryFindPropertyEditor(PropertyPath path, out PropertyEditor propertyEditor)
	{
		string s = path.Elements[Path.Elements.Length];
		if (!int.TryParse(s, out var result) || result < 0 || result >= _items.Count)
		{
			propertyEditor = null;
			return false;
		}
		if (path.Elements.Length > Path.Elements.Length + 1)
		{
			return _items[result].ValueEditor.TryFindPropertyEditor(path, out propertyEditor);
		}
		propertyEditor = _items[result];
		return true;
	}

	public override void ClearDiagnosticsOfDescendants()
	{
		foreach (PropertyEditor item in _items)
		{
			item.ClearDiagnostics();
			item.ValueEditor.ClearDiagnosticsOfDescendants();
		}
	}
}
