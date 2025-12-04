using System.Collections.Generic;
using HytaleClient.AssetEditor.Backends;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class DayTimeControls : Element
{
	private readonly AssetEditorOverlay _assetEditorOverlay;

	private CheckBox _togglePauseTimeCheckBox;

	private Button _previousDay;

	private Button _nextDay;

	private CheckBox _lockPreview;

	private Label _currentDayLabel;

	private float _secondsSinceLastTimeUpdate;

	public DayTimeControls(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
		FlexWeight = 1;
		_layoutMode = LayoutMode.Left;
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		UpdateLockPreviewButton();
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (_assetEditorOverlay.Backend is ServerAssetEditorBackend)
		{
			_secondsSinceLastTimeUpdate += deltaTime;
			if ((double)_secondsSinceLastTimeUpdate >= 0.3)
			{
				_secondsSinceLastTimeUpdate = 0f;
				SetDay(_assetEditorOverlay.Interface.App.Editor.GameTime.Time.DayOfYear);
			}
		}
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/DayTimeControls.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_togglePauseTimeCheckBox = uIFragment.Get<CheckBox>("TogglePauseTime");
		_togglePauseTimeCheckBox.ValueChanged = delegate
		{
			_assetEditorOverlay.Interface.App.Editor.GameTime.SetTimePaused(_togglePauseTimeCheckBox.Value);
		};
		_previousDay = uIFragment.Get<Button>("PreviousDay");
		_previousDay.Activating = delegate
		{
			ModifyLocalDaytimeOverride(-1);
		};
		_nextDay = uIFragment.Get<Button>("NextDay");
		_nextDay.Activating = delegate
		{
			ModifyLocalDaytimeOverride(1);
		};
		_lockPreview = uIFragment.Get<CheckBox>("LockTimeAndWeather");
		_lockPreview.ValueChanged = delegate
		{
			_assetEditorOverlay.Interface.App.Editor.GameTime.SetLocked(_lockPreview.Value);
		};
		_currentDayLabel = uIFragment.Get<Label>("CurrentDay");
		SetDay(0, doLayout: false);
	}

	public void ResetState()
	{
		_togglePauseTimeCheckBox.Value = _assetEditorOverlay.Interface.App.Editor.GameTime.IsPaused;
		UpdateLockPreviewButton();
	}

	private void UpdateLockPreviewButton()
	{
		_lockPreview.Value = _assetEditorOverlay.Interface.App.Editor.GameTime.IsLocked;
		_lockPreview.Layout();
	}

	private void SetDay(int day, bool doLayout = true)
	{
		_currentDayLabel.Text = Desktop.Provider.GetText("ui.assetEditor.weatherDaytimeBar.currentDay", new Dictionary<string, string> { 
		{
			"day, number",
			Desktop.Provider.FormatNumber(day)
		} });
		if (doLayout)
		{
			_currentDayLabel.Layout();
		}
	}

	private void ModifyLocalDaytimeOverride(int mod)
	{
		_assetEditorOverlay.Interface.App.Editor.GameTime.ModifyDayOverride(mod);
	}

	public void OnGameTimePauseUpdated(bool isPaused)
	{
		_togglePauseTimeCheckBox.Value = isPaused;
		if (base.IsMounted)
		{
			_togglePauseTimeCheckBox.Layout();
		}
	}
}
