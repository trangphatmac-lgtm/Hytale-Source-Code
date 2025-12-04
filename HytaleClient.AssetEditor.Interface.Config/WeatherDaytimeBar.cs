using System;
using HytaleClient.AssetEditor.Backends;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class WeatherDaytimeBar : Element
{
	private readonly AssetEditorOverlay _assetEditorOverlay;

	private Group _timelineGroup;

	private Group _timeMarker;

	private float _secondsSinceLastTimeUpdate;

	public int CurrentHour { get; private set; }

	public WeatherDaytimeBar(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/WeatherDaytimeBar.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_timelineGroup = uIFragment.Get<Group>("Timeline");
		_timeMarker = uIFragment.Get<Group>("TimeMarker");
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (!(_assetEditorOverlay.Backend is ServerAssetEditorBackend))
		{
			return;
		}
		_secondsSinceLastTimeUpdate += deltaTime;
		if ((double)_secondsSinceLastTimeUpdate >= 0.3)
		{
			_secondsSinceLastTimeUpdate = 0f;
			if (!base.CapturedMouseButton.HasValue)
			{
				GameTimeState gameTime = _assetEditorOverlay.Interface.App.Editor.GameTime;
				float num = gameTime.GameDayProgressInHours / 24f;
				_timeMarker.Anchor.Left = Desktop.UnscaleRound(num * (float)_timelineGroup.AnchoredRectangle.Width - (float)_timeMarker.AnchoredRectangle.Width / 2f);
				_timeMarker.Layout();
				UpdateTimelineEditors(num);
			}
		}
	}

	public override Element HitTest(Point position)
	{
		if (_waitingForLayoutAfterMount || !_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return (!_timelineGroup.AnchoredRectangle.Contains(position)) ? base.HitTest(position) : this;
	}

	public void ResetState()
	{
		CurrentHour = 0;
		_timeMarker.Anchor.Left = _timeMarker.Anchor.Width.Value / 2;
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		UpdateTimeFromMousePosition();
	}

	protected override void OnMouseMove()
	{
		if (base.CapturedMouseButton.HasValue)
		{
			UpdateTimeFromMousePosition();
		}
	}

	private void UpdateTimeFromMousePosition()
	{
		float value = (float)(Desktop.MousePosition.X - _timelineGroup.AnchoredRectangle.Left) / (float)_timelineGroup.AnchoredRectangle.Width;
		value = MathHelper.Clamp(value, 0f, 1f);
		_timeMarker.Anchor.Left = Desktop.UnscaleRound(value * (float)_timelineGroup.AnchoredRectangle.Width - (float)_timeMarker.AnchoredRectangle.Width / 2f);
		_timeMarker.Layout();
		_assetEditorOverlay.Interface.App.Editor.GameTime.SetTimeOverride(value);
		UpdateTimelineEditors(value);
	}

	private void UpdateTimelineEditors(float timePercentage)
	{
		int num = (int)System.Math.Min(System.Math.Floor(timePercentage * 24f), 23.0);
		if (num == CurrentHour)
		{
			return;
		}
		foreach (TimelineEditor mountedTimelineEditor in _assetEditorOverlay.ConfigEditor.MountedTimelineEditors)
		{
			mountedTimelineEditor.UpdateHighlightedHour(num);
			mountedTimelineEditor.Layout();
		}
		CurrentHour = num;
	}
}
