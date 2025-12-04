using HytaleClient.Data.EntityStats;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Hud;

internal abstract class EntityStatBarComponent : InterfaceComponent
{
	private const float ProgressBarAnimationSpeed = 5f;

	private string _documentPath;

	private bool _hideIfFull;

	private ProgressBar _progressBar;

	private float _lerpValue = 1f;

	private float _targetValue = 1f;

	public bool Display => !_hideIfFull || _lerpValue != _targetValue;

	public EntityStatBarComponent(InGameView view, string documentPath, bool hideIfFull = false)
		: base(view.Interface, view.HudContainer)
	{
		_documentPath = documentPath;
		_hideIfFull = hideIfFull;
	}

	public virtual void Build()
	{
		Clear();
		Interface.TryGetDocument(_documentPath, out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_progressBar = uIFragment.Get<ProgressBar>("ProgressBar");
		_lerpValue = _targetValue;
		UpdateProgress();
		if (base.IsMounted)
		{
			Layout();
		}
	}

	protected virtual void UpdateProgress()
	{
		_progressBar.Value = _lerpValue;
		if (_hideIfFull)
		{
			base.Visible = _progressBar.Value < 1f;
		}
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		UpdateVisibility();
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected abstract void UpdateVisibility();

	protected virtual void Animate(float deltaTime)
	{
		if (_lerpValue != _targetValue)
		{
			_lerpValue = MathHelper.Lerp(_lerpValue, _targetValue, deltaTime * 5f);
			if (MathHelper.Distance(_targetValue, _lerpValue) < 0.001f)
			{
				_lerpValue = _targetValue;
			}
			UpdateProgress();
			Layout();
		}
	}

	public virtual void ResetState()
	{
		_progressBar.Value = 1f;
		_lerpValue = 1f;
		_targetValue = 1f;
		if (_hideIfFull)
		{
			base.Visible = false;
		}
	}

	public virtual void OnStatChanged(ClientEntityStatValue value)
	{
		_targetValue = ((value.Max > 0f) ? value.AsPercentage() : 0f);
		if (_hideIfFull && _targetValue != _lerpValue)
		{
			base.Visible = true;
			Layout();
		}
	}
}
