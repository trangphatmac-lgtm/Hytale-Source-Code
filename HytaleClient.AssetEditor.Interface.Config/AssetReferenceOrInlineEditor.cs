#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Modals;
using HytaleClient.Graphics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class AssetReferenceOrInlineEditor : ObjectEditor
{
	private PropertyEditor _referenceEditor;

	public AssetReferenceOrInlineEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
		_layoutMode = ((value == null) ? LayoutMode.Left : LayoutMode.Top);
	}

	protected override void Build()
	{
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Invalid comparison between Unknown and I4
		_referenceEditor = null;
		Group group;
		if (base.Value == null)
		{
			_layoutMode = LayoutMode.Left;
			new Group(Desktop, this).Anchor = new Anchor
			{
				Width = 250
			};
			new Group(Desktop, this)
			{
				Anchor = new Anchor
				{
					Width = 1
				},
				Background = new PatchStyle(PropertyEditor.BorderColor)
			};
			group = new Group(Desktop, this)
			{
				FlexWeight = 1,
				LayoutMode = LayoutMode.Left
			};
			CreateButton("ui.assetEditor.assetReferenceOrInlineEditor.useExisting", OnUseExisting);
			CreateButton("ui.assetEditor.assetReferenceOrInlineEditor.createEmbedded", OnCreateEmbedded);
		}
		else if ((int)base.Value.Type == 8)
		{
			_layoutMode = LayoutMode.Full;
			string value = Schema.AssetType;
			if (ConfigEditor.AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(Schema.AssetType, out var value2))
			{
				value = value2.Name;
			}
			SchemaNode schema = new SchemaNode
			{
				Type = SchemaNode.NodeType.AssetIdDropdown,
				AssetType = Schema.AssetType
			};
			string text = Desktop.Provider.GetText("ui.assetEditor.assetReferenceOrInlineEditor.reference", new Dictionary<string, string> { { "assetType", value } });
			_referenceEditor = new PropertyEditor(Desktop, this, null, schema, Path, Schema, ConfigEditor, this, text);
			_referenceEditor.Build(base.Value, filterCategory: false, IsDetachedEditor);
		}
		else
		{
			_layoutMode = LayoutMode.Top;
			Build(ConfigEditor.AssetEditorOverlay.ResolveSchemaInCurrentContext(Schema.Value));
		}
		void CreateButton(string messageId, Action action)
		{
			new TextButton(Desktop, group)
			{
				Text = Desktop.Provider.GetText(messageId),
				Padding = new Padding
				{
					Horizontal = 8,
					Vertical = 6
				},
				Anchor = new Anchor
				{
					Full = 2,
					Right = 0
				},
				Activating = action,
				Style = new TextButton.TextButtonStyle
				{
					Default = new TextButton.TextButtonStyleState
					{
						Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 150)),
						LabelStyle = new LabelStyle
						{
							FontSize = 13f
						}
					},
					Hovered = new TextButton.TextButtonStyleState
					{
						Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 180)),
						LabelStyle = new LabelStyle
						{
							FontSize = 13f
						}
					},
					Pressed = new TextButton.TextButtonStyleState
					{
						Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 165)),
						LabelStyle = new LabelStyle
						{
							FontSize = 13f
						}
					}
				}
			};
		}
	}

	private void OnCreateEmbedded()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		HandleChangeValue((JToken)new JObject());
		ConfigEditor.Layout();
	}

	private void OnUseExisting()
	{
		HandleChangeValue(JToken.op_Implicit(""));
		ConfigEditor.Layout();
		_referenceEditor.ValueEditor.Focus();
	}

	public override void SetValue(JToken value)
	{
		base.SetValue(value);
		_referenceEditor?.ValueEditor.SetValue(value);
	}

	public override void SetValueRecursively(JToken value)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		JToken value2 = base.Value;
		bool flag = value2 != null && (int)value2.Type == 8 && value != null && (int)value.Type == 8;
		SetValue(value);
		if (!flag)
		{
			_properties.Clear();
			Clear();
			Build();
			ConfigEditor.UpdateCategories();
		}
	}

	protected internal override void UpdateDisplayedValue()
	{
		_referenceEditor?.ValueEditor.UpdateDisplayedValue();
	}

	public void CreateDedicatedAsset()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		Debug.Assert(base.Value is JObject);
		string currentAssetId = ConfigEditor.GetCurrentAssetId();
		CreateAssetModal createAssetModal = ConfigEditor.AssetEditorOverlay.CreateAssetModal;
		string assetType = Schema.AssetType;
		JObject json = (JObject)base.Value;
		createAssetModal.Open(assetType, null, null, currentAssetId, json, delegate(string filePath, FormattedMessage error)
		{
			if (error == null && base.IsMounted)
			{
				HandleChangeValue(JToken.op_Implicit(ConfigEditor.AssetEditorOverlay.GetAssetIdFromReference(new AssetReference(Schema.AssetType, filePath))));
				Validate();
				ConfigEditor.Layout();
			}
		});
	}

	public void EmbedReference()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("Parent", JToken.op_Implicit((string)base.Value));
		HandleChangeValue((JToken)val);
		Clear();
		Build();
		ConfigEditor.Layout();
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		return (int)value.Type == 8 || (int)value.Type == 1;
	}
}
