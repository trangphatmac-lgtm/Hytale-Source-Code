using System;
using HytaleClient.Graphics;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal class AOECylinderSelector : SelectorType
{
	private class Runtime : Selector
	{
		private readonly AOECylinderSelector _selector;

		private Vector3? _debugColor;

		public Runtime(AOECylinderSelector selector)
		{
			_selector = selector;
		}

		public void Tick(GameInstance gameInstance, Entity attacker, float time, float runTime)
		{
			if (gameInstance.InteractionModule.ShowSelectorDebug)
			{
				Vector3 vector = SelectTargetPosition(attacker);
				float x = vector.X;
				float yPosition = vector.Y + _selector.Height / 2f;
				float z = vector.Z;
				Mesh result = default(Mesh);
				MeshProcessor.CreateSphere(ref result, 5, 8, _selector.Range, 0);
				Vector3 vector2 = _debugColor ?? SelectorType.GenerateDebugColor();
				_debugColor = vector2;
				Matrix matrix = Matrix.CreateTranslation(x, yPosition, z);
				gameInstance.InteractionModule.SelectorDebugMeshes.Add(new InteractionModule.DebugSelectorMesh(matrix, result, 5f, vector2));
			}
		}

		public void SelectTargetEntities(GameInstance gameInstance, Entity attacker, EntityHitConsumer consumer, Predicate<Entity> filter)
		{
			Vector3 position = SelectTargetPosition(attacker);
			SelectorType.SelectNearbyEntities(gameInstance, attacker, position, _selector.Range, delegate(Entity entity)
			{
				float num = entity.Position.Y - position.Y;
				if (!(num < 0f) && !(num > _selector.Height))
				{
					consumer(entity, new Vector4(entity.Position, 1f));
				}
			}, filter);
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

	private AOECylinderSelector _selector;

	public AOECylinderSelector(AOECylinderSelector selector)
	{
		_selector = selector;
	}

	public override Selector NewSelector(Random random)
	{
		return new Runtime(_selector);
	}
}
