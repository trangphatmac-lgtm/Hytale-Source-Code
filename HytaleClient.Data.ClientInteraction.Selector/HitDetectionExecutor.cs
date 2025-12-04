using System;
using System.Collections.Generic;
using HytaleClient.Data.Map;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Collision;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal class HitDetectionExecutor
{
	public static LineOfSightProvider DefaultLineOfSightTrue = (GameInstance gameInstance, float x, float y, float z, float toX, float toY, float toZ) => true;

	public static LineOfSightProvider DefaultLineOfSightSolid = delegate(GameInstance gameInstance, float x, float y, float z, float toX, float toY, float toZ)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		Vector3 vector = new Vector3(x, y, z);
		Vector3 direction = new Vector3(toX, toY, toZ) - vector;
		float maxDistance = direction.Length();
		BlockIterator blockIterator = new BlockIterator(vector, direction, maxDistance);
		BlockAccessor blockAccessor = new BlockAccessor(gameInstance.MapModule);
		while (blockIterator.HasNext())
		{
			blockIterator.Step(out var b, out var _, out var _, out var _);
			ClientBlockType blockTypeFiller = blockAccessor.GetBlockTypeFiller(b);
			if ((int)blockTypeFiller.CollisionMaterial == 1)
			{
				return false;
			}
		}
		return true;
	};

	private static Vector4[] VERTEX_POINTS = new Vector4[8]
	{
		new Vector4(new Vector3(0f, 1f, 1f), 1f),
		new Vector4(new Vector3(0f, 1f, 0f), 1f),
		new Vector4(new Vector3(1f, 1f, 1f), 1f),
		new Vector4(new Vector3(1f, 1f, 0f), 1f),
		new Vector4(new Vector3(0f, 0f, 1f), 1f),
		new Vector4(new Vector3(0f, 0f, 0f), 1f),
		new Vector4(new Vector3(1f, 0f, 1f), 1f),
		new Vector4(new Vector3(1f, 0f, 0f), 1f)
	};

	public static Quad4[] CUBE_QUADS = new Quad4[6]
	{
		new Quad4(VERTEX_POINTS, 0, 1, 3, 2),
		new Quad4(VERTEX_POINTS, 0, 4, 5, 1),
		new Quad4(VERTEX_POINTS, 4, 5, 7, 6),
		new Quad4(VERTEX_POINTS, 2, 3, 7, 6),
		new Quad4(VERTEX_POINTS, 1, 3, 7, 5),
		new Quad4(VERTEX_POINTS, 0, 2, 6, 4)
	};

	private Matrix _pvmMatrix;

	private Matrix _invPvMatrix;

	private Vector4 _origin;

	private readonly HitDetectionBuffer _buffer = new HitDetectionBuffer();

	public Matrix ProjectionMatrix;

	public Matrix ViewMatrix;

	public LineOfSightProvider LosProvider = DefaultLineOfSightSolid;

	private int _maxRayTests = 10;

	private readonly Random _random;

	public HitDetectionExecutor(Random random)
	{
		_random = random;
	}

	public void SetOrigin(Vector3 origin)
	{
		_origin = new Vector4(origin);
	}

	public Vector4 GetHitLocation()
	{
		return _buffer.HitPosition;
	}

	public bool Test(GameInstance gameInstance, Quad4[] model, Matrix modelMatrix)
	{
		SetupMatrices(modelMatrix);
		return TestModel(gameInstance, model);
	}

	private void SetupMatrices(Matrix modelMatrix)
	{
		_pvmMatrix = ViewMatrix * ProjectionMatrix;
		_invPvMatrix = Matrix.Invert(_pvmMatrix);
		_pvmMatrix = modelMatrix * _pvmMatrix;
	}

	private bool TestModel(GameInstance gameInstance, Quad4[] model)
	{
		int num = 0;
		double num2 = double.PositiveInfinity;
		foreach (Quad4 quad in model)
		{
			if (num++ == _maxRayTests)
			{
				return false;
			}
			_buffer.TransformedQuad = quad.Multiply(_pvmMatrix);
			if (InsideFrustum())
			{
				Vector4 vector = ((!_buffer.ContainsFully) ? _buffer.VisibleTriangle.GetRandom(_random) : _buffer.TransformedQuad.GetRandom(_random));
				vector = Vector4.Transform(vector, _invPvMatrix).PerspectiveTransform();
				double num3 = _origin.X - vector.X;
				double num4 = _origin.Y - vector.Y;
				double num5 = _origin.Z - vector.Z;
				double num6 = num3 * num3 + num4 * num4 + num5 * num5;
				if (!(num6 >= num2) && LosProvider(gameInstance, _origin.X, _origin.Y, _origin.Z, vector.X, vector.Y, vector.Z))
				{
					num2 = num6;
					_buffer.HitPosition = vector;
				}
			}
		}
		return !double.IsPositiveInfinity(num2);
	}

	private bool InsideFrustum()
	{
		Quad4 transformedQuad = _buffer.TransformedQuad;
		if (transformedQuad.IsFullyInsideFrustum())
		{
			_buffer.ContainsFully = true;
			return true;
		}
		_buffer.ContainsFully = false;
		List<Vector4> list = new List<Vector4>();
		list.Add(transformedQuad.A);
		list.Add(transformedQuad.B);
		list.Add(transformedQuad.C);
		list.Add(transformedQuad.D);
		if (ClipPolygonAxis(list, 0) && ClipPolygonAxis(list, 1) && ClipPolygonAxis(list, 2))
		{
			Vector4 a = list[0];
			_buffer.VisibleTriangle = new Triangle4(a, list[1], list[2]);
			return true;
		}
		return false;
	}

	private bool ClipPolygonAxis(List<Vector4> vertices, int componentIndex)
	{
		List<Vector4> list = ClipPolygonComponent(vertices, componentIndex, 1f);
		vertices.Clear();
		if (list.Count == 0)
		{
			return false;
		}
		List<Vector4> list2 = ClipPolygonComponent(list, componentIndex, -1f);
		vertices.AddRange(list2);
		return list2.Count != 0;
	}

	private List<Vector4> ClipPolygonComponent(List<Vector4> vertices, int componentIndex, float componentFactor)
	{
		List<Vector4> list = new List<Vector4>();
		Vector4 value = vertices[vertices.Count - 1];
		float num = value.Get(componentIndex) * componentFactor;
		bool flag = num <= value.W;
		foreach (Vector4 vertex in vertices)
		{
			float num2 = vertex.Get(componentIndex) * componentFactor;
			bool flag2 = num2 <= vertex.W;
			if (flag2 ^ flag)
			{
				float amount = (value.W - num) / (value.W - num - (vertex.W - num2));
				list.Add(Vector4.Lerp(value, vertex, amount));
			}
			if (flag2)
			{
				list.Add(vertex);
			}
			value = vertex;
			num = num2;
			flag = flag2;
		}
		return list;
	}
}
