using System;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal class RaycastSelector : SelectorType
{
	private class Runtime : Selector
	{
		private readonly RaycastSelector _selector;

		private Vector3? _debugColor;

		private bool _hasEntity;

		private HitDetection.EntityHitData _entityHitData;

		public Runtime(RaycastSelector selector)
		{
			_selector = selector;
		}

		public void Tick(GameInstance gameInstance, Entity attacker, float time, float runTime)
		{
			Vector3 origin = SelectTargetPosition(attacker);
			Vector3 lookOrientation = attacker.LookOrientation;
			Quaternion rotation = Quaternion.CreateFromYawPitchRoll(lookOrientation.Yaw, lookOrientation.Pitch, 0f);
			Vector3 direction = Vector3.Transform(Vector3.Forward, rotation);
			gameInstance.HitDetection.Raycast(origin, direction, new HitDetection.RaycastOptions
			{
				Distance = _selector.Distance,
				RequiredBlockTag = _selector.BlockTagIndex,
				IgnoreFluids = _selector.IgnoreFluids,
				IgnoreEmptyCollisionMaterial = _selector.IgnoreEmptyCollisionMaterial
			}, out var _, out var _, out _hasEntity, out _entityHitData);
		}

		public void SelectTargetEntities(GameInstance gameInstance, Entity attacker, EntityHitConsumer consumer, Predicate<Entity> filter)
		{
			if (_hasEntity)
			{
				consumer(_entityHitData.Entity, new Vector4(_entityHitData.RayBoxCollision.Position));
			}
		}

		private Vector3 SelectTargetPosition(Entity attacker)
		{
			Vector3 value = attacker.Position;
			Vector3f offset = _selector.Offset;
			if (offset.X != 0f || offset.Y != 0f || offset.Z != 0f)
			{
				value = new Vector3(offset.X, offset.Y, offset.Z);
				Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, attacker.LookOrientation.Yaw);
				Vector3.Transform(ref value, ref rotation, out value);
				value += attacker.Position;
			}
			return value;
		}
	}

	private const int MaxDistance = 30;

	private RaycastSelector _selector;

	public RaycastSelector(RaycastSelector selector)
	{
		_selector = selector;
	}

	public override Selector NewSelector(Random random)
	{
		return new Runtime(_selector);
	}
}
