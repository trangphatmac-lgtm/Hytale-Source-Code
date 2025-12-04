using System;
using System.Collections.Generic;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Gizmos.Models;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;

internal class BrushTool : Disposable
{
	private enum Keybind
	{
		BrushToggleReachLock,
		BrushToggleInvert,
		BrushIncreaseWidth,
		BrushDecreaseWidth,
		BrushIncreaseHeight,
		BrushDecreaseHeight,
		BrushIncreaseParam,
		BrushDecreaseParam,
		BrushSphereShape,
		BrushCubeShape,
		BrushCylinderShape,
		BrushConeShape,
		BrushPyramidShape,
		BrushNextShapeOrigin,
		BrushPreviousShapeOrigin
	}

	public enum LockMode
	{
		None,
		OnHold,
		Always
	}

	public enum AxisAndPlanes
	{
		X,
		Y,
		Z,
		XY,
		XZ,
		ZY
	}

	private const string CREATE_BRUSH_RELEASE_SOUNDID = "CREATE_BRUSH_RELEASE";

	private const string CREATE_BRUSH_STAMP_RELEASE_SOUNDID = "CREATE_BRUSH_STAMP_RELEASE";

	private const string CREATE_BRUSH_ERASE_SOUNDID = "CREATE_BRUSH_ERASE";

	private const string CREATE_BRUSH_PAINT_SOUNDID = "CREATE_BRUSH_PAINT";

	private const string CREATE_BRUSH_STAMP_SOUNDID = "CREATE_BRUSH_STAMP";

	private const string CREATE_BRUSH_MODE_SOUNDID = "CREATE_BRUSH_MODE";

	private readonly GameInstance _gameInstance;

	private readonly GraphicsDevice _graphics;

	private readonly BrushToolRenderer _renderer;

	private readonly BlockShapeRenderer _shapeRenderer;

	private readonly BoxRenderer _boxRenderer;

	private readonly BoundingBox _blockBox;

	public Vector3 BrushColor = Vector3.One;

	public BrushData _brushData;

	public Vector3 initialBlockPosition;

	public AxisAndPlanes unlockedAxis = AxisAndPlanes.X;

	public LockMode lockMode = LockMode.None;

	public bool lockModeActive;

	public Vector3 lastSuccessfulPosition;

	public long timeOfLastSuccessfulPlace;

	public bool isHoldingDownBrush;

	public BrushAxisLockPlane _brushAxisLockPlane;

	public bool useServerRaytrace = true;

	private Dictionary<Keybind, SDL_Scancode> _keybinds = new Dictionary<Keybind, SDL_Scancode>
	{
		{
			Keybind.BrushToggleReachLock,
			(SDL_Scancode)99
		},
		{
			Keybind.BrushToggleInvert,
			(SDL_Scancode)85
		},
		{
			Keybind.BrushIncreaseWidth,
			(SDL_Scancode)79
		},
		{
			Keybind.BrushDecreaseWidth,
			(SDL_Scancode)80
		},
		{
			Keybind.BrushIncreaseHeight,
			(SDL_Scancode)82
		},
		{
			Keybind.BrushDecreaseHeight,
			(SDL_Scancode)81
		},
		{
			Keybind.BrushNextShapeOrigin,
			(SDL_Scancode)75
		},
		{
			Keybind.BrushPreviousShapeOrigin,
			(SDL_Scancode)78
		}
	};

	public bool UseBlockShapeRendering => _gameInstance.BuilderToolsModule.builderToolsSettings.EnableBrushShapeRendering;

	public BrushTool(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		_graphics = gameInstance.Engine.Graphics;
		_renderer = new BrushToolRenderer(_graphics);
		_shapeRenderer = new BlockShapeRenderer(_graphics, (int)_graphics.GPUProgramStore.BuilderToolProgram.AttribPosition.Index, (int)_graphics.GPUProgramStore.BuilderToolProgram.AttribTexCoords.Index);
		_boxRenderer = new BoxRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		Vector3 vector = new Vector3(0.05f, 0.05f, 0.05f);
		_blockBox = new BoundingBox(Vector3.Zero - vector, Vector3.One + vector);
		_brushAxisLockPlane = new BrushAxisLockPlane(gameInstance);
	}

	protected override void DoDispose()
	{
		_boxRenderer.Dispose();
		_shapeRenderer.Dispose();
		_renderer.Dispose();
		_brushAxisLockPlane.Dispose();
	}

	public void Update(float deltaTime)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		if ((int)_gameInstance.GameMode == 1)
		{
			_brushAxisLockPlane.Update(deltaTime);
		}
	}

	public void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (!_brushAxisLockPlane.OnInteract(interactionType, clickType, firstRun))
		{
			InitializeBrushOnInteraction(context, firstRun);
			if (!CheckForClickHoldRelease(interactionType, clickType, context, firstRun) && !UseBrush(interactionType, clickType, context, firstRun))
			{
			}
		}
	}

	private void InitializeBrushOnInteraction(InteractionContext context, bool firstRun)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (!firstRun || _gameInstance.BuilderToolsModule.BrushTargetPosition.IsNaN())
		{
			return;
		}
		_gameInstance.LocalPlayer.UsePrimaryItem();
		isHoldingDownBrush = true;
		if (lockMode != 0)
		{
			lockModeActive = true;
			if (lockMode == LockMode.OnHold)
			{
				initialBlockPosition = _gameInstance.BuilderToolsModule.BrushTargetPosition;
			}
		}
		context.State.State = (InteractionState)4;
	}

	private bool CheckForClickHoldRelease(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (!isHoldingDownBrush)
		{
			return false;
		}
		if (clickType == InteractionModule.ClickType.None)
		{
			isHoldingDownBrush = false;
			context.State.State = (InteractionState)0;
			lockModeActive = lockMode == LockMode.Always;
			string soundEventId = (((int)interactionType == 0 || useServerRaytrace) ? "CREATE_BRUSH_RELEASE" : "CREATE_BRUSH_STAMP_RELEASE");
			_gameInstance.AudioModule.PlayLocalSoundEvent(soundEventId);
			return true;
		}
		context.State.State = (InteractionState)4;
		return false;
	}

	private bool UseBrush(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		BuilderToolsModule builderToolsModule = _gameInstance.BuilderToolsModule;
		if (builderToolsModule.BrushTargetPosition.IsNaN())
		{
			return false;
		}
		if (_gameInstance.App.Settings.EnableBrushSpacing)
		{
			float num = Vector3.DistanceSquared(lastSuccessfulPosition, builderToolsModule.BrushTargetPosition);
			if (num < (float)(_gameInstance.App.Settings.BrushSpacingBlocks * _gameInstance.App.Settings.BrushSpacingBlocks))
			{
				builderToolsModule.TimeOfLastToolInteraction = timeOfLastSuccessfulPlace;
				return true;
			}
			long num2 = DateTime.UtcNow.Ticks / 10000;
			timeOfLastSuccessfulPlace = num2;
			lastSuccessfulPosition = new Vector3(builderToolsModule.BrushTargetPosition.X, builderToolsModule.BrushTargetPosition.Y, builderToolsModule.BrushTargetPosition.Z);
		}
		bool isAltPlaySculptBrushModDown = _gameInstance.Input.IsBindingHeld(_gameInstance.App.Settings.InputBindings.AlternatePlaySculptBrushModeModifier);
		SendServerInteraction(interactionType, clickType, context, isAltPlaySculptBrushModDown, firstRun);
		return true;
	}

	private void SendServerInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool isAltPlaySculptBrushModDown, bool firstRun)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Invalid comparison between Unknown and I4
		BuilderToolsModule builderToolsModule = _gameInstance.BuilderToolsModule;
		Vector3 brushTargetPosition = builderToolsModule.BrushTargetPosition;
		bool flag = (!_gameInstance.Input.IsAltHeld() || !_gameInstance.App.Settings.PlaceBlocksAtRange(_gameInstance.GameMode)) && useServerRaytrace && !lockModeActive;
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolOnUseInteraction(interactionType, (int)brushTargetPosition.X, (int)brushTargetPosition.Y, (int)brushTargetPosition.Z, builderToolsModule.ToolVectorOffset.X, builderToolsModule.ToolVectorOffset.Y, builderToolsModule.ToolVectorOffset.Z, isAltPlaySculptBrushModDown, clickType == InteractionModule.ClickType.Held, flag, _gameInstance.App.Settings.BuilderToolsSettings.ShowBuilderToolsNotifications, _gameInstance.App.Settings.PaintOperationsIgnoreHistoryLength));
		if (firstRun)
		{
			string soundEventId = (((int)interactionType != 0) ? (flag ? "CREATE_BRUSH_PAINT" : "CREATE_BRUSH_STAMP") : "CREATE_BRUSH_ERASE");
			_gameInstance.AudioModule.PlayLocalSoundEvent(soundEventId);
		}
	}

	public void Draw(ref Matrix viewProjectionMatrix, Vector3 position, float opacity)
	{
		if (!position.IsNaN())
		{
			GLFunctions gL = _graphics.GL;
			if (!UseBlockShapeRendering)
			{
				ForceFieldProgram builderToolProgram = _graphics.GPUProgramStore.BuilderToolProgram;
				gL.UseProgram(builderToolProgram);
				_renderer.Draw(ref viewProjectionMatrix, ref _gameInstance.SceneRenderer.Data.ViewRotationMatrix, _gameInstance.SceneRenderer.Data.ViewportSize, position, _graphics.BlackColor, opacity, _gameInstance.BuilderToolsModule.DrawHighlightAndUndergroundColor);
				gL.UseProgram(_graphics.GPUProgramStore.BasicProgram);
				return;
			}
			_boxRenderer.Draw(position, _blockBox, viewProjectionMatrix, BrushColor, 0.25f, _graphics.WhiteColor, 0.1f);
			Vector3 vector = _gameInstance.SceneRenderer.Data.CameraDirection * 0.06f;
			Vector3 position2 = -vector;
			Matrix.CreateTranslation(ref position2, out var result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ViewRotationMatrix, out result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ProjectionMatrix, out var result2);
			ForceFieldProgram builderToolProgram2 = _graphics.GPUProgramStore.BuilderToolProgram;
			gL.UseProgram(builderToolProgram2);
			Matrix.CreateTranslation(ref position, out var result3);
			builderToolProgram2.ModelMatrix.SetValue(ref result3);
			builderToolProgram2.ViewMatrix.SetValue(ref _gameInstance.SceneRenderer.Data.ViewRotationMatrix);
			builderToolProgram2.ViewProjectionMatrix.SetValue(ref viewProjectionMatrix);
			builderToolProgram2.CurrentInvViewportSize.SetValue(_gameInstance.SceneRenderer.Data.InvViewportSize);
			Matrix matrix = Matrix.Transpose(Matrix.Invert(result3));
			builderToolProgram2.NormalMatrix.SetValue(ref matrix);
			builderToolProgram2.UVAnimationSpeed.SetValue(0f, 0f);
			builderToolProgram2.OutlineMode.SetValue(builderToolProgram2.OutlineModeNone);
			builderToolProgram2.DrawAndBlendMode.SetValue(builderToolProgram2.DrawModeColor, builderToolProgram2.BlendModeLinear);
			Vector4 value = new Vector4(1f, 1f, 1f, 0.5f);
			float value2 = (_gameInstance.BuilderToolsModule.DrawHighlightAndUndergroundColor ? 0.2f : 0f);
			builderToolProgram2.IntersectionHighlightColorOpacity.SetValue(value);
			builderToolProgram2.IntersectionHighlightThickness.SetValue(value2);
			builderToolProgram2.ColorOpacity.SetValue(1f, 1f, 1f, 0f);
			_shapeRenderer.DrawBlockShape();
			_graphics.SaveColorMask();
			gL.DepthMask(write: true);
			gL.ColorMask(red: false, green: false, blue: false, alpha: false);
			builderToolProgram2.ColorOpacity.SetValue(Vector4.One);
			result3.M42 -= 0.1f;
			builderToolProgram2.ModelMatrix.SetValue(ref result3);
			gL.DepthFunc(GL.ALWAYS);
			_shapeRenderer.DrawBlockShape();
			gL.DepthFunc(GL.LEQUAL);
			_shapeRenderer.DrawBlockShape();
			gL.DepthMask(write: false);
			_graphics.RestoreColorMask();
			builderToolProgram2.IntersectionHighlightColorOpacity.SetValue(1f, 1f, 1f, 0f);
			builderToolProgram2.ColorOpacity.SetValue(0f, 0f, 0f, opacity);
			_shapeRenderer.DrawBlockShape();
			gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
			builderToolProgram2.ViewProjectionMatrix.SetValue(ref result2);
			builderToolProgram2.ColorOpacity.SetValue(1f, 1f, 1f, opacity - 0.1f);
			_shapeRenderer.DrawBlockShapeOutline();
			gL.UseProgram(_graphics.GPUProgramStore.BasicProgram);
		}
	}

	public void UpdateBrushData(BrushData brushData, bool force = false)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected I4, but got Unknown
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Invalid comparison between Unknown and I4
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Invalid comparison between Unknown and I4
		if (!force && (brushData == null || brushData.Equals(_brushData)))
		{
			return;
		}
		_brushData = brushData;
		if (!UseBlockShapeRendering)
		{
			_renderer.UpdateBrushData(brushData, null);
			return;
		}
		int num = (int)((float)brushData.Width * 0.5f);
		int num2 = (int)((float)brushData.Height * 0.5f);
		int num3 = (int)((float)brushData.Width * 0.5f);
		bool[,,] blockData = new bool[0, 0, 0];
		BrushShape shape = brushData.Shape;
		BrushShape val = shape;
		switch ((int)val)
		{
		case 1:
			blockData = SphereModel.BuildVoxelData(num, num2, num3);
			break;
		case 0:
			blockData = CubeModel.BuildVoxelData(num, num2, num3);
			break;
		case 2:
			blockData = CylinderModel.BuildVoxelData(num, num2 * 2, num3);
			break;
		case 5:
			blockData = PyramidModel.BuildVoxelData(num, (num2 == 0) ? 1 : (num2 * 2), num3);
			break;
		case 6:
			blockData = PyramidModel.BuildInvertedVoxelData(num, (num2 == 0) ? 1 : (num2 * 2), num3);
			break;
		case 3:
			blockData = ConeModel.BuildVoxelData(num, (num2 == 0) ? 1 : (num2 * 2), num3);
			break;
		case 4:
			blockData = ConeModel.BuildInvertedVoxelData(num, (num2 == 0) ? 1 : (num2 * 2), num3);
			break;
		}
		int num4 = 0;
		if ((int)_brushData.Origin == 1)
		{
			num4 = num2 + 1;
		}
		else if ((int)_brushData.Origin == 2)
		{
			num4 = -num2;
		}
		_shapeRenderer.UpdateModelData(blockData, -num, -num2 + num4, -num3);
	}

	public void OnKeyDown()
	{
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		Input input = _gameInstance.Input;
		_brushAxisLockPlane.OnKeyDown();
		if (input.ConsumeBinding(_gameInstance.App.Settings.InputBindings.NextBrushLockAxisOrPlane))
		{
			if (lockMode != LockMode.Always)
			{
				AxisAndPlanes[] array = (AxisAndPlanes[])Enum.GetValues(typeof(AxisAndPlanes));
				int num = Array.IndexOf(array, unlockedAxis);
				unlockedAxis = array[(num + 1) % array.Length];
				_gameInstance.Chat.Log($"Set unlocked axis or plane to '{unlockedAxis}'.");
			}
			else
			{
				_gameInstance.Chat.Log("Cannot change locked axis/plane while in Lock Mode: 'Always'");
			}
		}
		if (_gameInstance.Input.ConsumeBinding(_gameInstance.App.Settings.InputBindings.UsePaintModeForBrush))
		{
			useServerRaytrace = !useServerRaytrace;
			_gameInstance.Chat.Log("Set paint mode for PlayPaint Brush to: " + useServerRaytrace);
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_BRUSH_MODE");
		}
		if (input.ConsumeBinding(_gameInstance.App.Settings.InputBindings.NextBrushLockMode))
		{
			LockMode[] array2 = (LockMode[])Enum.GetValues(typeof(LockMode));
			int num2 = Array.IndexOf(array2, lockMode);
			lockMode = array2[(num2 + 1) % array2.Length];
			_gameInstance.Chat.Log($"Brush lock mode set to '{lockMode}'");
			if (lockMode == LockMode.Always)
			{
				initialBlockPosition = _gameInstance.BuilderToolsModule.BrushTargetPosition;
				lockModeActive = true;
			}
			else
			{
				lockModeActive = false;
			}
		}
		if (CheckKey(_keybinds[Keybind.BrushIncreaseWidth]))
		{
			OffsetBrushWidth(2);
		}
		if (CheckKey(_keybinds[Keybind.BrushDecreaseWidth]))
		{
			OffsetBrushWidth(-2);
		}
		if (CheckKey(_keybinds[Keybind.BrushIncreaseHeight]))
		{
			OffsetBrushHeight(2);
		}
		if (CheckKey(_keybinds[Keybind.BrushDecreaseHeight]))
		{
			OffsetBrushHeight(-2);
		}
		if (input.ConsumeKey(_keybinds[Keybind.BrushNextShapeOrigin]))
		{
			if (input.IsShiftHeld())
			{
				BrushOrigin val = _brushData.NextBrushOrigin();
				_gameInstance.Chat.Log($"Set brush origin to: {val}");
			}
			else
			{
				BrushShape val2 = _brushData.NextBrushShape();
				_gameInstance.Chat.Log($"Set brush shape to: {val2}");
			}
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_BRUSH_SHAPE");
		}
		if (input.ConsumeKey(_keybinds[Keybind.BrushPreviousShapeOrigin]))
		{
			if (input.IsShiftHeld())
			{
				BrushOrigin val3 = _brushData.NextBrushOrigin(moveForward: false);
				_gameInstance.Chat.Log($"Set brush origin to: {val3}");
			}
			else
			{
				BrushShape val4 = _brushData.NextBrushShape(moveForward: false);
				_gameInstance.Chat.Log($"Set brush shape to: {val4}");
			}
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_BRUSH_SHAPE");
		}
		if (input.ConsumeKey(_keybinds[Keybind.BrushToggleReachLock]))
		{
			_gameInstance.BuilderToolsModule.builderToolsSettings.ToolReachLock = !_gameInstance.BuilderToolsModule.builderToolsSettings.ToolReachLock;
			_gameInstance.App.Settings.Save();
		}
		if (!input.ConsumeKey((SDL_Scancode)19))
		{
			return;
		}
		BuilderToolsModule builderToolsModule = _gameInstance.BuilderToolsModule;
		if (!builderToolsModule.HasActiveBrush || builderToolsModule.BrushTargetPosition.IsNaN())
		{
			return;
		}
		int block = _gameInstance.MapModule.GetBlock(builderToolsModule.BrushTargetPosition, int.MaxValue);
		if (block != int.MaxValue)
		{
			string text = _gameInstance.MapModule.ClientBlockTypes[block]?.Name;
			string[] favoriteMaterials = _brushData.FavoriteMaterials;
			if (favoriteMaterials != null && Array.IndexOf(favoriteMaterials, text) != -1)
			{
				List<string> list = new List<string>(favoriteMaterials);
				list.Remove(text);
				_brushData.SetFavoriteMaterials(list.ToArray());
				_gameInstance.Chat.Log("Removed from favorite materials: " + text);
			}
			else if (favoriteMaterials == null || favoriteMaterials.Length < 5)
			{
				List<string> list2 = ((favoriteMaterials != null) ? new List<string>(favoriteMaterials) : new List<string>());
				list2.Add(text);
				_brushData.SetFavoriteMaterials(list2.ToArray());
				_gameInstance.Chat.Log("Added to favorite materials: " + text);
			}
		}
		bool CheckKey(SDL_Scancode code)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			return input.IsShiftHeld() ? input.IsKeyHeld(code) : input.ConsumeKey(code);
		}
	}

	private void SetBrushShape(BrushShape shape)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_brushData.SetBrushShape(shape);
		_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_BRUSH_SHAPE");
	}

	private void OffsetBrushHeight(int amount)
	{
		if (_brushData.OffsetBrushHeight(amount))
		{
			OnBrushDimensionChanged(amount);
		}
	}

	private void OffsetBrushWidth(int amount)
	{
		if (_brushData.OffsetBrushWidth(amount))
		{
			OnBrushDimensionChanged(amount);
		}
	}

	private void OnBrushDimensionChanged(int amount)
	{
		_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_SCALE");
	}

	public Vector3 GetLockedBrushPosition(out int distance)
	{
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		if (_brushAxisLockPlane.IsEnabled() && HitDetection.CheckRayPlaneIntersection(_brushAxisLockPlane.GetPosition(), _brushAxisLockPlane.GetNormal(), lookRay.Position, lookRay.Direction, out var intersection, forwardOnly: true))
		{
			distance = (int)Vector3.Distance(intersection, lookRay.Position);
			return intersection;
		}
		Vector3 vector = (_gameInstance.InteractionModule.HasFoundTargetBlock ? _gameInstance.InteractionModule.TargetBlockHit.BlockNormal : Vector3.Zero);
		Vector3 planePoint = initialBlockPosition;
		distance = int.MaxValue;
		Vector3 intersection2 = Vector3.NaN;
		switch (unlockedAxis)
		{
		case AxisAndPlanes.X:
		{
			if (vector == Vector3.Right || vector == Vector3.Left)
			{
				planePoint.X -= vector.X;
			}
			Vector3 planeNormal = Vector3.Subtract(lookRay.Position, new Vector3(lookRay.Position.X, planePoint.Y, planePoint.Z));
			planeNormal.Normalize();
			HitDetection.CheckRayPlaneIntersection(planePoint, planeNormal, lookRay.Position, lookRay.Direction, out intersection2);
			intersection2.Y = planePoint.Y;
			intersection2.Z = planePoint.Z;
			break;
		}
		case AxisAndPlanes.Y:
		{
			if (vector == Vector3.Up || vector == Vector3.Down)
			{
				planePoint.Y -= vector.Y;
			}
			Vector3 planeNormal = Vector3.Subtract(lookRay.Position, new Vector3(planePoint.X, lookRay.Position.Y, planePoint.Z));
			planeNormal.Normalize();
			HitDetection.CheckRayPlaneIntersection(planePoint, planeNormal, lookRay.Position, lookRay.Direction, out intersection2);
			intersection2.X = planePoint.X;
			intersection2.Z = planePoint.Z;
			break;
		}
		case AxisAndPlanes.Z:
		{
			if (vector == Vector3.Backward || vector == Vector3.Forward)
			{
				planePoint.Z -= vector.Z;
			}
			Vector3 planeNormal = Vector3.Subtract(lookRay.Position, new Vector3(planePoint.X, planePoint.Y, lookRay.Position.Z));
			planeNormal.Normalize();
			HitDetection.CheckRayPlaneIntersection(planePoint, planeNormal, lookRay.Position, lookRay.Direction, out intersection2);
			intersection2.X = planePoint.X;
			intersection2.Y = planePoint.Y;
			break;
		}
		case AxisAndPlanes.XY:
			if (vector == Vector3.Right || vector == Vector3.Left)
			{
				planePoint.X -= vector.X;
			}
			else if (vector == Vector3.Up || vector == Vector3.Down)
			{
				planePoint.Y -= vector.Y;
			}
			HitDetection.CheckRayPlaneIntersection(planePoint, new Vector3(0f, 0f, 1f), lookRay.Position, lookRay.Direction, out intersection2);
			intersection2.Z = planePoint.Z;
			break;
		case AxisAndPlanes.XZ:
			if (vector == Vector3.Right || vector == Vector3.Left)
			{
				planePoint.X -= vector.X;
			}
			else if (vector == Vector3.Backward || vector == Vector3.Forward)
			{
				planePoint.Z -= vector.Z;
			}
			HitDetection.CheckRayPlaneIntersection(planePoint, new Vector3(0f, 1f, 0f), lookRay.Position, lookRay.Direction, out intersection2);
			intersection2.Y = planePoint.Y;
			break;
		case AxisAndPlanes.ZY:
			if (vector == Vector3.Up || vector == Vector3.Down)
			{
				planePoint.Y -= vector.Y;
			}
			else if (vector == Vector3.Backward || vector == Vector3.Forward)
			{
				planePoint.Z -= vector.Z;
			}
			HitDetection.CheckRayPlaneIntersection(planePoint, new Vector3(1f, 0f, 0f), lookRay.Position, lookRay.Direction, out intersection2);
			intersection2.X = planePoint.X;
			break;
		}
		if (!intersection2.IsNaN())
		{
			distance = (int)Vector3.Distance(intersection2, lookRay.Position);
		}
		return Vector3.Floor(intersection2);
	}
}
