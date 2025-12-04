#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Data;

internal class SchemaParser
{
	private const string TypeNumber = "number";

	private const string TypeInt = "integer";

	private const string TypeString = "string";

	private const string TypeNull = "null";

	private const string TypeObject = "object";

	private const string TypeArray = "array";

	private const string TypeBoolean = "boolean";

	private const string CustomTypeColor = "Color";

	private const string CustomTypeColorAlpha = "ColorAlpha";

	private const string CustomTypeColorShort = "ColorShort";

	private readonly Stack<string> _keyStack = new Stack<string>();

	private readonly bool _collectKeyStack;

	public string CurrentPath
	{
		get
		{
			string text = "";
			while (_keyStack.Count > 0)
			{
				string text2 = _keyStack.Pop();
				if (text != "")
				{
					text2 += ".";
				}
				text = text2 + text;
			}
			return text;
		}
	}

	public SchemaParser(bool collectKeyStack)
	{
		_collectKeyStack = collectKeyStack;
	}

	public SchemaNode Parse(JObject json, Dictionary<string, SchemaNode> definitions)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		SchemaNode schemaNode = new SchemaNode();
		if (json.ContainsKey("$id"))
		{
			schemaNode.Id = (string)json["$id"];
		}
		if (json.ContainsKey("definitions"))
		{
			foreach (KeyValuePair<string, JToken> item in (JObject)json["definitions"])
			{
				if (_collectKeyStack)
				{
					PushKey("[Definition=" + item.Key + "]");
				}
				SchemaNode schemaNode2 = ParseNode((JObject)item.Value, definitions);
				if (_collectKeyStack)
				{
					PopKey();
				}
				definitions.Add("#/definitions/" + item.Key, schemaNode2);
				if (schemaNode2.Id != null)
				{
					definitions.Add("#" + schemaNode2.Id, schemaNode2);
				}
			}
		}
		SetupMeta(schemaNode, json);
		TrySetupSchema(schemaNode, json, definitions);
		Debug.Assert(_keyStack.Count == 0);
		return schemaNode;
	}

	private SchemaNode ParseNode(JObject json, Dictionary<string, SchemaNode> definitions)
	{
		SchemaNode schemaNode = new SchemaNode();
		SetupMeta(schemaNode, json);
		TrySetupSchema(schemaNode, json, definitions);
		return schemaNode;
	}

	private bool TrySetupSchema(SchemaNode node, JObject json, Dictionary<string, SchemaNode> definitions)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Invalid comparison between Unknown and I4
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Expected O, but got Unknown
		if (json.ContainsKey("$ref"))
		{
			node.SchemaReference = (string)json["$ref"];
			return true;
		}
		if (json.ContainsKey("const"))
		{
			node.Const = json["const"];
			return true;
		}
		JObject val = (JObject)json["hytale"];
		if (val != null && val.ContainsKey("type"))
		{
			switch ((string)val["type"])
			{
			case "Color":
				node.Type = SchemaNode.NodeType.Color;
				node.ColorFormat = ColorPicker.ColorFormat.Rgb;
				return true;
			case "ColorAlpha":
				node.Type = SchemaNode.NodeType.Color;
				node.ColorFormat = ColorPicker.ColorFormat.Rgba;
				return true;
			case "ColorShort":
				node.Type = SchemaNode.NodeType.Color;
				node.ColorFormat = ColorPicker.ColorFormat.RgbShort;
				return true;
			}
		}
		if (json.ContainsKey("anyOf"))
		{
			if (json.ContainsKey("hytaleAssetRef"))
			{
				SetupAssetReferenceOrInline(node, json, definitions);
				return true;
			}
			if (json.ContainsKey("hytaleSchemaTypeField"))
			{
				SetupSchemaTypeField(node, json, definitions);
				return true;
			}
			return TrySetupFromAnyOf(node, (JArray)json["anyOf"], definitions);
		}
		if (json.ContainsKey("type"))
		{
			if ((int)json["type"].Type == 2)
			{
				foreach (JToken item in (JArray)json["type"])
				{
					string text = (string)item;
					if (text == "null" || !TrySetupNodeFromType(node, text, json, definitions))
					{
						continue;
					}
					return true;
				}
			}
			else if (TrySetupNodeFromType(node, (string)json["type"], json, definitions))
			{
				return true;
			}
		}
		return false;
	}

	private void SetupMeta(SchemaNode node, JObject json)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Expected O, but got Unknown
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Expected O, but got Unknown
		if (json.ContainsKey("default"))
		{
			node.DefaultValue = json["default"];
		}
		if (json.ContainsKey("description"))
		{
			node.Description = (string)json["description"];
		}
		JObject val = (JObject)json["hytale"];
		if (val == null)
		{
			return;
		}
		if (val.ContainsKey("uiPropertyTitle"))
		{
			node.Title = (string)val["uiPropertyTitle"];
		}
		if (val.ContainsKey("uiSectionStart"))
		{
			node.SectionStart = (string)val["uiSectionStart"];
		}
		if (val.ContainsKey("uiCollapsedByDefault"))
		{
			node.IsCollapsedByDefault = (bool)val["uiCollapsedByDefault"];
		}
		if (val.ContainsKey("inheritsProperty"))
		{
			node.InheritsProperty = (bool)val["inheritsProperty"];
		}
		if (val.ContainsKey("mergesProperties"))
		{
			node.MergesProperties = (bool)val["mergesProperties"];
		}
		if (val.ContainsKey("allowEmptyObject"))
		{
			node.AllowEmptyObject = (bool)val["allowEmptyObject"];
		}
		if (val.ContainsKey("uiDisplayMode"))
		{
			string text = (string)val["uiDisplayMode"];
			string text2 = text;
			if (!(text2 == "Hidden"))
			{
				if (text2 == "Compact")
				{
					node.DisplayCompact = true;
				}
			}
			else
			{
				node.IsHidden = true;
			}
		}
		if (!val.ContainsKey("uiRebuildCaches"))
		{
			return;
		}
		JArray val2 = (JArray)val["uiRebuildCaches"];
		node.RebuildCaches = new AssetEditorRebuildCaches();
		foreach (JToken item in val2)
		{
			switch ((string)item)
			{
			case "BlockTextures":
				node.RebuildCaches.BlockTextures = true;
				break;
			case "Models":
				node.RebuildCaches.Models = true;
				break;
			case "ModelTextures":
				node.RebuildCaches.ModelTextures = true;
				break;
			case "MapGeometry":
				node.RebuildCaches.MapGeometry = true;
				break;
			case "ItemIcons":
				node.RebuildCaches.ItemIcons = true;
				break;
			}
		}
		node.RebuildCachesForChildProperties = (bool)val["uiRebuildCachesForChildProperties"];
	}

	private bool TrySetupNodeFromType(SchemaNode node, string type, JObject json, Dictionary<string, SchemaNode> definitions)
	{
		switch (type)
		{
		case "string":
			SetupString(node, json);
			return true;
		case "integer":
		case "number":
			SetupNumber(node, json, type);
			return true;
		case "boolean":
			node.Type = SchemaNode.NodeType.Checkbox;
			node.DefaultValue = JToken.op_Implicit(false);
			return true;
		case "object":
			SetupObject(node, json, definitions);
			return true;
		case "array":
			SetupArray(node, json, definitions);
			return true;
		default:
			return false;
		}
	}

	private bool TrySetupFromAnyOf(SchemaNode node, JArray anyOf, Dictionary<string, SchemaNode> definitions)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		for (int i = 0; i < ((JContainer)anyOf).Count; i++)
		{
			JToken val = anyOf[i];
			JObject json = (JObject)val;
			if (_collectKeyStack)
			{
				PushKey("[AnyOf=" + i + "]");
			}
			if (TrySetupSchema(node, json, definitions))
			{
				if (_collectKeyStack)
				{
					PopKey();
				}
				SetupMeta(node, json);
				return true;
			}
			if (_collectKeyStack)
			{
				PopKey();
			}
		}
		return false;
	}

	private void SetupNumber(SchemaNode node, JObject json, string type)
	{
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected O, but got Unknown
		node.Type = SchemaNode.NodeType.Number;
		node.MaxDecimalPlaces = ((type == "number") ? 3 : 0);
		double num = ((node.MaxDecimalPlaces == 0) ? 1.0 : 0.001);
		if (HasDecimal("minimum"))
		{
			node.Min = (double)json["minimum"];
		}
		if (HasDecimal("exclusiveMinimum"))
		{
			node.Min = (double)json["exclusiveMinimum"] + num;
		}
		if (HasDecimal("maximum"))
		{
			node.Max = (double)json["maximum"];
		}
		if (HasDecimal("exclusiveMaximum"))
		{
			node.Max = (double)json["exclusiveMaximum"] - num;
		}
		JToken obj = json["hytale"];
		JObject val = (JObject)((obj != null) ? obj[(object)"uiEditorComponent"] : null);
		if (val != null && (string)val["component"] == SchemaNode.NodeType.Number.ToString())
		{
			if (val.ContainsKey("maxDecimalPlaces"))
			{
				node.MaxDecimalPlaces = (int)val["maxDecimalPlaces"];
			}
			if (val.ContainsKey("suffix"))
			{
				node.Suffix = (string)val["suffix"];
			}
			if (val.ContainsKey("step"))
			{
				node.Step = (double)val["step"];
			}
		}
		bool HasDecimal(string key)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Invalid comparison between Unknown and I4
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Invalid comparison between Unknown and I4
			JToken obj2 = json[key];
			int result;
			if (obj2 == null || (int)obj2.Type != 6)
			{
				JToken obj3 = json[key];
				result = ((obj3 != null && (int)obj3.Type == 7) ? 1 : 0);
			}
			else
			{
				result = 1;
			}
			return (byte)result != 0;
		}
	}

	private void SetupString(SchemaNode node, JObject obj)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Expected O, but got Unknown
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Expected O, but got Unknown
		JToken obj2 = obj["hytale"];
		JObject val = (JObject)((obj2 != null) ? obj2[(object)"uiEditorComponent"] : null);
		if (obj.ContainsKey("oneOf"))
		{
			node.Type = SchemaNode.NodeType.Dropdown;
			JArray val2 = (JArray)obj["oneOf"];
			node.Enum = new string[((JContainer)val2).Count];
			node.EnumTitles = new string[((JContainer)val2).Count];
			node.EnumDescriptions = new string[((JContainer)val2).Count];
			for (int i = 0; i < node.Enum.Length; i++)
			{
				JObject val3 = (JObject)val2[i];
				node.Enum[i] = (string)val3["const"];
				if (val3.ContainsKey("title"))
				{
					node.EnumTitles[i] = (string)val3["title"];
				}
				if (val3.ContainsKey("description"))
				{
					node.EnumDescriptions[i] = (string)val3["description"];
				}
			}
		}
		else if (obj.ContainsKey("enum"))
		{
			node.Type = SchemaNode.NodeType.Dropdown;
			node.Enum = ((IEnumerable<JToken>)(JArray)obj["enum"]).Select((JToken a) => ((object)a).ToString()).ToArray();
			node.EnumTitles = new string[node.Enum.Length];
			node.EnumDescriptions = new string[node.Enum.Length];
		}
		else if (val != null && (string)val["component"] == SchemaNode.NodeType.Dropdown.ToString())
		{
			node.Type = SchemaNode.NodeType.Dropdown;
			node.DataSet = (string)val["dataSet"];
		}
		else if (val != null && (string)val["component"] == SchemaNode.NodeType.Text.ToString())
		{
			node.Type = SchemaNode.NodeType.Text;
			node.DataSet = (string)val["dataSet"];
		}
		else if (obj.ContainsKey("hytaleAssetRef"))
		{
			node.Type = SchemaNode.NodeType.AssetIdDropdown;
			node.AssetType = (string)obj["hytaleAssetRef"];
		}
		else if (obj.ContainsKey("hytaleParent"))
		{
			node.Type = SchemaNode.NodeType.AssetIdDropdown;
			node.AssetType = (string)obj["hytaleParent"][(object)"type"];
			node.IsParentProperty = true;
		}
		else if (obj.ContainsKey("hytaleCommonAsset"))
		{
			node.Type = SchemaNode.NodeType.AssetFileDropdown;
			JObject val4 = (JObject)obj["hytaleCommonAsset"];
			if (val4.ContainsKey("requiredRoots"))
			{
				JArray val5 = (JArray)val4["requiredRoots"];
				node.AllowedDirectories = new string[((JContainer)val5).Count];
				for (int j = 0; j < ((JContainer)val5).Count; j++)
				{
					string text = (string)val5[j];
					if (!text.StartsWith("/"))
					{
						text = "/" + text;
					}
					if (!text.EndsWith("/"))
					{
						text += "/";
					}
					node.AllowedDirectories[j] = text;
				}
			}
			if (val4.ContainsKey("requiredExtension"))
			{
				node.AllowedFileExtensions = new string[1] { (string)val4["requiredExtension"] };
			}
			if (val4.ContainsKey("isUIAsset"))
			{
				node.IsUITexture = (bool)val4["isUIAsset"];
			}
			if (val != null && (string)val["component"] == SchemaNode.NodeType.ItemIcon.ToString())
			{
				node.Type = SchemaNode.NodeType.ItemIcon;
			}
		}
		else
		{
			node.Type = SchemaNode.NodeType.Text;
		}
		if (obj.ContainsKey("maxLength"))
		{
			node.MaxLength = (int)obj["maxLength"];
		}
	}

	private void SetupArray(SchemaNode node, JObject obj, Dictionary<string, SchemaNode> definitions)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		if ((int)obj["items"].Type == 2)
		{
			node.Value = ParseNode((JObject)obj["items"][(object)0], definitions);
		}
		else
		{
			node.Value = ParseNode((JObject)obj["items"], definitions);
		}
		node.Type = SchemaNode.NodeType.List;
		if (node.Value.DefaultValue == null)
		{
			node.Value.DefaultValue = GetDefaultValue(node.Value);
		}
		JToken obj2 = obj["hytale"];
		JObject val = (JObject)((obj2 != null) ? obj2[(object)"uiEditorComponent"] : null);
		if (val != null && (string)val["component"] == SchemaNode.NodeType.Timeline.ToString())
		{
			node.Type = SchemaNode.NodeType.Timeline;
		}
	}

	private void SetupObject(SchemaNode node, JObject obj, Dictionary<string, SchemaNode> definitions)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Expected O, but got Unknown
		if (obj.ContainsKey("additionalProperties") && (int)obj["additionalProperties"].Type == 1)
		{
			node.Type = SchemaNode.NodeType.Map;
			node.Value = ParseNode((JObject)obj["additionalProperties"], definitions);
			if (node.Value.DefaultValue == null)
			{
				node.Value.DefaultValue = GetDefaultValue(node.Value);
			}
			if (obj.ContainsKey("propertyNames"))
			{
				JObject val = (JObject)obj["propertyNames"];
				val["type"] = JToken.op_Implicit("string");
				node.Key = ParseNode(val, definitions);
			}
			else
			{
				node.Key = new SchemaNode
				{
					Type = SchemaNode.NodeType.Text
				};
			}
			JToken obj2 = obj["hytale"];
			JObject val2 = (JObject)((obj2 != null) ? obj2[(object)"uiEditorComponent"] : null);
			if (val2 != null && (string)val2["component"] == SchemaNode.NodeType.WeightedTimeline.ToString())
			{
				node.Type = SchemaNode.NodeType.WeightedTimeline;
			}
			return;
		}
		if ((!obj.ContainsKey("additionalProperties") || !(bool)obj["additionalProperties"]) && !obj.ContainsKey("properties"))
		{
			node.Type = SchemaNode.NodeType.Source;
			return;
		}
		node.Type = SchemaNode.NodeType.Object;
		node.Properties = new Dictionary<string, SchemaNode>();
		if (!obj.ContainsKey("properties"))
		{
			return;
		}
		foreach (KeyValuePair<string, JToken> item in Extensions.Value<JObject>((IEnumerable<JToken>)obj["properties"]))
		{
			JObject json = (JObject)item.Value;
			if (_collectKeyStack)
			{
				PushKey("[Prop=" + item.Key + "]");
			}
			SchemaNode value = ParseNode(json, definitions);
			if (_collectKeyStack)
			{
				PopKey();
			}
			node.Properties.Add(item.Key, value);
		}
	}

	private void SetupAssetReferenceOrInline(SchemaNode node, JObject obj, Dictionary<string, SchemaNode> definitions)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		node.AssetType = (string)obj["hytaleAssetRef"];
		node.Type = SchemaNode.NodeType.AssetReferenceOrInline;
		foreach (JToken item in (IEnumerable<JToken>)obj["anyOf"])
		{
			if ((string)item[(object)"type"] == "string")
			{
				continue;
			}
			node.Value = ParseNode((JObject)item, definitions);
			break;
		}
	}

	private void SetupSchemaTypeField(SchemaNode node, JObject obj, Dictionary<string, SchemaNode> definitions)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected O, but got Unknown
		node.Type = SchemaNode.NodeType.Object;
		JObject val = (JObject)obj["hytaleSchemaTypeField"];
		JArray val2 = (JArray)val["values"];
		JArray val3 = (JArray)obj["anyOf"];
		node.TypePropertyKey = (string)val["property"];
		node.Value = new SchemaNode
		{
			Type = SchemaNode.NodeType.Dropdown,
			Enum = new string[((JContainer)val3).Count],
			EnumTitles = new string[((JContainer)val3).Count],
			EnumDescriptions = new string[((JContainer)val3).Count]
		};
		node.TypeSchemas = new SchemaNode[((JContainer)val3).Count];
		for (int i = 0; i < ((JContainer)val3).Count; i++)
		{
			if (_collectKeyStack)
			{
				PushKey("[Type=" + i + "]");
			}
			node.TypeSchemas[i] = ParseNode((JObject)val3[i], definitions);
			if (_collectKeyStack)
			{
				PopKey();
			}
			node.Value.Enum[i] = (string)val2[i];
		}
		if (val.ContainsKey("hasParentProperty"))
		{
			node.HasParentProperty = (bool)val["hasParentProperty"];
		}
		if (val.ContainsKey("defaultValue"))
		{
			node.DefaultTypeSchema = (string)val["defaultValue"];
		}
	}

	private void PushKey(string key)
	{
		_keyStack.Push(key);
	}

	private void PopKey()
	{
		_keyStack.Pop();
	}

	public static JToken GetDefaultValue(SchemaNode node)
	{
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		if (node.DefaultValue != null)
		{
			return node.DefaultValue;
		}
		switch (node.Type)
		{
		case SchemaNode.NodeType.Number:
			return JToken.op_Implicit(0);
		case SchemaNode.NodeType.Dropdown:
			if (node.Enum == null || node.Enum.Length == 0)
			{
				return JToken.op_Implicit("");
			}
			return JToken.op_Implicit(node.Enum[0]);
		case SchemaNode.NodeType.Color:
			return (JToken)(node.ColorFormat switch
			{
				ColorPicker.ColorFormat.Rgba => JToken.op_Implicit("rgba(#ffffff, 1)"), 
				ColorPicker.ColorFormat.RgbShort => JToken.op_Implicit("#fffff"), 
				_ => JToken.op_Implicit("#ffffff"), 
			});
		case SchemaNode.NodeType.Text:
			return JToken.op_Implicit("");
		case SchemaNode.NodeType.Timeline:
		case SchemaNode.NodeType.List:
			return (JToken)new JArray();
		case SchemaNode.NodeType.WeightedTimeline:
		case SchemaNode.NodeType.Map:
		case SchemaNode.NodeType.Object:
		case SchemaNode.NodeType.Source:
			return (JToken)new JObject();
		case SchemaNode.NodeType.Checkbox:
			return JToken.op_Implicit(false);
		default:
			return null;
		}
	}
}
