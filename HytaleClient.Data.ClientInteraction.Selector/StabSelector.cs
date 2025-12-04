using System;
using HytaleClient.Graphics;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal class StabSelector : SelectorType
{
	private class Runtime : Selector
	{
		private readonly StabSelector _selector;

		private Matrix _projectionMatrix;

		private BoundingFrustum _projectionFrustum = new BoundingFrustum(Matrix.Identity);

		private Matrix _viewMatrix;

		private readonly HitDetectionExecutor _executor;

		private float _lastTime = 0f;

		private float _runTimeDeltaPercentageSum;

		private Vector3? _debugColor;

		public Runtime(Random random, StabSelector selector)
		{
			_executor = new HitDetectionExecutor(random);
			_selector = selector;
			if (_selector.TestLineOfSight)
			{
				_executor.LosProvider = HitDetectionExecutor.DefaultLineOfSightSolid;
			}
			else
			{
				_executor.LosProvider = HitDetectionExecutor.DefaultLineOfSightTrue;
			}
		}

		public void Tick(GameInstance gameInstance, Entity attacker, float time, float runTime)
		{
			float eyeOffset = attacker.EyeOffset;
			Vector3 position = attacker.Position;
			float x = position.X;
			float num = position.Y + eyeOffset;
			float z = position.Z;
			float num2 = time - _lastTime;
			_lastTime = time;
			float num3 = num2 / runTime;
			float num4 = _selector.EndDistance - _selector.StartDistance;
			float num5 = _runTimeDeltaPercentageSum * num4 + _selector.StartDistance;
			float num6 = (_runTimeDeltaPercentageSum + num3) * num4 + _selector.StartDistance;
			Matrix matrix = Matrix.CreateRotationZ(0f - _selector.RollOffset) * Matrix.CreateRotationY(0f - _selector.YawOffset) * Matrix.CreateRotationX(0f - _selector.PitchOffset);
			float near = num5;
			float far = num6;
			_projectionMatrix = matrix * Matrix.CreateProjectionOrtho(_selector.ExtendLeft, _selector.ExtendRight, _selector.ExtendBottom, _selector.ExtendTop, near, far);
			Vector3 vector = Vector3.CreateFromYawPitch(attacker.LookOrientation.Yaw, attacker.LookOrientation.Pitch);
			_viewMatrix = Matrix.CreateViewDirection(x, num, z, vector.X, vector.Y, vector.Z, 0f, 1f, 0f);
			_executor.SetOrigin(new Vector3(x, num, z));
			_executor.ProjectionMatrix = _projectionMatrix;
			_executor.ViewMatrix = _viewMatrix;
			if (gameInstance.InteractionModule.ShowSelectorDebug)
			{
				_projectionFrustum.Matrix = _projectionMatrix;
				Mesh result = default(Mesh);
				MeshProcessor.CreateFrustum(ref result, ref _projectionFrustum);
				Vector3 vector2 = _debugColor ?? SelectorType.GenerateDebugColor();
				_debugColor = vector2;
				Matrix matrix2 = Matrix.CreateRotationX(attacker.LookOrientation.Pitch) * Matrix.CreateRotationY(attacker.LookOrientation.Yaw) * Matrix.CreateTranslation(x, num, z);
				gameInstance.InteractionModule.SelectorDebugMeshes.Add(new InteractionModule.DebugSelectorMesh(matrix2, result, 5f, vector2));
			}
			_runTimeDeltaPercentageSum += num3;
		}

		public void SelectTargetEntities(GameInstance gameInstance, Entity attacker, EntityHitConsumer consumer, Predicate<Entity> filter)
		{
			SelectorType.SelectNearbyEntities(gameInstance, attacker, _selector.EndDistance + 1f, delegate(Entity entity)
			{
				BoundingBox hitbox = entity.Hitbox;
				Matrix modelMatrix = Matrix.CreateScale(hitbox.GetSize()) * Matrix.CreateTranslation(hitbox.Min) * Matrix.CreateTranslation(entity.Position);
				if (_executor.Test(gameInstance, HitDetectionExecutor.CUBE_QUADS, modelMatrix))
				{
					consumer(entity, _executor.GetHitLocation());
				}
			}, filter);
		}
	}

	private StabSelector _selector;

	public StabSelector(StabSelector selector)
	{
		_selector = selector;
	}

	public override Selector NewSelector(Random random)
	{
		return new Runtime(random, _selector);
	}
}
