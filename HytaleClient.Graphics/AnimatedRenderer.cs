#define DEBUG
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HytaleClient.Core;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Items;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Graphics;

internal abstract class AnimatedRenderer : Disposable
{
	public class AnimationSlot
	{
		public float SpeedMultiplier;

		private float _blendingTime;

		private float _blendingDuration;

		public BlockyAnimation Animation { get; private set; }

		public bool IsLooping { get; private set; }

		public ClientItemPullbackConfig PullbackConfig { get; private set; }

		public float AnimationTime { get; private set; }

		public float BlendingProgress => Easing.CubicEaseInAndOut(_blendingTime / _blendingDuration);

		public bool IsBlending => _blendingDuration != 0f;

		public void Copy(AnimationSlot targetAnimationSlot)
		{
			Animation = targetAnimationSlot.Animation;
			IsLooping = targetAnimationSlot.IsLooping;
			SpeedMultiplier = targetAnimationSlot.SpeedMultiplier;
			AnimationTime = targetAnimationSlot.AnimationTime;
			_blendingTime = targetAnimationSlot._blendingTime;
			_blendingDuration = targetAnimationSlot._blendingDuration;
		}

		public void SetAnimation(BlockyAnimation animation, bool isLooping = true, float speedMultiplier = 1f, float startTime = 0f, float blendingDuration = 0f, ClientItemPullbackConfig pullbackConfig = null)
		{
			Animation = animation;
			IsLooping = isLooping;
			SpeedMultiplier = speedMultiplier;
			PullbackConfig = pullbackConfig;
			if (Animation != null)
			{
				if (!isLooping && startTime >= (float)Animation.Duration)
				{
					AnimationTime = Animation.Duration;
				}
				else
				{
					AnimationTime = startTime % (float)Animation.Duration;
				}
			}
			_blendingDuration = blendingDuration;
			_blendingTime = 0f;
		}

		public void SetAnimationNoBlending(BlockyAnimation animation, bool isLooping = true, float speedMultiplier = 1f, float startTime = 0f)
		{
			Animation = animation;
			IsLooping = isLooping;
			SpeedMultiplier = speedMultiplier;
			if (Animation != null)
			{
				if (!isLooping && startTime >= (float)Animation.Duration)
				{
					AnimationTime = Animation.Duration;
				}
				else
				{
					AnimationTime = startTime % (float)Animation.Duration;
				}
			}
			_blendingDuration = 0f;
		}

		public void SetPullback(ClientItemPullbackConfig pullbackConfig)
		{
			PullbackConfig = pullbackConfig;
		}

		public void AdvancePlayback(float elapsedTime, out bool finishedBlending)
		{
			finishedBlending = false;
			if (IsBlending)
			{
				_blendingTime += elapsedTime;
				if (_blendingTime >= _blendingDuration)
				{
					finishedBlending = true;
					_blendingDuration = 0f;
				}
			}
			if (Animation == null)
			{
				return;
			}
			AnimationTime += elapsedTime * SpeedMultiplier;
			if (Animation != null && AnimationTime >= (float)Animation.Duration)
			{
				if (IsLooping)
				{
					AnimationTime %= Animation.Duration;
				}
				else
				{
					AnimationTime = Animation.Duration;
				}
			}
		}
	}

	public struct NodeTransform
	{
		public Vector3 Position;

		public Quaternion Orientation;
	}

	private struct LastFrame
	{
		public Vector3 Position;

		public Quaternion Orientation;

		public Vector3 ShapeStretch;

		public bool Visible;

		public Point UvOffset;
	}

	public enum CameraControlNode
	{
		None,
		LookYaw,
		LookPitch,
		Look
	}

	public uint NodeBufferOffset;

	private readonly ushort _nodeCount;

	private bool _selfManageNodeBuffer;

	private readonly BlockyModel _model;

	private readonly Point[] _atlasSizes;

	protected GraphicsDevice _graphics;

	private const int MaxAnimationSlots = 9;

	private AnimationSlot[] _animationSlots = new AnimationSlot[9];

	public readonly Matrix[] NodeMatrices;

	public readonly NodeTransform[] NodeTransforms;

	private readonly GCHandle _nodeMatricesHandle;

	protected readonly IntPtr _nodeMatricesAddr;

	private readonly NodeTransform[] _nodeLocalParentTransforms;

	private readonly BlockyAnimation.BlockyAnimNodeAnim[] _targetNodeAnimsPerSlot;

	private LastFrame[] _lastFramesBeforeBlending;

	private BitArray _hasLastFramesBeforeBlending;

	private readonly int[] _highestActiveSlotByNodes;

	private bool _areAnimatedNodeSlotsDirty;

	private Vector3 _animShapeStretch;

	private Point _animUvOffset;

	private bool _animVisible;

	private Vector3 _targetAnimPosition;

	private Quaternion _targetAnimOrientation;

	private Vector3 _targetAnimShapeStretch;

	private Quaternion _cameraOrientation = Quaternion.Identity;

	private CameraControlNode[] _cameraNodes = new CameraControlNode[Enum.GetNames(typeof(CameraNode)).Length];

	public bool SelfManagedNodeBuffer => _selfManageNodeBuffer;

	public BlockyModel Model => _model;

	public GLBuffer NodeBuffer { get; private set; }

	public ushort NodeCount => _nodeCount;

	public AnimatedRenderer(BlockyModel model, Point[] atlasSizes, bool selfManageNodeBuffer)
	{
		_model = model;
		_nodeCount = (ushort)_model.NodeCount;
		_atlasSizes = atlasSizes;
		_selfManageNodeBuffer = selfManageNodeBuffer;
		NodeMatrices = new Matrix[_nodeCount];
		_nodeMatricesHandle = GCHandle.Alloc(NodeMatrices, GCHandleType.Pinned);
		_nodeMatricesAddr = _nodeMatricesHandle.AddrOfPinnedObject();
		NodeTransforms = new NodeTransform[_nodeCount];
		_nodeLocalParentTransforms = new NodeTransform[_nodeCount];
		_targetNodeAnimsPerSlot = new BlockyAnimation.BlockyAnimNodeAnim[9 * _nodeCount];
		_lastFramesBeforeBlending = new LastFrame[9 * _nodeCount];
		_hasLastFramesBeforeBlending = new BitArray(9 * _nodeCount);
		_highestActiveSlotByNodes = new int[_nodeCount];
		for (int i = 0; i < 9; i++)
		{
			_animationSlots[i] = new AnimationSlot();
		}
		SetupAnimatedNodeSlots();
	}

	public virtual void CreateGPUData(GraphicsDevice graphics)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_graphics = graphics;
		if (_selfManageNodeBuffer)
		{
			GLFunctions gL = graphics.GL;
			NodeBuffer = gL.GenBuffer();
		}
	}

	protected override void DoDispose()
	{
		_nodeMatricesHandle.Free();
		if (_graphics != null && _selfManageNodeBuffer)
		{
			GLFunctions gL = _graphics.GL;
			gL.DeleteBuffer(NodeBuffer);
		}
	}

	public void AdvancePlayback(float elapsedTime)
	{
		for (int i = 0; i < 9; i++)
		{
			_animationSlots[i].AdvancePlayback(elapsedTime, out var finishedBlending);
			if (finishedBlending)
			{
				int num = i * _nodeCount;
				for (int j = 0; j < _nodeCount; j++)
				{
					_hasLastFramesBeforeBlending[num + j] = false;
				}
				_areAnimatedNodeSlotsDirty = true;
			}
		}
	}

	public void CopyAllSlotAnimations(AnimatedRenderer targetModelRenderer)
	{
		for (int i = 0; i < 9; i++)
		{
			_animationSlots[i].Copy(targetModelRenderer._animationSlots[i]);
		}
		for (int j = 0; j < _nodeCount; j++)
		{
			int nameId = _model.AllNodes[j].NameId;
			if (targetModelRenderer._model.NodeIndicesByNameId.TryGetValue(nameId, out var value))
			{
				for (int k = 0; k < 9; k++)
				{
					int num = k * _nodeCount + j;
					int num2 = k * targetModelRenderer._nodeCount + value;
					_hasLastFramesBeforeBlending[j] = targetModelRenderer._hasLastFramesBeforeBlending[num2];
					_lastFramesBeforeBlending[j] = targetModelRenderer._lastFramesBeforeBlending[num2];
				}
			}
		}
		_cameraOrientation = targetModelRenderer._cameraOrientation;
		_areAnimatedNodeSlotsDirty = true;
	}

	public void SetSlotAnimation(int slotIndex, BlockyAnimation animation, bool isLooping = true, float speedMultiplier = 1f, float startTime = 0f, float blendingDuration = 0f, ClientItemPullbackConfig pullbackConfig = null, bool force = false)
	{
		if (_animationSlots[slotIndex].Animation == animation && !force)
		{
			_animationSlots[slotIndex].SetPullback(pullbackConfig);
			return;
		}
		if (_areAnimatedNodeSlotsDirty)
		{
			SetupAnimatedNodeSlots();
		}
		BlockyAnimation animation2 = _animationSlots[slotIndex].Animation;
		for (int i = 0; i < _nodeCount; i++)
		{
			BlockyModelNode node = _model.AllNodes[i];
			int num = slotIndex * _nodeCount + i;
			if ((animation2 != null && animation2.NodeAnimationsByNameId.ContainsKey(node.NameId)) || _hasLastFramesBeforeBlending[num])
			{
				int num2 = _model.ParentNodes[i];
				ref NodeTransform reference = ref NodeTransforms[i];
				ComputeNodeTransform(ref reference, node, i, slotIndex);
				_hasLastFramesBeforeBlending[num] = true;
				_lastFramesBeforeBlending[num] = new LastFrame
				{
					Position = reference.Position,
					Orientation = reference.Orientation,
					ShapeStretch = _animShapeStretch,
					Visible = _animVisible,
					UvOffset = _animUvOffset
				};
			}
			else
			{
				_hasLastFramesBeforeBlending[num] = false;
			}
		}
		_animationSlots[slotIndex].SetAnimation(animation, isLooping, speedMultiplier, startTime, blendingDuration, pullbackConfig);
		_areAnimatedNodeSlotsDirty = true;
	}

	public void SetSlotAnimationNoBlending(int slotIndex, BlockyAnimation animation, bool isLooping = true, float speedMultiplier = 1f, float startTime = 0f)
	{
		if (_animationSlots[slotIndex].Animation != animation)
		{
			int num = slotIndex * _nodeCount;
			for (int i = 0; i < _nodeCount; i++)
			{
				_hasLastFramesBeforeBlending[num + i] = false;
			}
			_animationSlots[slotIndex].SetAnimationNoBlending(animation, isLooping, speedMultiplier, startTime);
			_areAnimatedNodeSlotsDirty = true;
		}
	}

	public void SetSlotAnimationSpeedMultiplier(int slotIndex, float speedMultiplier)
	{
		_animationSlots[slotIndex].SpeedMultiplier = speedMultiplier;
	}

	public BlockyAnimation GetSlotAnimation(int slot)
	{
		return _animationSlots[slot].Animation;
	}

	public float GetSlotAnimationTime(int slot)
	{
		return _animationSlots[slot].AnimationTime;
	}

	public float GetSlotAnimationSpeedMultiplier(int slot)
	{
		return _animationSlots[slot].SpeedMultiplier;
	}

	public ClientItemPullbackConfig GetSlotPullbackConfig(int slot)
	{
		return _animationSlots[slot].PullbackConfig;
	}

	public bool IsSlotPlayingAnimation(int slotIndex)
	{
		AnimationSlot animationSlot = _animationSlots[slotIndex];
		if (animationSlot.Animation == null)
		{
			return false;
		}
		if (animationSlot.IsLooping)
		{
			return true;
		}
		return animationSlot.AnimationTime < (float)animationSlot.Animation.Duration;
	}

	private void SetupAnimatedNodeSlots()
	{
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Invalid comparison between Unknown and I4
		for (int i = 0; i < _nodeCount; i++)
		{
			ref BlockyModelNode reference = ref _model.AllNodes[i];
			bool flag = false;
			_highestActiveSlotByNodes[i] = -1;
			if (!reference.IsPiece)
			{
				for (int num = 8; num >= 0; num--)
				{
					bool flag2 = false;
					AnimationSlot animationSlot = _animationSlots[num];
					int num2 = num * _nodeCount + i;
					if (animationSlot.IsBlending && _hasLastFramesBeforeBlending[num2])
					{
						flag = true;
						flag2 = true;
					}
					if (animationSlot.Animation != null && animationSlot.Animation.NodeAnimationsByNameId.TryGetValue(reference.NameId, out _targetNodeAnimsPerSlot[num2]))
					{
						flag = true;
						flag2 = true;
					}
					else
					{
						_targetNodeAnimsPerSlot[num2] = null;
					}
					if (flag2 && _highestActiveSlotByNodes[i] == -1)
					{
						_highestActiveSlotByNodes[i] = num;
					}
				}
			}
			if (!flag)
			{
				if ((int)reference.CameraNode > 0)
				{
					_highestActiveSlotByNodes[i] = 0;
					continue;
				}
				_nodeLocalParentTransforms[i].Position = reference.Position + Vector3.Transform(reference.Offset, reference.Orientation) + Vector3.Transform(reference.ProceduralOffset, Quaternion.Identity);
				Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(reference.ProceduralRotation.Yaw, reference.ProceduralRotation.Pitch, reference.ProceduralRotation.Roll);
				_nodeLocalParentTransforms[i].Orientation = quaternion * reference.Orientation;
			}
		}
	}

	public void SetCameraOrientation(Quaternion orientation)
	{
		_cameraOrientation = orientation;
	}

	public void SetCameraNodes(CameraSettings cameraSettings)
	{
		for (int i = 0; i < _cameraNodes.Length; i++)
		{
			_cameraNodes[i] = CameraControlNode.None;
		}
		int num = 0;
		while (true)
		{
			int num2 = num;
			CameraAxis yaw = cameraSettings.Yaw;
			if (num2 >= ((yaw != null) ? yaw.TargetNodes.Length : 0))
			{
				break;
			}
			_cameraNodes[(int)cameraSettings.Yaw.TargetNodes[num]] = CameraControlNode.LookYaw;
			num++;
		}
		int num3 = 0;
		while (true)
		{
			int num4 = num3;
			CameraAxis pitch = cameraSettings.Pitch;
			if (num4 < ((pitch != null) ? pitch.TargetNodes.Length : 0))
			{
				if (_cameraNodes[(int)cameraSettings.Pitch.TargetNodes[num3]] == CameraControlNode.LookYaw)
				{
					_cameraNodes[(int)cameraSettings.Pitch.TargetNodes[num3]] = CameraControlNode.Look;
				}
				else
				{
					_cameraNodes[(int)cameraSettings.Pitch.TargetNodes[num3]] = CameraControlNode.LookPitch;
				}
				num3++;
				continue;
			}
			break;
		}
	}

	public void UpdatePose()
	{
		if (_areAnimatedNodeSlotsDirty)
		{
			SetupAnimatedNodeSlots();
			_areAnimatedNodeSlotsDirty = false;
		}
		for (int i = 0; i < _nodeCount; i++)
		{
			UpdatePoseForNode(ref _model.AllNodes[i], i);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SendDataToGPU()
	{
		Debug.Assert(_selfManageNodeBuffer, "Error: trying to send Node data to a GPU buffer when _selfManageNodeBuffer is false.");
		GLFunctions gL = _graphics.GL;
		gL.BindBuffer(GL.UNIFORM_BUFFER, NodeBuffer);
		gL.BufferData(GL.UNIFORM_BUFFER, (IntPtr)(_nodeCount * 64), _nodeMatricesAddr, GL.DYNAMIC_DRAW);
	}

	private void UpdatePoseForNode(ref BlockyModelNode node, int nodeIndex)
	{
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Invalid comparison between Unknown and I4
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Expected I4, but got Unknown
		int num = _model.ParentNodes[nodeIndex];
		ref NodeTransform reference = ref NodeTransforms[nodeIndex];
		if (_highestActiveSlotByNodes[nodeIndex] == -1)
		{
			if (num == -1)
			{
				reference.Position = _nodeLocalParentTransforms[nodeIndex].Position;
				reference.Orientation = _nodeLocalParentTransforms[nodeIndex].Orientation;
			}
			else
			{
				reference.Position = Vector3.Transform(_nodeLocalParentTransforms[nodeIndex].Position, NodeTransforms[num].Orientation) + NodeTransforms[num].Position;
				reference.Orientation = NodeTransforms[num].Orientation * _nodeLocalParentTransforms[nodeIndex].Orientation;
			}
			if (node.Visible)
			{
				Matrix.Compose(reference.Orientation, reference.Position, out NodeMatrices[nodeIndex]);
			}
			else
			{
				Matrix.CreateScale(0f, out NodeMatrices[nodeIndex]);
			}
			return;
		}
		int slotIndex = _highestActiveSlotByNodes[nodeIndex];
		ComputeNodeTransform(ref reference, node, nodeIndex, slotIndex);
		if (!node.IsPiece && (int)node.CameraNode > 0)
		{
			int num2 = (int)node.CameraNode;
			if (_cameraNodes[num2] == CameraControlNode.Look)
			{
				Quaternion.Multiply(ref reference.Orientation, ref _cameraOrientation, out reference.Orientation);
			}
			else if (_cameraNodes[num2] == CameraControlNode.LookYaw)
			{
				Vector3 planeNormal = Vector3.Transform(Vector3.Up, reference.Orientation);
				Vector3 vector = Vector3.Transform(Vector3.Forward, _cameraOrientation);
				Vector3 destination = Vector3.ProjectOnPlane(vector, planeNormal);
				Vector3 source = Vector3.Forward;
				Quaternion.CreateFromNormalizedVectors(ref source, ref destination, out var result);
				reference.Orientation = result * reference.Orientation;
			}
			else if (_cameraNodes[num2] == CameraControlNode.LookPitch)
			{
				Vector3 planeNormal2 = Vector3.Transform(Vector3.Right, reference.Orientation);
				Vector3 vector2 = Vector3.Transform(Vector3.Up, _cameraOrientation);
				Vector3 destination2 = Vector3.ProjectOnPlane(vector2, planeNormal2);
				Vector3 source2 = Vector3.Up;
				Quaternion.CreateFromNormalizedVectors(ref source2, ref destination2, out var result2);
				reference.Orientation = result2 * reference.Orientation;
			}
		}
		reference.Position += Vector3.Transform(node.Offset, reference.Orientation);
		reference.Position += Vector3.Transform(node.ProceduralOffset, Quaternion.Identity);
		if (num != -1)
		{
			reference.Position = Vector3.Transform(reference.Position, NodeTransforms[num].Orientation) + NodeTransforms[num].Position;
			reference.Orientation = NodeTransforms[num].Orientation * reference.Orientation;
		}
		Matrix.Compose(reference.Orientation, reference.Position, out NodeMatrices[nodeIndex]);
		Matrix.ApplyScale(ref NodeMatrices[nodeIndex], _animShapeStretch);
		NodeMatrices[nodeIndex].M14 = (float)_animUvOffset.X / (float)_atlasSizes[node.AtlasIndex].X;
		NodeMatrices[nodeIndex].M24 = (0f - (float)_animUvOffset.Y) / (float)_atlasSizes[node.AtlasIndex].Y;
	}

	private void ComputeNodeTransform(ref NodeTransform refNodeTransform, BlockyModelNode node, int nodeIndex, int slotIndex)
	{
		Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(node.ProceduralRotation.Yaw, node.ProceduralRotation.Pitch, node.ProceduralRotation.Roll);
		refNodeTransform.Position = node.Position;
		refNodeTransform.Orientation = quaternion * node.Orientation;
		_animShapeStretch = Vector3.One;
		_animUvOffset.X = 0;
		_animUvOffset.Y = 0;
		_animVisible = node.Visible;
		AnimationSlot animationSlot = _animationSlots[slotIndex];
		if (animationSlot.IsBlending)
		{
			_targetAnimPosition = refNodeTransform.Position;
			_targetAnimOrientation = refNodeTransform.Orientation;
			_targetAnimShapeStretch = _animShapeStretch;
			int num = -1;
			for (int num2 = slotIndex; num2 >= 0; num2--)
			{
				int num3 = num2 * _nodeCount + nodeIndex;
				if (_hasLastFramesBeforeBlending[num3])
				{
					num = num3;
					break;
				}
			}
			if (num != -1)
			{
				ref LastFrame reference = ref _lastFramesBeforeBlending[num];
				refNodeTransform.Position = reference.Position;
				refNodeTransform.Orientation = reference.Orientation;
				_animShapeStretch = reference.ShapeStretch;
				_animVisible = reference.Visible;
				_animUvOffset = reference.UvOffset;
			}
			else
			{
				BlockyAnimation.BlockyAnimNodeAnim blockyAnimNodeAnim = null;
				float time = 0f;
				for (int num4 = slotIndex - 1; num4 >= 0; num4--)
				{
					blockyAnimNodeAnim = _targetNodeAnimsPerSlot[num4 * _nodeCount + nodeIndex];
					time = _animationSlots[num4].AnimationTime;
					if (blockyAnimNodeAnim != null)
					{
						break;
					}
				}
				if (blockyAnimNodeAnim != null)
				{
					GetInterpolationData(blockyAnimNodeAnim.Frames, time, out var previousFrame, out var nextFrame, out var delta);
					if (blockyAnimNodeAnim.HasPosition)
					{
						refNodeTransform.Position += Vector3.Lerp(previousFrame.Position, nextFrame.Position, delta);
					}
					if (blockyAnimNodeAnim.HasOrientation)
					{
						Quaternion quaternion2 = Quaternion.Lerp(previousFrame.Orientation, nextFrame.Orientation, delta);
						Quaternion.Multiply(ref refNodeTransform.Orientation, ref quaternion2, out refNodeTransform.Orientation);
					}
					if (blockyAnimNodeAnim.HasShapeStretch)
					{
						_animShapeStretch *= Vector3.Lerp(previousFrame.ShapeStretch, nextFrame.ShapeStretch, delta);
					}
					if (blockyAnimNodeAnim.HasShapeVisible)
					{
						_animVisible = previousFrame.ShapeVisible;
					}
					if (blockyAnimNodeAnim.HasShapeUvOffset)
					{
						_animUvOffset = previousFrame.ShapeUvOffset;
					}
				}
			}
			float blendingProgress = animationSlot.BlendingProgress;
			float animationTime = animationSlot.AnimationTime;
			BlockyAnimation.BlockyAnimNodeAnim blockyAnimNodeAnim2 = _targetNodeAnimsPerSlot[slotIndex * _nodeCount + nodeIndex];
			if (blockyAnimNodeAnim2 == null)
			{
				for (int num5 = slotIndex - 1; num5 >= 0; num5--)
				{
					blockyAnimNodeAnim2 = _targetNodeAnimsPerSlot[num5 * _nodeCount + nodeIndex];
					animationTime = _animationSlots[num5].AnimationTime;
					if (blockyAnimNodeAnim2 != null)
					{
						break;
					}
				}
			}
			if (blockyAnimNodeAnim2 != null)
			{
				GetInterpolationData(blockyAnimNodeAnim2.Frames, animationTime, out var previousFrame2, out var nextFrame2, out var delta2);
				if (blockyAnimNodeAnim2.HasPosition)
				{
					_targetAnimPosition += Vector3.Lerp(previousFrame2.Position, nextFrame2.Position, delta2);
				}
				if (blockyAnimNodeAnim2.HasOrientation)
				{
					Quaternion quaternion3 = Quaternion.Lerp(previousFrame2.Orientation, nextFrame2.Orientation, delta2);
					Quaternion.Multiply(ref _targetAnimOrientation, ref quaternion3, out _targetAnimOrientation);
				}
				if (blockyAnimNodeAnim2.HasShapeStretch)
				{
					_targetAnimShapeStretch *= Vector3.Lerp(previousFrame2.ShapeStretch, nextFrame2.ShapeStretch, delta2);
				}
				if (blockyAnimNodeAnim2.HasShapeVisible)
				{
					_animVisible = previousFrame2.ShapeVisible;
				}
				if (blockyAnimNodeAnim2.HasShapeUvOffset)
				{
					_animUvOffset = previousFrame2.ShapeUvOffset;
				}
			}
			Vector3.Lerp(ref refNodeTransform.Position, ref _targetAnimPosition, blendingProgress, out refNodeTransform.Position);
			Quaternion.Lerp(ref refNodeTransform.Orientation, ref _targetAnimOrientation, blendingProgress, out refNodeTransform.Orientation);
			Vector3.Lerp(ref _animShapeStretch, ref _targetAnimShapeStretch, blendingProgress, out _animShapeStretch);
		}
		else
		{
			float animationTime2 = animationSlot.AnimationTime;
			BlockyAnimation.BlockyAnimNodeAnim blockyAnimNodeAnim3 = _targetNodeAnimsPerSlot[slotIndex * _nodeCount + nodeIndex];
			if (blockyAnimNodeAnim3 == null)
			{
				for (int num6 = slotIndex - 1; num6 >= 0; num6--)
				{
					blockyAnimNodeAnim3 = _targetNodeAnimsPerSlot[num6 * _nodeCount + nodeIndex];
					animationTime2 = _animationSlots[num6].AnimationTime;
					if (blockyAnimNodeAnim3 != null)
					{
						break;
					}
				}
			}
			if (blockyAnimNodeAnim3 != null)
			{
				GetInterpolationData(blockyAnimNodeAnim3.Frames, animationTime2, out var previousFrame3, out var nextFrame3, out var delta3);
				if (blockyAnimNodeAnim3.HasPosition)
				{
					refNodeTransform.Position += Vector3.Lerp(previousFrame3.Position, nextFrame3.Position, delta3);
				}
				if (blockyAnimNodeAnim3.HasOrientation)
				{
					Quaternion quaternion4 = Quaternion.Lerp(previousFrame3.Orientation, nextFrame3.Orientation, delta3);
					Quaternion.Multiply(ref refNodeTransform.Orientation, ref quaternion4, out refNodeTransform.Orientation);
				}
				if (blockyAnimNodeAnim3.HasShapeStretch)
				{
					_animShapeStretch *= Vector3.Lerp(previousFrame3.ShapeStretch, nextFrame3.ShapeStretch, delta3);
				}
				if (blockyAnimNodeAnim3.HasShapeVisible)
				{
					_animVisible = previousFrame3.ShapeVisible;
				}
				if (blockyAnimNodeAnim3.HasShapeUvOffset)
				{
					_animUvOffset = previousFrame3.ShapeUvOffset;
				}
			}
		}
		if (!_animVisible)
		{
			_animShapeStretch.X = (_animShapeStretch.Y = (_animShapeStretch.Z = 0f));
		}
	}

	private void GetInterpolationData(BlockyAnimation.BlockyAnimNodeFrame[] frames, float time, out BlockyAnimation.BlockyAnimNodeFrame previousFrame, out BlockyAnimation.BlockyAnimNodeFrame nextFrame, out float delta)
	{
		previousFrame = frames[(int)time];
		nextFrame = frames[((int)time + 1) % frames.Length];
		delta = time - (float)(int)time;
	}
}
