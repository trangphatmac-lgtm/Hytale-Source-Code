using System;
using System.Collections.Generic;
using HytaleClient.InGame.Commands;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules;

internal class TimeModule : Module
{
	private struct PingInfo
	{
		private const int PingHistoryLength = 10;

		public readonly PongType Type;

		private readonly List<long> _pingHistory;

		private readonly List<long> _pingMidwayHistory;

		public Metric Ping;

		public Metric PingMidway;

		public PingInfo(PongType type)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			Type = type;
			_pingHistory = new List<long>();
			_pingMidwayHistory = new List<long>();
			Ping = new Metric(default(AverageCollector));
			PingMidway = new Metric(default(AverageCollector));
		}

		public void StoreData(DateTime serverDateTime, DateTime now, int lastPingValueMicros)
		{
			StoreData(_pingHistory, ref Ping, lastPingValueMicros);
			StoreData(_pingMidwayHistory, ref PingMidway, (now - serverDateTime).Ticks / 10);
		}

		private static void StoreData(IList<long> history, ref Metric metric, long value)
		{
			history.Add(value);
			metric.Add(value);
			if (history.Count > 10)
			{
				long value2 = history[0];
				history.RemoveAt(0);
				metric.Remove(value2);
			}
		}

		public override string ToString()
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			return string.Format("{0}: {1}, {2}: {3}, {4}: {5}", "Type", Type, "Ping", Ping, "PingMidway", PingMidway);
		}
	}

	public static int SecondsPerGameDay = 1800;

	private readonly PingInfo[] _pingInfoArray = new PingInfo[Enum.GetValues(typeof(PongType)).Length];

	private DateTime _estimatedDateTime = DateTime.MinValue;

	private DateTime _serverDateTime;

	public bool IsServerTimePaused;

	private bool _isTimePausedByEditor;

	private InstantData _lastServerInstantData;

	public float OperationTimeoutThreshold => (float)System.Math.Round(_pingInfoArray[2].Ping.Average.Val * 1.2000000476837158) * 0.001f + 75f;

	public long StatTimeoutThreshold => (long)(System.Math.Round(_pingInfoArray[2].Ping.Average.Val) * 0.0010000000474974513 + 50.0);

	public bool IsEditorTimeOverrideActive { get; private set; }

	public DateTime GameTime { get; private set; }

	public float GameDayProgressInHours { get; private set; }

	public TimeModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_gameInstance.RegisterCommand("ping", PingCommand);
		for (int i = 0; i < _pingInfoArray.Length; i++)
		{
			_pingInfoArray[i] = new PingInfo((PongType)i);
		}
	}

	public void ProcessGameTimeFromServer(InstantData gameTime)
	{
		_lastServerInstantData = gameTime;
		if (!IsEditorTimeOverrideActive)
		{
			DateTime gameTime2 = TimeHelper.InstantDataToDateTime(gameTime);
			UpdateGameTime(gameTime2);
		}
	}

	public void ProcessEditorTimeOverride(InstantData gameTime, bool isPaused)
	{
		IsEditorTimeOverrideActive = true;
		_isTimePausedByEditor = isPaused;
		DateTime gameTime2 = TimeHelper.InstantDataToDateTime(gameTime);
		UpdateGameTime(gameTime2);
	}

	public void ProcessClearEditorTimeOverride()
	{
		IsEditorTimeOverrideActive = false;
		_isTimePausedByEditor = false;
		DateTime gameTime = TimeHelper.InstantDataToDateTime(_lastServerInstantData);
		UpdateGameTime(gameTime);
	}

	private void UpdateGameTime(DateTime gameTime)
	{
		GameTime = gameTime;
		GameDayProgressInHours = TimeHelper.GetDayProgressInHours(gameTime);
		_gameInstance.WeatherModule.UpdateMoonPhase();
	}

	public double GetAveragePing(PongType type)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return _pingInfoArray[type].Ping.Average.Val;
	}

	[Obsolete]
	public override void Tick()
	{
		if (!_isTimePausedByEditor && (!IsServerTimePaused || IsEditorTimeOverrideActive))
		{
			UpdateGameTime(TimeHelper.IncrementDateTimeBySeconds(GameTime, 1f / 60f, SecondsPerGameDay));
		}
	}

	[Obsolete]
	public override void OnNewFrame(float deltaTime)
	{
		_estimatedDateTime += TimeSpan.FromSeconds(deltaTime);
	}

	public void UpdatePing(InstantData serverTime, DateTime now, PongType type, int lastPingValueMicro)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		DateTime serverDateTime = TimeHelper.InstantDataToDateTime(serverTime);
		_pingInfoArray[type].StoreData(serverDateTime, now, lastPingValueMicro);
		if ((int)type == 2)
		{
			_serverDateTime = serverDateTime;
			_estimatedDateTime = _serverDateTime;
		}
	}

	[Usage("ping", new string[] { })]
	[Description("Prints latency information to the server")]
	private void PingCommand(string[] args)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		string text = "Ping:\n           Min  /  Avg  /  Max\n";
		PingInfo[] pingInfoArray = _pingInfoArray;
		for (int i = 0; i < pingInfoArray.Length; i++)
		{
			PingInfo pingInfo = pingInfoArray[i];
			string[] obj = new string[15]
			{
				text, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null
			};
			PongType type = pingInfo.Type;
			obj[1] = ((object)(PongType)(ref type)).ToString();
			obj[2] = ":\n  Ping:   ";
			Metric ping = pingInfo.Ping;
			obj[3] = TimeHelper.FormatMicros(ping.Min);
			obj[4] = " / ";
			obj[5] = TimeHelper.FormatMicros((long)pingInfo.Ping.Average.Val);
			obj[6] = " / ";
			ping = pingInfo.Ping;
			obj[7] = TimeHelper.FormatMicros(ping.Max);
			obj[8] = "\n  Midway: ";
			ping = pingInfo.PingMidway;
			obj[9] = TimeHelper.FormatMicros(ping.Min);
			obj[10] = " / ";
			obj[11] = TimeHelper.FormatMicros((long)pingInfo.PingMidway.Average.Val);
			obj[12] = " / ";
			ping = pingInfo.PingMidway;
			obj[13] = TimeHelper.FormatMicros(ping.Max);
			obj[14] = "\n";
			text = string.Concat(obj);
		}
		_gameInstance.Chat.Log(text);
	}
}
