using System.Collections.Generic;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Data;

public class SchemaNode
{
	public enum NodeType
	{
		ReadOnly,
		Dropdown,
		AssetIdDropdown,
		AssetFileDropdown,
		AssetReferenceOrInline,
		Text,
		Number,
		ItemIcon,
		Checkbox,
		Color,
		Timeline,
		WeightedTimeline,
		List,
		Map,
		Object,
		Source
	}

	public string Id;

	public NodeType Type;

	public string Title;

	public string Description;

	public JToken DefaultValue;

	public JToken Const;

	public string SchemaReference;

	public bool IsHidden;

	public string SectionStart;

	public bool InheritsProperty;

	public bool IsParentProperty;

	public AssetEditorRebuildCaches RebuildCaches;

	public bool RebuildCachesForChildProperties;

	public Dictionary<string, SchemaNode> Properties;

	public bool MergesProperties;

	public SchemaNode Value;

	public SchemaNode Key;

	public bool IsCollapsedByDefault = true;

	public bool DisplayCompact;

	public bool AllowEmptyObject;

	public string TypePropertyKey;

	public SchemaNode[] TypeSchemas;

	public bool HasParentProperty;

	public string DefaultTypeSchema;

	public string[] Enum;

	public string[] EnumTitles;

	public string[] EnumDescriptions;

	public string DataSet;

	public int MaxLength;

	public double? Step;

	public double? Min;

	public double? Max;

	public string Suffix;

	public int MaxDecimalPlaces;

	public string AssetType;

	public string[] AllowedFileExtensions;

	public string[] AllowedDirectories;

	public bool IsUITexture;

	public ColorPicker.ColorFormat ColorFormat;

	private Dictionary<string, SchemaNode> DeepCloneProperties()
	{
		Dictionary<string, SchemaNode> dictionary = new Dictionary<string, SchemaNode>();
		foreach (KeyValuePair<string, SchemaNode> property in Properties)
		{
			dictionary.Add(property.Key, property.Value.Clone());
		}
		return dictionary;
	}

	public SchemaNode Clone(bool deep = false)
	{
		return new SchemaNode
		{
			Id = Id,
			Type = Type,
			Const = Const,
			Title = Title,
			Description = Description,
			DefaultValue = DefaultValue,
			SchemaReference = SchemaReference,
			IsHidden = IsHidden,
			SectionStart = SectionStart,
			InheritsProperty = InheritsProperty,
			IsParentProperty = IsParentProperty,
			RebuildCaches = RebuildCaches,
			RebuildCachesForChildProperties = RebuildCachesForChildProperties,
			Properties = ((Properties == null) ? null : (deep ? DeepCloneProperties() : new Dictionary<string, SchemaNode>(Properties))),
			MergesProperties = MergesProperties,
			Value = ((!deep) ? Value : Value?.Clone()),
			Key = ((!deep) ? Key : Key?.Clone()),
			IsCollapsedByDefault = IsCollapsedByDefault,
			DisplayCompact = DisplayCompact,
			AllowEmptyObject = AllowEmptyObject,
			TypePropertyKey = TypePropertyKey,
			TypeSchemas = ((TypeSchemas == null) ? null : (deep ? DeepCloneSchemaArray(TypeSchemas) : TypeSchemas)),
			HasParentProperty = HasParentProperty,
			Enum = Enum,
			EnumTitles = EnumTitles,
			EnumDescriptions = EnumDescriptions,
			DataSet = DataSet,
			MaxLength = MaxLength,
			Step = Step,
			Min = Min,
			Max = Max,
			Suffix = Suffix,
			MaxDecimalPlaces = MaxDecimalPlaces,
			AssetType = AssetType,
			AllowedDirectories = AllowedDirectories,
			AllowedFileExtensions = AllowedFileExtensions,
			ColorFormat = ColorFormat
		};
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}, {6}: {7}, {8}: {9}, {10}: {11}, {12}: {13}, {14}: {15}, {16}: {17}, {18}: {19}, {20}: {21}, {22}: {23}, {24}: {25}, {26}: {27}, {28}: {29}, {30}: {31}, {32}: {33}, {34}: {35}, {36}: {37}, {38}: {39}, {40}: {41}, {42}: {43}, {44}: {45}, {46}: {47}, {48}: {49}, {50}: {51}, {52}: {53}, {54}: {55}, {56}: {57}, {58}: {59}, {60}: {61}", "Id", Id, "Type", Type, "Const", Const, "Title", Title, "Description", Description, "DefaultValue", DefaultValue, "SchemaReference", SchemaReference, "IsHidden", IsHidden, "Properties", Properties, "Value", Value, "Key", Key, "IsCollapsedByDefault", IsCollapsedByDefault, "DisplayCompact", DisplayCompact, "AllowEmptyObject", AllowEmptyObject, "TypePropertyKey", TypePropertyKey, "TypeSchemas", TypeSchemas, "HasParentProperty", HasParentProperty, "Enum", Enum, "EnumTitles", EnumTitles, "EnumDescriptions", EnumDescriptions, "DataSet", DataSet, "MaxLength", MaxLength, "Step", Step, "Min", Min, "Max", Max, "Suffix", Suffix, "MaxDecimalPlaces", MaxDecimalPlaces, "AssetType", AssetType, "AllowedFileExtensions", AllowedFileExtensions, "AllowedDirectories", AllowedDirectories, "ColorFormat", ColorFormat);
	}

	private static SchemaNode[] DeepCloneSchemaArray(SchemaNode[] nodes)
	{
		SchemaNode[] array = new SchemaNode[nodes.Length];
		for (int i = 0; i < nodes.Length; i++)
		{
			array[i] = nodes[i].Clone();
		}
		return array;
	}
}
