using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal class ReplaceCommand : BaseSelectionCommand
{
	private Group _materialContainer;

	private Group _swapMaterialContainer;

	private Group _replaceMaterialContainer;

	private Group _swapReplaceMaterialContainer;

	private CheckBox _swapCheckbox;

	private List<MaterialConfigurationInput> _materials = new List<MaterialConfigurationInput>();

	private MaterialConfigurationInput _replaceMaterial;

	private MaterialConfigurationInput _swapReplaceMaterial;

	private MaterialConfigurationInput _swapMaterial;

	private TextButton _addMaterialButton;

	public ReplaceCommand(InGameView inGameView, Desktop desktop, Element parent = null)
		: base(inGameView, desktop, parent)
	{
	}

	public override void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("InGame/Pages/Inventory/BuilderTools/CommandReplace.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_materialContainer = uIFragment.Get<Group>("MaterialContainer");
		AddMaterial();
		_addMaterialButton = uIFragment.Get<TextButton>("AddMaterialButton");
		_addMaterialButton.Activating = delegate
		{
			AddMaterial();
			Parent.Parent.Layout();
		};
		_replaceMaterialContainer = uIFragment.Get<Group>("ReplaceMaterialContainer");
		_replaceMaterial = new MaterialConfigurationInput(_inGameView, Desktop, null, hasWeight: false);
		_replaceMaterial.Build();
		_replaceMaterial.HideRemoveButton();
		_replaceMaterialContainer.Add(_replaceMaterial);
		_swapReplaceMaterialContainer = uIFragment.Get<Group>("SwapReplaceMaterialContainer");
		_swapReplaceMaterial = new MaterialConfigurationInput(_inGameView, Desktop, null, hasWeight: false, hasPitch: false);
		_swapReplaceMaterial.Build();
		_swapReplaceMaterial.HideRemoveButton();
		_swapReplaceMaterialContainer.Add(_swapReplaceMaterial);
		_swapMaterialContainer = uIFragment.Get<Group>("SwapMaterialContainer");
		_swapMaterial = new MaterialConfigurationInput(_inGameView, Desktop, null, hasWeight: false, hasPitch: false, 7);
		_swapMaterial.Build();
		_swapMaterial.HideRemoveButton();
		_swapMaterialContainer.Add(_swapMaterial);
		_swapCheckbox = uIFragment.Get<CheckBox>("SwapCheckbox");
		_swapCheckbox.ValueChanged = delegate
		{
			ToggleSwapMaterialVisibility();
		};
		ToggleSwapMaterialVisibility();
	}

	private void ToggleSwapMaterialVisibility()
	{
		if (_swapCheckbox.Value)
		{
			_swapMaterialContainer.Visible = true;
			_swapReplaceMaterialContainer.Visible = true;
			_replaceMaterialContainer.Visible = false;
			_materialContainer.Visible = false;
			_addMaterialButton.Visible = false;
			Layout();
		}
		else
		{
			_swapMaterialContainer.Visible = false;
			_swapReplaceMaterialContainer.Visible = false;
			_replaceMaterialContainer.Visible = true;
			_materialContainer.Visible = true;
			_addMaterialButton.Visible = true;
			Layout();
		}
	}

	private void AddMaterial()
	{
		MaterialConfigurationInput materialConfigurationInput = new MaterialConfigurationInput(_inGameView, Desktop, OnRemoveMaterial, hasWeight: false);
		materialConfigurationInput.Build();
		_materialContainer.Add(materialConfigurationInput);
		_materials.Add(materialConfigurationInput);
		ManageRemoveButtonVisibility();
	}

	private void OnRemoveMaterial(MaterialConfigurationInput materialConfigurationInput)
	{
		if (_materials.Count > 1)
		{
			_materialContainer.Remove(materialConfigurationInput);
			_materials.Remove(materialConfigurationInput);
			Parent.Parent.Layout();
			ManageRemoveButtonVisibility();
		}
	}

	public override string GetChatCommand()
	{
		string text = "";
		if (_swapCheckbox.Value)
		{
			text = GetMaterialSet(_swapMaterial.GetCommandArgs());
			return "/replace --swap " + text + " " + GetMaterialSet(_swapReplaceMaterial.GetCommandArgs());
		}
		foreach (MaterialConfigurationInput material in _materials)
		{
			text += material.GetCommandArgs();
			if (material != _materials.Last())
			{
				text += ",";
			}
		}
		return "/replace --exact " + text + " " + _replaceMaterial.GetCommandArgs();
	}

	private string GetMaterialSet(string materialsArgs)
	{
		string[] array = materialsArgs.Split(new char[1] { ',' });
		string text = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			ClientItemBase item = _inGameView.InGame.Instance.ItemLibraryModule.GetItem(text2);
			if (item != null)
			{
				text += item.Set;
				if (text2 != array.Last())
				{
					text += ",";
				}
			}
		}
		return text;
	}

	private void ManageRemoveButtonVisibility()
	{
		if (_materials.Count > 1)
		{
			foreach (MaterialConfigurationInput material in _materials)
			{
				material.ShowRemoveButton();
			}
			Layout();
			return;
		}
		foreach (MaterialConfigurationInput material2 in _materials)
		{
			material2.HideRemoveButton();
		}
	}
}
