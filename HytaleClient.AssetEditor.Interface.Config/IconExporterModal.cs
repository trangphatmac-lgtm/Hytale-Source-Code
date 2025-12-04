using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.AssetEditor.Interface.Previews;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Data;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class IconExporterModal : Element
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const int IconSizeRender = 128;

	private const int IconSizeExport = 64;

	private readonly ConfigEditor _configEditor;

	private Group _container;

	private AssetSelectorDropdown _copyFromDropdown;

	private BlockPreview _blockPreview;

	private ModelPreview _modelPreview;

	private Group _rotationXGroup;

	private Group _rotationYGroup;

	private Group _rotationZGroup;

	private Group _translationXGroup;

	private Group _translationYGroup;

	private Group _scaleGroup;

	private Group _previewZoomGroup;

	private Group _previewFrame;

	private Group _previewArea;

	private TextField _filePathField;

	private CancellationTokenSource _cancellationTokenSource;

	private float _itemScale = 1f;

	public IconExporterModal(ConfigEditor configEditor)
		: base(configEditor.Desktop, null)
	{
		_configEditor = configEditor;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/IconExporterModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("Container");
		_previewFrame = uIFragment.Get<Group>("PreviewFrame");
		_previewArea = uIFragment.Get<Group>("PreviewArea");
		_filePathField = uIFragment.Get<TextField>("FilePathField");
		uIFragment.Get<TextButton>("CancelButton").Activating = Dismiss;
		uIFragment.Get<TextButton>("SaveButton").Activating = Export;
		uIFragment.Get<TextButton>("ResetButton").Activating = Reset;
		_copyFromDropdown = new AssetSelectorDropdown(Desktop, uIFragment.Get<Group>("CopyProperties"), _configEditor.AssetEditorOverlay)
		{
			AssetType = "Item",
			Style = _configEditor.FileDropdownBoxStyle,
			FlexWeight = 1,
			ValueChanged = delegate
			{
				CancellationToken token = _cancellationTokenSource.Token;
				AssetEditorOverlay assetEditorOverlay = _configEditor.AssetEditorOverlay;
				assetEditorOverlay.Assets.TryGetPathForAssetId("Item", _copyFromDropdown.Value, out var filePath);
				LoadCopyFromAsset(new AssetReference("Item", filePath), token);
			}
		};
		_modelPreview = new ModelPreview(_configEditor.AssetEditorOverlay, uIFragment.Get<Group>("PreviewArea"));
		_modelPreview.Visible = false;
		_blockPreview = new BlockPreview(_configEditor.AssetEditorOverlay, uIFragment.Get<Group>("PreviewArea"));
		_blockPreview.Visible = false;
		_rotationXGroup = uIFragment.Get<Group>("RotationX");
		_rotationYGroup = uIFragment.Get<Group>("RotationY");
		_rotationZGroup = uIFragment.Get<Group>("RotationZ");
		_translationXGroup = uIFragment.Get<Group>("TranslationX");
		_translationYGroup = uIFragment.Get<Group>("TranslationY");
		_scaleGroup = uIFragment.Get<Group>("Scale");
		_previewZoomGroup = uIFragment.Get<Group>("PreviewZoom");
		_previewZoomGroup.Find<NumberField>("NumberField").Format.Suffix = "%";
		UpdatePreviewZoom(3f, doLayout: false);
		SetupInput(_previewZoomGroup, delegate(decimal value)
		{
			UpdatePreviewZoom((float)(value / 100m));
		});
		SetupInput(_scaleGroup, delegate(decimal value)
		{
			AssetPreview activePreview6 = GetActivePreview();
			activePreview6.CameraScale = (float)value * _itemScale / 32f;
			activePreview6.UpdateViewMatrices();
		});
		SetupInput(_rotationXGroup, delegate(decimal value)
		{
			AssetPreview activePreview5 = GetActivePreview();
			activePreview5.CameraOrientation.Pitch = 0f - MathHelper.ToRadians((float)value);
			activePreview5.UpdateViewMatrices();
		});
		SetupInput(_rotationYGroup, delegate(decimal value)
		{
			AssetPreview activePreview4 = GetActivePreview();
			activePreview4.CameraOrientation.Yaw = 0f - MathHelper.ToRadians((float)value);
			activePreview4.UpdateViewMatrices();
		});
		SetupInput(_rotationZGroup, delegate(decimal value)
		{
			AssetPreview activePreview3 = GetActivePreview();
			activePreview3.CameraOrientation.Roll = 0f - MathHelper.ToRadians((float)value);
			activePreview3.UpdateViewMatrices();
		});
		SetupInput(_translationXGroup, delegate(decimal value)
		{
			AssetPreview activePreview2 = GetActivePreview();
			activePreview2.CameraPosition.X = 0f - (float)value;
			activePreview2.UpdateViewMatrices();
		});
		SetupInput(_translationYGroup, delegate(decimal value)
		{
			AssetPreview activePreview = GetActivePreview();
			activePreview.CameraPosition.Y = 0f - (float)value;
			activePreview.UpdateViewMatrices();
		});
		static void SetupInput(Group container, Action<decimal> valueChanged)
		{
			Slider slider = container.Find<Slider>("Slider");
			NumberField numberField = container.Find<NumberField>("NumberField");
			slider.ValueChanged = delegate
			{
				numberField.Value = (decimal)((float)slider.Value / 1000f);
				valueChanged(numberField.Value);
			};
			numberField.ValueChanged = delegate
			{
				slider.Value = (int)(numberField.Value * 1000m);
				slider.Layout();
				valueChanged(numberField.Value);
			};
		}
	}

	protected override void OnMounted()
	{
		_cancellationTokenSource = new CancellationTokenSource();
	}

	protected override void OnUnmounted()
	{
		_cancellationTokenSource.Cancel();
		_modelPreview.Visible = false;
		_blockPreview.Visible = false;
	}

	private AssetPreview GetActivePreview()
	{
		if (_modelPreview.Visible)
		{
			return _modelPreview;
		}
		if (_blockPreview.Visible)
		{
			return _blockPreview;
		}
		return null;
	}

	private void LoadCopyFromAsset(AssetReference assetReference, CancellationToken cancellationToken)
	{
		_configEditor.AssetEditorOverlay.Backend.FetchAsset(assetReference, delegate(object asset, FormattedMessage error)
		{
			if (asset != null && !cancellationToken.IsCancellationRequested)
			{
				_configEditor.AssetEditorOverlay.Backend.FetchJsonAssetWithParents(assetReference, delegate(Dictionary<string, TrackedAsset> assets, FormattedMessage fetchParentsError)
				{
					//IL_0060: Unknown result type (might be due to invalid IL or missing references)
					//IL_006a: Unknown result type (might be due to invalid IL or missing references)
					//IL_0070: Expected O, but got Unknown
					if (assets != null && !cancellationToken.IsCancellationRequested)
					{
						SchemaNode schema = _configEditor.AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type].Schema;
						JObject val = (JObject)((JToken)(JObject)asset).DeepClone();
						_configEditor.AssetEditorOverlay.ApplyAssetInheritance(schema, val, assets, schema);
						if (ItemPreviewUtils.TryGetIconProperties(val, out var iconProperties))
						{
							SetupInputs(iconProperties.Rotation.Value, iconProperties.Translation.Value, iconProperties.Scale);
							Layout();
						}
					}
				});
			}
		});
	}

	private void UpdatePreviewZoom(float zoom, bool doLayout = true)
	{
		_previewFrame.Anchor.Width = (int)(74f * zoom);
		_previewFrame.Anchor.Height = (int)(74f * zoom);
		_previewArea.Anchor.Width = (int)(64f * zoom);
		_previewArea.Anchor.Height = (int)(64f * zoom);
		if (doLayout)
		{
			_previewFrame.Parent.Layout();
		}
	}

	public void OnTrackedAssetsChanged(TrackedAsset trackedAsset)
	{
		if (_modelPreview.IsMounted)
		{
			_modelPreview.OnTrackedAssetChanged(trackedAsset);
		}
		if (_blockPreview.IsMounted)
		{
			_blockPreview.OnTrackedAssetChanged(trackedAsset);
		}
	}

	public void Open()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		AssetEditorOverlay assetEditorOverlay = _configEditor.AssetEditorOverlay;
		SchemaNode schema = assetEditorOverlay.AssetTypeRegistry.AssetTypes[assetEditorOverlay.CurrentAsset.Type].Schema;
		JObject val = (JObject)((JToken)_configEditor.Value).DeepClone();
		assetEditorOverlay.ApplyAssetInheritance(schema, val, assetEditorOverlay.TrackedAssets, schema);
		string assetIdFromReference = assetEditorOverlay.GetAssetIdFromReference(assetEditorOverlay.CurrentAsset);
		AssetEditorAppEditor editor = _configEditor.AssetEditorOverlay.Interface.App.Editor;
		if (ItemPreviewUtils.TryGetIconProperties(val, out var iconProperties))
		{
			if (editor.BlockPreview != null)
			{
				_modelPreview.Visible = false;
				_blockPreview.Visible = true;
				_blockPreview.Setup(editor.BlockPreview, editor.PreviewCameraSettings);
				_filePathField.Value = "Icons/ItemsGenerated/" + assetIdFromReference + ".png";
				SetupInputs(iconProperties.Rotation.Value, iconProperties.Translation.Value, iconProperties.Scale);
				Desktop.SetLayer(4, this);
				return;
			}
			if (editor.ModelPreview != null)
			{
				_blockPreview.Visible = false;
				_modelPreview.Visible = true;
				_modelPreview.Setup(editor.ModelPreview, editor.PreviewCameraSettings);
				_filePathField.Value = "Icons/ItemsGenerated/" + assetIdFromReference + ".png";
				SetupInputs(iconProperties.Rotation.Value, iconProperties.Translation.Value, iconProperties.Scale);
				Desktop.SetLayer(4, this);
				return;
			}
		}
		_modelPreview.Visible = false;
		_blockPreview.Visible = false;
		_configEditor.AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, Desktop.Provider.GetText("ui.assetEditor.iconExporterModal.errors.invalidConfig"));
	}

	private void SetupInputs(Vector3 rotation, Vector2 translation, float scale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0062: Expected O, but got Unknown
		AssetEditorPreviewCameraSettings cameraSettings = new AssetEditorPreviewCameraSettings
		{
			ModelScale = scale * _itemScale,
			CameraPosition = new Vector3f(0f - translation.X, 0f - translation.Y, 0f),
			CameraOrientation = new Vector3f(0f - MathHelper.ToRadians(rotation.X), 0f - MathHelper.ToRadians(rotation.Y), 0f - MathHelper.ToRadians(rotation.Z))
		};
		GetActivePreview()?.UpdateCameraSettings(cameraSettings);
		_rotationXGroup.Find<Slider>("Slider").Value = (int)(rotation.X * 1000f);
		_rotationXGroup.Find<NumberField>("NumberField").Value = (decimal)rotation.X;
		_rotationYGroup.Find<Slider>("Slider").Value = (int)(rotation.Y * 1000f);
		_rotationYGroup.Find<NumberField>("NumberField").Value = (decimal)rotation.Y;
		_rotationZGroup.Find<Slider>("Slider").Value = (int)(rotation.Z * 1000f);
		_rotationZGroup.Find<NumberField>("NumberField").Value = (decimal)rotation.Z;
		_translationXGroup.Find<Slider>("Slider").Value = (int)(translation.X * 1000f);
		_translationXGroup.Find<NumberField>("NumberField").Value = (decimal)translation.X;
		_translationYGroup.Find<Slider>("Slider").Value = (int)(translation.Y * 1000f);
		_translationYGroup.Find<NumberField>("NumberField").Value = (decimal)translation.Y;
		_scaleGroup.Find<Slider>("Slider").Value = (int)(scale * 1000f);
		_scaleGroup.Find<NumberField>("NumberField").Value = (decimal)scale;
	}

	private void Reset()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		AssetEditorOverlay assetEditorOverlay = _configEditor.AssetEditorOverlay;
		SchemaNode schema = assetEditorOverlay.AssetTypeRegistry.AssetTypes[assetEditorOverlay.CurrentAsset.Type].Schema;
		JObject val = (JObject)((JToken)(JObject)assetEditorOverlay.TrackedAssets[_configEditor.CurrentAsset.FilePath].Data).DeepClone();
		assetEditorOverlay.ApplyAssetInheritance(schema, val, assetEditorOverlay.TrackedAssets, schema);
		if (ItemPreviewUtils.TryGetDefaultIconProperties(val, out var iconProperties))
		{
			SetupInputs(iconProperties.Rotation.Value, iconProperties.Translation.Value, iconProperties.Scale);
			Layout();
		}
	}

	private void HandleCreateOrUpdateCallback(FormattedMessage error)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected O, but got Unknown
		//IL_0137: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Expected O, but got Unknown
		if (error != null)
		{
			_configEditor.AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, error);
		}
		else if (_filePathField.Value.StartsWith("Icons/ItemsGenerated/"))
		{
			JObject val = new JObject();
			val.Add("Scale", JToken.op_Implicit(_scaleGroup.Find<NumberField>("NumberField").Value));
			JArray val2 = new JArray();
			val2.Add(JToken.op_Implicit(_rotationXGroup.Find<NumberField>("NumberField").Value));
			val2.Add(JToken.op_Implicit(_rotationYGroup.Find<NumberField>("NumberField").Value));
			val2.Add(JToken.op_Implicit(_rotationZGroup.Find<NumberField>("NumberField").Value));
			val.Add("Rotation", (JToken)val2);
			JArray val3 = new JArray();
			val3.Add(JToken.op_Implicit(_translationXGroup.Find<NumberField>("NumberField").Value));
			val3.Add(JToken.op_Implicit(_translationYGroup.Find<NumberField>("NumberField").Value));
			val.Add("Translation", (JToken)val3);
			JObject value = val;
			JObject value2 = _configEditor.Value;
			ConfigEditor configEditor = _configEditor;
			PropertyPath path = PropertyPath.FromString("Icon");
			JToken value3 = JToken.op_Implicit(_filePathField.Value);
			JToken obj = value2["Icon"];
			configEditor.OnChangeValue(path, value3, (obj != null) ? obj.DeepClone() : null, new AssetEditorRebuildCaches
			{
				ItemIcons = true
			}, withheldCommand: true, insertItem: false, updateDisplayedValue: true);
			ConfigEditor configEditor2 = _configEditor;
			PropertyPath path2 = PropertyPath.FromString("IconProperties");
			JToken obj2 = value2["IconProperties"];
			configEditor2.OnChangeValue(path2, (JToken)(object)value, (obj2 != null) ? obj2.DeepClone() : null, null, withheldCommand: true);
			_configEditor.SubmitPendingUpdateCommands();
			_configEditor.Layout();
		}
	}

	private void Export()
	{
		string fullPath = Path.GetFullPath(Path.Combine(Paths.BuiltInAssets, "Common"));
		string fullPath2 = Path.GetFullPath(Path.Combine(Paths.BuiltInAssets, "Common", _filePathField.Value));
		if (!Paths.IsSubPathOf(fullPath2, fullPath))
		{
			Logger.Warn<string, string>("Path must resolve to within common assets directory: {0} in {1}", fullPath2, fullPath);
			return;
		}
		AssetPreview activePreview = GetActivePreview();
		byte[] pixels = activePreview.Capture(128, 128);
		pixels = BilinearFilter.ApplyFilter(pixels, 128, 64);
		Image data = new Image(64, 64, pixels);
		string assetPathWithCommon = AssetPathUtils.GetAssetPathWithCommon(_filePathField.Value);
		AssetReference assetReference = new AssetReference("Texture", assetPathWithCommon);
		AssetEditorOverlay assetEditorOverlay = _configEditor.AssetEditorOverlay;
		if (assetEditorOverlay.Assets.TryGetFile(assetReference.FilePath, out var _))
		{
			assetEditorOverlay.Backend.UpdateAsset(assetReference, data, HandleCreateOrUpdateCallback);
		}
		else
		{
			assetEditorOverlay.Backend.CreateAsset(assetReference, data, null, openInTab: false, HandleCreateOrUpdateCallback);
		}
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
	}

	public override Element HitTest(Point position)
	{
		return base.HitTest(position) ?? this;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (activate && !_container.AnchoredRectangle.Contains(Desktop.MousePosition))
		{
			Dismiss();
		}
	}
}
