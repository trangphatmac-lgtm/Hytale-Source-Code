using System.Collections.Generic;
using System.Linq;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Modals;

internal class FilterModal : Element
{
	private readonly AssetEditorOverlay _assetEditorOverlay;

	private Group _container;

	private Group _entriesContainer;

	private TextField _searchInput;

	private Button.ButtonStyle _defaultStyle;

	private Button.ButtonStyle _selectedStyle;

	private Dictionary<string, Button> _buttons = new Dictionary<string, Button>();

	public FilterModal(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/FilterModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("Container");
		_entriesContainer = uIFragment.Get<Group>("Entries");
		_searchInput = uIFragment.Get<TextField>("SearchInput");
		_searchInput.ValueChanged = OnSearchChanged;
		_searchInput.KeyDown = OnSearchKeyDown;
		uIFragment.Get<TextButton>("CloseButton").Activating = Dismiss;
		uIFragment.Get<TextButton>("SelectAllButton").Activating = OnActivateSelectAll;
		uIFragment.Get<TextButton>("DeselectAllButton").Activating = OnActivateDeselectAll;
		if (base.IsMounted)
		{
			Setup();
		}
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

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		base.OnKeyDown(keycode, repeat);
		HandleKeyShortcuts(keycode);
	}

	private void HandleKeyShortcuts(SDL_Keycode keycode)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		if (!Desktop.IsShortcutKeyDown)
		{
			return;
		}
		if ((int)keycode == 97)
		{
			if (Desktop.IsShiftKeyDown)
			{
				OnActivateDeselectAll();
			}
			else
			{
				OnActivateSelectAll();
			}
		}
	}

	private void OnSearchChanged()
	{
		Setup();
	}

	private void OnSearchKeyDown(SDL_Keycode keycode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		HandleKeyShortcuts(keycode);
	}

	private void OnActivateDeselectAll()
	{
		_assetEditorOverlay.GetDisplayedAssetTypes().Clear();
		ApplyButtonStyles();
		_assetEditorOverlay.OnDisplayedAssetTypesChanged();
	}

	private void OnActivateSelectAll()
	{
		HashSet<string> displayedAssetTypes = _assetEditorOverlay.GetDisplayedAssetTypes();
		displayedAssetTypes.Clear();
		foreach (AssetTypeConfig value in _assetEditorOverlay.AssetTypeRegistry.AssetTypes.Values)
		{
			if (value.AssetTree == _assetEditorOverlay.Interface.App.Settings.ActiveAssetTree && !value.IsVirtual)
			{
				displayedAssetTypes.Add(value.Id);
			}
		}
		ApplyButtonStyles();
		_assetEditorOverlay.OnDisplayedAssetTypesChanged();
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
	}

	public void ResetState()
	{
		_entriesContainer.Clear();
	}

	public void Setup()
	{
		_entriesContainer.Clear();
		_buttons.Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/FilterEntry.ui", out var document);
		_defaultStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "DefaultStyle");
		_selectedStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "SelectedStyle");
		IOrderedEnumerable<string> orderedEnumerable = _assetEditorOverlay.AssetTypeRegistry.AssetTypes.Keys.OrderBy((string k) => _assetEditorOverlay.AssetTypeRegistry.AssetTypes[k].Name);
		HashSet<string> displayedAssetTypes = _assetEditorOverlay.GetDisplayedAssetTypes();
		string text = _searchInput.Value.Trim().ToLowerInvariant();
		foreach (string item in orderedEnumerable)
		{
			AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[item];
			if (assetTypeConfig.AssetTree == _assetEditorOverlay.Interface.App.Settings.ActiveAssetTree && !assetTypeConfig.IsVirtual && (!(text != "") || assetTypeConfig.Name.ToLowerInvariant().Contains(text)))
			{
				UIFragment uIFragment = document.Instantiate(Desktop, _entriesContainer);
				Button button = uIFragment.Get<Button>("Button");
				button.Activating = delegate
				{
					OnButtonActivate(assetTypeConfig.Id);
				};
				button.DoubleClicking = Dismiss;
				button.Style = (displayedAssetTypes.Contains(assetTypeConfig.Id) ? _selectedStyle : _defaultStyle);
				uIFragment.Get<Label>("NameLabel").Text = assetTypeConfig.Name;
				uIFragment.Get<Group>("Icon").Background = assetTypeConfig.Icon;
				_buttons.Add(assetTypeConfig.Id, button);
			}
		}
		if (base.IsMounted)
		{
			_entriesContainer.Layout();
		}
	}

	private void ApplyButtonStyles()
	{
		HashSet<string> displayedAssetTypes = _assetEditorOverlay.GetDisplayedAssetTypes();
		foreach (KeyValuePair<string, Button> button in _buttons)
		{
			button.Value.Style = (displayedAssetTypes.Contains(button.Key) ? _selectedStyle : _defaultStyle);
			button.Value.Layout();
		}
	}

	private void OnButtonActivate(string key)
	{
		HashSet<string> displayedAssetTypes = _assetEditorOverlay.GetDisplayedAssetTypes();
		if (Desktop.IsShiftKeyDown)
		{
			if (displayedAssetTypes.Contains(key))
			{
				displayedAssetTypes.Remove(key);
			}
			else
			{
				displayedAssetTypes.Add(key);
			}
		}
		else if (displayedAssetTypes.Count == 1 && displayedAssetTypes.Contains(key))
		{
			displayedAssetTypes.Clear();
		}
		else
		{
			displayedAssetTypes.Clear();
			displayedAssetTypes.Add(key);
		}
		ApplyButtonStyles();
		_assetEditorOverlay.OnDisplayedAssetTypesChanged();
	}

	public void Open()
	{
		if (Desktop.GetLayer(4) == null && _assetEditorOverlay.AssetTypeRegistry.AssetTypes.Count != 0)
		{
			_searchInput.Value = "";
			Setup();
			Desktop.SetLayer(4, this);
		}
	}
}
