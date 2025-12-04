using Coherent.UI.Binding;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Gizmos.Models;

namespace HytaleClient.InGame.Modules.Machinima.Actors;

[CoherentType]
internal class PlayerActor : EntityActor
{
	private GameInstance _gameInstance;

	private PrimitiveModelRenderer _modelRenderer;

	protected override ActorType GetActorType()
	{
		return ActorType.Player;
	}

	public PlayerActor(GameInstance gameInstance, string name)
		: base(gameInstance, name, gameInstance.LocalPlayer)
	{
		_gameInstance = gameInstance;
		_modelRenderer = new PrimitiveModelRenderer(gameInstance.Engine.Graphics, gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram);
		_modelRenderer.UpdateModelData(CameraModel.BuildModelData());
	}
}
