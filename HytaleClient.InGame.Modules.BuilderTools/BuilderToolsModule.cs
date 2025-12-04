using System;
using System.Collections.Generic;
using System.Globalization;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Data.Items;
using HytaleClient.Data.UserSettings;
using HytaleClient.InGame.Commands;
using HytaleClient.InGame.Modules.BuilderTools.Tools;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Client;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.InGame.Modules.Shortcuts;
using HytaleClient.Interface;
using HytaleClient.Math;
using HytaleClient.Protocol;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.BuilderTools;

internal class BuilderToolsModule : Module
{
	private class PickBlockActionData
	{
		public enum ActionType
		{
			SetMaterial,
			SetMask
		}

		public readonly int X;

		public readonly int Y;

		public readonly int Z;

		public readonly int BlockId;

		public readonly ActionType Action;

		public PickBlockActionData(int x, int y, int z, int blockId, ActionType action)
		{
			X = x;
			Y = y;
			Z = z;
			BlockId = blockId;
			Action = action;
		}

		public bool Equals(PickBlockActionData other)
		{
			return other != null && X == other.X && Y == other.Y && Z == other.Z && BlockId == other.BlockId && Action == other.Action;
		}
	}

	public readonly Dictionary<string, ClientTool> ClientTools;

	private ToolInstance _activeTool;

	public bool DrawHighlightAndUndergroundColor = true;

	private ToolInstance _configuringTool;

	private int _configuringToolSection;

	private int _configuringToolSlot;

	private PickBlockActionData _lastPickBlockAction;

	public long TimeOfLastToolInteraction = 0L;

	private int _toolSurfaceOffset = 0;

	public IntVector3 ToolVectorOffset = IntVector3.Zero;

	public SelectionArea SelectionArea;

	public static readonly Logger BuilderToolsLogger = LogManager.GetCurrentClassLogger();

	public BuilderToolsSettings builderToolsSettings => _gameInstance.App.Settings.BuilderToolsSettings;

	public SelectionTool SelectionTool { get; private set; }

	public PlaySelectionTool PlaySelection { get; private set; }

	public BrushTool Brush { get; private set; }

	public PasteTool Paste { get; private set; }

	public AnchorTool Anchor { get; private set; }

	public ToolInstance ActiveTool
	{
		get
		{
			return _activeTool;
		}
		private set
		{
			if (value?.ClientTool != null && _activeTool?.ClientTool == value.ClientTool)
			{
				value.ClientTool.OnToolItemChange(value.ItemStack);
			}
			else
			{
				_activeTool?.ClientTool?.SetInactive();
				_activeTool = value;
				_activeTool?.ClientTool?.SetActive(_activeTool?.ItemStack);
			}
			UpdateUIForActiveTool();
		}
	}

	public ToolInstance ConfiguringTool
	{
		get
		{
			return _configuringTool;
		}
		set
		{
			_configuringTool = value;
			UpdateUIForConfiguringTool();
		}
	}

	public bool HasActiveTool => ActiveTool != null;

	public bool HasActiveBrush => HasActiveTool && ActiveTool.BuilderTool.IsBrushTool;

	public Vector3 BrushTargetPosition { get; private set; } = Vector3.NaN;


	public int ToolsInteractionDistance
	{
		get
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			if (builderToolsSettings.useToolReachDistance)
			{
				return builderToolsSettings.ToolReachDistance;
			}
			if (_gameInstance.App.Settings.PlaceBlocksAtRange(_gameInstance.GameMode) && _gameInstance.Input.IsAltHeld())
			{
				return _gameInstance.App.Settings.CurrentCreativeInteractionDistance;
			}
			return _gameInstance.App.Settings.creativeInteractionDistance;
		}
	}

	[Usage("tool", new string[]
	{
		"arg [id] [value]", "brush [id] [value]", "args", "reach [distance] [lock]", "delay [msTime]", "offset [value]", "axislock [x] [y] [z]", "color [red] [green] [blue]", "listblocks", "list",
		"macros"
	})]
	[Description("Builder Tools command")]
	public void ToolsCommand(string[] args)
	{
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0])
		{
		case "arg":
		case "brush":
		{
			if (args.Length != 2 && args.Length != 3)
			{
				throw new InvalidCommandUsage();
			}
			BuilderToolArgGroup argGroup = (BuilderToolArgGroup)(!(args[0] == "arg"));
			string argValue = ((args.Length == 3) ? args[2] : "");
			SendConfiguringToolArgUpdate(argGroup, args[1], argValue, delegate(FailureReply err, SuccessReply reply)
			{
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Invalid comparison between Unknown and I4
				if (reply != null)
				{
					_gameInstance.Chat.Log((((int)argGroup == 1) ? "Brush" : "Tool") + " " + args[1] + " changed to " + argValue);
				}
				else
				{
					_gameInstance.Chat.AddBsonMessage(err.Message);
				}
			});
			break;
		}
		case "args":
		{
			if (!HasActiveTool)
			{
				_gameInstance.Chat.Log("You need an active builder tool in hand to use this.");
				break;
			}
			string toolArgsLogText = ActiveTool.BuilderTool.GetToolArgsLogText(ActiveTool);
			_gameInstance.Chat.Log(toolArgsLogText);
			break;
		}
		case "reach":
			if (args.Length == 3)
			{
				if (args[2].ToLower() != "lock")
				{
					throw new InvalidCommandUsage();
				}
				builderToolsSettings.ToolReachLock = true;
			}
			else
			{
				if (args.Length != 2)
				{
					throw new InvalidCommandUsage();
				}
				builderToolsSettings.ToolReachLock = false;
			}
			builderToolsSettings.ToolReachDistance = int.Parse(args[1], CultureInfo.InvariantCulture);
			if (builderToolsSettings.ToolReachLock)
			{
				_gameInstance.Chat.Log($"Tool reach distance locked to {builderToolsSettings.ToolReachDistance}.");
			}
			else
			{
				_gameInstance.Chat.Log($"Tool reach distance set to {builderToolsSettings.ToolReachDistance}.");
			}
			break;
		case "delay":
			if (args.Length != 2)
			{
				throw new InvalidCommandUsage();
			}
			builderToolsSettings.ToolDelayMin = int.Parse(args[1], CultureInfo.InvariantCulture);
			_gameInstance.Chat.Log($"Tool delay set to {builderToolsSettings.ToolDelayMin}.");
			_gameInstance.App.Settings.Save();
			break;
		case "surfaceoffset":
			if (args.Length != 2)
			{
				throw new InvalidCommandUsage();
			}
			_toolSurfaceOffset = int.Parse(args[1], CultureInfo.InvariantCulture);
			_gameInstance.Chat.Log($"Surface offset set to {_toolSurfaceOffset}.");
			break;
		case "offset":
			if (args.Length == 4)
			{
				ToolVectorOffset = new IntVector3(int.Parse(args[1], CultureInfo.InvariantCulture), int.Parse(args[2], CultureInfo.InvariantCulture), int.Parse(args[3], CultureInfo.InvariantCulture));
				_gameInstance.Chat.Log($"Offset set to {ToolVectorOffset}.");
				break;
			}
			if (args.Length == 2)
			{
				ToolVectorOffset = new IntVector3(0, int.Parse(args[1], CultureInfo.InvariantCulture), 0);
				_gameInstance.Chat.Log($"Offset set to {ToolVectorOffset}.");
				break;
			}
			throw new InvalidCommandUsage();
		case "axislock":
			if (args.Length == 4)
			{
				if (!float.TryParse(args[1], out var result))
				{
					throw new InvalidCommandUsage();
				}
				if (!float.TryParse(args[2], out var result2))
				{
					throw new InvalidCommandUsage();
				}
				if (!float.TryParse(args[3], out var result3))
				{
					throw new InvalidCommandUsage();
				}
				Brush.initialBlockPosition = new Vector3(result, result2, result3);
			}
			else if (args.Length == 2 && _gameInstance.InteractionModule.HasFoundTargetBlock)
			{
				Vector3 blockPosition = _gameInstance.InteractionModule.TargetBlockHit.BlockPosition;
				if (!float.TryParse(args[1], out var result4))
				{
					throw new InvalidCommandUsage();
				}
				blockPosition.Y += result4;
				Brush.initialBlockPosition = blockPosition;
			}
			else
			{
				if (args.Length != 1 || !_gameInstance.InteractionModule.HasFoundTargetBlock)
				{
					throw new InvalidCommandUsage();
				}
				Brush.initialBlockPosition = _gameInstance.InteractionModule.TargetBlockHit.BlockPosition;
			}
			Brush.lockModeActive = true;
			Brush.lockMode = BrushTool.LockMode.Always;
			Brush.unlockedAxis = BrushTool.AxisAndPlanes.XZ;
			_gameInstance.Chat.Log($"Brush axis lock set to mode {Brush.lockMode}, unlocked axis {Brush.unlockedAxis} and initial position {Brush.initialBlockPosition}.");
			break;
		case "color":
		{
			if (args.Length != 4)
			{
				throw new InvalidCommandUsage();
			}
			int num = int.Parse(args[1], CultureInfo.InvariantCulture);
			int num2 = int.Parse(args[2], CultureInfo.InvariantCulture);
			int num3 = int.Parse(args[3], CultureInfo.InvariantCulture);
			SelectionTool.Color = new Vector3(num, num2, num3);
			_gameInstance.Chat.Log($"Selection tool display color set to {SelectionTool.Color}.");
			break;
		}
		case "listblocks":
			if (args.Length >= 2)
			{
				if (args[1] == "-c")
				{
					SelectionArea.ListBlocks(clipobardOutput: true);
				}
				else
				{
					SelectionArea.ListBlocks(clipobardOutput: false, args[1]);
				}
			}
			else
			{
				SelectionArea.ListBlocks();
			}
			break;
		case "list":
		{
			ClientItemBase[] builderToolItems = BuilderTool.GetBuilderToolItems(_gameInstance);
			List<string> list = new List<string>();
			for (int i = 0; i < builderToolItems.Length; i++)
			{
				list.Add(builderToolItems[i].BuilderTool.ToString());
			}
			list.Sort();
			string text = string.Join<string>(", ", (IEnumerable<string>)list);
			_gameInstance.Chat.Log("Loaded builder tools: [" + text + "]");
			break;
		}
		case "macros":
		{
			ShortcutsModule shortcutsModule = _gameInstance.ShortcutsModule;
			shortcutsModule.AddMacro("w", ".tool brush width %1");
			shortcutsModule.AddMacro("h", ".tool brush height %1");
			shortcutsModule.AddMacro("s", ".tool brush shape %1");
			shortcutsModule.AddMacro("o", ".tool brush origin %1");
			shortcutsModule.AddMacro("b", ".tool brush material %1");
			shortcutsModule.AddMacro("m", ".tool brush mask %1");
			shortcutsModule.AddMacro("a", ".tool arg %1 %2");
			shortcutsModule.AddMacro("aa", ".tool args");
			_gameInstance.Chat.Log("Tool shortcut macros added!");
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
	}

	public bool HasConfigurationToolBrushDataOrArguments()
	{
		int num = ConfiguringTool?.BuilderTool.GetItemToolArgs(ConfiguringTool.ItemStack).Count ?? 0;
		return (ConfiguringTool != null && ConfiguringTool.BrushData != null) || num != 0;
	}

	public BuilderToolsModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		ClientTools = new Dictionary<string, ClientTool>();
		UpdateUIToolSettings();
		RegisterEvents();
		_gameInstance.RegisterCommand("tool", ToolsCommand);
		SelectionArea = new SelectionArea(gameInstance);
	}

	public override void Initialize()
	{
		Brush = new BrushTool(_gameInstance);
		Action<ClientTool> action = delegate(ClientTool tool)
		{
			ClientTools.Add(tool.ToolId, tool);
		};
		SelectionTool obj = (SelectionTool = new SelectionTool(_gameInstance));
		action(obj);
		PlaySelectionTool obj2 = (PlaySelection = new PlaySelectionTool(_gameInstance));
		action(obj2);
		PasteTool obj3 = (Paste = new PasteTool(_gameInstance));
		action(obj3);
		action(new EntityTool(_gameInstance));
		action(new ExtrudeTool(_gameInstance));
		action(new LineTool(_gameInstance));
		action(new HitboxTool(_gameInstance));
		action(new MachinimaTool(_gameInstance));
		AnchorTool obj4 = (Anchor = new AnchorTool(_gameInstance));
		action(obj4);
	}

	protected override void DoDispose()
	{
		Brush?.Dispose();
		UnregisterEvents();
		foreach (ClientTool value in ClientTools.Values)
		{
			value.Dispose();
		}
		SelectionArea.DoDispose();
	}

	public bool ShouldSendMouseWheelEventToPlaySelectionTool()
	{
		return HasActiveTool && ActiveTool?.ClientTool != null && typeof(PlaySelectionTool).IsInstanceOfType(ActiveTool.ClientTool) && ((PlaySelectionTool)ActiveTool.ClientTool).IsInTransformationMode();
	}

	public void SendMouseWheelEventToPlaySelectionTool(int directionOfScroll)
	{
		if (ShouldSendMouseWheelEventToPlaySelectionTool())
		{
			((PlaySelectionTool)ActiveTool.ClientTool).OnScrollWheelEvent(directionOfScroll);
		}
	}

	public void Update(float deltaTime)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		if ((int)_gameInstance.GameMode != 1 || !HasActiveTool)
		{
			return;
		}
		Brush?.Update(deltaTime);
		if (builderToolsSettings.ToolReachLock)
		{
			Ray lookRay = _gameInstance.CameraModule.GetLookRay();
			Vector3 brushTargetPosition = lookRay.Position + lookRay.Direction * ToolsInteractionDistance;
			brushTargetPosition.X = (int)System.Math.Floor(brushTargetPosition.X);
			brushTargetPosition.Y = (int)System.Math.Floor(brushTargetPosition.Y);
			brushTargetPosition.Z = (int)System.Math.Floor(brushTargetPosition.Z);
			BrushTargetPosition = brushTargetPosition;
		}
		else if (Brush != null && ((Brush.lockMode != 0 && Brush.lockModeActive) || Brush._brushAxisLockPlane.IsEnabled()))
		{
			int distance;
			Vector3 lockedBrushPosition = Brush.GetLockedBrushPosition(out distance);
			if (distance <= ToolsInteractionDistance)
			{
				BrushTargetPosition = lockedBrushPosition;
				if (ToolVectorOffset != IntVector3.Zero)
				{
					BrushTargetPosition += (Vector3)ToolVectorOffset;
				}
				if (_toolSurfaceOffset != 0)
				{
					Vector3 vector = _gameInstance.InteractionModule.TargetBlockHit.Normal * _toolSurfaceOffset;
					BrushTargetPosition += vector;
				}
			}
			else
			{
				BrushTargetPosition = Vector3.NaN;
			}
		}
		else if (_gameInstance.InteractionModule.HasFoundTargetBlock && (_gameInstance.InteractionModule.PlacingAtRange || _gameInstance.InteractionModule.TargetBlockHit.Distance <= (float)ToolsInteractionDistance))
		{
			BrushTargetPosition = _gameInstance.InteractionModule.TargetBlockHit.BlockPosition;
			if (ToolVectorOffset != IntVector3.Zero)
			{
				BrushTargetPosition += (Vector3)ToolVectorOffset;
			}
			if (_toolSurfaceOffset != 0)
			{
				Vector3 vector2 = _gameInstance.InteractionModule.TargetBlockHit.Normal * _toolSurfaceOffset;
				BrushTargetPosition += vector2;
			}
		}
		else
		{
			BrushTargetPosition = Vector3.NaN;
		}
		Input input = _gameInstance.Input;
		if (ActiveTool.ClientTool != null)
		{
			ActiveTool.ClientTool.Update(deltaTime);
		}
		if (ActiveTool.BuilderTool.IsBrushTool && input.IsAnyKeyHeld())
		{
			Brush.OnKeyDown();
		}
		if (input.IsShiftHeld())
		{
			InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
			if (input.ConsumeBinding(inputBindings.UndoItemAction))
			{
				OnUndo();
			}
			else if (input.ConsumeBinding(inputBindings.RedoItemAction))
			{
				OnRedo();
			}
		}
	}

	public void Draw(ref Matrix viewProjectionMatrix)
	{
		if (!NeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		if (SelectionArea.NeedsDrawing())
		{
			if (SelectionArea.RenderMode == SelectionArea.SelectionRenderMode.LegacySelection)
			{
				SelectionTool.Draw(ref viewProjectionMatrix);
			}
			else
			{
				PlaySelection.Draw(ref viewProjectionMatrix);
			}
		}
		if (Anchor.NeedsDrawing())
		{
			Anchor.Draw(ref viewProjectionMatrix);
		}
		if (!ToolNeedsDrawing())
		{
			return;
		}
		if (ActiveTool?.ClientTool != null && ActiveTool.ClientTool.NeedsDrawing())
		{
			if (!typeof(SelectionTool).IsInstanceOfType(ActiveTool.ClientTool) && !typeof(PlaySelectionTool).IsInstanceOfType(ActiveTool.ClientTool))
			{
				ActiveTool.ClientTool.Draw(ref viewProjectionMatrix);
			}
		}
		else if (ActiveTool.BuilderTool.IsBrushTool)
		{
			Brush._brushAxisLockPlane.Draw(ref viewProjectionMatrix);
			bool flag = Brush._brushAxisLockPlane.GetMode() != 0 && Brush._brushAxisLockPlane.IsEnabled();
			if (!BrushTargetPosition.IsNaN() && !flag)
			{
				Brush.Draw(ref viewProjectionMatrix, BrushTargetPosition - _gameInstance.SceneRenderer.Data.CameraPosition, (float)builderToolsSettings.BrushOpacity * 0.01f);
				Brush.Draw(ref viewProjectionMatrix, BrushTargetPosition - _gameInstance.SceneRenderer.Data.CameraPosition, (float)builderToolsSettings.BrushOpacity * 0.01f);
			}
		}
	}

	public void DrawText(ref Matrix viewProjectionMatrix)
	{
		if (!NeedsTextDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with TextNeedsDrawing() first before calling this.");
		}
		if (ActiveTool != null && ActiveTool.ClientTool != null && ActiveTool.ClientTool.NeedsTextDrawing())
		{
			ActiveTool.ClientTool.DrawText(ref viewProjectionMatrix);
		}
		if (SelectionTool.NeedsDrawing() && SelectionArea.IsSelectionDefined())
		{
			SelectionTool.DrawText(ref viewProjectionMatrix);
		}
		if (PlaySelection.NeedsDrawing() && SelectionArea.IsSelectionDefined())
		{
			PlaySelection.DrawText(ref viewProjectionMatrix);
		}
	}

	public bool NeedsDrawing()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		return (int)_gameInstance.GameMode == 1 && (ToolNeedsDrawing() || SelectionTool.NeedsDrawing() || PlaySelection.NeedsDrawing() || Anchor.NeedsDrawing());
	}

	public bool NeedsTextDrawing()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		return (int)_gameInstance.GameMode == 1 && (PlaySelection.NeedsDrawing() || SelectionTool.NeedsDrawing() || (ActiveTool?.ClientTool != null && ActiveTool.ClientTool.NeedsTextDrawing()));
	}

	private bool ToolNeedsDrawing()
	{
		if (!HasActiveTool)
		{
			return false;
		}
		if (ActiveTool.ClientTool != null && ActiveTool.ClientTool.NeedsDrawing())
		{
			return true;
		}
		if (ActiveTool.BuilderTool.IsBrushTool)
		{
			if (builderToolsSettings.ToolReachLock)
			{
				return true;
			}
			if (Brush._brushAxisLockPlane.IsEnabled())
			{
				return true;
			}
			if (Brush.lockModeActive)
			{
				return true;
			}
			InteractionModule interactionModule = _gameInstance.InteractionModule;
			if (interactionModule.HasFoundTargetBlock && interactionModule.TargetBlockHit.Distance <= (float)ToolsInteractionDistance)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateUIToolSettings()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		JObject val = new JObject();
		val["ToolDelayMin"] = JToken.op_Implicit(builderToolsSettings.ToolDelayMin);
		val["ToolReachDistance"] = JToken.op_Implicit(ToolsInteractionDistance);
		val["ToolReachLock"] = JToken.op_Implicit(builderToolsSettings.ToolReachLock);
		val["BrushOpacity"] = JToken.op_Implicit(builderToolsSettings.BrushOpacity);
		val["SelectionOpacity"] = JToken.op_Implicit(builderToolsSettings.SelectionOpacity);
		val["BrushShapeRendering"] = JToken.op_Implicit(builderToolsSettings.EnableBrushShapeRendering);
	}

	private void UpdateUIForConfiguringTool()
	{
		bool flag = _gameInstance.App.InGame.Instance.BuilderToolsModule.HasConfigurationToolBrushDataOrArguments() && !_gameInstance.App.InGame.IsToolsSettingsModalOpened;
		if (flag)
		{
			_gameInstance.App.Interface.InGameView.InventoryPage.BuilderToolPanel.ConfiguringToolChange(_configuringTool);
		}
		_gameInstance.App.Interface.InGameView.InventoryPage.BuilderToolPanel.Visible = flag;
		bool visible = _configuringTool?.ItemStack?.Id == "EditorTool_PlaySelection" && !_gameInstance.App.InGame.IsToolsSettingsModalOpened;
		_gameInstance.App.Interface.InGameView.InventoryPage.SelectionCommandsPanel.Visible = visible;
		_gameInstance.App.Interface.InGameView.InventoryPage.Layout();
	}

	private void UpdateUIForActiveTool()
	{
		_gameInstance.App.Interface.InGameView.BuilderToolsLegend.ActiveToolChange(_activeTool);
	}

	public void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		if (!HasActiveTool || (int)interactionType == 7)
		{
			return;
		}
		if (clickType != InteractionModule.ClickType.None)
		{
			if (clickType == InteractionModule.ClickType.Held)
			{
				context.State.State = (InteractionState)4;
			}
			long num = DateTime.UtcNow.Ticks / 10000;
			if (num - TimeOfLastToolInteraction < builderToolsSettings.ToolDelayMin)
			{
				return;
			}
			TimeOfLastToolInteraction = num;
		}
		if (HasActiveBrush)
		{
			Brush.OnInteraction(interactionType, clickType, context, firstRun);
		}
		else if (ActiveTool?.ClientTool != null)
		{
			ActiveTool.ClientTool.OnInteraction(interactionType, clickType, context, firstRun);
		}
	}

	public void OnPickBlockInteraction()
	{
		if (!HasActiveTool || BrushTargetPosition.IsNaN())
		{
			return;
		}
		int block = _gameInstance.MapModule.GetBlock(BrushTargetPosition, int.MaxValue);
		if (block == int.MaxValue)
		{
			return;
		}
		string blockName = _gameInstance.MapModule.ClientBlockTypes[block]?.Name;
		PickBlockActionData.ActionType actionType = (_gameInstance.Input.IsAltHeld() ? PickBlockActionData.ActionType.SetMask : PickBlockActionData.ActionType.SetMaterial);
		PickBlockActionData pickBlockActionData = new PickBlockActionData((int)BrushTargetPosition.X, (int)BrushTargetPosition.Y, (int)BrushTargetPosition.Z, block, actionType);
		if (HasActiveBrush)
		{
			string argKey2;
			string argValue2;
			if (actionType == PickBlockActionData.ActionType.SetMaterial)
			{
				argKey2 = "Material";
				if (pickBlockActionData.Equals(_lastPickBlockAction))
				{
					argValue2 = NextMaterialValue(ActiveTool.BrushData.Material);
				}
				else if (_gameInstance.Input.IsShiftHeld())
				{
					argValue2 = ActiveTool.BrushData.Material + "," + blockName;
				}
				else
				{
					argValue2 = blockName;
				}
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_EYEDROP_SELECT");
			}
			else
			{
				argKey2 = "Mask";
				string mask2 = ActiveTool.BrushData.Mask;
				if (pickBlockActionData.Equals(_lastPickBlockAction))
				{
					argValue2 = NextMaskValue(mask2);
				}
				else if (_gameInstance.Input.IsShiftHeld() && !string.IsNullOrEmpty(mask2))
				{
					argValue2 = AppendBlockNameToMaskList(mask2, blockName);
				}
				else
				{
					argValue2 = blockName;
				}
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_MASK_ADD");
			}
			SendActiveToolArgUpdate((BuilderToolArgGroup)1, argKey2, argValue2, delegate(FailureReply err, SuccessReply reply)
			{
				if (reply != null)
				{
					_gameInstance.Chat.Log("Brush " + argKey2 + " changed to: " + argValue2);
				}
				else
				{
					_gameInstance.Chat.AddBsonMessage(err.Message);
				}
			});
		}
		else
		{
			string argKey = ActiveTool.BuilderTool.GetFirstBlockArgId();
			if (argKey != null)
			{
				ClientItemStack item = ActiveTool.ItemStack;
				string text = ((actionType == PickBlockActionData.ActionType.SetMaterial) ? NextMaterialValue(ActiveTool.BuilderTool.GetItemArgValueOrDefault(ref item, argKey)) : NextMaskValue(ActiveTool.BuilderTool.GetItemArgValueOrDefault(ref item, argKey)));
				string argValue;
				if (pickBlockActionData.Equals(_lastPickBlockAction))
				{
					argValue = text;
				}
				else if (_gameInstance.Input.IsShiftHeld())
				{
					argValue = ActiveTool.BuilderTool.GetItemArgValueOrDefault(ref item, argKey) + "," + blockName;
				}
				else
				{
					argValue = blockName;
				}
				SendActiveToolArgUpdate((BuilderToolArgGroup)0, argKey, argValue, delegate(FailureReply err, SuccessReply reply)
				{
					if (reply != null)
					{
						_gameInstance.Chat.Log(argKey + " changed to: " + argValue);
					}
					else
					{
						_gameInstance.Chat.AddBsonMessage(err.Message);
					}
				});
			}
		}
		_lastPickBlockAction = pickBlockActionData;
		string NextMaskValue(string mask)
		{
			return mask switch
			{
				"-" => "Empty", 
				"Empty" => "!Empty", 
				"!Empty" => blockName, 
				_ => string.Empty, 
			};
		}
		string NextMaterialValue(string material)
		{
			return (material == blockName) ? "Empty" : blockName;
		}
	}

	private static string AppendBlockNameToMaskList(string masks, string blockName)
	{
		if (masks.Equals("-"))
		{
			return blockName;
		}
		string[] array = masks.Split(new char[1] { ',' });
		if (array.Length > 6)
		{
			ArraySegment<string> arraySegment = new ArraySegment<string>(array, 0, 6);
			masks = string.Join(",", arraySegment);
		}
		return masks + "," + blockName;
	}

	public void setActiveBrushMaterial(string blockName, bool isShiftHeld, bool isAltHeld)
	{
		if (!HasActiveBrush)
		{
			return;
		}
		string argKey;
		string argValue;
		if (isAltHeld)
		{
			argKey = "Mask";
			string mask = ActiveTool.BrushData.Mask;
			if (isShiftHeld && !string.IsNullOrEmpty(mask))
			{
				argValue = AppendBlockNameToMaskList(mask, blockName);
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_MASK_ADD");
			}
			else
			{
				argValue = blockName;
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_MASK_SET");
			}
		}
		else
		{
			argKey = "Material";
			if (isShiftHeld)
			{
				argValue = ActiveTool.BrushData.Material + "," + blockName;
			}
			else
			{
				argValue = blockName;
			}
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_EYEDROP_SELECT");
		}
		SendActiveToolArgUpdate((BuilderToolArgGroup)1, argKey, argValue, delegate(FailureReply err, SuccessReply reply)
		{
			if (reply != null)
			{
				_gameInstance.Chat.Log("Brush " + argKey + " changed to: " + argValue);
			}
			else
			{
				_gameInstance.Chat.AddBsonMessage(err.Message);
			}
		});
	}

	public bool TrySelectActiveTool()
	{
		int activeInventorySectionType = _gameInstance.InventoryModule.GetActiveInventorySectionType();
		ClientItemStack activeItem = _gameInstance.InventoryModule.GetActiveItem();
		int activeSlot = _gameInstance.InventoryModule.GetActiveSlot();
		return TrySelectActiveTool(activeInventorySectionType, activeSlot, activeItem);
	}

	public bool TrySelectHotbarActiveTool()
	{
		int sectionId = -1;
		int activeHotbarSlot = _gameInstance.InventoryModule.GetActiveHotbarSlot();
		ClientItemStack activeHotbarItem = _gameInstance.InventoryModule.GetActiveHotbarItem();
		return TrySelectActiveTool(sectionId, activeHotbarSlot, activeHotbarItem);
	}

	public bool TrySelectActiveTool(int sectionId, int slot, ClientItemStack itemStack)
	{
		if (!TrySelectTool(itemStack, out var toolInstance))
		{
			ClearConfiguringTool();
			ActiveTool = null;
			return false;
		}
		SetConfiguringTool(toolInstance, sectionId, slot);
		ActiveTool = toolInstance;
		_gameInstance.App.Interface.InGameView.OnActiveBuilderToolSelected(HasActiveBrush, ActiveTool.BrushData?.FavoriteMaterials?.Length);
		return true;
	}

	public bool TryConfigureTool(int sectionId, int slot, ClientItemStack itemStack)
	{
		if (!TrySelectTool(itemStack, out var toolInstance))
		{
			ClearConfiguringTool();
			return false;
		}
		_gameInstance.App.InGame.CloseToolsSettingsModal();
		SetConfiguringTool(toolInstance, sectionId, slot);
		return true;
	}

	private void SetConfiguringTool(ToolInstance toolInstance, int sectionId, int slot)
	{
		ConfiguringTool = toolInstance;
		_configuringToolSection = sectionId;
		_configuringToolSlot = slot;
	}

	public void ClearConfiguringTool()
	{
		if (ConfiguringTool != null)
		{
			ConfiguringTool = null;
			_configuringToolSection = 0;
			_configuringToolSlot = -1;
		}
	}

	private bool TrySelectTool(ClientItemStack itemStack, out ToolInstance toolInstance)
	{
		toolInstance = null;
		BuilderTool toolFromItemStack = BuilderTool.GetToolFromItemStack(_gameInstance, itemStack);
		if (toolFromItemStack == null)
		{
			return false;
		}
		BrushData brushData = ((!toolFromItemStack.IsBrushTool) ? null : new BrushData(itemStack, toolFromItemStack, delegate(string arg, string val)
		{
			SendConfiguringToolArgUpdate((BuilderToolArgGroup)1, arg, val, delegate(FailureReply err, SuccessReply reply)
			{
				if (err != null)
				{
					_gameInstance.Chat.AddBsonMessage(err.Message);
				}
			});
		}));
		Brush.UpdateBrushData(brushData);
		if (brushData != null)
		{
			_gameInstance.App.Interface.InGameView.BuilderToolsMaterialSlotSelector.SetItemStacks(brushData.GetFavoriteMaterialStacks());
		}
		_gameInstance.App.Interface.InGameView.OnActiveItemSelectorChanged();
		ClientTools.TryGetValue(toolFromItemStack.Id, out var value);
		toolInstance = new ToolInstance(itemStack, toolFromItemStack, value, brushData);
		return true;
	}

	public void SendConfiguringToolArgUpdate(BuilderToolArgGroup argGroup, string argId, string argValue, Action<FailureReply, SuccessReply> callback)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		SendToolArgUpdate(_configuringToolSection, _configuringToolSlot, argGroup, argId, argValue, callback);
	}

	private void SendActiveToolArgUpdate(BuilderToolArgGroup argGroup, string argId, string argValue, Action<FailureReply, SuccessReply> callback)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		int activeInventorySectionType = _gameInstance.InventoryModule.GetActiveInventorySectionType();
		int activeSlot = _gameInstance.InventoryModule.GetActiveSlot();
		SendToolArgUpdate(activeInventorySectionType, activeSlot, argGroup, argId, argValue, callback);
	}

	private void SendToolArgUpdate(int sectionId, int slot, BuilderToolArgGroup argGroup, string argId, string argValue, Action<FailureReply, SuccessReply> callback)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		int num = _gameInstance.AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				callback?.Invoke(err, reply);
			});
		});
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolArgUpdate(num, sectionId, slot, argGroup, argId, argValue));
	}

	private void OnUndo()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolGeneralAction((BuilderToolAction)3));
	}

	private void OnRedo()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolGeneralAction((BuilderToolAction)4));
	}

	private void RegisterEvents()
	{
		HytaleClient.Interface.Interface @interface = _gameInstance.App.Interface;
		@interface.RegisterForEvent("builderTools.argValueChange", _gameInstance, delegate(BuilderToolArgGroup argGroup, string argId, string argValue)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			SendConfiguringToolArgUpdate(argGroup, argId, argValue, delegate(FailureReply err, SuccessReply reply)
			{
				if (err != null)
				{
					_gameInstance.Chat.AddBsonMessage(err.Message);
				}
			});
		});
		@interface.RegisterForEvent("builderTools.selectActiveToolMaterial", _gameInstance, delegate(ClientItemStack stack)
		{
			if (HasActiveBrush)
			{
				ActiveTool.BrushData.SetBrushMaterial(stack.Id);
				_gameInstance.App.Interface.InGameView.BuilderToolsLegend.SetSelectedMaterial(stack);
				_gameInstance.App.Interface.InGameView.BuilderToolsLegend.Layout();
			}
		});
	}

	private void UnregisterEvents()
	{
		HytaleClient.Interface.Interface @interface = _gameInstance.App.Interface;
		@interface.UnregisterFromEvent("builderTools.argValueChange");
		@interface.UnregisterFromEvent("builderTools.selectActiveToolMaterial");
	}
}
