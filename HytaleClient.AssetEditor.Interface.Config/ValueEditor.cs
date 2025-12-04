using System;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal abstract class ValueEditor : Element
{
	public class InvalidJsonTypeException : Exception
	{
		public InvalidJsonTypeException(string path, JToken value, SchemaNode.NodeType type)
			: base($"Invalid JSON type at '{path}', got {value.Type} for {type} editor field")
		{
		}//IL_0008: Unknown result type (might be due to invalid IL or missing references)

	}

	public class InvalidValueException : Exception
	{
		public readonly string DisplayMessage;

		public InvalidValueException(string path, SchemaNode.NodeType type, Exception ex)
			: base(ex.Message)
		{
			DisplayMessage = $"Invalid value for '{path}' with type {type}.";
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	protected readonly SchemaNode _parentSchema;

	public readonly ConfigEditor ConfigEditor;

	public readonly SchemaNode Schema;

	public readonly PropertyEditor ParentPropertyEditor;

	public PropertyPath Path;

	public bool FilterCategory;

	public bool IsDetachedEditor;

	public CacheRebuildInfo CachesToRebuild;

	public JToken Value { get; private set; }

	protected ValueEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		Schema = schema;
		_parentSchema = parentSchema;
		ConfigEditor = configEditor;
		Value = ((value != null && (int)value.Type == 10) ? null : value);
		Path = path;
		ParentPropertyEditor = parentPropertyEditor;
	}

	protected abstract void Build();

	public void BuildEditor()
	{
		Build();
	}

	public void ValidateValue()
	{
		ValidateValue(Value);
	}

	public void ValidateValue(JToken value)
	{
		if (value != null && !ValidateType(value))
		{
			throw new InvalidJsonTypeException(Path.ToString(), value, Schema.Type);
		}
	}

	public virtual void PasteValue(string text)
	{
		JToken value = JsonUtils.ParseLenient(text);
		value = SanitizeValue(value);
		try
		{
			ValidateValue(value);
		}
		catch (Exception ex)
		{
			Logger.Info(ex, "Failed to paste value because validation failed.");
			return;
		}
		try
		{
			HandleChangeValue(value, withheldCommand: false, confirmed: false, updateDisplayedValue: true);
		}
		catch (InvalidJsonTypeException ex2)
		{
			Logger.Info((Exception)ex2, "Failed to paste value because validation failed.");
			ConfigEditor.BuildPropertyEditors();
		}
		ConfigEditor.Layout();
	}

	protected virtual JToken SanitizeValue(JToken value)
	{
		return value;
	}

	protected void HandleChangeValue(JToken value, bool withheldCommand = false, bool confirmed = false, bool updateDisplayedValue = false)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		if (IsDetachedEditor)
		{
			Value = value;
			return;
		}
		PropertyEditor parentPropertyEditor = ParentPropertyEditor;
		PropertyPath parentPath;
		JObject parentValue;
		if (parentPropertyEditor != null && parentPropertyEditor.IsSchemaTypeField && !confirmed)
		{
			parentPath = Path.GetParent();
			parentValue = (JObject)ConfigEditor.GetValue(parentPath);
			if (parentValue != null && ((JContainer)parentValue).Count != 0 && (((JContainer)parentValue).Count > 1 || !parentValue.ContainsKey(_parentSchema.TypePropertyKey)))
			{
				ConfigEditor.AssetEditorOverlay.ConfirmationModal.Open(Desktop.Provider.GetText("ui.assetEditor.changeConfirmationModal.title"), Desktop.Provider.GetText("ui.assetEditor.changeConfirmationModal.text"), Confirm, UpdateDisplayedValue);
			}
			else
			{
				Confirm();
			}
		}
		else
		{
			ConfigEditor configEditor = ConfigEditor;
			PropertyPath path = Path;
			JToken value2 = value;
			JToken value3 = Value;
			configEditor.OnChangeValue(path, value2, (value3 != null) ? value3.DeepClone() : null, CachesToRebuild?.Caches, withheldCommand, insertItem: false, updateDisplayedValue);
		}
		void Confirm()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Expected O, but got Unknown
			JObject val = new JObject();
			val.Add(_parentSchema.TypePropertyKey, JToken.op_Implicit((string)value));
			JObject value4 = val;
			ConfigEditor configEditor2 = ConfigEditor;
			PropertyPath path2 = parentPath;
			JObject obj = parentValue;
			configEditor2.OnChangeValue(path2, (JToken)(object)value4, (obj != null) ? ((JToken)obj).DeepClone() : null, CachesToRebuild?.Caches);
			ConfigEditor.Layout();
			PropertyEditor parentPropertyEditor2 = ParentPropertyEditor;
			if (parentPropertyEditor2 != null && (parentPropertyEditor2.ParentValueEditor?.FilterCategory).GetValueOrDefault())
			{
				ConfigEditor.UpdateCategories();
			}
		}
	}

	protected void SubmitUpdateCommand()
	{
		ConfigEditor.SubmitPendingUpdateCommands();
	}

	public virtual void Focus()
	{
	}

	protected internal virtual void UpdateDisplayedValue()
	{
	}

	public virtual void SetValue(JToken value)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		if (value != null && (int)value.Type == 10)
		{
			value = null;
		}
		Value = value;
		ParentPropertyEditor?.UpdateAppearance();
	}

	public virtual void SetValueRecursively(JToken value)
	{
		SetValue(value);
	}

	public virtual void UpdatePathRecursively(PropertyPath path)
	{
		Path = path;
	}

	protected virtual bool ValidateType(JToken JToken)
	{
		return true;
	}

	protected virtual bool IsValueEmptyOrDefault(JToken value)
	{
		return false;
	}

	public virtual bool TryFindPropertyEditor(PropertyPath path, out PropertyEditor propertyEditor)
	{
		propertyEditor = null;
		return false;
	}

	public virtual void ClearDiagnosticsOfDescendants()
	{
	}

	public static ValueEditor CreateFromSchema(Element parent, SchemaNode schemaNode, PropertyPath path, PropertyEditor parentProperty, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
	{
		return schemaNode.Type switch
		{
			SchemaNode.NodeType.Text => new TextEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Number => new NumberEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Dropdown => new DropdownEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Color => new ColorEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Object => new ObjectEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.List => new ListEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Map => new MapEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.AssetIdDropdown => new AssetDropdownEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Checkbox => new CheckBoxEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.AssetFileDropdown => new AssetFileSelectorEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.ItemIcon => new IconEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.AssetReferenceOrInline => new AssetReferenceOrInlineEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Source => new JsonEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.Timeline => new TimelineEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			SchemaNode.NodeType.WeightedTimeline => new WeightedTimelineEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
			_ => new ReadOnlyEditor(parent.Desktop, parent, schemaNode, path, parentProperty, parentSchema, configEditor, value), 
		};
	}
}
