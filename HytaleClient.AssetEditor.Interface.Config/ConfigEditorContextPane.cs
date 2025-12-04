using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.AssetEditor.Interface.Previews;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class ConfigEditorContextPane : Element
{
	private readonly AssetEditorOverlay _assetEditorOverlay;

	public readonly DayTimeControls DayTimeControls;

	private DynamicPane _previewGroup;

	private ModelPreview _modelPreview;

	private BlockPreview _blockPreview;

	private Group _buttonsGroup;

	private Group _sectionsGroup;

	private readonly Dictionary<string, TextButton> _sectionButtons = new Dictionary<string, TextButton>();

	private TextButton.TextButtonStyle _style;

	private TextButton.TextButtonStyle _activeStyle;

	private string _activeCategory;

	public ConfigEditorContextPane(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
		DayTimeControls = new DayTimeControls(assetEditorOverlay);
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/ConfigEditorContextPane.ui", out var document);
		Desktop.Provider.TryGetDocument("AssetEditor/ConfigEditorContextPaneSectionButton.ui", out var document2);
		_style = document2.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "Style");
		_activeStyle = document2.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "ActiveStyle");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_buttonsGroup = uIFragment.Get<Group>("Buttons");
		_sectionsGroup = uIFragment.Get<Group>("Sections");
		_previewGroup = uIFragment.Get<DynamicPane>("PreviewGroup");
		_previewGroup.MouseButtonReleased = delegate
		{
			if (_assetEditorOverlay.CurrentAsset.Type != null && _assetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(_assetEditorOverlay.CurrentAsset.Type, out var value))
			{
				switch (value.Preview)
				{
				case AssetTypeConfig.PreviewType.Item:
					_assetEditorOverlay.UpdatePaneSize(AssetEditorSettings.Panes.ConfigEditorSidebarPreviewItem, _previewGroup.Anchor.Height.Value);
					break;
				case AssetTypeConfig.PreviewType.Model:
					_assetEditorOverlay.UpdatePaneSize(AssetEditorSettings.Panes.ConfigEditorSidebarPreviewModel, _previewGroup.Anchor.Height.Value);
					break;
				}
			}
		};
		_modelPreview = new ModelPreview(_assetEditorOverlay, _previewGroup);
		_modelPreview.Visible = false;
		_blockPreview = new BlockPreview(_assetEditorOverlay, _previewGroup);
		_blockPreview.Visible = false;
		if (base.IsMounted)
		{
			Update();
		}
	}

	public void SetActiveCategory(string key, bool doLayout = true)
	{
		if (_activeCategory != null && _sectionButtons.TryGetValue(_activeCategory, out var value))
		{
			value.Style = _style;
			if (doLayout)
			{
				value.Layout();
			}
		}
		_activeCategory = key;
		if (_activeCategory != null && _sectionButtons.TryGetValue(_activeCategory, out var value2))
		{
			value2.Style = _activeStyle;
			if (doLayout)
			{
				value2.Layout();
			}
		}
	}

	private void SendAction(string action)
	{
		_assetEditorOverlay.Backend.OnSidebarButtonActivated(action);
	}

	public void ResetState()
	{
		DayTimeControls.ResetState();
	}

	public void Update()
	{
		_sectionButtons.Clear();
		_sectionsGroup.Clear();
		_buttonsGroup.Clear();
		if (_assetEditorOverlay.CurrentAsset.Type != null && _assetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(_assetEditorOverlay.CurrentAsset.Type, out var value))
		{
			if (_assetEditorOverlay.Mode == AssetEditorOverlay.EditorMode.Editor && value.Schema != null)
			{
				UpdateCategories();
			}
			if (value.SidebarButtons != null)
			{
				Desktop.Provider.TryGetDocument("AssetEditor/ConfigEditorContextPaneButton.ui", out var document);
				foreach (AssetTypeConfig.Button sidebarButton in value.SidebarButtons)
				{
					TextButton textButton = document.Instantiate(Desktop, _buttonsGroup).Get<TextButton>("Button");
					textButton.Text = _assetEditorOverlay.Backend.GetButtonText(sidebarButton.TextId ?? "");
					textButton.Activating = delegate
					{
						SendAction(sidebarButton.Action);
					};
				}
			}
			else if (value.HasFeature(AssetTypeConfig.EditorFeature.WeatherDaytimeBar))
			{
				_buttonsGroup.Add(DayTimeControls);
			}
		}
		UpdatePreview(doLayout: false);
		Layout();
	}

	public void UpdatePreview(bool doLayout = true)
	{
		if (_assetEditorOverlay.CurrentAsset.Type != null && _assetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(_assetEditorOverlay.CurrentAsset.Type, out var value))
		{
			if (value.Preview != 0)
			{
				_previewGroup.Anchor.Height = ((value.Preview == AssetTypeConfig.PreviewType.Item) ? 280 : 380);
				_previewGroup.Visible = true;
				AssetEditorAppEditor editor = _assetEditorOverlay.Interface.App.Editor;
				if (editor.ModelPreview != null)
				{
					_modelPreview.Setup(editor.ModelPreview, editor.PreviewCameraSettings);
					_modelPreview.Visible = true;
					_blockPreview.Visible = false;
				}
				else if (editor.BlockPreview != null)
				{
					_blockPreview.Setup(editor.BlockPreview, editor.PreviewCameraSettings);
					_blockPreview.Visible = true;
					_modelPreview.Visible = false;
				}
			}
			else
			{
				_previewGroup.Visible = false;
			}
		}
		else
		{
			_previewGroup.Visible = false;
		}
		_previewGroup.Anchor.Height = _assetEditorOverlay.Interface.App.Settings.PaneSizes[AssetEditorSettings.Panes.ConfigEditorSidebarPreviewModel];
		if (doLayout)
		{
			Layout();
		}
	}

	public void UpdateCategories()
	{
		_sectionButtons.Clear();
		_sectionsGroup.Clear();
		_activeCategory = _assetEditorOverlay.ConfigEditor.State.ActiveCategory.ToString();
		if (_assetEditorOverlay.Mode != 0 || _assetEditorOverlay.ConfigEditor.Categories.Count <= 1)
		{
			return;
		}
		Desktop.Provider.TryGetDocument("AssetEditor/ConfigEditorContextPaneSectionButton.ui", out var doc);
		MakeSectionButton("", Desktop.Provider.GetText("ui.assetEditor.configEditor.showAllProperties"));
		new Group(Desktop, _sectionsGroup)
		{
			Anchor = new Anchor
			{
				Height = 1
			},
			Background = new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 50))
		};
		foreach (KeyValuePair<string, string> category in _assetEditorOverlay.ConfigEditor.Categories)
		{
			MakeSectionButton(category.Key, category.Value);
		}
		void MakeSectionButton(string path, string text)
		{
			int num = path.Split(new char[1] { '.' }).Length;
			UIFragment uIFragment = doc.Instantiate(Desktop, _sectionsGroup);
			TextButton textButton = uIFragment.Get<TextButton>("Button");
			textButton.Padding.Left = textButton.Padding.Left.GetValueOrDefault() + (num - 1) * 16;
			textButton.Text = text;
			textButton.Activating = delegate
			{
				_assetEditorOverlay.ConfigEditor.State.ActiveCategory = PropertyPath.FromString(path);
				_assetEditorOverlay.ConfigEditor.Update();
				_assetEditorOverlay.ConfigEditor.ScrollToTop();
				SetActiveCategory(path);
			};
			uIFragment.Get<Group>("Icon").Visible = num > 1;
			if (path == _activeCategory)
			{
				textButton.Style = _activeStyle;
			}
			_sectionButtons[path] = textButton;
		}
	}

	public void OnTrackedAssetChanged(TrackedAsset trackedAsset)
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
}
