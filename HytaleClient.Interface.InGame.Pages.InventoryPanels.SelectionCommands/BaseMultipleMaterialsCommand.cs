using System.Collections.Generic;
using System.Linq;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Pages.InventoryPanels.SelectionCommands;

internal abstract class BaseMultipleMaterialsCommand : BaseSelectionCommand
{
	private Group _materialContainer;

	private List<MaterialConfigurationInput> _materials = new List<MaterialConfigurationInput>();

	private TextButton _addMaterialButton;

	public BaseMultipleMaterialsCommand(InGameView inGameView, Desktop desktop, Element parent = null)
		: base(inGameView, desktop, parent)
	{
	}

	public override void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("InGame/Pages/Inventory/BuilderTools/MultipleMaterialsCommand.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_materialContainer = uIFragment.Get<Group>("MaterialContainer");
		_addMaterialButton = uIFragment.Get<TextButton>("AddMaterialButton");
	}

	protected override void OnMounted()
	{
		_addMaterialButton.Activating = delegate
		{
			AddMaterial();
			Parent.Parent.Layout();
		};
		if (_materials.Count == 0)
		{
			AddMaterial();
		}
	}

	private void AddMaterial()
	{
		MaterialConfigurationInput materialConfigurationInput = new MaterialConfigurationInput(_inGameView, Desktop, OnRemoveMaterial);
		materialConfigurationInput.Build();
		_materialContainer.Add(materialConfigurationInput);
		_materials.Add(materialConfigurationInput);
		ManageRemoveButtonVisibility();
		ManageAddMaterialVisibility();
	}

	private void OnRemoveMaterial(MaterialConfigurationInput materialConfigurationInput)
	{
		if (_materials.Count > 1)
		{
			_materialContainer.Remove(materialConfigurationInput);
			_materials.Remove(materialConfigurationInput);
			Parent.Parent.Layout();
			ManageRemoveButtonVisibility();
			ManageAddMaterialVisibility();
		}
	}

	public override string GetChatCommand()
	{
		string text = "";
		foreach (MaterialConfigurationInput material in _materials)
		{
			text += material.GetCommandArgs();
			if (material != _materials.Last())
			{
				text += ",";
			}
		}
		return "[" + text + "]";
	}

	private void ManageAddMaterialVisibility()
	{
		_addMaterialButton.Visible = _materials.Count <= 10;
		Parent.Parent.Layout();
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
