using System;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels;

internal class ProcessingPanel : WindowPanel
{
	private Label _titleLabel;

	private Label _descriptiveLabel;

	private Group _fuelContainer;

	private Group _fuelInputContainer;

	private Group _inputProcessingBars;

	private ItemGrid _fuelItemGrid;

	private ItemGrid _inputItemGrid;

	private ItemGrid _outputItemGrid;

	private TextButton _onOffButton;

	private TextButton.TextButtonStyle _offButtonStyle;

	private TextButton.TextButtonStyle _onButtonStyle;

	private PatchStyle[] _inputSlotIcons;

	private PatchStyle[] _fuelSlotIcons;

	private bool _isOn;

	private PatchStyle _inputSlotActiveOverlay;

	private PatchStyle _fuelSlotActiveOverlay;

	public bool[] CompatibleInputSlots { get; private set; }

	public int FuelSlotCount => _fuelItemGrid.Slots.Length;

	public ProcessingPanel(InGameView inGameView, Element parent = null)
		: base(inGameView, parent)
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/ProcessingPanel.ui", out var document);
		_offButtonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Interface, "OffButtonStyle");
		_onButtonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Interface, "OnButtonStyle");
		_fuelSlotActiveOverlay = document.ResolveNamedValue<PatchStyle>(Interface, "FuelSlotActiveOverlay");
		_inputSlotActiveOverlay = document.ResolveNamedValue<PatchStyle>(Interface, "InputSlotActiveOverlay");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_titleLabel = uIFragment.Get<Label>("TitleLabel");
		_fuelContainer = uIFragment.Get<Group>("FuelContainer");
		_fuelInputContainer = uIFragment.Get<Group>("FuelInputContainer");
		_inputProcessingBars = uIFragment.Get<Group>("InputProcessingBars");
		_fuelItemGrid = uIFragment.Get<ItemGrid>("FuelItemGrid");
		_fuelItemGrid.Slots = new ItemGridSlot[1];
		_fuelItemGrid.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(_inventoryWindow.Id, slotIndex, button);
		};
		_fuelItemGrid.SlotDoubleClicking = delegate(int slotIndex)
		{
			_inGameView.HandleInventoryDoubleClick(_inventoryWindow.Id, slotIndex);
		};
		_fuelItemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_fuelItemGrid, _inventoryWindow.Id, targetSlotIndex, sourceItemGrid, dragData);
		};
		_fuelItemGrid.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(_inventoryWindow.Id, slotIndex, button);
		};
		_fuelItemGrid.SlotMouseEntered = OnFuelSlotMouseEntered;
		_fuelItemGrid.SlotMouseExited = OnFuelSlotMouseExited;
		_inputItemGrid = uIFragment.Get<ItemGrid>("InputItemGrid");
		_inputItemGrid.Slots = new ItemGridSlot[0];
		_inputItemGrid.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(_inventoryWindow.Id, slotIndex + _fuelItemGrid.Slots.Length, button);
		};
		_inputItemGrid.SlotDoubleClicking = delegate(int slotIndex)
		{
			_inGameView.HandleInventoryDoubleClick(_inventoryWindow.Id, slotIndex + _fuelItemGrid.Slots.Length);
		};
		_inputItemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_inputItemGrid, _inventoryWindow.Id, targetSlotIndex + _fuelItemGrid.Slots.Length, sourceItemGrid, dragData);
		};
		_inputItemGrid.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(_inventoryWindow.Id, slotIndex + _fuelItemGrid.Slots.Length, button);
		};
		_inputItemGrid.SlotMouseEntered = OnInputSlotMouseEntered;
		_inputItemGrid.SlotMouseExited = OnInputSlotMouseExited;
		_outputItemGrid = uIFragment.Get<ItemGrid>("OutputItemGrid");
		_outputItemGrid.Slots = new ItemGridSlot[0];
		_outputItemGrid.SlotClicking = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryClick(_inventoryWindow.Id, slotIndex + _fuelItemGrid.Slots.Length + _inputItemGrid.Slots.Length, button);
		};
		_outputItemGrid.SlotDoubleClicking = delegate(int slotIndex)
		{
			_inGameView.HandleInventoryDoubleClick(_inventoryWindow.Id, slotIndex + _fuelItemGrid.Slots.Length + _inputItemGrid.Slots.Length);
		};
		_outputItemGrid.Dropped = delegate(int targetSlotIndex, Element sourceItemGrid, ItemGrid.ItemDragData dragData)
		{
			_inGameView.HandleInventoryDragEnd(_outputItemGrid, _inventoryWindow.Id, targetSlotIndex + _fuelItemGrid.Slots.Length + _inputItemGrid.Slots.Length, sourceItemGrid, dragData);
		};
		_outputItemGrid.DragCancelled = delegate(int slotIndex, int button)
		{
			_inGameView.HandleInventoryDropItem(_inventoryWindow.Id, slotIndex + _fuelItemGrid.Slots.Length + _inputItemGrid.Slots.Length, button);
		};
		_descriptiveLabel = uIFragment.Get<Group>("DescriptiveLabel").Find<Label>("PanelTitle");
		_onOffButton = uIFragment.Get<TextButton>("OnOffButton");
		_onOffButton.Activating = delegate
		{
			SetOn(!_isOn);
		};
		_onOffButton.Text = Desktop.Provider.GetText("ui.windows.processing.turn" + (_isOn ? "Off" : "On"));
		_onOffButton.Style = (_isOn ? _onButtonStyle : _offButtonStyle);
	}

	public void OnSetStacks()
	{
		Update();
	}

	private void OnFuelSlotMouseEntered(int slotIndex)
	{
		_inGameView.HandleItemSlotMouseEntered(_inventoryWindow.Id, slotIndex);
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}

	private void OnInputSlotMouseEntered(int slotIndex)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		_inGameView.HandleItemSlotMouseEntered(_inventoryWindow.Id, slotIndex + FuelSlotCount);
		JArray val = (JArray)_inventoryWindow.WindowData["inventoryHints"];
		CompatibleInputSlots = new bool[_inGameView.StorageStacks.Length + _inGameView.HotbarStacks.Length];
		if (val != null)
		{
			foreach (JToken item in val)
			{
				CompatibleInputSlots[(int)item] = true;
			}
		}
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}

	private void OnFuelSlotMouseExited(int slotIndex)
	{
		_inGameView.HandleItemSlotMouseExited(_inventoryWindow.Id, slotIndex);
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}

	private void OnInputSlotMouseExited(int slotIndex)
	{
		_inGameView.HandleItemSlotMouseExited(_inventoryWindow.Id, slotIndex + FuelSlotCount);
		_inGameView.InventoryPage.StoragePanel.UpdateGrid();
		_inGameView.HotbarComponent.SetupGrid();
	}

	protected override void Setup()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		_titleLabel.Text = Desktop.Provider.GetText(((string)_inventoryWindow.WindowData["name"]) ?? "");
		JArray val = (JArray)_inventoryWindow.WindowData["fuel"];
		_fuelContainer.Visible = val != null;
		_fuelItemGrid.InventorySectionId = _inventoryWindow.Id;
		_inputItemGrid.InventorySectionId = _inventoryWindow.Id;
		_outputItemGrid.InventorySectionId = _inventoryWindow.Id;
		JArray val2 = (JArray)_inventoryWindow.WindowData["input"];
		_inputSlotIcons = new PatchStyle[((JContainer)val2).Count];
		for (int i = 0; i < _inputSlotIcons.Length; i++)
		{
			string text = (string)val2[i][(object)"icon"];
			if (text != null && _inGameView.TryMountAssetTexture(text, out var textureArea))
			{
				_inputSlotIcons[i] = new PatchStyle(textureArea);
			}
			else
			{
				_inputSlotIcons[i] = null;
			}
		}
		_fuelSlotIcons = new PatchStyle[(val != null) ? ((JContainer)val).Count : 0];
		for (int j = 0; j < _fuelSlotIcons.Length; j++)
		{
			string text2 = (string)val[j][(object)"icon"];
			if (text2 != null && _inGameView.TryMountAssetTexture(text2, out var textureArea2))
			{
				_fuelSlotIcons[j] = new PatchStyle(textureArea2);
			}
			else
			{
				_fuelSlotIcons[j] = null;
			}
		}
		int count = ((JContainer)val2).Count;
		int num = (int)_inventoryWindow.WindowData["outputSlotsCount"];
		_inputItemGrid.Slots = new ItemGridSlot[count];
		_inputItemGrid.Parent.Anchor.Height = _inputItemGrid.Style.SlotSize * count + _inputItemGrid.Style.SlotSpacing * (count - 1) + _inputItemGrid.Parent.Padding.Vertical + _inputItemGrid.Padding.Vertical;
		_inputProcessingBars.Clear();
		Interface.TryGetDocument("InGame/Pages/Inventory/ProcessingBar.ui", out var document);
		for (int k = 0; k < count; k++)
		{
			Group group = document.Instantiate(Desktop, _inputProcessingBars).Get<Group>("ProcessingBarContainer");
			group.Anchor.Top = k * (_inputItemGrid.Style.SlotSize + _inputItemGrid.Style.SlotSpacing) - 1;
		}
		_outputItemGrid.SlotsPerRow = ((num <= 3) ? 1 : 2);
		_outputItemGrid.Slots = new ItemGridSlot[num];
		_outputItemGrid.Parent.Anchor.Width = _outputItemGrid.Style.SlotSize * _outputItemGrid.SlotsPerRow + _outputItemGrid.Style.SlotSpacing * (_outputItemGrid.SlotsPerRow - 1) + _outputItemGrid.Padding.Vertical;
		int num2 = (int)System.Math.Ceiling((float)num / (float)_outputItemGrid.SlotsPerRow);
		_outputItemGrid.Parent.Anchor.Height = _outputItemGrid.Style.SlotSize * num2 + _outputItemGrid.Style.SlotSpacing * (num2 - 1) + _outputItemGrid.Padding.Vertical;
		int num3 = ((val != null) ? ((JContainer)val).Count : 0);
		_fuelInputContainer.Anchor.Width = _fuelItemGrid.Style.SlotSize * num3;
		_fuelItemGrid.Slots = new ItemGridSlot[num3];
		_isOn = (bool)_inventoryWindow.WindowData["active"];
		_onOffButton.Text = Desktop.Provider.GetText("ui.windows.processing.turn" + (_isOn ? "Off" : "On"));
		_onOffButton.Style = (_isOn ? _onButtonStyle : _offButtonStyle);
		_descriptiveLabel.Text = Desktop.Provider.GetText(string.Format("items.{0}.bench.descriptiveLabel", _inventoryWindow.WindowData["blockItemId"]), null, returnFallback: false) ?? Desktop.Provider.GetText("ui.windows.processing.descriptiveLabel");
		Update();
	}

	protected override void Update()
	{
		if ((bool)_inventoryWindow.WindowData["active"] != _isOn)
		{
			SetOn((bool)_inventoryWindow.WindowData["active"], sendPacket: false);
		}
		float num = _inventoryWindow.WindowData["progress"].ToObject<float>();
		for (int i = 0; i < _inputItemGrid.Slots.Length; i++)
		{
			ClientItemStack clientItemStack = _inventoryWindow.Inventory[i + _fuelItemGrid.Slots.Length];
			if (clientItemStack == null)
			{
				_inputItemGrid.Slots[i] = new ItemGridSlot
				{
					Icon = _inputSlotIcons[i],
					InventorySlotIndex = i + _fuelItemGrid.Slots.Length
				};
			}
			else
			{
				_inputItemGrid.Slots[i] = new ItemGridSlot
				{
					ItemStack = clientItemStack,
					InventorySlotIndex = i + _fuelItemGrid.Slots.Length,
					Overlay = (((double)num > 0.001 && num < 1f) ? _inputSlotActiveOverlay : null)
				};
			}
		}
		_inputItemGrid.Layout();
		int slotOffset = _inputItemGrid.Slots.Length + _fuelItemGrid.Slots.Length;
		_outputItemGrid.SetItemStacks(_inventoryWindow.Inventory, slotOffset);
		_outputItemGrid.Layout();
		foreach (Element child in _inputProcessingBars.Children)
		{
			ProgressBar progressBar = child.Find<ProgressBar>("ProcessingBar");
			progressBar.Value = num;
			progressBar.Layout();
		}
		if (!_fuelContainer.Visible)
		{
			return;
		}
		for (int j = 0; j < _fuelItemGrid.Slots.Length; j++)
		{
			ClientItemStack clientItemStack2 = _inventoryWindow.Inventory[j];
			if (clientItemStack2 == null && _fuelSlotIcons[j] != null)
			{
				_fuelItemGrid.Slots[j] = new ItemGridSlot
				{
					Icon = _fuelSlotIcons[j],
					Overlay = (_isOn ? _fuelSlotActiveOverlay : null)
				};
			}
			else
			{
				_fuelItemGrid.Slots[j] = new ItemGridSlot
				{
					ItemStack = clientItemStack2,
					Overlay = (_isOn ? _fuelSlotActiveOverlay : null)
				};
			}
		}
		_fuelItemGrid.Layout();
	}

	private void SetOn(bool isOn, bool sendPacket = true)
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		_isOn = isOn;
		_onOffButton.Text = Desktop.Provider.GetText("ui.windows.processing.turn" + (_isOn ? "Off" : "On"));
		_onOffButton.Style = (_isOn ? _onButtonStyle : _offButtonStyle);
		_onOffButton.Layout();
		if (_fuelItemGrid.IsMounted)
		{
			Update();
		}
		if (sendPacket)
		{
			InventoryPage inventoryPage = _inGameView.InventoryPage;
			int id = _inventoryWindow.Id;
			JObject val = new JObject();
			val.Add("state", JToken.op_Implicit(isOn));
			inventoryPage.SendWindowAction(id, "setActive", val);
		}
	}
}
