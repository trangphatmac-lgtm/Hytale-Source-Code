using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Graphics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class PropertyLabel : ReorderableListGrip
{
	private static readonly PatchStyle IconUncollapsed = new PatchStyle("Common/CaretUncollapsed.png")
	{
		Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 153)
	};

	private static readonly PatchStyle IconCollapsed = new PatchStyle("Common/CaretCollapsed.png")
	{
		Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 153)
	};

	private static readonly PatchStyle IconError = new PatchStyle("AssetEditor/ErrorIcon.png");

	private static readonly PatchStyle IconWarning = new PatchStyle("AssetEditor/WarningIcon.png");

	public static readonly string IconRemove = "AssetEditor/PropertyRemoveIcon.png";

	private readonly PropertyEditor _propertyEditor;

	private readonly Label _label;

	private readonly Group _diagnosticsIcon;

	private readonly Group _collapseIcon;

	private readonly Button _removeButton;

	public string Text
	{
		set
		{
			_label.Text = value;
		}
	}

	public PropertyLabel(PropertyEditor propertyEditor, Element parent, bool isCollapsable)
		: base(propertyEditor.Desktop, parent)
	{
		_propertyEditor = propertyEditor;
		_layoutMode = LayoutMode.Left;
		SchemaNode parentSchema = propertyEditor.ParentSchema;
		int isDragEnabled;
		if (parentSchema == null || parentSchema.Type != SchemaNode.NodeType.Map)
		{
			SchemaNode parentSchema2 = propertyEditor.ParentSchema;
			isDragEnabled = ((parentSchema2 != null && parentSchema2.Type == SchemaNode.NodeType.List) ? 1 : 0);
		}
		else
		{
			isDragEnabled = 1;
		}
		IsDragEnabled = (byte)isDragEnabled != 0;
		Padding.Left = 12 + (propertyEditor.Path.Elements.Length - 1) * 18;
		SchemaNode parentSchema3 = propertyEditor.ParentSchema;
		if (parentSchema3 == null || parentSchema3.Type != SchemaNode.NodeType.List)
		{
			SchemaNode parentSchema4 = propertyEditor.ParentSchema;
			if (parentSchema4 == null || parentSchema4.Type != SchemaNode.NodeType.Map)
			{
				goto IL_0127;
			}
		}
		Padding.Left = 3;
		new Group(Desktop, this)
		{
			Anchor = new Anchor
			{
				Width = 6,
				Height = 18,
				Right = 3 + (propertyEditor.Path.Elements.Length - 1) * 18
			},
			Background = new PatchStyle("AssetEditor/GripIcon.png")
		};
		goto IL_0127;
		IL_0127:
		if (isCollapsable)
		{
			_collapseIcon = new Group(Desktop, this)
			{
				Anchor = new Anchor
				{
					Right = 6,
					Width = 12,
					Height = 12
				}
			};
		}
		_diagnosticsIcon = new Group(Desktop, this)
		{
			Anchor = new Anchor
			{
				Right = 6,
				Width = 18,
				Height = 18
			},
			Visible = false
		};
		_label = new Label(Desktop, this)
		{
			FlexWeight = 1,
			Style = new LabelStyle
			{
				HorizontalAlignment = LabelStyle.LabelAlignment.Start,
				FontSize = 14f,
				VerticalAlignment = LabelStyle.LabelAlignment.Center
			}
		};
		SchemaNode parentSchema5 = propertyEditor.ParentSchema;
		if (parentSchema5 == null || parentSchema5.Type != SchemaNode.NodeType.List)
		{
			SchemaNode parentSchema6 = propertyEditor.ParentSchema;
			if (parentSchema6 == null || parentSchema6.Type != SchemaNode.NodeType.Map)
			{
				goto IL_0251;
			}
		}
		_label.Style.RenderBold = true;
		goto IL_0251;
		IL_0251:
		_removeButton = new Button(Desktop, this)
		{
			Visible = false,
			Anchor = new Anchor
			{
				Right = 5,
				Width = 22,
				Height = 22
			},
			Style = new Button.ButtonStyle
			{
				Default = new Button.ButtonStyleState
				{
					Background = new PatchStyle(IconRemove)
					{
						Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 38)
					}
				},
				Hovered = new Button.ButtonStyleState
				{
					Background = new PatchStyle(IconRemove)
					{
						Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 120)
					}
				},
				Pressed = new Button.ButtonStyleState
				{
					Background = new PatchStyle(IconRemove)
					{
						Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 100)
					}
				}
			},
			Activating = OnRemoveButtonActivating
		};
		SchemaNode parentSchema7 = _propertyEditor.ParentSchema;
		if (parentSchema7 != null && parentSchema7.DisplayCompact)
		{
			Padding.Left = 6;
			_label.Style.HorizontalAlignment = LabelStyle.LabelAlignment.End;
			Reorder(_label, base.Children.Count - 1);
		}
	}

	public void ApplyTextColor()
	{
		if (_propertyEditor.HasErrors)
		{
			_label.Style.TextColor = UInt32Color.FromRGBA(232, 96, 96, (byte)((_propertyEditor.ValueEditor.Value != null) ? 200u : 120u));
		}
		else if (_propertyEditor.HasWarnings)
		{
			_label.Style.TextColor = UInt32Color.FromRGBA(232, 151, 96, (byte)((_propertyEditor.ValueEditor.Value != null) ? 200u : 120u));
		}
		else
		{
			_label.Style.TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)((_propertyEditor.ValueEditor.Value != null) ? 200u : 80u));
		}
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		if (_propertyEditor.HasErrors)
		{
			_diagnosticsIcon.Visible = true;
			_diagnosticsIcon.Background = IconError;
		}
		else if (_propertyEditor.HasWarnings)
		{
			_diagnosticsIcon.Visible = true;
			_diagnosticsIcon.Background = IconWarning;
		}
		else
		{
			_diagnosticsIcon.Visible = false;
		}
		if (_collapseIcon != null)
		{
			_collapseIcon.Background = (_propertyEditor.IsCollapsed ? IconCollapsed : IconUncollapsed);
		}
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? (base.HitTest(position) ?? this) : null;
	}

	private void OnRemoveButtonActivating()
	{
		_removeButton.Visible = false;
		_propertyEditor.HandleRemoveProperty();
	}

	protected override void OnMouseEnter()
	{
		AssetEditorOverlay assetEditorOverlay = _propertyEditor.ConfigEditor.AssetEditorOverlay;
		List<Label.LabelSpan> list = new List<Label.LabelSpan>();
		switch (_propertyEditor.ParentSchema.Type)
		{
		case SchemaNode.NodeType.List:
			list.Add(new Label.LabelSpan
			{
				Text = Desktop.Provider.GetText("ui.assetEditor.property.tooltip.arrayItem", new Dictionary<string, string> { { "index", _propertyEditor.PropertyName } }),
				IsBold = true
			});
			break;
		case SchemaNode.NodeType.Map:
			list.Add(new Label.LabelSpan
			{
				Text = Desktop.Provider.GetText("ui.assetEditor.property.tooltip.mapItem", new Dictionary<string, string> { { "key", _propertyEditor.PropertyName } }),
				IsBold = true
			});
			break;
		default:
			list.Add(new Label.LabelSpan
			{
				Text = (_propertyEditor.DisplayName ?? _propertyEditor.PropertyName),
				IsBold = true
			});
			list.Add(new Label.LabelSpan
			{
				Text = " (" + _propertyEditor.PropertyName + ")"
			});
			break;
		}
		if (_propertyEditor.Schema.Description != null)
		{
			list.Add(new Label.LabelSpan
			{
				Text = "\n"
			});
			FormattedMessageConverter.AppendLabelSpansFromMarkup(_propertyEditor.Schema.Description, list, new SpanStyle
			{
				Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 180),
				IsItalics = true
			});
		}
		if (_propertyEditor.HasChildErrors)
		{
			list.Add(new Label.LabelSpan
			{
				Text = "\n\n" + Desktop.Provider.GetText("ui.assetEditor.diagnosticsTooltip.hasChildErrors")
			});
		}
		if (_propertyEditor.HasChildWarnings)
		{
			list.Add(new Label.LabelSpan
			{
				Text = "\n\n" + Desktop.Provider.GetText("ui.assetEditor.diagnosticsTooltip.hasChildWarnings")
			});
		}
		if ((_propertyEditor.HasErrors || _propertyEditor.HasWarnings) && assetEditorOverlay.Diagnostics.TryGetValue(assetEditorOverlay.CurrentAsset.FilePath, out var value))
		{
			if (value.Errors != null && value.Errors.Length != 0)
			{
				bool flag = false;
				AssetDiagnosticMessage[] errors = value.Errors;
				for (int i = 0; i < errors.Length; i++)
				{
					AssetDiagnosticMessage assetDiagnosticMessage = errors[i];
					if (_propertyEditor.Path.Equals(assetDiagnosticMessage.Property))
					{
						if (!flag)
						{
							flag = true;
							list.Add(new Label.LabelSpan
							{
								Text = "\n\n" + Desktop.Provider.GetText("ui.assetEditor.diagnosticsTooltip.errors"),
								IsBold = true
							});
						}
						list.Add(new Label.LabelSpan
						{
							Text = "\n- " + assetDiagnosticMessage.Message
						});
					}
				}
			}
			if (value.Warnings != null && value.Warnings.Length != 0)
			{
				bool flag2 = false;
				AssetDiagnosticMessage[] warnings = value.Warnings;
				for (int j = 0; j < warnings.Length; j++)
				{
					AssetDiagnosticMessage assetDiagnosticMessage2 = warnings[j];
					if (_propertyEditor.Path.Equals(assetDiagnosticMessage2.Property))
					{
						if (!flag2)
						{
							flag2 = true;
							list.Add(new Label.LabelSpan
							{
								Text = "\n\n" + Desktop.Provider.GetText("ui.assetEditor.diagnosticsTooltip.warnings"),
								IsBold = true
							});
						}
						list.Add(new Label.LabelSpan
						{
							Text = "\n- " + assetDiagnosticMessage2.Message
						});
					}
				}
			}
		}
		TextTooltipLayer textTooltipLayer = _propertyEditor.ConfigEditor.AssetEditorOverlay.TextTooltipLayer;
		textTooltipLayer.TextSpans = list;
		textTooltipLayer.Start();
		if (_propertyEditor.ParentValueEditor is ListEditor || _propertyEditor.ParentValueEditor is MapEditor || _propertyEditor.ValueEditor.Value != null)
		{
			_removeButton.Visible = true;
			Layout();
		}
	}

	protected override void OnMouseLeave()
	{
		_removeButton.Visible = false;
		_propertyEditor.ConfigEditor.AssetEditorOverlay.TextTooltipLayer.Stop();
	}

	protected override void OnMouseStartDrag()
	{
		_propertyEditor.ConfigEditor.AssetEditorOverlay.TextTooltipLayer.Stop();
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (!activate || _wasDragging)
		{
			return;
		}
		switch ((uint)evt.Button)
		{
		case 1u:
			if (_collapseIcon != null)
			{
				_propertyEditor.SetCollapseState(_propertyEditor.IsCollapsed);
			}
			switch (evt.Clicks)
			{
			case 1:
				Desktop.FocusElement(this);
				break;
			case 2:
				if (Desktop.FocusedElement == this && _propertyEditor.ParentValueEditor is MapEditor)
				{
					_propertyEditor.OpenRenameKeyModal();
				}
				break;
			}
			break;
		case 3u:
			_propertyEditor.OpenContextPopup();
			break;
		}
	}
}
