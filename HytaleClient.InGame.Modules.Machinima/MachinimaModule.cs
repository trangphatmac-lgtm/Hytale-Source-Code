#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Coherent.UI.Binding;
using Hypixel.ProtoPlus;
using HytaleClient.Application;
using HytaleClient.Core;
using HytaleClient.Data.Items;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Commands;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Client;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Machinima.Events;
using HytaleClient.InGame.Modules.Machinima.Settings;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.InGame.Modules.Machinima.TrackPath;
using HytaleClient.Interface.CoherentUI;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.InGame.Modules.Machinima;

internal class MachinimaModule : Module
{
	public delegate bool OnUseToolItem(InteractionType action);

	private class CurveHandle
	{
		public readonly SceneActor Actor;

		public readonly TrackKeyframe Keyframe;

		public readonly TrackKeyframe PrevKeyframe;

		public readonly TrackKeyframe NextKeyframe;

		public readonly int Index;

		public bool UpdateTangent;

		public CurveHandle(SceneActor actor, TrackKeyframe keyframe, int index, TrackKeyframe prevKeyframe = null, TrackKeyframe nextKeyframe = null)
		{
			Actor = actor;
			Keyframe = keyframe;
			PrevKeyframe = prevKeyframe;
			NextKeyframe = nextKeyframe;
			Index = index;
		}

		public bool Matches(SceneActor actor, TrackKeyframe keyframe, int index)
		{
			return Actor == actor && Keyframe == keyframe && Index == index;
		}
	}

	private class Tooltip : Disposable
	{
		private readonly GraphicsDevice _graphics;

		private readonly Font _font;

		private readonly TextRenderer _textRenderer;

		private readonly QuadRenderer _backgroundRenderer;

		private Matrix _tempMatrix;

		private Matrix _matrix;

		private Matrix _backgroundMatrix;

		private Matrix _progressMatrix;

		private Matrix _textMatrix;

		private Matrix _orthographicProjectionMatrix;

		public Vector2 WindowSize = Vector2.One;

		public Vector3 Position = Vector3.Zero;

		public Vector2 ScreenPosition = Vector2.NaN;

		public float Progress = 0f;

		public string TooltipText = "";

		public bool IsVisible = true;

		public Tooltip(GraphicsDevice graphics, Font font)
		{
			_graphics = graphics;
			_font = font;
			_backgroundRenderer = new QuadRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram.AttribPosition, _graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
			_textRenderer = new TextRenderer(_graphics, _font, TooltipText);
		}

		protected override void DoDispose()
		{
			_textRenderer.Dispose();
			_backgroundRenderer.Dispose();
		}

		public void DrawBackground(ref Matrix viewProjectionMatrix)
		{
			BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
			basicProgram.AssertInUse();
			_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
			UpdateTooltip(ref viewProjectionMatrix);
			basicProgram.Color.SetValue(_graphics.BlackColor);
			basicProgram.Opacity.SetValue(0.35f);
			basicProgram.MVPMatrix.SetValue(ref _backgroundMatrix);
			_backgroundRenderer.Draw();
			if (Progress > 0f)
			{
				basicProgram.Color.SetValue(_graphics.WhiteColor);
				basicProgram.Opacity.SetValue(0.75f);
				basicProgram.MVPMatrix.SetValue(ref _progressMatrix);
				_backgroundRenderer.Draw();
			}
		}

		public void DrawText(ref Matrix viewProjectionMatrix)
		{
			TextProgram textProgram = _graphics.GPUProgramStore.TextProgram;
			GLFunctions gL = _graphics.GL;
			textProgram.AssertInUse();
			textProgram.FillThreshold.SetValue(0f);
			textProgram.FillBlurThreshold.SetValue(0.1f);
			textProgram.OutlineThreshold.SetValue(0f);
			textProgram.OutlineBlurThreshold.SetValue(0f);
			textProgram.OutlineOffset.SetValue(Vector2.Zero);
			textProgram.FogParams.SetValue(Vector4.Zero);
			textProgram.MVPMatrix.SetValue(ref _textMatrix);
			gL.DepthFunc(GL.ALWAYS);
			_textRenderer.Draw();
			gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
		}

		public void UpdateOrthographicProjectionMatrix(int width, int height)
		{
			Matrix.CreateOrthographicOffCenter(0f, width, 0f, height, 0.1f, 1000f, out _orthographicProjectionMatrix);
		}

		private void UpdateTooltip(ref Matrix viewProjectionMatrix)
		{
			float x = WindowSize.X;
			float y = WindowSize.Y;
			float num = 16f / (float)_font.BaseSize;
			Vector3 position = Position;
			float num2 = 3f;
			_textRenderer.Text = TooltipText;
			Vector2 screenPosition = ScreenPosition;
			Vector3 position2 = new Vector3(0f - _textRenderer.GetHorizontalOffset(TextRenderer.TextAlignment.Left), 0f - _textRenderer.GetVerticalOffset(TextRenderer.TextVerticalAlignment.Top) - num2, -1f);
			Matrix.CreateTranslation(ref position2, out _tempMatrix);
			Matrix.CreateScale(num, out _matrix);
			Matrix.Multiply(ref _tempMatrix, ref _matrix, out _matrix);
			Matrix.AddTranslation(ref _matrix, screenPosition.X + num2, screenPosition.Y - num2, 0f);
			Matrix.Multiply(ref _matrix, ref _orthographicProjectionMatrix, out _textMatrix);
			Vector3 scales = new Vector3(_textRenderer.GetHorizontalOffset(TextRenderer.TextAlignment.Right) + num2 * 6f, _textRenderer.GetVerticalOffset(TextRenderer.TextVerticalAlignment.Top), 1f);
			Matrix.CreateScale(ref scales, out _matrix);
			Matrix.CreateScale(num, out _tempMatrix);
			Matrix.Multiply(ref _tempMatrix, ref _matrix, out _matrix);
			_progressMatrix = _matrix;
			Matrix.AddTranslation(ref _matrix, screenPosition.X, screenPosition.Y - scales.Y * num, 0f);
			Matrix.Multiply(ref _matrix, ref _orthographicProjectionMatrix, out _backgroundMatrix);
			Matrix.CreateScale(Progress, -0.15f, 1f, out _matrix);
			Matrix.Multiply(ref _progressMatrix, ref _matrix, out _progressMatrix);
			Matrix.AddTranslation(ref _progressMatrix, screenPosition.X, screenPosition.Y - scales.Y * num, 0f);
			Matrix.Multiply(ref _progressMatrix, ref _orthographicProjectionMatrix, out _progressMatrix);
		}

		private static Vector2 WorldToScreenPos(ref Matrix viewProjectionMatrix, float viewWidth, float viewHeight, Vector3 worldPosition)
		{
			Matrix matrix = Matrix.CreateTranslation(worldPosition);
			Matrix.Multiply(ref matrix, ref viewProjectionMatrix, out matrix);
			Vector3 vector = matrix.Translation / matrix.M44;
			return new Vector2((vector.X / 2f + 0.5f) * viewWidth, (vector.Y / 2f + 0.5f) * viewHeight);
		}
	}

	public enum EditorMode
	{
		None,
		FreeMove,
		RotateHead,
		RotateBody,
		Translate
	}

	public enum EditorSelectionMode
	{
		Keyframe,
		Actor,
		Scene
	}

	private enum NodeType
	{
		Keyframe,
		CurveHandle
	}

	private enum Keybind
	{
		TogglePause,
		ToggleDisplay,
		ToggleCamera,
		RestartScene,
		FrameIncrement,
		FrameDecrement,
		KeyframeIncrement,
		KeyframeDecrement,
		FrameTimeDecrease,
		FrameTimeIncrease,
		AddKeyframe,
		RemoveKeyframe,
		OriginAction,
		EditKeyframe,
		CycleSelectionMode
	}

	[CoherentType]
	public class KeyframeEvent
	{
		[CoherentProperty("actor")]
		public int Actor;

		[CoherentProperty("keyframe")]
		public int Keyframe;

		[CoherentProperty("frame")]
		public int Frame;

		[CoherentProperty("objectType")]
		public ActorType ObjectType;

		[CoherentProperty("visible")]
		public bool Visible;
	}

	[CoherentType]
	public class KeyframeSettingEvent : KeyframeEvent
	{
		[CoherentProperty("settingName")]
		public string SettingName;

		[CoherentProperty("settingValue")]
		public string SettingValue;

		[CoherentProperty("settingType")]
		public string SettingType;
	}

	[CoherentType]
	public class KeyframeEventEvent
	{
		[CoherentProperty("actor")]
		public int Actor;

		[CoherentProperty("keyframe")]
		public int Keyframe;

		[CoherentProperty("event")]
		public int Event = -1;

		[CoherentProperty("type")]
		public string Type;

		[CoherentProperty("options")]
		public string Options;
	}

	private readonly Dictionary<string, GameInstance.Command> _subCommands = new Dictionary<string, GameInstance.Command>();

	private readonly JsonSerializerSettings _serializerSettings;

	private readonly MachinimaEditorSettings _settings;

	private bool _running;

	private bool _continousPlayback;

	private bool _autoRestartScene = true;

	private long _lastFrameTick;

	private long _nextAutosaveTick;

	private float _msTimePerFrame;

	private const string MachinimaToolName = "EditorTool_Machinima";

	private readonly HitDetection.RaycastOptions _toolRaycastOptions = new HitDetection.RaycastOptions
	{
		IgnoreFluids = true,
		CheckOversizedBoxes = true,
		CheckOnlyTangibleEntities = false
	};

	private readonly RotationGizmo _rotationGizmo;

	private readonly TranslationGizmo _translationGizmo;

	private readonly BoxRenderer _boxRenderer;

	private readonly TextRenderer _textRenderer;

	private readonly LineRenderer _curvePathRenderer;

	private readonly Tooltip _tooltip;

	private float _targetDistance;

	private Vector3 _lastKeyframePosition = Vector3.Zero;

	private CurveHandle _hoveredGrip;

	private CurveHandle _selectedGrip;

	private NodeType _selectedNodeType = NodeType.Keyframe;

	private Dictionary<Keybind, SDL_Scancode> _keybinds = new Dictionary<Keybind, SDL_Scancode>
	{
		{
			Keybind.TogglePause,
			(SDL_Scancode)19
		},
		{
			Keybind.ToggleDisplay,
			(SDL_Scancode)18
		},
		{
			Keybind.ToggleCamera,
			(SDL_Scancode)6
		},
		{
			Keybind.RestartScene,
			(SDL_Scancode)21
		},
		{
			Keybind.FrameDecrement,
			(SDL_Scancode)80
		},
		{
			Keybind.FrameIncrement,
			(SDL_Scancode)79
		},
		{
			Keybind.KeyframeDecrement,
			(SDL_Scancode)81
		},
		{
			Keybind.KeyframeIncrement,
			(SDL_Scancode)82
		},
		{
			Keybind.FrameTimeDecrease,
			(SDL_Scancode)45
		},
		{
			Keybind.FrameTimeIncrease,
			(SDL_Scancode)46
		},
		{
			Keybind.AddKeyframe,
			(SDL_Scancode)14
		},
		{
			Keybind.RemoveKeyframe,
			(SDL_Scancode)76
		},
		{
			Keybind.OriginAction,
			(SDL_Scancode)74
		},
		{
			Keybind.EditKeyframe,
			(SDL_Scancode)23
		},
		{
			Keybind.CycleSelectionMode,
			(SDL_Scancode)10
		}
	};

	private RotationGizmo.OnRotationChange _onRotationChange = null;

	private OnUseToolItem _onUseToolItem = null;

	private bool _hasInterfaceLoaded;

	private bool _isInterfaceOpen;

	private TrackKeyframe _keyframeClipboard;

	private static readonly string SceneDirectory = Path.Combine(Paths.UserData, "Scenes");

	private static readonly string AutosaveDirectory = Path.Combine(SceneDirectory, "Autosave");

	private static readonly string DemoSceneDirectory = "Tools/Machinima/DemoScenes";

	private MachinimaScene _activeScene;

	private Dictionary<string, MachinimaScene> _scenes = new Dictionary<string, MachinimaScene>();

	public bool Running
	{
		get
		{
			return _running;
		}
		private set
		{
			_running = value;
			_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.sceneRunningChanged", value);
		}
	}

	public bool AutoRestartScene
	{
		get
		{
			return _autoRestartScene;
		}
		set
		{
			_autoRestartScene = value;
			_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.sceneAutoRestartChanged", value);
		}
	}

	public float CurrentFrame { get; private set; }

	public float PlaybackFPS
	{
		get
		{
			return (float)System.Math.Round(1000f / _msTimePerFrame * 100f) / 100f;
		}
		set
		{
			_msTimePerFrame = MathHelper.Min(MathHelper.Max(1000f / value, 0.01f), 1000f);
		}
	}

	public TrackKeyframe HoveredKeyframe { get; private set; }

	public TrackKeyframe SelectedKeyframe { get; private set; }

	public TrackKeyframe ActiveKeyframe { get; private set; }

	public SceneActor HoveredActor { get; private set; }

	public SceneActor SelectedActor { get; private set; }

	public SceneActor ActiveActor { get; private set; }

	public bool ShowEditor { get; private set; } = true;


	public bool ShowPathNodes { get; private set; } = true;


	public bool ShowCameraFrustum { get; private set; } = true;


	public bool ContinousPlaybackEnabled { get; private set; }

	public bool BodyRotateHover { get; private set; }

	public EditorMode EditMode { get; private set; } = EditorMode.None;


	public EditorSelectionMode SelectionMode { get; private set; } = EditorSelectionMode.Keyframe;


	public MachinimaScene ActiveScene
	{
		get
		{
			return _activeScene;
		}
		set
		{
			if (_activeScene != value)
			{
				if (_activeScene != null)
				{
					Autosave(force: true);
					_activeScene.IsActive = false;
				}
				if (value != null && !_scenes.ContainsKey(value.Name))
				{
					AddScene(value);
				}
				_activeScene = value;
				if (_activeScene != null)
				{
					_activeScene.IsActive = true;
				}
				_nextAutosaveTick = 0L;
				ResetScene(doUpdate: true);
			}
		}
	}

	private void RegisterCommands()
	{
		_gameInstance.RegisterCommand("mach", MachinimaCommand);
		_subCommands.Add("scene", SceneCommand);
		_subCommands.Add("actor", ActorCommand);
		_subCommands.Add("event", EventCommand);
		_subCommands.Add("fps", FpsCommand);
		_subCommands.Add("clear", ClearCommand);
		_subCommands.Add("display", DisplayCommand);
		_subCommands.Add("restart", RestartCommand);
		_subCommands.Add("autorestart", AutorestartCommand);
		_subCommands.Add("pause", PauseCommand);
		_subCommands.Add("files", FilesCommand);
		_subCommands.Add("tool", ToolCommand);
		_subCommands.Add("key", KeyCommand);
		_subCommands.Add("save", SaveCommand);
		_subCommands.Add("load", LoadCommand);
		_subCommands.Add("update", UpdateCommand);
		_subCommands.Add("edit", EditCommand);
		_subCommands.Add("speed", SpeedCommand);
		_subCommands.Add("bezier", BezierCommand);
		_subCommands.Add("spline", SplineCommand);
		_subCommands.Add("modeldebug", ModelDebugCommand);
		_subCommands.Add("demo", DemoCommand);
		_subCommands.Add("align", AlignCommand);
		_subCommands.Add("offset", OffsetCommand);
		_subCommands.Add("fixlook", FixLookCommand);
		_subCommands.Add("camera", CameraCommand);
		_subCommands.Add("entitylight", EntityLightCommand);
		_subCommands.Add("zip", ZipCommand);
		_subCommands.Add("autosave", AutosaveCommand);
	}

	private string GetCommandList()
	{
		string text = "Mach Sub Commands: [";
		foreach (KeyValuePair<string, GameInstance.Command> subCommand in _subCommands)
		{
			text = text + subCommand.Key + ", ";
		}
		return text.Substring(0, text.Length - 2) + "]";
	}

	[Usage("mach", new string[] { })]
	[Description("Machinima commands")]
	public void MachinimaCommand(string[] args)
	{
		if (args.Length == 0)
		{
			_gameInstance.Chat.Log("Please enter a mach sub command.");
			_gameInstance.Chat.Log(GetCommandList());
			return;
		}
		_subCommands.TryGetValue(args[0], out var value);
		if (value == null)
		{
			_gameInstance.Chat.Log("Unknown mach sub command! '" + args[0] + "'");
			_gameInstance.Chat.Log(GetCommandList());
		}
		else
		{
			value(args);
		}
	}

	[Usage("mach scene", new string[]
	{
		"list", "clear", "add [name]", "copy", "save [name]", "load [name]", "rotate", "modelupdate", "remove [name]", "set [name]",
		"rename [name]"
	})]
	private void SceneCommand(string[] args)
	{
		if (args.Length < 2)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[1])
		{
		case "list":
			ListScenes();
			break;
		case "clear":
			ClearScenes();
			break;
		case "add":
		{
			string text = ((args.Length < 3) ? "" : args[2]);
			text = GetNextSceneName(string.IsNullOrWhiteSpace(text) ? "scene" : text);
			MachinimaScene scene = new MachinimaScene(_gameInstance, text);
			if (!AddScene(scene, makeActive: true))
			{
				_gameInstance.Chat.Log("A scene already exists with the name '" + text + "'");
			}
			else
			{
				_gameInstance.Chat.Log("Added new scene '" + text + "'");
			}
			break;
		}
		case "save":
		{
			string text2 = ((args.Length > 2 && args[2] != "json") ? args[2] : ActiveScene.Name);
			if (string.IsNullOrEmpty(text2) || !_scenes.ContainsKey(text2))
			{
				if (args.Length < 3)
				{
					_gameInstance.Chat.Log("No active scene found, please specify a scene name, or set one to active");
				}
				else
				{
					_gameInstance.Chat.Log("Unable to find scene '" + args[2] + "'");
				}
				break;
			}
			SceneDataType dataType = (_settings.CompressSaveFiles ? SceneDataType.CompressedFile : SceneDataType.JSONFile);
			if (args.Length > 2)
			{
				if (args[2] == "json")
				{
					dataType = SceneDataType.JSONFile;
				}
				else if (args[2] == "hms")
				{
					dataType = SceneDataType.CompressedFile;
				}
			}
			MachinimaScene scene2 = _scenes[text2];
			SaveSceneFile(scene2, dataType);
			_gameInstance.Chat.Log("'" + text2 + "' scene successfully saved to file");
			break;
		}
		case "copy":
		{
			string text3 = ((args.Length > 2 && args[2] != "json") ? args[2] : ActiveScene.Name);
			if (string.IsNullOrEmpty(text3) || !_scenes.ContainsKey(text3))
			{
				if (args.Length < 3)
				{
					_gameInstance.Chat.Log("No active scene found, please specify a scene name, or set one to active");
				}
				else
				{
					_gameInstance.Chat.Log("Unable to find scene '" + args[2] + "'");
				}
			}
			else
			{
				MachinimaScene scene3 = _scenes[text3];
				SaveSceneFile(scene3, SceneDataType.Clipboard);
				_gameInstance.Chat.Log("'" + text3 + "' scene successfully saved to the clipboard!");
			}
			break;
		}
		case "load":
		{
			MachinimaScene machinimaScene = ((args.Length >= 3) ? LoadSceneFile(Path.Combine(SceneDirectory, args[2])) : LoadSceneFile("clipboard", updateInterface: true, SceneDataType.Clipboard));
			if (machinimaScene != null)
			{
				_gameInstance.Chat.Log("Loaded scene '" + machinimaScene.Name + "'");
				SetActiveScene(machinimaScene.Name);
			}
			break;
		}
		case "rotate":
			_gameInstance.Chat.Log("Please left click an actor keyframe to rotate around, or right click to cancel");
			_onUseToolItem = delegate(InteractionType actionType)
			{
				//IL_000e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0010: Invalid comparison between Unknown and I4
				if ((int)actionType == 1)
				{
					_gameInstance.Chat.Log("Rotate stopped");
					return false;
				}
				Vector3 rotatePosition;
				if (HoveredKeyframe != null)
				{
					ActiveActor = HoveredActor;
					rotatePosition = HoveredKeyframe.GetSetting<Vector3>("Position").Value;
				}
				else
				{
					rotatePosition = ActiveScene.Origin;
				}
				Vector3 currentRotation = Vector3.Zero;
				_rotationGizmo.Show(rotatePosition, currentRotation, delegate(Vector3 newRotation)
				{
					Vector3 rotation = newRotation - currentRotation;
					ActiveScene.Rotate(rotation, rotatePosition);
					currentRotation = newRotation;
					UpdateFrame(0L, forceUpdate: true);
				});
				EditMode = EditorMode.RotateBody;
				return false;
			};
			break;
		case "modelupdate":
		{
			if (ActiveScene == null)
			{
				_gameInstance.Chat.Log("No active scene found");
				break;
			}
			for (int i = 0; i < ActiveScene.Actors.Count; i++)
			{
				if (ActiveScene.Actors[i] is EntityActor entityActor)
				{
					entityActor.UpdateModel(_gameInstance);
				}
			}
			_gameInstance.Chat.Log("Scene models updated");
			break;
		}
		case "remove":
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			if (!RemoveScene(args[2]))
			{
				_gameInstance.Chat.Log("Unable to find scene '" + args[2] + "'");
			}
			else
			{
				_gameInstance.Chat.Log("Removed scene '" + args[2] + "'");
			}
			break;
		case "set":
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			if (!_scenes.ContainsKey(args[2]))
			{
				_gameInstance.Chat.Log("Unable to find scene '" + args[2] + "'");
				break;
			}
			SetActiveScene(args[2]);
			_gameInstance.Chat.Log("Active scene set to '" + args[2] + "'");
			break;
		case "rename":
		{
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			MachinimaScene activeScene = ActiveScene;
			_scenes.Remove(activeScene.Name);
			activeScene.Name = args[2];
			_scenes.Add(activeScene.Name, activeScene);
			SetActiveScene(args[2]);
			_gameInstance.Chat.Log("Active scene set to '" + args[2] + "'");
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("mach actor", new string[] { "list", "clear", "model", "move", "clone", "rotate", "modelupdate", "add [name]", "remove [name]", "set [name]" })]
	private void ActorCommand(string[] args)
	{
		if (args.Length < 2)
		{
			throw new InvalidCommandUsage();
		}
		if (ActiveScene == null)
		{
			_gameInstance.Chat.Log("No active scene found");
			return;
		}
		switch (args[1])
		{
		case "list":
			ActiveScene.ListActors();
			break;
		case "clear":
			ActiveScene.ClearActors();
			break;
		case "model":
			_gameInstance.Chat.Log("Please select an entity to copy the model from.");
			_onUseToolItem = delegate
			{
				Ray lookRay = _gameInstance.CameraModule.GetLookRay();
				_gameInstance.HitDetection.Raycast(lookRay.Position, lookRay.Direction, _toolRaycastOptions, out var _, out var _, out var hasFoundTargetEntity, out var entityHitData);
				if (!hasFoundTargetEntity)
				{
					_gameInstance.Chat.Log("Unable to find an entity in this location, aborting...");
				}
				else
				{
					Entity entity = entityHitData.Entity;
					Model selectedEntityModel = entity.ModelPacket;
					_gameInstance.Chat.Log("Entity selected!");
					_gameInstance.Chat.Log("Now please select the the EntityActor to apply the model to.");
					_onUseToolItem = delegate
					{
						Ray lookRay2 = _gameInstance.CameraModule.GetLookRay();
						_gameInstance.HitDetection.Raycast(lookRay2.Position, lookRay2.Direction, _toolRaycastOptions, out var _, out var _, out var hasFoundTargetEntity2, out var entityHitData2);
						if (!hasFoundTargetEntity2)
						{
							_gameInstance.Chat.Log("Unable to find an Entity in that location, aborting...");
							return false;
						}
						Entity entity2 = entityHitData2.Entity;
						EntityActor actorFromEntity = GetActorFromEntity(entity2);
						if (actorFromEntity == null)
						{
							_gameInstance.Chat.Log("Unable to find an Actor for that Entity, aborting...");
						}
						else if (!(actorFromEntity is PlayerActor))
						{
							actorFromEntity.SetBaseModel(selectedEntityModel);
							_gameInstance.Chat.Log("Model successfully applied to EntityActor '" + actorFromEntity.Name + "'");
						}
						else
						{
							_gameInstance.Chat.Log("Invalid actor type found, aborting...");
						}
						return false;
					};
				}
				return false;
			};
			break;
		case "move":
		case "clone":
		{
			bool isMoving = args[1] == "move";
			_gameInstance.Chat.Log("Please select an actor keyframe to " + (isMoving ? "move" : "copy from"));
			Func<SceneActor, Vector3, SceneActor> cloneActor = delegate(SceneActor actor, Vector3 position)
			{
				Vector3 vector3 = actor.Track.GetStartingPosition() - position;
				SceneActor sceneActor3 = actor.Clone(_gameInstance);
				sceneActor3.Name = ActiveScene.GetNextActorName("clone");
				sceneActor3.Track.OffsetPositions(-vector3);
				sceneActor3.Track.UpdateKeyframeData();
				return sceneActor3;
			};
			_onUseToolItem = delegate
			{
				TrackKeyframe hoveredKeyframe = HoveredKeyframe;
				if (hoveredKeyframe == null)
				{
					_gameInstance.Chat.Log("Unable to find a keyframe in this location, aborting...");
				}
				else
				{
					SceneActor selectedActor = HoveredActor;
					ActiveActor = selectedActor;
					_gameInstance.Chat.Log("Now select an offset position to " + (isMoving ? "move to" : "add"));
					Vector3 keyframeOffset = HoveredKeyframe.GetSetting<Vector3>("Position").Value - HoveredActor.Track.GetStartingPosition();
					_onUseToolItem = delegate(InteractionType actionTypeSub)
					{
						//IL_0001: Unknown result type (might be due to invalid IL or missing references)
						//IL_0003: Invalid comparison between Unknown and I4
						if ((int)actionTypeSub == 1)
						{
							_gameInstance.Chat.Log(isMoving ? "Move cancelled" : "Clone stopped");
							return false;
						}
						Ray lookRay3 = _gameInstance.CameraModule.GetLookRay();
						if (!_gameInstance.InteractionModule.HasFoundTargetBlock)
						{
							_gameInstance.Chat.Log("Invalid position, aborting...");
							return false;
						}
						Vector3 vector = _gameInstance.InteractionModule.TargetBlockHit.HitPosition - keyframeOffset;
						if (isMoving)
						{
							Vector3 vector2 = ActiveActor.Track.GetStartingPosition() - vector;
							ActiveActor.Track.OffsetPositions(-vector2);
							_gameInstance.Chat.Log("Actor offset complete");
							return false;
						}
						SceneActor sceneActor2 = cloneActor(selectedActor, vector);
						ActiveScene.AddActor(sceneActor2, addStartKeyframe: false);
						ActiveActor = sceneActor2;
						selectedActor = sceneActor2;
						_gameInstance.Chat.Log("Clone Finished!");
						_gameInstance.Chat.Log("Continue Cloning with left click, or right to finish");
						return true;
					};
				}
				return false;
			};
			break;
		}
		case "rotate:":
			_gameInstance.Chat.Log("Please left click an actor keyframe to rotate around, or right click to cancel");
			_onUseToolItem = delegate(InteractionType actionType)
			{
				//IL_000e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0010: Invalid comparison between Unknown and I4
				if ((int)actionType == 1)
				{
					_gameInstance.Chat.Log("Rotate stopped");
					return false;
				}
				Vector3 rotateCenter;
				if (HoveredKeyframe != null)
				{
					ActiveActor = HoveredActor;
					rotateCenter = HoveredKeyframe.GetSetting<Vector3>("Position").Value;
				}
				else
				{
					if (ActiveActor == null)
					{
						_gameInstance.Chat.Log("No active actor found, select one and try again");
						return false;
					}
					rotateCenter = ActiveActor.Track.GetStartingPosition();
				}
				Vector3 currentRotation = Vector3.Zero;
				_rotationGizmo.Show(rotateCenter, currentRotation, delegate(Vector3 newRotation)
				{
					Vector3 rotation = newRotation - currentRotation;
					ActiveActor.Track.RotatePath(rotation, rotateCenter);
					currentRotation = newRotation;
					UpdateFrame(0L, forceUpdate: true);
				});
				EditMode = EditorMode.RotateBody;
				return false;
			};
			break;
		case "modelupdate":
			if (ActiveActor == null)
			{
				_gameInstance.Chat.Log("No active actor found");
			}
			else if (ActiveActor is EntityActor)
			{
				(ActiveActor as EntityActor).UpdateModel(_gameInstance);
				_gameInstance.Chat.Log("Entity model updated");
			}
			else
			{
				_gameInstance.Chat.Log("Unable to update model, actor is not an Entity");
			}
			break;
		case "add":
		{
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			string text = args[2];
			string text2 = ((args.Length > 3) ? args[3] : "");
			text2 = ActiveScene.GetNextActorName(string.IsNullOrWhiteSpace(text2) ? text : text2);
			if (ActiveScene.GetActor(text2) != null)
			{
				_gameInstance.Chat.Log("An actor already exists with the name '" + text2 + "' in scene '" + ActiveScene.Name + "'");
				break;
			}
			SceneActor sceneActor;
			switch (text)
			{
			case "player":
				sceneActor = new PlayerActor(_gameInstance, text2);
				break;
			case "entity":
				sceneActor = new EntityActor(_gameInstance, text2, null);
				((EntityActor)sceneActor).SetBaseModel(_gameInstance.LocalPlayer.ModelPacket);
				break;
			case "camera":
				sceneActor = new CameraActor(_gameInstance, text2);
				break;
			case "ref":
				sceneActor = new ReferenceActor(_gameInstance, text2);
				break;
			default:
				_gameInstance.Chat.Log("Invalid actor type '" + text + "', acceptable types are player, entity and camera.");
				return;
			}
			if (!ActiveScene.AddActor(sceneActor))
			{
				_gameInstance.Chat.Log("Error adding '" + text2 + "' to scene '" + ActiveScene.Name + "'");
				sceneActor.Dispose();
				break;
			}
			if (sceneActor is CameraActor)
			{
				sceneActor.Track.Keyframes[0].AddEvent(new CameraEvent(cameraState: true));
			}
			ActiveActor = sceneActor;
			_gameInstance.Chat.Log("New actor '" + text2 + "' added to scene '" + ActiveScene.Name + "'");
			break;
		}
		case "remove":
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			if (!ActiveScene.RemoveActor(args[2]))
			{
				_gameInstance.Chat.Log("Unable to find actor: " + args[2]);
			}
			else
			{
				_gameInstance.Chat.Log("Removed actor: " + args[2]);
			}
			break;
		case "set":
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			if (!ActiveScene.HasActor(args[2]))
			{
				_gameInstance.Chat.Log("Unable to find actor: " + args[2]);
			}
			else
			{
				ActiveActor = ActiveScene.GetActor(args[2]);
			}
			break;
		case "scale":
			if (args.Length < 3)
			{
				throw new InvalidCommandUsage();
			}
			if (ActiveActor == null)
			{
				_gameInstance.Chat.Log("No active actor found");
			}
			else if (ActiveActor is EntityActor entityActor)
			{
				if (float.TryParse(args[2], out var result))
				{
					entityActor.SetScale(result);
					_gameInstance.Chat.Log($"Actor scale set to {result}");
				}
				else
				{
					_gameInstance.Chat.Log("Unable to parse float value from: `" + args[2] + "`");
				}
			}
			else
			{
				_gameInstance.Chat.Log("Active actor must be an entiy type to use this.");
			}
			break;
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("mach event", new string[] { "list", "add target", "add animation [id]", "add command [text]", "add camera [on|off]", "add particle [id]", "remove [id]" })]
	private void EventCommand(string[] args)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		if (args.Length < 2)
		{
			throw new InvalidCommandUsage();
		}
		if (ActiveScene == null)
		{
			_gameInstance.Chat.Log("No active scene found");
		}
		if (ActiveActor == null)
		{
			_gameInstance.Chat.Log("No active actor found in the scene");
		}
		switch (args[1])
		{
		case "list":
			ActiveActor.Track.ListEvents();
			break;
		case "add":
		{
			HytaleClient.InGame.Modules.Machinima.Events.KeyframeEvent newEvent;
			if (args[2] == "target")
			{
				newEvent = new TargetEvent((SceneActor)null);
			}
			else
			{
				if (args.Length < 4)
				{
					throw new InvalidCommandUsage();
				}
				if (args[2] == "animation")
				{
					string text = args[3].Trim();
					AnimationSlot slot = (AnimationSlot)0;
					if (text == "off")
					{
						text = null;
					}
					if (args.Length > 4)
					{
						slot = (AnimationSlot)Enum.Parse(typeof(AnimationSlot), args[4].Trim(), ignoreCase: true);
					}
					newEvent = new AnimationEvent(text, slot);
				}
				else if (args[2] == "command")
				{
					string text2 = "";
					for (int i = 3; i < args.Length; i++)
					{
						text2 = text2 + args[i] + ((i - 1 == args.Length) ? "" : " ");
					}
					newEvent = new CommandEvent(text2);
				}
				else if (args[2] == "camera")
				{
					newEvent = new CameraEvent(bool.Parse(args[3]));
				}
				else
				{
					if (!(args[2] == "particle"))
					{
						_gameInstance.Chat.Log("Invalid event type '" + args[2] + "'");
						break;
					}
					newEvent = new ParticleEvent(args[3]);
				}
			}
			Func<TrackKeyframe, HytaleClient.InGame.Modules.Machinima.Events.KeyframeEvent, bool> addEvent = delegate(TrackKeyframe kFrame, HytaleClient.InGame.Modules.Machinima.Events.KeyframeEvent kEvent)
			{
				try
				{
					kFrame.AddEvent(kEvent);
					_gameInstance.Chat.Log($"New event added to keyframe at frame {kFrame.Frame}");
					return true;
				}
				catch (TrackKeyframe.DuplicateKeyframeEvent)
				{
					_gameInstance.Chat.Log("Only one instance of that event may be added to a keyframe, aborting...");
					return false;
				}
			};
			_gameInstance.Chat.Log("Please select the keyframe the event should be added to.");
			_onUseToolItem = delegate
			{
				TrackKeyframe hoveredFrame = HoveredKeyframe;
				if (hoveredFrame == null)
				{
					_gameInstance.Chat.Log("Unable to find a keyframe in this location, aborting...");
				}
				else if (newEvent is TargetEvent)
				{
					_gameInstance.Chat.Log("Please select the keyframe of the actor to target");
					_onUseToolItem = delegate
					{
						TrackKeyframe hoveredKeyframe = HoveredKeyframe;
						SceneActor hoveredActor = HoveredActor;
						newEvent = new TargetEvent((hoveredKeyframe == null) ? null : HoveredActor);
						addEvent(hoveredFrame, newEvent);
						return false;
					};
				}
				else
				{
					addEvent(hoveredFrame, newEvent);
				}
				return false;
			};
			break;
		}
		case "remove":
		{
			if (args.Length < 3)
			{
				_gameInstance.Chat.Log("Plese enter the keyframe event id # to remove");
				break;
			}
			if (!int.TryParse(args[2], out var result))
			{
				_gameInstance.Chat.Log("Unable to parse a keyframe id from '" + args[2] + "'");
				break;
			}
			TrackKeyframe eventKeyframe = ActiveScene.GetEventKeyframe(result);
			if (eventKeyframe == null)
			{
				_gameInstance.Chat.Log($"Unable to find event with id '{result}'");
				break;
			}
			eventKeyframe.RemoveEvent(result);
			_gameInstance.Chat.Log("Event succesfully removed from keyframe");
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("mach fps", new string[] { "[speed]" })]
	private void FpsCommand(string[] args)
	{
		if (args.Length < 2)
		{
			_gameInstance.Chat.Log($"Playback FPS currently set to {PlaybackFPS}");
			return;
		}
		if (!float.TryParse(args[1], out var result))
		{
			_gameInstance.Chat.Log("Unable to parse number from " + args[1]);
			return;
		}
		PlaybackFPS = result;
		_gameInstance.Chat.Log($"Playback FPS set to {PlaybackFPS}");
	}

	[Usage("mach clear", new string[] { })]
	private void ClearCommand(string[] args)
	{
		ClearScenes();
	}

	[Usage("mach display", new string[] { })]
	private void DisplayCommand(string[] args)
	{
		ShowEditor = !ShowEditor;
		string text = (ShowEditor ? "Enabled" : "Disabled");
		_gameInstance.Chat.Log("Path Display " + text);
	}

	[Usage("mach restart", new string[] { })]
	private void RestartCommand(string[] args)
	{
		ResetScene(doUpdate: true);
		_gameInstance.Chat.Log("Playback restarted.");
	}

	[Usage("mach autorestart", new string[] { })]
	private void AutorestartCommand(string[] args)
	{
		AutoRestartScene = !AutoRestartScene;
		string text = (AutoRestartScene ? "Enabled" : "Disabled");
		_gameInstance.Chat.Log("AutoRestart " + text);
	}

	[Usage("mach pause", new string[] { })]
	private void PauseCommand(string[] args)
	{
		Running = !Running;
		if (Running)
		{
			_gameInstance.Chat.Log($"Playback started at frame {CurrentFrame}");
			_lastFrameTick = GetCurrentTime();
		}
		else
		{
			_gameInstance.Chat.Log($"Paused at frame {CurrentFrame}");
		}
	}

	[Usage("mach files", new string[] { })]
	private void FilesCommand(string[] args)
	{
		Process.Start(SceneDirectory);
	}

	[Usage("mach tool", new string[] { })]
	private void ToolCommand(string[] args)
	{
		GiveMachinimaTool();
	}

	[Usage("mach key", new string[] { })]
	private void KeyCommand(string[] args)
	{
		if (ActiveActor == null)
		{
			_gameInstance.Chat.Log("No active actor found, please select a keyframe to set.");
			return;
		}
		SceneTrack track = ActiveActor.Track;
		TrackKeyframe trackKeyframe;
		if (args.Length > 1 && args[1] == "copy")
		{
			if (ActiveActor == null)
			{
				_gameInstance.Chat.Log("No Active Actor found!");
				return;
			}
			trackKeyframe = ActiveKeyframe.Clone();
			trackKeyframe.Frame = CurrentFrame;
		}
		else if (CurrentFrame >= track.GetTrackLength() || args.Length > 1)
		{
			float result = CurrentFrame;
			if (CurrentFrame == track.GetTrackLength())
			{
				result += (float)_gameInstance.App.Settings.MachinimaEditorSettings.NewKeyframeFrameOffset;
			}
			if (args.Length > 1 && !float.TryParse(args[1], out result))
			{
				_gameInstance.Chat.Log($"Unable to parse number from {args[1]}, using default value {result}");
			}
			PlayerEntity localPlayer = _gameInstance.LocalPlayer;
			Vector3 zero = Vector3.Zero;
			if (track.Parent is CameraActor)
			{
				zero.Y = localPlayer.EyeOffset;
			}
			Vector3 position = localPlayer.Position + zero;
			Vector3 lookOrientation = localPlayer.LookOrientation;
			Vector3 bodyOrientation = localPlayer.BodyOrientation;
			bodyOrientation.Y = MathHelper.WrapAngle(bodyOrientation.Y);
			lookOrientation.Y -= bodyOrientation.Y;
			trackKeyframe = ActiveActor.CreateKeyframe(result, position, bodyOrientation, lookOrientation);
			CurrentFrame = result;
		}
		else
		{
			trackKeyframe = track.GetCurrentFrame(CurrentFrame);
		}
		track.AddKeyframe(trackKeyframe);
		UpdateFrame(0L, forceUpdate: true);
		_gameInstance.Chat.Log($"Added key at frame {CurrentFrame}");
	}

	[Usage("mach save", new string[] { })]
	private void SaveCommand(string[] args)
	{
		SaveAllScenesToFile();
		_gameInstance.Chat.Log($"{_scenes.Count} scenes saved to file.");
	}

	[Usage("mach load", new string[] { })]
	private void LoadCommand(string[] args)
	{
		LoadAllScenesFromFile();
		SetActiveScene();
		_gameInstance.Chat.Log($"{_scenes.Count} scenes loaded from file.");
	}

	[Usage("mach update", new string[] { })]
	private void UpdateCommand(string[] args)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		string text = ((args.Length > 1) ? args[1] : "*");
		byte[] array = ActiveScene.ToCompressedByteArray(_serializerSettings);
		sbyte[] array2 = Array.ConvertAll(array, (byte b) => (sbyte)b);
		_gameInstance.Connection.SendPacket((ProtoPacket)new UpdateMachinimaScene(text, ActiveScene.Name, 0f, (SceneUpdateType)0, array2));
	}

	[Usage("mach edit", new string[] { })]
	private void EditCommand(string[] args)
	{
		bool editFrame = args.Length > 1 && args[1] == "frame";
		_gameInstance.Chat.Log("Please select the keyframe to " + (editFrame ? "change the frame of" : "edit"));
		_onUseToolItem = delegate
		{
			TrackKeyframe hoveredKeyframe = HoveredKeyframe;
			if (hoveredKeyframe == null)
			{
				_gameInstance.Chat.Log("Unable to find a keyframe in this location, aborting...");
			}
			else
			{
				ActiveActor = HoveredActor;
				if (editFrame)
				{
					if (HoveredActor.Track.GetKeyframeByFrame(CurrentFrame) == null)
					{
						hoveredKeyframe.Frame = CurrentFrame;
						HoveredActor.Track.UpdateKeyframeData();
						_gameInstance.Chat.Log("Keyframe updated.");
					}
					else
					{
						_gameInstance.Chat.Log($"Unable to change keyframe, one is already set at frame #{CurrentFrame}");
					}
					return false;
				}
				ActiveActor.Visible = false;
				KeyframeSetting<Vector3> postionSetting = hoveredKeyframe.GetSetting<Vector3>("Position");
				KeyframeSetting<Vector3> lookSetting = hoveredKeyframe.GetSetting<Vector3>("Look");
				KeyframeSetting<Vector3> rotationSetting = hoveredKeyframe.GetSetting<Vector3>("Rotation");
				if (postionSetting != null)
				{
					_ = postionSetting.Value;
					if (true)
					{
						Vector3 value = postionSetting.Value;
						if (ActiveActor is CameraActor)
						{
							value -= new Vector3(0f, _gameInstance.LocalPlayer.EyeOffset, 0f);
						}
						_gameInstance.LocalPlayer.SetPosition(value);
					}
				}
				if (rotationSetting != null)
				{
					_ = rotationSetting.Value;
					if (true)
					{
						_gameInstance.LocalPlayer.SetBodyOrientation(rotationSetting.Value);
					}
				}
				if (lookSetting != null)
				{
					_ = lookSetting.Value;
					if (true)
					{
						_gameInstance.LocalPlayer.LookOrientation = lookSetting.Value;
						if (rotationSetting != null)
						{
							_ = rotationSetting.Value;
							if (true)
							{
								_gameInstance.LocalPlayer.LookOrientation.Y += rotationSetting.Value.Y;
							}
						}
					}
				}
				CurrentFrame = hoveredKeyframe.Frame;
				_gameInstance.Chat.Log("Adjust your position and rotation then left click to set, or right click to cancel");
				_onUseToolItem = delegate(InteractionType actionTypeSub)
				{
					//IL_0070: Unknown result type (might be due to invalid IL or missing references)
					//IL_0072: Invalid comparison between Unknown and I4
					ActiveActor.Visible = true;
					Vector3 lookOrientation = _gameInstance.LocalPlayer.LookOrientation;
					Vector3 bodyOrientation = _gameInstance.LocalPlayer.BodyOrientation;
					bodyOrientation.Y = MathHelper.WrapAngle(bodyOrientation.Y);
					lookOrientation.Y -= bodyOrientation.Y;
					if ((int)actionTypeSub == 1)
					{
						_gameInstance.Chat.Log("Keyframe edit cancelled");
						return false;
					}
					if (postionSetting != null)
					{
						_ = postionSetting.Value;
						if (true)
						{
							Vector3 position = _gameInstance.LocalPlayer.Position;
							if (ActiveActor is CameraActor)
							{
								position += new Vector3(0f, _gameInstance.LocalPlayer.EyeOffset, 0f);
							}
							postionSetting.Value = position;
						}
					}
					if (rotationSetting != null)
					{
						_ = rotationSetting.Value;
						if (true)
						{
							rotationSetting.Value = bodyOrientation;
						}
					}
					if (lookSetting != null)
					{
						_ = lookSetting.Value;
						if (true)
						{
							lookSetting.Value = lookOrientation;
						}
					}
					ActiveActor.Track.UpdatePositions();
					ActiveScene?.Update(CurrentFrame);
					_gameInstance.Chat.Log("Keyframe updated.");
					return false;
				};
			}
			return false;
		};
	}

	[Usage("mach speed", new string[] { })]
	private void SpeedCommand(string[] args)
	{
		if (ActiveActor == null)
		{
			return;
		}
		ResetScene(doUpdate: true);
		bool flag = args.Length > 2 && args[2] == "scale";
		if (args.Length > 1)
		{
			if (!float.TryParse(args[1], out var result))
			{
				_gameInstance.Chat.Log("Unable to parse number from " + args[1]);
				return;
			}
			if (flag)
			{
				ActiveActor.Track.ScalePathSpeed(result);
			}
			else
			{
				ActiveActor.Track.SetPathSegmentSpeed(result / PlaybackFPS, 0, ActiveActor.Track.Keyframes.Count - 1);
			}
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < ActiveActor.Track.Keyframes.Count - 1; i++)
		{
			float num4 = ActiveActor.Track.GetPathSegmentSpeed(i) * PlaybackFPS;
			if (i == 0)
			{
				num = num4;
				num2 = num4;
			}
			else
			{
				if (num4 < num)
				{
					num = num4;
				}
				if (num4 > num2)
				{
					num2 = num4;
				}
			}
			num3 += num4;
		}
		double num5 = System.Math.Round(num3 / (float)(ActiveActor.Track.Keyframes.Count - 1), 2);
		_gameInstance.Chat.Log($"Speed Avg: {num5}, Min: {System.Math.Round(num, 2)}, Max: {System.Math.Round(num2, 2)}");
	}

	[Usage("mach bezier", new string[] { })]
	private void BezierCommand(string[] args)
	{
		if (ActiveActor == null)
		{
			_gameInstance.Chat.Log("No active actor found!");
			return;
		}
		if (args.Length > 1 && args[1] == "smooth")
		{
			if (!(ActiveActor.Track.Path is BezierPath))
			{
				ActiveActor.Track.SetPathType(SceneTrack.TrackPathType.Bezier);
			}
			ActiveActor.Track.SmoothBezierPath();
		}
		else if (args.Length > 1 && args[1] == "reset")
		{
			ActiveActor.Track.SetPathType(SceneTrack.TrackPathType.Bezier, reset: true);
		}
		else
		{
			ActiveActor.Track.SetPathType(SceneTrack.TrackPathType.Bezier);
		}
		_gameInstance.Chat.Log("Bezier path updated.");
	}

	[Usage("mach spline", new string[] { })]
	private void SplineCommand(string[] args)
	{
		if (ActiveActor == null)
		{
			_gameInstance.Chat.Log("No active actor found!");
		}
		else
		{
			ActiveActor.Track.SetPathType(SceneTrack.TrackPathType.Spline);
		}
	}

	[Usage("mach modeldebug", new string[] { })]
	private void ModelDebugCommand(string[] args)
	{
		if (ActiveScene == null)
		{
			_gameInstance.Chat.Log("No active scene found");
			return;
		}
		int num = 0;
		string text = "";
		for (int i = 0; i < ActiveScene.Actors.Count; i++)
		{
			SceneActor sceneActor = ActiveScene.Actors[i];
			if (sceneActor is EntityActor && string.IsNullOrWhiteSpace((sceneActor as EntityActor).ModelId))
			{
				text = text + sceneActor.Name + ", ";
				num++;
			}
		}
		_gameInstance.Chat.Log($"{num} actors found with missing models");
		if (num > 0)
		{
			_gameInstance.Chat.Log(text);
		}
	}

	[Usage("mach demo", new string[] { "[1|2|3|4|5|6|7]" })]
	private void DemoCommand(string[] args)
	{
		ResetScene(doUpdate: true);
		int num = 1;
		if (args.Length > 1)
		{
			if (!int.TryParse(args[1], out var result))
			{
				_gameInstance.Chat.Log("Invalid demo scene int number: " + args[1]);
				return;
			}
			num = result;
		}
		string key = $"{DemoSceneDirectory}/demo{num}.hms";
		if (!_gameInstance.HashesByServerAssetPath.TryGetValue(key, out var value))
		{
			_gameInstance.Chat.Log($"Unable to find demo scene #{num}");
			return;
		}
		byte[] assetUsingHash = AssetManager.GetAssetUsingHash(value);
		MachinimaScene activeScene = MachinimaScene.FromCompressedByteArray(assetUsingHash, _gameInstance, _serializerSettings);
		ActiveScene = activeScene;
		_gameInstance.LocalPlayer.LookOrientation = ActiveScene.OriginLook;
		ActiveScene.OffsetOrigin(_gameInstance.LocalPlayer.Position);
		ActiveScene.Update(0f);
		_gameInstance.Chat.Log($"Demo scene #{num} loaded.");
	}

	[Usage("mach align [true|false]", new string[] { })]
	private void AlignCommand(string[] args)
	{
		if (ActiveActor == null)
		{
			_gameInstance.Chat.Log("No active actor found!");
			return;
		}
		ResetScene(doUpdate: true);
		bool result = false;
		if (args.Length > 1)
		{
			bool.TryParse(args[1], out result);
		}
		ActiveActor.AlignToPath(result);
	}

	[Usage("mach offset", new string[] { })]
	private void OffsetCommand(string[] args)
	{
		if (ActiveActor == null)
		{
			_gameInstance.Chat.Log("No active actor found!");
			return;
		}
		int result = 0;
		int result2 = 0;
		if (args.Length > 1 && !int.TryParse(args[1], out result2))
		{
			_gameInstance.Chat.Log("Invalid value found for offset amount " + args[1] + "!");
			return;
		}
		if (args.Length > 2 && !int.TryParse(args[2], out result))
		{
			_gameInstance.Chat.Log("Invalid value found for insert frame " + args[1] + "!");
			return;
		}
		ResetScene(doUpdate: true);
		foreach (SceneActor actor in ActiveScene.Actors)
		{
			actor.Track.InsertKeyframeOffset(result, result2);
		}
		ResetScene();
		UpdateFrame(0L, forceUpdate: true);
		_gameInstance.Chat.Log("Offset actor keyframes!");
	}

	[Usage("mach fixlook", new string[] { })]
	private void FixLookCommand(string[] args)
	{
		if (ActiveScene == null)
		{
			return;
		}
		ResetScene(doUpdate: true);
		foreach (SceneActor actor in ActiveScene.Actors)
		{
			foreach (TrackKeyframe keyframe in actor.Track.Keyframes)
			{
				Vector3 value = keyframe.GetSetting<Vector3>("Look").Value;
				value.Y -= keyframe.GetSetting<Vector3>("Rotation").Value.Y;
				keyframe.GetSetting<Vector3>("Look").Value = value;
			}
			actor.Track.UpdateKeyframeData();
		}
		ResetScene(doUpdate: true);
		_gameInstance.Chat.Log("Keyframe look settings fixed!");
	}

	[Usage("mach camera", new string[] { })]
	private void CameraCommand(string[] args)
	{
		CameraActor cameraActor = null;
		if (ActiveActor is CameraActor)
		{
			cameraActor = ActiveActor as CameraActor;
		}
		else
		{
			List<SceneActor> actors = ActiveScene.GetActors();
			foreach (SceneActor item in actors)
			{
				if (item is CameraActor)
				{
					cameraActor = item as CameraActor;
					break;
				}
			}
		}
		if (cameraActor == null)
		{
			_gameInstance.Chat.Log("Unable to find any cameras in the current scene.");
		}
		else
		{
			cameraActor.SetState(!cameraActor.Active);
		}
	}

	[Usage("mach entitylight", new string[] { })]
	private void EntityLightCommand(string[] args)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		if (args.Length < 2)
		{
			_gameInstance.Chat.Log("Please add the name of the actor to change the light value");
			return;
		}
		string text = args[1];
		SceneActor actor = ActiveScene.GetActor(text);
		if (actor == null)
		{
			_gameInstance.Chat.Log("Unable to find an actor with name " + text);
		}
		else if (actor is EntityActor entityActor)
		{
			if (args.Length < 5)
			{
				entityActor.GetEntity().SetDynamicLight(null);
				return;
			}
			byte.TryParse(args[2], out var result);
			byte.TryParse(args[3], out var result2);
			byte.TryParse(args[4], out var result3);
			ColorLight dynamicLight = new ColorLight((sbyte)10, (sbyte)result, (sbyte)result2, (sbyte)result3);
			entityActor.GetEntity().SetDynamicLight(dynamicLight);
		}
		else
		{
			_gameInstance.Chat.Log("Only entity actors may have there light value set");
		}
	}

	[Usage("mach zip", new string[] { "[true|false]" })]
	private void ZipCommand(string[] args)
	{
		bool flag = !_settings.CompressSaveFiles;
		if (args.Length > 1)
		{
			if (!bool.TryParse(args[1].ToLower(), out var result))
			{
				_gameInstance.Chat.Log("Unable to parse bool value: '" + args[1] + "'");
				return;
			}
			flag = result;
		}
		_settings.CompressSaveFiles = flag;
		_gameInstance.App.Settings.Save();
		_gameInstance.Chat.Log("Scene file compression " + (flag ? "enabled" : "disabled") + ".");
		Autosave(force: true);
	}

	[Usage("mach autosave", new string[] { "[seconds]" })]
	private void AutosaveCommand(string[] args)
	{
		if (args.Length == 1)
		{
			_gameInstance.Chat.Log($"Autosave Delay: {_settings.AutosaveDelay} secs");
			return;
		}
		if (!int.TryParse(args[1].ToLower(), out var result))
		{
			_gameInstance.Chat.Log("Unable to parse int value: '" + args[1] + "'");
			return;
		}
		if (result < 10 || result > 600)
		{
			_gameInstance.Chat.Log($"Delay must be between {10} and {600}");
			return;
		}
		_settings.AutosaveDelay = result;
		_gameInstance.App.Settings.Save();
		_gameInstance.Chat.Log($"Autosave delay set to: {result} secs.");
	}

	public MachinimaModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		_serializerSettings = new JsonSerializerSettings
		{
			Converters = { (JsonConverter)(object)new MachinimaSceneJsonConverter(gameInstance, this) }
		};
		_settings = _gameInstance.App.Settings.MachinimaEditorSettings;
		_gameInstance = gameInstance;
		GraphicsDevice graphics = gameInstance.Engine.Graphics;
		FontFamily defaultFontFamily = gameInstance.App.Fonts.DefaultFontFamily;
		_rotationGizmo = new RotationGizmo(graphics, defaultFontFamily.RegularFont, OnRotationChange);
		_translationGizmo = new TranslationGizmo(graphics, OnPositionChange);
		_boxRenderer = new BoxRenderer(graphics, graphics.GPUProgramStore.BasicProgram);
		_textRenderer = new TextRenderer(graphics, defaultFontFamily.RegularFont, "Entity");
		_curvePathRenderer = new LineRenderer(graphics, graphics.GPUProgramStore.BasicProgram);
		_tooltip = new Tooltip(gameInstance.Engine.Graphics, gameInstance.App.Fonts.DefaultFontFamily.RegularFont);
		int width = _gameInstance.Engine.Window.Viewport.Width;
		int height = _gameInstance.Engine.Window.Viewport.Height;
		_tooltip.UpdateOrthographicProjectionMatrix(width, height);
		_tooltip.ScreenPosition = new Vector2(5f, height - 5);
		Directory.CreateDirectory(SceneDirectory);
		Directory.CreateDirectory(AutosaveDirectory);
		PlaybackFPS = 60f;
		RegisterCommands();
		RegisterEvents();
	}

	protected override void DoDispose()
	{
		Autosave(force: true);
		UnregisterEvents();
		foreach (MachinimaScene value in _scenes.Values)
		{
			value.Dispose();
		}
		_rotationGizmo.Dispose();
		_translationGizmo.Dispose();
		_boxRenderer.Dispose();
		_textRenderer.Dispose();
		_curvePathRenderer.Dispose();
		_tooltip.Dispose();
	}

	[Obsolete]
	public override void Tick()
	{
		long currentTime = GetCurrentTime();
		long num = currentTime - _lastFrameTick;
		if (HasActiveTool())
		{
			TickEditor(num);
			Autosave();
		}
		_lastFrameTick = currentTime;
	}

	[Obsolete]
	public override void OnNewFrame(float deltaTime)
	{
		if (Running)
		{
			UpdateFrame((long)(deltaTime * 1000f));
		}
	}

	private void UpdateFrame(long deltaTime = 0L, bool forceUpdate = false)
	{
		if (ActiveScene != null && (Running || forceUpdate))
		{
			int num = (int)CurrentFrame;
			float sceneLength = ActiveScene.GetSceneLength();
			if (CurrentFrame >= sceneLength && Running)
			{
				OnSceneEnd();
			}
			else
			{
				float num2 = (float)deltaTime / _msTimePerFrame;
				CurrentFrame = MathHelper.Max(CurrentFrame + num2, 0f);
			}
			if (ActiveScene != null)
			{
				ActiveScene.Update(CurrentFrame);
				float sceneLength2 = ActiveScene.GetSceneLength();
				_tooltip.Progress = CurrentFrame / sceneLength2;
				_tooltip.TooltipText = $"Frame: {System.Math.Round(CurrentFrame)}";
			}
			if (_isInterfaceOpen && (int)CurrentFrame != num)
			{
				UpdateCurrentFrameInInterface();
			}
		}
	}

	public void Draw(ref Matrix viewProjectionMatrix)
	{
		if (!HasActiveTool())
		{
			return;
		}
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		GLFunctions gL = graphics.GL;
		if ((EditMode == EditorMode.RotateHead || EditMode == EditorMode.RotateBody) && _rotationGizmo.Visible)
		{
			_rotationGizmo.Draw(ref viewProjectionMatrix, _gameInstance.CameraModule.Controller, Vector3.Zero);
		}
		if (EditMode == EditorMode.Translate)
		{
			_translationGizmo.Draw(ref viewProjectionMatrix, Vector3.Zero);
		}
		gL.DepthFunc(GL.ALWAYS);
		if (ShowEditor && ActiveScene != null)
		{
			ActiveScene.Draw(ref viewProjectionMatrix);
			_boxRenderer.Draw(ActiveScene.Origin, TrackKeyframe.PathBox, viewProjectionMatrix, graphics.WhiteColor, 0.7f, graphics.WhiteColor, 0.2f);
		}
		_tooltip.DrawBackground(ref viewProjectionMatrix);
		if (ActiveActor != null && ActiveKeyframe != null && ActiveActor.Track.PathType == SceneTrack.TrackPathType.Bezier && SelectionMode == EditorSelectionMode.Keyframe)
		{
			foreach (TrackKeyframe keyframe in ActiveActor.Track.Keyframes)
			{
				if (keyframe.Frame >= ActiveActor.Track.GetTrackLength())
				{
					continue;
				}
				Vector3 value = keyframe.GetSetting<Vector3>("Position").Value;
				KeyframeSetting<Vector3[]> setting = keyframe.GetSetting<Vector3[]>("Curve");
				Vector3 yellowColor = graphics.YellowColor;
				if (setting == null)
				{
					continue;
				}
				int nextKeyframe = ActiveActor.Track.GetNextKeyframe(keyframe.Frame);
				float num = 0.7f;
				if (_selectedGrip == null)
				{
					num = 0.3f;
				}
				Vector3[] value2 = setting.Value;
				Vector3[] array = new Vector3[value2.Length + 2];
				array[0] = value;
				for (int i = 0; i < value2.Length; i++)
				{
					bool flag = _hoveredGrip != null && _hoveredGrip.Matches(ActiveActor, keyframe, i);
					bool flag2 = flag && _selectedGrip != null;
					float num2 = num;
					if (flag || flag2)
					{
						num2 = 0.8f;
					}
					Vector3 vector = (flag2 ? graphics.CyanColor : (flag ? graphics.MagentaColor : graphics.YellowColor));
					_boxRenderer.Draw(value2[i] + value, TrackKeyframe.PathBox, viewProjectionMatrix, vector, num2, vector, num2 * 0.25f);
					array[i + 1] = value2[i] + value;
				}
				if (nextKeyframe != -1)
				{
					TrackKeyframe trackKeyframe = ActiveActor.Track.Keyframes[nextKeyframe];
					if (trackKeyframe.Frame != keyframe.Frame)
					{
						array[^1] = trackKeyframe.GetSetting<Vector3>("Position").Value;
						_curvePathRenderer.UpdateLineData(array);
						_curvePathRenderer.Draw(ref viewProjectionMatrix, yellowColor, num);
					}
				}
			}
		}
		gL.DepthFunc((!graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}

	public void DrawText(ref Matrix viewProjectionMatrix)
	{
		if (!TextNeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with TextNeedsDrawing() first before calling this.");
		}
		if (HasActiveTool())
		{
			if (EditMode == EditorMode.RotateHead || EditMode == EditorMode.RotateBody)
			{
				_rotationGizmo.DrawText();
			}
			_tooltip.DrawText(ref viewProjectionMatrix);
		}
	}

	public bool NeedsDrawing()
	{
		return HasActiveTool() && ShowEditor && ActiveScene != null;
	}

	public bool TextNeedsDrawing()
	{
		return HasActiveTool() && ShowEditor;
	}

	private void TogglePause()
	{
		Running = !Running;
		if (Running)
		{
			_lastFrameTick = GetCurrentTime();
		}
	}

	private long GetCurrentTime()
	{
		return DateTime.Now.Ticks / 10000;
	}

	public void OnInteraction(InteractionType interactionType)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Invalid comparison between Unknown and I4
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		if (_onUseToolItem != null)
		{
			OnUseToolItem onUseToolItem = _onUseToolItem;
			_onUseToolItem = null;
			if (onUseToolItem(interactionType))
			{
				_onUseToolItem = onUseToolItem;
			}
		}
		else if (_rotationGizmo.Visible && (!_gameInstance.Input.IsAnyModifierHeld() || (int)interactionType == 1 || _rotationGizmo.InUse()))
		{
			_rotationGizmo.OnInteract(interactionType);
			if (!_rotationGizmo.Visible && (EditMode == EditorMode.RotateHead || EditMode == EditorMode.RotateBody))
			{
				SelectedKeyframe = null;
				EditMode = EditorMode.None;
			}
		}
		else if (_translationGizmo.Visible && (!_gameInstance.Input.IsAnyModifierHeld() || (int)interactionType == 1 || _translationGizmo.InUse()))
		{
			Ray lookRay = _gameInstance.CameraModule.GetLookRay();
			_translationGizmo.OnInteract(lookRay, interactionType);
			if (!_translationGizmo.Visible && EditMode == EditorMode.Translate)
			{
				if (_selectedNodeType == NodeType.Keyframe)
				{
					SelectedKeyframe = null;
				}
				else
				{
					_selectedGrip = null;
				}
				EditMode = EditorMode.None;
			}
		}
		else if ((int)interactionType == 0)
		{
			if (EditMode == EditorMode.FreeMove)
			{
				if (_selectedNodeType == NodeType.Keyframe)
				{
					SelectedKeyframe = null;
				}
				else
				{
					_selectedGrip = null;
				}
				EditMode = EditorMode.None;
			}
			else if (EditMode == EditorMode.None && (HoveredKeyframe != null || _hoveredGrip != null))
			{
				SelectKeyframe();
			}
		}
		else if (EditMode == EditorMode.FreeMove)
		{
			if (SelectionMode == EditorSelectionMode.Keyframe)
			{
				OnPositionChange(_lastKeyframePosition);
			}
			else if (SelectedKeyframe != null)
			{
				Vector3 value = SelectedKeyframe.GetSetting<Vector3>("Position").Value;
				Vector3 vector = _lastKeyframePosition - value;
				if (SelectionMode == EditorSelectionMode.Actor)
				{
					ActiveActor.Track.OffsetPositions(vector);
				}
				else
				{
					ActiveScene.OffsetOrigin(ActiveScene.Origin + vector);
				}
			}
			if (_selectedNodeType == NodeType.Keyframe)
			{
				SelectedKeyframe = null;
			}
			else
			{
				_selectedGrip = null;
			}
			EditMode = EditorMode.None;
		}
		else if (EditMode == EditorMode.None && HoveredKeyframe != null)
		{
			ActiveActor = HoveredActor;
			ActiveKeyframe = HoveredKeyframe;
			if (ActiveActor.Track.Keyframes.Count > 1)
			{
				CurrentFrame = HoveredKeyframe.Frame;
				ActiveScene.Update(CurrentFrame);
				UpdateFrame(0L, forceUpdate: true);
			}
		}
	}

	private void TickEditor(float dt)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0664: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_05dd: Unknown result type (might be due to invalid IL or missing references)
		Input input = _gameInstance.Input;
		float num = 1f;
		if (input.IsAltHeld())
		{
			num = 0.5f;
		}
		else if (input.IsShiftHeld())
		{
			num = 2f;
		}
		if (input.IsKeyHeld(_keybinds[Keybind.FrameDecrement]))
		{
			UpdateFrame(-(long)(dt * num), forceUpdate: true);
		}
		if (input.IsKeyHeld(_keybinds[Keybind.FrameIncrement]))
		{
			UpdateFrame((long)(dt * num), forceUpdate: true);
		}
		if (input.ConsumeKey(_keybinds[Keybind.KeyframeDecrement]) && ActiveKeyframe != null && ActiveActor != null)
		{
			int previousKeyframe = ActiveActor.Track.GetPreviousKeyframe(input.IsShiftHeld() ? 0f : (ActiveKeyframe.Frame - 0.01f));
			ActiveKeyframe = ActiveActor.Track.Keyframes[previousKeyframe];
			CurrentFrame = ActiveKeyframe.Frame;
		}
		if (input.ConsumeKey(_keybinds[Keybind.KeyframeIncrement]) && ActiveKeyframe != null && ActiveActor != null)
		{
			int nextKeyframe = ActiveActor.Track.GetNextKeyframe(input.IsShiftHeld() ? ActiveScene.GetSceneLength() : ActiveKeyframe.Frame);
			ActiveKeyframe = ActiveActor.Track.Keyframes[nextKeyframe];
			CurrentFrame = ActiveKeyframe.Frame;
		}
		if (input.ConsumeKey(_keybinds[Keybind.CycleSelectionMode]))
		{
			CycleSelectionMode();
		}
		if (input.ConsumeKey(_keybinds[Keybind.TogglePause]))
		{
			if (input.IsShiftHeld())
			{
				_continousPlayback = !_continousPlayback;
				if (_continousPlayback && !AutoRestartScene)
				{
					AutoRestartScene = true;
				}
				_gameInstance.Chat.Log("Continous playback " + (_continousPlayback ? "enabled" : "disabled"));
			}
			else if (input.IsAltHeld())
			{
				AutoRestartScene = !AutoRestartScene;
				_gameInstance.Chat.Log("Auto scene restart " + (AutoRestartScene ? "enabled" : "disabled"));
			}
			else
			{
				TogglePause();
			}
		}
		if (input.ConsumeKey(_keybinds[Keybind.ToggleDisplay]))
		{
			if (input.IsShiftHeld())
			{
				ShowPathNodes = !ShowPathNodes;
			}
			else
			{
				ShowEditor = !ShowEditor;
			}
		}
		if (input.ConsumeKey(_keybinds[Keybind.ToggleCamera]))
		{
			if (input.IsShiftHeld())
			{
				ShowCameraFrustum = !ShowCameraFrustum;
			}
			else
			{
				_gameInstance.ExecuteCommand(".mach camera");
			}
		}
		if (input.ConsumeKey(_keybinds[Keybind.AddKeyframe]))
		{
			string text = ".mach key";
			if (input.IsAltHeld())
			{
				text += " copy";
			}
			else if (input.IsShiftHeld())
			{
				text += $" {CurrentFrame}";
			}
			_gameInstance.ExecuteCommand(text);
		}
		if (input.ConsumeKey(_keybinds[Keybind.RestartScene]))
		{
			if (input.IsShiftHeld())
			{
				EndScene();
			}
			else
			{
				_gameInstance.ExecuteCommand(".mach restart");
			}
		}
		if (input.ConsumeKey(_keybinds[Keybind.EditKeyframe]))
		{
			if (input.IsAltHeld())
			{
				_gameInstance.ExecuteCommand(".mach edit frame");
			}
			else if (input.IsShiftHeld())
			{
				_gameInstance.ExecuteCommand(".mach actor move");
			}
			else
			{
				_gameInstance.ExecuteCommand(".mach edit");
			}
		}
		if (input.ConsumeKey(_keybinds[Keybind.OriginAction]) && ActiveScene != null)
		{
			if (input.IsShiftHeld())
			{
				ActiveScene.Origin = _gameInstance.LocalPlayer.Position;
				ActiveScene.OriginLook = _gameInstance.LocalPlayer.LookOrientation;
				_gameInstance.Chat.Log("Scene origin set.");
			}
			else if (input.IsAltHeld())
			{
				ActiveScene.OffsetOrigin(_gameInstance.LocalPlayer.Position);
				_gameInstance.Chat.Log("Scene offset to origin.");
			}
			else
			{
				_gameInstance.LocalPlayer.SetPosition(ActiveScene.Origin);
				_gameInstance.LocalPlayer.LookOrientation = ActiveScene.OriginLook;
			}
		}
		if (HoveredKeyframe != null || SelectedKeyframe != null)
		{
			TrackKeyframe trackKeyframe = ((HoveredKeyframe == null) ? SelectedKeyframe : HoveredKeyframe);
			SceneTrack sceneTrack = ((HoveredActor == null) ? SelectedActor.Track : HoveredActor.Track);
			if (input.ConsumeKey(_keybinds[Keybind.RemoveKeyframe]))
			{
				try
				{
					float frame = trackKeyframe.Frame;
					sceneTrack.RemoveKeyframe(frame);
					_gameInstance.Chat.Log($"Removed keyframe for frame {frame}");
				}
				catch (Exception ex)
				{
					_gameInstance.Chat.Error(ex.Message);
				}
			}
			if (trackKeyframe.Frame > 0f)
			{
				if (input.ConsumeKey(_keybinds[Keybind.FrameTimeDecrease]))
				{
					sceneTrack.InsertKeyframeOffset(trackKeyframe.Frame, 1f);
				}
				if (input.ConsumeKey(_keybinds[Keybind.FrameTimeIncrease]))
				{
					sceneTrack.InsertKeyframeOffset(trackKeyframe.Frame, -1f);
				}
			}
		}
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		float targetBlockHitDistance = (_gameInstance.InteractionModule.HasFoundTargetBlock ? _gameInstance.InteractionModule.TargetBlockHit.Distance : 0f);
		if (EditMode == EditorMode.RotateHead || EditMode == EditorMode.RotateBody)
		{
			_rotationGizmo.Tick(lookRay, targetBlockHitDistance);
			_rotationGizmo.UpdateRotation(_gameInstance.Input.IsShiftHeld());
		}
		if (EditMode == EditorMode.Translate)
		{
			_translationGizmo.Tick(lookRay);
		}
		CheckKeyframeHover();
		if (EditMode != EditorMode.FreeMove)
		{
			return;
		}
		Vector3 vector = lookRay.Position + lookRay.Direction * _targetDistance;
		if (_gameInstance.InteractionModule.HasFoundTargetBlock && (_gameInstance.InteractionModule.TargetBlockHit.Distance < _targetDistance || input.IsShiftHeld()) && !input.IsAltHeld())
		{
			vector = _gameInstance.InteractionModule.TargetBlockHit.HitPosition;
		}
		if (SelectionMode == EditorSelectionMode.Keyframe)
		{
			if (_selectedNodeType == NodeType.Keyframe)
			{
				SelectedKeyframe.GetSetting<Vector3>("Position").Value = vector;
				ActiveActor.Track.UpdatePositions();
				if (ActiveActor is EntityActor)
				{
					ActiveActor.Position = vector;
					((EntityActor)ActiveActor).ForceUpdate(_gameInstance);
				}
				return;
			}
			KeyframeSetting<Vector3[]> setting = _selectedGrip.Keyframe.GetSetting<Vector3[]>("Curve");
			Vector3 value = _selectedGrip.Keyframe.GetSetting<Vector3>("Position").Value;
			Vector3[] value2 = setting.Value;
			value2[_selectedGrip.Index] = vector - value;
			setting.Value = value2;
			if (_selectedGrip.UpdateTangent)
			{
				if (_selectedGrip.PrevKeyframe != null)
				{
					KeyframeSetting<Vector3[]> setting2 = _selectedGrip.PrevKeyframe.GetSetting<Vector3[]>("Curve");
					Vector3 value3 = _selectedGrip.PrevKeyframe.GetSetting<Vector3>("Position").Value;
					Vector3[] value4 = setting2.Value;
					value4[1] = value + (vector - value) * -1f - value3;
					setting2.Value = value4;
				}
				if (_selectedGrip.NextKeyframe != null)
				{
					KeyframeSetting<Vector3[]> setting3 = _selectedGrip.NextKeyframe.GetSetting<Vector3[]>("Curve");
					Vector3 value5 = _selectedGrip.NextKeyframe.GetSetting<Vector3>("Position").Value;
					Vector3[] value6 = setting3.Value;
					value6[0] = value5 - vector;
					setting3.Value = value6;
				}
			}
			_selectedGrip.Actor.Track.UpdatePositions();
		}
		else if (_selectedNodeType == NodeType.Keyframe)
		{
			Vector3 value7 = SelectedKeyframe.GetSetting<Vector3>("Position").Value;
			Vector3 vector2 = vector - value7;
			if (SelectionMode == EditorSelectionMode.Actor)
			{
				ActiveActor.Track.OffsetPositions(vector2);
			}
			else
			{
				ActiveScene.OffsetOrigin(ActiveScene.Origin + vector2);
			}
		}
	}

	private void OnRotationChange(Vector3 rotation)
	{
		if (_onRotationChange != null)
		{
			_onRotationChange(rotation);
			return;
		}
		if (EditMode == EditorMode.RotateHead)
		{
			SelectedKeyframe.GetSetting<Vector3>("Look").Value = new Vector3(rotation.X, rotation.Y, rotation.Z);
			if (SelectedActor is EntityActor)
			{
				EntityActor entityActor = (EntityActor)SelectedActor;
				entityActor.GetEntity().LookOrientation.Z = rotation.Z;
			}
		}
		else
		{
			if (EditMode != EditorMode.RotateBody)
			{
				return;
			}
			SelectedKeyframe.GetSetting<Vector3>("Rotation").Value = new Vector3(rotation.X, rotation.Y, rotation.Z);
			if (!(SelectedActor is EntityActor))
			{
			}
		}
		SelectedActor?.Track.UpdatePositions();
		ActiveScene?.Update(CurrentFrame);
	}

	private void OnPositionChange(Vector3 position)
	{
		if (EditMode == EditorMode.Translate)
		{
			if (_selectedNodeType == NodeType.Keyframe)
			{
				SelectedKeyframe.GetSetting<Vector3>("Position").Value = position;
				SelectedActor.Track.UpdatePositions();
			}
			else
			{
				KeyframeSetting<Vector3[]> setting = _selectedGrip.Keyframe.GetSetting<Vector3[]>("Curve");
				Vector3 value = _selectedGrip.Keyframe.GetSetting<Vector3>("Position").Value;
				Vector3[] value2 = setting.Value;
				value2[_selectedGrip.Index] = position - value;
				setting.Value = value2;
				_selectedGrip.Actor.Track.UpdatePositions();
			}
		}
		else if (EditMode == EditorMode.FreeMove)
		{
			if (_selectedNodeType == NodeType.Keyframe && SelectedKeyframe != null)
			{
				SelectedKeyframe.GetSetting<Vector3>("Position").Value = position;
				SelectedActor.Track.UpdatePositions();
			}
			else if (_selectedNodeType == NodeType.CurveHandle && _selectedGrip != null)
			{
				Vector3 value3 = _selectedGrip.Keyframe.GetSetting<Vector3>("Position").Value;
				_selectedGrip.Keyframe.GetSetting<Vector3[]>("Curve").Value[_selectedGrip.Index] = position - value3;
				_selectedGrip.Actor.Track.UpdatePositions();
			}
		}
		ActiveScene?.Update(CurrentFrame);
	}

	private bool CheckKeyframeHover()
	{
		HoveredKeyframe = null;
		_hoveredGrip = null;
		BodyRotateHover = false;
		if (SelectedKeyframe != null)
		{
			return false;
		}
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		float num = float.NaN;
		Dictionary<string, MachinimaScene> scenes = GetScenes();
		if (ActiveScene == null)
		{
			return false;
		}
		List<SceneActor> actors = ActiveScene.GetActors();
		for (int i = 0; i < actors.Count; i++)
		{
			SceneTrack track = actors[i].Track;
			for (int j = 0; j < track.Keyframes.Count; j++)
			{
				TrackKeyframe trackKeyframe = track.Keyframes[j];
				KeyframeSetting<Vector3> setting = trackKeyframe.GetSetting<Vector3>("Position");
				if (setting == null)
				{
					continue;
				}
				Vector3 value = setting.Value;
				BoundingBox keyframeBox = TrackKeyframe.KeyframeBox;
				keyframeBox.Translate(value);
				if (ShowPathNodes && HitDetection.CheckRayBoxCollision(keyframeBox, lookRay.Position, lookRay.Direction, out var collision))
				{
					float num2 = Vector3.Distance(lookRay.Position, collision.Position);
					if (float.IsNaN(num) || num2 < num)
					{
						HoveredKeyframe = trackKeyframe;
						HoveredActor = track.Parent;
						num = num2;
					}
				}
				if (track.Parent is EntityActor && !(track.Parent is ItemActor))
				{
					keyframeBox.Translate(new Vector3(0f, 0f - TrackKeyframe.KeyframeBox.GetSize().Y, 0f));
					if (HitDetection.CheckRayBoxCollision(keyframeBox, lookRay.Position, lookRay.Direction, out collision))
					{
						float num3 = Vector3.Distance(lookRay.Position, collision.Position);
						if (float.IsNaN(num) || num3 < num)
						{
							HoveredKeyframe = trackKeyframe;
							HoveredActor = track.Parent;
							num = num3;
							BodyRotateHover = true;
						}
					}
				}
				if (track.PathType != SceneTrack.TrackPathType.Bezier || track.Parent != ActiveActor)
				{
					continue;
				}
				KeyframeSetting<Vector3[]> setting2 = trackKeyframe.GetSetting<Vector3[]>("Curve");
				if (setting2 == null)
				{
					continue;
				}
				Vector3[] value2 = setting2.Value;
				for (int k = 0; k < value2.Length; k++)
				{
					keyframeBox = TrackKeyframe.PathBox;
					keyframeBox.Translate(value2[k] + value);
					if (HitDetection.CheckRayBoxCollision(keyframeBox, lookRay.Position, lookRay.Direction, out collision))
					{
						float num4 = Vector3.Distance(lookRay.Position, collision.Position);
						if (float.IsNaN(num) || num4 < num)
						{
							int previousKeyframe = ActiveActor.Track.GetPreviousKeyframe(trackKeyframe.Frame - 0.01f);
							int nextKeyframe = ActiveActor.Track.GetNextKeyframe(trackKeyframe.Frame + 0.01f);
							TrackKeyframe prevKeyframe = ((k == 0 && trackKeyframe.Frame > 0f && previousKeyframe != -1) ? ActiveActor.Track.Keyframes[previousKeyframe] : null);
							TrackKeyframe nextKeyframe2 = ((k == 1 && nextKeyframe != -1 && nextKeyframe < ActiveActor.Track.Keyframes.Count - 1) ? ActiveActor.Track.Keyframes[nextKeyframe] : null);
							_hoveredGrip = new CurveHandle(ActiveActor, trackKeyframe, k, prevKeyframe, nextKeyframe2);
						}
					}
				}
			}
			num = float.NaN;
		}
		return HoveredKeyframe != null;
	}

	private void SelectKeyframe()
	{
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		if (_hoveredGrip != null)
		{
			_selectedGrip = _hoveredGrip;
			_selectedNodeType = NodeType.CurveHandle;
			Vector3 value = _selectedGrip.Keyframe.GetSetting<Vector3>("Position").Value;
			Vector3 vector = _selectedGrip.Keyframe.GetSetting<Vector3[]>("Curve").Value[_selectedGrip.Index];
			Vector3 vector2 = value + vector;
			_targetDistance = Vector3.Distance(lookRay.Position, vector2);
			_lastKeyframePosition = vector2;
			if (_gameInstance.Input.IsShiftHeld())
			{
				_translationGizmo.Show(vector2, Vector3.Zero);
				EditMode = EditorMode.Translate;
			}
			else if (_gameInstance.Input.IsAltHeld())
			{
				_selectedGrip.UpdateTangent = true;
				EditMode = EditorMode.FreeMove;
			}
			else
			{
				EditMode = EditorMode.FreeMove;
			}
		}
		else
		{
			if (HoveredKeyframe == null)
			{
				return;
			}
			TrackKeyframe activeKeyframe = (SelectedKeyframe = HoveredKeyframe);
			ActiveKeyframe = activeKeyframe;
			SceneActor activeActor = (SelectedActor = HoveredActor);
			ActiveActor = activeActor;
			_selectedNodeType = NodeType.Keyframe;
			Vector3 keyframePos = SelectedKeyframe.GetSetting<Vector3>("Position").Value;
			_targetDistance = Vector3.Distance(lookRay.Position, keyframePos);
			_lastKeyframePosition = keyframePos;
			Vector3 value2 = SelectedKeyframe.GetSetting<Vector3>("Look").Value;
			Vector3 value3 = SelectedKeyframe.GetSetting<Vector3>("Rotation").Value;
			if (_gameInstance.Input.IsAltHeld() || _gameInstance.Input.IsShiftHeld())
			{
				if (_gameInstance.Input.IsAltHeld())
				{
					EditMode = EditorMode.RotateBody;
					if (SelectionMode == EditorSelectionMode.Scene)
					{
						Vector3 currentRotation2 = Vector3.Zero;
						_rotationGizmo.Show(keyframePos, currentRotation2, delegate(Vector3 newRotation)
						{
							Vector3 rotation2 = newRotation - currentRotation2;
							ActiveScene.Rotate(rotation2, keyframePos);
							currentRotation2 = newRotation;
							UpdateFrame(0L, forceUpdate: true);
							if (!_rotationGizmo.Visible)
							{
								EditMode = EditorMode.None;
							}
						});
					}
					else if (SelectionMode == EditorSelectionMode.Actor)
					{
						Vector3 currentRotation = Vector3.Zero;
						_rotationGizmo.Show(keyframePos, currentRotation, delegate(Vector3 newRotation)
						{
							Vector3 rotation = newRotation - currentRotation;
							ActiveActor.Track.RotatePath(rotation, keyframePos);
							currentRotation = newRotation;
							if (!_rotationGizmo.Visible)
							{
								EditMode = EditorMode.None;
							}
						});
					}
					else if (ActiveActor is ItemActor || BodyRotateHover)
					{
						EditMode = EditorMode.RotateBody;
						_rotationGizmo.Show(keyframePos, new Vector3(value3.X, value3.Y, value3.Z), OnRotationChange);
					}
					else
					{
						EditMode = EditorMode.RotateHead;
						_rotationGizmo.Show(keyframePos, new Vector3(value2.X, value2.Y, 0f), OnRotationChange, new Vector3(0f, value3.Y, 0f));
					}
				}
				else if (_gameInstance.Input.IsShiftHeld())
				{
					EditMode = EditorMode.RotateBody;
					if (SelectionMode == EditorSelectionMode.Scene)
					{
						Vector3 currentPosition2 = keyframePos;
						_translationGizmo.Show(currentPosition2, Vector3.Zero, delegate(Vector3 newPosition)
						{
							Vector3 vector3 = newPosition - currentPosition2;
							ActiveScene.OffsetOrigin(ActiveScene.Origin + vector3);
							currentPosition2 = newPosition;
							if (!_translationGizmo.Visible)
							{
								EditMode = EditorMode.None;
							}
						});
					}
					else if (SelectionMode == EditorSelectionMode.Actor)
					{
						Vector3 currentPosition = keyframePos;
						_translationGizmo.Show(currentPosition, Vector3.Zero, delegate(Vector3 newPosition)
						{
							Vector3 offset = newPosition - currentPosition;
							ActiveActor.Track.OffsetPositions(offset);
							currentPosition = newPosition;
							if (!_translationGizmo.Visible)
							{
								EditMode = EditorMode.None;
							}
						});
					}
					else
					{
						_translationGizmo.Show(keyframePos, new Vector3(value2.X, value2.Y, 0f), OnPositionChange);
					}
					EditMode = EditorMode.Translate;
				}
			}
			else
			{
				EditMode = EditorMode.FreeMove;
			}
			if (!Running && SelectedActor.Track.Keyframes.Count > 1)
			{
				CurrentFrame = SelectedKeyframe.Frame;
			}
		}
	}

	private void CycleSelectionMode()
	{
		if (SelectionMode == EditorSelectionMode.Keyframe)
		{
			SelectionMode = EditorSelectionMode.Actor;
		}
		else if (SelectionMode == EditorSelectionMode.Actor)
		{
			SelectionMode = EditorSelectionMode.Scene;
		}
		else
		{
			SelectionMode = EditorSelectionMode.Keyframe;
		}
		ResetEditing();
		_gameInstance.Chat.Log($"SelectionMode mode set to {SelectionMode}");
	}

	private void ResetEditing()
	{
		EditMode = EditorMode.None;
		_translationGizmo.Hide();
		_rotationGizmo.Hide();
		SelectedKeyframe = null;
		SelectedActor = null;
	}

	private bool HasActiveTool()
	{
		return _gameInstance.BuilderToolsModule?.ActiveTool?.ClientTool is MachinimaTool;
	}

	private void GiveMachinimaTool()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		Item val = new ClientItemStack(new Item("EditorTool_Machinima", 1, 1.0, 1.0, false, new sbyte[0])).ToItemPacket(includeMetadata: true);
		int hotbarActiveSlot = _gameInstance.InventoryModule.HotbarActiveSlot;
		_gameInstance.Connection.SendPacket((ProtoPacket)new SetCreativeItem(new InventoryPosition(-1, hotbarActiveSlot, val), false));
	}

	private EntityActor GetActorFromEntity(Entity entity)
	{
		if (entity == null || !entity.IsLocalEntity)
		{
			return null;
		}
		foreach (MachinimaScene value in _scenes.Values)
		{
			foreach (SceneActor actor in value.Actors)
			{
				if (actor is EntityActor && (actor as EntityActor).GetEntity() == entity)
				{
					return actor as EntityActor;
				}
			}
		}
		return null;
	}

	private void RegisterEvents()
	{
		WebView webView = _gameInstance.EditorWebViewModule.WebView;
		webView.RegisterForEvent<bool>("machinima.setRunning", this, OnSetRunning);
		webView.RegisterForEvent<bool>("machinima.setLoop", this, OnSetLoop);
		webView.RegisterForEvent<float>("machinima.setCurrentFrame", this, OnSetCurrentFrame);
		webView.RegisterForEvent<string>("machinima.selectScene", this, OnSelectScene);
		webView.RegisterForEvent<string>("machinima.deleteScene", this, OnDeleteScene);
		webView.RegisterForEvent("machinima.saveScene", this, OnSaveScene);
		webView.RegisterForEvent<string>("machinima.addScene", this, OnAddScene);
		webView.RegisterForEvent<KeyframeEvent>("machinima.moveKeyframe", this, OnMoveKeyframe);
		webView.RegisterForEvent<KeyframeEvent>("machinima.deleteKeyframe", this, OnDeleteKeyframe);
		webView.RegisterForEvent<KeyframeEvent>("machinima.addKeyframe", this, OnAddKeyframe);
		webView.RegisterForEvent<KeyframeSettingEvent>("machinima.setKeyframeSetting", this, OnSetKeyframeSetting);
		webView.RegisterForEvent<KeyframeSettingEvent>("machinima.removeKeyframeSetting", this, OnRemoveKeyframeSetting);
		webView.RegisterForEvent<KeyframeEvent>("machinima.setActorVisibility", this, OnSetActorVisibility);
		webView.RegisterForEvent<int>("machinima.deleteActor", this, OnDeleteActor);
		webView.RegisterForEvent<int>("machinima.addActor", this, OnAddActor);
		webView.RegisterForEvent<int>("machinima.duplicateActor", this, OnDuplicateActor);
		webView.RegisterForEvent<KeyframeEvent>("machinima.selectKeyframe", this, OnSelectKeyframe);
		webView.RegisterForEvent("machinima.deselectKeyframe", this, OnDeselectKeyframe);
		webView.RegisterForEvent<int, string>("machinima.setActorModel", this, OnSetActorModel);
		webView.RegisterForEvent<int, string>("machinima.setActorItem", this, OnSetActorItem);
		webView.RegisterForEvent<int>("machinima.setActorModelToLocalPlayer", this, OnSetActorModelToLocalPlayer);
		webView.RegisterForEvent<int, string>("machinima.updateActor", this, OnUpdateActor);
		webView.RegisterForEvent<KeyframeEventEvent>("machinima.addKeyframeEvent", this, OnAddKeyframeEvent);
		webView.RegisterForEvent<KeyframeEventEvent>("machinima.updateKeyframeEvent", this, OnUpdateKeyframeEvent);
		webView.RegisterForEvent<KeyframeEventEvent>("machinima.deleteKeyframeEvent", this, OnDeleteKeyframeEvent);
		webView.RegisterForEvent<int>("machinima.selectCamera", this, OnSelectCamera);
		webView.RegisterForEvent<bool>("machinima.setIsInterfaceOpen", this, OnSetIsInterfaceOpen);
		webView.RegisterForEvent<KeyframeEvent>("machinima.setKeyframeClipboard", this, OnSetKeyframeClipboard);
		webView.RegisterForEvent<KeyframeEvent>("machinima.pasteKeyframe", this, OnPasteKeyframe);
		webView.RegisterForEvent("machinima.openAssetEditor", this, OnOpenAssetEditor);
		webView.RegisterForEvent<string, string>("machinima.openAssetEditorWith", this, OnOpenAssetEditorWith);
		webView.RegisterForEvent<int>("settings.machinimaEditorSettings.setNewKeyframeFrameOffset", this, OnSetNewKeyframeFrameOffset);
	}

	private void UnregisterEvents()
	{
		WebView webView = _gameInstance.EditorWebViewModule.WebView;
		webView.UnregisterFromEvent("machinima.setRunning");
		webView.UnregisterFromEvent("machinima.setLoop");
		webView.UnregisterFromEvent("machinima.setCurrentFrame");
		webView.UnregisterFromEvent("machinima.selectScene");
		webView.UnregisterFromEvent("machinima.deleteScene");
		webView.UnregisterFromEvent("machinima.saveScene");
		webView.UnregisterFromEvent("machinima.addScene");
		webView.UnregisterFromEvent("machinima.moveKeyframe");
		webView.UnregisterFromEvent("machinima.deleteKeyframe");
		webView.UnregisterFromEvent("machinima.addKeyframe");
		webView.UnregisterFromEvent("machinima.setKeyframeSetting");
		webView.UnregisterFromEvent("machinima.removeKeyframeSetting");
		webView.UnregisterFromEvent("machinima.setActorVisibility");
		webView.UnregisterFromEvent("machinima.deleteActor");
		webView.UnregisterFromEvent("machinima.addActor");
		webView.UnregisterFromEvent("machinima.duplicateActor");
		webView.UnregisterFromEvent("machinima.selectKeyframe");
		webView.UnregisterFromEvent("machinima.deselectKeyframe");
		webView.UnregisterFromEvent("machinima.setActorModel");
		webView.UnregisterFromEvent("machinima.setActorItem");
		webView.UnregisterFromEvent("machinima.setActorModelToLocalPlayer");
		webView.UnregisterFromEvent("machinima.updateActor");
		webView.UnregisterFromEvent("machinima.addKeyframeEvent");
		webView.UnregisterFromEvent("machinima.updateKeyframeEvent");
		webView.UnregisterFromEvent("machinima.deleteKeyframeEvent");
		webView.UnregisterFromEvent("machinima.selectCamera");
		webView.UnregisterFromEvent("machinima.setIsInterfaceOpen");
		webView.UnregisterFromEvent("machinima.setKeyframeClipboard");
		webView.UnregisterFromEvent("machinima.pasteKeyframe");
		webView.UnregisterFromEvent("machinima.openAssetEditor");
		webView.UnregisterFromEvent("machinima.openAssetEditorWith");
		webView.UnregisterFromEvent("settings.machinimaEditorSettings.setNewKeyframeFrameOffset");
	}

	private void OnSetNewKeyframeFrameOffset(int offset)
	{
		HytaleClient.Data.UserSettings.Settings settings = _gameInstance.App.Settings.Clone();
		settings.MachinimaEditorSettings.NewKeyframeFrameOffset = offset;
		_gameInstance.App.ApplyNewSettings(settings);
	}

	private void OnOpenAssetEditor()
	{
		if (_gameInstance.App.Stage == App.AppStage.InGame)
		{
			_gameInstance.App.InGame.OpenAssetEditor();
		}
	}

	private void OnOpenAssetEditorWith(string assetType, string assetId)
	{
		if (_gameInstance.App.Stage == App.AppStage.InGame)
		{
			_gameInstance.App.InGame.OpenAssetIdInAssetEditor(assetType, assetId);
		}
	}

	public void ShowInterface()
	{
		if (_gameInstance.App.Stage != App.AppStage.InGame)
		{
			return;
		}
		if (!_hasInterfaceLoaded)
		{
			if (GetScenes().Count == 0)
			{
				LoadAllScenesFromFile();
				SetActiveScene();
			}
			_hasInterfaceLoaded = true;
		}
		UpdateInterfaceData();
		_gameInstance.App.InGame.SetCurrentOverlay(AppInGame.InGameOverlay.MachinimaEditor);
	}

	private void UpdateCurrentFrameInInterface()
	{
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.currentFrameChanged", (int)CurrentFrame);
	}

	private void UpdateInterfaceData()
	{
		Dictionary<string, MachinimaScene> dictionary = new Dictionary<string, MachinimaScene>();
		foreach (string key in _scenes.Keys)
		{
			dictionary.Add(key, (key == ActiveScene.Name) ? ActiveScene : new MachinimaScene(_gameInstance, key));
		}
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.scenesInitialized", dictionary, Running, AutoRestartScene);
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.keybindsInitialized", _keybinds.Select((KeyValuePair<Keybind, SDL_Scancode> k) => k.Key.ToString()).ToList(), _keybinds.Select((KeyValuePair<Keybind, SDL_Scancode> k) => SDL.SDL_GetKeyName(SDL.SDL_GetKeyFromScancode(k.Value))).ToList());
	}

	private void OnSetIsInterfaceOpen(bool isOpen)
	{
		_isInterfaceOpen = isOpen;
	}

	private void OnSetKeyframeClipboard(KeyframeEvent e)
	{
		_keyframeClipboard = ActiveScene.GetActor(e.Actor).Track.GetKeyframe(e.Keyframe).Clone();
	}

	private void OnPasteKeyframe(KeyframeEvent e)
	{
		int num = e.Frame;
		foreach (TrackKeyframe keyframe in ActiveScene.GetActor(e.Actor).Track.Keyframes)
		{
			if (!(keyframe.Frame < (float)num))
			{
				if ((int)keyframe.Frame != num)
				{
					break;
				}
				num++;
			}
		}
		TrackKeyframe trackKeyframe = _keyframeClipboard.Clone();
		trackKeyframe.Frame = num;
		ActiveScene.GetActor(e.Actor).Track.AddKeyframe(trackKeyframe);
		CurrentFrame = num;
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.keyframeAdded", e.Actor, trackKeyframe);
	}

	private void OnSaveScene()
	{
		SaveSceneFile(ActiveScene, _settings.CompressSaveFiles ? SceneDataType.CompressedFile : SceneDataType.JSONFile);
	}

	private void OnSetActorModel(int id, string modelId)
	{
		(_activeScene.GetActor(id) as EntityActor).UpdateModel(_gameInstance, modelId);
	}

	private void OnSetActorItem(int id, string itemId)
	{
		(_activeScene.GetActor(id) as ItemActor).SetItemId(itemId, _gameInstance);
	}

	private void OnSetActorModelToLocalPlayer(int id)
	{
		((EntityActor)ActiveScene.GetActor(id)).SetBaseModel(_gameInstance.LocalPlayer.ModelPacket);
	}

	private void OnDuplicateActor(int actorId)
	{
		SceneActor sceneActor = ActiveScene.GetActor(actorId).Clone(_gameInstance);
		sceneActor.Name = ActiveScene.GetNextActorName("Copy");
		ActiveScene.AddActor(sceneActor, addStartKeyframe: false);
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.actorAdded", new Dictionary<string, SceneActor> { { sceneActor.Name, sceneActor } });
	}

	private void OnUpdateActor(int id, string name)
	{
		ActiveScene.GetActor(id).Name = name;
	}

	private void OnAddScene(string name)
	{
		MachinimaScene scene = new MachinimaScene(_gameInstance, name);
		AddScene(scene, makeActive: true);
	}

	private void OnDeleteScene(string name)
	{
		RemoveScene(name);
	}

	private void OnSelectScene(string name)
	{
		SetActiveScene(name);
	}

	private void OnMoveKeyframe(KeyframeEvent e)
	{
		SceneActor actor = ActiveScene.GetActor(e.Actor);
		foreach (TrackKeyframe keyframe in actor.Track.Keyframes)
		{
			if (keyframe.Frame == (float)e.Frame)
			{
				return;
			}
		}
		actor.Track.GetKeyframe(e.Keyframe).Frame = e.Frame;
		actor.Track.UpdateKeyframeData();
	}

	private void OnDeleteKeyframe(KeyframeEvent e)
	{
		if (ActiveScene.GetActor(e.Actor).Track.Keyframes.Count > 1)
		{
			ActiveScene.GetActor(e.Actor).Track.RemoveKeyframe(ActiveScene.GetActor(e.Actor).Track.GetKeyframe(e.Keyframe).Frame);
		}
	}

	private void OnSelectKeyframe(KeyframeEvent e)
	{
		ActiveActor = ActiveScene.GetActor(e.Actor);
		ActiveKeyframe = ActiveActor.Track.GetKeyframe(e.Keyframe);
		CurrentFrame = ActiveKeyframe.Frame;
		UpdateFrame(0L, forceUpdate: true);
	}

	private void OnDeselectKeyframe()
	{
		ActiveActor = null;
		ActiveKeyframe = null;
	}

	private void OnAddKeyframe(KeyframeEvent e)
	{
		SceneTrack track = ActiveScene.GetActor(e.Actor).Track;
		int frame = e.Frame;
		foreach (TrackKeyframe keyframe in track.Keyframes)
		{
			if (keyframe.Frame == (float)frame)
			{
				return;
			}
		}
		TrackKeyframe trackKeyframe = ((track.Keyframes.Count > 0 && (float)frame <= track.Keyframes[0].Frame) ? track.GetCurrentFrame(track.Keyframes[0].Frame) : ((!((float)frame >= track.GetTrackLength())) ? track.GetCurrentFrame(frame) : track.GetCurrentFrame(track.GetTrackLength())));
		trackKeyframe.Frame = frame;
		track.AddKeyframe(trackKeyframe);
		CurrentFrame = e.Frame;
		UpdateFrame(0L, forceUpdate: true);
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.keyframeAdded", e.Actor, trackKeyframe);
	}

	private void OnSetKeyframeSetting(KeyframeSettingEvent e)
	{
		JObject jsonData = JObject.Parse(e.SettingValue);
		IKeyframeSetting setting = KeyframeSetting<object>.ConvertJsonObject(e.SettingName, jsonData);
		ActiveScene.GetActor(e.Actor).Track.GetKeyframe(e.Keyframe).AddSetting(setting);
		ActiveScene.GetActor(e.Actor).Track.UpdatePositions();
	}

	private void OnRemoveKeyframeSetting(KeyframeSettingEvent e)
	{
		ActiveScene.GetActor(e.Actor).Track.GetKeyframe(e.Keyframe).RemoveSetting(e.SettingName);
		ActiveScene.GetActor(e.Actor).Track.UpdatePositions();
	}

	private void OnSetActorVisibility(KeyframeEvent e)
	{
		ActiveScene.GetActor(e.Actor).Visible = e.Visible;
	}

	private void OnDeleteActor(int actor)
	{
		ActiveScene.RemoveActor(ActiveScene.GetActor(actor).Name);
	}

	private void OnAddActor(int objectTypeId)
	{
		string availableObjectName = GetAvailableObjectName((ActorType)objectTypeId);
		SceneActor sceneActor;
		switch ((ActorType)objectTypeId)
		{
		default:
			return;
		case ActorType.Camera:
			sceneActor = new CameraActor(_gameInstance, availableObjectName);
			break;
		case ActorType.Entity:
			sceneActor = new EntityActor(_gameInstance, availableObjectName, null);
			((EntityActor)sceneActor).SetBaseModel(_gameInstance.LocalPlayer.ModelPacket);
			break;
		case ActorType.Player:
			sceneActor = new PlayerActor(_gameInstance, availableObjectName);
			break;
		case ActorType.Reference:
			sceneActor = new ReferenceActor(_gameInstance, availableObjectName);
			break;
		case ActorType.Item:
			sceneActor = new ItemActor(_gameInstance, availableObjectName, null);
			break;
		}
		ActiveScene.AddActor(sceneActor);
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.actorAdded", new Dictionary<string, SceneActor> { { sceneActor.Name, sceneActor } });
	}

	private void OnSetRunning(bool running)
	{
		Running = running;
		if (Running)
		{
			_lastFrameTick = GetCurrentTime();
		}
	}

	private void OnSetCurrentFrame(float frame)
	{
		CurrentFrame = frame;
		ActiveScene.Update(CurrentFrame);
	}

	private void OnSetLoop(bool loop)
	{
		AutoRestartScene = loop;
	}

	private string GetAvailableObjectName(ActorType objectType)
	{
		string text = "Object";
		switch (objectType)
		{
		case ActorType.Camera:
			text = "Camera";
			break;
		case ActorType.Entity:
			text = "Entity";
			break;
		case ActorType.Player:
			text = "Player";
			break;
		case ActorType.Reference:
			text = "Reference";
			break;
		case ActorType.Item:
			text = "Item";
			break;
		}
		Regex regex = new Regex("^" + text + " ([0-9]+)+$", RegexOptions.IgnoreCase);
		int num = 0;
		foreach (SceneActor actor in ActiveScene.Actors)
		{
			if (actor.Type != objectType)
			{
				continue;
			}
			Match match = regex.Match(actor.Name);
			if (match.Success)
			{
				int num2 = int.Parse(match.Groups[1].Value);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		return text + " " + (num + 1);
	}

	private void OnAddKeyframeEvent(KeyframeEventEvent evt)
	{
		UpdateKeyframeEvent(evt, insert: true);
	}

	private void OnUpdateKeyframeEvent(KeyframeEventEvent evt)
	{
		UpdateKeyframeEvent(evt, insert: false);
	}

	private void UpdateKeyframeEvent(KeyframeEventEvent evt, bool insert)
	{
		JObject jsonData = JObject.Parse(evt.Options);
		HytaleClient.InGame.Modules.Machinima.Events.KeyframeEvent keyframeEvent = HytaleClient.InGame.Modules.Machinima.Events.KeyframeEvent.ConvertJsonObject(jsonData);
		if (keyframeEvent == null)
		{
			throw new Exception("unknown keyframe event type");
		}
		keyframeEvent.Initialize(ActiveScene);
		TrackKeyframe keyframe = ActiveScene.GetActor(evt.Actor).Track.GetKeyframe(evt.Keyframe);
		if (insert)
		{
			keyframe.AddEvent(keyframeEvent);
		}
		else
		{
			keyframe.Events[keyframe.Events.IndexOf(keyframe.GetEvent(evt.Event))] = keyframeEvent;
		}
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.keyframeEventAdded", evt.Actor, evt.Keyframe, evt.Event, keyframeEvent.ToCoherentJson());
	}

	private void OnDeleteKeyframeEvent(KeyframeEventEvent evt)
	{
		TrackKeyframe keyframe = ActiveScene.GetActor(evt.Actor).Track.GetKeyframe(evt.Keyframe);
		keyframe.Events.Remove(keyframe.GetEvent(evt.Event));
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.keyframeEventDeleted", evt.Actor, evt.Keyframe, evt.Event);
	}

	private void OnSelectCamera(int actorId)
	{
		CameraActor cameraActor = (CameraActor)ActiveScene.GetActor(actorId);
		cameraActor.SetState(!cameraActor.Active);
	}

	private bool AddScene(MachinimaScene scene, bool makeActive = false)
	{
		if (_scenes.ContainsKey(scene.Name))
		{
			return false;
		}
		_scenes.Add(scene.Name, scene);
		if (makeActive)
		{
			SetActiveScene(scene.Name);
		}
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.sceneAdded", scene);
		return true;
	}

	private bool RemoveScene(string sceneName)
	{
		if (!_scenes.ContainsKey(sceneName))
		{
			return false;
		}
		if (ActiveScene != null && ActiveScene.Name == sceneName)
		{
			ResetScene();
			ActiveScene = null;
		}
		_scenes[sceneName].Dispose();
		_scenes.Remove(sceneName);
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.sceneDeleted", sceneName);
		return true;
	}

	private void ClearScenes()
	{
		ResetScene();
		ActiveScene = null;
		foreach (MachinimaScene value in _scenes.Values)
		{
			value.Dispose();
		}
		_scenes.Clear();
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.scenesCleared");
	}

	private void ListScenes()
	{
		string text = ActiveScene?.Name ?? "";
		if (_scenes.Count == 0)
		{
			_gameInstance.Chat.Log("No scenes currently exist");
			return;
		}
		_gameInstance.Chat.Log($"{_scenes.Count} Loaded Scenes:");
		foreach (KeyValuePair<string, MachinimaScene> scene in _scenes)
		{
			float num = 0f;
			foreach (SceneActor actor in scene.Value.Actors)
			{
				if (actor.Track.GetTrackLength() > num)
				{
					num = actor.Track.GetTrackLength();
				}
			}
			string text2 = $"{scene.Value.Actors.Count} Actors ({System.Math.Round(num / PlaybackFPS * 10f) / 10.0} sec)";
			string text3 = ((scene.Key == text) ? " - Active" : "");
			_gameInstance.Chat.Log("'" + scene.Key + "' - " + text2 + text3);
		}
	}

	private void OnSceneEnd()
	{
		if (AutoRestartScene)
		{
			ResetScene(doUpdate: true);
		}
		if (_continousPlayback)
		{
			Running = true;
		}
	}

	public MachinimaScene GetScene(string sceneName)
	{
		if (_scenes.ContainsKey(sceneName))
		{
			return _scenes[sceneName];
		}
		return null;
	}

	public Dictionary<string, MachinimaScene> GetScenes()
	{
		return _scenes;
	}

	private void ResetScene(bool doUpdate = false)
	{
		Running = false;
		CurrentFrame = 0f;
		if (doUpdate)
		{
			UpdateFrame(0L, forceUpdate: true);
		}
	}

	private void EndScene()
	{
		CurrentFrame = ((ActiveScene == null) ? 0f : ActiveScene.GetSceneLength());
		UpdateFrame(0L, forceUpdate: true);
		Running = false;
	}

	private void SetActiveScene(string sceneName = null)
	{
		if (sceneName == null && _scenes.Count > 0)
		{
			Dictionary<string, MachinimaScene>.Enumerator enumerator = _scenes.GetEnumerator();
			enumerator.MoveNext();
			ActiveScene = enumerator.Current.Value;
		}
		else if (sceneName != null && _scenes.ContainsKey(sceneName))
		{
			ActiveScene = _scenes[sceneName];
		}
		string data = ((ActiveScene == null) ? null : ActiveScene.Name);
		if (_isInterfaceOpen)
		{
			UpdateInterfaceData();
		}
		_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.activeSceneChanged", data);
	}

	private string GetNextSceneName(string sceneName)
	{
		string text = sceneName;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "scene";
		}
		if (_scenes.ContainsKey(text))
		{
			string text2 = text;
			for (int i = 1; i < 999999; i++)
			{
				text2 = $"{text}{i}";
				if (!_scenes.ContainsKey(text2))
				{
					return text2;
				}
			}
		}
		return text;
	}

	private void Autosave(bool force = false)
	{
		if (!Running && ActiveScene != null && (_nextAutosaveTick <= _lastFrameTick || force))
		{
			int num = _settings.AutosaveDelay * 1000;
			if (_nextAutosaveTick == 0)
			{
				_nextAutosaveTick = _lastFrameTick + num;
				return;
			}
			SaveSceneFile(ActiveScene, _settings.CompressSaveFiles ? SceneDataType.CompressedFile : SceneDataType.JSONFile, null, AutosaveDirectory);
			_nextAutosaveTick = _lastFrameTick + num;
			_gameInstance.Notifications.AddNotification("Scene '" + ActiveScene.Name + "' autosaved to file.", null);
		}
	}

	private void SaveAllScenesToFile()
	{
		foreach (MachinimaScene value in _scenes.Values)
		{
			SaveSceneFile(value, _settings.CompressSaveFiles ? SceneDataType.CompressedFile : SceneDataType.JSONFile);
		}
	}

	private void LoadAllScenesFromFile()
	{
		if (!Directory.Exists(SceneDirectory))
		{
			return;
		}
		string[] files = Directory.GetFiles(SceneDirectory);
		foreach (string text in files)
		{
			try
			{
				if (text.EndsWith(".json") || text.EndsWith(".hms"))
				{
					LoadSceneFile(text, updateInterface: false);
				}
			}
			catch (Exception value)
			{
				Trace.WriteLine("Failed to load machinima scene: " + text);
				Trace.WriteLine(value);
			}
		}
	}

	private void SaveSceneFile(MachinimaScene scene, SceneDataType dataType, string filename = null, string path = "")
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			filename = scene.Name;
		}
		if (string.IsNullOrWhiteSpace(path))
		{
			path = SceneDirectory;
		}
		switch (dataType)
		{
		case SceneDataType.CompressedFile:
		{
			string text3 = Path.Combine(path, filename + ".hms");
			byte[] bytes = scene.ToCompressedByteArray(_serializerSettings);
			try
			{
				File.WriteAllBytes(text3, bytes);
				break;
			}
			catch (Exception ex2)
			{
				_gameInstance.Chat.Log("Error saving data to file " + text3 + "! - " + ex2.Message);
				Trace.WriteLine(ex2);
				break;
			}
		}
		case SceneDataType.JSONFile:
		{
			string text2 = Path.Combine(SceneDirectory, filename + ".json");
			string contents = scene.Serialize(_serializerSettings);
			try
			{
				File.WriteAllText(text2, contents);
				break;
			}
			catch (Exception ex)
			{
				_gameInstance.Chat.Log("Error saving data to file " + text2 + "! - " + ex.Message);
				Trace.WriteLine(ex);
				break;
			}
		}
		case SceneDataType.Clipboard:
		{
			string text = scene.Serialize(_serializerSettings);
			SDL.SDL_SetClipboardText(text);
			break;
		}
		}
	}

	private MachinimaScene LoadSceneFile(string filename, bool updateInterface = true, SceneDataType dataType = SceneDataType.CompressedFile)
	{
		if (filename.EndsWith(".json"))
		{
			dataType = SceneDataType.JSONFile;
		}
		else if (filename.EndsWith(".hms"))
		{
			dataType = SceneDataType.CompressedFile;
		}
		else
		{
			filename += (_settings.CompressSaveFiles ? ".hms" : ".json");
		}
		if (dataType != SceneDataType.Clipboard && !File.Exists(filename))
		{
			_gameInstance.Chat.Log("Unable to find file '" + filename + "'");
			return null;
		}
		MachinimaScene machinimaScene = null;
		switch (dataType)
		{
		case SceneDataType.Clipboard:
		{
			string jsonString = SDL.SDL_GetClipboardText();
			machinimaScene = MachinimaScene.Deserialize(jsonString, _gameInstance, _serializerSettings);
			break;
		}
		case SceneDataType.JSONFile:
			machinimaScene = MachinimaScene.Deserialize(File.ReadAllText(filename), _gameInstance, _serializerSettings);
			break;
		case SceneDataType.CompressedFile:
		{
			byte[] compressedByteArray;
			try
			{
				compressedByteArray = File.ReadAllBytes(filename);
			}
			catch (Exception ex)
			{
				_gameInstance.Chat.Log("Error reading data from file " + filename + "! - " + ex.Message);
				Trace.WriteLine(ex);
				return null;
			}
			machinimaScene = MachinimaScene.FromCompressedByteArray(compressedByteArray, _gameInstance, _serializerSettings);
			if (machinimaScene == null)
			{
				return null;
			}
			break;
		}
		}
		if (machinimaScene != null)
		{
			if (_scenes.ContainsKey(machinimaScene.Name))
			{
				RemoveScene(machinimaScene.Name);
			}
			AddScene(machinimaScene);
			if (updateInterface)
			{
				_gameInstance.EditorWebViewModule.WebView.TriggerEvent("machinima.scenesInitialized", _scenes);
			}
		}
		return machinimaScene;
	}

	public void HandleSceneUpdatePacket(UpdateMachinimaScene packet)
	{
		byte[] compressedByteArray = Array.ConvertAll(packet.Scene, (sbyte b) => (byte)b);
		MachinimaScene machinimaScene2 = (ActiveScene = MachinimaScene.FromCompressedByteArray(compressedByteArray, _gameInstance, _serializerSettings));
		_gameInstance.Chat.Log("Recieved '" + machinimaScene2.Name + "' scene update from " + packet.Player);
	}
}
