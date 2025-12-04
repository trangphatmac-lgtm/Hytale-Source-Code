#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using Coherent.UI.Binding;
using HytaleClient.Core;
using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Machinima.Events;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zlib;

namespace HytaleClient.InGame.Modules.Machinima;

[CoherentType]
internal class MachinimaScene : Disposable
{
	[JsonProperty(PropertyName = "Version")]
	public const int FILE_VERSION = 1;

	private GameInstance _gameInstance;

	[JsonProperty(PropertyName = "Name")]
	[CoherentProperty("name")]
	public string Name;

	private Vector3 _origin = Vector3.NaN;

	[JsonProperty(PropertyName = "OriginLook")]
	[CoherentProperty("originLook")]
	public Vector3 OriginLook = Vector3.Zero;

	[JsonProperty(PropertyName = "StartupCommands")]
	private List<string> _startupCommands = new List<string>();

	private bool _isActive;

	private float _lastFrame;

	[JsonProperty(PropertyName = "Actors")]
	[CoherentProperty("sceneObjects")]
	public List<SceneActor> Actors { get; } = new List<SceneActor>();


	[JsonProperty(PropertyName = "Origin")]
	[CoherentProperty("origin")]
	public Vector3 Origin
	{
		get
		{
			if (_origin.IsNaN())
			{
				_origin = _gameInstance.LocalPlayer.Position;
				OriginLook = _gameInstance.LocalPlayer.LookOrientation;
			}
			return _origin;
		}
		set
		{
			_origin = value;
		}
	}

	[JsonIgnore]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (_isActive == value)
			{
				return;
			}
			_isActive = value;
			foreach (SceneActor actor in Actors)
			{
				if (actor is EntityActor entityActor)
				{
					if (_isActive)
					{
						entityActor.Spawn(_gameInstance);
					}
					else
					{
						entityActor.Despawn(_gameInstance);
					}
				}
			}
			if (_isActive)
			{
				RunStartupCommands();
			}
		}
	}

	public MachinimaScene(GameInstance gameInstance, string name)
	{
		_gameInstance = gameInstance;
		Name = name;
	}

	public void Initialize(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		for (int i = 0; i < Actors.Count; i++)
		{
			Actors[i].Track.Initialize(_gameInstance, Actors[i]);
			foreach (TrackKeyframe keyframe in Actors[i].Track.Keyframes)
			{
				if (keyframe.Events.Count <= 0)
				{
					continue;
				}
				foreach (KeyframeEvent @event in keyframe.Events)
				{
					@event.Initialize(this);
				}
			}
		}
	}

	public void Update(float frame)
	{
		foreach (SceneActor actor in Actors)
		{
			actor.Update(frame, _lastFrame);
		}
		_lastFrame = frame;
	}

	protected override void DoDispose()
	{
		foreach (SceneActor actor in Actors)
		{
			actor.Dispose();
		}
	}

	public void Draw(ref Matrix viewProjectionMatrix)
	{
		foreach (SceneActor actor in Actors)
		{
			if (actor.Visible)
			{
				actor.Draw(ref viewProjectionMatrix);
			}
		}
	}

	public float GetSceneLength()
	{
		float num = 0f;
		float num2 = 0f;
		foreach (SceneActor actor in Actors)
		{
			num2 = actor.Track.GetTrackLength();
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public void ListActors()
	{
		if (Actors.Count == 0)
		{
			_gameInstance.Chat.Log("No actors currently exist in scene '" + Name + "'");
			return;
		}
		_gameInstance.Chat.Log($"{Actors.Count} Actors in scene '{Name}':");
		foreach (SceneActor actor in Actors)
		{
			int count = actor.Track.Keyframes.Count;
			float num = ((count > 0) ? actor.Track.Keyframes[count - 1].Frame : 0f);
			double num2 = System.Math.Round(num / _gameInstance.MachinimaModule.PlaybackFPS * 10f) / 10.0;
			string text = actor.GetType().Name.Replace("Actor", "");
			string text2 = ((actor == _gameInstance.MachinimaModule.ActiveActor) ? " - Active" : "");
			string message = $"'{actor.Name}' ({text}) - [{count} keyframes - {num2} sec]{text2}";
			_gameInstance.Chat.Log(message);
		}
	}

	public bool AddActor(SceneActor actor, bool addStartKeyframe = true)
	{
		if (string.IsNullOrEmpty(actor.Name) || HasActor(actor.Name))
		{
			return false;
		}
		if (actor is PlayerActor)
		{
			foreach (SceneActor actor2 in Actors)
			{
				if (actor2 is PlayerActor)
				{
					_gameInstance.Chat.Log("Only one player actor may be added per scene.");
					return false;
				}
			}
		}
		else if (actor is EntityActor entityActor && IsActive)
		{
			entityActor.Spawn(_gameInstance);
		}
		if (addStartKeyframe)
		{
			Vector3 position = ((actor is CameraActor) ? _gameInstance.CameraModule.GetLookRay().Position : _gameInstance.LocalPlayer.Position);
			Vector3 lookOrientation = _gameInstance.LocalPlayer.LookOrientation;
			Vector3 bodyOrientation = _gameInstance.LocalPlayer.BodyOrientation;
			bodyOrientation.Y = MathHelper.WrapAngle(bodyOrientation.Y);
			lookOrientation.Y -= bodyOrientation.Y;
			TrackKeyframe keyframe = actor.CreateKeyframe(0f, position, bodyOrientation, lookOrientation);
			actor.Track.AddKeyframe(keyframe);
		}
		Actors.Add(actor);
		return true;
	}

	public bool RemoveActor(string actorName)
	{
		int num = -1;
		for (int i = 0; i < Actors.Count; i++)
		{
			if (Actors[i].Name == actorName)
			{
				num = i;
				break;
			}
		}
		if (num > -1)
		{
			if (Actors[num] is EntityActor entityActor)
			{
				entityActor.Despawn(_gameInstance);
			}
			Actors[num].Dispose();
			Actors.RemoveAt(num);
			return true;
		}
		return false;
	}

	public SceneActor GetActor(string actorName)
	{
		foreach (SceneActor actor in Actors)
		{
			if (actor.Name == actorName)
			{
				return actor;
			}
		}
		return null;
	}

	public SceneActor GetActor(int actorId)
	{
		foreach (SceneActor actor in Actors)
		{
			if (actor.Id == actorId)
			{
				return actor;
			}
		}
		return null;
	}

	public TrackKeyframe GetEventKeyframe(int eventId)
	{
		foreach (SceneActor actor in Actors)
		{
			foreach (TrackKeyframe keyframe in actor.Track.Keyframes)
			{
				if (keyframe.HasEvent(eventId))
				{
					return keyframe;
				}
			}
		}
		return null;
	}

	public bool HasActor(string actorName)
	{
		return GetActor(actorName) != null;
	}

	public List<SceneActor> GetActors()
	{
		return Actors;
	}

	public void ClearActors()
	{
		foreach (SceneActor actor in Actors)
		{
			actor.Dispose();
		}
		Actors.Clear();
	}

	public void OffsetOrigin(Vector3 offset)
	{
		Vector3 offset2 = offset - Origin;
		foreach (SceneActor actor in Actors)
		{
			actor.Track.OffsetPositions(offset2);
		}
		Origin = offset;
	}

	public void Rotate(Vector3 rotation, Vector3 origin)
	{
		if (origin.IsNaN())
		{
			origin = Origin;
		}
		Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
		Matrix.CreateFromQuaternion(ref quaternion, out var result);
		for (int i = 0; i < Actors.Count; i++)
		{
			Actors[i].Track.RotatePath(rotation, origin);
		}
		Origin = Vector3.Transform(Origin - origin, result) + origin;
		OriginLook.Y = MathHelper.WrapAngle(OriginLook.Y + rotation.Y);
	}

	public string GetNextActorName(string actorName)
	{
		string text = actorName;
		if (string.IsNullOrEmpty(text))
		{
			text = "actor";
		}
		if (HasActor(text))
		{
			string text2 = text;
			for (int i = 1; i < 999999; i++)
			{
				text2 = $"{text}{i}";
				if (!HasActor(text2))
				{
					return text2;
				}
			}
		}
		return text;
	}

	private void RunStartupCommands()
	{
		foreach (string startupCommand in _startupCommands)
		{
			if (startupCommand.StartsWith("."))
			{
				_gameInstance.ExecuteCommand(startupCommand);
			}
			else if (startupCommand.StartsWith("/"))
			{
				_gameInstance.Chat.SendCommand(startupCommand.Substring(1));
			}
		}
	}

	public string Serialize(JsonSerializerSettings serializerSettings)
	{
		return JsonConvert.SerializeObject((object)this, (Formatting)1, serializerSettings);
	}

	public static MachinimaScene Deserialize(string jsonString, GameInstance gameInstance, JsonSerializerSettings serializerSettings)
	{
		try
		{
			JObject sceneObject = JObject.Parse(jsonString);
			UpdateSceneData(ref sceneObject);
			MachinimaScene machinimaScene = ((JToken)sceneObject).ToObject<MachinimaScene>(JsonSerializer.Create(serializerSettings));
			machinimaScene.Initialize(gameInstance);
			return machinimaScene;
		}
		catch (Exception ex)
		{
			gameInstance.Chat.Error("Error deserializing scene data! " + ex.Message);
			Trace.WriteLine(ex);
			return null;
		}
	}

	public byte[] ToCompressedByteArray(JsonSerializerSettings serializerSettings)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		string s = Serialize(serializerSettings);
		byte[] bytes = Encoding.Default.GetBytes(s);
		int num = bytes.Length;
		using MemoryStream memoryStream = new MemoryStream();
		ZLibStream val = new ZLibStream((Stream)memoryStream, CompressionMode.Compress);
		try
		{
			((Stream)(object)val).Write(bytes, 0, bytes.Length);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		return memoryStream.ToArray();
	}

	public static MachinimaScene FromCompressedByteArray(byte[] compressedByteArray, GameInstance gameInstance, JsonSerializerSettings serializerSettings)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		MemoryStream memoryStream = new MemoryStream();
		byte[] bytes;
		using (MemoryStream memoryStream2 = new MemoryStream(compressedByteArray))
		{
			ZLibStream val = new ZLibStream((Stream)memoryStream2, CompressionMode.Decompress);
			try
			{
				((Stream)(object)val).CopyTo((Stream)memoryStream);
				((Stream)(object)val).Close();
				memoryStream.Position = 0L;
				bytes = memoryStream.ToArray();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		string @string = Encoding.Default.GetString(bytes);
		return Deserialize(@string, gameInstance, serializerSettings);
	}

	public static void UpdateSceneData(ref JObject sceneObject)
	{
		JToken obj = sceneObject["Version"];
		if (obj == null || obj.ToObject<int>() == 0)
		{
			foreach (JToken item in (IEnumerable<JToken>)sceneObject["Actors"])
			{
				JToken obj2 = item[(object)"Track"];
				foreach (JToken item2 in (IEnumerable<JToken>)((obj2 != null) ? obj2[(object)"Keyframes"] : null))
				{
					Vector3 vector = item2[(object)"Settings"][(object)"Look"].ToObject<Vector3>();
					vector.Y -= item2[(object)"Settings"][(object)"Rotation"].ToObject<Vector3>().Y;
					item2[(object)"Settings"][(object)"Look"] = (JToken)(object)JObject.FromObject((object)vector);
				}
			}
		}
		sceneObject["Version"] = JToken.op_Implicit(1);
	}
}
