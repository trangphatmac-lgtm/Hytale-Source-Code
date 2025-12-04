using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Interaction;

internal class BlockBreakHealth : Disposable
{
	private readonly GameInstance _gameInstance;

	private static readonly Vector3 baseOffset = new Vector3(0.5f, 0.5f, 0.5f);

	private readonly Dictionary<Vector3, Entity> _entities = new Dictionary<Vector3, Entity>();

	private readonly Vector3 _offset = baseOffset;

	public bool IsEnabled => _gameInstance.App.Settings.BlockHealth;

	public BlockBreakHealth(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	private Entity SpawnEntity(Vector3 position)
	{
		_gameInstance.EntityStoreModule.Spawn(-1, out var entity);
		entity.SetIsTangible(isTangible: false);
		entity.VisibilityPrediction = true;
		_gameInstance.AudioModule.TryRegisterSoundObject(Vector3.Zero, Vector3.Zero, ref entity.SoundObjectReference);
		entity.SetPosition(new Vector3(position.X + _offset.X, position.Y + _offset.Y, position.Z + _offset.Z));
		entity.PositionProgress = 1f;
		return entity;
	}

	protected override void DoDispose()
	{
		foreach (KeyValuePair<Vector3, Entity> entry in _entities)
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_gameInstance.EntityStoreModule.Despawn(entry.Value.NetworkId);
			}, allowCallFromMainThread: true);
		}
		_entities.Clear();
	}

	public bool NeedsDrawing()
	{
		return IsEnabled && _entities.Count > 0;
	}

	public void Draw()
	{
		SceneRenderer.SceneData data = _gameInstance.SceneRenderer.Data;
		float x = data.ViewportSize.X;
		float y = data.ViewportSize.Y;
		foreach (KeyValuePair<Vector3, Entity> entity in _entities)
		{
			Entity value = entity.Value;
			Vector3 position = value.Position;
			Vector2 vector = Vector3.WorldToScreenPos(ref data.ViewProjectionMatrix, x, y, position);
			Matrix.CreateTranslation(vector.X - x / 2f, 0f - (vector.Y - y / 2f), 0f, out var result);
			Vector3 position2 = _gameInstance.CameraModule.Controller.Position;
			float distanceToCamera = Vector3.Distance(value.RenderPosition, position2);
			_gameInstance.App.Interface.InGameView.RegisterEntityUIDrawTasks(ref result, value, distanceToCamera);
		}
	}

	public Entity GetEntity(Vector3 position, int blockId)
	{
		_entities.TryGetValue(position, out var value);
		if (value == null && blockId > 0)
		{
			value = SpawnEntity(position);
			_entities[position] = value;
		}
		return value;
	}

	public void UpdateHealth(int blockId, int worldX, int worldY, int worldZ, float maxBlockHealth, float health)
	{
		Vector3 vector = new Vector3(worldX, worldY, worldZ);
		Entity entity = GetEntity(vector, blockId);
		if (entity != null)
		{
			if (health == maxBlockHealth || blockId == -1)
			{
				_gameInstance.EntityStoreModule.Despawn(entity.NetworkId);
				_entities.Remove(vector);
			}
			else
			{
				entity.SmoothHealth = health;
			}
		}
	}
}
