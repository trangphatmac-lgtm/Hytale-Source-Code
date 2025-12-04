using System.Collections.Generic;
using Hypixel.ProtoPlus;
using HytaleClient.AssetEditor.Interface.Config;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class HitboxTool : ClientTool
{
	private readonly BoxEditorGizmo _boxEditorGizmo;

	private readonly BoxRenderer _boxRenderer;

	private readonly Vector3[] _boxColors;

	private ToolState _state = ToolState.None;

	private int _blockIndex;

	private Vector3 _blockPosition;

	private BlockHitbox _hitbox;

	private int _hitboxType;

	private int _boxId;

	private readonly BoundingBox VoxelBox = new BoundingBox(Vector3.Zero, Vector3.One);

	public override string ToolId => "Hitbox";

	public HitboxTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		_boxEditorGizmo = new BoxEditorGizmo(_gameInstance.Engine.Graphics, delegate(BoundingBox box)
		{
			_hitbox.Boxes[_boxId] = box;
		});
		_boxRenderer = new BoxRenderer(_gameInstance.Engine.Graphics, _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram);
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		_boxColors = new Vector3[8] { graphics.WhiteColor, graphics.RedColor, graphics.GreenColor, graphics.BlueColor, graphics.CyanColor, graphics.YellowColor, graphics.MagentaColor, graphics.BlackColor };
	}

	protected override void DoDispose()
	{
		_boxRenderer.Dispose();
		_boxEditorGizmo.Dispose();
	}

	public override bool NeedsDrawing()
	{
		return _state != ToolState.None;
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		if (_state == ToolState.None)
		{
			return;
		}
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		GLFunctions gL = graphics.GL;
		Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
		gL.DepthFunc(GL.ALWAYS);
		BlockHitbox blockHitbox;
		if (_state == ToolState.Hover)
		{
			int block = _gameInstance.MapModule.GetBlock(_blockPosition, int.MaxValue);
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
			blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
		}
		else
		{
			blockHitbox = _hitbox;
		}
		HitDetection.RaycastHit targetBlockHit = _gameInstance.InteractionModule.TargetBlockHit;
		Vector3 color = Vector3.One;
		for (int i = 0; i < blockHitbox.Boxes.Length; i++)
		{
			Vector3 vector = ((_state == ToolState.Hover) ? _boxColors[0] : _boxColors[i % _boxColors.Length]);
			if (_boxId == i)
			{
				color = vector;
			}
			float num = 0.1f;
			if (_state != ToolState.Editing || _boxId != i)
			{
				if (targetBlockHit.BoxId == i || (_state == ToolState.Editing && _boxId == i))
				{
					num = 0.3f;
				}
				if (_state == ToolState.Hover || (_state == ToolState.Selected && _blockPosition != targetBlockHit.BlockOrigin))
				{
					num = 0.1f;
				}
				else if (_state == ToolState.Editing && _boxId != i)
				{
					num = 0.05f;
				}
				_boxRenderer.Draw(_blockPosition - cameraPosition, blockHitbox.Boxes[i], viewProjectionMatrix, vector, num * 3f, vector, num);
			}
		}
		if (_state == ToolState.Editing || _state == ToolState.Hover)
		{
			_boxEditorGizmo.Draw(ref viewProjectionMatrix, -cameraPosition, color);
		}
		gL.DepthFunc((!graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}

	public override void Update(float deltaTime)
	{
		switch (_state)
		{
		case ToolState.Editing:
			_boxEditorGizmo.Tick(_gameInstance.CameraModule.GetLookRay(), _gameInstance.Input.IsAltHeld());
			break;
		case ToolState.Selected:
		{
			if (_gameInstance.Input.ConsumeKey((SDL_Scancode)73))
			{
				BoundingBox[] array = new BoundingBox[_hitbox.Boxes.Length + 1];
				for (int i = 0; i < _hitbox.Boxes.Length; i++)
				{
					array[i] = _hitbox.Boxes[i];
				}
				array[^1] = VoxelBox;
				_hitbox = new BlockHitbox(array);
				OnBoxUpdate();
				_gameInstance.Chat.Log("Added new bounding box to the block hitbox.");
			}
			if (!_gameInstance.Input.ConsumeKey((SDL_Scancode)76) || !_gameInstance.InteractionModule.HasFoundTargetBlock || !(_gameInstance.InteractionModule.TargetBlockHit.BlockOrigin == _blockPosition))
			{
				break;
			}
			if (_hitbox.Boxes.Length < 2)
			{
				_gameInstance.Chat.Log("Unable to remove box, at least one must be present in the hitbox.");
				break;
			}
			BoundingBox[] array2 = new BoundingBox[_hitbox.Boxes.Length - 1];
			int num = 0;
			for (int j = 0; j < _hitbox.Boxes.Length; j++)
			{
				if (j != _gameInstance.InteractionModule.TargetBlockHit.BoxId)
				{
					array2[num] = _hitbox.Boxes[j];
					num++;
				}
			}
			_hitbox = new BlockHitbox(array2);
			OnBoxUpdate();
			_gameInstance.Chat.Log("Bounding box removed.");
			break;
		}
		case ToolState.None:
		case ToolState.Hover:
			if (_gameInstance.InteractionModule.HasFoundTargetBlock)
			{
				_blockPosition = _gameInstance.InteractionModule.TargetBlockHit.BlockOrigin;
				_state = ToolState.Hover;
			}
			else
			{
				_state = ToolState.None;
			}
			break;
		}
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Invalid comparison between Unknown and I4
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Invalid comparison between Unknown and I4
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		if (clickType == InteractionModule.ClickType.None)
		{
			return;
		}
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		switch (_state)
		{
		case ToolState.Hover:
			if ((int)interactionType == 0)
			{
				int block = _gameInstance.MapModule.GetBlock(_blockPosition, int.MaxValue);
				if (block != int.MaxValue && block != 1)
				{
					ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
					ClientBlockType clientBlockTypeFromName = _gameInstance.MapModule.GetClientBlockTypeFromName(ClientBlockType.GetOriginalBlockName(clientBlockType.Name));
					string value = null;
					ClientBlockType.GetBlockVariantData(clientBlockType.Name)?.TryGetValue("State", out value);
					_hitboxType = clientBlockType.HitboxType;
					_hitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType].Clone();
					_state = ToolState.Selected;
					string message = "Block: " + clientBlockTypeFromName.Name + ((clientBlockTypeFromName.Variants.Count == 0) ? "\n" : $" - {clientBlockTypeFromName.Variants.Count} Variants\n") + ((value == null) ? "" : ("State: " + value + "\n")) + $"Hitbox: {clientBlockTypeFromName.HitboxType} - Used by {GetHitboxUsageCount(block)} other blocks";
					_gameInstance.Chat.Log(message);
				}
			}
			else
			{
				_state = ToolState.None;
			}
			break;
		case ToolState.Selected:
			if ((int)interactionType == 0)
			{
				if (_gameInstance.InteractionModule.HasFoundTargetBlock && !(_blockPosition != _gameInstance.InteractionModule.TargetBlockHit.BlockOrigin))
				{
					_boxId = _gameInstance.InteractionModule.TargetBlockHit.BoxId;
					Vector3[] hitboxSnapValues = GetHitboxSnapValues(_hitbox, _boxId);
					_boxEditorGizmo.Show(_blockPosition, _hitbox.Boxes[_boxId], hitboxSnapValues);
					_boxEditorGizmo.Tick(lookRay, _gameInstance.Input.IsAltHeld());
					_boxEditorGizmo.OnInteract(interactionType, lookRay, _gameInstance.Input.IsShiftHeld(), _gameInstance.Input.IsAltHeld());
					_state = ToolState.Editing;
				}
			}
			else
			{
				_state = ToolState.None;
			}
			break;
		case ToolState.Editing:
			if ((int)interactionType == 0)
			{
				_boxEditorGizmo.OnInteract(interactionType, lookRay, _gameInstance.Input.IsShiftHeld(), _gameInstance.Input.IsAltHeld());
				OnBoxUpdate();
			}
			else
			{
				_boxEditorGizmo.ResetBox();
			}
			_boxEditorGizmo.Hide();
			_state = ToolState.Selected;
			break;
		}
	}

	private void OnBoxUpdate()
	{
		BlockHitbox blockHitbox = _hitbox.Clone();
		if (!_gameInstance.ServerSettings.BlockHitboxes[_hitboxType].Equals(blockHitbox))
		{
			int block = _gameInstance.MapModule.GetBlock(_blockPosition, int.MaxValue);
			UpdateHitboxAsset(block, blockHitbox);
			_gameInstance.Chat.Log("Block hitbox updated!");
		}
	}

	private void UpdateHitboxAsset(int blockId, BlockHitbox hitbox)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_00f8: Expected O, but got Unknown
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[blockId];
		string text = clientBlockType.Name;
		if ((int)clientBlockType.RotationPitch != 0 || (int)clientBlockType.RotationYaw > 0)
		{
			RotateHitBox(clientBlockType, ref hitbox);
			text = ClientBlockType.GetOriginalBlockName(clientBlockType.Name);
			clientBlockType = _gameInstance.MapModule.GetClientBlockTypeFromName(text);
		}
		int hitboxType = clientBlockType.HitboxType;
		if (hitboxType == 0 || hitboxType == int.MinValue)
		{
			_gameInstance.Chat.Error("Hitbox for block \"" + text + "\" can't be edited!");
			return;
		}
		JArray val = JArray.FromObject((object)hitbox.Boxes);
		JsonUpdateCommand[] array = new JsonUpdateCommand[1];
		JsonUpdateCommand val2 = new JsonUpdateCommand
		{
			Type = (JsonUpdateType)0,
			Path = PropertyPath.FromString("Boxes").Elements
		};
		JObject val3 = new JObject();
		val3.Add("value", (JToken)(object)val);
		val2.Value = (sbyte[])(object)ProtoHelper.SerializeBson(val3);
		array[0] = val2;
		JsonUpdateCommand[] commands = (JsonUpdateCommand[])(object)array;
		_gameInstance.Connection.SendPacket((ProtoPacket)new AssetEditorUpdateJsonAsset
		{
			Token = 0,
			AssetType = "BlockBoundingBoxes",
			AssetIndex = hitboxType,
			Commands = commands
		});
	}

	private int GetHitboxUsageCount(int blockId)
	{
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[blockId];
		string originalBlockName = ClientBlockType.GetOriginalBlockName(clientBlockType.Name);
		ClientBlockType clientBlockTypeFromName = _gameInstance.MapModule.GetClientBlockTypeFromName(originalBlockName);
		int num = 0;
		ClientBlockType[] clientBlockTypes = _gameInstance.MapModule.ClientBlockTypes;
		foreach (ClientBlockType clientBlockType2 in clientBlockTypes)
		{
			if (clientBlockType2?.HitboxType == clientBlockTypeFromName.HitboxType && !clientBlockType2.Name.Contains('|'.ToString()) && clientBlockType2.Name != clientBlockTypeFromName.Name)
			{
				num++;
			}
		}
		return num;
	}

	private static Vector3[] GetHitboxSnapValues(BlockHitbox hitbox, int excludedBox)
	{
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < hitbox.Boxes.Length; i++)
		{
			if (i != excludedBox)
			{
				list.Add(hitbox.Boxes[i].Min);
				list.Add(hitbox.Boxes[i].Max);
			}
		}
		return list.ToArray();
	}

	private static void RotateHitBox(ClientBlockType blockType, ref BlockHitbox hitbox)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected I4, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected I4, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		Rotation rotation = (Rotation)0;
		Rotation rotationYaw = blockType.RotationYaw;
		Rotation val = rotationYaw;
		switch ((int)val)
		{
		case 0:
			rotation = (Rotation)0;
			break;
		case 1:
			rotation = (Rotation)3;
			break;
		case 2:
			rotation = (Rotation)2;
			break;
		case 3:
			rotation = (Rotation)1;
			break;
		}
		Rotation rotation2 = (Rotation)0;
		Rotation rotationPitch = blockType.RotationPitch;
		Rotation val2 = rotationPitch;
		switch ((int)val2)
		{
		case 0:
			rotation2 = (Rotation)0;
			break;
		case 1:
			rotation2 = (Rotation)3;
			break;
		case 2:
			rotation2 = (Rotation)2;
			break;
		case 3:
			rotation2 = (Rotation)1;
			break;
		}
		hitbox.Rotate(MathHelper.RotationToDegrees(rotation2), MathHelper.RotationToDegrees(rotation));
	}
}
