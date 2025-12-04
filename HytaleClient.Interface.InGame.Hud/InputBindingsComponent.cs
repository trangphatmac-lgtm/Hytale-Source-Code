using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Hud;

internal class InputBindingsComponent : InterfaceComponent
{
	private InGameView _inGameView;

	private Button _bindingQuests;

	private Button _bindingBook;

	private Button _bindingMap;

	private Button _bindingInventory;

	private Button _bindingSocial;

	private PatchStyle _backgroundStyle;

	private PatchStyle _backgroundStyleActive;

	public InputBindingsComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/InputBindings.ui", out var document);
		_backgroundStyle = document.ResolveNamedValue<PatchStyle>(Interface, "BackgroundStyle");
		_backgroundStyleActive = document.ResolveNamedValue<PatchStyle>(Interface, "BackgroundStyleActive");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_bindingQuests = uIFragment.Get<Button>("BindingQuests");
		_bindingBook = uIFragment.Get<Button>("BindingBook");
		_bindingMap = uIFragment.Get<Button>("BindingMap");
		_bindingMap.Activating = delegate
		{
			_inGameView.InGame.SetCurrentPage((Page)6, wasOpenedWithInteractionBinding: false, playSound: true);
		};
		_bindingInventory = uIFragment.Get<Button>("BindingInventory");
		_bindingInventory.Activating = delegate
		{
			_inGameView.InGame.SetCurrentPage((Page)2, wasOpenedWithInteractionBinding: false, playSound: true);
		};
		_bindingSocial = uIFragment.Get<Button>("BindingSocial");
		if (base.IsMounted)
		{
			UpdateBindings(doLayout: false);
		}
	}

	protected override void OnMounted()
	{
		UpdateBindings(doLayout: false);
	}

	public void ResetState()
	{
		UpdateBindings(doLayout: false);
	}

	public void OnWorldMapSettingsUpdated()
	{
		UpdateBindings();
	}

	public void UpdateBindings(bool doLayout = true)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Invalid comparison between Unknown and I4
		if (!Interface.HasMarkupError)
		{
			UpdateState(_bindingQuests, "Quests", isActive: false, "???");
			UpdateState(_bindingBook, "Book", isActive: false, "???");
			UpdateState(_bindingInventory, "Inventory", (int)_inGameView.InGame.CurrentPage == 2 && _inGameView.InventoryPage.IsFieldcraft, _inGameView.Interface.App.Settings.InputBindings.OpenInventory.BoundInputLabel);
			UpdateState(_bindingMap, "Map", (int)_inGameView.InGame.CurrentPage == 6, _inGameView.Interface.App.Settings.InputBindings.OpenMap.BoundInputLabel);
			UpdateState(_bindingSocial, "Social", isActive: false, "???");
			_bindingMap.Visible = _inGameView.InGame.Instance.WorldMapModule.IsWorldMapEnabled;
			if (doLayout)
			{
				Layout();
			}
		}
		void UpdateState(Button button, string id, bool isActive, string label)
		{
			button.Background = (isActive ? _backgroundStyleActive : _backgroundStyle);
			button.Find<Label>("Label").Text = label;
			button.Find<Group>("Icon").Background = new PatchStyle("InGame/Hud/InputBindingIcon" + id + (isActive ? "Active" : "") + ".png");
		}
	}
}
