using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Gizmos.Models;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal class ParticleSystemDebug : Disposable
{
	private const float AttractorIndicatorSize = 0.05f;

	private const float SpawnerIndicatorSize = 0.02f;

	private const int GroupModelSegments = 12;

	private const int SpawnerModelSegments = 24;

	private static Vector3[] Colors = new Vector3[5]
	{
		new Vector3(0.99215686f, 0.49803922f, 0.49803922f),
		new Vector3(0.6784314f, 1f, 28f / 51f),
		new Vector3(1f, 0.7647059f, 0.4745098f),
		new Vector3(0.44313726f, 0.75686276f, 0.99215686f),
		new Vector3(0.96862745f, 1f, 0.47058824f)
	};

	private GraphicsDevice _graphics;

	private readonly ParticleSystem _particleSystem;

	private readonly Dictionary<string, Vector3> _spawnerColors = new Dictionary<string, Vector3>();

	private readonly Dictionary<string, PrimitiveModelRenderer> _spawnerSpawnAreaRenderers = new Dictionary<string, PrimitiveModelRenderer>();

	private readonly Dictionary<string, PrimitiveModelRenderer> _particleSpawnAreaRenderers = new Dictionary<string, PrimitiveModelRenderer>();

	private readonly Dictionary<string, List<PrimitiveModelRenderer>> _groupAttractorRenderers = new Dictionary<string, List<PrimitiveModelRenderer>>();

	private readonly Dictionary<string, List<PrimitiveModelRenderer>> _spawnerAttractorRenderers = new Dictionary<string, List<PrimitiveModelRenderer>>();

	private readonly PrimitiveModelRenderer _systemPositionRenderer;

	private readonly PrimitiveModelRenderer _attractorPositionRenderer;

	public ParticleSystemDebug(GraphicsDevice graphics, ParticleSystem particleSystem)
	{
		_graphics = graphics;
		_particleSystem = particleSystem;
		_systemPositionRenderer = new PrimitiveModelRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_systemPositionRenderer.UpdateModelData(SphereModel.BuildModelData(0.025f, 0.05f, 4, 4));
		_attractorPositionRenderer = new PrimitiveModelRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_attractorPositionRenderer.UpdateModelData(BipyramidModel.BuildModelData(0.025f, 0.05f, 16));
		int num = 0;
		for (int i = 0; i < _particleSystem.SpawnerGroups.Length; i++)
		{
			ref ParticleSystem.SystemSpawnerGroup reference = ref _particleSystem.SpawnerGroups[i];
			string particleSpawnerId = reference.Settings.ParticleSpawnerId;
			_spawnerColors[particleSpawnerId] = Colors[num];
			num = ((num != Colors.Length - 1) ? (num + 1) : 0);
			PrimitiveModelRenderer primitiveModelRenderer = new PrimitiveModelRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
			Vector3 vector = Vector3.Max(reference.Settings.EmitOffsetMax, new Vector3(0.05f));
			primitiveModelRenderer.UpdateModelData(SphereModel.BuildModelData(vector.X, vector.Y * 2f, 12, 12, vector.Z));
			_spawnerSpawnAreaRenderers[particleSpawnerId] = primitiveModelRenderer;
			PrimitiveModelRenderer primitiveModelRenderer2 = new PrimitiveModelRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
			vector = Vector3.Max(reference.Settings.ParticleSpawnerSettings.EmitOffsetMax, new Vector3(0.05f));
			ParticleSpawnerSettings.Shape emitShape = reference.Settings.ParticleSpawnerSettings.EmitShape;
			ParticleSpawnerSettings.Shape shape = emitShape;
			if (shape == ParticleSpawnerSettings.Shape.FullCube)
			{
				primitiveModelRenderer2.UpdateModelData(CubeModel.BuildModelData(vector.X, vector.Y, vector.Z));
			}
			else
			{
				primitiveModelRenderer2.UpdateModelData(SphereModel.BuildModelData(vector.X, vector.Y * 2f, 24, 24, vector.Z));
			}
			_particleSpawnAreaRenderers[particleSpawnerId] = primitiveModelRenderer2;
			for (int j = 0; j < reference.Settings.Attractors.Length; j++)
			{
				ref ParticleAttractor reference2 = ref reference.Settings.Attractors[j];
				PrimitiveModelRenderer primitiveModelRenderer3 = new PrimitiveModelRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
				if (reference2.RadialAxis == Vector3.Zero)
				{
					primitiveModelRenderer3.UpdateModelData(SphereModel.BuildModelData(reference2.Radius, reference2.Radius * 2f, 12, 12));
				}
				else
				{
					primitiveModelRenderer3.UpdateModelData(CylinderModel.BuildModelData(reference2.Radius, 300f, 12));
				}
				if (j == 0)
				{
					_groupAttractorRenderers[particleSpawnerId] = new List<PrimitiveModelRenderer>();
				}
				_groupAttractorRenderers[reference.Settings.ParticleSpawnerId].Add(primitiveModelRenderer3);
			}
			for (int k = 0; k < reference.Settings.ParticleSpawnerSettings.Attractors.Length; k++)
			{
				ref ParticleAttractor reference3 = ref reference.Settings.ParticleSpawnerSettings.Attractors[k];
				PrimitiveModelRenderer primitiveModelRenderer4 = new PrimitiveModelRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
				if (reference3.RadialAxis == Vector3.Zero)
				{
					primitiveModelRenderer4.UpdateModelData(SphereModel.BuildModelData(reference3.Radius, reference3.Radius * 2f, 24, 24));
				}
				else
				{
					primitiveModelRenderer4.UpdateModelData(CylinderModel.BuildModelData(reference3.Radius, 300f, 24));
				}
				if (k == 0)
				{
					_spawnerAttractorRenderers[reference.Settings.ParticleSpawnerId] = new List<PrimitiveModelRenderer>();
				}
				_spawnerAttractorRenderers[reference.Settings.ParticleSpawnerId].Add(primitiveModelRenderer4);
			}
		}
	}

	protected override void DoDispose()
	{
		_systemPositionRenderer.Dispose();
		_attractorPositionRenderer.Dispose();
		foreach (PrimitiveModelRenderer value in _spawnerSpawnAreaRenderers.Values)
		{
			value.Dispose();
		}
		foreach (PrimitiveModelRenderer value2 in _particleSpawnAreaRenderers.Values)
		{
			value2.Dispose();
		}
		foreach (List<PrimitiveModelRenderer> value3 in _groupAttractorRenderers.Values)
		{
			foreach (PrimitiveModelRenderer item in value3)
			{
				item.Dispose();
			}
		}
		foreach (List<PrimitiveModelRenderer> value4 in _spawnerAttractorRenderers.Values)
		{
			foreach (PrimitiveModelRenderer item2 in value4)
			{
				item2.Dispose();
			}
		}
		_graphics = null;
	}

	public void Draw(Matrix viewProjectionMatrix)
	{
		Matrix transformMatrix = Matrix.CreateTranslation(_particleSystem.Position);
		_systemPositionRenderer.Draw(viewProjectionMatrix, transformMatrix, _graphics.BlackColor, 1f);
		for (int i = 0; i < _particleSystem.SpawnerGroups.Length; i++)
		{
			ref ParticleSystem.SystemSpawnerGroup reference = ref _particleSystem.SpawnerGroups[i];
			for (int j = 0; j < reference.Settings.Attractors.Length; j++)
			{
				ParticleAttractor particleAttractor = reference.Settings.Attractors[j];
				transformMatrix = Matrix.CreateTranslation(_particleSystem.Position + reference.Settings.Attractors[j].Position);
				_groupAttractorRenderers[reference.Settings.ParticleSpawnerId][j].Draw(viewProjectionMatrix, transformMatrix, _spawnerColors[reference.Settings.ParticleSpawnerId], 1f);
				_attractorPositionRenderer.Draw(viewProjectionMatrix, transformMatrix, _spawnerColors[reference.Settings.ParticleSpawnerId], 1f);
			}
			transformMatrix = Matrix.CreateTranslation(_particleSystem.Position + reference.Settings.PositionOffset);
			_spawnerSpawnAreaRenderers[reference.Settings.ParticleSpawnerId].Draw(viewProjectionMatrix, transformMatrix, _graphics.BlackColor, 1f);
		}
		for (int k = 0; k < _particleSystem.AliveSpawnerCount; k++)
		{
			ref ParticleSystem.SystemSpawner reference2 = ref _particleSystem.SystemSpawners[k];
			ParticleSystemSettings.SystemSpawnerSettings settings = _particleSystem.SpawnerGroups[reference2.GroupId].Settings;
			string particleSpawnerId = settings.ParticleSpawnerId;
			Vector3 vector = _particleSystem.Position + (reference2.Position + settings.PositionOffset) * _particleSystem.Scale;
			transformMatrix = Matrix.CreateTranslation(vector);
			_particleSpawnAreaRenderers[particleSpawnerId].Draw(viewProjectionMatrix, transformMatrix, _spawnerColors[particleSpawnerId], 1f);
			for (int l = 0; l < settings.ParticleSpawnerSettings.Attractors.Length; l++)
			{
				Vector3 vector2 = vector + settings.ParticleSpawnerSettings.Attractors[l].Position;
				transformMatrix = Matrix.CreateFromQuaternion(_particleSystem.Rotation);
				Matrix.AddTranslation(ref transformMatrix, vector2.X, vector2.Y, vector2.Z);
				_spawnerAttractorRenderers[particleSpawnerId][l].Draw(viewProjectionMatrix, transformMatrix, _spawnerColors[particleSpawnerId], 1f);
				_attractorPositionRenderer.Draw(viewProjectionMatrix, transformMatrix, _spawnerColors[particleSpawnerId], 1f);
			}
		}
	}
}
