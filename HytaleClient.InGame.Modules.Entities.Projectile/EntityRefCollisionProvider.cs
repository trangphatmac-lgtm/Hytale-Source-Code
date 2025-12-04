using System;
using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class EntityRefCollisionProvider
{
	protected const int AllocSize = 4;

	protected const float ExtraDistance = 8f;

	protected EntityContactData[] _contacts;

	protected EntityContactData[] _sortBuffer;

	public int Count;

	protected Vector2 _minMax = default(Vector2);

	protected Vector3 _collisionPosition = default(Vector3);

	protected float _nearestCollisionStart;

	protected Vector3 _position;

	protected Vector3 _direction;

	protected BoundingBox _boundingBox;

	protected Func<Entity, bool> _entityFilter;

	protected Entity _ignoreSelf;

	protected Entity _ignoreOther;

	public EntityRefCollisionProvider()
	{
		_contacts = new EntityContactData[4];
		_sortBuffer = new EntityContactData[4];
		for (int i = 0; i < _contacts.Length; i++)
		{
			_contacts[i] = new EntityContactData();
		}
	}

	public EntityContactData GetContact(int i)
	{
		return _contacts[i];
	}

	public void Clear()
	{
		for (int i = 0; i < Count; i++)
		{
			_contacts[i].Clear();
		}
		Count = 0;
	}

	public float ComputeNearest(GameInstance gameInstance, BoundingBox entityBoundingBox, Vector3 pos, Vector3 dir, Entity ignoreSelf, Entity ignore)
	{
		return ComputeNearest(gameInstance, pos, dir, entityBoundingBox, dir.Length() + 8f, DefaultEntityFilter, ignoreSelf, ignore);
	}

	public float ComputeNearest(GameInstance gameInstance, Vector3 pos, Vector3 dir, BoundingBox boundingBox, float radius, Func<Entity, bool> entityFilter, Entity ignoreSelf, Entity ignoreOther)
	{
		_ignoreSelf = ignoreSelf;
		_ignoreOther = ignoreOther;
		_nearestCollisionStart = float.MaxValue;
		_entityFilter = entityFilter;
		IterateEntitiesInSphere(gameInstance, pos, dir, boundingBox, radius, delegate(EntityRefCollisionProvider provider, Entity entity)
		{
			provider.AcceptNearestIgnore(entity);
		});
		if (Count == 0)
		{
			_nearestCollisionStart = float.MinValue;
		}
		ClearRefs();
		_ignoreSelf = null;
		_ignoreOther = null;
		return _nearestCollisionStart;
	}

	protected void IterateEntitiesInSphere(GameInstance gameInstance, Vector3 pos, Vector3 dir, BoundingBox boundingBox, float radius, Action<EntityRefCollisionProvider, Entity> consumer)
	{
		_position = pos;
		_direction = dir;
		_boundingBox = boundingBox;
		List<Entity> entitiesInSphere = gameInstance.EntityStoreModule.GetEntitiesInSphere(pos, radius);
		for (int i = 0; i < entitiesInSphere.Count; i++)
		{
			Entity arg = entitiesInSphere[i];
			consumer(this, arg);
		}
	}

	protected void SetContact(Entity entity, string detailName)
	{
		_collisionPosition = _position + _direction * _minMax.X;
		_contacts[0].Assign(_collisionPosition, _minMax.X, _minMax.Y, entity, detailName);
		Count = 1;
	}

	protected bool IsColliding(Entity entity, ref Vector2 minMax, out string hitDetail)
	{
		if (entity.DetailBoundingBoxes.Count > 0)
		{
			Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, entity.BodyOrientation.Y);
			foreach (KeyValuePair<string, Entity.DetailBoundingBox[]> detailBoundingBox2 in entity.DetailBoundingBoxes)
			{
				Entity.DetailBoundingBox[] value = detailBoundingBox2.Value;
				for (int i = 0; i < value.Length; i++)
				{
					Entity.DetailBoundingBox detailBoundingBox = value[i];
					Vector3 value2 = detailBoundingBox.Offset;
					Vector3.Transform(ref value2, ref rotation, out value2);
					value2 += entity.NextPosition;
					if (CollisionMath.IntersectSweptAABBs(_position, _direction, _boundingBox, value2, detailBoundingBox.Box, ref minMax) && minMax.X <= 1f)
					{
						hitDetail = detailBoundingBox2.Key;
						return true;
					}
				}
			}
			hitDetail = null;
			return false;
		}
		hitDetail = null;
		return CollisionMath.IntersectSweptAABBs(_position, _direction, _boundingBox, entity.NextPosition, entity.Hitbox, ref minMax) && minMax.X <= 1f;
	}

	protected void ClearRefs()
	{
		_position = Vector3.Zero;
		_direction = Vector3.Zero;
		_entityFilter = null;
	}

	public static bool DefaultEntityFilter(Entity entity)
	{
		if (entity.IsDead())
		{
			return false;
		}
		if (entity.Predictable)
		{
			return false;
		}
		if (!entity.IsTangible())
		{
			return false;
		}
		return true;
	}

	protected void AcceptNearestIgnore(Entity entity)
	{
		if (_entityFilter(entity) && !entity.Equals(_ignoreSelf) && !entity.Equals(_ignoreOther) && IsColliding(entity, ref _minMax, out var hitDetail) && _minMax.X < _nearestCollisionStart)
		{
			_nearestCollisionStart = _minMax.X;
			SetContact(entity, hitDetail);
		}
	}
}
