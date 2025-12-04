using System;
using System.Collections.Generic;
using Hypixel.ProtoPlus;
using HytaleClient.Common.Collections;
using HytaleClient.Common.Memory;
using HytaleClient.Core;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class PasteTool : ClientTool
{
	private enum Keybind
	{
		ShiftLeft,
		ShiftRight,
		ShiftForward,
		ShiftBackward,
		ShiftUp,
		ShiftDown,
		ShiftReset
	}

	private readonly BoxRenderer _renderer;

	private readonly BlockShapeRenderer _shapeRenderer;

	private BoundingBox _blockSetBox;

	private Vector3 _offset = new Vector3(0f, 1f, 0f);

	private bool _isPositionLocked = false;

	private IntVector3 _lockedPosition = IntVector3.Zero;

	private bool _drawBlocks;

	private BlockChange[] _cachedBlockChanges;

	private bool _needsDrawBlockUpdate;

	private List<Entity> _previewEntities = new List<Entity>(16);

	private List<Vector3> _previewOffsets = new List<Vector3>(16);

	private Dictionary<Keybind, SDL_Scancode> _keybinds = new Dictionary<Keybind, SDL_Scancode>
	{
		{
			Keybind.ShiftLeft,
			(SDL_Scancode)80
		},
		{
			Keybind.ShiftRight,
			(SDL_Scancode)79
		},
		{
			Keybind.ShiftForward,
			(SDL_Scancode)82
		},
		{
			Keybind.ShiftBackward,
			(SDL_Scancode)81
		},
		{
			Keybind.ShiftUp,
			(SDL_Scancode)75
		},
		{
			Keybind.ShiftDown,
			(SDL_Scancode)78
		},
		{
			Keybind.ShiftReset,
			(SDL_Scancode)74
		}
	};

	public override string ToolId => "Paste";

	public PasteTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		_renderer = new BoxRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_shapeRenderer = new BlockShapeRenderer(_graphics, (int)_graphics.GPUProgramStore.BasicProgram.AttribPosition.Index, (int)_graphics.GPUProgramStore.BasicProgram.AttribTexCoords.Index);
	}

	protected override void DoDispose()
	{
		_shapeRenderer.Dispose();
		_renderer.Dispose();
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		if (_drawBlocks)
		{
			for (int i = 0; i < _previewEntities.Count; i++)
			{
				_previewEntities[i].SetPosition(base.BrushTarget + _previewOffsets[i] + _offset + new Vector3(0.5f, 0f, 0.5f));
			}
			return;
		}
		GLFunctions gL = _graphics.GL;
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		Vector3 position = (_isPositionLocked ? ((Vector3)_lockedPosition) : base.BrushTarget) + _offset;
		Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
		cameraPosition += _gameInstance.SceneRenderer.Data.CameraDirection * 0.06f;
		Vector3 position2 = -cameraPosition;
		Matrix.CreateTranslation(ref position2, out var result);
		Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ViewRotationMatrix, out result);
		Matrix.CreateTranslation(ref position, out var result2);
		Matrix.Multiply(ref result2, ref viewProjectionMatrix, out result2);
		Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ProjectionMatrix, out var result3);
		Matrix.CreateTranslation(ref position, out var result4);
		Matrix.Multiply(ref result4, ref result3, out result4);
		Vector3 zero = Vector3.Zero;
		float value = 0.3f;
		Vector3 one = Vector3.One;
		float value2 = 0.2f;
		basicProgram.MVPMatrix.SetValue(ref result2);
		basicProgram.Color.SetValue(zero);
		basicProgram.Opacity.SetValue(value);
		_graphics.SaveColorMask();
		gL.DepthMask(write: true);
		gL.ColorMask(red: false, green: false, blue: false, alpha: false);
		_shapeRenderer.DrawBlockShape();
		gL.DepthMask(write: false);
		_graphics.RestoreColorMask();
		_shapeRenderer.DrawBlockShape();
		basicProgram.Color.SetValue(one);
		basicProgram.Opacity.SetValue(value2);
		basicProgram.MVPMatrix.SetValue(ref result4);
		_shapeRenderer.DrawBlockShapeOutline();
	}

	protected override void OnActiveStateChange(bool newState)
	{
		if (!newState)
		{
			for (int i = 0; i < _previewEntities.Count; i++)
			{
				_gameInstance.EntityStoreModule.Despawn(_previewEntities[i].NetworkId);
			}
			_previewEntities.Clear();
			_previewOffsets.Clear();
			_needsDrawBlockUpdate = true;
		}
	}

	public void SetDrawBlocks(bool drawBlocks)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (_drawBlocks != drawBlocks || _needsDrawBlockUpdate)
		{
			if (_cachedBlockChanges != null && drawBlocks && _needsDrawBlockUpdate)
			{
				_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue("PrototypePastePreview", out var value);
				Vector3[] positionOffsets;
				int[] adjacentLookup;
				NativeArray<int> blockIds = GenerateChunkArray(_cachedBlockChanges, out positionOffsets, out adjacentLookup);
				List<int> list = FilterVisibleBlocks(blockIds, positionOffsets, adjacentLookup, _gameInstance, _previewOffsets);
				blockIds.Dispose();
				for (int i = 0; i < list.Count; i++)
				{
					int block = list[i];
					_gameInstance.EntityStoreModule.Spawn(-1, out var entity);
					entity.SetIsTangible(isTangible: false);
					entity.SetBlock(block);
					entity.AddEffect(value);
					_previewEntities.Add(entity);
				}
				_needsDrawBlockUpdate = false;
			}
			for (int j = 0; j < _previewEntities.Count; j++)
			{
				_previewEntities[j].IsVisible = drawBlocks;
				_previewEntities[j].SetPositionTeleport(base.BrushTarget + _previewOffsets[j] + _offset + new Vector3(0.5f, 0f, 0.5f));
			}
		}
		_drawBlocks = drawBlocks;
	}

	public static NativeArray<int> GenerateChunkArray(BlockChange[] blockChanges, out Vector3[] positionOffsets, out int[] adjacentLookup)
	{
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		List<Vector3> list = new List<Vector3>(8);
		for (int i = 0; i < blockChanges.Length; i++)
		{
			BlockChange val = blockChanges[i];
			if (val.Block > 0)
			{
				list.Add(new Vector3(blockChanges[i].X, blockChanges[i].Y, blockChanges[i].Z));
			}
		}
		if (list.Count == 0)
		{
			list.Add(Vector3.Zero);
		}
		BoundingBox boundingBox = BoundingBox.CreateFromPoints(list);
		Vector3 size = boundingBox.GetSize();
		IntVector3 intVector = new IntVector3((int)size.X, (int)size.Y, (int)size.Z);
		intVector += IntVector3.One;
		intVector += new IntVector3(2, 2, 2);
		adjacentLookup = new int[6]
		{
			intVector.Z * intVector.X,
			-intVector.Z * intVector.X,
			-1,
			1,
			intVector.X,
			-intVector.X
		};
		int num = intVector.X * intVector.Y * intVector.Z;
		NativeArray<int> result = default(NativeArray<int>);
		result._002Ector(num, (Allocator)0, (AllocOptions)0);
		positionOffsets = new Vector3[num];
		IntVector3 intVector2 = new IntVector3((int)boundingBox.Min.X, (int)boundingBox.Min.Y, (int)boundingBox.Min.Z);
		foreach (BlockChange val2 in blockChanges)
		{
			IntVector3 intVector3 = new IntVector3(val2.X, val2.Y, val2.Z);
			intVector3.X += -intVector2.X;
			intVector3.Y += -intVector2.Y;
			intVector3.Z += -intVector2.Z;
			intVector3 += IntVector3.One;
			int num2 = intVector3.Y * intVector.Z * intVector.X + intVector3.Z * intVector.X + intVector3.X;
			result[num2] = val2.Block;
			positionOffsets[num2] = new Vector3(val2.X, val2.Y, val2.Z);
		}
		return result;
	}

	public static NativeArray<int> GenerateChunkArray(SelectionArea selectionArea, GameInstance gameInstance, out Vector3[] positionOffsets, out int[] adjacentLookup)
	{
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		BoundingBox bounds = selectionArea.GetBounds();
		Vector3 size = bounds.GetSize();
		IntVector3 intVector = new IntVector3((int)size.X, (int)size.Y, (int)size.Z);
		intVector += IntVector3.One;
		intVector += new IntVector3(2, 2, 2);
		adjacentLookup = new int[6]
		{
			intVector.Z * intVector.X,
			-intVector.Z * intVector.X,
			-1,
			1,
			intVector.X,
			-intVector.X
		};
		int num = intVector.X * intVector.Y * intVector.Z;
		NativeArray<int> result = default(NativeArray<int>);
		result._002Ector(num, (Allocator)0, (AllocOptions)0);
		positionOffsets = new Vector3[num];
		IntVector3 intVector2 = new IntVector3((int)bounds.Min.X, (int)bounds.Min.Y, (int)bounds.Min.Z);
		foreach (Vector3 item in selectionArea)
		{
			int block = gameInstance.MapModule.GetBlock(item, int.MaxValue);
			if (block > 0)
			{
				IntVector3 intVector3 = new IntVector3((int)item.X, (int)item.Y, (int)item.Z);
				intVector3.X += -intVector2.X;
				intVector3.Y += -intVector2.Y;
				intVector3.Z += -intVector2.Z;
				intVector3 += IntVector3.One;
				int num2 = intVector3.Y * intVector.Z * intVector.X + intVector3.Z * intVector.X + intVector3.X;
				result[num2] = block;
				positionOffsets[num2] = item;
			}
		}
		return result;
	}

	public static List<int> FilterVisibleBlocks(NativeArray<int> blockIds, Vector3[] positionOffsets, int[] adjacentLookup, GameInstance gameInstance, List<Vector3> outPreviewOffsets)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		List<int> list = new List<int>(8);
		for (int i = 0; i < blockIds.Length; i++)
		{
			int num = blockIds[i];
			ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[num];
			if ((int)clientBlockType.DrawType == 0)
			{
				continue;
			}
			bool flag = false;
			if (clientBlockType.ShouldRenderCube)
			{
				for (int j = 0; j < 6; j++)
				{
					int num2 = i + adjacentLookup[j];
					int num3 = blockIds[num2];
					if (num3 != int.MaxValue)
					{
						ClientBlockType clientBlockType2 = gameInstance.MapModule.ClientBlockTypes[num3];
						if (num3 == 0 || (!clientBlockType2.ShouldRenderCube && clientBlockType2.VerticalFill == 8) || (clientBlockType2.RequiresAlphaBlending && !clientBlockType.RequiresAlphaBlending))
						{
							flag = true;
							break;
						}
					}
				}
			}
			if (flag || (clientBlockType.RenderedBlockyModel != null && (!clientBlockType.ShouldRenderCube || clientBlockType.RequiresAlphaBlending)))
			{
				list.Add(num);
				outPreviewOffsets.Add(positionOffsets[i]);
			}
		}
		return list;
	}

	public override bool NeedsDrawing()
	{
		_ = _blockSetBox;
		return _isPositionLocked || !base.BrushTarget.IsNaN();
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		if (clickType == InteractionModule.ClickType.None)
		{
			_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_PASTE_RELEASE");
		}
		else if ((int)interactionType == 1)
		{
			if (_gameInstance.Input.IsShiftHeld())
			{
				_isPositionLocked = !_isPositionLocked;
				if (_isPositionLocked)
				{
					_lockedPosition = new IntVector3(base.BrushTarget);
				}
				_gameInstance.Notifications.AddNotification("Paste tool position " + (_isPositionLocked ? "locked" : "unlocked") + ".", null);
			}
			else
			{
				if (!_isPositionLocked && base.BrushTarget.IsNaN())
				{
					return;
				}
				Vector3 vector = (_isPositionLocked ? ((Vector3)_lockedPosition) : base.BrushTarget);
				OnClipboardPaste(vector + _offset);
				_isPositionLocked = false;
			}
			if (firstRun)
			{
				_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_PASTE");
			}
		}
		else if (_gameInstance.Input.IsShiftHeld())
		{
			OnClipboardRotate(90, (Axis)0);
		}
		else if (_gameInstance.Input.IsAltHeld())
		{
			OnClipboardRotate(90, (Axis)2);
		}
		else
		{
			OnClipboardRotate(90, (Axis)1);
		}
	}

	public override void Update(float deltaTime)
	{
		if (_gameInstance.Input.IsAnyKeyHeld())
		{
			OnKeyDown();
		}
		SetDrawBlocks(_gameInstance.Input.IsBindingHeld(_gameInstance.App.Settings.InputBindings.PastePreview));
	}

	private void OnKeyDown()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		Input input = _gameInstance.Input;
		if (input.ConsumeKey(_keybinds[Keybind.ShiftReset]))
		{
			_offset = Vector3.Zero;
		}
		if (input.ConsumeKey(_keybinds[Keybind.ShiftUp]))
		{
			_offset += Vector3.Up;
		}
		if (input.ConsumeKey(_keybinds[Keybind.ShiftDown]))
		{
			_offset += Vector3.Down;
		}
		if (!input.IsKeyHeld(_keybinds[Keybind.ShiftLeft]) && !input.IsKeyHeld(_keybinds[Keybind.ShiftRight]) && !input.IsKeyHeld(_keybinds[Keybind.ShiftForward]) && !input.IsKeyHeld(_keybinds[Keybind.ShiftBackward]))
		{
			return;
		}
		Vector3 playerLookDirection = GetPlayerLookDirection();
		if (input.IsAltHeld())
		{
			if (input.ConsumeKey(_keybinds[Keybind.ShiftLeft]))
			{
				if (playerLookDirection.X != 0f)
				{
					OnClipboardRotate((playerLookDirection.X == 1f) ? (-90) : 90, (Axis)0);
				}
				if (playerLookDirection.Z != 0f)
				{
					OnClipboardRotate((playerLookDirection.Z == 1f) ? (-90) : 90, (Axis)2);
				}
			}
			else if (input.ConsumeKey(_keybinds[Keybind.ShiftRight]))
			{
				if (playerLookDirection.X != 0f)
				{
					OnClipboardRotate((playerLookDirection.X == 1f) ? 90 : (-90), (Axis)0);
				}
				if (playerLookDirection.Z != 0f)
				{
					OnClipboardRotate((playerLookDirection.Z == 1f) ? 90 : (-90), (Axis)2);
				}
			}
			else if (input.ConsumeKey(_keybinds[Keybind.ShiftForward]))
			{
				if (playerLookDirection.X != 0f)
				{
					OnClipboardRotate((playerLookDirection.X == 1f) ? (-90) : 90, (Axis)2);
				}
				if (playerLookDirection.Z != 0f)
				{
					OnClipboardRotate((playerLookDirection.Z == 1f) ? 90 : (-90), (Axis)0);
				}
			}
			else if (input.ConsumeKey(_keybinds[Keybind.ShiftBackward]))
			{
				if (playerLookDirection.X != 0f)
				{
					OnClipboardRotate((playerLookDirection.X == 1f) ? 90 : (-90), (Axis)2);
				}
				if (playerLookDirection.Z != 0f)
				{
					OnClipboardRotate((playerLookDirection.Z == 1f) ? (-90) : 90, (Axis)0);
				}
			}
		}
		else if (input.IsShiftHeld())
		{
			if (input.ConsumeKey(_keybinds[Keybind.ShiftLeft]))
			{
				OnClipboardRotate(-90, (Axis)1);
			}
			if (input.ConsumeKey(_keybinds[Keybind.ShiftRight]))
			{
				OnClipboardRotate(90, (Axis)1);
			}
		}
		else
		{
			Vector3 vector = Vector3.Zero;
			if (input.ConsumeKey(_keybinds[Keybind.ShiftLeft]))
			{
				vector = new Vector3(playerLookDirection.Z, 0f, 0f - playerLookDirection.X);
			}
			if (input.ConsumeKey(_keybinds[Keybind.ShiftRight]))
			{
				vector = new Vector3(0f - playerLookDirection.Z, 0f, playerLookDirection.X);
			}
			if (input.ConsumeKey(_keybinds[Keybind.ShiftForward]))
			{
				vector = playerLookDirection;
			}
			if (input.ConsumeKey(_keybinds[Keybind.ShiftBackward]))
			{
				vector = -playerLookDirection;
			}
			if (vector != Vector3.Zero)
			{
				_offset += vector;
			}
		}
	}

	public void UpdateBlockSet(BlockChange[] blockChanges)
	{
		if (blockChanges.Length != 0)
		{
			Vector3[] array = new Vector3[blockChanges.Length];
			for (int i = 0; i < blockChanges.Length; i++)
			{
				array[i] = new Vector3(blockChanges[i].X, blockChanges[i].Y, blockChanges[i].Z);
			}
			_blockSetBox = BoundingBox.CreateFromPoints(array);
			_blockSetBox.Max += Vector3.One;
			Vector3 size = _blockSetBox.GetSize();
			bool[,,] array2 = new bool[(int)size.X, (int)size.Y, (int)size.Z];
			Vector3 min = _blockSetBox.Min;
			for (int j = 0; j < blockChanges.Length; j++)
			{
				Vector3 vector = array[j] - min;
				array2[(int)vector.X, (int)vector.Y, (int)vector.Z] = blockChanges[j].Block > 0;
			}
			_shapeRenderer.UpdateModelData(array2, (int)min.X, (int)min.Y, (int)min.Z);
			for (int k = 0; k < _previewEntities.Count; k++)
			{
				_gameInstance.EntityStoreModule.Despawn(_previewEntities[k].NetworkId);
			}
			_previewEntities.Clear();
			_previewOffsets.Clear();
			_cachedBlockChanges = blockChanges;
			_needsDrawBlockUpdate = true;
		}
	}

	private void OnClipboardPaste(Vector3 position)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolPasteClipboard((int)position.X, (int)position.Y, (int)position.Z));
	}

	private void OnClipboardRotate(int angle, Axis axis)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolRotateClipboard(angle, axis));
	}

	private Vector3 GetPlayerLookDirection()
	{
		float num = MathHelper.SnapValue(_gameInstance.LocalPlayer.LookOrientation.Yaw, (float)System.Math.PI / 2f);
		if (num == 0f)
		{
			return Vector3.Forward;
		}
		if (num == -(float)System.Math.PI / 2f)
		{
			return Vector3.Right;
		}
		if (num == (float)System.Math.PI / 2f)
		{
			return Vector3.Left;
		}
		return Vector3.Backward;
	}
}
