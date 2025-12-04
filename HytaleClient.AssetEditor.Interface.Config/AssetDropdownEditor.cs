using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.Graphics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class AssetDropdownEditor : ValueEditor
{
	private AssetSelectorDropdown _dropdown;

	public AssetDropdownEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void Build()
	{
		_layoutMode = LayoutMode.Left;
		string text = Schema.AssetType;
		if (ConfigEditor.AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(text, out var value))
		{
			if (value.IdProvider != null)
			{
				text = value.IdProvider;
			}
			_dropdown = new AssetSelectorDropdown(Desktop, this, ConfigEditor.AssetEditorOverlay)
			{
				FlexWeight = 1,
				AssetType = text,
				Value = (string)base.Value,
				ValueChanged = delegate
				{
					HandleChangeValue(JToken.op_Implicit(_dropdown.Value));
					Validate();
				},
				Style = ConfigEditor.FileDropdownBoxStyle
			};
			new Group(Desktop, this)
			{
				Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 70)),
				Anchor = new Anchor
				{
					Width = 1
				}
			};
			Button obj = new Button(Desktop, this)
			{
				Anchor = new Anchor
				{
					Width = 32
				}
			};
			string tooltipText = (base.TooltipText = Desktop.Provider.GetText("ui.assetEditor.assetDropdownEditor.openAssetTooltip"));
			obj.TooltipText = tooltipText;
			obj.Background = new PatchStyle("AssetEditor/OpenAssetIcon.png");
			obj.Activating = OpenSelectedAssetInNewTab;
			Button button = obj;
			new Group(Desktop, this)
			{
				Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 70)),
				Anchor = new Anchor
				{
					Width = 1
				}
			};
			new TextButton(Desktop, this)
			{
				Anchor = new Anchor
				{
					Width = 30
				},
				Style = new TextButton.TextButtonStyle
				{
					Default = new TextButton.TextButtonStyleState
					{
						LabelStyle = new LabelStyle
						{
							RenderBold = true,
							FontSize = 20f,
							Alignment = LabelStyle.LabelAlignment.Center
						}
					}
				},
				TooltipText = Desktop.Provider.GetText("ui.assetEditor.assetDropdownEditor.createAssetTooltip", new Dictionary<string, string> { { "assetType", value.Name } }),
				Text = "+",
				Activating = delegate
				{
					CreateNewAssetAndReference();
				}
			};
		}
	}

	public void OpenSelectedAssetInNewTab()
	{
		if (_dropdown != null && _dropdown.Value != null)
		{
			string assetType = _dropdown.AssetType;
			if (ConfigEditor.AssetEditorOverlay.Assets.TryGetPathForAssetId(assetType, _dropdown.Value, out var filePath))
			{
				ConfigEditor.AssetEditorOverlay.OpenExistingAsset(new AssetReference(assetType, filePath), bringAssetIntoAssetTreeView: true);
			}
		}
	}

	public void CreateNewAssetAndReference(string assetToCopy = null, string id = null)
	{
		if (_dropdown == null)
		{
			return;
		}
		if (id == null)
		{
			id = ConfigEditor.GetCurrentAssetId();
		}
		string assetType = _dropdown.AssetType;
		ConfigEditor.AssetEditorOverlay.CreateAssetModal.Open(assetType, assetToCopy, null, id, null, delegate(string filePath, FormattedMessage error)
		{
			if (error == null && base.IsMounted)
			{
				HandleChangeValue(JToken.op_Implicit(ConfigEditor.AssetEditorOverlay.GetAssetIdFromReference(new AssetReference(assetType, filePath))));
				Validate();
			}
		});
	}

	public void CopyAssetAndReference()
	{
		if (_dropdown != null && _dropdown.Value != null && ConfigEditor.AssetEditorOverlay.Assets.TryGetPathForAssetId(_dropdown.AssetType, _dropdown.Value, out var filePath))
		{
			string currentAssetId = ConfigEditor.GetCurrentAssetId();
			CreateNewAssetAndReference(filePath, currentAssetId);
		}
	}

	public override void Focus()
	{
		if (_dropdown != null)
		{
			_dropdown.Open();
		}
	}

	protected internal override void UpdateDisplayedValue()
	{
		if (_dropdown != null)
		{
			_dropdown.Value = (string)base.Value;
			_dropdown.Layout();
		}
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 8;
	}
}
