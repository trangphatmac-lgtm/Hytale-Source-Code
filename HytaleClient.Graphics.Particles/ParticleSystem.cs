using System;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Graphics.Particles;

internal class ParticleSystem
{
	public struct SystemSpawnerGroup
	{
		public int Id;

		public ParticleSystemSettings.SystemSpawnerSettings Settings;

		public float SpawnerCrumbs;

		public bool IsSingleSpawner;

		public int SpawnersLeft;

		public int ActiveSpawners;

		public uint ActiveSpawnersBits;

		public int SpawnerIdStart;

		public int SpawnerParticlesIdStart;

		public float StartTimer;

		public bool HasWaves;

		public float WaveTimer;

		public bool WaveEnded;
	}

	public struct SystemSpawner
	{
		public int Id;

		public int GroupId;

		public ParticleSpawner ParticleSpawner;

		public Vector3 Position;

		public Vector3 SystemPositionAtSpawn;

		public Vector3 Velocity;

		public Vector3 AttractorVelocity;

		public float LifeSpanTimer;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public ParticleSystemProxy Proxy;

	public Vector3 Position;

	public Quaternion Rotation = Quaternion.Identity;

	public float Scale = 1f;

	public UInt32Color DefaultColor = ParticleSettings.DefaultColor;

	public bool IsOvergroundOnly = false;

	public SystemSpawnerGroup[] SpawnerGroups;

	public SystemSpawner[] SystemSpawners;

	public int AliveSpawnerCount;

	private readonly ParticleSystemSettings _particleSystemSettings;

	private ParticleFXSystem _particleFXSystem;

	private bool _isGPULowEnd;

	private UpdateSpawnerLightingFunc _updateLightingFunc;

	private UpdateParticleCollisionFunc _updateCollisionFunc;

	private InitParticleFunc _initParticleFunc;

	private int _particleBufferStartIndex;

	private int _maxParticles;

	private int _nextSystemSpawnerId;

	private Random _random;

	private float _attractorStep = 0f;

	private bool _hasUniqueSpawners = true;

	private bool _isWaitingForSpawners = false;

	private float _lifeSpanTimer;

	private Vector2 _textureAltasInverseSize;

	public float CullDistanceSquared => _particleSystemSettings.CullDistanceSquared;

	public float BoundingRadius => _particleSystemSettings.BoundingRadius;

	public int Id { get; private set; }

	public bool IsExpiring { get; private set; } = false;


	public bool IsExpired { get; private set; } = false;


	public bool IsPaused { get; private set; } = false;


	public bool IsFirstPerson { get; private set; } = false;


	public bool IsImportant => _particleSystemSettings.IsImportant;

	public ParticleSystem(ParticleFXSystem particleFXSystem, bool isGPULowEnd, UpdateSpawnerLightingFunc updateLightingFunc, UpdateParticleCollisionFunc updateCollisionFunc, InitParticleFunc initParticleFunc, Vector2 textureAltasInverseSize, int id, ParticleSystemSettings particleSystemSettings)
	{
		Id = id;
		_particleFXSystem = particleFXSystem;
		_isGPULowEnd = isGPULowEnd;
		_updateLightingFunc = updateLightingFunc;
		_updateCollisionFunc = updateCollisionFunc;
		_initParticleFunc = initParticleFunc;
		_textureAltasInverseSize = textureAltasInverseSize;
		_random = new Random(id);
		_particleSystemSettings = particleSystemSettings;
	}

	public void Dispose()
	{
		Release();
		_initParticleFunc = null;
		_updateCollisionFunc = null;
		_updateLightingFunc = null;
		_particleFXSystem = null;
	}

	public bool Initialize()
	{
		_lifeSpanTimer = _particleSystemSettings.LifeSpan;
		_hasUniqueSpawners = true;
		int num = 0;
		int num2 = 0;
		SpawnerGroups = new SystemSpawnerGroup[_particleSystemSettings.SystemSpawnerCount];
		for (int i = 0; i < _particleSystemSettings.SystemSpawnerCount; i++)
		{
			SpawnerGroups[i].Id = i;
			SpawnerGroups[i].Settings = _particleSystemSettings.SystemSpawnerSettingsList[i];
			SpawnerGroups[i].StartTimer = SpawnerGroups[i].Settings.StartDelay;
			SpawnerGroups[i].SpawnersLeft = SpawnerGroups[i].Settings.TotalSpawners;
			SpawnerGroups[i].IsSingleSpawner = SpawnerGroups[i].SpawnersLeft == 1;
			SpawnerGroups[i].HasWaves = SpawnerGroups[i].Settings.WaveDelay != Vector2.Zero;
			_hasUniqueSpawners = _hasUniqueSpawners && SpawnerGroups[i].IsSingleSpawner;
			SpawnerGroups[i].SpawnerIdStart = num;
			SpawnerGroups[i].SpawnerParticlesIdStart = num2;
			int maxConcurrent = SpawnerGroups[i].Settings.MaxConcurrent;
			num += maxConcurrent;
			num2 += maxConcurrent * SpawnerGroups[i].Settings.ParticleSpawnerSettings.MaxConcurrentParticles;
		}
		SystemSpawners = new SystemSpawner[num];
		AliveSpawnerCount = 0;
		_maxParticles = num2;
		_particleBufferStartIndex = _particleFXSystem.RequestParticleBufferStorage(_maxParticles);
		return _particleBufferStartIndex >= 0 && _particleBufferStartIndex < _particleFXSystem.ParticleBufferStorageMaxCount;
	}

	public void Release()
	{
		Proxy = null;
		for (int i = 0; i < AliveSpawnerCount; i++)
		{
			SystemSpawners[i].ParticleSpawner.Dispose();
			SystemSpawners[i].ParticleSpawner = null;
		}
		AliveSpawnerCount = 0;
		if (_maxParticles > 0)
		{
			_particleFXSystem.ReleaseParticleBufferStorage(_particleBufferStartIndex, _maxParticles);
		}
	}

	public void Expire(bool instant = false)
	{
		for (int i = 0; i < AliveSpawnerCount; i++)
		{
			SystemSpawners[i].ParticleSpawner.Expire(instant);
		}
		IsExpiring = true;
	}

	public void Pause(bool pause = true)
	{
		for (int i = 0; i < AliveSpawnerCount; i++)
		{
			SystemSpawners[i].ParticleSpawner.IsPaused = pause;
			UpdateSpawnerPositionAndRotation(i);
		}
		IsPaused = pause;
	}

	public void LightUpdate()
	{
		for (int i = 0; i < AliveSpawnerCount; i++)
		{
			ref SystemSpawner reference = ref SystemSpawners[i];
			UpdateSpawnerPositionAndRotation(i);
			reference.ParticleSpawner.LightUpdate();
		}
	}

	public void Update(float deltaTime)
	{
		ConsumeSystemLifeSpan(deltaTime);
		TryToCreateSpawners(deltaTime);
		int totalSteps = UpdateAttractorSteps(deltaTime);
		bool flag;
		for (int i = 0; i < AliveSpawnerCount; i += ((!flag) ? 1 : 0))
		{
			ParticleSpawner particleSpawner = SystemSpawners[i].ParticleSpawner;
			UpdateSpawnerSimulation(i, totalSteps);
			particleSpawner.Update(deltaTime, totalSteps);
			_updateLightingFunc(particleSpawner);
			flag = CheckSpawnerDeath(i, deltaTime);
		}
	}

	private void ConsumeSystemLifeSpan(float deltaTime)
	{
		if (_lifeSpanTimer > 0f)
		{
			_lifeSpanTimer -= deltaTime;
			if (_lifeSpanTimer <= 0f)
			{
				Expire();
			}
		}
	}

	private void TryToCreateSpawners(float deltaTime)
	{
		_isWaitingForSpawners = false;
		if (!IsExpiring && !IsPaused)
		{
			for (int i = 0; i < SpawnerGroups.Length; i++)
			{
				ref SystemSpawnerGroup reference = ref SpawnerGroups[i];
				if (reference.StartTimer > 0f)
				{
					reference.StartTimer -= deltaTime;
					_isWaitingForSpawners = true;
				}
				else
				{
					if (reference.SpawnersLeft == 0 || reference.ActiveSpawners >= reference.Settings.MaxConcurrent || reference.WaveEnded)
					{
						continue;
					}
					if (reference.IsSingleSpawner)
					{
						TakeStorageSlot(ref reference, out var systemSpawnerId, out var spawnerParticleBufferStartIndex);
						InitializeSpawner(ref reference, systemSpawnerId, _particleBufferStartIndex + spawnerParticleBufferStartIndex);
						reference.SpawnersLeft = 0;
						continue;
					}
					if (reference.HasWaves && reference.WaveTimer > 0f)
					{
						reference.WaveTimer -= deltaTime;
						continue;
					}
					float num = _random.NextFloat(reference.Settings.SpawnRate.X, reference.Settings.SpawnRate.Y) * deltaTime + reference.SpawnerCrumbs;
					reference.SpawnerCrumbs = num % 1f;
					if (num >= 1f)
					{
						TakeStorageSlot(ref reference, out var systemSpawnerId2, out var spawnerParticleBufferStartIndex2);
						InitializeSpawner(ref reference, systemSpawnerId2, _particleBufferStartIndex + spawnerParticleBufferStartIndex2);
						reference.ActiveSpawners++;
						if (reference.SpawnersLeft > 0)
						{
							reference.SpawnersLeft--;
						}
						if (reference.HasWaves && !reference.WaveEnded && reference.ActiveSpawners == reference.Settings.MaxConcurrent)
						{
							reference.WaveEnded = true;
						}
					}
				}
			}
		}
		IsExpired = (_hasUniqueSpawners && !_isWaitingForSpawners) || IsExpiring;
	}

	private int UpdateAttractorSteps(float deltaTime)
	{
		_attractorStep += deltaTime;
		int num = 0;
		while (_attractorStep >= 1f / 60f)
		{
			num++;
			_attractorStep -= 1f / 60f;
		}
		return num;
	}

	private bool CheckSpawnerDeath(int spawnerIndex, float deltaTime)
	{
		ref SystemSpawner reference = ref SystemSpawners[spawnerIndex];
		bool result = false;
		if (reference.LifeSpanTimer > 0f)
		{
			reference.LifeSpanTimer -= deltaTime;
			if (reference.LifeSpanTimer <= 0f)
			{
				reference.ParticleSpawner.Expire();
			}
		}
		if (reference.ParticleSpawner.IsExpired())
		{
			result = true;
			ref SystemSpawnerGroup reference2 = ref SpawnerGroups[reference.GroupId];
			FreeStorageSlot(ref reference2, reference.Id);
			if (reference2.ActiveSpawners > 0)
			{
				reference2.ActiveSpawners--;
			}
			if (reference2.WaveEnded && reference2.ActiveSpawners == 0)
			{
				reference2.WaveEnded = false;
				reference2.WaveTimer = _random.NextFloat(reference2.Settings.WaveDelay.X, reference2.Settings.WaveDelay.Y);
			}
			reference.ParticleSpawner.Dispose();
			SystemSpawners[spawnerIndex] = SystemSpawners[AliveSpawnerCount - 1];
			SystemSpawners[AliveSpawnerCount - 1].ParticleSpawner = null;
			AliveSpawnerCount--;
		}
		else
		{
			IsExpired = false;
		}
		return result;
	}

	private void UpdateSpawnerSimulation(int spawnerIndex, int totalSteps)
	{
		ref SystemSpawner reference = ref SystemSpawners[spawnerIndex];
		ParticleSystemSettings.SystemSpawnerSettings settings = SpawnerGroups[reference.GroupId].Settings;
		for (float num = 0f; num < (float)totalSteps; num += 1f)
		{
			reference.AttractorVelocity = Vector3.Zero;
			for (int i = 0; i < settings.Attractors.Length; i++)
			{
				settings.Attractors[i].Apply(reference.Position, reference.SystemPositionAtSpawn - Position, ref reference.Velocity, ref reference.AttractorVelocity);
			}
			reference.Position += reference.Velocity + reference.AttractorVelocity;
		}
		UpdateSpawnerPositionAndRotation(spawnerIndex);
	}

	private void UpdateSpawnerPositionAndRotation(int spawnerIndex)
	{
		ref SystemSpawner reference = ref SystemSpawners[spawnerIndex];
		ParticleSystemSettings.SystemSpawnerSettings settings = SpawnerGroups[reference.GroupId].Settings;
		reference.ParticleSpawner.Position = Position + Vector3.Transform(reference.Position + settings.PositionOffset, Rotation) * Scale;
		if (!settings.FixedRotation)
		{
			reference.ParticleSpawner.Rotation = Rotation * settings.RotationOffset;
		}
	}

	public void UpdateLife(float deltaTime)
	{
		ConsumeSystemLifeSpan(deltaTime);
		TryToCreateSpawners(deltaTime);
		bool flag;
		for (int i = 0; i < AliveSpawnerCount; i += ((!flag) ? 1 : 0))
		{
			SystemSpawners[i].ParticleSpawner.UpdateLife(deltaTime);
			flag = CheckSpawnerDeath(i, deltaTime);
		}
	}

	public void UpdateSimulation(float deltaTime)
	{
		int totalSteps = UpdateAttractorSteps(deltaTime);
		for (int i = 0; i < AliveSpawnerCount; i++)
		{
			ParticleSpawner particleSpawner = SystemSpawners[i].ParticleSpawner;
			UpdateSpawnerSimulation(i, totalSteps);
			particleSpawner.UpdateSimulation(deltaTime, totalSteps);
			_updateLightingFunc(particleSpawner);
		}
	}

	public void PrepareForDraw(Vector3 cameraPosition, ref FXVertexBuffer vertexBuffer, IntPtr gpuDrawDataPtr)
	{
		for (int i = 0; i < AliveSpawnerCount; i++)
		{
			if (SystemSpawners[i].ParticleSpawner.ActiveParticles > 0)
			{
				SystemSpawners[i].ParticleSpawner.PrepareForDraw(cameraPosition, ref vertexBuffer, gpuDrawDataPtr);
			}
		}
	}

	public void SetFirstPerson(bool isFirstPerson)
	{
		IsFirstPerson = isFirstPerson;
		for (int i = 0; i < AliveSpawnerCount; i++)
		{
			SystemSpawners[i].ParticleSpawner.IsFirstPerson = IsFirstPerson;
		}
	}

	private void InitializeSpawner(ref SystemSpawnerGroup group, int systemSpawnerId, int particleBufferStartIndex)
	{
		ParticleSpawnerSettings particleSpawnerSettings = group.Settings.ParticleSpawnerSettings;
		ParticleSettings particleSettings = particleSpawnerSettings.ParticleSettings;
		bool useSoftParticles = particleSettings.SoftParticlesOption == ParticleSettings.SoftParticles.Require || (particleSettings.SoftParticlesOption == ParticleSettings.SoftParticles.Enable && !_isGPULowEnd);
		ParticleSpawner particleSpawner = new ParticleSpawner(_particleFXSystem, _updateCollisionFunc, _initParticleFunc, _random, particleSpawnerSettings, useSoftParticles, _textureAltasInverseSize, particleBufferStartIndex);
		ref SystemSpawner reference = ref SystemSpawners[AliveSpawnerCount];
		AliveSpawnerCount++;
		reference.Id = systemSpawnerId;
		reference.GroupId = group.Id;
		reference.ParticleSpawner = particleSpawner;
		reference.LifeSpanTimer = _random.NextFloat(group.Settings.LifeSpan.X, group.Settings.LifeSpan.Y);
		reference.SystemPositionAtSpawn = Position;
		float num = _random.NextFloat(group.Settings.EmitOffsetMin.X, group.Settings.EmitOffsetMax.X);
		float num2 = _random.NextFloat(group.Settings.EmitOffsetMin.Y, group.Settings.EmitOffsetMax.Y);
		float num3 = _random.NextFloat(group.Settings.EmitOffsetMin.Z, group.Settings.EmitOffsetMax.Z);
		float num4 = (float)_random.NextDouble() * ((float)System.Math.PI * 2f);
		float num5 = (float)_random.NextDouble() * ((float)System.Math.PI * 2f);
		reference.Position = new Vector3(num * (float)System.Math.Cos(num4) * (float)System.Math.Cos(num5), num2 * (float)System.Math.Sin(num4) * (float)System.Math.Cos(num5), num3 * (float)System.Math.Sin(num5));
		float yaw = _random.NextFloat(group.Settings.InitialVelocityMin.Yaw, group.Settings.InitialVelocityMax.Yaw);
		float pitch = _random.NextFloat(group.Settings.InitialVelocityMin.Pitch, group.Settings.InitialVelocityMax.Pitch);
		float num6 = _random.NextFloat(group.Settings.InitialVelocityMin.Speed, group.Settings.InitialVelocityMax.Speed);
		reference.Velocity = Vector3.Transform(Vector3.Forward * num6, Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0f));
		reference.ParticleSpawner.SpawnAtPosition(Position + Vector3.Transform(reference.Position + group.Settings.PositionOffset, Rotation) * Scale, (!group.Settings.FixedRotation) ? (Rotation * group.Settings.RotationOffset) : group.Settings.RotationOffset);
		reference.ParticleSpawner.IsOvergroundOnly = IsOvergroundOnly;
		reference.ParticleSpawner.SetScale(Scale);
		reference.ParticleSpawner.IsFirstPerson = IsFirstPerson;
		reference.ParticleSpawner.DefaultColor = DefaultColor;
	}

	private void TakeStorageSlot(ref SystemSpawnerGroup group, out int systemSpawnerId, out int spawnerParticleBufferStartIndex)
	{
		int num = (int)BitUtils.FindFirstBitOff(group.ActiveSpawnersBits);
		if (BitUtils.IsBitOn(num, group.ActiveSpawnersBits))
		{
			Logger.Info("Error in the ActiveSpawnersBits management.");
		}
		BitUtils.SwitchOnBit(num, ref group.ActiveSpawnersBits);
		systemSpawnerId = num + group.SpawnerIdStart;
		spawnerParticleBufferStartIndex = group.SpawnerParticlesIdStart + num * group.Settings.ParticleSpawnerSettings.MaxConcurrentParticles;
	}

	private void FreeStorageSlot(ref SystemSpawnerGroup group, int systemSpawnerId)
	{
		int bitId = systemSpawnerId - group.SpawnerIdStart;
		if (!BitUtils.IsBitOn(bitId, group.ActiveSpawnersBits))
		{
			Logger.Info("Error in the ActiveSpawnersBits management.");
		}
		BitUtils.SwitchOffBit(bitId, ref group.ActiveSpawnersBits);
	}
}
