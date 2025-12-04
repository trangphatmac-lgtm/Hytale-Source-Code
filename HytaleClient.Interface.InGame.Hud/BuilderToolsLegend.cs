using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Data.Items;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Modules.BuilderTools.Tools;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud;

internal class BuilderToolsLegend : InterfaceComponent
{
	private readonly InGameView _inGameView;

	private LabelStyle _rowHintTextStyle;

	private int _rowHintIconSize;

	private Label _toggleLegendKey;

	private Group _container;

	private Group _hintPickMaterial;

	private Group _hintSetMask;

	private Group _hintAddMask;

	private Group _hintFavoriteMaterials;

	private Group _hintUndo;

	private Group _hintRedo;

	private Group _hintAddRemoveFavoriteMaterial;

	private ItemGrid _selectedMaterial;

	private readonly Dictionary<Input.MouseButton, PatchStyle> _mouseHintIcons = new Dictionary<Input.MouseButton, PatchStyle>();

	public BuilderToolsLegend(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		_mouseHintIcons.Clear();
		Interface.TryGetDocument("InGame/Hud/BuilderToolsLegend/Legend.ui", out var document);
		_rowHintTextStyle = document.ResolveNamedValue<LabelStyle>(Desktop.Provider, "RowHintTextStyle");
		_rowHintIconSize = document.ResolveNamedValue<int>(Desktop.Provider, "RowHintIconSize");
		_mouseHintIcons.Add(Input.MouseButton.SDL_BUTTON_LEFT, document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "RowHintIconMouseLeftClick"));
		_mouseHintIcons.Add(Input.MouseButton.SDL_BUTTON_MIDDLE, document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "RowHintIconMouseMiddleClick"));
		_mouseHintIcons.Add(Input.MouseButton.SDL_BUTTON_RIGHT, document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "RowHintIconMouseRightClick"));
		_container = new Group(Desktop, this);
		UIFragment uIFragment = document.Instantiate(Desktop, _container);
		_toggleLegendKey = uIFragment.Get<Label>("ToggleLegendKey");
		_hintPickMaterial = uIFragment.Get<Group>("HintPickMaterial");
		_hintSetMask = uIFragment.Get<Group>("HintSetMask");
		_hintAddMask = uIFragment.Get<Group>("HintAddMask");
		_hintFavoriteMaterials = uIFragment.Get<Group>("HintFavoriteMaterials");
		_selectedMaterial = uIFragment.Get<ItemGrid>("SelectedMaterial");
		_selectedMaterial.Slots = new ItemGridSlot[1];
		_hintUndo = uIFragment.Get<Group>("HintUndo");
		_hintRedo = uIFragment.Get<Group>("HintRedo");
		_hintAddRemoveFavoriteMaterial = uIFragment.Get<Group>("HintAddRemoveFavoriteMaterial");
		if (base.IsMounted)
		{
			UpdateInputHints(doClear: false);
		}
	}

	public void SetSelectedMaterial(string material)
	{
		_selectedMaterial.Slots[0] = new ItemGridSlot((material == null) ? null : new ClientItemStack(material));
	}

	public void SetSelectedMaterial(ClientItemStack stack)
	{
		_selectedMaterial.Slots[0] = new ItemGridSlot(stack);
	}

	public void ResetState()
	{
		_selectedMaterial.Slots = new ItemGridSlot[1];
		_hintPickMaterial.Clear();
		_hintSetMask.Clear();
		_hintAddMask.Clear();
		_hintFavoriteMaterials.Clear();
		_hintUndo.Clear();
		_hintRedo.Clear();
		_hintAddRemoveFavoriteMaterial.Clear();
	}

	public void UpdateInputHints(bool doClear = true, bool doLayout = false)
	{
		if (doClear)
		{
			_hintPickMaterial.Clear();
			_hintSetMask.Clear();
			_hintAddMask.Clear();
			_hintFavoriteMaterials.Clear();
			_hintUndo.Clear();
			_hintRedo.Clear();
			_hintAddRemoveFavoriteMaterial.Clear();
		}
		InputBindings inputBindings = _inGameView.InGame.Instance.App.Settings.InputBindings;
		_toggleLegendKey.Text = " [" + inputBindings.ToggleBuilderToolsLegend.BoundInputLabel + "]";
		SetHintText(_hintFavoriteMaterials, inputBindings.TertiaryItemAction.BoundInputLabel);
		SetHintText(_hintUndo, inputBindings.UndoItemAction.BoundInputLabel);
		SetHintText(_hintRedo, inputBindings.RedoItemAction.BoundInputLabel);
		SetHintText(_hintAddRemoveFavoriteMaterial, inputBindings.AddRemoveFavoriteMaterialItemAction.BoundInputLabel);
		if (inputBindings.PickBlock.MouseButton.HasValue && _mouseHintIcons.TryGetValue(inputBindings.PickBlock.MouseButton.Value, out var value))
		{
			SetHintIcon(_hintPickMaterial, value);
			SetHintIcon(_hintSetMask, value);
			SetHintIcon(_hintAddMask, value);
		}
		else
		{
			string boundInputLabel = inputBindings.PickBlock.BoundInputLabel;
			SetHintText(_hintPickMaterial, boundInputLabel);
			SetHintText(_hintSetMask, boundInputLabel);
			SetHintText(_hintAddMask, boundInputLabel);
		}
		if (doLayout)
		{
			Layout();
		}
	}

	private void SetHintText(Group parent, string text)
	{
		new Label(Desktop, parent)
		{
			Style = _rowHintTextStyle,
			Text = text
		};
	}

	private void SetHintIcon(Group parent, PatchStyle icon)
	{
		new Group(Desktop, parent)
		{
			Anchor = new Anchor
			{
				Width = _rowHintIconSize,
				Height = _rowHintIconSize
			},
			Background = icon
		};
	}

	public void ActiveToolChange(ToolInstance toolInstance)
	{
		SetSelectedMaterial(toolInstance?.BrushData?.Material);
		UpdateInputHints(doClear: true, doLayout: true);
	}
}
