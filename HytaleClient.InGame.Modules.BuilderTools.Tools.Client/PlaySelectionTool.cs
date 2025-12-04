using System;
using System.Collections.Generic;
using Hypixel.ProtoPlus;
using HytaleClient.Common.Collections;
using HytaleClient.Core;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class PlaySelectionTool : ClientTool
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

	public enum SelectionPoint
	{
		PosOne,
		PosTwo
	}

	public enum EditMode
	{
		None,
		MoveBox,
		MovePos1,
		MovePos2,
		ResizeSide,
		ResizePos1,
		ResizePos2,
		ExtrudeBlocksFromFace,
		CreateSelectionSet2DPlane,
		CreateSelectionSetThirdDimension,
		TranslateSide,
		TransformRotation,
		TransformTranslationGizmos,
		TransformTranslationBrushPoint
	}

	public Vector3 Color = Vector3.One;

	public bool lockX;

	public bool lockY;

	public bool lockZ;

	public Vector3 initialBlockNormal;

	public bool flipXZSelectionExpansionAxis = true;

	public Vector3 gizmoNormal = Vector3.NaN;

	public Vector3 gizmoPosition = Vector3.NaN;

	public bool wasLastInteractionFirstRun;

	public SelectionPoint resizePoint = SelectionPoint.PosOne;

	private const int MaxResizeDistance = 150;

	private const int BlockPreviewSizeLimit = 32768;

	private const int MaxExtrudeDistance = 25;

	private readonly RotationGizmo _rotationGizmo;

	private readonly TranslationGizmo _translationGizmo;

	private SelectionToolRenderer.SelectionDrawMode _selectionDrawMode = SelectionToolRenderer.SelectionDrawMode.Normal;

	private bool renderSideGizmos = true;

	private List<Entity> _previewEntities = new List<Entity>(128);

	private List<Vector3> _entityOffsetFromRotationOrigin = new List<Vector3>(128);

	private Matrix _translationMatrix = Matrix.Identity;

	private Matrix _rotationMatrixSinceStartOfInteraction = Matrix.Identity;

	private Matrix _cumulativeRotationMatrix = Matrix.Identity;

	private IntVector3 _minPositionAtBeginningOfTransform;

	private IntVector3 _maxPositionAtBeginningOfTransform;

	private Vector3 initialRotationOrigin = default(Vector3);

	private Vector3 rotationOrigin = default(Vector3);

	private IntVector3 lastBlockActivatedOnTranslation = IntVector3.Zero;

	private Vector3 lastRotation = default(Vector3);

	private Vector3 positionOneOffsetFromRotationPoint = default(Vector3);

	private Vector3 positionTwoOffsetFromRotationPoint = default(Vector3);

	private BlockChange[] _cachedBlockChanges;

	private IntVector3 initialPasteLocationForPasteMode = IntVector3.Min;

	private bool shouldCutOriginal = false;

	private Dictionary<IntVector3, int> blockIdsAtLocationOfGizmoEdit = new Dictionary<IntVector3, int>();

	private List<List<Entity>> extrudeLayerPreviewEntities = new List<List<Entity>>();

	private Dictionary<IntVector3, int> extrusionPreviewEntityOffsets = new Dictionary<IntVector3, int>();

	private IntVector3 minExtrusionCoordinate;

	private IntVector3 maxExtrusionCoordinate;

	private int currentExtrusionDepth;

	private HitDetection.RayBoxCollision _rayBoxHit;

	private Vector3 _resizePosition1;

	private Vector3 _resizePosition2;

	private Vector3 _resizeNormal;

	private Vector3 _resizeOrigin;

	private float _resizeDistance;

	private Direction _resizeDirection = Direction.None;

	public Vector3[] _blockFaceNormals = new Vector3[6]
	{
		Vector3.Up,
		Vector3.Down,
		Vector3.Backward,
		Vector3.Forward,
		Vector3.Left,
		Vector3.Right
	};

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

	private Vector3 target = new Vector3(0f, 0f, 0f);

	public override string ToolId => "PlaySelection";

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

	public PlaySelectionTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		SelectionArea = gameInstance.BuilderToolsModule.SelectionArea;
		_rotationGizmo = new RotationGizmo(_graphics, _gameInstance.App.Fonts.DefaultFontFamily.RegularFont, OnRotationChange, (float)System.Math.PI / 2f);
		_translationGizmo = new TranslationGizmo(_graphics, OnPositionChange);
	}

	protected override void DoDispose()
	{
		_translationGizmo.Dispose();
		_rotationGizmo.Dispose();
	}

	public override void Update(float deltaTime)
	{
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		float targetBlockHitDistance = (_gameInstance.InteractionModule.HasFoundTargetBlock ? _gameInstance.InteractionModule.TargetBlockHit.Distance : 0f);
		_translationGizmo.Tick(lookRay);
		_rotationGizmo.Tick(lookRay, targetBlockHitDistance);
		_rotationGizmo.UpdateRotation(snapValue: true);
		switch (Mode)
		{
		case EditMode.ResizeSide:
		case EditMode.CreateSelectionSetThirdDimension:
		case EditMode.TranslateSide:
			OnResize();
			break;
		case EditMode.MoveBox:
			OnMove();
			break;
		case EditMode.MovePos1:
		case EditMode.MovePos2:
		case EditMode.ResizePos1:
		case EditMode.ResizePos2:
		{
			Ray lookRay2 = _gameInstance.CameraModule.GetLookRay();
			target = lookRay2.Position + lookRay2.Direction * _resizeDistance;
			target.X = FloorInt(target.X);
			target.Y = FloorInt(target.Y);
			target.Z = FloorInt(target.Z);
			if (Mode == EditMode.ResizePos1)
			{
				SelectionArea.Position1 = target;
			}
			else if (Mode == EditMode.ResizePos2)
			{
				SelectionArea.Position2 = target;
			}
			else if (Mode == EditMode.MovePos1)
			{
				SelectionArea.Position1 = target;
				SelectionArea.Position2 = SelectionArea.Position1 + _resizePosition2 - _resizePosition1;
			}
			else if (Mode == EditMode.MovePos2)
			{
				SelectionArea.Position2 = target;
				SelectionArea.Position1 = SelectionArea.Position2 + _resizePosition1 - _resizePosition2;
			}
			SelectionArea.IsSelectionDirty = true;
			break;
		}
		case EditMode.CreateSelectionSet2DPlane:
		{
			Ray lookRay3 = _gameInstance.CameraModule.GetLookRay();
			Vector3 planePoint = new Vector3(SelectionArea.Position1.X, SelectionArea.Position1.Y, SelectionArea.Position1.Z);
			if (initialBlockNormal == Vector3.Right)
			{
				planePoint.X += 1f;
			}
			else if (initialBlockNormal == Vector3.Up)
			{
				planePoint.Y += 1f;
			}
			else if (initialBlockNormal == Vector3.Backward)
			{
				planePoint.Z += 1f;
			}
			if (HitDetection.CheckRayPlaneIntersection(planePoint, new Vector3(lockX ? 1 : 0, lockY ? 1 : 0, lockZ ? 1 : 0), lookRay3.Position, lookRay3.Direction, out var intersection))
			{
				target = intersection;
			}
			target.X = (lockX ? SelectionArea.Position1.X : ((float)FloorInt(target.X)));
			target.Y = (lockY ? SelectionArea.Position1.Y : ((float)FloorInt(target.Y)));
			target.Z = (lockZ ? SelectionArea.Position1.Z : ((float)FloorInt(target.Z)));
			if (SelectionArea.Position2 != target)
			{
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_SCALE");
			}
			SelectionArea.Position2 = target;
			SelectionArea.IsSelectionDirty = true;
			break;
		}
		case EditMode.TransformTranslationBrushPoint:
		{
			int num = FloorInt(SelectionArea.SelectionSize.Y / 2f + 0.5f);
			if (!base.BrushTarget.IsNaN())
			{
				OnPositionChange(base.BrushTarget + new Vector3(0f, num, 0f));
			}
			break;
		}
		case EditMode.ExtrudeBlocksFromFace:
		{
			int extrusionDepth = GetExtrusionDepth();
			if (extrusionDepth != currentExtrusionDepth)
			{
				currentExtrusionDepth = extrusionDepth;
				EnsureExtrudePreviewEntitiesMatchDepth(currentExtrusionDepth);
			}
			break;
		}
		}
		UpdateSelectionHighlight();
		UpdateGizmoSelection();
		if (_gameInstance.Input.IsAnyKeyHeld())
		{
			OnKeyDown();
		}
	}

	protected override void OnActiveStateChange(bool newState)
	{
		if (newState)
		{
			SelectionArea.RenderMode = SelectionArea.SelectionRenderMode.PlaySelection;
		}
		else
		{
			SelectionArea.RenderMode = SelectionArea.SelectionRenderMode.LegacySelection;
		}
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun && (int)interactionType == 1)
		{
			CancelAllActions(context);
		}
		else if (!PerformActions(interactionType, clickType, context, firstRun))
		{
			wasLastInteractionFirstRun = firstRun;
		}
	}

	public void CancelAllActions(InteractionContext context = null)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		ExitTransformMode(cancelTransform: true);
		Mode = EditMode.None;
		lockX = (lockY = (lockZ = false));
		renderSideGizmos = true;
		if (context != null)
		{
			context.State.State = (InteractionState)0;
		}
	}

	private bool PerformActions(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		if (SetPointsInteraction(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (SwapToOtherSelectionInteraction(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (SingleClickSetBasicCubeInteraction(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (TranslationGizmoInteraction(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (RotationGizmoInteraction(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (TranslateBoxPositionWithPanelGizmo(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (ExtrudeBlocksTouchingSelectionFaceWithPanelGizmo(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (ExtendBoxSideWithPanelGizmo(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		if (CreateSelectionDragOnInteract(interactionType, clickType, context, firstRun))
		{
			return true;
		}
		return false;
	}

	private bool TranslationGizmoInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (Mode == EditMode.TransformTranslationGizmos && (firstRun || clickType == InteractionModule.ClickType.None))
		{
			_translationGizmo.OnInteract(_gameInstance.CameraModule.GetLookRay(), interactionType);
			if (clickType == InteractionModule.ClickType.None)
			{
				lastBlockActivatedOnTranslation = ToIntVector(SelectionArea.CenterPos);
				_translationGizmo.Show(SelectionArea.CenterPos, Vector3.Forward);
			}
			if (firstRun)
			{
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_WIDGET");
			}
			return true;
		}
		return false;
	}

	private bool RotationGizmoInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (Mode == EditMode.TransformRotation && (firstRun || clickType == InteractionModule.ClickType.None))
		{
			_rotationGizmo.OnInteract(interactionType);
			if (clickType == InteractionModule.ClickType.None)
			{
				FinishRotationGizmoInteraction();
			}
			return true;
		}
		return false;
	}

	private void FinishRotationGizmoInteraction()
	{
		_rotationGizmo.Show(rotationOrigin, Vector3.Zero);
		_cumulativeRotationMatrix = Matrix.Multiply(_cumulativeRotationMatrix, _rotationMatrixSinceStartOfInteraction);
		Vector3 vector = new Vector3(0f, 0.5f, 0f);
		for (int i = 0; i < _entityOffsetFromRotationOrigin.Count; i++)
		{
			_entityOffsetFromRotationOrigin[i] = _previewEntities[i].Position + vector - rotationOrigin;
		}
		positionOneOffsetFromRotationPoint = SelectionArea.Position1 - rotationOrigin + new Vector3(0.5f, 0.5f, 0.5f);
		positionTwoOffsetFromRotationPoint = SelectionArea.Position2 - rotationOrigin + new Vector3(0.5f, 0.5f, 0.5f);
		lastRotation = Vector3.NaN;
	}

	private bool SingleClickSetBasicCubeInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		if (IsEditModeATransformationMode(Mode))
		{
			return false;
		}
		if (base.BrushTarget.IsNaN())
		{
			return false;
		}
		if (firstRun && Mode == EditMode.None && !IsCursorOverSelection)
		{
			wasLastInteractionFirstRun = true;
		}
		else
		{
			if (wasLastInteractionFirstRun && clickType == InteractionModule.ClickType.None)
			{
				wasLastInteractionFirstRun = false;
				Mode = EditMode.None;
				SelectionArea.Position1 = base.BrushTarget;
				SelectionArea.Position2 = base.BrushTarget;
				SelectionArea.IsSelectionDirty = true;
				SelectionArea.Update();
				SelectionArea.OnSelectionChange();
				return true;
			}
			wasLastInteractionFirstRun = false;
		}
		return false;
	}

	private bool SetPointsInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		if (IsEditModeATransformationMode(Mode))
		{
			return false;
		}
		if (!firstRun || !_gameInstance.Input.IsKeyHeld((SDL_Scancode)8) || base.BrushTarget.IsNaN())
		{
			return false;
		}
		if ((int)interactionType == 0)
		{
			SelectionArea.Position1 = base.BrushTarget;
		}
		else
		{
			SelectionArea.Position2 = base.BrushTarget;
		}
		SelectionArea.IsSelectionDirty = true;
		SelectionArea.OnSelectionChange();
		return true;
	}

	private bool SwapToOtherSelectionInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		if (IsEditModeATransformationMode(Mode))
		{
			return false;
		}
		if (!firstRun || !_gameInstance.Input.IsShiftHeld())
		{
			return false;
		}
		float num = -1f;
		int num2 = -1;
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		for (int i = 0; i < 8; i++)
		{
			if (SelectionArea.SelectionData[i] != null && HitDetection.CheckRayBoxCollision(SelectionArea.SelectionData[i].Item3, lookRay.Position, lookRay.Direction, out var collision, checkReverse: true))
			{
				float num3 = Vector3.Distance(lookRay.Position, collision.Position);
				if (num2 == -1 || num3 < num)
				{
					num2 = i;
					num = num3;
				}
			}
		}
		if (num2 > -1)
		{
			SelectionArea.SetSelectionIndex(num2);
			return true;
		}
		return false;
	}

	private bool ExtrudeBlocksTouchingSelectionFaceWithPanelGizmo(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun && _gameInstance.Input.IsAltHeld() && Mode == EditMode.None && (int)interactionType == 0 && !gizmoNormal.IsNaN())
		{
			Mode = EditMode.ExtrudeBlocksFromFace;
			InitializeFaceGizmoOperation();
			SetExtrusionCoordinateAndPreviewOffsets();
			return true;
		}
		if (clickType == InteractionModule.ClickType.None && Mode == EditMode.ExtrudeBlocksFromFace)
		{
			context.State.State = (InteractionState)0;
			Mode = EditMode.None;
			_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolStackArea(minExtrusionCoordinate.ToBlockPosition(), maxExtrusionCoordinate.ToBlockPosition(), FloorInt(gizmoNormal.X), FloorInt(gizmoNormal.Y), FloorInt(gizmoNormal.Z), currentExtrusionDepth));
			EnsureExtrudePreviewEntitiesMatchDepth(0);
			return true;
		}
		if (Mode == EditMode.ExtrudeBlocksFromFace)
		{
			context.State.State = (InteractionState)4;
			return true;
		}
		return false;
	}

	private void EnsureExtrudePreviewEntitiesMatchDepth(int depthOfExtrusion)
	{
		if (extrudeLayerPreviewEntities.Count == depthOfExtrusion)
		{
			return;
		}
		while (extrudeLayerPreviewEntities.Count > depthOfExtrusion)
		{
			List<Entity> list = extrudeLayerPreviewEntities[extrudeLayerPreviewEntities.Count - 1];
			foreach (Entity item in list)
			{
				item.IsVisible = false;
				_gameInstance.EntityStoreModule.Despawn(item.NetworkId);
			}
			extrudeLayerPreviewEntities.RemoveAt(extrudeLayerPreviewEntities.Count - 1);
		}
		_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue("PrototypePastePreview", out var value);
		Vector3 vector = new Vector3(0.5f, 0f, 0.5f);
		while (extrudeLayerPreviewEntities.Count < depthOfExtrusion)
		{
			List<Entity> list2 = new List<Entity>();
			int num = extrudeLayerPreviewEntities.Count + 1;
			IntVector3 intVector = new IntVector3(FloorInt(gizmoNormal.X * (float)num), FloorInt(gizmoNormal.Y * (float)num), FloorInt(gizmoNormal.Z * (float)num));
			foreach (KeyValuePair<IntVector3, int> extrusionPreviewEntityOffset in extrusionPreviewEntityOffsets)
			{
				Vector3 positionTeleport = new Vector3((float)(minExtrusionCoordinate.X + extrusionPreviewEntityOffset.Key.X + intVector.X) + vector.X, (float)(minExtrusionCoordinate.Y + extrusionPreviewEntityOffset.Key.Y + intVector.Y) + vector.Y, (float)(minExtrusionCoordinate.Z + extrusionPreviewEntityOffset.Key.Z + intVector.Z) + vector.Z);
				_gameInstance.EntityStoreModule.Spawn(-1, out var entity);
				entity.SetIsTangible(isTangible: false);
				entity.SetBlock(extrusionPreviewEntityOffset.Value);
				entity.AddEffect(value);
				entity.SetPositionTeleport(positionTeleport);
				list2.Add(entity);
			}
			extrudeLayerPreviewEntities.Add(list2);
		}
	}

	public int GetExtrusionDepth()
	{
		if (!SelectionArea.IsSelectionDefined())
		{
			return 0;
		}
		Vector3 projectedCursorPosition = GetProjectedCursorPosition();
		int num = -1;
		if (_resizeDirection == Direction.Up || _resizeDirection == Direction.Down)
		{
			num = FloorInt(System.Math.Abs(projectedCursorPosition.Y - (float)minExtrusionCoordinate.Y));
		}
		else if (_resizeDirection == Direction.Left || _resizeDirection == Direction.Right)
		{
			num = FloorInt(System.Math.Abs(projectedCursorPosition.X - (float)minExtrusionCoordinate.X));
		}
		else if (_resizeDirection == Direction.Forward || _resizeDirection == Direction.Backward)
		{
			num = FloorInt(System.Math.Abs(projectedCursorPosition.Z - (float)minExtrusionCoordinate.Z));
		}
		if (num == -1)
		{
			return 0;
		}
		return MathHelper.Clamp(num, 0, 25);
	}

	private void SetExtrusionCoordinateAndPreviewOffsets()
	{
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		currentExtrusionDepth = 0;
		Vector3 vector = Vector3.Min(SelectionArea.Position1, SelectionArea.Position2);
		Vector3 vector2 = Vector3.Max(SelectionArea.Position1, SelectionArea.Position2);
		minExtrusionCoordinate = new IntVector3(FloorInt(vector.X), FloorInt(vector.Y), FloorInt(vector.Z));
		maxExtrusionCoordinate = new IntVector3(FloorInt(vector2.X), FloorInt(vector2.Y), FloorInt(vector2.Z));
		if (gizmoNormal == Vector3.Right)
		{
			minExtrusionCoordinate.X = FloorInt(vector2.X);
		}
		else if (gizmoNormal == Vector3.Left)
		{
			maxExtrusionCoordinate.X = FloorInt(vector.X);
		}
		else if (gizmoNormal == Vector3.Up)
		{
			minExtrusionCoordinate.Y = FloorInt(vector2.Y);
		}
		else if (gizmoNormal == Vector3.Down)
		{
			maxExtrusionCoordinate.Y = FloorInt(vector.Y);
		}
		else if (gizmoNormal == Vector3.Backward)
		{
			minExtrusionCoordinate.Z = FloorInt(vector2.Z);
		}
		else
		{
			if (!(gizmoNormal == Vector3.Forward))
			{
				return;
			}
			maxExtrusionCoordinate.Z = FloorInt(vector.Z);
		}
		extrusionPreviewEntityOffsets.Clear();
		extrudeLayerPreviewEntities.Clear();
		for (int i = minExtrusionCoordinate.X; i <= maxExtrusionCoordinate.X; i++)
		{
			for (int j = minExtrusionCoordinate.Y; j <= maxExtrusionCoordinate.Y; j++)
			{
				for (int k = minExtrusionCoordinate.Z; k <= maxExtrusionCoordinate.Z; k++)
				{
					int block = _gameInstance.MapModule.GetBlock(i, j, k, int.MaxValue);
					if ((int)_gameInstance.MapModule.ClientBlockTypes[block].DrawType != 0 && block != int.MaxValue)
					{
						IntVector3 key = new IntVector3(i - minExtrusionCoordinate.X, j - minExtrusionCoordinate.Y, k - minExtrusionCoordinate.Z);
						extrusionPreviewEntityOffsets[key] = block;
					}
				}
			}
		}
	}

	private bool TranslateBoxPositionWithPanelGizmo(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun && _gameInstance.Input.IsShiftHeld() && Mode == EditMode.None && (int)interactionType == 0 && !gizmoNormal.IsNaN())
		{
			Mode = EditMode.MoveBox;
			InitializeFaceGizmoOperation();
			SelectionArea.IsSelectionDirty = true;
			return true;
		}
		if (clickType == InteractionModule.ClickType.None && Mode == EditMode.MoveBox)
		{
			context.State.State = (InteractionState)0;
			Mode = EditMode.None;
			SelectionArea.IsSelectionDirty = true;
			SelectionArea.Update();
			SelectionArea.OnSelectionChange();
			return true;
		}
		if (Mode == EditMode.MoveBox)
		{
			context.State.State = (InteractionState)4;
			return true;
		}
		return false;
	}

	private bool ExtendBoxSideWithPanelGizmo(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (firstRun && Mode == EditMode.None && (int)interactionType == 0 && !gizmoNormal.IsNaN())
		{
			context.State.State = (InteractionState)0;
			Mode = EditMode.TranslateSide;
			InitializeFaceGizmoOperation();
			SelectionArea.IsSelectionDirty = true;
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_WIDGET");
			return true;
		}
		if (clickType == InteractionModule.ClickType.None && Mode == EditMode.TranslateSide)
		{
			context.State.State = (InteractionState)0;
			Mode = EditMode.None;
			SelectionArea.IsSelectionDirty = true;
			SelectionArea.OnSelectionChange();
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_PLACE");
			return true;
		}
		if (Mode == EditMode.TranslateSide)
		{
			context.State.State = (InteractionState)4;
			return true;
		}
		return false;
	}

	public void InitializeFaceGizmoOperation()
	{
		_resizeOrigin = gizmoPosition;
		_resizeNormal = gizmoNormal;
		_resizeDirection = GetVectorDirection(_resizeNormal);
		_resizePosition1 = SelectionArea.Position1;
		_resizePosition2 = SelectionArea.Position2;
		if (gizmoNormal == Vector3.Right)
		{
			resizePoint = ((!(SelectionArea.Position1.X > SelectionArea.Position2.X)) ? SelectionPoint.PosTwo : SelectionPoint.PosOne);
		}
		else if (gizmoNormal == Vector3.Left)
		{
			resizePoint = ((!(SelectionArea.Position1.X < SelectionArea.Position2.X)) ? SelectionPoint.PosTwo : SelectionPoint.PosOne);
		}
		else if (gizmoNormal == Vector3.Up)
		{
			resizePoint = ((!(SelectionArea.Position1.Y > SelectionArea.Position2.Y)) ? SelectionPoint.PosTwo : SelectionPoint.PosOne);
		}
		else if (gizmoNormal == Vector3.Down)
		{
			resizePoint = ((!(SelectionArea.Position1.Y < SelectionArea.Position2.Y)) ? SelectionPoint.PosTwo : SelectionPoint.PosOne);
		}
		else if (gizmoNormal == Vector3.Backward)
		{
			resizePoint = ((!(SelectionArea.Position1.Z > SelectionArea.Position2.Z)) ? SelectionPoint.PosTwo : SelectionPoint.PosOne);
		}
		else if (gizmoNormal == Vector3.Forward)
		{
			resizePoint = ((!(SelectionArea.Position1.Z < SelectionArea.Position2.Z)) ? SelectionPoint.PosTwo : SelectionPoint.PosOne);
		}
	}

	private bool CreateSelectionDragOnInteract(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		if (IsEditModeATransformationMode(Mode))
		{
			return false;
		}
		if (firstRun)
		{
			if (Mode == EditMode.None && (int)interactionType == 0 && !IsCursorOverSelection)
			{
				if (base.BrushTarget.IsNaN())
				{
					return false;
				}
				Vector3 brushTarget = base.BrushTarget;
				SelectionArea.Position1 = (SelectionArea.Position2 = brushTarget);
				Mode = EditMode.CreateSelectionSet2DPlane;
				lockX = (lockY = (lockZ = false));
				context.State.State = (InteractionState)4;
				initialBlockNormal = _gameInstance.InteractionModule.TargetBlockHit.BlockNormal;
				if (initialBlockNormal.X == 1f || initialBlockNormal.X == -1f)
				{
					lockX = true;
				}
				else if (initialBlockNormal.Y == 1f || initialBlockNormal.Y == -1f)
				{
					lockY = true;
				}
				else if (initialBlockNormal.Z == 1f || initialBlockNormal.Z == -1f)
				{
					lockZ = true;
				}
				SelectionArea.IsSelectionDirty = true;
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_WIDGET");
				return true;
			}
			if (Mode == EditMode.CreateSelectionSetThirdDimension)
			{
				Mode = EditMode.None;
				lockX = (lockY = (lockZ = false));
				context.State.State = (InteractionState)0;
				SelectionArea.IsSelectionDirty = true;
				SelectionArea.OnSelectionChange();
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_PLACE");
				return true;
			}
		}
		if (clickType == InteractionModule.ClickType.None && Mode == EditMode.CreateSelectionSet2DPlane)
		{
			context.State.State = (InteractionState)0;
			Mode = EditMode.CreateSelectionSetThirdDimension;
			Vector3 position = _gameInstance.CameraModule.Controller.Position;
			Vector3 rotation = _gameInstance.CameraModule.Controller.Rotation;
			Quaternion rotation2 = Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f);
			Vector3 direction = Vector3.Transform(Vector3.Forward, rotation2);
			HitDetection.CheckRayBoxCollision(SelectionArea.GetBoundsExclusiveMax(), position, direction, out _rayBoxHit, checkReverse: true);
			_resizeOrigin = _rayBoxHit.Position;
			_resizeNormal = (flipXZSelectionExpansionAxis ? initialBlockNormal.Sign(new Vector3(-1f, 1f, -1f)) : initialBlockNormal);
			_resizeDirection = GetVectorDirection(_resizeNormal);
			_resizePosition1 = SelectionArea.Position1;
			_resizePosition2 = SelectionArea.Position2;
			lockX = (lockY = (lockZ = false));
			SelectionArea.IsSelectionDirty = true;
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_PLACE");
			return true;
		}
		if (Mode == EditMode.CreateSelectionSet2DPlane)
		{
			context.State.State = (InteractionState)4;
			return true;
		}
		return false;
	}

	public IntVector3 ToIntVector(Vector3 vector3)
	{
		return new IntVector3(FloorInt(vector3.X + 0.1f), FloorInt(vector3.Y + 0.1f), FloorInt(vector3.Z + 0.1f));
	}

	public void EnterTransformMode(EditMode editModeToEnter, bool cutOriginal = true, BlockChange[] clipboard = null)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		if (Mode != 0 || !IsEditModeATransformationMode(editModeToEnter))
		{
			return;
		}
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolSetTransformationModeState(true));
		_translationMatrix = Matrix.Identity;
		_rotationMatrixSinceStartOfInteraction = Matrix.Identity;
		_cumulativeRotationMatrix = Matrix.Identity;
		shouldCutOriginal = cutOriginal;
		lastRotation = Vector3.Zero;
		if (clipboard != null && clipboard.Length != 0)
		{
			initialPasteLocationForPasteMode = ToIntVector(_gameInstance.BuilderToolsModule.BrushTargetPosition) + new IntVector3(0, 1, 0);
			int num = 0;
			foreach (BlockChange val in clipboard)
			{
				if (val.Y < 0 && System.Math.Abs(val.Y) > num)
				{
					num = System.Math.Abs(val.Y);
				}
			}
			Vector3 vector = new Vector3(clipboard[0].X, clipboard[0].Y, clipboard[0].Z);
			Vector3 vector2 = new Vector3(clipboard[0].X, clipboard[0].Y, clipboard[0].Z);
			for (int j = 0; j < clipboard.Length; j++)
			{
				BlockChange obj = clipboard[j];
				obj.Y += num;
				BlockChange val2 = clipboard[j];
				Vector3 value = new Vector3(val2.X, val2.Y, val2.Z);
				vector = Vector3.Min(vector, value);
				vector2 = Vector3.Max(vector2, value);
			}
			SelectionArea.Position1 = initialPasteLocationForPasteMode.ToVector3() + vector2;
			SelectionArea.Position2 = initialPasteLocationForPasteMode.ToVector3() + vector;
			SelectionArea.IsSelectionDirty = true;
			SelectionArea.Update();
		}
		rotationOrigin = SelectionArea.CenterPos + GetRotationGizmoOffsetFromDimensions(SelectionArea.SelectionSize);
		initialRotationOrigin = new Vector3(rotationOrigin.X, rotationOrigin.Y, rotationOrigin.Z);
		positionOneOffsetFromRotationPoint = SelectionArea.Position1 - rotationOrigin + new Vector3(0.5f, 0.5f, 0.5f);
		positionTwoOffsetFromRotationPoint = SelectionArea.Position2 - rotationOrigin + new Vector3(0.5f, 0.5f, 0.5f);
		lastBlockActivatedOnTranslation = ToIntVector(SelectionArea.CenterPos);
		BoundingBox bounds = SelectionArea.GetBounds();
		_minPositionAtBeginningOfTransform = ToIntVector(bounds.Min);
		_maxPositionAtBeginningOfTransform = ToIntVector(bounds.Max);
		renderSideGizmos = false;
		if (clipboard != null)
		{
			CreateBlockEntityPreview(clipboard);
		}
		else
		{
			CreateBlockEntityPreview();
		}
		SwapTransformMode(editModeToEnter);
	}

	public void SwapTransformMode(EditMode editModeToSwitchTo)
	{
		if (IsEditModeATransformationMode(editModeToSwitchTo))
		{
			switch (editModeToSwitchTo)
			{
			case EditMode.TransformRotation:
				_translationGizmo.Hide();
				_rotationGizmo.Show(rotationOrigin, Vector3.Zero);
				Mode = EditMode.TransformRotation;
				break;
			case EditMode.TransformTranslationGizmos:
				_rotationGizmo.Hide();
				_translationGizmo.Show(SelectionArea.CenterPos, Vector3.Forward);
				Mode = EditMode.TransformTranslationGizmos;
				lastBlockActivatedOnTranslation = ToIntVector(SelectionArea.CenterPos);
				break;
			case EditMode.TransformTranslationBrushPoint:
				_rotationGizmo.Hide();
				_translationGizmo.Hide();
				Mode = EditMode.TransformTranslationBrushPoint;
				lastBlockActivatedOnTranslation = ToIntVector(SelectionArea.CenterPos);
				break;
			}
		}
	}

	public bool IsInTransformationMode()
	{
		return IsEditModeATransformationMode(Mode);
	}

	private bool IsEditModeATransformationMode(EditMode editMode)
	{
		return editMode == EditMode.TransformRotation || editMode == EditMode.TransformTranslationGizmos || editMode == EditMode.TransformTranslationBrushPoint;
	}

	public void ExitTransformMode(bool cancelTransform)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		if (!IsInTransformationMode())
		{
			return;
		}
		if (cancelTransform)
		{
			SelectionArea.Position1 = _maxPositionAtBeginningOfTransform.ToVector3();
			SelectionArea.Position2 = _minPositionAtBeginningOfTransform.ToVector3();
			SelectionArea.IsSelectionDirty = true;
			foreach (KeyValuePair<IntVector3, int> item in blockIdsAtLocationOfGizmoEdit)
			{
				_gameInstance.MapModule.SetClientBlock(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
			}
			_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolSetTransformationModeState(false));
		}
		else
		{
			ConfirmTransformationModePlacement(applyTransformationToSelectionMinMax: true, isExitingTransformMode: true);
		}
		_translationGizmo.Hide();
		_rotationGizmo.Hide();
		for (int i = 0; i < _previewEntities.Count; i++)
		{
			Entity entity = _previewEntities[i];
			entity.IsVisible = false;
			_gameInstance.EntityStoreModule.Despawn(entity.NetworkId);
		}
		_previewEntities.Clear();
		initialPasteLocationForPasteMode = IntVector3.Min;
		renderSideGizmos = true;
		Mode = EditMode.None;
	}

	public void ConfirmTransformationModePlacement(bool applyTransformationToSelectionMinMax = false, bool isExitingTransformMode = false)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		if (IsInTransformationMode())
		{
			BlockPosition val = null;
			if (!initialPasteLocationForPasteMode.Equals(IntVector3.Min))
			{
				val = new BlockPosition(initialPasteLocationForPasteMode.X, initialPasteLocationForPasteMode.Y, initialPasteLocationForPasteMode.Z);
			}
			Matrix matrix = Matrix.Multiply(_cumulativeRotationMatrix, _translationMatrix);
			_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolSelectionTransform(Matrix.ToFlatFloatArray(matrix), _minPositionAtBeginningOfTransform.ToBlockPosition(), _maxPositionAtBeginningOfTransform.ToBlockPosition(), initialRotationOrigin.ToProtocolVector3f(), shouldCutOriginal, applyTransformationToSelectionMinMax, isExitingTransformMode, val));
		}
	}

	public float SnapRadianTo90Degrees(float radian)
	{
		return (float)(1.5707963705062866 * System.Math.Round(radian / ((float)System.Math.PI / 2f)));
	}

	private void OnRotationChange(Vector3 rotation)
	{
		Vector3 vector = new Vector3(SnapRadianTo90Degrees(rotation.Pitch), SnapRadianTo90Degrees(rotation.Yaw), SnapRadianTo90Degrees(rotation.Roll));
		if (!vector.Equals(lastRotation))
		{
			lastRotation = vector;
			_rotationMatrixSinceStartOfInteraction = Matrix.CreateFromYawPitchRoll(lastRotation.Yaw, lastRotation.Pitch, lastRotation.Roll);
			for (int i = 0; i < _previewEntities.Count; i++)
			{
				Vector3 vector2 = rotationOrigin + Vector3.Transform(_entityOffsetFromRotationOrigin[i], _rotationMatrixSinceStartOfInteraction);
				Vector3 positionTeleport = new Vector3(vector2.X, (float)System.Math.Floor(vector2.Y), vector2.Z);
				_previewEntities[i].SetPositionTeleport(positionTeleport);
			}
			SelectionArea.Position1 = Vector3.Transform(positionOneOffsetFromRotationPoint, _rotationMatrixSinceStartOfInteraction) + rotationOrigin - new Vector3(0.5f, 0.5f, 0.5f);
			SelectionArea.Position2 = Vector3.Transform(positionTwoOffsetFromRotationPoint, _rotationMatrixSinceStartOfInteraction) + rotationOrigin - new Vector3(0.5f, 0.5f, 0.5f);
			SelectionArea.IsSelectionDirty = true;
			SelectionArea.Update();
		}
	}

	private void OnPositionChange(Vector3 translatedTo)
	{
		if (lastBlockActivatedOnTranslation.Equals(IntVector3.Zero))
		{
			lastBlockActivatedOnTranslation = ToIntVector(SelectionArea.CenterPos);
		}
		IntVector3 intVector = ToIntVector(translatedTo);
		IntVector3 intVector2 = intVector - lastBlockActivatedOnTranslation;
		if (intVector2.X < 1 && intVector2.Y < 1 && intVector2.Z < 1 && intVector2.X > -1 && intVector2.Y > -1 && intVector2.Z > -1)
		{
			return;
		}
		lastBlockActivatedOnTranslation = intVector;
		SelectionArea.Position1 = (ToIntVector(SelectionArea.Position1) + intVector2).ToVector3();
		SelectionArea.Position2 = (ToIntVector(SelectionArea.Position2) + intVector2).ToVector3();
		Matrix.AddTranslation(ref _translationMatrix, intVector2.X, intVector2.Y, intVector2.Z);
		Vector3 vector = intVector2.ToVector3();
		foreach (Entity previewEntity in _previewEntities)
		{
			previewEntity.SetPositionTeleport(previewEntity.NextPosition + vector);
		}
		rotationOrigin += vector;
		SelectionArea.IsSelectionDirty = true;
		SelectionArea.Update();
	}

	public bool TryEnterRotationMode(bool cutOriginal = true)
	{
		if (Mode == EditMode.None)
		{
			EnterTransformMode(EditMode.TransformRotation, cutOriginal);
		}
		else
		{
			if (Mode != EditMode.TransformTranslationGizmos && Mode != EditMode.TransformTranslationBrushPoint)
			{
				return false;
			}
			SwapTransformMode(EditMode.TransformRotation);
		}
		return true;
	}

	public bool TryEnterTranslationGizmoMode(bool cutOriginal = true)
	{
		if (Mode == EditMode.None)
		{
			EnterTransformMode(EditMode.TransformTranslationGizmos, cutOriginal);
		}
		else
		{
			if (Mode != EditMode.TransformRotation && Mode != EditMode.TransformTranslationBrushPoint)
			{
				return false;
			}
			SwapTransformMode(EditMode.TransformTranslationGizmos);
		}
		return true;
	}

	public bool TryEnterTranslationBrushPointMode(bool cutOriginal = true)
	{
		if (Mode == EditMode.None)
		{
			EnterTransformMode(EditMode.TransformTranslationBrushPoint, cutOriginal);
		}
		else
		{
			if (Mode != EditMode.TransformRotation && Mode != EditMode.TransformTranslationGizmos)
			{
				return false;
			}
			SwapTransformMode(EditMode.TransformTranslationBrushPoint);
		}
		return true;
	}

	public bool TryEnterTranslationModeWithClipboard(BlockChange[] clipboard)
	{
		if (Mode != 0)
		{
			return false;
		}
		if (clipboard == null || clipboard.Length == 0)
		{
			_gameInstance.Chat.Log("You do not currently have anything copied in your clipboard.");
			return false;
		}
		EnterTransformMode(EditMode.TransformTranslationGizmos, cutOriginal: true, clipboard);
		return true;
	}

	public void OnScrollWheelEvent(int directionOfScroll)
	{
		if (IsInTransformationMode())
		{
			OnRotationChange(new Vector3(0f, (float)directionOfScroll * ((float)System.Math.PI / 2f), 0f));
			FinishRotationGizmoInteraction();
		}
	}

	private void OnKeyDown()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		Input input = _gameInstance.Input;
		if (Mode == EditMode.None && input.IsShiftHeld() && input.ConsumeKey((SDL_Scancode)18))
		{
			_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolSelectionToolAskForClipboard());
		}
		else if (input.ConsumeKey((SDL_Scancode)12))
		{
			TryEnterTranslationBrushPointMode(!input.IsAltHeld());
		}
		else if (input.ConsumeKey((SDL_Scancode)18))
		{
			TryEnterTranslationGizmoMode(!input.IsAltHeld());
		}
		else if (input.ConsumeKey((SDL_Scancode)21))
		{
			TryEnterRotationMode(!input.IsAltHeld());
		}
		else if (input.ConsumeKey((SDL_Scancode)14))
		{
			if (input.IsAltHeld())
			{
				ConfirmTransformationModePlacement();
			}
			else
			{
				ExitTransformMode(input.IsShiftHeld());
			}
		}
		if (input.IsKeyHeld((SDL_Scancode)8))
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
			CancelAllActions();
			SelectionArea.ClearSelection();
		}
		if (input.IsShiftHeld() && input.ConsumeKey(_keybinds[Keybind.SelectionCopy]))
		{
			OnGeneralAction((BuilderToolAction)2);
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionPosOne]))
		{
			SelectionArea.Position1 = Vector3.Floor(_gameInstance.LocalPlayer.Position);
			if (SelectionArea.Position2.IsNaN())
			{
				SelectionArea.Position2 = Vector3.Floor(_gameInstance.LocalPlayer.Position);
			}
			SelectionArea.IsSelectionDirty = true;
			SelectionArea.Update();
			SelectionArea.OnSelectionChange();
		}
		if (input.ConsumeKey(_keybinds[Keybind.SelectionPosTwo]))
		{
			SelectionArea.Position2 = Vector3.Floor(_gameInstance.LocalPlayer.Position);
			if (SelectionArea.Position1.IsNaN())
			{
				SelectionArea.Position1 = Vector3.Floor(_gameInstance.LocalPlayer.Position);
			}
			SelectionArea.IsSelectionDirty = true;
			SelectionArea.Update();
			SelectionArea.OnSelectionChange();
		}
	}

	public void CreateBlockEntityPreview(BlockChange[] clipboard = null)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue("PrototypePastePreview", out var value);
		_previewEntities.Clear();
		_entityOffsetFromRotationOrigin.Clear();
		blockIdsAtLocationOfGizmoEdit.Clear();
		Vector3 vector = new Vector3(0.5f, 0f, 0.5f);
		Vector3[] positionOffsets;
		int[] adjacentLookup;
		NativeArray<int> blockIds = ((clipboard != null) ? PasteTool.GenerateChunkArray(clipboard, out positionOffsets, out adjacentLookup) : PasteTool.GenerateChunkArray(SelectionArea, _gameInstance, out positionOffsets, out adjacentLookup));
		List<Vector3> list = new List<Vector3>(16);
		List<int> list2 = PasteTool.FilterVisibleBlocks(blockIds, positionOffsets, adjacentLookup, _gameInstance, list);
		blockIds.Dispose();
		if (list2.Count > 32768)
		{
			return;
		}
		for (int i = 0; i < list2.Count; i++)
		{
			int block = list2[i];
			Vector3 vector2 = list[i];
			Vector3 vector3 = vector2 + vector;
			if (clipboard != null)
			{
				vector3 += initialPasteLocationForPasteMode.ToVector3();
			}
			_gameInstance.EntityStoreModule.Spawn(-1, out var entity);
			entity.SetIsTangible(isTangible: false);
			entity.SetBlock(block);
			entity.AddEffect(value);
			entity.SetPositionTeleport(vector3);
			_previewEntities.Add(entity);
			_entityOffsetFromRotationOrigin.Add(vector3 + new Vector3(0f, 0.5f, 0f) - rotationOrigin);
		}
		if (clipboard != null || !shouldCutOriginal)
		{
			return;
		}
		foreach (Vector3 item in SelectionArea)
		{
			int block2 = _gameInstance.MapModule.GetBlock(item, int.MaxValue);
			_gameInstance.MapModule.SetClientBlock((int)item.X, (int)item.Y, (int)item.Z, 0);
			blockIdsAtLocationOfGizmoEdit[ToIntVector(item)] = block2;
		}
	}

	private Vector3 GetRotationGizmoOffsetFromDimensions(Vector3 dimensions)
	{
		int num = 0;
		if (dimensions.X % 2f == 0f)
		{
			num |= 4;
		}
		if (dimensions.Y % 2f == 0f)
		{
			num |= 2;
		}
		if (dimensions.Z % 2f == 0f)
		{
			num |= 1;
		}
		return num switch
		{
			0 => new Vector3(0f, 0f, 0f), 
			1 => new Vector3(0f, 0f, 0.5f), 
			2 => new Vector3(0f, 0.5f, 0f), 
			4 => new Vector3(0.5f, 0f, 0f), 
			3 => new Vector3(0.5f, 0f, 0f), 
			6 => new Vector3(0f, 0f, 0.5f), 
			5 => new Vector3(0f, 0.5f, 0f), 
			7 => new Vector3(0f, 0f, 0f), 
			_ => new Vector3(0f, 0f, 0f), 
		};
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		if (SelectionArea.IsSelectionDefined())
		{
			GLFunctions gL = _graphics.GL;
			SceneRenderer.SceneData data = _gameInstance.SceneRenderer.Data;
			if (_rotationGizmo.Visible)
			{
				_rotationGizmo.Draw(ref viewProjectionMatrix, _gameInstance.CameraModule.Controller, -data.CameraPosition);
			}
			if (_translationGizmo.Visible)
			{
				_translationGizmo.Draw(ref viewProjectionMatrix, -data.CameraPosition);
			}
			Vector3 vector = _gameInstance.SceneRenderer.Data.CameraDirection * 0.1f;
			Vector3 position = -vector;
			Matrix.CreateTranslation(ref position, out var result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ViewRotationMatrix, out result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ProjectionMatrix, out var result2);
			_graphics.SaveColorMask();
			gL.DepthMask(write: true);
			gL.ColorMask(red: false, green: false, blue: false, alpha: false);
			gL.DepthFunc(GL.ALWAYS);
			SelectionArea.Renderer.DrawOutlineBox(ref viewProjectionMatrix, ref data.ViewRotationMatrix, -data.CameraPosition, data.ViewportSize, _graphics.BlackColor, _graphics.BlackColor, 0f, 1f);
			gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
			SelectionArea.Renderer.DrawOutlineBox(ref viewProjectionMatrix, ref data.ViewRotationMatrix, -data.CameraPosition, data.ViewportSize, _graphics.BlackColor, _graphics.BlackColor, 0f, 1f);
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
			else if (Mode == EditMode.ResizePos2 || Mode == EditMode.MovePos2 || Mode == EditMode.CreateSelectionSet2DPlane || Mode == EditMode.CreateSelectionSetThirdDimension)
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
			if (renderSideGizmos)
			{
				Vector3 vector2 = gizmoNormal;
				if (Mode == EditMode.CreateSelectionSetThirdDimension || Mode == EditMode.TranslateSide || Mode == EditMode.ResizeSide)
				{
					Vector3 other = new Vector3(-1f, -1f, -1f);
					Vector3 vector3 = ((resizePoint == SelectionPoint.PosOne) ? SelectionArea.Position1 : SelectionArea.Position2);
					Vector3 vector4 = ((resizePoint == SelectionPoint.PosOne) ? SelectionArea.Position2 : SelectionArea.Position1);
					if (gizmoNormal == Vector3.Right)
					{
						vector2 = ((vector3.X > vector4.X) ? vector2 : vector2.Sign(other));
					}
					else if (gizmoNormal == Vector3.Left)
					{
						vector2 = ((vector3.X < vector4.X) ? vector2 : vector2.Sign(other));
					}
					else if (gizmoNormal == Vector3.Up)
					{
						vector2 = ((vector3.Y > vector4.Y) ? vector2 : vector2.Sign(other));
					}
					else if (gizmoNormal == Vector3.Down)
					{
						vector2 = ((vector3.Y < vector4.Y) ? vector2 : vector2.Sign(other));
					}
					else if (gizmoNormal == Vector3.Backward)
					{
						vector2 = ((vector3.Z > vector4.Z) ? vector2 : vector2.Sign(other));
					}
					else if (gizmoNormal == Vector3.Forward)
					{
						vector2 = ((vector3.Z < vector4.Z) ? vector2 : vector2.Sign(other));
					}
				}
				Vector3[] blockFaceNormals = _blockFaceNormals;
				for (int i = 0; i < blockFaceNormals.Length; i++)
				{
					Vector3 selectionNormal = blockFaceNormals[i];
					Vector3 color = ((!_gameInstance.Input.IsShiftHeld()) ? ((!_gameInstance.Input.IsAltHeld()) ? (selectionNormal.Equals(vector2) ? _graphics.CyanColor : _graphics.BlueColor) : (selectionNormal.Equals(vector2) ? _graphics.YellowColor : _graphics.WhiteColor)) : (selectionNormal.Equals(vector2) ? _graphics.MagentaColor : _graphics.RedColor));
					Settings settings = _gameInstance.App.Settings;
					SelectionArea.Renderer.DrawResizeGizmoForFace(_gameInstance.SceneRenderer.Data.CameraPosition, ref viewProjectionMatrix, selectionNormal, color, settings.MinPlaySelectGizmoSize, settings.MaxPlaySelectGizmoSize, settings.PercentageOfPlaySelectionLengthGizmoShouldRender);
				}
			}
		}
		if (FaceHighlightNeedsDrawing())
		{
			Vector3 vector5 = ((Mode == EditMode.MoveBox || Mode == EditMode.ResizeSide || Mode == EditMode.CreateSelectionSetThirdDimension || Mode == EditMode.TranslateSide) ? _resizeNormal : _rayBoxHit.Normal);
			Vector3 other2 = new Vector3(-1f, -1f, -1f);
			Vector3 vector6 = ((resizePoint == SelectionPoint.PosOne) ? SelectionArea.Position1 : SelectionArea.Position2);
			Vector3 vector7 = ((resizePoint == SelectionPoint.PosOne) ? SelectionArea.Position2 : SelectionArea.Position1);
			if (gizmoNormal == Vector3.Right)
			{
				vector5 = ((vector6.X > vector7.X) ? vector5 : vector5.Sign(other2));
			}
			else if (gizmoNormal == Vector3.Left)
			{
				vector5 = ((vector6.X < vector7.X) ? vector5 : vector5.Sign(other2));
			}
			else if (gizmoNormal == Vector3.Up)
			{
				vector5 = ((vector6.Y > vector7.Y) ? vector5 : vector5.Sign(other2));
			}
			else if (gizmoNormal == Vector3.Down)
			{
				vector5 = ((vector6.Y < vector7.Y) ? vector5 : vector5.Sign(other2));
			}
			else if (gizmoNormal == Vector3.Backward)
			{
				vector5 = ((vector6.Z > vector7.Z) ? vector5 : vector5.Sign(other2));
			}
			else if (gizmoNormal == Vector3.Forward)
			{
				vector5 = ((vector6.Z < vector7.Z) ? vector5 : vector5.Sign(other2));
			}
			Vector3 color2 = ((Mode == EditMode.MoveBox) ? _graphics.MagentaColor : ((Mode == EditMode.ResizeSide || Mode == EditMode.CreateSelectionSetThirdDimension || Mode == EditMode.TranslateSide) ? _graphics.CyanColor : _graphics.BlueColor));
			SelectionArea.Renderer.DrawFaceHighlight(ref viewProjectionMatrix, vector5, color2, -_gameInstance.SceneRenderer.Data.CameraPosition);
		}
		if (!SelectionArea.IsAnySelectionDefined())
		{
			return;
		}
		GLFunctions gL2 = _graphics.GL;
		gL2.DepthFunc(GL.ALWAYS);
		for (int j = 0; j < 8; j++)
		{
			if (SelectionArea.SelectionData[j] != null && j != SelectionArea.SelectionIndex)
			{
				Vector3 vector8 = SelectionArea.SelectionColors[j];
				SelectionArea.BoxRenderer.Draw(Vector3.Zero, SelectionArea.SelectionData[j].Item3, viewProjectionMatrix, vector8, 0.4f, vector8, 0.03f);
			}
		}
		gL2.DepthFunc((!_graphics.UseReverseZ) ? GL.GEQUAL : GL.LEQUAL);
	}

	public override void DrawText(ref Matrix viewProjectionMatrix)
	{
		base.DrawText(ref viewProjectionMatrix);
		if (Mode == EditMode.TransformRotation && _rotationGizmo.Visible)
		{
			_rotationGizmo.DrawText();
		}
		SelectionArea.Renderer.DrawText(ref viewProjectionMatrix, _gameInstance.CameraModule.Controller);
	}

	private void OnResize()
	{
		if (!SelectionArea.IsSelectionDefined())
		{
			return;
		}
		Vector3 projectedCursorPosition = GetProjectedCursorPosition();
		Vector3 size = SelectionArea.GetSize();
		float num = size.X * size.Y * size.Z;
		if (_resizeDirection == Direction.Up || _resizeDirection == Direction.Down)
		{
			float num2 = System.Math.Abs(projectedCursorPosition.Y - SelectionArea.CenterPos.Y);
			if (num2 > 150f)
			{
				projectedCursorPosition.Y = SelectionArea.CenterPos.Y + _resizeNormal.Y * 150f;
			}
			float y = MathHelper.Min(MathHelper.Max(FloorInt(projectedCursorPosition.Y), 0f), ChunkHelper.Height - 1);
			if (resizePoint == SelectionPoint.PosOne)
			{
				SelectionArea.Position1.Y = y;
			}
			else
			{
				SelectionArea.Position2.Y = y;
			}
		}
		else if (_resizeDirection == Direction.Left || _resizeDirection == Direction.Right)
		{
			float num3 = System.Math.Abs(projectedCursorPosition.X - SelectionArea.CenterPos.X);
			if (num3 > 150f)
			{
				projectedCursorPosition.X = SelectionArea.CenterPos.X + _resizeNormal.X * 150f;
			}
			if (resizePoint == SelectionPoint.PosOne)
			{
				SelectionArea.Position1.X = FloorInt(projectedCursorPosition.X);
			}
			else
			{
				SelectionArea.Position2.X = FloorInt(projectedCursorPosition.X);
			}
		}
		else if (_resizeDirection == Direction.Forward || _resizeDirection == Direction.Backward)
		{
			float num4 = System.Math.Abs(projectedCursorPosition.Z - SelectionArea.CenterPos.Z);
			if (num4 > 150f)
			{
				projectedCursorPosition.Z = SelectionArea.CenterPos.Z + _resizeNormal.Z * 150f;
			}
			if (resizePoint == SelectionPoint.PosOne)
			{
				SelectionArea.Position1.Z = FloorInt(projectedCursorPosition.Z);
			}
			else
			{
				SelectionArea.Position2.Z = FloorInt(projectedCursorPosition.Z);
			}
		}
		Vector3 size2 = SelectionArea.GetSize();
		float num5 = size2.X * size2.Y * size2.Z;
		if (num5 > num)
		{
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_SCALE");
		}
		else if (num5 < num)
		{
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_SCALE");
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
		Vector3 position = SelectionArea.Position1;
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
		if (position != SelectionArea.Position1)
		{
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_SELECTION_DRAG");
		}
		SelectionArea.IsSelectionDirty = true;
	}

	private void UpdateGizmoSelection()
	{
		if (Mode != 0)
		{
			return;
		}
		Vector3 position = _gameInstance.CameraModule.Controller.Position;
		Vector3 rotation = _gameInstance.CameraModule.Controller.Rotation;
		Quaternion rotation2 = Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f);
		Vector3 lineDirection = Vector3.Transform(Vector3.Forward, rotation2);
		lineDirection.Normalize();
		Settings settings = _gameInstance.App.Settings;
		float num = MathHelper.Clamp(SelectionArea.SelectionSize.X * settings.PercentageOfPlaySelectionLengthGizmoShouldRender, settings.MinPlaySelectGizmoSize, settings.MaxPlaySelectGizmoSize) / 2f;
		float num2 = MathHelper.Clamp(SelectionArea.SelectionSize.Y * settings.PercentageOfPlaySelectionLengthGizmoShouldRender, settings.MinPlaySelectGizmoSize, settings.MaxPlaySelectGizmoSize) / 2f;
		float num3 = MathHelper.Clamp(SelectionArea.SelectionSize.Z * settings.PercentageOfPlaySelectionLengthGizmoShouldRender, settings.MinPlaySelectGizmoSize, settings.MaxPlaySelectGizmoSize) / 2f;
		float num4 = float.MaxValue;
		bool flag = false;
		Vector3[] blockFaceNormals = _blockFaceNormals;
		for (int i = 0; i < blockFaceNormals.Length; i++)
		{
			Vector3 planeNormal = blockFaceNormals[i];
			Vector3 vector = new Vector3(SelectionArea.CenterPos.X + SelectionArea.SelectionSize.X / 2f * planeNormal.X, SelectionArea.CenterPos.Y + SelectionArea.SelectionSize.Y / 2f * planeNormal.Y, SelectionArea.CenterPos.Z + SelectionArea.SelectionSize.Z / 2f * planeNormal.Z);
			if (!HitDetection.CheckRayPlaneIntersection(vector, planeNormal, position, lineDirection, out var intersection, forwardOnly: true))
			{
				continue;
			}
			Vector3 vector2 = Vector3.Subtract(vector, intersection).Abs();
			if ((planeNormal.X != 0f || !(vector2.X > num)) && (planeNormal.Y != 0f || !(vector2.Y > num2)) && (planeNormal.Z != 0f || !(vector2.Z > num3)))
			{
				float num5 = Vector3.DistanceSquared(intersection, position);
				if (!(num5 >= num4))
				{
					num4 = num5;
					gizmoPosition = intersection;
					gizmoNormal = planeNormal;
					flag = true;
				}
			}
		}
		if (!flag)
		{
			gizmoNormal = Vector3.NaN;
		}
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
		if (Mode == EditMode.MoveBox || Mode == EditMode.ResizeSide || Mode == EditMode.TranslateSide || Mode == EditMode.CreateSelectionSetThirdDimension)
		{
			return true;
		}
		if (Mode != 0 || HoverMode != 0 || _builderTools.ActiveTool?.BuilderTool?.Id != ToolId)
		{
			return false;
		}
		return false;
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
