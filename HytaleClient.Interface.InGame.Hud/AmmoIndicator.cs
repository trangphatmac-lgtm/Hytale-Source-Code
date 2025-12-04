using System.Collections.Generic;
using HytaleClient.Data.EntityStats;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud;

internal class AmmoIndicator : InterfaceComponent
{
	private readonly InGameView _inGameView;

	private int _containerSize;

	private int _containerSpacing;

	private PatchStyle _containerEmptyBackground;

	private PatchStyle _containerFullBackground;

	private Group _ammoContainersParent;

	private readonly List<Group> _ammoContainers = new List<Group>();

	private int _max;

	private int _loaded;

	public AmmoIndicator(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		ResetState();
		Clear();
		Interface.TryGetDocument("InGame/Hud/AmmoIndicator/AmmoIndicator.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_containerSize = document.ResolveNamedValue<int>(Interface, "ContainerSize");
		_containerSpacing = document.ResolveNamedValue<int>(Interface, "ContainerSpacing");
		_containerEmptyBackground = document.ResolveNamedValue<PatchStyle>(Interface, "ContainerEmptyBackground");
		_containerFullBackground = document.ResolveNamedValue<PatchStyle>(Interface, "ContainerFullBackground");
		_ammoContainersParent = uIFragment.Get<Group>("AmmoContainers");
	}

	public void OnAmmoChanged(ClientEntityStatValue value)
	{
		int num = (int)value.Max;
		_loaded = (int)value.Value;
		if (_max != num)
		{
			_max = num;
			BuildContainers();
		}
		else
		{
			Update();
		}
	}

	private void BuildContainers()
	{
		_ammoContainersParent.Clear();
		_ammoContainers.Clear();
		for (int i = 0; i < _max; i++)
		{
			_ammoContainers.Add(new Group(_inGameView.Interface.Desktop, _ammoContainersParent)
			{
				Anchor = 
				{
					Width = _containerSize,
					Height = _containerSize,
					Horizontal = _containerSpacing
				},
				Background = ((i <= _loaded - 1) ? _containerFullBackground : _containerEmptyBackground)
			});
		}
		Layout();
	}

	private void Update()
	{
		for (int i = 0; i < _ammoContainers.Count; i++)
		{
			_ammoContainers[i].Background = ((i < _loaded) ? _containerFullBackground : _containerEmptyBackground);
		}
		Layout();
	}

	public void ResetState()
	{
		_ammoContainers.Clear();
		_max = 0;
		_loaded = 0;
	}
}
