using System;
using Coherent.UI.Binding;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Machinima.Actors;

[CoherentType]
internal class ReferenceActor : SceneActor
{
	private const float ActorBoxScale = 0.375f;

	private GameInstance _gameInstance;

	private readonly BoxRenderer _boxRenderer;

	private Vector3 _boxColor;

	protected override ActorType GetActorType()
	{
		return ActorType.Reference;
	}

	public ReferenceActor(GameInstance gameInstance, string name)
		: base(gameInstance, name)
	{
		_gameInstance = gameInstance;
		_boxRenderer = new BoxRenderer(_gameInstance.Engine.Graphics, _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram);
		_boxColor = _gameInstance.Engine.Graphics.YellowColor;
	}

	protected override void DoDispose()
	{
		_boxRenderer.Dispose();
		base.DoDispose();
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		Vector3 position = Position;
		Vector3 position2 = Vector3.One / -2f;
		_modelMatrix = Matrix.Identity;
		Matrix.CreateTranslation(ref position2, out _tempMatrix);
		Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
		Matrix.CreateScale(0.375f, out _tempMatrix);
		Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
		Matrix.CreateFromYawPitchRoll(Look.Y + (float)System.Math.PI / 2f, 0f, Look.X, out _tempMatrix);
		Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(Look.Yaw, Look.Pitch, Look.Roll);
		Matrix.CreateFromQuaternion(ref quaternion, out _tempMatrix);
		Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
		Matrix.CreateTranslation(ref position, out _tempMatrix);
		Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
		Matrix.Multiply(ref _modelMatrix, ref viewProjectionMatrix, out _modelMatrix);
		_boxRenderer.Draw(ref _modelMatrix, _boxColor, 0.7f, _boxColor, 0.2f);
	}

	public override SceneActor Clone(GameInstance gameInstance)
	{
		SceneActor actor = new ReferenceActor(gameInstance, base.Name + "-copy");
		base.Track.CopyToActor(ref actor);
		return actor;
	}
}
