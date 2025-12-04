using System;
using System.Collections.Generic;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class SelectionTool : ClientTool
{
	private enum Keybind
	{
		SelectionShiftUp,
		SelectionShiftDown,
		SelectionPosOne,
		SelectionPosTwo,
		SelectionCopy,
		SelectionClear,
		SelectionNextDrawMode,
		SelectionNextSet,
		SelectionPreviousSet
	}

	private enum Direction
	{
		None,
		Up,
		Down,
		Left,
		Right,
		Forward,
		Backward
	}

	public enum EditMode
	{
		None,
		MoveSide,
		MovePos1,
		MovePos2,
		ResizeSide,
		ResizePos1,
		ResizePos2
	}

	public Vector3 Color = Vector3.One;

	private SelectionToolRenderer.SelectionDrawMode _selectionDrawMode = SelectionToolRenderer.SelectionDrawMode.Normal;

	private HitDetection.RayBoxCollision _rayBoxHit;

	private Vector3 _resizePosition1;

	private Vector3 _resizePosition2;

	private Vector3 _resizeNormal;

	private Vector3 _resizeOrigin;

	private float _resizeDistance;

	private Direction _resizeDirection = Direction.None;

	private readonly Dictionary<Keybind, SDL_Scancode> _keybinds = new Dictionary<Keybind, SDL_Scancode>
	{
		{
			Keybind.SelectionShiftUp,
			(SDL_Scancode)75
		},
		{
			Keybind.SelectionShiftDown,
			(SDL_Scancode)78
		},
		{
			Keybind.SelectionPosOne,
			(SDL_Scancode)47
		},
		{
			Keybind.SelectionPosTwo,
			(SDL_Scancode)48
		},
		{
			Keybind.SelectionCopy,
			(SDL_Scancode)6
		},
		{
			Keybind.SelectionClear,
			(SDL_Scancode)76
		},
		{
			Keybind.SelectionNextDrawMode,
			(SDL_Scancode)54
		},
		{
			Keybind.SelectionNextSet,
			(SDL_Scancode)75
		},
		{
			Keybind.SelectionPreviousSet,
			(SDL_Scancode)78
		}
	};

	public SelectionArea SelectionArea;

	private long _toolDelayTime = 0L;

	public override string ToolId => "Selection";

	public bool IsCursorOverSelection { get; private set; } = false;


	public EditMode Mode { get; set; } = EditMode.None;


	public EditMode HoverMode { get; set; } = EditMode.None;


	public override bool NeedsDrawing()
	{
		return SelectionArea.NeedsDrawing();
	}

	public override bool NeedsTextDrawing()
	{
		return SelectionArea.NeedsTextDrawing();
	}

	public SelectionTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		SelectionArea = gameInstance.BuilderToolsModule.SelectionArea;
	}

	public override void Update(float deltaTime)
	{
		switch (Mode)
		{
		case EditMode.ResizeSide:
			OnResize();
			break;
		case EditMode.MoveSide:
			OnMove();
			break;
		case EditMode.MovePos1:
		case EditMode.MovePos2:
		case EditMode.ResizePos1:
		case EditMode.ResizePos2:
		{
			Ray lookRay = _gameInstance.CameraModule.GetLookRay();
			Vector3 vector = lookRay.Position + lookRay.Direction * _resizeDistance;
			vector.X = (int)System.Math.Floor(vector.X);
			vector.Y = (int)System.Math.Floor(vector.Y);
			vector.Z = (int)System.Math.Floor(vector.Z);
			if (Mode == EditMode.ResizePos1)
			{
				SelectionArea.Position1 = vector;
			}
			else if (Mode == EditMode.ResizePos2)
			{
				SelectionArea.Position2 = vector;
			}
			else if (Mode == EditMode.MovePos1)
			{
				SelectionArea.Position1 = vector;
				SelectionArea.Position2 = SelectionArea.Position1 + _resizePosition2 - _resizePosition1;
			}
			else if (Mode == EditMode.MovePos2)
			{
				SelectionArea.Position2 = vector;
				SelectionArea.Position1 = SelectionArea.Position2 + _resizePosition1 - _resizePosition2;
			}
			SelectionArea.IsSelectionDirty = true;
			break;
		}
		}
		UpdateSelectionHighlight(_gameInstance.Input.IsAltHeld() && !_gameInstance.Input.IsShiftHeld());
		if (_gameInstance.Input.IsAnyKeyHeld())
		{
			OnKeyDown();
		}
	}

	protected override void OnActiveStateChange(bool newState)
	{
		if (newState)
		{
			SelectionArea.RenderMode = SelectionArea.SelectionRenderMode.LegacySelection;
		}
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		if (SelectionArea.IsSelectionDefined())
		{
			GLFunctions gL = _graphics.GL;
			SceneRenderer.SceneData data = _gameInstance.SceneRenderer.Data;
			Vector3 vector = _gameInstance.SceneRenderer.Data.CameraDirection * 0.1f;
			Vector3 position = -vector;
			Matrix.CreateTranslation(ref position, out var result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ViewRotationMatrix, out result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ProjectionMatrix, out var result2);
			_graphics.SaveColorMask();
			gL.DepthMask(write: true);
			gL.ColorMask(red: false, green: false, blue: false, alpha: false);
			gL.DepthFunc(GL.ALWAYS);
			SelectionArea.Renderer.DrawOutlineBox(ref data.ViewRotationProjectionMatrix, ref data.ViewRotationMatrix, -data.CameraPosition, data.ViewportSize, _graphics.BlackColor, _graphics.BlackColor, 0f, 1f);
			gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
			SelectionArea.Renderer.DrawOutlineBox(ref data.ViewRotationProjectionMatrix, ref data.ViewRotationMatrix, -data.CameraPosition, data.ViewportSize, _graphics.BlackColor, _graphics.BlackColor, 0f, 1f);
			gL.DepthMask(write: false);
			_graphics.RestoreColorMask();
			float num = (float)_builderTools.builderToolsSettings.SelectionOpacity * 0.01f;
			SelectionArea.Renderer.DrawGrid(ref result2, -data.CameraPosition, Color, num, _selectionDrawMode);
			gL.DepthFunc(GL.ALWAYS);
			SelectionArea.Renderer.DrawOutlineBox(ref result2, ref data.ViewRotationMatrix, -data.CameraPosition, data.ViewportSize, _graphics.WhiteColor, _graphics.BlackColor, num, num * 0.25f, _builderTools.DrawHighlightAndUndergroundColor);
			gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
			if (Mode == EditMode.ResizePos1 || Mode == EditMode.MovePos1)
			{
				SelectionArea.Renderer.DrawCornerBoxes(ref viewProjectionMatrix, -data.CameraPosition, _graphics.GreenColor, _graphics.RedColor, 0.4f);
			}
			else if (Mode == EditMode.ResizePos2 || Mode == EditMode.MovePos2)
			{
				SelectionArea.Renderer.DrawCornerBoxes(ref viewProjectionMatrix, -data.CameraPosition, _graphics.GreenColor, _graphics.RedColor, 0.05f, 0.4f);
			}
			else if (HoverMode == EditMode.ResizePos1 || HoverMode == EditMode.MovePos1)
			{
				SelectionArea.Renderer.DrawCornerBoxes(ref viewProjectionMatrix, -data.CameraPosition, _graphics.GreenColor, _graphics.RedColor, 0.2f);
			}
			else if (HoverMode == EditMode.ResizePos2 || HoverMode == EditMode.MovePos2)
			{
				SelectionArea.Renderer.DrawCornerBoxes(ref viewProjectionMatrix, -data.CameraPosition, _graphics.GreenColor, _graphics.RedColor, 0.05f, 0.2f);
			}
			else
			{
				SelectionArea.Renderer.DrawCornerBoxes(ref viewProjectionMatrix, -data.CameraPosition, _graphics.GreenColor, _graphics.RedColor);
			}
		}
		if (FaceHighlightNeedsDrawing())
		{
			Vector3 selectionNormal = ((Mode == EditMode.MoveSide || Mode == EditMode.ResizeSide) ? _resizeNormal : _rayBoxHit.Normal);
			Vector3 color = ((Mode == EditMode.MoveSide) ? _graphics.MagentaColor : ((Mode == EditMode.ResizeSide) ? _graphics.BlueColor : _graphics.CyanColor));
			SelectionArea.Renderer.DrawFaceHighlight(ref _gameInstance.SceneRenderer.Data.ViewRotationProjectionMatrix, selectionNormal, color, -_gameInstance.SceneRenderer.Data.CameraPosition);
		}
		if (!SelectionArea.IsAnySelectionDefined())
		{
			return;
		}
		GLFunctions gL2 = _graphics.GL;
		gL2.DepthFunc(GL.ALWAYS);
		for (int i = 0; i < 8; i++)
		{
			if (SelectionArea.SelectionData[i] != null && i != SelectionArea.SelectionIndex)
			{
				Vector3 vector2 = SelectionArea.SelectionColors[i];
				SelectionArea.BoxRenderer.Draw(Vector3.Zero, SelectionArea.SelectionData[i].Item3, viewProjectionMatrix, vector2, 0.4f, vector2, 0.03f);
			}
		}
		gL2.DepthFunc((!_graphics.UseReverseZ) ? GL.GEQUAL : GL.LEQUAL);
	}

	public override void DrawText(ref Matrix viewProjectionMatrix)
	{
		base.DrawText(ref viewProjectionMatrix);
		SelectionArea.Renderer.DrawText(ref viewProjectionMatrix, _gameInstance.CameraModule.Controller);
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Invalid comparison between Unknown and I4
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Invalid comparison between Unknown and I4
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0465: Invalid comparison between Unknown and I4
		long num = DateTime.UtcNow.Ticks / 10000;
		if (num - _toolDelayTime < 350)
		{
			return;
		}
		_toolDelayTime = num;
		Input input = _gameInstance.Input;
		if (Mode != 0)
		{
			Mode = EditMode.None;
			if ((int)interactionType == 1)
			{
				SelectionArea.Position1 = _resizePosition1;
				SelectionArea.Position2 = _resizePosition2;
				SelectionArea.IsSelectionDirty = true;
			}
			SelectionArea.OnSelectionChange();
		}
		else if (!IsCursorOverSelection && input.IsAltHeld())
		{
			float num2 = -1f;
			int num3 = -1;
			Ray lookRay = _gameInstance.CameraModule.GetLookRay();
			for (int i = 0; i < 8; i++)
			{
				if (SelectionArea.SelectionData[i] != null && HitDetection.CheckRayBoxCollision(SelectionArea.SelectionData[i].Item3, lookRay.Position, lookRay.Direction, out var collision, checkReverse: true))
				{
					float num4 = Vector3.Distance(lookRay.Position, collision.Position);
					if (num3 == -1 || num4 < num2)
					{
						num3 = i;
						num2 = num4;
					}
				}
			}
			if (num3 > -1)
			{
				SelectionArea.SetSelectionIndex(num3);
			}
		}
		else if (IsCursorOverSelection && (input.IsShiftHeld() || input.IsAltHeld()))
		{
			if ((int)interactionType != 0)
			{
				return;
			}
			if (HoverMode == EditMode.ResizePos1 || HoverMode == EditMode.ResizePos2)
			{
				if (input.IsShiftHeld() && input.IsAltHeld())
				{
					Mode = ((HoverMode == EditMode.ResizePos1) ? EditMode.MovePos1 : EditMode.MovePos2);
				}
				else if (input.IsShiftHeld())
				{
					Mode = ((HoverMode == EditMode.ResizePos1) ? EditMode.ResizePos1 : EditMode.ResizePos2);
				}
				Vector3 vector = ((Mode == EditMode.ResizePos1 || Mode == EditMode.MovePos1) ? SelectionArea.Position1 : SelectionArea.Position2);
				float resizeDistance = Vector3.Distance(_gameInstance.CameraModule.Controller.Position, vector);
				_resizeDistance = resizeDistance;
			}
			else
			{
				if (input.IsShiftHeld() && input.IsAltHeld())
				{
					Mode = EditMode.MoveSide;
				}
				else
				{
					Mode = EditMode.ResizeSide;
				}
				_resizeOrigin = _rayBoxHit.Position;
				_resizeNormal = _rayBoxHit.Normal;
				_resizeDirection = GetVectorDirection(_resizeNormal);
			}
			_resizePosition1 = SelectionArea.Position1;
			_resizePosition2 = SelectionArea.Position2;
		}
		else if (!input.IsAnyModifierHeld())
		{
			Vector3 brushTarget = base.BrushTarget;
			if (!brushTarget.IsNaN())
			{
				if (!SelectionArea.IsSelectionDefined())
				{
					SelectionArea.Position1 = (SelectionArea.Position2 = brushTarget);
				}
				else if ((int)interactionType == 0)
				{
					SelectionArea.Position1 = brushTarget;
				}
				else
				{
					SelectionArea.Position2 = brushTarget;
				}
				SelectionArea.IsSelectionDirty = true;
				SelectionArea.OnSelectionChange();
			}
		}
		else
		{
			if (IsCursorOverSelection || !input.IsShiftHeld())
			{
				return;
			}
			Vector3 brushTarget2 = base.BrushTarget;
			if (!brushTarget2.IsNaN())
			{
				if (!SelectionArea.IsSelectionDefined())
				{
					SelectionArea.Position1 = (SelectionArea.Position2 = brushTarget2);
				}
				_resizePosition1 = SelectionArea.Position1;
				_resizePosition2 = SelectionArea.Position2;
				_resizeDistance = Vector3.Distance(_gameInstance.CameraModule.Controller.Position, brushTarget2);
				if ((int)interactionType == 0)
				{
					SelectionArea.Position1 = brushTarget2;
					Mode = EditMode.ResizePos1;
				}
				else
				{
					SelectionArea.Position2 = brushTarget2;
					Mode = EditMode.ResizePos2;
				}
				SelectionArea.IsSelectionDirty = true;
			}
		}
	}

	private void OnKeyDown()
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		Input input = _gameInstance.Input;
		if (input.IsAltHeld())
		{
			if (input.ConsumeKey(_keybinds[Keybind.SelectionNextSet]))
			{
				SelectionArea.CycleSelectionIndex();
			}
			if (input.ConsumeKey(_keybinds[Keybind.SelectionPreviousSet]))
			{
				SelectionArea.CycleSelectionIndex(forward: false);
			}
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionShiftUp]))
		{
			SelectionArea.Shift(Vector3.Up);
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionShiftDown]))
		{
			SelectionArea.Shift(Vector3.Down);
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionNextDrawMode]))
		{
			NextDrawMode();
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionClear]))
		{
			SelectionArea.ClearSelection();
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionPosOne]))
		{
			OnGeneralAction((BuilderToolAction)0);
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionPosTwo]))
		{
			OnGeneralAction((BuilderToolAction)1);
		}
		if (input.IsShiftHeld() && input.ConsumeKey(_keybinds[Keybind.SelectionCopy]))
		{
			OnGeneralAction((BuilderToolAction)2);
		}
	}

	private void OnResize()
	{
		if (!SelectionArea.IsSelectionDefined())
		{
			return;
		}
		Vector3 projectedCursorPosition = GetProjectedCursorPosition();
		if (_resizeDirection == Direction.Up)
		{
			if (SelectionArea.Position1.Y > SelectionArea.Position2.Y)
			{
				SelectionArea.Position1.Y = MathHelper.Min(MathHelper.Max(FloorInt(projectedCursorPosition.Y), 0f), ChunkHelper.Height - 1);
			}
			else
			{
				SelectionArea.Position2.Y = MathHelper.Min(MathHelper.Max(FloorInt(projectedCursorPosition.Y), 0f), ChunkHelper.Height - 1);
			}
		}
		else if (_resizeDirection == Direction.Down)
		{
			if (SelectionArea.Position1.Y < SelectionArea.Position2.Y)
			{
				SelectionArea.Position1.Y = MathHelper.Min(MathHelper.Max(FloorInt(projectedCursorPosition.Y), 0f), ChunkHelper.Height - 1);
			}
			else
			{
				SelectionArea.Position2.Y = MathHelper.Min(MathHelper.Max(FloorInt(projectedCursorPosition.Y), 0f), ChunkHelper.Height - 1);
			}
		}
		else if (_resizeDirection == Direction.Left)
		{
			if (SelectionArea.Position1.X < SelectionArea.Position2.X)
			{
				SelectionArea.Position1.X = FloorInt(projectedCursorPosition.X);
			}
			else
			{
				SelectionArea.Position2.X = FloorInt(projectedCursorPosition.X);
			}
		}
		else if (_resizeDirection == Direction.Right)
		{
			if (SelectionArea.Position1.X > SelectionArea.Position2.X)
			{
				SelectionArea.Position1.X = FloorInt(projectedCursorPosition.X);
			}
			else
			{
				SelectionArea.Position2.X = FloorInt(projectedCursorPosition.X);
			}
		}
		else if (_resizeDirection == Direction.Forward)
		{
			if (SelectionArea.Position1.Z > SelectionArea.Position2.Z)
			{
				SelectionArea.Position2.Z = FloorInt(projectedCursorPosition.Z);
			}
			else
			{
				SelectionArea.Position1.Z = FloorInt(projectedCursorPosition.Z);
			}
		}
		else if (_resizeDirection == Direction.Backward)
		{
			if (SelectionArea.Position1.Z < SelectionArea.Position2.Z)
			{
				SelectionArea.Position2.Z = FloorInt(projectedCursorPosition.Z);
			}
			else
			{
				SelectionArea.Position1.Z = FloorInt(projectedCursorPosition.Z);
			}
		}
		SelectionArea.IsSelectionDirty = true;
	}

	private void OnMove()
	{
		if (!SelectionArea.IsSelectionDefined())
		{
			return;
		}
		Vector3 projectedCursorPosition = GetProjectedCursorPosition();
		Vector3 size = SelectionArea.GetSize();
		if (_resizeDirection == Direction.Up)
		{
			if (SelectionArea.Position1.Y > SelectionArea.Position2.Y)
			{
				SelectionArea.Position1.Y = FloorInt(projectedCursorPosition.Y);
				SelectionArea.Position2.Y = SelectionArea.Position1.Y - size.Y + 1f;
			}
			else
			{
				SelectionArea.Position2.Y = FloorInt(projectedCursorPosition.Y);
				SelectionArea.Position1.Y = SelectionArea.Position2.Y - size.Y + 1f;
			}
		}
		else if (_resizeDirection == Direction.Down)
		{
			if (SelectionArea.Position1.Y < SelectionArea.Position2.Y)
			{
				SelectionArea.Position1.Y = FloorInt(projectedCursorPosition.Y);
				SelectionArea.Position2.Y = SelectionArea.Position1.Y + size.Y - 1f;
			}
			else
			{
				SelectionArea.Position2.Y = FloorInt(projectedCursorPosition.Y);
				SelectionArea.Position1.Y = SelectionArea.Position2.Y - size.Y + 1f;
			}
		}
		else if (_resizeDirection == Direction.Left)
		{
			if (SelectionArea.Position1.X < SelectionArea.Position2.X)
			{
				SelectionArea.Position1.X = FloorInt(projectedCursorPosition.X);
				SelectionArea.Position2.X = SelectionArea.Position1.X + size.X - 1f;
			}
			else
			{
				SelectionArea.Position2.X = FloorInt(projectedCursorPosition.X);
				SelectionArea.Position1.X = SelectionArea.Position2.X - size.X + 1f;
			}
		}
		else if (_resizeDirection == Direction.Right)
		{
			if (SelectionArea.Position1.X > SelectionArea.Position2.X)
			{
				SelectionArea.Position1.X = FloorInt(projectedCursorPosition.X);
				SelectionArea.Position2.X = SelectionArea.Position1.X - size.X + 1f;
			}
			else
			{
				SelectionArea.Position2.X = FloorInt(projectedCursorPosition.X);
				SelectionArea.Position1.X = SelectionArea.Position2.X - size.X + 1f;
			}
		}
		else if (_resizeDirection == Direction.Forward)
		{
			if (SelectionArea.Position1.Z > SelectionArea.Position2.Z)
			{
				SelectionArea.Position2.Z = FloorInt(projectedCursorPosition.Z);
				SelectionArea.Position1.Z = SelectionArea.Position2.Z + size.Z - 1f;
			}
			else
			{
				SelectionArea.Position1.Z = FloorInt(projectedCursorPosition.Z);
				SelectionArea.Position2.Z = SelectionArea.Position1.Z - size.Z + 1f;
			}
		}
		else if (_resizeDirection == Direction.Backward)
		{
			if (SelectionArea.Position1.Z < SelectionArea.Position2.Z)
			{
				SelectionArea.Position2.Z = FloorInt(projectedCursorPosition.Z);
				SelectionArea.Position1.Z = SelectionArea.Position2.Z - size.Z + 1f;
			}
			else
			{
				SelectionArea.Position1.Z = FloorInt(projectedCursorPosition.Z);
				SelectionArea.Position2.Z = SelectionArea.Position1.Z - size.Z + 1f;
			}
		}
		SelectionArea.IsSelectionDirty = true;
	}

	protected override void DoDispose()
	{
	}

	private bool UpdateSelectionHighlight(bool reverse = false)
	{
		Vector3 position = _gameInstance.CameraModule.Controller.Position;
		Vector3 rotation = _gameInstance.CameraModule.Controller.Rotation;
		Quaternion rotation2 = Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f);
		Vector3 vector = Vector3.Transform(Vector3.Forward, rotation2);
		if (reverse)
		{
			vector = -vector;
		}
		if (_gameInstance.Input.IsShiftHeld())
		{
			BoundingBox box = new BoundingBox(SelectionArea.Position1, SelectionArea.Position1 + Vector3.One);
			if (HitDetection.CheckRayBoxCollision(box, position, vector, out _rayBoxHit, checkReverse: true))
			{
				HoverMode = EditMode.ResizePos1;
			}
			else
			{
				box = new BoundingBox(SelectionArea.Position2, SelectionArea.Position2 + Vector3.One);
				if (HitDetection.CheckRayBoxCollision(box, position, vector, out _rayBoxHit, checkReverse: true))
				{
					HoverMode = EditMode.ResizePos2;
				}
				else
				{
					HoverMode = EditMode.None;
				}
			}
		}
		else
		{
			HoverMode = EditMode.None;
		}
		if (SelectionArea.IsSelectionDefined() && HitDetection.CheckRayBoxCollision(SelectionArea.GetBoundsExclusiveMax(), position, vector, out _rayBoxHit, checkReverse: true))
		{
			return IsCursorOverSelection = true;
		}
		return IsCursorOverSelection = false;
	}

	public bool FaceHighlightNeedsDrawing()
	{
		if (Mode == EditMode.MoveSide || Mode == EditMode.ResizeSide)
		{
			return true;
		}
		if (Mode != 0 || HoverMode != 0 || _builderTools.ActiveTool?.BuilderTool?.Id != ToolId)
		{
			return false;
		}
		return _rayBoxHit.Normal != Vector3.Zero && IsCursorOverSelection && (_gameInstance.Input.IsShiftHeld() || _gameInstance.Input.IsAltHeld());
	}

	private Vector3 GetProjectedCursorPosition()
	{
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		Vector3 vector = _resizeOrigin - lookRay.Position;
		Vector2 ray1Position;
		Vector2 ray1Direction;
		Vector2 ray2Position;
		Vector2 ray2Direction;
		if (_resizeDirection == Direction.Up || _resizeDirection == Direction.Down)
		{
			float x = vector.X;
			vector.X = vector.Z;
			vector.Z = 0f - x;
			ray1Position = new Vector2(lookRay.Position.X, lookRay.Position.Z);
			ray1Direction = new Vector2(lookRay.Direction.X, lookRay.Direction.Z);
			ray2Position = new Vector2(_resizeOrigin.X, _resizeOrigin.Z);
			ray2Direction = new Vector2(vector.X, vector.Z);
		}
		else if (_resizeDirection == Direction.Left || _resizeDirection == Direction.Right)
		{
			float x = vector.Y;
			vector.Y = vector.Z;
			vector.Z = 0f - x;
			ray1Position = new Vector2(lookRay.Position.Y, lookRay.Position.Z);
			ray1Direction = new Vector2(lookRay.Direction.Y, lookRay.Direction.Z);
			ray2Position = new Vector2(_resizeOrigin.Y, _resizeOrigin.Z);
			ray2Direction = new Vector2(vector.Y, vector.Z);
		}
		else
		{
			float x = vector.X;
			vector.X = vector.Y;
			vector.Y = 0f - x;
			ray1Position = new Vector2(lookRay.Position.X, lookRay.Position.Y);
			ray1Direction = new Vector2(lookRay.Direction.X, lookRay.Direction.Y);
			ray2Position = new Vector2(_resizeOrigin.X, _resizeOrigin.Y);
			ray2Direction = new Vector2(vector.X, vector.Y);
		}
		if (HitDetection.Get2DRayIntersection(ray1Position, ray1Direction, ray2Position, ray2Direction, out var intersection))
		{
			float num;
			if (_resizeDirection == Direction.Up || _resizeDirection == Direction.Down)
			{
				num = (intersection.X - lookRay.Position.X) / lookRay.Direction.X;
				return new Vector3(intersection.X, num * lookRay.Direction.Y + lookRay.Position.Y, intersection.Y);
			}
			if (_resizeDirection == Direction.Left || _resizeDirection == Direction.Right)
			{
				num = (intersection.Y - lookRay.Position.Z) / lookRay.Direction.Z;
				return new Vector3(num * lookRay.Direction.X + lookRay.Position.X, intersection.X, intersection.Y);
			}
			num = (intersection.X - lookRay.Position.X) / lookRay.Direction.X;
			return new Vector3(intersection.X, intersection.Y, num * lookRay.Direction.Z + lookRay.Position.Z);
		}
		return lookRay.Position;
	}

	public void NextDrawMode()
	{
		if (_selectionDrawMode == SelectionToolRenderer.SelectionDrawMode.Subtract)
		{
			_selectionDrawMode = SelectionToolRenderer.SelectionDrawMode.Normal;
		}
		else
		{
			_selectionDrawMode++;
		}
	}

	private void OnGeneralAction(BuilderToolAction action)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolGeneralAction(action));
	}

	private static int FloorInt(float v)
	{
		return (int)System.Math.Floor(v);
	}

	private static Direction GetVectorDirection(Vector3 vector)
	{
		if (vector == Vector3.Up)
		{
			return Direction.Up;
		}
		if (vector == Vector3.Down)
		{
			return Direction.Down;
		}
		if (vector == Vector3.Left)
		{
			return Direction.Left;
		}
		if (vector == Vector3.Right)
		{
			return Direction.Right;
		}
		if (vector == Vector3.Forward)
		{
			return Direction.Forward;
		}
		if (vector == Vector3.Backward)
		{
			return Direction.Backward;
		}
		return Direction.None;
	}
}
