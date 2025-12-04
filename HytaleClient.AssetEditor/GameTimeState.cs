using System;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.AssetEditor;

internal class GameTimeState
{
	private const int DefaultSecondsPerGameDay = 1800;

	private static readonly DateTime PenultimateDay;

	private static readonly DateTime SecondDay;

	public int SecondsPerGameDay = 1800;

	private readonly AssetEditorApp _app;

	public DateTime Time { get; private set; }

	public float GameDayProgressInHours { get; private set; }

	public bool IsPaused { get; private set; }

	public bool IsLocked { get; private set; }

	public GameTimeState(AssetEditorApp app)
	{
		_app = app;
	}

	public void Cleanup()
	{
		Time = DateTime.MinValue;
		IsPaused = false;
		SecondsPerGameDay = 1800;
		GameDayProgressInHours = 0f;
		IsLocked = false;
	}

	public void ProcessServerTimeUpdate(InstantData gameTime, bool isPaused)
	{
		IsPaused = isPaused;
		DateTime gameTime2 = TimeHelper.InstantDataToDateTime(gameTime);
		SetGameTime(gameTime2);
		_app.Interface.AssetEditor.ConfigEditorContextPane.DayTimeControls.OnGameTimePauseUpdated(isPaused);
	}

	public void SetTimeOverride(float time)
	{
		DateTime time2 = Time;
		DateTime dateTime = new DateTime(time2.Year, time2.Month, time2.Day, 0, 0, 0, DateTimeKind.Utc);
		SetGameTime(dateTime + TimeSpan.FromTicks((long)(time * 8.64E+11f)));
		_app.Editor.Backend.SetGameTime(Time, IsPaused);
	}

	public void SetTimePaused(bool paused)
	{
		IsPaused = paused;
		_app.Editor.Backend.SetGameTime(Time, IsPaused);
	}

	public void SetLocked(bool locked)
	{
		IsLocked = locked;
		_app.Editor.Backend.SetWeatherAndTimeLock(locked);
	}

	public void ModifyDayOverride(int dayModifier)
	{
		if ((dayModifier > -1 || !(Time < SecondDay)) && (dayModifier < 1 || !(Time > PenultimateDay)))
		{
			Time = Time.AddDays(dayModifier);
			_app.Editor.Backend.SetGameTime(Time, IsPaused);
		}
	}

	private void SetGameTime(DateTime gameTime)
	{
		Time = gameTime;
		GameDayProgressInHours = TimeHelper.GetDayProgressInHours(gameTime);
	}

	public void OnNewFrame(float deltaTime)
	{
		if (!IsPaused)
		{
			SetGameTime(TimeHelper.IncrementDateTimeBySeconds(Time, deltaTime, SecondsPerGameDay));
		}
	}

	static GameTimeState()
	{
		DateTime maxTime = TimeHelper.MaxTime;
		PenultimateDay = maxTime.AddDays(-1.0);
		maxTime = TimeHelper.ZeroYear;
		SecondDay = maxTime.AddDays(1.0);
	}
}
