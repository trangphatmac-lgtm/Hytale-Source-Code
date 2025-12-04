using System;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Elements;

public abstract class BaseTooltipLayer : Element
{
	[UIMarkupProperty]
	public float ShowDelay = 0.5f;

	private float _timer;

	private bool _isActive;

	protected BaseTooltipLayer(Desktop desktop)
		: base(desktop, null)
	{
		_layoutMode = LayoutMode.Top;
	}

	public void Start(bool resetTimer = false)
	{
		if (resetTimer && _timer < ShowDelay)
		{
			_timer = 0f;
		}
		if (!_isActive)
		{
			_isActive = true;
			Desktop.RegisterAnimationCallback(Animate);
		}
	}

	private void Animate(float deltaTime)
	{
		if (_timer < ShowDelay)
		{
			_timer = System.Math.Min(ShowDelay, _timer + deltaTime);
			if (!(_timer >= ShowDelay))
			{
				return;
			}
			Desktop.SetPassiveLayer(this);
		}
		UpdatePosition();
		Layout();
	}

	protected abstract void UpdatePosition();

	public void Stop()
	{
		if (_isActive)
		{
			if (_timer >= ShowDelay)
			{
				Desktop.SetPassiveLayer(null);
			}
			_timer = 0f;
			_isActive = false;
			Desktop.UnregisterAnimationCallback(Animate);
		}
	}
}
