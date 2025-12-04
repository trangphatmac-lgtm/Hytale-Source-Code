using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class ObjectEditor : ValueEditor
{
	protected readonly IDictionary<string, PropertyEditor> _properties = new Dictionary<string, PropertyEditor>();

	public ObjectEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
		_layoutMode = (Schema.DisplayCompact ? LayoutMode.Left : LayoutMode.Top);
	}

	protected override void Build()
	{
		Build(Schema);
	}

	protected void Build(SchemaNode schema)
	{
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Invalid comparison between Unknown and I4
		//IL_04dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Invalid comparison between Unknown and I4
		string text = ((!IsDetachedEditor) ? ConfigEditor.SearchQuery : "");
		if (schema.Properties != null)
		{
			bool flag = false;
			foreach (KeyValuePair<string, SchemaNode> property in schema.Properties)
			{
				if (property.Value.IsHidden)
				{
					continue;
				}
				PropertyPath child = Path.GetChild(property.Key);
				bool filterCategory = false;
				if (FilterCategory && text == "")
				{
					if (!flag)
					{
						if (property.Value.SectionStart != null && ConfigEditor.State.ActiveCategory.HasValue && ConfigEditor.State.ActiveCategory.Equals(child))
						{
							flag = true;
						}
						else
						{
							if (!ConfigEditor.State.ActiveCategory.Value.IsDescendantOf(child))
							{
								continue;
							}
							filterCategory = true;
						}
					}
					else if (property.Value.SectionStart != null)
					{
						break;
					}
				}
				string text2 = property.Value.Title ?? JsonUtils.GetTitleFromKey(property.Key);
				if (text != "" && !property.Key.ToLower().Contains(text) && (property.Value.Title == null || !property.Value.Title.ToLower().Contains(text)) && !text2.ToLower().Contains(text))
				{
					continue;
				}
				JToken value = base.Value;
				JToken val = ((value != null) ? value[(object)property.Key] : null);
				if (ConfigEditor.DisplayUnsetProperties || (val != null && (int)val.Type != 10))
				{
					SchemaNode schemaNode = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(property.Value);
					PropertyEditor propertyEditor2 = (_properties[property.Key] = new PropertyEditor(Desktop, this, property.Key, schemaNode, child, Schema, ConfigEditor, this, text2));
					PropertyEditor propertyEditor3 = propertyEditor2;
					CacheRebuildInfo cacheRebuildInfo = null;
					if (schemaNode.RebuildCaches != null)
					{
						cacheRebuildInfo = new CacheRebuildInfo(schemaNode.RebuildCaches, schemaNode.RebuildCachesForChildProperties);
					}
					else if (CachesToRebuild != null && CachesToRebuild.AppliesToChildProperties)
					{
						cacheRebuildInfo = CachesToRebuild;
					}
					propertyEditor3.Build(val, filterCategory, IsDetachedEditor, cacheRebuildInfo);
				}
			}
		}
		if (schema.TypePropertyKey == null)
		{
			return;
		}
		JToken value2 = base.Value;
		JToken value3 = ((value2 != null) ? value2[(object)schema.TypePropertyKey] : null);
		SchemaNode schemaNode2 = schema;
		if (ConfigEditor.AssetEditorOverlay.TryResolveTypeSchemaInCurrentContext(base.Value, ref schemaNode2))
		{
			bool flag2 = false;
			{
				foreach (KeyValuePair<string, SchemaNode> property2 in schemaNode2.Properties)
				{
					if (property2.Value.IsHidden)
					{
						continue;
					}
					PropertyPath child2 = Path.GetChild(property2.Key);
					bool filterCategory2 = false;
					if (FilterCategory && text == "")
					{
						if (!flag2)
						{
							if (property2.Value.SectionStart != null && ConfigEditor.State.ActiveCategory.Value.Equals(child2))
							{
								flag2 = true;
							}
							else
							{
								if (!ConfigEditor.State.ActiveCategory.Value.IsDescendantOf(child2))
								{
									continue;
								}
								filterCategory2 = true;
							}
						}
						else if (property2.Value.SectionStart != null)
						{
							break;
						}
					}
					if (text != "" && !property2.Key.ToLower().Contains(text) && (property2.Value.Title == null || !property2.Value.Title.ToLower().Contains(text)))
					{
						continue;
					}
					JToken value4 = base.Value;
					JToken val2 = ((value4 != null) ? value4[(object)property2.Key] : null);
					if (ConfigEditor.DisplayUnsetProperties || (val2 != null && (int)val2.Type != 10))
					{
						SchemaNode schema2 = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext((property2.Key == schema.TypePropertyKey) ? schema.Value : property2.Value);
						PropertyEditor propertyEditor2 = (_properties[property2.Key] = new PropertyEditor(Desktop, this, property2.Key, schema2, child2, schema, ConfigEditor, this, property2.Value.Title ?? JsonUtils.GetTitleFromKey(property2.Key)));
						PropertyEditor propertyEditor5 = propertyEditor2;
						propertyEditor5.IsSchemaTypeField = property2.Key == schema.TypePropertyKey;
						CacheRebuildInfo cacheRebuildInfo2 = null;
						if (schema.RebuildCaches != null)
						{
							cacheRebuildInfo2 = new CacheRebuildInfo(schema.RebuildCaches, schema.RebuildCachesForChildProperties);
						}
						else if (CachesToRebuild != null && CachesToRebuild.AppliesToChildProperties)
						{
							cacheRebuildInfo2 = CachesToRebuild;
						}
						propertyEditor5.Build(val2, filterCategory2, IsDetachedEditor, cacheRebuildInfo2);
					}
				}
				return;
			}
		}
		if (schema.TypeSchemas.Length != 0)
		{
			SchemaNode schema3 = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(schema.Value);
			PropertyPath child3 = Path.GetChild(schema.TypePropertyKey);
			PropertyEditor propertyEditor2 = (_properties[schema.TypePropertyKey] = new PropertyEditor(Desktop, this, schema.TypePropertyKey, schema3, child3, schema, ConfigEditor, this, schema.Value.Title));
			PropertyEditor propertyEditor7 = propertyEditor2;
			propertyEditor7.IsSchemaTypeField = true;
			propertyEditor7.Build(value3, filterCategory: false, IsDetachedEditor);
			if (schema.HasParentProperty)
			{
				SchemaNode schemaNode3 = ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(schema.TypeSchemas[0]);
				SchemaNode schemaNode4 = schemaNode3.Properties["Parent"];
				PropertyPath child4 = Path.GetChild("Parent");
				propertyEditor2 = (_properties["Parent"] = new PropertyEditor(Desktop, this, "Parent", ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(schemaNode4), child4, schema, ConfigEditor, this, schemaNode4.Title));
				PropertyEditor propertyEditor9 = propertyEditor2;
				JToken value5 = base.Value;
				propertyEditor9.Build((value5 != null) ? value5[(object)"Parent"] : null, filterCategory: false, IsDetachedEditor);
			}
		}
	}

	public override void SetValueRecursively(JToken value)
	{
		base.SetValueRecursively(value);
		if (base.Value == null)
		{
			foreach (PropertyEditor value2 in _properties.Values)
			{
				value2.ValueEditor.SetValueRecursively(null);
				value2.ValueEditor.UpdateDisplayedValue();
			}
			return;
		}
		Clear();
		_properties.Clear();
		Build();
	}

	public override void UpdatePathRecursively(PropertyPath path)
	{
		base.UpdatePathRecursively(path);
		foreach (KeyValuePair<string, PropertyEditor> property in _properties)
		{
			property.Value.UpdatePathRecursively(property.Key, path.GetChild(property.Key));
		}
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 1;
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
