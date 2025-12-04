using System.Collections.Generic;
using HytaleClient.InGame.Modules.WorldMap;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using SDL2;

namespace HytaleClient.Interface.InGame.Pages;

internal class MapPage : InterfaceComponent
{
	private readonly InGameView _inGameView;

	private Label _zoneName;

	private Label _biomeName;

	private WorldMapModule.MarkerSelection _contextMarker;

	private PopupMenuLayer _popup;

	private SoundStyle _teleportSound;

	private SoundStyle _selectLocationSound;

	private SoundStyle _deselectLocationSound;

	private SoundStyle _placeMarkerSound;

	public SoundStyle OpenSound { get; private set; }

	public SoundStyle CloseSound { get; private set; }

	public MapPage(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
		_inGameView = inGameView;
		Interface.RegisterForEventFromEngine<string, string>("worldMap.setHighlightedBiome", OnSetHighlightedBiome);
		Interface.RegisterForEventFromEngine("worldMap.hideContextMenu", OnHideContextMenu);
	}

	public void Build()
	{
		Clear();
		_zoneName = new Label(Desktop, this)
		{
			Anchor = 
			{
				Width = 500,
				Height = 30,
				Right = 50,
				Bottom = 150
			},
			Visible = false
		};
		_biomeName = new Label(Desktop, this)
		{
			Anchor = 
			{
				Width = 500,
				Height = 30,
				Right = 50,
				Bottom = 120
			},
			Visible = false
		};
		Interface.TryGetDocument("Common.ui", out var document);
		PopupMenuLayerStyle style = document.ResolveNamedValue<PopupMenuLayerStyle>(Desktop.Provider, "DefaultPopupMenuLayerStyle");
		_popup = new PopupMenuLayer(Desktop, null)
		{
			Style = style
		};
		Interface.TryGetDocument("InGame/Pages/MapPage.ui", out var document2);
		OpenSound = document2.ResolveNamedValue<SoundStyle>(Desktop.Provider, "OpenSound");
		CloseSound = document2.ResolveNamedValue<SoundStyle>(Desktop.Provider, "CloseSound");
		document2.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "TeleportSound", out _teleportSound);
		document2.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "SelectLocationSound", out _selectLocationSound);
		document2.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "DeselectLocationSound", out _deselectLocationSound);
		document2.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "PlaceMarkerSound", out _placeMarkerSound);
	}

	protected List<PopupMenuItem> BuildMenuItems(bool isSelectedMarker)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Invalid comparison between Unknown and I4
		List<PopupMenuItem> list = new List<PopupMenuItem>();
		if (isSelectedMarker)
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.map.deselectLocation"), OnPopupDeselectLocation, null, _deselectLocationSound));
		}
		else
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.map.selectLocation"), OnPopupSelectLocation, null, _selectLocationSound));
		}
		if ((int)_inGameView.InGame.Instance.GameMode == 1)
		{
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.map.teleport"), OnPopupTeleport, null, _teleportSound));
		}
		return list;
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_zoneName.Style = new LabelStyle
		{
			FontSize = 20f,
			RenderBold = true,
			HorizontalAlignment = LabelStyle.LabelAlignment.End
		};
		_biomeName.Style = new LabelStyle
		{
			FontSize = 20f,
			HorizontalAlignment = LabelStyle.LabelAlignment.End
		};
	}

	protected override void OnMounted()
	{
		_inGameView.InGame.SetSceneBlurEnabled(enabled: true);
		_inGameView.InGame.Instance.WorldMapModule.SetVisible(visible: true);
		_zoneName.Visible = true;
		_inGameView.HudContainer.Remove(_inGameView.InputBindingsComponent);
		Add(_inGameView.InputBindingsComponent);
	}

	protected override void OnUnmounted()
	{
		_inGameView.InGame.SetSceneBlurEnabled(enabled: false);
		_inGameView.InGame.Instance.WorldMapModule.SetVisible(visible: false);
		_zoneName.Visible = false;
		_popup.Close();
		Remove(_inGameView.InputBindingsComponent);
		_inGameView.HudContainer.Add(_inGameView.InputBindingsComponent);
		_inGameView.HudContainer.Layout();
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		base.OnKeyDown(keycode, repeat);
		if ((int)keycode == 1073742049)
		{
			_biomeName.Visible = true;
			Layout();
		}
	}

	protected internal override void OnKeyUp(SDL_Keycode keycode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)keycode == 1073742049)
		{
			_biomeName.Visible = false;
		}
	}

	private void OnSetHighlightedBiome(string zoneName, string biomeName)
	{
		string text = Desktop.Provider.GetText("map.zones." + zoneName, null, returnFallback: false) ?? "";
		_zoneName.Text = text;
		_zoneName.Visible = text != string.Empty;
		_biomeName.Text = biomeName;
		if (base.IsMounted)
		{
			Layout();
		}
	}

	public void OnMarkerPlaced()
	{
		if (_placeMarkerSound != null)
		{
			Desktop.Provider.PlaySound(_placeMarkerSound);
		}
	}

	public void OnShowContextMenu(WorldMapModule.MarkerSelection contextMarker, bool isSelectedMarker)
	{
		_contextMarker = contextMarker;
		_popup.SetTitle(contextMarker.MapMarker?.Name ?? $"X: {contextMarker.Coordinates.X}, Y: {contextMarker.Coordinates.Y}");
		_popup.SetItems(BuildMenuItems(isSelectedMarker));
		_popup.Open();
	}

	private void OnHideContextMenu()
	{
		_popup.Close();
	}

	private void OnPopupSelectLocation()
	{
		_inGameView.InGame.Instance.WorldMapModule.OnSelectContextMarker();
		_inGameView.CompassComponent.SelectContextMarker(_contextMarker);
	}

	private void OnPopupDeselectLocation()
	{
		_inGameView.InGame.Instance.WorldMapModule.OnDeselectContextMarker();
		_inGameView.CompassComponent.DeselectContextMarker();
	}

	private void OnPopupTeleport()
	{
		_inGameView.InGame.Instance.WorldMapModule.OnTeleportToContextMarker();
	}
}
