using System;
using HytaleClient.Application;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface;

internal class GameLoadingView : InterfaceComponent
{
	private Label _statusTextLabel;

	private ProgressBar _progressBar;

	private float _targetProgress;

	private float _lerpProgress;

	public GameLoadingView(Interface @interface)
		: base(@interface, null)
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("GameLoading/GameLoading.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_statusTextLabel = uIFragment.Get<Label>("StatusText");
		uIFragment.Get<TextButton>("CancelButton").Activating = Dismiss;
		_progressBar = uIFragment.Get<ProgressBar>("ProgressBar");
		if (base.IsMounted)
		{
			Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
		}
	}

	protected override void OnMounted()
	{
		Interface.SocialBar.SetContainer(Find<Group>("SocialBarContainer"));
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (!base.Visible)
		{
			return;
		}
		App app = Interface.App;
		if (app.GameLoading.LoadingStage == AppGameLoading.GameLoadingStage.WaitingForServerToShutdown && app.ShuttingDownSingleplayerServer == null)
		{
			app.GameLoading.StartSingleplayerServer();
		}
		else if (app.GameLoading.LoadingStage >= AppGameLoading.GameLoadingStage.Loading)
		{
			app.InGame.Instance.OnNewFrame(deltaTime, needsDrawing: false);
		}
		if (_targetProgress != _lerpProgress)
		{
			if (_lerpProgress > _targetProgress)
			{
				_lerpProgress = _targetProgress;
			}
			_lerpProgress = MathHelper.Lerp(_lerpProgress, _targetProgress, System.Math.Min(deltaTime * 20f, 1f));
			_progressBar.Value = _lerpProgress;
			_progressBar.Layout();
		}
	}

	public void SetStatus(string statusText, float percent)
	{
		if (!Interface.HasMarkupError)
		{
			_statusTextLabel.Text = statusText;
			_targetProgress = percent / 100f;
			Layout();
		}
	}

	protected internal override void Dismiss()
	{
		Interface.App.GameLoading.Abort();
	}
}
