using System;
using System.Collections.Generic;
using System.Globalization;
using HytaleClient.InGame.Commands;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.InGame.Modules;

internal class AutoCameraModule : Module
{
	private class AutoCameraController : ICameraController
	{
		public float Speed = 1f;

		private readonly GameInstance _gameInstance;

		private float _accumDelta;

		private Vector3 _tempVec3;

		private List<Tuple<Vector3, Vector2>> _positions;

		public float SpeedModifier { get; set; } = 1f;


		public Vector3 AttachmentPosition => Vector3.Zero;

		public Vector3 PositionOffset => Vector3.Zero;

		public Vector3 RotationOffset => Vector3.Zero;

		public Vector3 Position { get; private set; }

		public Vector3 Rotation { get; private set; }

		public Vector3 LookAt { get; private set; }

		public Vector3 MovementForceRotation => Rotation;

		public Entity AttachedTo
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public bool IsFirstPerson => false;

		public bool SkipCharacterPhysics => true;

		public bool CanMove => false;

		public bool AllowPitchControls => false;

		public bool DisplayCursor => false;

		public bool DisplayReticle => false;

		public bool InteractFromEntity => false;

		public bool Paused { get; set; }

		public AutoCameraController(GameInstance gameInstance)
		{
			_gameInstance = gameInstance;
		}

		public void Reset(GameInstance gameInstance, ICameraController previousCameraController)
		{
		}

		public void ApplyLook(float deltaTime, Vector2 look)
		{
		}

		public void SetRotation(Vector3 rotation)
		{
		}

		public void ApplyMove(Vector3 move)
		{
		}

		public void OnMouseInput(SDL_Event evt)
		{
		}

		public void Start(List<Tuple<Vector3, Vector2>> positions)
		{
			_gameInstance.CameraModule.SetCustomCameraController(this);
			_positions = new List<Tuple<Vector3, Vector2>>(positions);
			Paused = false;
			_accumDelta = 0f;
		}

		public void Stop()
		{
			_positions = null;
			_accumDelta = 0f;
		}

		public void Update(float deltaTime)
		{
			if (_positions == null || Paused)
			{
				return;
			}
			_accumDelta += deltaTime;
			float num = _accumDelta * Speed;
			int num2 = -1;
			float num3 = 0f;
			for (int i = 0; i < _positions.Count - 1; i++)
			{
				Tuple<Vector3, Vector2> tuple = _positions[i];
				Tuple<Vector3, Vector2> tuple2 = _positions[i + 1];
				float num4 = Vector3.Distance(tuple.Item1, tuple2.Item1);
				if (num >= num3 && num < num3 + num4)
				{
					num2 = i;
					break;
				}
				num3 += num4;
			}
			if (num2 == -1)
			{
				_gameInstance.CameraModule.ResetCameraController();
				Stop();
				return;
			}
			Tuple<Vector3, Vector2> tuple3 = _positions[num2];
			Tuple<Vector3, Vector2> tuple4 = _positions[num2 + 1];
			Tuple<Vector3, Vector2> tuple5 = ((num2 - 1 >= 0) ? _positions[num2 - 1] : tuple3);
			Tuple<Vector3, Vector2> tuple6 = ((num2 + 2 < _positions.Count) ? _positions[num2 + 2] : tuple4);
			float t = (num - num3) / Vector3.Distance(tuple3.Item1, tuple4.Item1);
			Vector3 p = tuple5.Item1;
			Vector3 p2 = tuple3.Item1;
			Vector3 p3 = tuple4.Item1;
			Vector3 p4 = tuple6.Item1;
			Vector3.Spline(ref t, ref p, ref p2, ref p3, ref p4, out _tempVec3);
			Position = _tempVec3;
			Vector2 item = tuple5.Item2;
			Vector2 item2 = tuple3.Item2;
			Vector2 item3 = tuple4.Item2;
			Vector2 item4 = tuple6.Item2;
			float value = MathHelper.Spline(t, item.X, item2.X, item3.X, item4.X);
			float angle = MathHelper.SplineAngle(t, item.Y, item2.Y, item3.Y, item4.Y);
			Rotation = new Vector3(MathHelper.Clamp(value, -(float)System.Math.PI / 2f, (float)System.Math.PI / 2f), MathHelper.WrapAngle(angle), 0f);
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly List<Tuple<Vector3, Vector2>> _positions = new List<Tuple<Vector3, Vector2>>();

	private readonly AutoCameraController _autoCameraController;

	public AutoCameraModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_autoCameraController = new AutoCameraController(_gameInstance);
		_gameInstance.RegisterCommand("cam", CamCommand);
	}

	[Usage("cam", new string[] { "add [index] [[x y z] [yaw pitch]]", "remove [index]", "clear", "list", "start [speed]", "pause", "stop", "save", "load" })]
	public void CamCommand(string[] args)
	{
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		int num = 0;
		switch (args[num++].ToLower())
		{
		case "add":
		{
			int num3 = -1;
			if (args.Length == 1 + num || args.Length >= 4 + num)
			{
				num3 = int.Parse(args[num++], CultureInfo.InvariantCulture);
				if (num3 < 0)
				{
					_gameInstance.Chat.Log("Index must be greater than 0");
					break;
				}
				if (num3 > _positions.Count)
				{
					_gameInstance.Chat.Log($"Index must be less than than the number of positions saved, {_positions.Count}");
					break;
				}
			}
			else if (args.Length != num)
			{
				_gameInstance.Chat.Log("Invalid usage!");
				break;
			}
			Vector3 position = _gameInstance.LocalPlayer.Position;
			float num4 = position.X;
			float num5 = position.Y;
			float num6 = position.Z;
			if (args.Length > 3 + num)
			{
				num4 = float.Parse(args[num++], CultureInfo.InvariantCulture);
				num5 = float.Parse(args[num++], CultureInfo.InvariantCulture);
				num6 = float.Parse(args[num++], CultureInfo.InvariantCulture);
			}
			float num7 = _gameInstance.LocalPlayer.LookOrientation.Pitch;
			float num8 = _gameInstance.LocalPlayer.LookOrientation.Yaw;
			if (args.Length > 2 + num)
			{
				num8 = MathHelper.ToRadians(float.Parse(args[num++], CultureInfo.InvariantCulture));
				num7 = MathHelper.ToRadians(float.Parse(args[num], CultureInfo.InvariantCulture));
			}
			Tuple<Vector3, Vector2> item = new Tuple<Vector3, Vector2>(new Vector3(num4, num5, num6), new Vector2(num7, num8));
			if (num3 >= 0)
			{
				_positions.Insert(num3, item);
				_gameInstance.Chat.Log($"Insert position at {num3}: X: {num4}, Y: {num5}, Z: {num6}, Yaw: {num8}, Pitch: {num7}");
			}
			else
			{
				_positions.Add(item);
				_gameInstance.Chat.Log($"Added position at {_positions.Count - 1}: X: {num4}, Y: {num5}, Z: {num6}, Yaw: {num8}, Pitch: {num7}");
			}
			break;
		}
		case "remove":
		{
			int num2 = -1;
			if (args.Length == 1 + num)
			{
				num2 = int.Parse(args[num], CultureInfo.InvariantCulture);
				if (num2 < 0)
				{
					_gameInstance.Chat.Log("Index must be greater than 0");
					break;
				}
				if (num2 >= _positions.Count)
				{
					_gameInstance.Chat.Log($"Index must be less than than the number of positions saved {_positions.Count}");
					break;
				}
			}
			if (num2 == -1)
			{
				num2 = _positions.Count - 1;
			}
			Tuple<Vector3, Vector2> tuple = _positions[num2];
			_positions.RemoveAt(num2);
			_gameInstance.Chat.Log($"Removed point at {num2}: {tuple.Item1.X}, {tuple.Item1.Y}, {tuple.Item1.Z}, {tuple.Item2.X}, {tuple.Item2.Y}");
			break;
		}
		case "clear":
			_positions.Clear();
			_gameInstance.Chat.Log("Cleared points!");
			break;
		case "list":
		{
			_gameInstance.Chat.Log("Points:");
			for (int i = 0; i < _positions.Count; i++)
			{
				Tuple<Vector3, Vector2> tuple2 = _positions[i];
				_gameInstance.Chat.Log($"{i}: {tuple2.Item1.X}, {tuple2.Item1.Y}, {tuple2.Item1.Z}, {tuple2.Item2.X}, {tuple2.Item2.Y}");
			}
			break;
		}
		case "start":
			if (!_gameInstance.CameraModule.IsCustomCameraControllerSet())
			{
				if (_positions.Count <= 0)
				{
					_gameInstance.Chat.Log("No points stored! Use .cam add");
					break;
				}
				if (args.Length > num)
				{
					_autoCameraController.Speed = float.Parse(args[num], CultureInfo.InvariantCulture);
				}
				else
				{
					_autoCameraController.Speed = 1f;
				}
				_autoCameraController.Start(_positions);
				_gameInstance.Chat.Log("Started Auto Camera!");
			}
			else
			{
				_gameInstance.Chat.Log("A custom camera controller is already set! Disable it before enabling the camera mod.");
			}
			break;
		case "pause":
			if (!_gameInstance.CameraModule.IsCustomCameraControllerSet())
			{
				_gameInstance.Chat.Log("The auto camera has not been started.");
			}
			else if (_gameInstance.CameraModule.Controller == _autoCameraController)
			{
				_autoCameraController.Paused = true;
				_gameInstance.CameraModule.ResetCameraController();
				_gameInstance.Chat.Log("Paused Auto Camera!");
			}
			else
			{
				_gameInstance.Chat.Log("A custom camera controller is already set! Disable it before enabling the camera mod.");
			}
			break;
		case "stop":
			if (!_gameInstance.CameraModule.IsCustomCameraControllerSet())
			{
				_gameInstance.Chat.Log("The auto camera has not been started.");
			}
			else if (_gameInstance.CameraModule.Controller == _autoCameraController)
			{
				_autoCameraController.Stop();
				_gameInstance.CameraModule.ResetCameraController();
				_gameInstance.Chat.Log("Stopped Auto Camera!");
			}
			else if (_autoCameraController.Paused)
			{
				_autoCameraController.Stop();
				_gameInstance.Chat.Log("Stopped Auto Camera!");
			}
			else
			{
				_gameInstance.Chat.Log("A custom camera controller is already set! Disable it before enabling the camera mod.");
			}
			break;
		case "save":
			SDL.SDL_SetClipboardText(JsonConvert.SerializeObject((object)_positions, (Formatting)1));
			_gameInstance.Chat.Log("Copied camera track to clipboard!");
			break;
		case "load":
			try
			{
				string text = SDL.SDL_GetClipboardText();
				JArray val = JArray.Parse(text);
				List<Tuple<Vector3, Vector2>> collection = ((JToken)val).ToObject<List<Tuple<Vector3, Vector2>>>();
				_positions.Clear();
				_positions.AddRange(collection);
				_gameInstance.Chat.Log("Loaded the camera track from clipboard!");
				break;
			}
			catch (Exception ex)
			{
				_gameInstance.Chat.Log("Failed to parse clipboard contents! " + ex.Message);
				Logger.Error(ex, "Failed to parse clipboard contents:");
				break;
			}
		default:
			throw new InvalidCommandUsage();
		}
	}
}
