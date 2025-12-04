#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class PropertyEditor : Element
{
	private static readonly UInt32Color HeaderBorderColor = UInt32Color.FromRGBA(48, 48, 48, byte.MaxValue);

	public static readonly UInt32Color BorderColor = UInt32Color.FromRGBA(48, 48, 48, 168);

	public readonly SchemaNode Schema;

	public readonly SchemaNode ParentSchema;

	public readonly ConfigEditor ConfigEditor;

	private PropertyLabel _nameLabel;

	private Label _containerHeader;

	private Group _valueContainer;

	private CheckBox _syncCheckBox;

	private Button _initializeButton;

	public readonly ValueEditor ParentValueEditor;

	public bool HasChildErrors;

	public bool HasChildWarnings;

	public bool IsSchemaTypeField;

	public readonly string DisplayName;

	private bool _drawBottomBorder;

	public string PropertyName { get; private set; }

	public PropertyPath Path { get; private set; }

	public ValueEditor ValueEditor { get; private set; }

	public bool HasErrors { get; private set; }

	public bool HasWarnings { get; private set; }

	public bool IsCollapsed => !_valueContainer.Visible;

	public bool SyncPropertyChanges => _syncCheckBox.Value;

	public PropertyEditor(Desktop desktop, Element parent, string propertyName, SchemaNode schema, PropertyPath path, SchemaNode parentSchema, ConfigEditor configEditor, ValueEditor parentValueEditor, string displayName = null, bool isVertical = false)
		: base(desktop, parent)
	{
		PropertyName = propertyName;
		Schema = schema;
		Path = path;
		ParentSchema = parentSchema;
		ConfigEditor = configEditor;
		ParentValueEditor = parentValueEditor;
		DisplayName = displayName;
		Padding.Bottom = 1;
		Name = "P_" + PropertyName;
	}

	public void Build(JToken value, bool filterCategory = false, bool isDetached = false, CacheRebuildInfo cacheRebuildInfo = null)
	{
		SchemaNode parentSchema = ParentSchema;
		_drawBottomBorder = parentSchema == null || !parentSchema.DisplayCompact;
		try
		{
			if ((Schema.Type == SchemaNode.NodeType.List || Schema.Type == SchemaNode.NodeType.Map || Schema.Type == SchemaNode.NodeType.Object || Schema.Type == SchemaNode.NodeType.AssetReferenceOrInline || Schema.Type == SchemaNode.NodeType.Source || Schema.Type == SchemaNode.NodeType.Timeline || Schema.Type == SchemaNode.NodeType.WeightedTimeline) && !Schema.DisplayCompact)
			{
				BuildContainerProperty(value, filterCategory, isDetached, cacheRebuildInfo);
			}
			else
			{
				BuildBasicProperty(value, filterCategory, isDetached, cacheRebuildInfo);
			}
		}
		catch (Exception innerException)
		{
			throw new Exception($"Failed to build property {Path} with value {value}", innerException);
		}
	}

	private string GetHeaderText(JToken value)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		switch (Schema.Type)
		{
		case SchemaNode.NodeType.List:
		{
			IUIProvider provider3 = Desktop.Provider;
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			IUIProvider provider4 = Desktop.Provider;
			JArray val2 = (JArray)value;
			dictionary2.Add("count", provider4.FormatNumber(((int)val2 != 0) ? ((JContainer)val2).Count : 0));
			return provider3.GetText("ui.assetEditor.listEditor.header", dictionary2);
		}
		case SchemaNode.NodeType.Map:
		{
			IUIProvider provider = Desktop.Provider;
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			IUIProvider provider2 = Desktop.Provider;
			JObject val = (JObject)value;
			dictionary.Add("count", provider2.FormatNumber(((int)val != 0) ? ((JContainer)val).Count : 0));
			return provider.GetText("ui.assetEditor.mapEditor.header", dictionary);
		}
		default:
			return null;
		}
	}

	private void BuildContainerProperty(JToken value, bool filterCategory, bool isDetachedEditor, CacheRebuildInfo cacheRebuildInfo = null)
	{
		Clear();
		_layoutMode = LayoutMode.Top;
		PatchStyle background = new PatchStyle(UInt32Color.FromRGBA(59, 59, 59, byte.MaxValue));
		Button parent = new Button(Desktop, this)
		{
			LayoutMode = LayoutMode.Left,
			Anchor = new Anchor
			{
				Height = 32,
				Bottom = 0
			},
			Activating = delegate
			{
				SetCollapseState(!_valueContainer.Visible);
			},
			RightClicking = OpenContextPopup
		};
		_nameLabel = new PropertyLabel(this, parent, isCollapsable: true)
		{
			Text = (DisplayName ?? PropertyName),
			Background = background,
			Anchor = new Anchor
			{
				Width = 250
			}
		};
		new ResizerHandle(Desktop, parent)
		{
			Anchor = new Anchor
			{
				Width = 1
			},
			Background = background,
			Resizing = OnResize,
			MouseButtonReleased = OnResizeComplete
		};
		new ResizerHandle(Desktop, parent)
		{
			Anchor = new Anchor
			{
				Width = 1
			},
			Background = new PatchStyle(HeaderBorderColor),
			Resizing = OnResize,
			MouseButtonReleased = OnResizeComplete
		};
		new ResizerHandle(Desktop, parent)
		{
			Anchor = new Anchor
			{
				Width = 1
			},
			Background = background,
			Resizing = OnResize,
			MouseButtonReleased = OnResizeComplete
		};
		_containerHeader = new Label(Desktop, parent)
		{
			Background = background,
			Padding = new Padding
			{
				Left = 8,
				Right = 5,
				Top = 6
			},
			FlexWeight = 1,
			Style = new LabelStyle
			{
				HorizontalAlignment = LabelStyle.LabelAlignment.Start,
				TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 80),
				FontSize = 14f,
				RenderBold = false,
				RenderItalics = true
			}
		};
		if (Schema.Type == SchemaNode.NodeType.Timeline || Schema.Type == SchemaNode.NodeType.WeightedTimeline)
		{
			new Group(Desktop, parent)
			{
				Anchor = new Anchor
				{
					Width = 1
				},
				Background = new PatchStyle(HeaderBorderColor)
			};
			_syncCheckBox = new CheckBox(Desktop, parent)
			{
				Background = background,
				Anchor = new Anchor
				{
					Width = 32
				},
				TooltipText = "Enable/disable synchronization of property changes",
				Style = new CheckBox.CheckBoxStyle
				{
					Checked = new CheckBoxStyleState
					{
						DefaultBackground = new PatchStyle("AssetEditor/SyncPropertiesIcon.png")
					},
					Unchecked = new CheckBoxStyleState
					{
						DefaultBackground = new PatchStyle("AssetEditor/SyncPropertiesIcon.png")
						{
							Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 50)
						}
					}
				}
			};
		}
		if (Schema.Type == SchemaNode.NodeType.List || Schema.Type == SchemaNode.NodeType.Map || Schema.Type == SchemaNode.NodeType.WeightedTimeline)
		{
			new Group(Desktop, parent)
			{
				Anchor = new Anchor
				{
					Width = 1
				},
				Background = new PatchStyle(HeaderBorderColor)
			};
			new TextButton(Desktop, parent)
			{
				Text = "+",
				Background = background,
				Anchor = new Anchor
				{
					Width = 32
				},
				Style = new TextButton.TextButtonStyle
				{
					Default = new TextButton.TextButtonStyleState
					{
						LabelStyle = new LabelStyle
						{
							RenderBold = true,
							FontSize = 28f,
							Alignment = LabelStyle.LabelAlignment.Center
						}
					}
				},
				Activating = OnActivateInsertButton
			};
		}
		if (Schema.Type == SchemaNode.NodeType.Object && Schema.AllowEmptyObject)
		{
			new Group(Desktop, parent)
			{
				Anchor = new Anchor
				{
					Width = 1
				},
				Background = new PatchStyle(HeaderBorderColor)
			};
			_initializeButton = new Button(Desktop, parent)
			{
				Background = background,
				Anchor = new Anchor
				{
					Width = 32
				},
				Style = new Button.ButtonStyle
				{
					Default = new Button.ButtonStyleState()
				},
				Activating = OnActivateInitializeObjectButton
			};
		}
		new Group(Desktop, this)
		{
			Anchor = new Anchor
			{
				Height = 1
			},
			Background = new PatchStyle(HeaderBorderColor)
		};
		_valueContainer = new Group(Desktop, this)
		{
			FlexWeight = 1,
			Background = new PatchStyle(UInt32Color.FromRGBA(162, 162, 162, 41))
		};
		if (ConfigEditor.State.UncollapsedProperties.TryGetValue(Path, out var value2))
		{
			_valueContainer.Visible = value2;
		}
		else
		{
			_valueContainer.Visible = !Schema.IsCollapsedByDefault;
		}
		ValueEditor = ValueEditor.CreateFromSchema(_valueContainer, Schema, Path, this, ParentSchema, ConfigEditor, value);
		ValueEditor.FilterCategory = filterCategory;
		ValueEditor.IsDetachedEditor = isDetachedEditor;
		ValueEditor.CachesToRebuild = cacheRebuildInfo;
		ValueEditor.ValidateValue();
		ValueEditor.BuildEditor();
		UpdateAppearance();
	}

	public override Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		PropertyLabel nameLabel = _nameLabel;
		Anchor anchor = default(Anchor);
		SchemaNode parentSchema = ParentSchema;
		anchor.Width = ((parentSchema == null || !parentSchema.DisplayCompact) ? ConfigEditor.AssetEditorOverlay.Interface.App.Settings.PaneSizes[AssetEditorSettings.Panes.ConfigEditorPropertyNames] : 70);
		nameLabel.Anchor = anchor;
		return base.ComputeScaledMinSize(maxWidth, maxHeight);
	}

	private void BuildBasicProperty(JToken value, bool filterCategory, bool isDetachedEditor, CacheRebuildInfo cacheRebuildInfo = null)
	{
		Clear();
		_layoutMode = LayoutMode.Left;
		Anchor = new Anchor
		{
			Height = 33
		};
		_nameLabel = new PropertyLabel(this, this, isCollapsable: false)
		{
			Text = (DisplayName ?? PropertyName)
		};
		PropertyLabel nameLabel = _nameLabel;
		Anchor anchor = default(Anchor);
		SchemaNode parentSchema = ParentSchema;
		anchor.Width = ((parentSchema == null || !parentSchema.DisplayCompact) ? 250 : 70);
		nameLabel.Anchor = anchor;
		SchemaNode parentSchema2 = ParentSchema;
		if (parentSchema2 == null || !parentSchema2.DisplayCompact)
		{
			new ResizerHandle(Desktop, this)
			{
				Anchor = new Anchor
				{
					Width = 1
				},
				Resizing = OnResize,
				MouseButtonReleased = OnResizeComplete
			};
			new ResizerHandle(Desktop, this)
			{
				Anchor = new Anchor
				{
					Width = 1
				},
				Background = new PatchStyle(BorderColor),
				Resizing = OnResize,
				MouseButtonReleased = OnResizeComplete
			};
			new ResizerHandle(Desktop, this)
			{
				Anchor = new Anchor
				{
					Width = 1
				},
				Resizing = OnResize,
				MouseButtonReleased = OnResizeComplete
			};
		}
		Group group = new Group(Desktop, this);
		SchemaNode parentSchema3 = ParentSchema;
		if (parentSchema3 != null && parentSchema3.DisplayCompact)
		{
			group.FlexWeight = 0;
			group.Anchor = new Anchor
			{
				Vertical = 4,
				Left = 4,
				Width = 64
			};
			group.OutlineColor = UInt32Color.FromRGBA(106, 106, 106, byte.MaxValue);
			group.OutlineSize = 1f;
			group.Background = new PatchStyle(UInt32Color.FromRGBA(66, 66, 66, 114));
		}
		else
		{
			group.FlexWeight = 1;
		}
		ValueEditor = ValueEditor.CreateFromSchema(group, Schema, Path, this, ParentSchema, ConfigEditor, value);
		ValueEditor.FilterCategory = filterCategory;
		ValueEditor.IsDetachedEditor = isDetachedEditor;
		ValueEditor.CachesToRebuild = cacheRebuildInfo;
		ValueEditor.ValidateValue();
		ValueEditor.BuildEditor();
		UpdateAppearance();
	}

	private void OnResize()
	{
		int num = Desktop.UnscaleRound(base.AnchoredRectangle.Width) - 100;
		int num2 = Desktop.UnscaleRound(Desktop.MousePosition.X - _rectangleAfterPadding.X);
		if (num2 < 100)
		{
			num2 = 100;
		}
		else if (num2 > num)
		{
			num2 = num;
		}
		_nameLabel.Anchor.Width = num2;
		Layout();
	}

	private void OnResizeComplete()
	{
		ConfigEditor.AssetEditorOverlay.UpdatePaneSize(AssetEditorSettings.Panes.ConfigEditorPropertyNames, _nameLabel.Anchor.Width.Value);
		ConfigEditor.Layout();
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if ((long)evt.Button == 3 && activate)
		{
			OpenContextPopup();
		}
	}

	private void OnActivateInsertButton()
	{
		ValueEditor valueEditor = ValueEditor;
		ValueEditor valueEditor2 = valueEditor;
		if (!(valueEditor2 is ListEditor listEditor))
		{
			if (!(valueEditor2 is MapEditor))
			{
				if (valueEditor2 is WeightedTimelineEditor weightedTimelineEditor)
				{
					ConfigEditor.KeyModal.OpenInsertKey(Path, weightedTimelineEditor.IdSchema, ConfigEditor);
				}
			}
			else
			{
				ConfigEditor.KeyModal.OpenInsertKey(Path, Schema.Key, ConfigEditor);
			}
		}
		else
		{
			listEditor.HandleInsertItem();
		}
	}

	private void OnActivateInitializeObjectButton()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		if (ValueEditor.Value == null)
		{
			ConfigEditor.OnChangeValue(Path, (JToken)new JObject(), ValueEditor.Value, ValueEditor?.CachesToRebuild?.Caches);
			Layout();
		}
		else
		{
			HandleRemoveProperty();
		}
	}

	internal void SetCollapseState(bool uncollapsed, bool doDiagnosticsAndLayout = true)
	{
		if (_containerHeader == null || _valueContainer.Visible == uncollapsed)
		{
			return;
		}
		ConfigEditor.State.UncollapsedProperties[Path] = !_valueContainer.Visible;
		if (_valueContainer.Visible != uncollapsed)
		{
			_valueContainer.Visible = uncollapsed;
			if (doDiagnosticsAndLayout)
			{
				ConfigEditor.SetupDiagnostics(doLayout: false);
				ConfigEditor.Layout();
			}
		}
	}

	internal void OpenRenameKeyModal()
	{
		ConfigEditor.KeyModal.OpenEditKey(PropertyName, ParentValueEditor.Path, ParentValueEditor.Schema.Key, ConfigEditor);
	}

	internal void OpenContextPopup()
	{
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Invalid comparison between Unknown and I4
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Invalid comparison between Unknown and I4
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Invalid comparison between Unknown and I4
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_0686: Invalid comparison between Unknown and I4
		PopupMenuLayer popup = ConfigEditor.AssetEditorOverlay.Popup;
		List<PopupMenuItem> list = new List<PopupMenuItem>();
		if (ParentValueEditor is ListEditor || ParentValueEditor is MapEditor || ValueEditor?.Value != null)
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.remove"), delegate
			{
				HandleRemoveProperty();
			}));
		}
		ValueEditor parentValueEditor = ParentValueEditor;
		ListEditor listParentValueEditor = parentValueEditor as ListEditor;
		if (listParentValueEditor != null)
		{
			int index = int.Parse(PropertyName);
			if (index > 0)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.moveUp"), delegate
				{
					listParentValueEditor.HandleMoveItem(index, index - 1);
				}));
			}
			if (index < listParentValueEditor.Count() - 1)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.moveDown"), delegate
				{
					listParentValueEditor.HandleMoveItem(index, index + 1);
				}));
			}
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.insertSiblingBefore"), delegate
			{
				listParentValueEditor.HandleInsertItem(index);
			}));
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.insertSiblingAfter"), delegate
			{
				listParentValueEditor.HandleInsertItem(index + 1);
			}));
		}
		parentValueEditor = ParentValueEditor;
		MapEditor parentMapEditor = parentValueEditor as MapEditor;
		if (parentMapEditor != null)
		{
			int num = -1;
			for (int i = 0; i < Parent.Children.Count; i++)
			{
				if (Parent.Children[i] == this)
				{
					num = i;
					break;
				}
			}
			if (num > 0)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.moveUp"), delegate
				{
					parentMapEditor.HandleMoveKey(PropertyName, backwards: true);
				}));
			}
			if (num < ((JContainer)(JObject)parentMapEditor.Value).Count - 1)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.moveDown"), delegate
				{
					parentMapEditor.HandleMoveKey(PropertyName, backwards: false);
				}));
			}
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.renameKey"), OpenRenameKeyModal));
		}
		parentValueEditor = ParentValueEditor;
		AssetReferenceOrInlineEditor parentAssetReferenceOrInlineEditor = parentValueEditor as AssetReferenceOrInlineEditor;
		if (parentAssetReferenceOrInlineEditor != null)
		{
			JToken value = parentAssetReferenceOrInlineEditor.Value;
			if (value != null && (int)value.Type == 8)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.createEmbeddedAsset"), delegate
				{
					parentAssetReferenceOrInlineEditor.EmbedReference();
				}));
			}
		}
		parentValueEditor = ValueEditor;
		AssetReferenceOrInlineEditor assetReferenceOrInlineEditor = parentValueEditor as AssetReferenceOrInlineEditor;
		if (assetReferenceOrInlineEditor != null)
		{
			JToken value2 = assetReferenceOrInlineEditor.Value;
			if (value2 != null && (int)value2.Type == 8)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.createEmbeddedAsset"), delegate
				{
					assetReferenceOrInlineEditor.EmbedReference();
				}));
			}
			else
			{
				JToken value3 = assetReferenceOrInlineEditor.Value;
				if (value3 != null && (int)value3.Type == 1)
				{
					list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.createDedicatedAsset"), delegate
					{
						assetReferenceOrInlineEditor.CreateDedicatedAsset();
					}));
				}
			}
		}
		parentValueEditor = ValueEditor;
		ListEditor listEditor = parentValueEditor as ListEditor;
		if (listEditor != null)
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.uncollapseAllEntries"), delegate
			{
				listEditor.SetCollapseStateForAllItems(uncollapsed: true);
			}));
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.collapseAllEntries"), delegate
			{
				listEditor.SetCollapseStateForAllItems(uncollapsed: false);
			}));
		}
		parentValueEditor = ValueEditor;
		MapEditor mapEditor = parentValueEditor as MapEditor;
		if (mapEditor != null)
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.uncollapseAllEntries"), delegate
			{
				mapEditor.SetCollapseStateForAllItems(uncollapsed: true);
			}));
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.collapseAllEntries"), delegate
			{
				mapEditor.SetCollapseStateForAllItems(uncollapsed: false);
			}));
		}
		parentValueEditor = ValueEditor;
		AssetDropdownEditor assetDropdownEditor = parentValueEditor as AssetDropdownEditor;
		if (assetDropdownEditor != null)
		{
			if (ValueEditor.Value != null)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.openAssetInNewTab"), delegate
				{
					assetDropdownEditor.OpenSelectedAssetInNewTab();
				}));
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.copyAndReferenceAsset"), delegate
				{
					assetDropdownEditor.CopyAssetAndReference();
				}));
			}
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.createAndReferenceAsset"), delegate
			{
				assetDropdownEditor.CreateNewAssetAndReference();
			}));
		}
		if (ValueEditor?.Value != null)
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.copyValue"), CopyValue));
		}
		if ((int)SDL.SDL_HasClipboardText() == 1)
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.property.pasteValue"), PasteValue));
		}
		if (list.Count != 0)
		{
			popup.SetTitle(Desktop.Provider.GetText("ui.assetEditor.property.title", new Dictionary<string, string> { 
			{
				"name",
				Schema.Title ?? PropertyName
			} }));
			popup.SetItems(list);
			popup.Open();
		}
	}

	private void CopyValue()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		string text = ((object)ValueEditor.Value).ToString();
		if ((int)ValueEditor.Value.Type == 9)
		{
			text = text.ToLowerInvariant();
		}
		SDL.SDL_SetClipboardText(text);
	}

	private void PasteValue()
	{
		string text = SDL.SDL_GetClipboardText();
		ValueEditor.PasteValue(text);
	}

	private void ClearParentProperty()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		PropertyPath parent = Path.GetParent();
		JToken value = ConfigEditor.GetValue(parent);
		SchemaNode schemaNodeInCurrentContext = ConfigEditor.AssetEditorOverlay.GetSchemaNodeInCurrentContext(ConfigEditor.Value, parent);
		ConfigEditor.OnChangeValue(parent, (JToken)new JObject(), (value != null) ? value.DeepClone() : null, schemaNodeInCurrentContext.RebuildCaches);
		ConfigEditor.Layout();
		if (IsSchemaTypeField)
		{
			ValueEditor parentValueEditor = ParentValueEditor;
			if (parentValueEditor != null && parentValueEditor.FilterCategory)
			{
				ConfigEditor.UpdateCategories();
			}
		}
	}

	public void HandleRemoveProperty(bool confirmed = false)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		if (IsSchemaTypeField && !confirmed)
		{
			JObject val = (JObject)ConfigEditor.GetValue(Path.GetParent());
			if (((JContainer)val).Count > 1 || !val.ContainsKey(ParentSchema.TypePropertyKey))
			{
				ConfigEditor.AssetEditorOverlay.ConfirmationModal.Open(Desktop.Provider.GetText("ui.assetEditor.changeConfirmationModal.title"), Desktop.Provider.GetText("ui.assetEditor.changeConfirmationModal.text"), ClearParentProperty);
			}
			else
			{
				ClearParentProperty();
			}
			return;
		}
		ConfigEditor.OnRemoveProperty(Path, ValueEditor?.CachesToRebuild?.Caches);
		ValueEditor.UpdateDisplayedValue();
		ConfigEditor.Layout();
		SchemaNode parentSchema = ParentSchema;
		if (parentSchema == null || parentSchema.Type != SchemaNode.NodeType.List)
		{
			SchemaNode parentSchema2 = ParentSchema;
			if (parentSchema2 == null || parentSchema2.Type != SchemaNode.NodeType.Map)
			{
				return;
			}
		}
		ParentValueEditor?.ParentPropertyEditor.UpdateAppearance();
	}

	public void UpdateAppearance()
	{
		_nameLabel.ApplyTextColor();
		if (Schema.Type == SchemaNode.NodeType.List || Schema.Type == SchemaNode.NodeType.Map)
		{
			_containerHeader.Text = GetHeaderText(ValueEditor.Value) ?? "";
			if (base.IsMounted)
			{
				_containerHeader.Layout();
			}
		}
		if (_initializeButton != null)
		{
			_initializeButton.TooltipText = Desktop.Provider.GetText((ValueEditor.Value != null) ? "ui.assetEditor.initializeButton.active" : "ui.assetEditor.initializeButton.inactive");
			_initializeButton.Style.Default.Background = new PatchStyle((ValueEditor.Value != null) ? "AssetEditor/DeinitializeIcon.png" : "AssetEditor/InitializeIcon.png");
			_initializeButton.Layout();
		}
	}

	public void UpdatePathRecursively(string propertyName, PropertyPath path)
	{
		PropertyName = propertyName;
		Path = path;
		ValueEditor?.UpdatePathRecursively(path);
		SchemaNode.NodeType? nodeType = ParentSchema?.Type;
		SchemaNode.NodeType? nodeType2 = nodeType;
		if (nodeType2.HasValue)
		{
			SchemaNode.NodeType valueOrDefault = nodeType2.GetValueOrDefault();
			if ((uint)(valueOrDefault - 12) <= 1u)
			{
				_nameLabel.Text = PropertyName;
			}
		}
	}

	public void SetHasError(bool doLayout = true)
	{
		if (!HasErrors)
		{
			HasErrors = true;
			_nameLabel.ApplyTextColor();
			if (doLayout)
			{
				_nameLabel.Layout();
			}
		}
	}

	public void SetHasWarning(bool doLayout = true)
	{
		if (HasWarnings)
		{
			return;
		}
		HasWarnings = true;
		if (!HasErrors)
		{
			_nameLabel.ApplyTextColor();
			if (doLayout)
			{
				_nameLabel.Layout();
			}
		}
	}

	public void ClearDiagnostics(bool doLayout = true)
	{
		if (HasErrors || HasWarnings)
		{
			HasErrors = false;
			HasWarnings = false;
			HasChildErrors = false;
			HasChildWarnings = false;
			_nameLabel.ApplyTextColor();
			if (doLayout)
			{
				_nameLabel.Layout();
			}
		}
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (_drawBottomBorder)
		{
			TextureArea whitePixel = Desktop.Provider.WhitePixel;
			Desktop.Batcher2D.RequestDrawTexture(whitePixel.Texture, whitePixel.Rectangle, new Vector3(_anchoredRectangle.X, (float)(_anchoredRectangle.Y + _anchoredRectangle.Height) - 1f, 0f), _anchoredRectangle.Width, 1f, (_layoutMode == LayoutMode.Top) ? HeaderBorderColor : BorderColor);
		}
	}
}
