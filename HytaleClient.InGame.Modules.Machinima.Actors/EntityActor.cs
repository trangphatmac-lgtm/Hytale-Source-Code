using Coherent.UI.Binding;
using Hypixel.ProtoPlus;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Machinima.Settings;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Actors;

[CoherentType]
internal class EntityActor : SceneActor
{
	protected Model _baseModel;

	public string ModelId = "";

	[JsonIgnore]
	protected Entity _entity { get; set; }

	public float Scale { get; private set; } = 1f;


	protected override ActorType GetActorType()
	{
		return ActorType.Entity;
	}

	public EntityActor(GameInstance gameInstance, string name, Entity entity)
		: base(gameInstance, name)
	{
		_entity = entity;
	}

	public void SetEntity(Entity entity)
	{
		_entity = entity;
	}

	public Entity GetEntity()
	{
		return _entity;
	}

	public Model GetModel()
	{
		return _entity?.ModelPacket;
	}

	public void SetBaseModel(Model model)
	{
		_baseModel = model;
		if (_entity != null)
		{
			_entity.SetCharacterModel(model, new string[0]);
		}
	}

	public void SetScale(float scale)
	{
		Scale = scale;
		if (_entity != null)
		{
			_entity.Scale = scale;
		}
	}

	public void ForceUpdate(GameInstance gameInstance)
	{
		if (_entity != null)
		{
			_entity.SetPosition(Position);
			_entity.SetBodyOrientation(Rotation);
			_entity.LookOrientation = Look;
			_entity.PositionProgress = 1f;
			_entity.BodyOrientationProgress = 1f;
		}
	}

	public Model GetBaseModel()
	{
		return _baseModel;
	}

	public virtual void Spawn(GameInstance gameInstance)
	{
		if (_entity == null && !(this is PlayerActor))
		{
			Model val = GetBaseModel();
			if (val == null)
			{
				val = gameInstance.LocalPlayer.ModelPacket;
				SetBaseModel(val);
			}
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
			gameInstance.EntityStoreModule.Spawn(-1, out var entity);
			entity.SetIsTangible(isTangible: true);
			entity.SetCharacterModel(val, new string[0]);
			entity.SetSpawnTransform(position, bodyOrientation, lookOrientation);
			entity.Scale = Scale;
			_entity = entity;
		}
	}

	public void UpdateModel(GameInstance gameInstance, string modelId = null)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		if (modelId != null)
		{
			ModelId = modelId;
		}
		if (_entity != null && !string.IsNullOrWhiteSpace(ModelId))
		{
			gameInstance.Connection.SendPacket((ProtoPacket)new RequestMachinimaActorModel(ModelId, gameInstance.MachinimaModule.ActiveScene.Name, base.Name));
		}
	}

	public virtual void Despawn(GameInstance gameInstance)
	{
		if (_entity != null && !(this is PlayerActor))
		{
			gameInstance.EntityStoreModule.Despawn(_entity.NetworkId);
			_entity = null;
		}
	}

	public override TrackKeyframe CreateKeyframe(float frame)
	{
		TrackKeyframe trackKeyframe = new TrackKeyframe(frame);
		trackKeyframe.AddSetting(new PositionSetting(_entity.Position));
		trackKeyframe.AddSetting(new RotationSetting(_entity.BodyOrientation));
		trackKeyframe.AddSetting(new LookSetting(_entity.LookOrientation));
		return trackKeyframe;
	}

	public override void LoadKeyframe(TrackKeyframe keyframe)
	{
		base.LoadKeyframe(keyframe);
		if (_entity != null)
		{
			_entity.SetPosition(Position);
			_entity.SetBodyOrientation(Rotation);
			_entity.LookOrientation = Look;
			_entity.BodyOrientationProgress = 1f;
			_entity.PositionProgress = 1f;
		}
	}

	public override SceneActor Clone(GameInstance gameInstance)
	{
		SceneActor actor = new EntityActor(gameInstance, "clone", null);
		base.Track.CopyToActor(ref actor);
		EntityActor entityActor = actor as EntityActor;
		entityActor.SetBaseModel(_baseModel);
		entityActor.SetScale(Scale);
		return entityActor;
	}
}
