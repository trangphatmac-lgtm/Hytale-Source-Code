using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Machinima.Actors;

internal class ItemActor : EntityActor
{
	public string ItemId { get; private set; } = "";


	protected override ActorType GetActorType()
	{
		return ActorType.Item;
	}

	public ItemActor(GameInstance gameInstance, string name, Entity entity, string itemId = "")
		: base(gameInstance, name, entity)
	{
		ItemId = itemId;
	}

	public override void Spawn(GameInstance gameInstance)
	{
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		if (base._entity == null)
		{
			Vector3 position = new Vector3(0f, 100f, 0f);
			Vector3 bodyOrientation = Vector3.Zero;
			Vector3 lookOrientation = Vector3.Zero;
			SceneTrack track = base.Track;
			if (track != null && track.Keyframes?.Count > 0)
			{
				TrackKeyframe trackKeyframe = base.Track.Keyframes[0];
				position = trackKeyframe.GetSetting<Vector3>("Position").Value;
				bodyOrientation = trackKeyframe.GetSetting<Vector3>("Rotation").Value;
				lookOrientation = trackKeyframe.GetSetting<Vector3>("Look").Value;
			}
			Item item = (string.IsNullOrWhiteSpace(ItemId) ? ((Item)null) : new Item(ItemId, 1, 0.0, 0.0, false, new sbyte[0]));
			gameInstance.EntityStoreModule.Spawn(-1, out var entity);
			entity.SetIsTangible(isTangible: true);
			entity.SetItem(item);
			entity.SetSpawnTransform(position, bodyOrientation, lookOrientation);
			entity.Scale = base.Scale;
			base._entity = entity;
		}
	}

	public void SetItemId(string itemId, GameInstance gameInstance = null)
	{
		ItemId = itemId;
		if (gameInstance != null)
		{
			if (base._entity != null)
			{
				Despawn(gameInstance);
			}
			Spawn(gameInstance);
		}
	}

	public override SceneActor Clone(GameInstance gameInstance)
	{
		SceneActor actor = new ItemActor(gameInstance, "clone", null);
		base.Track.CopyToActor(ref actor);
		ItemActor itemActor = actor as ItemActor;
		itemActor.SetItemId(ItemId);
		itemActor.SetScale(base.Scale);
		return itemActor;
	}
}
