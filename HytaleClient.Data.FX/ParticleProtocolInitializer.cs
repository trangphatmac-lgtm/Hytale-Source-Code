using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data.FX;

internal class ParticleProtocolInitializer
{
	public static void Initialize(BlockParticleSet networkBlockParticleSet, ref ClientBlockParticleSet clientBlockParticleSet)
	{
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Expected I4, but got Unknown
		if (networkBlockParticleSet.Color_ != null)
		{
			clientBlockParticleSet.Color = UInt32Color.FromRGBA((byte)networkBlockParticleSet.Color_.Red, (byte)networkBlockParticleSet.Color_.Green, (byte)networkBlockParticleSet.Color_.Blue, byte.MaxValue);
		}
		else
		{
			clientBlockParticleSet.Color = ParticleSettings.DefaultColor;
		}
		clientBlockParticleSet.Scale = networkBlockParticleSet.Scale;
		if (networkBlockParticleSet.PositionOffset != null)
		{
			clientBlockParticleSet.PositionOffset.X = networkBlockParticleSet.PositionOffset.X;
			clientBlockParticleSet.PositionOffset.Y = networkBlockParticleSet.PositionOffset.Y;
			clientBlockParticleSet.PositionOffset.Z = networkBlockParticleSet.PositionOffset.Z;
		}
		if (networkBlockParticleSet.RotationOffset != null)
		{
			clientBlockParticleSet.RotationOffset = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(networkBlockParticleSet.RotationOffset.Yaw), MathHelper.ToRadians(networkBlockParticleSet.RotationOffset.Pitch), MathHelper.ToRadians(networkBlockParticleSet.RotationOffset.Roll));
		}
		else
		{
			clientBlockParticleSet.RotationOffset = Quaternion.Identity;
		}
		if (networkBlockParticleSet.ParticleSystemIds == null)
		{
			return;
		}
		clientBlockParticleSet.ParticleSystemIds = new Dictionary<ClientBlockParticleEvent, string>();
		foreach (KeyValuePair<BlockParticleEvent, string> particleSystemId in networkBlockParticleSet.ParticleSystemIds)
		{
			clientBlockParticleSet.ParticleSystemIds[(ClientBlockParticleEvent)particleSystemId.Key] = particleSystemId.Value;
		}
	}

	public static void Initialize(ModelParticle networkParticle, ref ModelParticleSettings clientModelParticle, NodeNameManager nodeNameManager)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		clientModelParticle.SystemId = networkParticle.SystemId;
		clientModelParticle.DetachedFromModel = networkParticle.DetachedFromModel;
		if (networkParticle.Color_ != null)
		{
			clientModelParticle.Color = UInt32Color.FromRGBA((byte)networkParticle.Color_.Red, (byte)networkParticle.Color_.Green, (byte)networkParticle.Color_.Blue, byte.MaxValue);
		}
		clientModelParticle.Scale = networkParticle.Scale;
		clientModelParticle.TargetEntityPart = networkParticle.TargetEntityPart;
		if (networkParticle.TargetNodeName != null)
		{
			clientModelParticle.TargetNodeNameId = nodeNameManager.GetOrAddNameId(networkParticle.TargetNodeName);
		}
		if (networkParticle.PositionOffset != null)
		{
			clientModelParticle.PositionOffset.X = networkParticle.PositionOffset.X;
			clientModelParticle.PositionOffset.Y = networkParticle.PositionOffset.Y;
			clientModelParticle.PositionOffset.Z = networkParticle.PositionOffset.Z;
		}
		if (networkParticle.RotationOffset != null)
		{
			clientModelParticle.RotationOffset = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(networkParticle.RotationOffset.Yaw), MathHelper.ToRadians(networkParticle.RotationOffset.Pitch), MathHelper.ToRadians(networkParticle.RotationOffset.Roll));
		}
	}

	public static void Initialize(ModelParticle[] networkParticles, out ModelParticleSettings[] clientModelParticles, NodeNameManager nodeNameManager)
	{
		if (networkParticles == null || networkParticles.Length == 0)
		{
			clientModelParticles = null;
			return;
		}
		clientModelParticles = new ModelParticleSettings[networkParticles.Length];
		for (int i = 0; i < networkParticles.Length; i++)
		{
			ModelParticleSettings clientModelParticle = new ModelParticleSettings();
			Initialize(networkParticles[i], ref clientModelParticle, nodeNameManager);
			clientModelParticles[i] = clientModelParticle;
		}
	}

	public static void Initialize(Particle particle, ParticleRotationInfluence rotationInfluence, ParticleRotationInfluence collisionRotationInfluence, ref ParticleSettings clientParticle)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected I4, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected I4, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected I4, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Invalid comparison between Unknown and I4
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Invalid comparison between Unknown and I4
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Invalid comparison between Unknown and I4
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		clientParticle.TexturePath = particle.TexturePath;
		if (particle.FrameSize != null)
		{
			clientParticle.FrameSize.X = (ushort)particle.FrameSize.Width;
			clientParticle.FrameSize.Y = (ushort)particle.FrameSize.Height;
		}
		clientParticle.UVOption = (ParticleSettings.UVOptions)particle.UvOption;
		clientParticle.ScaleRatio = (ParticleSettings.ScaleRatioConstraint)particle.ScaleRatioConstraint;
		clientParticle.SoftParticlesOption = (ParticleSettings.SoftParticles)particle.SoftParticles;
		clientParticle.SoftParticlesFadeFactor = particle.SoftParticlesFadeFactor;
		clientParticle.UseSpriteBlending = particle.UseSpriteBlending;
		bool flag = (int)rotationInfluence == 1 || (int)rotationInfluence == 2 || (int)rotationInfluence == 3;
		bool isBillboard = (int)collisionRotationInfluence == 1 || (int)collisionRotationInfluence == 2 || (int)collisionRotationInfluence == 3;
		Vector2 vector = Vector2.Zero;
		Vector2 vector2 = Vector2.Zero;
		Vector2 vector3 = Vector2.Zero;
		bool flag2 = false;
		Vector2 vector4 = ParticleSettings.DefaultScale;
		Vector2 vector5 = ParticleSettings.DefaultScale;
		bool flag3 = false;
		float num = 1f;
		bool flag4 = false;
		Color val = null;
		bool flag5 = false;
		ByteVector2 byteVector = new ByteVector2(0, 0);
		bool flag6 = false;
		if (particle.InitialAnimationFrame != null)
		{
			if (particle.InitialAnimationFrame.Rotation != null)
			{
				if (particle.InitialAnimationFrame.Rotation.X != null && !flag)
				{
					vector = CreateRotationFrame(particle.InitialAnimationFrame.Rotation.X);
				}
				if (particle.InitialAnimationFrame.Rotation.Y != null && !flag)
				{
					vector2 = CreateRotationFrame(particle.InitialAnimationFrame.Rotation.Y);
				}
				if (particle.InitialAnimationFrame.Rotation.Z != null)
				{
					vector3 = CreateRotationFrame(particle.InitialAnimationFrame.Rotation.Z);
				}
			}
			if (particle.InitialAnimationFrame.Scale?.X != null)
			{
				vector4 = ConversionHelper.RangeToVector2(particle.InitialAnimationFrame.Scale.X);
			}
			if (particle.InitialAnimationFrame.Scale?.Y != null)
			{
				vector5 = ConversionHelper.RangeToVector2(particle.InitialAnimationFrame.Scale.Y);
			}
			val = particle.InitialAnimationFrame.Color_;
			if (particle.InitialAnimationFrame.Opacity != -1f)
			{
				num = particle.InitialAnimationFrame.Opacity;
			}
			if (particle.InitialAnimationFrame.FrameIndex != null)
			{
				byteVector = ConversionHelper.RangeToByteVector2(particle.InitialAnimationFrame.FrameIndex);
			}
		}
		if (particle.AnimationFrames == null)
		{
			return;
		}
		List<ParticleSettings.ScaleKeyframe> list = new List<ParticleSettings.ScaleKeyframe>(10);
		List<ParticleSettings.RotationKeyframe> list2 = new List<ParticleSettings.RotationKeyframe>(10);
		List<ParticleSettings.RangeKeyframe> list3 = new List<ParticleSettings.RangeKeyframe>(10);
		List<ParticleSettings.ColorKeyframe> list4 = new List<ParticleSettings.ColorKeyframe>(10);
		List<ParticleSettings.OpacityKeyframe> list5 = new List<ParticleSettings.OpacityKeyframe>(10);
		int[] array = particle.AnimationFrames.Keys.ToArray();
		ParticleAnimationFrame[] array2 = particle.AnimationFrames.Values.ToArray();
		Array.Sort(array, array2, 0, array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = array[i];
			ParticleAnimationFrame val2 = array2[i];
			if (num2 >= 0 && num2 <= 100)
			{
				if (val2.FrameIndex != null)
				{
					flag6 = flag6 || num2 == 0;
					ByteVector2 byteVector2 = ConversionHelper.RangeToByteVector2(val2.FrameIndex);
					list3.Add(new ParticleSettings.RangeKeyframe
					{
						Time = (byte)num2,
						Min = byteVector2.X,
						Max = byteVector2.Y
					});
				}
				if (val2.Scale != null)
				{
					flag3 = flag3 || num2 == 0;
					ParticleSettings.ScaleKeyframe item = CreateScaleKeyframe((byte)num2, val2.Scale);
					item.Min.X *= vector4.X;
					item.Min.Y *= vector5.X;
					item.Max.X *= vector4.Y;
					item.Max.Y *= vector5.Y;
					Sort(ref item.Min.X, ref item.Max.X);
					Sort(ref item.Min.Y, ref item.Max.Y);
					list.Add(item);
				}
				if (val2.Rotation != null)
				{
					flag2 = flag2 || num2 == 0;
					ParticleSettings.RotationKeyframe item2 = CreateRotationKeyframe((byte)num2, val2.Rotation, flag);
					item2.Min.X += vector.X;
					item2.Min.Y += vector2.X;
					item2.Min.Z += vector3.X;
					item2.Max.X += vector.Y;
					item2.Max.Y += vector2.Y;
					item2.Max.Z += vector3.Y;
					Sort(ref item2.Min.X, ref item2.Max.X);
					Sort(ref item2.Min.Y, ref item2.Max.Y);
					Sort(ref item2.Min.Z, ref item2.Max.Z);
					list2.Add(item2);
				}
				if (val2.Color_ != null)
				{
					flag5 = flag5 || num2 == 0;
					ParticleSettings.ColorKeyframe item3 = CreateColorKeyframe((byte)num2, val2.Color_);
					list4.Add(item3);
				}
				if (val2.Opacity != -1f)
				{
					flag4 = flag4 || num2 == 0;
					list5.Add(new ParticleSettings.OpacityKeyframe
					{
						Time = (byte)num2,
						Opacity = MathHelper.Clamp(val2.Opacity * num, 0f, 1f)
					});
				}
			}
		}
		if (particle.CollisionAnimationFrame != null)
		{
			if (particle.CollisionAnimationFrame.Scale != null)
			{
				ParticleSettings.ScaleKeyframe item4 = CreateScaleKeyframe(101, particle.CollisionAnimationFrame.Scale);
				Sort(ref item4.Min.X, ref item4.Max.X);
				Sort(ref item4.Min.Y, ref item4.Max.Y);
				list.Add(item4);
			}
			if (particle.CollisionAnimationFrame.Rotation != null)
			{
				ParticleSettings.RotationKeyframe item5 = CreateRotationKeyframe(101, particle.CollisionAnimationFrame.Rotation, isBillboard);
				Sort(ref item5.Min.X, ref item5.Max.X);
				Sort(ref item5.Min.Y, ref item5.Max.Y);
				Sort(ref item5.Min.Z, ref item5.Max.Z);
				list2.Add(item5);
			}
			if (particle.CollisionAnimationFrame.Opacity != -1f)
			{
				list5.Add(new ParticleSettings.OpacityKeyframe
				{
					Time = 101,
					Opacity = MathHelper.Clamp(particle.CollisionAnimationFrame.Opacity, 0f, 1f)
				});
			}
			if (particle.CollisionAnimationFrame.Color_ != null)
			{
				ParticleSettings.ColorKeyframe item6 = CreateColorKeyframe(101, particle.CollisionAnimationFrame.Color_);
				list4.Add(item6);
			}
			if (particle.CollisionAnimationFrame.FrameIndex != null)
			{
				ByteVector2 byteVector3 = ConversionHelper.RangeToByteVector2(particle.CollisionAnimationFrame.FrameIndex);
				list3.Add(new ParticleSettings.RangeKeyframe
				{
					Time = 101,
					Min = byteVector3.X,
					Max = byteVector3.Y
				});
			}
		}
		if (list.Count == 0 || !flag3)
		{
			ParticleSettings.ScaleKeyframe item7;
			if (particle.InitialAnimationFrame?.Scale != null)
			{
				item7 = CreateScaleKeyframe(0, particle.InitialAnimationFrame.Scale);
				Sort(ref item7.Min.X, ref item7.Max.X);
				Sort(ref item7.Min.Y, ref item7.Max.Y);
			}
			else
			{
				ParticleSettings.ScaleKeyframe scaleKeyframe = default(ParticleSettings.ScaleKeyframe);
				scaleKeyframe.Time = 0;
				scaleKeyframe.Min = ParticleSettings.DefaultScale * (1f / 32f);
				scaleKeyframe.Max = ParticleSettings.DefaultScale * (1f / 32f);
				item7 = scaleKeyframe;
			}
			list.Insert(0, item7);
		}
		clientParticle.ScaleKeyframes = list.ToArray();
		for (int num3 = clientParticle.ScaleKeyframes.Length - 1; num3 >= 0; num3--)
		{
			ref ParticleSettings.ScaleKeyframe reference = ref clientParticle.ScaleKeyframes[num3];
			if (reference.Time <= 100)
			{
				clientParticle.ScaleKeyFrameCount++;
				if (reference.Time > 0 && num3 != 0)
				{
					ref ParticleSettings.ScaleKeyframe reference2 = ref clientParticle.ScaleKeyframes[num3 - 1];
					reference.Min.X = reference.Min.X - reference2.Min.X;
					reference.Min.Y = reference.Min.Y - reference2.Min.Y;
					reference.Max.X = reference.Max.X - reference2.Max.X;
					reference.Max.Y = reference.Max.Y - reference2.Max.Y;
				}
			}
		}
		if (particle.InitialAnimationFrame?.Rotation != null && !flag2)
		{
			ParticleSettings.RotationKeyframe item8 = CreateRotationKeyframe(0, particle.InitialAnimationFrame.Rotation, flag);
			list2.Insert(0, item8);
		}
		clientParticle.RotationKeyframes = list2.ToArray();
		for (int num4 = clientParticle.RotationKeyframes.Length - 1; num4 >= 0; num4--)
		{
			ref ParticleSettings.RotationKeyframe reference3 = ref clientParticle.RotationKeyframes[num4];
			if (reference3.Time <= 100)
			{
				clientParticle.RotationKeyFrameCount++;
				if (reference3.Time > 0 && num4 != 0)
				{
					ref ParticleSettings.RotationKeyframe reference4 = ref clientParticle.RotationKeyframes[num4 - 1];
					reference3.Min.X = reference3.Min.X - reference4.Min.X;
					reference3.Min.Y = reference3.Min.Y - reference4.Min.Y;
					reference3.Min.Z = reference3.Min.Z - reference4.Min.Z;
					reference3.Max.X = reference3.Max.X - reference4.Max.X;
					reference3.Max.Y = reference3.Max.Y - reference4.Max.Y;
					reference3.Max.Z = reference3.Max.Z - reference4.Max.Z;
				}
			}
		}
		if (!flag6)
		{
			list3.Insert(0, new ParticleSettings.RangeKeyframe
			{
				Time = 0,
				Min = byteVector.X,
				Max = byteVector.Y
			});
		}
		clientParticle.TextureIndexKeyFrames = list3.ToArray();
		for (int j = 0; j < clientParticle.TextureIndexKeyFrames.Length; j++)
		{
			if (clientParticle.TextureIndexKeyFrames[j].Time <= 100)
			{
				clientParticle.TextureKeyFrameCount++;
			}
		}
		if (val != null && !flag5)
		{
			ParticleSettings.ColorKeyframe item9 = CreateColorKeyframe(0, val);
			list4.Insert(0, item9);
		}
		clientParticle.ColorKeyframes = list4.ToArray();
		for (int k = 0; k < clientParticle.ColorKeyframes.Length; k++)
		{
			if (clientParticle.ColorKeyframes[k].Time <= 100)
			{
				clientParticle.ColorKeyFrameCount++;
			}
		}
		if (num != 1f && !flag4)
		{
			list5.Insert(0, new ParticleSettings.OpacityKeyframe
			{
				Time = 0,
				Opacity = num
			});
		}
		clientParticle.OpacityKeyframes = list5.ToArray();
		for (int l = 0; l < clientParticle.OpacityKeyframes.Length; l++)
		{
			if (clientParticle.OpacityKeyframes[l].Time <= 100)
			{
				clientParticle.OpacityKeyFrameCount++;
			}
		}
	}

	public static void Initialize(ParticleSpawner particleSpawner, ref ParticleSpawnerSettings clientParticleSpawner)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected I4, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected I4, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected I4, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected I4, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected I4, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Expected I4, but got Unknown
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Expected I4, but got Unknown
		FXRenderMode renderMode = particleSpawner.RenderMode;
		FXRenderMode val = renderMode;
		switch ((int)val)
		{
		case 0:
			clientParticleSpawner.RenderMode = FXSystem.RenderMode.BlendLinear;
			break;
		case 1:
			clientParticleSpawner.RenderMode = FXSystem.RenderMode.BlendAdd;
			break;
		case 2:
			clientParticleSpawner.RenderMode = FXSystem.RenderMode.Erosion;
			break;
		case 3:
			clientParticleSpawner.RenderMode = FXSystem.RenderMode.Distortion;
			break;
		}
		clientParticleSpawner.RotationInfluence = (ParticleFXSystem.ParticleRotationInfluence)particleSpawner.ParticleRotationInfluence_;
		clientParticleSpawner.ParticleRotateWithSpawner = particleSpawner.ParticleRotateWithSpawner;
		clientParticleSpawner.TrailSpawnerPositionMultiplier = particleSpawner.TrailSpawnerPositionMultiplier;
		clientParticleSpawner.TrailSpawnerRotationMultiplier = particleSpawner.TrailSpawnerRotationMultiplier;
		if (particleSpawner.ParticleCollision_ != null)
		{
			clientParticleSpawner.ParticleCollisionBlockType = (ParticleFXSystem.ParticleCollisionBlockType)particleSpawner.ParticleCollision_.BlockType;
			clientParticleSpawner.ParticleCollisionAction = (ParticleFXSystem.ParticleCollisionAction)particleSpawner.ParticleCollision_.Action;
			clientParticleSpawner.ParticleCollisionRotationInfluence = (ParticleFXSystem.ParticleRotationInfluence)particleSpawner.ParticleCollision_.ParticleRotationInfluence_;
		}
		clientParticleSpawner.LinearFiltering = particleSpawner.LinearFiltering;
		clientParticleSpawner.IsLowRes = particleSpawner.IsLowRes;
		UVMotion uvMotion_ = particleSpawner.UvMotion_;
		if (uvMotion_ != null)
		{
			clientParticleSpawner.UVMotion.TexturePath = uvMotion_.Texture;
			clientParticleSpawner.UVMotion.AddRandomUVOffset = uvMotion_.AddRandomUVOffset;
			clientParticleSpawner.UVMotion.Speed = new Vector2(uvMotion_.SpeedX, uvMotion_.SpeedY);
			clientParticleSpawner.UVMotion.Strength = uvMotion_.Strength;
			clientParticleSpawner.UVMotion.StrengthCurveType = (ParticleSpawnerSettings.UVMotionCurveType)uvMotion_.StrengthCurveType;
			clientParticleSpawner.UVMotion.Scale = uvMotion_.Scale;
		}
		if (particleSpawner.IntersectionHighlight_ != null && particleSpawner.IntersectionHighlight_.HighlightColor != null)
		{
			clientParticleSpawner.IntersectionHighlight.Color = new Vector3((float)(int)(byte)particleSpawner.IntersectionHighlight_.HighlightColor.Red / 255f, (float)(int)(byte)particleSpawner.IntersectionHighlight_.HighlightColor.Green / 255f, (float)(int)(byte)particleSpawner.IntersectionHighlight_.HighlightColor.Blue / 255f);
			clientParticleSpawner.IntersectionHighlight.Threshold = particleSpawner.IntersectionHighlight_.HighlightThreshold;
		}
		clientParticleSpawner.CameraOffset = particleSpawner.CameraOffset;
		clientParticleSpawner.VelocityStretchMultiplier = particleSpawner.VelocityStretchMultiplier;
		clientParticleSpawner.LightInfluence = particleSpawner.LightInfluence;
		clientParticleSpawner.LifeSpan = particleSpawner.LifeSpan;
		if (particleSpawner.TotalParticles != null)
		{
			clientParticleSpawner.TotalParticles = ConversionHelper.RangeToPoint(particleSpawner.TotalParticles);
		}
		if (particleSpawner.MaxConcurrentParticles > 0 && particleSpawner.MaxConcurrentParticles < 512)
		{
			clientParticleSpawner.MaxConcurrentParticles = particleSpawner.MaxConcurrentParticles;
		}
		if (particleSpawner.ParticleLifeSpan != null)
		{
			clientParticleSpawner.ParticleLifeSpan = ConversionHelper.RangeToVector2(particleSpawner.ParticleLifeSpan);
		}
		if (particleSpawner.SpawnRate != null)
		{
			clientParticleSpawner.SpawnRate = ConversionHelper.RangeToVector2(particleSpawner.SpawnRate);
		}
		clientParticleSpawner.SpawnBurst = particleSpawner.SpawnBurst;
		if (!clientParticleSpawner.SpawnBurst)
		{
			clientParticleSpawner.MaxConcurrentParticles = (int)System.Math.Min(System.Math.Ceiling(clientParticleSpawner.SpawnRate.Y * clientParticleSpawner.ParticleLifeSpan.Y), clientParticleSpawner.MaxConcurrentParticles);
		}
		if (particleSpawner.WaveDelay != null)
		{
			clientParticleSpawner.WaveDelay = ConversionHelper.RangeToVector2(particleSpawner.WaveDelay);
		}
		InitialVelocity initialVelocity_ = particleSpawner.InitialVelocity_;
		if (initialVelocity_ != null)
		{
			Vector2 vector = ((initialVelocity_.Yaw == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(initialVelocity_.Yaw));
			Vector2 vector2 = ((initialVelocity_.Pitch == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(initialVelocity_.Pitch));
			Vector2 vector3 = ((initialVelocity_.Speed == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(initialVelocity_.Speed));
			clientParticleSpawner.InitialVelocityMin = new ParticleSpawnerSettings.InitialVelocity(MathHelper.ToRadians(vector.X), MathHelper.ToRadians(vector2.X), vector3.X);
			clientParticleSpawner.InitialVelocityMax = new ParticleSpawnerSettings.InitialVelocity(MathHelper.ToRadians(vector.Y), MathHelper.ToRadians(vector2.Y), vector3.Y);
			clientParticleSpawner.InitialVelocityMin.Speed *= 1f / 60f;
			clientParticleSpawner.InitialVelocityMax.Speed *= 1f / 60f;
		}
		clientParticleSpawner.EmitShape = (ParticleSpawnerSettings.Shape)particleSpawner.Shape;
		RangeVector3f emitOffset = particleSpawner.EmitOffset;
		if (emitOffset != null)
		{
			Vector2 vector4 = ((emitOffset.X == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(emitOffset.X));
			Vector2 vector5 = ((emitOffset.Y == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(emitOffset.Y));
			Vector2 vector6 = ((emitOffset.Z == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(emitOffset.Z));
			clientParticleSpawner.EmitOffsetMin = new Vector3(vector4.X, vector5.X, vector6.X);
			clientParticleSpawner.EmitOffsetMax = new Vector3(vector4.Y, vector5.Y, vector6.Y);
		}
		clientParticleSpawner.UseEmitDirection = particleSpawner.UseEmitDirection;
		if (particleSpawner.Attractors != null)
		{
			clientParticleSpawner.Attractors = new ParticleAttractor[particleSpawner.Attractors.Length];
			for (int i = 0; i < particleSpawner.Attractors.Length; i++)
			{
				ParticleAttractor val2 = particleSpawner.Attractors[i];
				ParticleAttractor particleAttractor = default(ParticleAttractor);
				particleAttractor.Position = ((val2.Position != null) ? new Vector3(val2.Position.X, val2.Position.Y, val2.Position.Z) : Vector3.Zero);
				particleAttractor.RadialAxis = ((val2.RadialAxis != null) ? new Vector3(val2.RadialAxis.X, val2.RadialAxis.Y, val2.RadialAxis.Z) : Vector3.Zero);
				particleAttractor.TrailPositionMultiplier = val2.TrailPositionMultiplier;
				particleAttractor.RadialAcceleration = val2.RadialAcceleration;
				particleAttractor.RadialTangentAcceleration = val2.RadialTangentAcceleration;
				particleAttractor.LinearAcceleration = ((val2.LinearAcceleration != null) ? new Vector3(val2.LinearAcceleration.X, val2.LinearAcceleration.Y, val2.LinearAcceleration.Z) : Vector3.Zero);
				particleAttractor.RadialImpulse = val2.RadialImpulse;
				particleAttractor.RadialTangentImpulse = val2.RadialTangentImpulse;
				particleAttractor.LinearImpulse = ((val2.LinearImpulse != null) ? new Vector3(val2.LinearImpulse.X, val2.LinearImpulse.Y, val2.LinearImpulse.Z) : Vector3.Zero);
				particleAttractor.Radius = val2.Radius;
				particleAttractor.DampingMultiplier = ((val2.DampingMultiplier != null) ? new Vector3(val2.DampingMultiplier.X, val2.DampingMultiplier.Y, val2.DampingMultiplier.Z) : Vector3.One);
				ParticleAttractor particleAttractor2 = particleAttractor;
				if (particleAttractor2.RadialAxis != Vector3.Zero)
				{
					particleAttractor2.RadialAxis = Vector3.Normalize(particleAttractor2.RadialAxis);
				}
				particleAttractor2.LinearImpulse *= 1f / 60f;
				particleAttractor2.RadialImpulse *= 1f / 60f;
				particleAttractor2.RadialTangentImpulse *= 1f / 60f;
				particleAttractor2.DampingMultiplier.X = (float)System.Math.Pow(particleAttractor2.DampingMultiplier.X, 0.1666666716337204);
				particleAttractor2.DampingMultiplier.Y = (float)System.Math.Pow(particleAttractor2.DampingMultiplier.Y, 0.1666666716337204);
				particleAttractor2.DampingMultiplier.Z = (float)System.Math.Pow(particleAttractor2.DampingMultiplier.Z, 0.1666666716337204);
				particleAttractor2.LinearAcceleration *= 0.0002777778f;
				particleAttractor2.RadialAcceleration *= 0.0002777778f;
				particleAttractor2.RadialTangentAcceleration *= 0.0002777778f;
				clientParticleSpawner.Attractors[i] = particleAttractor2;
			}
		}
		if (clientParticleSpawner.EmitShape == ParticleSpawnerSettings.Shape.Sphere && ((clientParticleSpawner.EmitOffsetMin.X == 0f && clientParticleSpawner.EmitOffsetMax.X == 0f) || (clientParticleSpawner.EmitOffsetMin.Y == 0f && clientParticleSpawner.EmitOffsetMax.Y == 0f) || (clientParticleSpawner.EmitOffsetMin.Z == 0f && clientParticleSpawner.EmitOffsetMax.Z == 0f)))
		{
			clientParticleSpawner.EmitShape = ParticleSpawnerSettings.Shape.Circle;
		}
		if (clientParticleSpawner.EmitShape == ParticleSpawnerSettings.Shape.Cube && clientParticleSpawner.EmitOffsetMin.X == 0f && clientParticleSpawner.EmitOffsetMin.Y == 0f && clientParticleSpawner.EmitOffsetMin.Z == 0f)
		{
			clientParticleSpawner.EmitShape = ParticleSpawnerSettings.Shape.FullCube;
		}
	}

	public static void Initialize(ParticleSystem particleSystem, ref ParticleSystemSettings clientParticleSystem)
	{
		if (particleSystem.LifeSpan > 0f)
		{
			clientParticleSystem.LifeSpan = particleSystem.LifeSpan;
		}
		clientParticleSystem.CullDistanceSquared = ((particleSystem.CullDistance >= 1f) ? (particleSystem.CullDistance * particleSystem.CullDistance) : 1600f);
		clientParticleSystem.BoundingRadius = ((particleSystem.BoundingRadius >= 1f) ? particleSystem.BoundingRadius : 10f);
		clientParticleSystem.IsImportant = particleSystem.IsImportant;
		ParticleSpawnerGroup[] spawners = particleSystem.Spawners;
		int num = ((spawners != null) ? spawners.Length : 0);
		clientParticleSystem.CreateSpawnerSettingsStorage((byte)num);
		for (int i = 0; i < num; i++)
		{
			ParticleSpawnerGroup val = particleSystem.Spawners[i];
			ParticleSystemSettings.SystemSpawnerSettings systemSpawnerSettings = new ParticleSystemSettings.SystemSpawnerSettings();
			systemSpawnerSettings.ParticleSpawnerId = val.SpawnerId;
			if (val.PositionOffset != null)
			{
				systemSpawnerSettings.PositionOffset = new Vector3(val.PositionOffset.X, val.PositionOffset.Y, val.PositionOffset.Z);
			}
			if (val.RotationOffset != null)
			{
				systemSpawnerSettings.RotationOffset = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(val.RotationOffset.Yaw), MathHelper.ToRadians(val.RotationOffset.Pitch), MathHelper.ToRadians(val.RotationOffset.Roll));
			}
			systemSpawnerSettings.FixedRotation = val.FixedRotation;
			systemSpawnerSettings.StartDelay = val.StartDelay;
			if (val.SpawnRate != null)
			{
				systemSpawnerSettings.SpawnRate = ConversionHelper.RangeToVector2(val.SpawnRate);
			}
			Vector2 vector = ((systemSpawnerSettings.SpawnRate == ParticleSystemSettings.DefaultSpawnRate) ? ParticleSystemSettings.DefaultSingleSpawnerLifeSpan : ParticleSystemSettings.DefaultSpawnerLifeSpan);
			systemSpawnerSettings.LifeSpan = ((val.LifeSpan != null) ? ConversionHelper.RangeToVector2(val.LifeSpan) : vector);
			if (val.TotalSpawners != 0)
			{
				systemSpawnerSettings.TotalSpawners = val.TotalSpawners;
			}
			if (val.MaxConcurrent > 0 && systemSpawnerSettings.SpawnRate != ParticleSystemSettings.DefaultSpawnRate)
			{
				systemSpawnerSettings.MaxConcurrent = (int)MathHelper.Min(val.MaxConcurrent, 10f);
			}
			if (val.WaveDelay != null)
			{
				systemSpawnerSettings.WaveDelay = ConversionHelper.RangeToVector2(val.WaveDelay);
			}
			InitialVelocity initialVelocity_ = val.InitialVelocity_;
			if (initialVelocity_ != null)
			{
				Vector2 vector2 = ((initialVelocity_.Yaw == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(initialVelocity_.Yaw));
				Vector2 vector3 = ((initialVelocity_.Pitch == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(initialVelocity_.Pitch));
				Vector2 vector4 = ((initialVelocity_.Speed == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(initialVelocity_.Speed));
				systemSpawnerSettings.InitialVelocityMin = new ParticleSpawnerSettings.InitialVelocity(MathHelper.ToRadians(vector2.X), MathHelper.ToRadians(vector3.X), vector4.X);
				systemSpawnerSettings.InitialVelocityMax = new ParticleSpawnerSettings.InitialVelocity(MathHelper.ToRadians(vector2.Y), MathHelper.ToRadians(vector3.Y), vector4.Y);
				systemSpawnerSettings.InitialVelocityMin.Speed *= 1f / 60f;
				systemSpawnerSettings.InitialVelocityMax.Speed *= 1f / 60f;
			}
			RangeVector3f emitOffset = val.EmitOffset;
			if (emitOffset != null)
			{
				Vector2 vector5 = ((emitOffset.X == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(emitOffset.X));
				Vector2 vector6 = ((emitOffset.Y == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(emitOffset.Y));
				Vector2 vector7 = ((emitOffset.Z == null) ? Vector2.Zero : ConversionHelper.RangeToVector2(emitOffset.Z));
				systemSpawnerSettings.EmitOffsetMin = new Vector3(vector5.X, vector6.X, vector7.X);
				systemSpawnerSettings.EmitOffsetMax = new Vector3(vector5.Y, vector6.Y, vector7.Y);
			}
			if (val.Attractors != null)
			{
				systemSpawnerSettings.Attractors = new ParticleAttractor[val.Attractors.Length];
				for (int j = 0; j < val.Attractors.Length; j++)
				{
					ParticleAttractor val2 = val.Attractors[j];
					ParticleAttractor particleAttractor = default(ParticleAttractor);
					particleAttractor.Position = ((val2.Position != null) ? new Vector3(val2.Position.X, val2.Position.Y, val2.Position.Z) : Vector3.Zero);
					particleAttractor.RadialAxis = ((val2.RadialAxis != null) ? new Vector3(val2.RadialAxis.X, val2.RadialAxis.Y, val2.RadialAxis.Z) : Vector3.Zero);
					particleAttractor.TrailPositionMultiplier = val2.TrailPositionMultiplier;
					particleAttractor.RadialAcceleration = val2.RadialAcceleration;
					particleAttractor.RadialTangentAcceleration = val2.RadialTangentAcceleration;
					particleAttractor.LinearAcceleration = ((val2.LinearAcceleration != null) ? new Vector3(val2.LinearAcceleration.X, val2.LinearAcceleration.Y, val2.LinearAcceleration.Z) : Vector3.Zero);
					particleAttractor.RadialImpulse = val2.RadialImpulse;
					particleAttractor.RadialTangentImpulse = val2.RadialTangentImpulse;
					particleAttractor.LinearImpulse = ((val2.LinearImpulse != null) ? new Vector3(val2.LinearImpulse.X, val2.LinearImpulse.Y, val2.LinearImpulse.Z) : Vector3.Zero);
					particleAttractor.Radius = val2.Radius;
					particleAttractor.DampingMultiplier = ((val2.DampingMultiplier != null) ? new Vector3(val2.DampingMultiplier.X, val2.DampingMultiplier.Y, val2.DampingMultiplier.Z) : Vector3.One);
					ParticleAttractor particleAttractor2 = particleAttractor;
					if (particleAttractor2.RadialAxis != Vector3.Zero)
					{
						particleAttractor2.RadialAxis = Vector3.Normalize(particleAttractor2.RadialAxis);
					}
					particleAttractor2.LinearImpulse *= 1f / 60f;
					particleAttractor2.RadialImpulse *= 1f / 60f;
					particleAttractor2.RadialTangentImpulse *= 1f / 60f;
					particleAttractor2.DampingMultiplier.X = (float)System.Math.Pow(particleAttractor2.DampingMultiplier.X, 0.1666666716337204);
					particleAttractor2.DampingMultiplier.Y = (float)System.Math.Pow(particleAttractor2.DampingMultiplier.Y, 0.1666666716337204);
					particleAttractor2.DampingMultiplier.Z = (float)System.Math.Pow(particleAttractor2.DampingMultiplier.Z, 0.1666666716337204);
					particleAttractor2.LinearAcceleration *= 0.0002777778f;
					particleAttractor2.RadialAcceleration *= 0.0002777778f;
					particleAttractor2.RadialTangentAcceleration *= 0.0002777778f;
					systemSpawnerSettings.Attractors[j] = particleAttractor2;
				}
			}
			clientParticleSystem.SystemSpawnerSettingsList[i] = systemSpawnerSettings;
		}
	}

	private static ParticleSettings.ScaleKeyframe CreateScaleKeyframe(byte time, RangeVector2f scale)
	{
		Vector2 vector = ((scale.X == null) ? ParticleSettings.DefaultScale : new Vector2(scale.X.Min, scale.X.Max));
		Vector2 vector2 = ((scale.Y == null) ? ParticleSettings.DefaultScale : new Vector2(scale.Y.Min, scale.Y.Max));
		ParticleSettings.ScaleKeyframe result = default(ParticleSettings.ScaleKeyframe);
		result.Time = time;
		result.Min = new Vector2(vector.X, vector2.X) * (1f / 32f);
		result.Max = new Vector2(vector.Y, vector2.Y) * (1f / 32f);
		return result;
	}

	private static Vector2 CreateRotationFrame(Rangef rotationRange)
	{
		Vector2 result = ConversionHelper.RangeToVector2(rotationRange);
		result.X = MathHelper.ToRadians(result.X);
		result.Y = MathHelper.ToRadians(result.Y);
		return result;
	}

	private static ParticleSettings.RotationKeyframe CreateRotationKeyframe(byte time, RangeVector3f rotation, bool isBillboard)
	{
		Vector2 vector = ((rotation.X == null || isBillboard) ? Vector2.Zero : new Vector2(MathHelper.ToRadians(rotation.X.Min), MathHelper.ToRadians(rotation.X.Max)));
		Vector2 vector2 = ((rotation.Y == null || isBillboard) ? Vector2.Zero : new Vector2(MathHelper.ToRadians(rotation.Y.Min), MathHelper.ToRadians(rotation.Y.Max)));
		Vector2 vector3 = ((rotation.Z == null) ? Vector2.Zero : new Vector2(MathHelper.ToRadians(rotation.Z.Min), MathHelper.ToRadians(rotation.Z.Max)));
		ParticleSettings.RotationKeyframe result = default(ParticleSettings.RotationKeyframe);
		result.Time = time;
		result.Min = new Vector3(vector.X, vector2.X, vector3.X);
		result.Max = new Vector3(vector.Y, vector2.Y, vector3.Y);
		return result;
	}

	private static ParticleSettings.ColorKeyframe CreateColorKeyframe(byte time, Color networkColor)
	{
		UInt32Color defaultColor = ParticleSettings.DefaultColor;
		defaultColor.SetR((byte)MathHelper.Clamp((byte)networkColor.Red, 0, 255));
		defaultColor.SetG((byte)MathHelper.Clamp((byte)networkColor.Green, 0, 255));
		defaultColor.SetB((byte)MathHelper.Clamp((byte)networkColor.Blue, 0, 255));
		ParticleSettings.ColorKeyframe result = default(ParticleSettings.ColorKeyframe);
		result.Time = time;
		result.Color = defaultColor;
		return result;
	}

	private static void Sort(ref float min, ref float max)
	{
		if (min > max)
		{
			float num = min;
			min = max;
			max = num;
		}
	}
}
