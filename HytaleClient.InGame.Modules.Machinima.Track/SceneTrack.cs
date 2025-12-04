using System;
using System.Collections.Generic;
using Coherent.UI.Binding;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Particles;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Machinima.Events;
using HytaleClient.InGame.Modules.Machinima.Settings;
using HytaleClient.InGame.Modules.Machinima.TrackPath;
using HytaleClient.Math;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Track;

[CoherentType]
internal class SceneTrack : Disposable
{
	public enum TrackPathType
	{
		Line,
		Spline,
		Bezier
	}

	private GameInstance _gameInstance;

	private const float DrawnPathSectionLength = 0.1f;

	private bool _initialized = false;

	[JsonIgnore]
	public SceneActor Parent;

	private LineRenderer _pathLineRenderer;

	private LineRenderer _keyframeAngleRenderer;

	private BoxRenderer _keyframeBoxRenderer;

	private List<ParticleSystemProxy> _particleSystemProxies = new List<ParticleSystemProxy>();

	private Vector3[] _pathControlPositions;

	private Matrix _modelMatrix;

	private Matrix _tempMatrix;

	private List<Tuple<Vector3, Vector3, Vector3>> _positions;

	private Dictionary<KeyframeSettingType, List<TrackKeyframe>> _keyframeData;

	[JsonProperty(PropertyName = "Keyframes")]
	[CoherentProperty("keyframes")]
	public List<TrackKeyframe> Keyframes { get; private set; } = new List<TrackKeyframe>();


	[JsonIgnore]
	public Vector3[] DrawPoints { get; private set; }

	[JsonIgnore]
	public float[] DrawFrames { get; private set; }

	[JsonIgnore]
	public float[] SegmentLengths { get; private set; }

	[JsonIgnore]
	public LinePath Path { get; private set; } = new SplinePath();


	[JsonProperty(PropertyName = "PathType")]
	public TrackPathType PathType { get; private set; } = TrackPathType.Spline;


	public SceneTrack()
	{
	}

	public SceneTrack(GameInstance gameInstance, SceneActor parent)
	{
		Initialize(gameInstance, parent);
	}

	public void Initialize(GameInstance gameInstance, SceneActor parent)
	{
		if (!_initialized)
		{
			Parent = parent;
			GraphicsDevice graphics = gameInstance.Engine.Graphics;
			BasicProgram basicProgram = graphics.GPUProgramStore.BasicProgram;
			_pathLineRenderer = new LineRenderer(graphics, basicProgram);
			_keyframeAngleRenderer = new LineRenderer(graphics, basicProgram);
			_keyframeBoxRenderer = new BoxRenderer(graphics, basicProgram);
			_gameInstance = gameInstance;
			if (PathType == TrackPathType.Bezier)
			{
				Path = new BezierPath();
			}
			_keyframeAngleRenderer.UpdateLineData(new Vector3[2]
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(0.5f, 0f, 0f)
			});
			UpdatePositions();
			_initialized = true;
		}
	}

	public void Draw(ref Matrix viewProjectionMatrix, bool drawPath = true, bool drawNodes = true, bool drawRotationAngle = true)
	{
		if (!_initialized || (Parent is CameraActor && (Parent as CameraActor).Active) || (!drawPath && !drawNodes && !drawRotationAngle))
		{
			return;
		}
		MachinimaModule machinimaModule = _gameInstance.MachinimaModule;
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		bool flag = Parent == _gameInstance.MachinimaModule.ActiveActor;
		float num = 0.3f;
		if (flag)
		{
			num = 0.7f;
		}
		if (drawPath && _positions.Count > 1)
		{
			_pathLineRenderer.Draw(ref viewProjectionMatrix, graphics.WhiteColor, num);
		}
		if (!(drawNodes || drawRotationAngle) || !machinimaModule.ShowPathNodes)
		{
			return;
		}
		for (int i = 0; i < Keyframes.Count; i++)
		{
			Vector3 position = _positions[i].Item1;
			TrackKeyframe trackKeyframe = Keyframes[i];
			Vector3 vector;
			if (machinimaModule.SelectedKeyframe != trackKeyframe)
			{
				vector = ((machinimaModule.HoveredKeyframe == trackKeyframe) ? graphics.YellowColor : ((machinimaModule.ActiveKeyframe == trackKeyframe) ? graphics.MagentaColor : ((i == 0) ? graphics.GreenColor : ((i != Keyframes.Count - 1) ? graphics.BlueColor : graphics.RedColor))));
			}
			else
			{
				vector = graphics.CyanColor;
				UpdatePositions();
				float currentFrame = machinimaModule.CurrentFrame;
				if (!(machinimaModule.SelectedActor is PlayerActor))
				{
					Update(currentFrame, currentFrame);
				}
			}
			if ((machinimaModule.SelectionMode == MachinimaModule.EditorSelectionMode.Actor && machinimaModule.HoveredKeyframe != null && machinimaModule.HoveredActor.Track == this) || (machinimaModule.SelectionMode == MachinimaModule.EditorSelectionMode.Scene && machinimaModule.HoveredKeyframe != null))
			{
				vector = graphics.YellowColor;
			}
			else if ((machinimaModule.SelectionMode == MachinimaModule.EditorSelectionMode.Actor && machinimaModule.ActiveActor == Parent) || (machinimaModule.SelectionMode == MachinimaModule.EditorSelectionMode.Scene && machinimaModule.ActiveKeyframe != null))
			{
				vector = graphics.MagentaColor;
			}
			if (drawNodes)
			{
				if (_gameInstance.Input.IsAltHeld() && machinimaModule.HoveredKeyframe == trackKeyframe && machinimaModule.HoveredActor is EntityActor && !(machinimaModule.HoveredActor is ItemActor) && machinimaModule.SelectionMode == MachinimaModule.EditorSelectionMode.Keyframe)
				{
					Vector3 vector2 = graphics.BlueColor;
					if (machinimaModule.BodyRotateHover)
					{
						vector = graphics.BlueColor;
						vector2 = graphics.YellowColor;
					}
					_keyframeBoxRenderer.Draw(position, TrackKeyframe.KeyframeBox, viewProjectionMatrix, vector, num, vector, num / 3f);
					position.Y -= 0.25f;
					_keyframeBoxRenderer.Draw(position, TrackKeyframe.KeyframeBox, viewProjectionMatrix, vector2, num, vector2, num / 3f);
				}
				else
				{
					_keyframeBoxRenderer.Draw(position, TrackKeyframe.KeyframeBox, viewProjectionMatrix, vector, num, vector, num / 3f);
				}
			}
			if (drawRotationAngle)
			{
				Vector3 item = _positions[i].Item3;
				Vector3 item2 = _positions[i].Item2;
				Matrix.CreateFromYawPitchRoll(item.Y + item2.Y + (float)System.Math.PI / 2f, 0f, item.X, out _modelMatrix);
				Matrix.CreateTranslation(ref position, out _tempMatrix);
				Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
				Matrix.Multiply(ref _modelMatrix, ref viewProjectionMatrix, out _modelMatrix);
				_keyframeAngleRenderer.Draw(ref _modelMatrix, vector, num);
			}
		}
	}

	protected override void DoDispose()
	{
		if (_initialized)
		{
			if (Parent is EntityActor entityActor)
			{
				entityActor.Despawn(_gameInstance);
			}
			_pathLineRenderer.Dispose();
			_keyframeAngleRenderer.Dispose();
			_keyframeBoxRenderer.Dispose();
			ClearParticles();
		}
	}

	public void Update(float currentFrame, float lastFrame)
	{
		TrackKeyframe currentFrame2 = GetCurrentFrame(currentFrame);
		if (currentFrame2 == null)
		{
			return;
		}
		Parent.LoadKeyframe(currentFrame2);
		for (int i = 0; i < _particleSystemProxies.Count; i++)
		{
			ParticleSystemProxy particleSystemProxy = _particleSystemProxies[i];
			if (particleSystemProxy == null || particleSystemProxy.IsExpired)
			{
				_particleSystemProxies.RemoveAt(i);
				continue;
			}
			particleSystemProxy.Position = Parent.Position;
			particleSystemProxy.Rotation = Quaternion.CreateFromYawPitchRoll(Parent.Rotation.Yaw, Parent.Rotation.Pitch, Parent.Rotation.Roll);
		}
		if (_gameInstance.MachinimaModule.Running)
		{
			TriggerEvents(currentFrame, lastFrame);
		}
	}

	public void UpdateKeyframeData()
	{
		Keyframes.Sort((TrackKeyframe x, TrackKeyframe y) => x.Frame.CompareTo(y.Frame));
		if (Parent != null)
		{
			UpdatePositions();
		}
	}

	public void UpdatePositions()
	{
		_positions = new List<Tuple<Vector3, Vector3, Vector3>>();
		List<Vector3> list = new List<Vector3>();
		float frame = -1f;
		for (int i = 0; i < Keyframes.Count; i++)
		{
			int nextKeyframe = GetNextKeyframe(frame);
			if (nextKeyframe == -1)
			{
				break;
			}
			frame = Keyframes[nextKeyframe].Frame;
			KeyframeSetting<Vector3> setting = Keyframes[nextKeyframe].GetSetting<Vector3>("Position");
			KeyframeSetting<Vector3> setting2 = Keyframes[nextKeyframe].GetSetting<Vector3>("Rotation");
			KeyframeSetting<Vector3> setting3 = Keyframes[nextKeyframe].GetSetting<Vector3>("Look");
			if (setting == null || setting3 == null)
			{
				continue;
			}
			Vector3 value = setting.Value;
			Vector3 value2 = setting2.Value;
			Vector3 value3 = setting3.Value;
			_positions.Add(new Tuple<Vector3, Vector3, Vector3>(value, value2, value3));
			list.Add(value);
			if (PathType != TrackPathType.Bezier || i >= Keyframes.Count - 1)
			{
				continue;
			}
			KeyframeSetting<Vector3[]> setting4 = Keyframes[nextKeyframe].GetSetting<Vector3[]>("Curve");
			if (setting4 != null)
			{
				Vector3[] value4 = setting4.Value;
				for (int j = 0; j < value4.Length; j++)
				{
					list.Add(value4[j] + value);
				}
			}
		}
		if (_positions.Count >= 2)
		{
			_pathControlPositions = list.ToArray();
			Path.UpdatePoints(_pathControlPositions);
			SegmentLengths = Path.GetSegmentLengths();
			DrawFrames = Path.GetDrawFrames();
			DrawPoints = Path.GetDrawPoints();
			_pathLineRenderer.UpdateLineData(DrawPoints);
		}
	}

	public float GetPositionPathFrame(Vector3 position)
	{
		if (_positions.Count < 2)
		{
			return -1f;
		}
		for (int i = 0; i < _positions.Count - 1; i++)
		{
			int num = i;
			int num2 = i + 1;
			int index = ((i - 1 >= 0) ? (i - 1) : num);
			int index2 = ((i + 2 < _positions.Count) ? (i + 2) : num2);
			Vector3 p = _positions[index].Item1;
			Vector3 p2 = _positions[num].Item1;
			Vector3 p3 = _positions[num2].Item1;
			Vector3 p4 = _positions[index2].Item1;
			float num3 = Vector3.Distance(_positions[num].Item1, _positions[num2].Item1);
			double num4 = System.Math.Round(num3 / 0.1f);
			for (int j = 0; (double)j <= num4; j++)
			{
				float t = (float)((double)j / num4);
				Vector3.Spline(ref t, ref p, ref p2, ref p3, ref p4, out var result);
				if (result == position)
				{
					float num5 = Keyframes[i + 1].Frame - Keyframes[i].Frame;
					return Keyframes[i].Frame + num5 * t;
				}
			}
		}
		return -1f;
	}

	public TrackKeyframe GetKeyframeByFrame(float keyframePosition)
	{
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (Keyframes[i].Frame == keyframePosition)
			{
				return Keyframes[i];
			}
		}
		return null;
	}

	public TrackKeyframe GetKeyframe(int keyframeId)
	{
		foreach (TrackKeyframe keyframe in Keyframes)
		{
			if (keyframe.Id == keyframeId)
			{
				return keyframe;
			}
		}
		return null;
	}

	public void AddKeyframe(TrackKeyframe keyframe, bool update = true)
	{
		if (GetKeyframeByFrame(keyframe.Frame) != null)
		{
			throw new Exception("Unable to add new keyframe, one already exists at that point in time.");
		}
		if (Path is BezierPath)
		{
			KeyframeSetting<Vector3[]> setting = keyframe.GetSetting<Vector3[]>("Curve");
			if (setting == null)
			{
				Vector3[] positions = new Vector3[2]
				{
					Vector3.One,
					Vector3.One * -1f
				};
				setting = new CurveSetting(positions);
				keyframe.AddSetting(setting);
			}
		}
		Keyframes.Add(keyframe);
		if (update)
		{
			UpdateKeyframeData();
		}
	}

	public void RemoveKeyframe(float keyframePosition)
	{
		if (Keyframes.Count <= 1)
		{
			throw new Exception("Unable to remove keyframe, a minimum of one is required.");
		}
		int num = -1;
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (Keyframes[i].Frame == keyframePosition)
			{
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			Keyframes.RemoveAt(num);
			UpdateKeyframeData();
		}
	}

	public void ClearKeyframes()
	{
		Keyframes.RemoveRange(1, Keyframes.Count - 1);
		UpdateKeyframeData();
	}

	public void OffsetPositions(Vector3 offset)
	{
		for (int i = 0; i < Keyframes.Count; i++)
		{
			TrackKeyframe trackKeyframe = Keyframes[i];
			KeyframeSetting<Vector3> setting = trackKeyframe.GetSetting<Vector3>("Position");
			if (setting != null)
			{
				setting.Value += offset;
			}
		}
		UpdatePositions();
	}

	private void TriggerEvents(float currentFrame, float lastFrame)
	{
		if (currentFrame == lastFrame)
		{
			return;
		}
		for (int i = 0; i < Keyframes.Count; i++)
		{
			TrackKeyframe trackKeyframe = Keyframes[i];
			if (trackKeyframe.Frame <= currentFrame && trackKeyframe.Frame >= lastFrame)
			{
				trackKeyframe.TriggerEvents(_gameInstance, this);
			}
		}
	}

	public SceneTrack CopyToEntity(Entity entity)
	{
		EntityActor entityActor = new EntityActor(_gameInstance, entity.Name, entity);
		entityActor.SetBaseModel(entity.ModelPacket);
		SceneTrack sceneTrack = Clone();
		sceneTrack.Initialize(_gameInstance, entityActor);
		entityActor.Track = sceneTrack;
		Vector3 offset = entity.Position - GetStartingPosition();
		sceneTrack.OffsetPositions(offset);
		return sceneTrack;
	}

	public void CopyToActor(ref SceneActor actor)
	{
		SceneTrack sceneTrack = Clone();
		actor.Track.Dispose();
		actor.Track = sceneTrack;
		sceneTrack.Initialize(_gameInstance, actor);
	}

	private SceneTrack Clone()
	{
		SceneTrack sceneTrack = new SceneTrack();
		sceneTrack.PathType = PathType;
		foreach (TrackKeyframe keyframe in Keyframes)
		{
			sceneTrack.AddKeyframe(keyframe.Clone());
		}
		return sceneTrack;
	}

	public Vector3 GetStartingPosition()
	{
		float frame = -1f;
		for (int i = 0; i < Keyframes.Count; i++)
		{
			int nextKeyframe = GetNextKeyframe(frame);
			if (nextKeyframe == -1)
			{
				break;
			}
			frame = Keyframes[nextKeyframe].Frame;
			KeyframeSetting<Vector3> setting = Keyframes[nextKeyframe].GetSetting<Vector3>("Position");
			if (setting != null)
			{
				return setting.Value;
			}
		}
		return Vector3.Zero;
	}

	public TrackKeyframe GetCurrentFrame(float frame)
	{
		if (Keyframes.Count == 0)
		{
			return null;
		}
		int previousKeyframe = GetPreviousKeyframe(frame);
		int nextKeyframe = GetNextKeyframe(frame);
		if (previousKeyframe == -1 || nextKeyframe == -1 || Keyframes.Count == previousKeyframe)
		{
			return null;
		}
		return InterpolateKeyframe(frame, previousKeyframe, nextKeyframe);
	}

	public void InsertKeyframeOffset(float insertFrame, float timeOffset)
	{
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (Keyframes[i].Frame >= insertFrame)
			{
				Keyframes[i].Frame += timeOffset;
			}
		}
		Keyframes.Sort((TrackKeyframe x, TrackKeyframe y) => x.Frame.CompareTo(y.Frame));
	}

	public int GetPreviousKeyframe(float frame)
	{
		if (Keyframes.Count == 0)
		{
			return -1;
		}
		if (frame <= Keyframes[0].Frame)
		{
			return 0;
		}
		int result = -1;
		float num = -1f;
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (!(Keyframes[i].Frame > frame))
			{
				float num2 = frame - Keyframes[i].Frame;
				if (num == -1f || num2 < num)
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	public int GetNextKeyframe(float frame)
	{
		if (Keyframes.Count == 0)
		{
			return -1;
		}
		if (frame >= Keyframes[Keyframes.Count - 1].Frame)
		{
			return Keyframes.Count - 1;
		}
		int result = -1;
		float num = -1f;
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (!(Keyframes[i].Frame <= frame))
			{
				float num2 = Keyframes[i].Frame - frame;
				if (num == -1f || num2 < num)
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	public int GetNextKeyframe(float frame, KeyframeSettingType settingType)
	{
		if (Keyframes.Count == 0)
		{
			return -1;
		}
		if (frame >= Keyframes[Keyframes.Count - 1].Frame)
		{
			return Keyframes.Count - 1;
		}
		int result = -1;
		float num = -1f;
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (Keyframes[i].Frame <= frame)
			{
				continue;
			}
			bool flag = false;
			foreach (KeyValuePair<string, IKeyframeSetting> setting in Keyframes[i].Settings)
			{
				if (setting.Value.Name == settingType.ToString())
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				float num2 = Keyframes[i].Frame - frame;
				if (num == -1f || num2 < num)
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	public TrackKeyframe GetKeyframeFromEventId(int eventId)
	{
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (Keyframes[i].HasEvent(eventId))
			{
				return Keyframes[i];
			}
		}
		return null;
	}

	private TrackKeyframe InterpolateKeyframe(float frame, int prevFrameIndex, int nextFrameIndex)
	{
		TrackKeyframe trackKeyframe;
		if (prevFrameIndex == nextFrameIndex)
		{
			trackKeyframe = Keyframes[prevFrameIndex].Clone();
			trackKeyframe.Frame = frame;
			return trackKeyframe;
		}
		TrackKeyframe trackKeyframe2 = Keyframes[prevFrameIndex];
		TrackKeyframe trackKeyframe3 = Keyframes[nextFrameIndex];
		float frame2 = trackKeyframe2.Frame;
		float frame3 = trackKeyframe3.Frame;
		float num = frame3 - frame2;
		float num2 = frame - frame2;
		float num3 = ((num <= 0f) ? 0f : (num2 / num));
		int num4 = prevFrameIndex + 1;
		int index = ((prevFrameIndex - 1 >= 0) ? (prevFrameIndex - 1) : prevFrameIndex);
		int index2 = ((prevFrameIndex + 2 < _positions.Count) ? (prevFrameIndex + 2) : num4);
		Easing.EasingType easingType = trackKeyframe2.GetSetting<Easing.EasingType>("Easing")?.Value ?? Easing.EasingType.Linear;
		trackKeyframe = new TrackKeyframe(frame);
		foreach (KeyValuePair<string, IKeyframeSetting> setting2 in trackKeyframe3.Settings)
		{
			string name = setting2.Value.Name;
			Type valueType = setting2.Value.ValueType;
			switch (name)
			{
			case "Position":
				trackKeyframe.AddSetting(new PositionSetting(Path.GetPathPosition(prevFrameIndex, num3, lengthCorrected: true, easingType)));
				break;
			case "Rotation":
			{
				Vector3 item5 = _positions[index].Item2;
				Vector3 item6 = _positions[prevFrameIndex].Item2;
				Vector3 item7 = _positions[num4].Item2;
				Vector3 item8 = _positions[index2].Item2;
				float value4 = MathHelper.Spline(num3, item5.X, item6.X, item7.X, item8.X);
				float angle3 = MathHelper.SplineAngle(num3, item5.Y, item6.Y, item7.Y, item8.Y);
				float angle4 = MathHelper.SplineAngle(num3, item5.Z, item6.Z, item7.Z, item8.Z);
				Vector3 position2 = new Vector3(MathHelper.Clamp(value4, -(float)System.Math.PI / 2f, (float)System.Math.PI / 2f), MathHelper.WrapAngle(angle3), MathHelper.WrapAngle(angle4));
				trackKeyframe.AddSetting(new RotationSetting(position2));
				break;
			}
			case "Look":
			{
				Vector3 item = _positions[index].Item3;
				Vector3 item2 = _positions[prevFrameIndex].Item3;
				Vector3 item3 = _positions[num4].Item3;
				Vector3 item4 = _positions[index2].Item3;
				float value3 = MathHelper.Spline(num3, item.X, item2.X, item3.X, item4.X);
				float angle = MathHelper.SplineAngle(num3, item.Y, item2.Y, item3.Y, item4.Y);
				float angle2 = MathHelper.SplineAngle(num3, item.Z, item2.Z, item3.Z, item4.Z);
				Vector3 position = new Vector3(MathHelper.Clamp(value3, -(float)System.Math.PI / 2f, (float)System.Math.PI / 2f), MathHelper.WrapAngle(angle), MathHelper.WrapAngle(angle2));
				trackKeyframe.AddSetting(new LookSetting(position));
				break;
			}
			case "FieldOfView":
			{
				KeyframeSetting<float> setting = trackKeyframe2.GetSetting<float>("FieldOfView");
				if (setting != null)
				{
					float value = setting.Value;
					float value2 = trackKeyframe3.GetSetting<float>("FieldOfView").Value;
					float fov = MathHelper.Lerp(value, value2, num3);
					trackKeyframe.AddSetting(new FieldOfViewSetting(fov));
				}
				break;
			}
			}
		}
		return trackKeyframe;
	}

	public float GetTrackLength()
	{
		float num = 0f;
		for (int i = 0; i < Keyframes.Count; i++)
		{
			if (Keyframes[i].Frame > num)
			{
				num = Keyframes[i].Frame;
			}
		}
		return num;
	}

	public void ListKeyframes()
	{
		if (Keyframes.Count == 0)
		{
			_gameInstance.Chat.Log("No keyframes found for actor '" + Parent.Name + "'");
			return;
		}
		_gameInstance.Chat.Log($"{Keyframes.Count} keyframes found for actor '{Parent.Name}'");
		for (int i = 0; i < Keyframes.Count; i++)
		{
			TrackKeyframe trackKeyframe = Keyframes[i];
			string arg = ((trackKeyframe.Events.Count == 0) ? "" : $" - Events: {trackKeyframe.Events.Count}");
			_gameInstance.Chat.Log($"#{i} - Frame: {trackKeyframe.Frame}{arg}");
		}
	}

	public void ListEvents()
	{
		List<Tuple<float, List<string>>> list = new List<Tuple<float, List<string>>>();
		foreach (TrackKeyframe keyframe in Keyframes)
		{
			if (keyframe.Events.Count == 0)
			{
				continue;
			}
			list.Add(new Tuple<float, List<string>>(keyframe.Frame, new List<string>()));
			int index = list.Count - 1;
			foreach (KeyframeEvent @event in keyframe.Events)
			{
				list[index].Item2.Add(@event.ToString());
			}
		}
		if (list.Count == 0)
		{
			_gameInstance.Chat.Log("No keyframe events found for actor '" + Parent.Name + "'");
			return;
		}
		foreach (Tuple<float, List<string>> item in list)
		{
			_gameInstance.Chat.Log($"{item.Item2.Count} events for keyframe position {item.Item1}");
			foreach (string item2 in item.Item2)
			{
				_gameInstance.Chat.Log(item2);
			}
		}
	}

	public void SetPathType(TrackPathType pathType, bool reset = false)
	{
		if (PathType == pathType && !reset)
		{
			return;
		}
		bool flag = false;
		switch (pathType)
		{
		case TrackPathType.Spline:
			Path = new SplinePath();
			break;
		case TrackPathType.Bezier:
		{
			Path = new BezierPath();
			for (int i = 0; i < Keyframes.Count; i++)
			{
				KeyframeSetting<Vector3[]> setting = Keyframes[i].GetSetting<Vector3[]>("Curve");
				if (setting != null)
				{
					if (!flag)
					{
						flag = true;
					}
					if (!reset)
					{
						continue;
					}
				}
				Vector3[] positions;
				if (i == Keyframes.Count - 1)
				{
					positions = new Vector3[2]
					{
						Vector3.One,
						Vector3.One * -1f
					};
				}
				else
				{
					Vector3 vector = Keyframes[i + 1].GetSetting<Vector3>("Position").Value - Keyframes[i].GetSetting<Vector3>("Position").Value;
					positions = new Vector3[2]
					{
						vector * 0.33f,
						vector * 0.67f
					};
				}
				setting = new CurveSetting(positions);
				Keyframes[i].AddSetting(setting);
			}
			break;
		}
		}
		PathType = pathType;
		if (!flag && !reset)
		{
			SmoothBezierPath();
		}
		UpdateKeyframeData();
	}

	public float GetPathSegmentLength(int index, int count = 1)
	{
		float num = 0f;
		for (int i = index; i < index + count && i < SegmentLengths.Length; i++)
		{
			num += SegmentLengths[i];
		}
		return num;
	}

	public float GetPathSegmentSpeed(int index, int count = 1)
	{
		float pathSegmentLength = GetPathSegmentLength(index, count);
		float num = 0f;
		int index2 = (int)MathHelper.Min(index + count, Keyframes.Count - 1);
		num = Keyframes[index2].Frame - Keyframes[index].Frame;
		return pathSegmentLength / num;
	}

	public void SetPathSegmentSpeed(float speed, int index, int count = 1)
	{
		for (int i = index; i < index + count && i < SegmentLengths.Length; i++)
		{
			float num = ((i == 0) ? 0f : Keyframes[i].Frame);
			float pathSegmentLength = GetPathSegmentLength(i);
			float num2 = MathHelper.Max(pathSegmentLength / speed, 1f);
			Keyframes[i + 1].Frame = (float)System.Math.Round(num + num2);
		}
		UpdateKeyframeData();
	}

	public void ScalePathSpeed(float scale)
	{
		scale = 1f / scale;
		List<float> list = new List<float>();
		for (int i = 1; i < Keyframes.Count; i++)
		{
			float frame = Keyframes[i - 1].Frame;
			float frame2 = Keyframes[i].Frame;
			float num = frame2 - frame;
			list.Add(num * scale);
		}
		for (int j = 1; j < Keyframes.Count; j++)
		{
			Keyframes[j].Frame = Keyframes[j - 1].Frame + list[j - 1];
		}
		UpdateKeyframeData();
	}

	public void RotatePath(Vector3 rotation, Vector3 origin)
	{
		if (origin.IsNaN())
		{
			origin = GetStartingPosition();
		}
		Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
		Matrix.CreateFromQuaternion(ref quaternion, out _tempMatrix);
		for (int i = 0; i < Keyframes.Count; i++)
		{
			KeyframeSetting<Vector3[]> setting = Keyframes[i].GetSetting<Vector3[]>("Curve");
			if (setting != null && setting.Value != null)
			{
				Vector3 value = Keyframes[i].GetSetting<Vector3>("Position").Value;
				Vector3 vector = Vector3.Transform(value - origin, _tempMatrix) + origin;
				for (int j = 0; j < setting.Value.Length; j++)
				{
					Vector3 vector2 = Vector3.Transform(setting.Value[j] + value - origin, _tempMatrix);
					setting.Value[j] = vector2 + origin - vector;
				}
			}
			KeyframeSetting<Vector3> setting2 = Keyframes[i].GetSetting<Vector3>("Position");
			if (setting2 != null)
			{
				_ = setting2.Value;
				if (true)
				{
					Vector3 vector3 = Vector3.Transform(setting2.Value - origin, _tempMatrix);
					setting2.Value = vector3 + origin;
				}
			}
			KeyframeSetting<Vector3> setting3 = Keyframes[i].GetSetting<Vector3>("Rotation");
			if (setting3 != null)
			{
				_ = setting3.Value;
				if (true)
				{
					Vector3 value2 = setting3.Value;
					value2.Y = MathHelper.WrapAngle(value2.Y + rotation.Y);
					setting3.Value = value2;
				}
			}
		}
		UpdateKeyframeData();
	}

	public void SmoothBezierPath()
	{
		if (!(Path is BezierPath))
		{
			return;
		}
		if (Keyframes.Count < 3)
		{
			if (Keyframes.Count == 1)
			{
				Keyframes[0].AddSetting(new CurveSetting(new Vector3[2]
				{
					Vector3.Zero,
					Vector3.Zero
				}));
			}
			else if (Keyframes.Count == 2)
			{
				Vector3 vector = Keyframes[1].GetSetting<Vector3>("Position").Value - Keyframes[0].GetSetting<Vector3>("Position").Value;
				Keyframes[0].AddSetting(new CurveSetting(new Vector3[2]
				{
					vector * 0.33f,
					vector * 0.67f
				}));
				Keyframes[1].AddSetting(new CurveSetting(new Vector3[2]
				{
					Vector3.Zero,
					Vector3.Zero
				}));
			}
		}
		else
		{
			Vector3[] array = new Vector3[Keyframes.Count];
			for (int i = 0; i < Keyframes.Count; i++)
			{
				array[i] = Keyframes[i].GetSetting<Vector3>("Position").Value;
			}
			BezierPath.GetCurveControlPoints(array, out var firstControlPoints, out var secondControlPoints);
			for (int j = 0; j < Keyframes.Count - 1; j++)
			{
				KeyframeSetting<Vector3[]> setting = Keyframes[j].GetSetting<Vector3[]>("Curve");
				Vector3 value = Keyframes[j].GetSetting<Vector3>("Position").Value;
				setting.Value[0] = firstControlPoints[j] - value;
				setting.Value[1] = secondControlPoints[j] - value;
			}
			UpdateKeyframeData();
		}
	}

	public void AddParticleSystem(ParticleSystemProxy particleSystem)
	{
		_particleSystemProxies.Add(particleSystem);
	}

	public void ClearParticles()
	{
		foreach (ParticleSystemProxy particleSystemProxy in _particleSystemProxies)
		{
			if (particleSystemProxy != null && !particleSystemProxy.IsExpired)
			{
				particleSystemProxy.Expire();
			}
		}
		_particleSystemProxies.Clear();
	}
}
