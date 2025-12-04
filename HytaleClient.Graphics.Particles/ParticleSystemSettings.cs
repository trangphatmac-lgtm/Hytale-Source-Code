#define DEBUG
using System.Diagnostics;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal class ParticleSystemSettings
{
	public class SystemSpawnerSettings
	{
		public string ParticleSpawnerId;

		public ParticleSpawnerSettings ParticleSpawnerSettings;

		public bool FixedRotation = false;

		public Vector3 PositionOffset = Vector3.Zero;

		public Quaternion RotationOffset = Quaternion.Identity;

		public Vector2 LifeSpan = DefaultSingleSpawnerLifeSpan;

		public float StartDelay = 0f;

		public Vector2 SpawnRate = DefaultSpawnRate;

		public Vector2 WaveDelay;

		public int TotalSpawners = 1;

		public int MaxConcurrent = 1;

		public ParticleSpawnerSettings.InitialVelocity InitialVelocityMin;

		public ParticleSpawnerSettings.InitialVelocity InitialVelocityMax;

		public Vector3 EmitOffsetMin;

		public Vector3 EmitOffsetMax;

		public ParticleAttractor[] Attractors = new ParticleAttractor[0];
	}

	public const int MaxDefaultConcurrentSpawners = 1;

	public const int MaxPossibleConcurrentSpawners = 10;

	public const float DefaultCullDistance = 40f;

	public const float DefaultCullDistanceSquared = 1600f;

	public const float DefaultBoundingRadius = 10f;

	public static readonly Vector2 DefaultSingleSpawnerLifeSpan = new Vector2(-1f, -1f);

	public static readonly Vector2 DefaultSpawnerLifeSpan = new Vector2(2f, 2f);

	public static readonly Vector2 DefaultSpawnRate = Vector2.One;

	public bool IsImportant;

	public float CullDistanceSquared;

	public float BoundingRadius;

	public float LifeSpan = -1f;

	private SystemSpawnerSettings[] _systemSpawnerSettingsList;

	private byte _systemSpawnerCount;

	public SystemSpawnerSettings[] SystemSpawnerSettingsList => _systemSpawnerSettingsList;

	public byte SystemSpawnerCount => _systemSpawnerCount;

	public void CreateSpawnerSettingsStorage(byte itemCount)
	{
		_systemSpawnerSettingsList = new SystemSpawnerSettings[itemCount];
		_systemSpawnerCount = itemCount;
	}

	public void DeleteSpawnerSettings(byte index)
	{
		Debug.Assert(index < SystemSpawnerCount);
		Debug.Assert(SystemSpawnerCount > 0);
		for (int i = index; i <= SystemSpawnerCount - 2; i++)
		{
			_systemSpawnerSettingsList[i] = _systemSpawnerSettingsList[i + 1];
		}
		_systemSpawnerCount--;
	}
}
