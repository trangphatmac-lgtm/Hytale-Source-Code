using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.DevTools;

internal class DevToolsLayer : Element
{
	private readonly Desktop _overlayDesktop;

	private readonly Interface _interface;

	private float _scale = 1f;

	public readonly DevToolsOverlay DevTools;

	public DevToolsLayer(Interface @interface)
		: base(@interface.Desktop, null)
	{
		_interface = @interface;
		_overlayDesktop = new Desktop(@interface, @interface.Engine.Graphics, @interface.Engine.Graphics.Batcher2D);
		DevTools = new DevToolsOverlay(@interface, _overlayDesktop);
	}

	protected override void OnMounted()
	{
		_overlayDesktop.SetLayer(0, DevTools);
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		_overlayDesktop.ClearAllLayers();
	}

	public void Build()
	{
		DevTools.Build();
	}

	private void Animate(float deltaTime)
	{
		_overlayDesktop.Update(deltaTime);
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void OnMouseMove()
	{
		_overlayDesktop.OnMouseMove(Desktop.MousePosition);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		_overlayDesktop.OnMouseDown(evt.Button, evt.Clicks);
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		_overlayDesktop.OnMouseUp(evt.Button, evt.Clicks);
	}

	protected internal override void OnKeyDown(SDL_Keycode keyCode, int repeat)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_overlayDesktop.OnKeyDown(keyCode, repeat);
	}

	protected internal override void OnKeyUp(SDL_Keycode keyCode)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_overlayDesktop.OnKeyUp(keyCode);
	}

	protected internal override void OnTextInput(string text)
	{
		_overlayDesktop.OnTextInput(text);
	}

	protected internal override bool OnMouseWheel(Point offset)
	{
		_overlayDesktop.OnMouseWheel(offset);
		return true;
	}

	protected override void LayoutSelf()
	{
		_overlayDesktop.SetViewport(Desktop.ViewportRectangle, _interface.Engine.Window.ViewportScale * _scale);
	}

	protected override void PrepareForDrawSelf()
	{
		_overlayDesktop.PrepareForDraw();
	}
}
