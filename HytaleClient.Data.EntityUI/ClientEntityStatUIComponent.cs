using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.InGame.EntityUI;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityUI;

internal class ClientEntityStatUIComponent : ClientEntityUIComponent
{
	public int EntityStatIndex;

	public EntityUIComponentRenderer<ClientEntityStatUIComponent> Renderer;

	private ClientEntityStatUIComponent()
	{
	}

	public ClientEntityStatUIComponent(int id, EntityUIComponent component, EntityUIComponentRenderer<ClientEntityStatUIComponent> renderer)
		: base(id, component)
	{
		EntityStatIndex = component.EntityStatIndex;
		Renderer = renderer;
	}

	public override ClientEntityUIComponent Clone()
	{
		return new ClientEntityStatUIComponent
		{
			Id = Id,
			HitboxOffset = HitboxOffset,
			Unknown = Unknown,
			EntityStatIndex = EntityStatIndex,
			Renderer = Renderer
		};
	}

	public override void RegisterDrawTasksForEntity(Entity entity, Matrix transformationMatrix, float distanceToCamera, int entitiesCount, ref EntityUIDrawTask[] drawTasks, ref int drawTasksCount)
	{
		Renderer.RegisterDrawTasksForEntity(this, entity, transformationMatrix, distanceToCamera, entitiesCount, ref drawTasks, ref drawTasksCount);
	}

	public override void PrepareForDraw(EntityUIDrawTask task)
	{
		Renderer.PrepareForDraw(this, task);
	}
}
