using System;
using System.Diagnostics;
using Hypixel.ProtoPlus;
using HytaleClient.Data.Entities;
using HytaleClient.Data.Entities.Initializers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules;

internal class NetworkModule : Module
{
	private const float MaxRelativePositionDelta = 3.2767f;

	public const float RelativePositionScale = 10000f;

	private const long OccasionalAbsolutePositionDelay = 2000L;

	private readonly Stopwatch _lastAbsolutePosition = Stopwatch.StartNew();

	private Vector3 _lastSentPosition;

	private Vector3 _lastSentBodyOrientation;

	private Vector3 _lastSentLookOrientation;

	private ClientMovementStates _lastSentMovementStates;

	public NetworkModule(GameInstance gameInstance)
		: base(gameInstance)
	{
	}

	[Obsolete]
	public override void Tick()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Expected O, but got Unknown
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		ref ClientMovementStates movementStates = ref _gameInstance.CharacterControllerModule.MovementController.MovementStates;
		bool flag = localPlayer.Position != _lastSentPosition;
		bool flag2 = localPlayer.BodyOrientation != _lastSentBodyOrientation;
		bool flag3 = localPlayer.LookOrientation != _lastSentLookOrientation;
		bool flag4 = movementStates != _lastSentMovementStates;
		if (!flag && !flag2 && !flag3 && !flag4)
		{
			return;
		}
		ClientMovement val = new ClientMovement();
		if (flag)
		{
			if (false && _lastAbsolutePosition.ElapsedMilliseconds < 2000)
			{
				Vector3 vector = localPlayer.Position - _lastSentPosition;
				val.RelativePosition = new HalfFloatPosition((short)(vector.X * 10000f), (short)(vector.Y * 10000f), (short)(vector.Z * 10000f));
			}
			else
			{
				val.AbsolutePosition = localPlayer.Position.ToPositionPacket();
				_lastAbsolutePosition.Restart();
			}
		}
		if (flag2)
		{
			val.BodyOrientation = localPlayer.BodyOrientation.ToDirectionPacket();
		}
		if (flag3)
		{
			val.LookOrientation = localPlayer.LookOrientation.ToDirectionPacket();
		}
		if (flag4)
		{
			val.MovementStates_ = ClientMovementStatesProtocolHelper.ToPacket(ref movementStates);
		}
		if (_gameInstance.CharacterControllerModule.MovementController.RunningKnockbackRemainingTime > 0f)
		{
			val.WishMovement = _gameInstance.CharacterControllerModule.MovementController.LastMoveForce.ToPositionPacket();
		}
		val.Velocity = new Vector3d((double)_gameInstance.CharacterControllerModule.MovementController.PreviousMovementOffset.X, (double)_gameInstance.CharacterControllerModule.MovementController.PreviousMovementOffset.Y, (double)_gameInstance.CharacterControllerModule.MovementController.PreviousMovementOffset.Z);
		_gameInstance.Connection.SendPacket((ProtoPacket)(object)val);
		_lastSentPosition = localPlayer.Position;
		_lastSentBodyOrientation = localPlayer.BodyOrientation;
		_lastSentLookOrientation = localPlayer.LookOrientation;
		_lastSentMovementStates = movementStates;
	}
}
