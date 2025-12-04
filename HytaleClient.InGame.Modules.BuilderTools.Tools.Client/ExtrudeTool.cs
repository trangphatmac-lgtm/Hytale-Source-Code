using System;
using Hypixel.ProtoPlus;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Map;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class ExtrudeTool : ClientTool
{
	private const string DepthArgKey = "ExtrudeDepth";

	private const string RadiusArgKey = "ExtrudeRadius";

	private readonly HitDetection.RaycastOptions _raycastOptions = new HitDetection.RaycastOptions
	{
		IgnoreEmptyCollisionMaterial = true,
		IgnoreFluids = true,
		CheckOversizedBoxes = true,
		Distance = 150f
	};

	private readonly BlockShapeRenderer _renderer;

	private int _extrudeDepth;

	private int _extrudeRadius;

	private bool _hasTarget;

	private Vector3 _target;

	private Vector3 _normal;

	public override string ToolId => "Extrude";

	public ExtrudeTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		_renderer = new BlockShapeRenderer(_graphics, (int)_graphics.GPUProgramStore.BasicProgram.AttribPosition.Index, (int)_graphics.GPUProgramStore.BasicProgram.AttribTexCoords.Index);
	}

	protected override void DoDispose()
	{
		_renderer.Dispose();
	}

	public override void Update(float deltaTime)
	{
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		if (!_gameInstance.HitDetection.RaycastBlock(lookRay.Position, lookRay.Direction, _raycastOptions, out var raycastHit))
		{
			_hasTarget = false;
		}
		else if (!_hasTarget || !(raycastHit.BlockPosition == _target) || !(raycastHit.Normal == _normal))
		{
			_hasTarget = true;
			_target = raycastHit.BlockPosition;
			_normal = raycastHit.Normal;
			UpdateBlockShapeModelData(_extrudeRadius);
		}
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		if (!base.BrushTarget.IsNaN())
		{
			GLFunctions gL = _graphics.GL;
			BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
			Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
			Vector3 vector = _gameInstance.SceneRenderer.Data.CameraDirection * 0.06f;
			Vector3 position = -vector;
			Vector3 position2 = _target - cameraPosition;
			Matrix.CreateTranslation(ref position, out var result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ViewRotationMatrix, out result);
			Matrix.Multiply(ref result, ref _gameInstance.SceneRenderer.Data.ProjectionMatrix, out var result2);
			Matrix.CreateTranslation(ref position2, out var result3);
			Matrix.Multiply(ref result3, ref viewProjectionMatrix, out result3);
			Matrix.CreateTranslation(ref position2, out var result4);
			Matrix.Multiply(ref result4, ref result2, out result4);
			Vector3 zero = Vector3.Zero;
			Vector3 one = Vector3.One;
			float value = 0.3f;
			_graphics.SaveColorMask();
			gL.DepthMask(write: true);
			gL.ColorMask(red: false, green: false, blue: false, alpha: false);
			basicProgram.MVPMatrix.SetValue(ref result3);
			basicProgram.Color.SetValue(zero);
			basicProgram.Opacity.SetValue(value);
			_renderer.DrawBlockShape();
			gL.DepthMask(write: false);
			_graphics.RestoreColorMask();
			_renderer.DrawBlockShape();
			basicProgram.Color.SetValue(one);
			basicProgram.MVPMatrix.SetValue(ref result4);
			_renderer.DrawBlockShapeOutline();
		}
	}

	public override bool NeedsDrawing()
	{
		return _hasTarget;
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		if (clickType != InteractionModule.ClickType.None)
		{
			_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolExtrudeAction((int)_target.X, (int)_target.Y, (int)_target.Z, (int)_normal.X, (int)_normal.Y, (int)_normal.Z));
		}
	}

	public override void OnToolItemChange(ClientItemStack itemStack)
	{
		BuilderTool toolFromItemStack = BuilderTool.GetToolFromItemStack(_gameInstance, itemStack);
		if (toolFromItemStack == null)
		{
			return;
		}
		object obj;
		if (itemStack == null)
		{
			obj = null;
		}
		else
		{
			JObject metadata = itemStack.Metadata;
			obj = ((metadata != null) ? metadata["ToolData"] : null);
		}
		if (obj != null)
		{
			_extrudeDepth = int.Parse(toolFromItemStack.GetItemArgValueOrDefault(ref itemStack, "ExtrudeDepth"));
			_extrudeRadius = int.Parse(toolFromItemStack.GetItemArgValueOrDefault(ref itemStack, "ExtrudeRadius"));
			if (_hasTarget)
			{
				UpdateBlockShapeModelData(_extrudeRadius);
			}
		}
	}

	private void UpdateBlockShapeModelData(int range)
	{
		Vector3 vector = new Vector3(range * 2, range * 2, range * 2);
		Vector3 vector2 = new Vector3(range * 2, range * 2, range * 2);
		Vector3 vector3 = new Vector3((float)(-range) - 0.5f, (float)(-range) - 0.5f, (float)(-range) - 0.5f);
		int num = System.Math.Abs(_extrudeDepth);
		if (_normal.X != 0f)
		{
			vector2.X = num;
			vector.X = 1f;
			if (_normal.X > 0f)
			{
				vector3.X = 0.5f;
			}
			else
			{
				vector3.X = (float)_extrudeDepth * _normal.X - 0.5f;
			}
		}
		else if (_normal.Y != 0f)
		{
			vector2.Y = num;
			vector.Y = 1f;
			if (_normal.Y > 0f)
			{
				vector3.Y = 0.5f;
			}
			else
			{
				vector3.Y = (float)_extrudeDepth * _normal.Y - 0.5f;
			}
		}
		else if (_normal.Z != 0f)
		{
			vector2.Z = num;
			vector.Z = 1f;
			if (_normal.Z > 0f)
			{
				vector3.Z = 0.5f;
			}
			else
			{
				vector3.Z = (float)_extrudeDepth * _normal.Z - 0.5f;
			}
		}
		bool[,,] array = new bool[(int)vector2.X, (int)vector2.Y, (int)vector2.Z];
		bool[,,] blockTestedPositionData = new bool[(int)vector.X, (int)vector.Y, (int)vector.Z];
		Vector3 normal = _normal;
		Vector3 vector4 = _target + normal;
		Vector3 testDirection = Vector3.Negate(normal);
		FindAllBlocksForFace(array, blockTestedPositionData, vector4, vector4, testDirection);
		normal.X -= (float)array.GetLength(0) / 2f + 0.5f;
		normal.Y -= (float)array.GetLength(1) / 2f + 0.5f;
		normal.Z -= (float)array.GetLength(2) / 2f + 0.5f;
		IntVector3 intVector = new IntVector3(vector3 + Vector3.One * 0.5f);
		_renderer.UpdateModelData(array, intVector.X, intVector.Y, intVector.Z);
	}

	private void FindAllBlocksForFace(bool[,,] blockPositionData, bool[,,] blockTestedPositionData, Vector3 startWorldPosition, Vector3 currentWorldPosition, Vector3 testDirection)
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Invalid comparison between Unknown and I4
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Invalid comparison between Unknown and I4
		Vector3 vector = currentWorldPosition - startWorldPosition;
		int length = blockTestedPositionData.GetLength(0);
		int length2 = blockTestedPositionData.GetLength(1);
		int length3 = blockTestedPositionData.GetLength(2);
		int num = length / 2 + (int)vector.X;
		int num2 = length2 / 2 + (int)vector.Y;
		int num3 = length3 / 2 + (int)vector.Z;
		if (num < 0 || num >= length || num2 < 0 || num2 >= length2 || num3 < 0 || num3 >= length3 || blockTestedPositionData[num, num2, num3])
		{
			return;
		}
		blockTestedPositionData[num, num2, num3] = true;
		int block = _gameInstance.MapModule.GetBlock(currentWorldPosition, int.MaxValue);
		if (block == int.MaxValue)
		{
			return;
		}
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if ((int)_gameInstance.MapModule.ClientBlockTypes[block].CollisionMaterial == 1)
		{
			return;
		}
		int block2 = _gameInstance.MapModule.GetBlock(currentWorldPosition + testDirection, int.MaxValue);
		if (!_gameInstance.MapModule.ClientBlockTypes[block2].IsOccluder && (int)_gameInstance.MapModule.ClientBlockTypes[block2].CollisionMaterial != 1)
		{
			return;
		}
		blockPositionData[num, num2, num3] = true;
		if (testDirection.X != 0f && blockPositionData.GetLength(0) > 1)
		{
			for (int i = 1; i < blockPositionData.GetLength(0); i++)
			{
				blockPositionData[num + i, num2, num3] = true;
			}
		}
		else if (testDirection.Y != 0f && blockPositionData.GetLength(1) > 1)
		{
			for (int j = 1; j < blockPositionData.GetLength(1); j++)
			{
				blockPositionData[num, num2 + j, num3] = true;
			}
		}
		else if (testDirection.Z != 0f && blockPositionData.GetLength(2) > 1)
		{
			for (int k = 1; k < blockPositionData.GetLength(2); k++)
			{
				blockPositionData[num, num2, num3 + k] = true;
			}
		}
		for (int l = 0; l < 6; l++)
		{
			Vector3 normal = ChunkGeometryBuilder.AdjacentBlockOffsetsBySide[l].Normal;
			if ((testDirection.X == 0f || normal.X == 0f) && (testDirection.Y == 0f || normal.Y == 0f) && (testDirection.Z == 0f || normal.Z == 0f))
			{
				Vector3 currentWorldPosition2 = currentWorldPosition + normal;
				FindAllBlocksForFace(blockPositionData, blockTestedPositionData, startWorldPosition, currentWorldPosition2, testDirection);
			}
		}
	}
}
