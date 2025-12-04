using System;
using Coherent.UI.Binding;
using HytaleClient.Core;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Machinima.Settings;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima.Actors;

[CoherentType]
internal abstract class SceneActor : Disposable
{
	private static int _nextId;

	[CoherentProperty("id")]
	public readonly int Id;

	[CoherentProperty("visible")]
	public bool Visible = true;

	public Vector3 Position = Vector3.Zero;

	public Vector3 Rotation = Vector3.Zero;

	public Vector3 Look = Vector3.Zero;

	private SceneTrack _track;

	protected Matrix _modelMatrix;

	protected Matrix _tempMatrix;

	[JsonProperty(PropertyName = "Name")]
	[CoherentProperty("name")]
	public string Name { get; set; }

	public SceneActor LookTargetActor { get; private set; }

	public SceneActor PositionTargetActor { get; private set; }

	[JsonProperty(PropertyName = "Track")]
	[CoherentProperty("track")]
	public SceneTrack Track
	{
		get
		{
			return _track;
		}
		set
		{
			if (_track != null)
			{
				_track.Dispose();
			}
			_track = value;
		}
	}

	[JsonProperty(PropertyName = "Type")]
	[CoherentProperty("type")]
	public ActorType Type => GetActorType();

	protected virtual ActorType GetActorType()
	{
		return ActorType.Reference;
	}

	public SceneActor(GameInstance gameInstance, string name)
	{
		Name = name;
		_track = new SceneTrack(gameInstance, this);
		Id = ++_nextId;
	}

	public virtual void Draw(ref Matrix viewProjectionMatrix)
	{
		Track.Draw(ref viewProjectionMatrix);
	}

	public virtual void Update(float currentFrame, float lastFrame)
	{
		Track.Update(currentFrame, lastFrame);
	}

	protected override void DoDispose()
	{
		Track.Dispose();
	}

	public void SetLookTarget(SceneActor actor)
	{
		LookTargetActor = actor;
	}

	public virtual TrackKeyframe CreateKeyframe(float frame)
	{
		TrackKeyframe trackKeyframe = new TrackKeyframe(frame);
		trackKeyframe.AddSetting(new PositionSetting(Position));
		trackKeyframe.AddSetting(new RotationSetting(Rotation));
		trackKeyframe.AddSetting(new LookSetting(Look));
		return trackKeyframe;
	}

	public TrackKeyframe CreateKeyframe(float frame, Vector3 position, Vector3 rotation, Vector3 look)
	{
		TrackKeyframe trackKeyframe = new TrackKeyframe(frame);
		trackKeyframe.AddSetting(new PositionSetting(position));
		trackKeyframe.AddSetting(new RotationSetting(rotation));
		trackKeyframe.AddSetting(new LookSetting(look));
		return trackKeyframe;
	}

	public virtual void LoadKeyframe(TrackKeyframe keyframe)
	{
		Vector3 position = Position;
		Vector3 value = keyframe.GetSetting<Vector3>("Position").Value;
		if (PositionTargetActor != null)
		{
			Vector3 position2 = PositionTargetActor.Position;
			Position = position2 + value;
		}
		else
		{
			Position = value;
		}
		Rotation = keyframe.GetSetting<Vector3>("Rotation").Value;
		if (LookTargetActor != null)
		{
			Vector3 position3 = LookTargetActor.Position;
			if (LookTargetActor is EntityActor)
			{
				Entity entity = (LookTargetActor as EntityActor).GetEntity();
				if (entity != null && entity.Type == Entity.EntityType.Character)
				{
					position3 = entity.Position;
					position3.Y += entity.EyeOffset;
				}
			}
			Vector3 position4 = Position;
			if (this is EntityActor)
			{
				Entity entity2 = (this as EntityActor).GetEntity();
				if (entity2 != null && entity2.Type == Entity.EntityType.Character)
				{
					position4 = entity2.Position;
					position4.Y += entity2.EyeOffset;
				}
			}
			Vector3 targetDirection = Vector3.GetTargetDirection(position4, position3);
			Vector3 value2 = keyframe.GetSetting<Vector3>("Look").Value;
			Look.X = targetDirection.X;
			Look.Y = targetDirection.Y;
			Look.Z = value2.Z;
		}
		else
		{
			Look = keyframe.GetSetting<Vector3>("Look").Value;
			Look.Y += Rotation.Y;
		}
		float num = MathHelper.WrapAngle(Look.Yaw - Rotation.Yaw);
		if (num > (float)System.Math.PI / 4f)
		{
			Rotation.Yaw += num - (float)System.Math.PI / 4f;
		}
		else if (num < -(float)System.Math.PI / 4f)
		{
			Rotation.Yaw += num + (float)System.Math.PI / 4f;
		}
	}

	public void AlignToPath(bool alignAll = false)
	{
		for (int i = 0; i < Track.Keyframes.Count; i++)
		{
			TrackKeyframe trackKeyframe = Track.Keyframes[i];
			float frame = trackKeyframe.Frame;
			KeyframeSetting<Vector3> setting = trackKeyframe.GetSetting<Vector3>("Position");
			if (setting != null)
			{
				_ = setting.Value;
				if (0 == 0)
				{
					Vector3 value = setting.Value;
					Vector3 source = ((i == 0) ? value : Track.Path.GetPathPosition(i - 1, 0.99f));
					Vector3 target = ((i >= Track.Keyframes.Count - 1) ? value : Track.Path.GetPathPosition(i, 0.1f));
					Vector3 targetDirection = Vector3.GetTargetDirection(source, target);
					Vector3 value2 = (alignAll ? targetDirection : new Vector3(0f, targetDirection.Y, 0f));
					Track.Keyframes[i].GetSetting<Vector3>("Rotation").Value = value2;
					Track.Keyframes[i].GetSetting<Vector3>("Look").Value = new Vector3(targetDirection.X, 0f, targetDirection.Z);
				}
			}
		}
		Track.UpdateKeyframeData();
	}

	public static SceneActor ConvertJsonObject(GameInstance gameInstance, SerializedSceneObject actorData)
	{
		SceneActor sceneActor;
		switch ((ActorType)actorData.Type)
		{
		case ActorType.Camera:
			sceneActor = new CameraActor(gameInstance, actorData.Name);
			break;
		case ActorType.Player:
			sceneActor = new PlayerActor(gameInstance, actorData.Name);
			((PlayerActor)sceneActor).SetBaseModel(actorData.Model);
			break;
		case ActorType.Entity:
		{
			sceneActor = new EntityActor(gameInstance, actorData.Name, null);
			EntityActor entityActor = sceneActor as EntityActor;
			entityActor.SetBaseModel(actorData.Model);
			entityActor.ModelId = ((actorData.ModelId == null) ? "" : actorData.ModelId);
			entityActor.SetScale((actorData.Scale <= 0f) ? 1f : actorData.Scale);
			break;
		}
		case ActorType.Item:
			sceneActor = new ItemActor(gameInstance, actorData.Name, null, actorData.ItemId);
			(sceneActor as EntityActor).SetScale((actorData.Scale <= 0f) ? 1f : actorData.Scale);
			break;
		case ActorType.Reference:
			sceneActor = new ReferenceActor(gameInstance, actorData.Name);
			break;
		default:
			return null;
		}
		sceneActor.Track = actorData.Track;
		return sceneActor;
	}

	public virtual void WriteToJsonObject(JsonSerializer serializer, JsonWriter writer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("Name", JToken.FromObject((object)Name));
		val.Add("Type", JToken.FromObject((object)Type));
		JTokenWriter val2 = new JTokenWriter();
		serializer.Serialize((JsonWriter)(object)val2, (object)Track, typeof(SceneTrack));
		((JsonWriter)val2).Close();
		val.Add("Track", val2.Token);
		if (this is ItemActor itemActor)
		{
			val.Add("ItemId", JToken.FromObject((object)itemActor.ItemId));
			val.Add("Scale", JToken.FromObject((object)itemActor.Scale));
		}
		else if (this is EntityActor entityActor)
		{
			Model val3 = entityActor.GetBaseModel();
			if (val3 == null)
			{
				val3 = entityActor?.GetEntity().ModelPacket;
			}
			if (val3 != null)
			{
				val.Add("Model", JToken.FromObject((object)entityActor.GetBaseModel()));
			}
			val.Add("ModelId", JToken.FromObject((object)entityActor.ModelId));
			val.Add("Scale", JToken.FromObject((object)entityActor.Scale));
		}
		((JToken)val).WriteTo(writer, Array.Empty<JsonConverter>());
	}

	public abstract SceneActor Clone(GameInstance gameInstance);
}
