using System;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Hud;

internal class CustomHud : InterfaceComponent
{
	private readonly InGameView _inGameView;

	private readonly Desktop _hudDesktop;

	private readonly Element _hudLayer;

	public CustomHud(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
		_hudDesktop = new Desktop(Interface.InGameCustomUIProvider, Desktop.Graphics, Interface.Engine.Graphics.Batcher2D);
		_hudLayer = new Element(_hudDesktop, null);
	}

	public void Build()
	{
		Clear();
		_hudDesktop.ClearAllLayers();
		if (base.IsMounted)
		{
			_hudDesktop.SetLayer(0, _hudLayer);
		}
	}

	public void ResetState()
	{
		_hudLayer.Clear();
	}

	protected override void OnMounted()
	{
		_hudDesktop.SetLayer(0, _hudLayer);
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		_hudDesktop.ClearAllLayers();
	}

	private void Animate(float deltaTime)
	{
		_hudDesktop.Update(deltaTime);
	}

	public void Apply(CustomHud packet)
	{
		if (packet.Clear)
		{
			_hudLayer.Clear();
		}
		try
		{
			Interface.InGameCustomUIProvider.ApplyCommands(packet.Commands, _hudLayer);
		}
		catch (Exception exception)
		{
			_hudLayer.Clear();
			_inGameView.DisconnectWithError("Failed to apply CustomUI HUD commands", exception);
		}
	}

	protected override void LayoutSelf()
	{
		_hudDesktop.SetViewport(Desktop.ViewportRectangle, Desktop.Scale);
	}

	protected override void PrepareForDrawSelf()
	{
		_hudDesktop.PrepareForDraw();
	}

	public void OnChangeDrawOutlines()
	{
		_hudDesktop.DrawOutlines = Desktop.DrawOutlines;
	}
}
