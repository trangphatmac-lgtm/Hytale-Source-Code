#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Graphics.Particles;

internal class ParticleSpawner
{
	private struct DrawData
	{
		public Vector4 StaticLightColorAndInfluence;

		public Quaternion InverseRotation;

		public Rectangle ImageLocation;

		public UShortVector2 FrameSize;

		public Vector4 UVMotion;

		public float UVMotionTextureId;

		public float AddRandomUVOffset;

		public int StrengthCurveType;

		public Vector4 IntersectionHighlight;

		public float CameraOffset;

		public float VelocityStretchMultiplier;

		public float SoftParticlesFadeFactor;
	}

	public const byte CollisionFrameTime = 101;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public Vector3 Position;

	public Quaternion Rotation = Quaternion.Identity;

	public UInt32Color DefaultColor;

	public bool IsOvergroundOnly = false;

	public bool IsFirstPerson = false;

	public bool IsPaused = false;

	private readonly ParticleSpawnerSettings _spawnerSettings;

	private readonly ParticleSettings _particleSettings;

	private readonly ParticleBuffers _particleBuffer;

	private ParticleFXSystem _particleFXSystem;

	private UpdateParticleCollisionFunc _updateCollisionFunc;

	private InitParticleFunc _initParticleFunc;

	private DrawData _drawData;

	private int _particleBufferStartIndex;

	private int _particleCount;

	private ushort _drawId;

	private int _particleVertexDataStartIndex;

	private int _particleDrawCount;

	private Vector3 _lastPosition;

	private Quaternion _lastRotation;

	private Random _random;

	private Vector2 _textureAltasInverseSize;

	private bool _useSpriteBlending = false;

	private bool _useSoftParticles = false;

	private int _particlesLeftToEmit;

	private int _activeParticles = 0;

	private float _particleCrumbs = 0f;

	private float _lifeSpanTimer;

	private int _particlesPerWave;

	private int _particlesInWave;

	private float _waveTimer;

	private readonly bool _hasWaves = false;

	private bool _waveEnded = false;

	private bool _wasFirstPerson = false;

	public int ParticleVertexDataStartIndex => _particleVertexDataStartIndex;

	public int ParticleDrawCount => _particleDrawCount;

	public FXSystem.RenderMode RenderMode => _spawnerSettings.RenderMode;

	public ParticleFXSystem.ParticleCollisionBlockType ParticleCollisionBlockType => _spawnerSettings.ParticleCollisionBlockType;

	public ParticleFXSystem.ParticleCollisionAction ParticleCollisionAction => _spawnerSettings.ParticleCollisionAction;

	public int ActiveParticles => _activeParticles;

	public float Scale { get; private set; } = 1f;


	public float ScaleFactor { get; private set; } = 1f;


	public float LightInfluence => _spawnerSettings.LightInfluence;

	public bool IsDistortion => _spawnerSettings.RenderMode == FXSystem.RenderMode.Distortion;

	public bool IsLowRes => _spawnerSettings.IsLowRes;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ComputeParticleScale(int particleIndex, int keyframeIndex, ref ParticleSettings.ScaleKeyframe scaleKeyframe, out Vector2 result)
	{
		float num = _random.NextFloat(scaleKeyframe.Min.X, scaleKeyframe.Max.X);
		float num2 = _random.NextFloat(scaleKeyframe.Min.Y, scaleKeyframe.Max.Y);
		if (_particleSettings.ScaleRatio != ParticleSettings.ScaleRatioConstraint.None)
		{
			ref float reference = ref _particleBuffer.ScaleRatio[particleIndex];
			if (keyframeIndex == 0)
			{
				reference = ((_particleSettings.ScaleRatio == ParticleSettings.ScaleRatioConstraint.Preserved) ? (num / num2) : 1f);
			}
			num2 = num * reference;
		}
		result = new Vector2(num, num2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ComputeParticleRotation(int rotationIdex, ref ParticleSettings.RotationKeyframe rotationKeyframe, out Vector3 result)
	{
		result.X = _random.NextFloat(rotationKeyframe.Min.X, rotationKeyframe.Max.X);
		result.Y = _random.NextFloat(rotationKeyframe.Min.Y, rotationKeyframe.Max.Y);
		result.Z = _random.NextFloat(rotationKeyframe.Min.Z, rotationKeyframe.Max.Z);
	}

	private void UpdateParticleAnimation(int particleIndex, float deltaTime)
	{
		ref ParticleBuffers.ParticleLifeData reference = ref _particleBuffer.Life[particleIndex];
		ref Vector2 reference2 = ref _particleBuffer.Scale[particleIndex];
		ref ParticleBuffers.ParticleSimulationData reference3 = ref _particleBuffer.Data0[particleIndex];
		ref ParticleBuffers.ParticleRenderData reference4 = ref _particleBuffer.Data1[particleIndex];
		float num = reference.LifeSpan - reference.LifeSpanTimer;
		UInt32Color uInt32Color = DefaultColor;
		UInt32Color uInt32Color2 = DefaultColor;
		float num2 = 1f;
		float value = 1f;
		if (BitUtils.IsBitOn(2, reference4.BoolData) && _spawnerSettings.ParticleCollisionAction == ParticleFXSystem.ParticleCollisionAction.LastFrame)
		{
			if (_particleSettings.ColorKeyframes.Length != 0)
			{
				reference4.Color = _particleSettings.ColorKeyframes[_particleSettings.ColorKeyframes.Length - 1].Color.ABGR;
			}
			if (_particleSettings.OpacityKeyframes.Length != 0)
			{
				reference4.Color |= (uint)(_particleSettings.OpacityKeyframes[_particleSettings.OpacityKeyframes.Length - 1].Opacity * 255f) << 24;
			}
			if (_particleSettings.ScaleKeyframes.Length != 0 && reference3.ScaleAnimationIndex != _particleSettings.ScaleKeyframes.Length - 1)
			{
				ref ParticleSettings.ScaleKeyframe scaleKeyframe = ref _particleSettings.ScaleKeyframes[_particleSettings.ScaleKeyframes.Length - 1];
				ComputeParticleScale(particleIndex, _particleSettings.ScaleKeyframes.Length - 1, ref scaleKeyframe, out reference2);
				reference3.ScaleAnimationIndex = (byte)(_particleSettings.ScaleKeyframes.Length - 1);
			}
			if (_particleSettings.RotationKeyframes.Length != 0 && reference3.RotationAnimationIndex != _particleSettings.RotationKeyframes.Length - 1)
			{
				ComputeParticleRotation(_particleSettings.RotationKeyframes.Length - 1, ref _particleSettings.RotationKeyframes[_particleSettings.RotationKeyframes.Length - 1], out reference3.CurrentRotation);
				reference3.RotationAnimationIndex = (byte)(_particleSettings.RotationKeyframes.Length - 1);
			}
			reference4.Rotation = reference3.RotationOffset * Quaternion.CreateFromYawPitchRoll(reference3.CurrentRotation.Yaw, reference3.CurrentRotation.Pitch, reference3.CurrentRotation.Roll);
			if (_particleSettings.TextureIndexKeyFrames.Length != 0 && reference3.TextureAnimationIndex != _particleSettings.TextureIndexKeyFrames.Length - 1)
			{
				ref ParticleSettings.RangeKeyframe reference5 = ref _particleSettings.TextureIndexKeyFrames[_particleSettings.TextureIndexKeyFrames.Length - 1];
				reference4.TargetTextureIndex = (byte)_random.Next(reference5.Min, reference5.Max + 1);
				reference4.PrevTargetTextureIndex = reference4.TargetTextureIndex;
				reference4.TargetTextureBlendProgress = 0;
				reference3.TextureAnimationIndex = (byte)(_particleSettings.TextureIndexKeyFrames.Length - 1);
			}
			return;
		}
		float num3 = 0.01f * reference.LifeSpan;
		float num4 = 0f;
		float num5 = 100f;
		for (int i = 0; i < _particleSettings.ColorKeyFrameCount; i++)
		{
			ref ParticleSettings.ColorKeyframe reference6 = ref _particleSettings.ColorKeyframes[i];
			float num6 = (float)(int)reference6.Time * num3;
			if (num6 <= num)
			{
				uInt32Color = reference6.Color;
				uInt32Color2 = uInt32Color;
				num4 = num6;
				continue;
			}
			uInt32Color2 = reference6.Color;
			num5 = num6;
			break;
		}
		float num7 = num5 - num4;
		float amount = ((num7 != 0f) ? ((num - num4) / num7) : 0f);
		reference4.Color = (uint)MathHelper.Lerp((int)uInt32Color.GetR(), (int)uInt32Color2.GetR(), amount) | ((uint)MathHelper.Lerp((int)uInt32Color.GetG(), (int)uInt32Color2.GetG(), amount) << 8) | ((uint)MathHelper.Lerp((int)uInt32Color.GetB(), (int)uInt32Color2.GetB(), amount) << 16);
		num4 = 0f;
		num5 = 100f;
		for (int j = 0; j < _particleSettings.OpacityKeyFrameCount; j++)
		{
			ref ParticleSettings.OpacityKeyframe reference7 = ref _particleSettings.OpacityKeyframes[j];
			float num8 = (float)(int)reference7.Time * num3;
			if (num8 <= num)
			{
				num2 = reference7.Opacity;
				value = num2;
				num4 = num8;
				continue;
			}
			value = reference7.Opacity;
			num5 = num8;
			break;
		}
		num7 = num5 - num4;
		amount = ((num7 != 0f) ? ((num - num4) / num7) : 0f);
		reference4.Color |= (uint)(MathHelper.Lerp(num2, value, amount) * 255f) << 24;
		if (reference3.ScaleNextKeyframeTime <= num)
		{
			float num9 = 0f;
			byte b = reference3.ScaleAnimationIndex;
			while (b < _particleSettings.ScaleKeyFrameCount)
			{
				ref ParticleSettings.ScaleKeyframe reference8 = ref _particleSettings.ScaleKeyframes[b];
				float num10 = (float)(int)reference8.Time * num3;
				if (num10 <= num)
				{
					if (b == 0)
					{
						ComputeParticleScale(particleIndex, b, ref reference8, out reference2);
					}
					reference3.ScaleStep = Vector2.Zero;
					num9 = num10;
					reference3.ScaleNextKeyframeTime = reference.LifeSpan;
					b++;
					continue;
				}
				ComputeParticleScale(particleIndex, b, ref reference8, out reference3.ScaleStep);
				reference3.ScaleStep /= num10 - num9;
				reference3.ScaleAnimationIndex = b;
				reference3.ScaleNextKeyframeTime = num10;
				break;
			}
		}
		reference2 += reference3.ScaleStep * deltaTime;
		if (reference3.RotationNextKeyframeTime <= num)
		{
			float num11 = 0f;
			byte b2 = reference3.RotationAnimationIndex;
			while (b2 < _particleSettings.RotationKeyFrameCount)
			{
				ref ParticleSettings.RotationKeyframe reference9 = ref _particleSettings.RotationKeyframes[b2];
				float num12 = (float)(int)reference9.Time * num3;
				if (num12 <= num)
				{
					if (b2 == 0)
					{
						ComputeParticleRotation(b2, ref reference9, out reference3.CurrentRotation);
					}
					reference3.RotationStep = Vector3.Zero;
					num11 = num12;
					reference3.RotationNextKeyframeTime = reference.LifeSpan;
					b2++;
					continue;
				}
				ComputeParticleRotation(b2, ref reference9, out reference3.RotationStep);
				reference3.RotationStep /= num12 - num11;
				reference3.RotationAnimationIndex = b2;
				reference3.RotationNextKeyframeTime = num12;
				break;
			}
		}
		reference3.CurrentRotation += reference3.RotationStep * deltaTime;
		reference4.Rotation = Quaternion.CreateFromYawPitchRoll(reference3.CurrentRotation.Yaw, reference3.CurrentRotation.Pitch, reference3.CurrentRotation.Roll);
		if (reference3.TextureNextKeyframeTime <= num)
		{
			byte b3 = reference3.TextureAnimationIndex;
			while (b3 < _particleSettings.TextureKeyFrameCount)
			{
				ref ParticleSettings.RangeKeyframe reference10 = ref _particleSettings.TextureIndexKeyFrames[b3];
				float num13 = (float)(int)reference10.Time * num3;
				if (num13 <= num)
				{
					reference3.TextureNextKeyframeTime = reference.LifeSpan;
					b3++;
					continue;
				}
				reference3.TextureAnimationIndex = b3;
				reference3.TextureNextKeyframeTime = num13;
				reference4.PrevTargetTextureIndex = reference4.TargetTextureIndex;
				reference4.TargetTextureIndex = (byte)_random.Next(reference10.Min, reference10.Max + 1);
				break;
			}
		}
		if (reference3.TextureAnimationIndex >= 1)
		{
			num4 = (float)(int)_particleSettings.TextureIndexKeyFrames[reference3.TextureAnimationIndex - 1].Time * num3;
			num5 = (float)(int)_particleSettings.TextureIndexKeyFrames[reference3.TextureAnimationIndex].Time * num3;
		}
		else
		{
			num4 = (num5 = 0f);
		}
		num7 = num5 - num4;
		amount = ((num7 != 0f) ? ((num - num4) / num7) : 0f);
		amount = MathHelper.Clamp(amount, 0f, 1f);
		if (!_useSpriteBlending)
		{
			amount = ((amount == 1f) ? 1f : 0f);
		}
		reference4.TargetTextureBlendProgress = (ushort)(amount * 65535f);
	}

	public ParticleSpawner(ParticleFXSystem particleFXSystem, UpdateParticleCollisionFunc updateCollisionFunc, InitParticleFunc initParticleFunc, Random random, ParticleSpawnerSettings spawnerSettings, bool useSoftParticles, Vector2 textureAltasInverseSize, int particleBufferStartIndex)
	{
		_particleFXSystem = particleFXSystem;
		_updateCollisionFunc = updateCollisionFunc;
		_initParticleFunc = initParticleFunc;
		_random = random;
		_spawnerSettings = spawnerSettings;
		_particleSettings = spawnerSettings.ParticleSettings;
		_particleBuffer = _particleFXSystem.ParticleBuffer;
		_particleCount = _spawnerSettings.MaxConcurrentParticles;
		_particleBufferStartIndex = particleBufferStartIndex;
		_drawData.UVMotion = new Vector4(_spawnerSettings.UVMotion.Speed, _spawnerSettings.UVMotion.Strength, _spawnerSettings.UVMotion.Scale);
		_drawData.UVMotionTextureId = _spawnerSettings.UVMotion.TextureId;
		_drawData.AddRandomUVOffset = (_spawnerSettings.UVMotion.AddRandomUVOffset ? 1f : 0f);
		_drawData.StrengthCurveType = (int)_spawnerSettings.UVMotion.StrengthCurveType;
		_drawData.IntersectionHighlight = new Vector4(_spawnerSettings.IntersectionHighlight.Color.X, _spawnerSettings.IntersectionHighlight.Color.Y, _spawnerSettings.IntersectionHighlight.Color.Z, _spawnerSettings.IntersectionHighlight.Threshold);
		_drawData.CameraOffset = _spawnerSettings.CameraOffset;
		_drawData.VelocityStretchMultiplier = _spawnerSettings.VelocityStretchMultiplier;
		_drawData.SoftParticlesFadeFactor = _particleSettings.SoftParticlesFadeFactor;
		_useSpriteBlending = _particleSettings.UseSpriteBlending;
		for (int i = _particleBufferStartIndex; i < _particleCount + _particleBufferStartIndex; i++)
		{
			ref ParticleBuffers.ParticleLifeData reference = ref _particleBuffer.Life[i];
			ref Vector2 reference2 = ref _particleBuffer.Scale[i];
			reference.LifeSpanTimer = 0f;
			reference2 = Vector2.Zero;
		}
		UpdateTextures(textureAltasInverseSize);
		_particlesLeftToEmit = _random.Next(_spawnerSettings.TotalParticles.X, _spawnerSettings.TotalParticles.Y + 1);
		_lifeSpanTimer = _spawnerSettings.LifeSpan;
		_hasWaves = _spawnerSettings.WaveDelay != Vector2.Zero;
		_useSoftParticles = useSoftParticles;
		_particlesPerWave = _spawnerSettings.MaxConcurrentParticles;
	}

	public void Dispose()
	{
		_particleBufferStartIndex = 0;
		_particleCount = 0;
		_initParticleFunc = null;
		_updateCollisionFunc = null;
		_particleFXSystem = null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Expire(bool clearActiveParticles = false)
	{
		if (clearActiveParticles)
		{
			_activeParticles = 0;
		}
		_particlesLeftToEmit = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExpired()
	{
		return ActiveParticles == 0 && _particlesLeftToEmit == 0;
	}

	private void UpdateTextures(Vector2 textureAltasInverseSize)
	{
		_textureAltasInverseSize = textureAltasInverseSize;
		_drawData.ImageLocation = _particleSettings.ImageLocation;
		UShortVector2 frameSize = _particleSettings.FrameSize;
		if (frameSize.X == 0 || frameSize.Y == 0 || frameSize.X > _drawData.ImageLocation.Width || frameSize.Y > _drawData.ImageLocation.Height)
		{
			_drawData.FrameSize.X = (ushort)_drawData.ImageLocation.Width;
			_drawData.FrameSize.Y = (ushort)_drawData.ImageLocation.Height;
		}
		else
		{
			_drawData.FrameSize = frameSize;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SpawnAtPosition(Vector3 position, Quaternion rotation)
	{
		Position = (_lastPosition = position);
		Rotation = (_lastRotation = rotation);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetScale(float scale)
	{
		Scale = scale;
		ScaleFactor = 1f / Scale;
	}

	public void LightUpdate()
	{
		ComputeSimulationParameters(out var inverseRotation, out var rotateWithSpawner, out var collisionPositionOffset, out var positionOffset, out var collisionRotationOffset, out var rotationOffset);
		int num = 0;
		for (int i = _particleBufferStartIndex; i < _particleCount + _particleBufferStartIndex; i++)
		{
			ref ParticleBuffers.ParticleLifeData reference = ref _particleBuffer.Life[i];
			ref ParticleBuffers.ParticleRenderData reference2 = ref _particleBuffer.Data1[i];
			ref Vector2 reference3 = ref _particleBuffer.Scale[i];
			if (!(reference.LifeSpanTimer <= 0f))
			{
				bool flag = BitUtils.IsBitOn(2, reference2.BoolData);
				Vector3 vector = (flag ? collisionPositionOffset : positionOffset);
				Quaternion rotation = (flag ? collisionRotationOffset : rotationOffset);
				reference2.Velocity = Vector3.Transform(reference2.Velocity, rotation);
				reference2.Position = Vector3.Transform(reference2.Position, rotation) + vector;
				if (reference3 != Vector2.Zero)
				{
					num++;
				}
			}
		}
		_particleDrawCount = num;
		UpdatePostSimulation(rotateWithSpawner ? Quaternion.Identity : inverseRotation);
	}

	public void Update(float deltaTime, int totalSteps)
	{
		float emitParticles = 0f;
		if (ConsumeSpawnerLifeSpan(deltaTime, ref emitParticles))
		{
			return;
		}
		ComputeSimulationParameters(out var inverseRotation, out var rotateWithSpawner, out var collisionPositionOffset, out var positionOffset, out var collisionRotationOffset, out var rotationOffset);
		int num = 0;
		for (int i = _particleBufferStartIndex; i < _particleCount + _particleBufferStartIndex; i++)
		{
			if (!TryToSpawnParticles(i, deltaTime, ref emitParticles) && UpdateParticleSimulation(i, deltaTime, totalSteps, ref collisionPositionOffset, ref positionOffset, ref collisionRotationOffset, ref rotationOffset, ref inverseRotation))
			{
				num++;
			}
		}
		_particleDrawCount = num;
		UpdatePostSimulation(rotateWithSpawner ? Quaternion.Identity : inverseRotation);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool ConsumeSpawnerLifeSpan(float deltaTime, ref float emitParticles)
	{
		if (_hasWaves && _waveTimer > 0f)
		{
			_waveTimer -= deltaTime;
			return !_spawnerSettings.SpawnBurst;
		}
		if (_lifeSpanTimer > 0f)
		{
			_lifeSpanTimer -= deltaTime;
			if (_lifeSpanTimer <= 0f)
			{
				Expire();
			}
		}
		float num = (_spawnerSettings.SpawnBurst ? 1f : deltaTime);
		emitParticles = _random.NextFloat(_spawnerSettings.SpawnRate.X, _spawnerSettings.SpawnRate.Y) * num + _particleCrumbs;
		_particleCrumbs = emitParticles % 1f;
		if (_spawnerSettings.SpawnBurst)
		{
			_particlesPerWave = (int)emitParticles;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryToSpawnParticles(int particleIndex, float deltaTime, ref float emitParticles)
	{
		ref ParticleBuffers.ParticleLifeData reference = ref _particleBuffer.Life[particleIndex];
		if (!IsPaused && !_waveEnded && _waveTimer <= 0f && emitParticles >= 1f && _particlesLeftToEmit != 0 && reference.LifeSpanTimer <= 0f)
		{
			if (!InitiateParticle(particleIndex))
			{
				return true;
			}
			_activeParticles++;
			_particlesInWave++;
			if (_hasWaves && !_waveEnded && _particlesInWave == _particlesPerWave)
			{
				bool spawnBurst = _spawnerSettings.SpawnBurst;
				_particlesInWave = 0;
				_waveEnded = !spawnBurst;
				if (spawnBurst)
				{
					_waveTimer = _random.NextFloat(_spawnerSettings.WaveDelay.X, _spawnerSettings.WaveDelay.Y);
				}
			}
			emitParticles -= 1f;
			if (_particlesLeftToEmit > 0)
			{
				_particlesLeftToEmit--;
			}
		}
		else
		{
			if (reference.LifeSpanTimer <= 0f)
			{
				return true;
			}
			reference.LifeSpanTimer -= deltaTime;
			if (reference.LifeSpanTimer <= 0f)
			{
				if (ActiveParticles > 0)
				{
					_activeParticles--;
				}
				if (_waveEnded && ActiveParticles == 0)
				{
					_waveEnded = false;
					_waveTimer = _random.NextFloat(_spawnerSettings.WaveDelay.X, _spawnerSettings.WaveDelay.Y);
				}
				_particleBuffer.Scale[particleIndex] = Vector2.Zero;
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ComputeSimulationParameters(out Quaternion inverseRotation, out bool rotateWithSpawner, out Vector3 collisionPositionOffset, out Vector3 positionOffset, out Quaternion collisionRotationOffset, out Quaternion rotationOffset)
	{
		float num = ((IsFirstPerson == _wasFirstPerson) ? _spawnerSettings.TrailSpawnerPositionMultiplier : 0f);
		float amount = ((IsFirstPerson == _wasFirstPerson) ? _spawnerSettings.TrailSpawnerRotationMultiplier : 0f);
		inverseRotation = Quaternion.Inverse(Rotation);
		rotateWithSpawner = _spawnerSettings.ParticleRotateWithSpawner;
		collisionPositionOffset = Vector3.Transform(_lastPosition - Position, inverseRotation);
		positionOffset = collisionPositionOffset * num * ScaleFactor;
		collisionRotationOffset = inverseRotation * _lastRotation;
		rotationOffset = Quaternion.Slerp(Quaternion.Identity, collisionRotationOffset, amount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool UpdateParticleSimulation(int particleIndex, float deltaTime, int totalSteps, ref Vector3 collisionPositionOffset, ref Vector3 positionOffset, ref Quaternion collisionRotationOffset, ref Quaternion rotationOffset, ref Quaternion inverseRotation)
	{
		ref ParticleBuffers.ParticleSimulationData reference = ref _particleBuffer.Data0[particleIndex];
		ref ParticleBuffers.ParticleRenderData reference2 = ref _particleBuffer.Data1[particleIndex];
		ref Vector2 reference3 = ref _particleBuffer.Scale[particleIndex];
		bool flag = BitUtils.IsBitOn(2, reference2.BoolData);
		Vector3 vector = (flag ? collisionPositionOffset : positionOffset);
		Quaternion rotation = (flag ? collisionRotationOffset : rotationOffset);
		reference2.Velocity = Vector3.Transform(reference2.Velocity, rotation);
		reference2.Position = Vector3.Transform(reference2.Position, rotation) + vector;
		Vector3 position = reference2.Position;
		if (!flag)
		{
			for (int i = 0; i < totalSteps; i++)
			{
				reference2.AttractorVelocity = Vector3.Zero;
				for (int j = 0; j < _spawnerSettings.Attractors.Length; j++)
				{
					_spawnerSettings.Attractors[j].Apply(reference2.Position, reference.SpawnerPositionAtSpawn - Position, ref reference2.Velocity, ref reference2.AttractorVelocity);
				}
				reference2.Position += reference2.Velocity + reference2.AttractorVelocity;
			}
			if (!_spawnerSettings.ParticleRotateWithSpawner)
			{
				reference.RotationOffset = rotationOffset * reference.RotationOffset;
			}
			if (_spawnerSettings.ParticleCollisionBlockType != 0)
			{
				ref ParticleBuffers.ParticleLifeData particleLife = ref _particleBuffer.Life[particleIndex];
				_updateCollisionFunc(this, ref reference, ref reference2, ref reference3, ref particleLife, position, inverseRotation);
			}
		}
		UpdateParticleAnimation(particleIndex, deltaTime);
		return reference3 != Vector2.Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdatePostSimulation(Quaternion inverseRotation)
	{
		_drawData.InverseRotation = inverseRotation;
		_lastPosition = Position;
		_lastRotation = Rotation;
		_wasFirstPerson = IsFirstPerson;
	}

	public void UpdateLife(float deltaTime)
	{
		float emitParticles = 0f;
		if (!ConsumeSpawnerLifeSpan(deltaTime, ref emitParticles))
		{
			for (int i = _particleBufferStartIndex; i < _particleCount + _particleBufferStartIndex; i++)
			{
				TryToSpawnParticles(i, deltaTime, ref emitParticles);
			}
		}
	}

	public void UpdateSimulation(float deltaTime, int totalSteps)
	{
		ComputeSimulationParameters(out var inverseRotation, out var rotateWithSpawner, out var collisionPositionOffset, out var positionOffset, out var collisionRotationOffset, out var rotationOffset);
		int num = 0;
		for (int i = _particleBufferStartIndex; i < _particleCount + _particleBufferStartIndex; i++)
		{
			if (!(_particleBuffer.Life[i].LifeSpanTimer <= 0f) && UpdateParticleSimulation(i, deltaTime, totalSteps, ref collisionPositionOffset, ref positionOffset, ref collisionRotationOffset, ref rotationOffset, ref inverseRotation))
			{
				num++;
			}
		}
		_particleDrawCount = num;
		UpdatePostSimulation(rotateWithSpawner ? Quaternion.Identity : inverseRotation);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateLight(Vector4 staticLightColor)
	{
		staticLightColor.W = _spawnerSettings.LightInfluence;
		_drawData.StaticLightColorAndInfluence = staticLightColor;
	}

	private bool InitiateParticle(int particleIndex)
	{
		ref ParticleBuffers.ParticleSimulationData reference = ref _particleBuffer.Data0[particleIndex];
		ref ParticleBuffers.ParticleRenderData reference2 = ref _particleBuffer.Data1[particleIndex];
		ref Vector2 reference3 = ref _particleBuffer.Scale[particleIndex];
		ref ParticleBuffers.ParticleLifeData reference4 = ref _particleBuffer.Life[particleIndex];
		float x = _random.NextFloat(_spawnerSettings.EmitOffsetMin.X, _spawnerSettings.EmitOffsetMax.X);
		float y = _random.NextFloat(_spawnerSettings.EmitOffsetMin.Y, _spawnerSettings.EmitOffsetMax.Y);
		float z = _random.NextFloat(_spawnerSettings.EmitOffsetMin.Z, _spawnerSettings.EmitOffsetMax.Z);
		Vector3 particlePosition = GetEmitPosition(_spawnerSettings.EmitShape, new Vector3(x, y, z));
		if (!_initParticleFunc(this, ref particlePosition))
		{
			return false;
		}
		reference3 = Vector2.Zero;
		reference4.LifeSpan = _random.NextFloat(_spawnerSettings.ParticleLifeSpan.X, _spawnerSettings.ParticleLifeSpan.Y);
		reference4.LifeSpanTimer = reference4.LifeSpan;
		reference.SpawnerPositionAtSpawn = Position;
		reference.RotationAnimationIndex = 0;
		reference.RotationNextKeyframeTime = 0f;
		reference.RotationStep = Vector3.Zero;
		reference.RotationOffset = Quaternion.Identity;
		reference.CurrentRotation = Vector3.Zero;
		reference.ScaleAnimationIndex = 0;
		reference.ScaleNextKeyframeTime = 0f;
		reference.ScaleStep = Vector2.Zero;
		reference.TextureAnimationIndex = 0;
		reference.TextureNextKeyframeTime = 0f;
		reference2.TargetTextureIndex = 0;
		reference2.Seed = (ushort)_random.Next();
		if (_particleSettings.TextureIndexKeyFrames != null)
		{
			float num = 0.01f * reference4.LifeSpan;
			ref ParticleSettings.RangeKeyframe reference5 = ref _particleSettings.TextureIndexKeyFrames[0];
			float num2 = (float)(int)reference5.Time * num;
			reference2.TargetTextureIndex = (byte)_random.Next(reference5.Min, reference5.Max + 1);
			reference.TextureNextKeyframeTime = ((_particleSettings.TextureIndexKeyFrames.Length > 1) ? num2 : reference4.LifeSpan);
		}
		reference2.PrevTargetTextureIndex = reference2.TargetTextureIndex;
		reference2.TargetTextureBlendProgress = 0;
		reference2.Position = particlePosition;
		reference2.Rotation = ParticleSettings.DefaultRotation;
		reference2.BoolData = 0;
		bool flag = false;
		bool flag2 = false;
		switch (_particleSettings.UVOption)
		{
		case ParticleSettings.UVOptions.FlipU:
			flag = true;
			break;
		case ParticleSettings.UVOptions.FlipV:
			flag2 = true;
			break;
		case ParticleSettings.UVOptions.FlipUV:
			flag = true;
			flag2 = true;
			break;
		case ParticleSettings.UVOptions.RandomFlipU:
			flag = _random.NextDouble() < 0.5;
			break;
		case ParticleSettings.UVOptions.RandomFlipV:
			flag2 = _random.NextDouble() < 0.5;
			break;
		case ParticleSettings.UVOptions.RandomFlipUV:
			flag = _random.NextDouble() < 0.5;
			flag2 = flag;
			break;
		}
		if (flag)
		{
			BitUtils.SwitchOnBit(0, ref reference2.BoolData);
		}
		if (flag2)
		{
			BitUtils.SwitchOnBit(1, ref reference2.BoolData);
		}
		Quaternion rotation;
		if (_spawnerSettings.UseEmitDirection)
		{
			Vector3 vector = particlePosition;
			if (vector == Vector3.Zero)
			{
				vector.X = _random.NextFloat(-1f, 1f);
				vector.Y = _random.NextFloat(-1f, 1f);
				vector.Z = _random.NextFloat(-1f, 1f);
				vector.Normalize();
			}
			Vector3 emitRotationVector = GetEmitRotationVector(_spawnerSettings.EmitShape, vector);
			rotation = Quaternion.CreateFromVectors(Vector3.Forward, emitRotationVector);
		}
		else
		{
			float yaw = _random.NextFloat(_spawnerSettings.InitialVelocityMin.Yaw, _spawnerSettings.InitialVelocityMax.Yaw);
			float pitch = _random.NextFloat(_spawnerSettings.InitialVelocityMin.Pitch, _spawnerSettings.InitialVelocityMax.Pitch);
			rotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0f);
		}
		float num3 = _random.NextFloat(_spawnerSettings.InitialVelocityMin.Speed, _spawnerSettings.InitialVelocityMax.Speed);
		reference2.Velocity = Vector3.Transform(Vector3.Forward * num3, rotation);
		return true;
	}

	private Vector3 GetEmitPosition(ParticleSpawnerSettings.Shape emitShape, Vector3 position)
	{
		Vector3 result = Vector3.Zero;
		switch (emitShape)
		{
		case ParticleSpawnerSettings.Shape.Sphere:
		{
			float num6 = (float)_random.NextDouble() * ((float)System.Math.PI * 2f);
			float num7 = (float)_random.NextDouble() * ((float)System.Math.PI * 2f);
			result = new Vector3(position.X * (float)System.Math.Cos(num6) * (float)System.Math.Cos(num7), position.Y * (float)System.Math.Sin(num6) * (float)System.Math.Cos(num7), position.Z * (float)System.Math.Sin(num7));
			break;
		}
		case ParticleSpawnerSettings.Shape.Circle:
		{
			float num5 = (float)_random.NextDouble() * ((float)System.Math.PI * 2f);
			result.X = ((position.X != 0f) ? (position.X * (float)System.Math.Cos(num5)) : 0f);
			if (position.Y != 0f)
			{
				result.Y = ((position.X == 0f) ? (position.Y * (float)System.Math.Cos(num5)) : (position.Y * (float)System.Math.Sin(num5)));
			}
			else
			{
				result.Y = 0f;
			}
			result.Z = ((position.Z != 0f) ? (position.Z * (float)System.Math.Sin(num5)) : 0f);
			break;
		}
		case ParticleSpawnerSettings.Shape.FullCube:
			position.X = position.X * 2f - _spawnerSettings.EmitOffsetMax.X;
			position.Y = position.Y * 2f - _spawnerSettings.EmitOffsetMax.Y;
			position.Z = position.Z * 2f - _spawnerSettings.EmitOffsetMax.Z;
			result = position;
			break;
		case ParticleSpawnerSettings.Shape.Cube:
		{
			int num = ((position.Z != 0f) ? ((position.X == 0f) ? 1 : ((position.Y != 0f) ? _random.Next(2) : 2)) : 0);
			float num2 = 0f;
			float num3 = 0f;
			float yaw = 0f;
			float pitch = 0f;
			switch (num)
			{
			case 0:
				num2 = position.X * 2f;
				num3 = position.Y * 2f;
				position.Z = _random.NextFloat(0f, position.Z * 2f) - position.Z;
				break;
			case 1:
				num2 = position.Z * 2f;
				num3 = position.Y * 2f;
				position.Z = _random.NextFloat(0f, position.X * 2f) - position.X;
				yaw = (float)System.Math.PI / 2f;
				break;
			case 2:
				num2 = position.X * 2f;
				num3 = position.Z * 2f;
				position.Z = _random.NextFloat(0f, position.Y * 2f) - position.Y;
				pitch = (float)System.Math.PI / 2f;
				break;
			}
			float num4 = _random.NextFloat(0f, num2 * 2f + num3 * 2f);
			if (num4 < num2)
			{
				position.X = num4 - num2 * 0.5f;
				position.Y = (0f - num3) * 0.5f;
			}
			else if (num4 < num2 * 2f)
			{
				position.X = num4 - num2 - num2 * 0.5f;
				position.Y = num3 * 0.5f;
			}
			else if (num4 < num2 * 2f + num3)
			{
				position.X = (0f - num2) * 0.5f;
				position.Y = num4 - num2 * 2f - num3 * 0.5f;
			}
			else
			{
				position.X = num2 * 0.5f;
				position.Y = num4 - (num2 * 2f + num3) - num3 * 0.5f;
			}
			result = Vector3.Transform(position, Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0f));
			break;
		}
		}
		return result;
	}

	private Vector3 GetEmitRotationVector(ParticleSpawnerSettings.Shape emitShape, Vector3 spawnPosition)
	{
		return Vector3.Normalize(spawnPosition);
	}

	public void ReserveVertexDataStorage(ref FXVertexBuffer vertexBuffer, ushort drawId)
	{
		_drawId = drawId;
		_particleVertexDataStartIndex = vertexBuffer.ReserveVertexDataStorage(_particleDrawCount);
	}

	public unsafe void PrepareForDraw(Vector3 cameraPosition, ref FXVertexBuffer vertexBuffer, IntPtr gpuDrawDataPtr)
	{
		Debug.Assert(ActiveParticles > 0, "No need to call PrepareForDraw() on a ParticleSpawner that has 0 particles active.");
		int num = 0;
		uint num2 = (uint)((_spawnerSettings.LinearFiltering ? 1 : 0) << FXVertex.ConfigBitShiftLinearFiltering);
		num2 |= (uint)((_useSoftParticles ? 1 : 0) << FXVertex.ConfigBitShiftSoftParticles);
		num2 |= (uint)((int)_spawnerSettings.RenderMode << FXVertex.ConfigBitShiftBlendMode);
		num2 |= (uint)((IsFirstPerson ? 1 : 0) << FXVertex.ConfigBitShiftIsFirstPerson);
		num2 |= (uint)(_drawId << FXVertex.ConfigBitShiftDrawId);
		float num3 = ((_spawnerSettings.RotationInfluence == ParticleFXSystem.ParticleRotationInfluence.Billboard || _spawnerSettings.RotationInfluence == ParticleFXSystem.ParticleRotationInfluence.BillboardY || _spawnerSettings.RotationInfluence == ParticleFXSystem.ParticleRotationInfluence.BillboardVelocity) ? Scale : 1f);
		for (int i = _particleBufferStartIndex; i < _particleCount + _particleBufferStartIndex; i++)
		{
			ref ParticleBuffers.ParticleRenderData reference = ref _particleBuffer.Data1[i];
			ref Vector2 reference2 = ref _particleBuffer.Scale[i];
			ref ParticleBuffers.ParticleLifeData reference3 = ref _particleBuffer.Life[i];
			if (!(reference2 == Vector2.Zero))
			{
				bool flag = BitUtils.IsBitOn(0, reference.BoolData);
				bool flag2 = BitUtils.IsBitOn(1, reference.BoolData);
				bool flag3 = BitUtils.IsBitOn(2, reference.BoolData);
				int particleIndex = num + _particleVertexDataStartIndex;
				vertexBuffer.SetParticleVertexDataPositionAndScale(particleIndex, reference.Position, reference2 * num3);
				Vector3 vector = reference.Velocity + reference.AttractorVelocity;
				if (_spawnerSettings.RotationInfluence == ParticleFXSystem.ParticleRotationInfluence.BillboardVelocity)
				{
					vector = Vector3.Transform(vector, Rotation);
				}
				vertexBuffer.SetParticleVertexDataVelocityAndRotation(particleIndex, vector, new Vector4(reference.Rotation.X, reference.Rotation.Y, reference.Rotation.Z, reference.Rotation.W));
				uint textureInfo = (uint)((reference.TargetTextureBlendProgress << 16) | (reference.PrevTargetTextureIndex << 8) | reference.TargetTextureIndex);
				vertexBuffer.SetParticleVertexDataTextureInfo(particleIndex, textureInfo);
				vertexBuffer.SetParticleVertexDataColor(particleIndex, reference.Color);
				ParticleFXSystem.ParticleRotationInfluence particleRotationInfluence = ((!flag3) ? _spawnerSettings.RotationInfluence : _spawnerSettings.ParticleCollisionRotationInfluence);
				uint num4 = num2 | (uint)((int)particleRotationInfluence << FXVertex.ConfigBitShiftQuadType);
				num4 |= (uint)((flag ? 1 : 0) << FXVertex.ConfigBitShiftInvertUTexture);
				num4 |= (uint)((flag2 ? 1 : 0) << FXVertex.ConfigBitShiftInvertVTexture);
				vertexBuffer.SetVertexDataConfig(particleIndex, num4);
				ushort seed = reference.Seed;
				float num5 = reference3.LifeSpan - reference3.LifeSpanTimer;
				ushort num6 = (ushort)(num5 / reference3.LifeSpan * 65535f);
				uint seedAndLifeRatio = (uint)((seed << 16) | num6);
				vertexBuffer.SetParticleVertexDataSeedAndLifeRatio(particleIndex, seedAndLifeRatio);
				num++;
			}
		}
		if (num != _particleDrawCount)
		{
			Logger.Info($"Unexpected divergence between visibleParticleId & _particleDrawCount : {num} vs {_particleDrawCount}.");
		}
		Vector3 position = ((!IsFirstPerson) ? (Position - cameraPosition) : Position);
		Matrix.CreateScale(Scale, out var result);
		Matrix.CreateFromQuaternion(ref Rotation, out var result2);
		Matrix.Multiply(ref result, ref result2, out result);
		Matrix.CreateTranslation(ref position, out result2);
		Matrix.Multiply(ref result, ref result2, out var result3);
		IntPtr pointer = IntPtr.Add(gpuDrawDataPtr, _drawId * FXRenderer.DrawDataSize);
		Matrix* ptr = (Matrix*)pointer.ToPointer();
		*ptr = result3;
		Vector4* ptr2 = (Vector4*)IntPtr.Add(pointer, sizeof(Matrix)).ToPointer();
		*ptr2 = _drawData.StaticLightColorAndInfluence;
		ptr2[1] = new Vector4(_drawData.InverseRotation.X, _drawData.InverseRotation.Y, _drawData.InverseRotation.Z, _drawData.InverseRotation.W);
		ptr2[2] = new Vector4(_drawData.ImageLocation.X, _drawData.ImageLocation.Y, _drawData.ImageLocation.Width, _drawData.ImageLocation.Height);
		ptr2[3] = new Vector4((int)_drawData.FrameSize.X, (int)_drawData.FrameSize.Y, _drawData.UVMotionTextureId, 0f);
		ptr2[4] = _drawData.UVMotion;
		ptr2[5] = _drawData.IntersectionHighlight;
		ptr2[6] = new Vector4(_drawData.CameraOffset, _drawData.VelocityStretchMultiplier, _drawData.SoftParticlesFadeFactor, _drawData.AddRandomUVOffset);
		ptr2[7] = new Vector4(_drawData.StrengthCurveType, 0f, 0f, 0f);
	}
}
