using System;
using HytaleClient.Graphics;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal class HorizontalSelector : SelectorType
{
	private class Runtime : Selector
	{
		private readonly HorizontalSelector _selector;

		private Matrix _projectionMatrix;

		private BoundingFrustum _projectionFrustum = new BoundingFrustum(Matrix.Identity);

		private Matrix _viewMatrix;

		private readonly HitDetectionExecutor _executor;

		private float _lastTime = 0f;

		private float _runTimeDeltaPercentageSum;

		private Vector3? _debugColor;

		public Runtime(Random random, HorizontalSelector selector)
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
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Invalid comparison between Unknown and I4
			int num = 1;
			if ((int)_selector.Direction == 1)
			{
				num = -1;
			}
			CharacterControllerModule characterControllerModule = gameInstance.CharacterControllerModule;
			float num2 = attacker.EyeOffset + ((gameInstance.LocalPlayer == attacker) ? (characterControllerModule.MovementController.AutoJumpHeightShift + characterControllerModule.MovementController.CrouchHeightShift) : 0f);
			Vector3 position = attacker.Position;
			float x = position.X;
			float num3 = position.Y + num2;
			float z = position.Z;
			float num4 = time - _lastTime;
			_lastTime = time;
			float num5 = num4 / runTime;
			float num6 = _selector.StartDistance / _selector.EndDistance;
			float num7 = _selector.YawLength * num5;
			float num8 = _selector.YawLength * _runTimeDeltaPercentageSum;
			float num9 = 2f * _selector.EndDistance * num7 / (float)System.Math.PI;
			float num10 = (num8 + num7 + _selector.YawStartOffset) * (float)num;
			float num11 = num9 * num6;
			_projectionMatrix = Matrix.CreateRotationZ(0f - _selector.RollOffset) * Matrix.CreateRotationY(0f - num10) * Matrix.CreateRotationX(0f - _selector.PitchOffset) * Matrix.CreateProjectionFrustum(near: _selector.StartDistance, far: _selector.EndDistance, left: num11, right: num11, bottom: _selector.ExtendBottom * num6, top: _selector.ExtendTop * num6);
			Vector3 vector = Vector3.CreateFromYawPitch(attacker.LookOrientation.Yaw, attacker.LookOrientation.Pitch);
			_viewMatrix = Matrix.CreateViewDirection(x, num3, z, vector.X, vector.Y, vector.Z, 0f, 1f, 0f);
			_executor.SetOrigin(new Vector3(x, num3, z));
			_executor.ProjectionMatrix = _projectionMatrix;
			_executor.ViewMatrix = _viewMatrix;
			if (gameInstance.InteractionModule.ShowSelectorDebug)
			{
				_projectionFrustum.Matrix = _projectionMatrix;
				Mesh result = default(Mesh);
				MeshProcessor.CreateFrustum(ref result, ref _projectionFrustum);
				Vector3 vector2 = _debugColor ?? SelectorType.GenerateDebugColor();
				_debugColor = vector2;
				Matrix matrix = Matrix.CreateRotationX(attacker.LookOrientation.Pitch) * Matrix.CreateRotationY(attacker.LookOrientation.Yaw) * Matrix.CreateTranslation(x, num3, z);
				gameInstance.InteractionModule.SelectorDebugMeshes.Add(new InteractionModule.DebugSelectorMesh(matrix, result, 5f, vector2));
			}
			_runTimeDeltaPercentageSum += num5;
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

	private HorizontalSelector _selector;

	public HorizontalSelector(HorizontalSelector selector)
	{
		_selector = selector;
	}

	public override Selector NewSelector(Random random)
	{
		return new Runtime(random, _selector);
	}
}
