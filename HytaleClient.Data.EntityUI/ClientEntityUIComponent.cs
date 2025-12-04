using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityUI;

internal abstract class ClientEntityUIComponent
{
	public int Id;

	public Vector2f HitboxOffset;

	public bool Unknown;

	protected ClientEntityUIComponent()
	{
	}

	protected ClientEntityUIComponent(int id, EntityUIComponent component)
	{
		Id = id;
		HitboxOffset = component.HitboxOffset;
		Unknown = component.Unknown;
	}

	public abstract ClientEntityUIComponent Clone();

	public abstract void RegisterDrawTasksForEntity(Entity entity, Matrix transformationMatrix, float distanceToCamera, int entitiesCount, ref EntityUIDrawTask[] drawTasks, ref int drawTasksCount);

	public abstract void PrepareForDraw(EntityUIDrawTask task);

	public Matrix ApplyHitboxOffset(Matrix transformationMatrix)
	{
		if (HitboxOffset.X != 0f)
		{
			transformationMatrix.M41 += HitboxOffset.X;
		}
		if (HitboxOffset.Y != 0f)
		{
			transformationMatrix.M42 += HitboxOffset.Y;
		}
		return transformationMatrix;
	}
}
