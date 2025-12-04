using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Graphics.Gizmos;

internal class BoxEditorGizmo : Disposable
{
	public delegate void OnBoxChange(BoundingBox boundingBox);

	private readonly GraphicsDevice _graphics;

	private readonly BoxRenderer _boxRenderer;

	private readonly QuadRenderer _quadRenderer;

	private BoundingBox _startBox;

	private BoundingBox _box;

	private OnBoxChange _onBoxChange;

	private HitDetection.RayBoxCollision _rayBoxHit;

	private Vector3 _origin = Vector3.Zero;

	private Vector3 _position1 = Vector3.NaN;

	private Vector3 _position2 = Vector3.NaN;

	private Vector3 _normal;

	private Vector3 _resizeStart;

	private Vector3[] _localSnapValues;

	private Matrix _tempMatrix;

	private Matrix _matrix;

	public float MinGridSnapDistance = 0.01f;

	public float MaxGridSnapDistance = 0.05f;

	public float MaxLocalSnapDistance = 0.1f;

	public bool Visible { get; private set; }

	public bool IsCursorOverSelection { get; private set; }

	public bool IsResizing { get; private set; }

	public bool IsMoving { get; private set; }

	public bool NeedsDrawing()
	{
		return Visible;
	}

	public BoxEditorGizmo(GraphicsDevice graphics, OnBoxChange onChange)
	{
		_graphics = graphics;
		_onBoxChange = onChange;
		_boxRenderer = new BoxRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_quadRenderer = new QuadRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram.AttribPosition, _graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
	}

	protected override void DoDispose()
	{
		_boxRenderer.Dispose();
		_quadRenderer.Dispose();
	}

	public void Draw(ref Matrix viewProjectionMatrix, Vector3 positionOffset, Vector3 color)
	{
		if (!NeedsDrawing())
		{
			return;
		}
		_graphics.GPUProgramStore.BasicProgram.AssertInUse();
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		_boxRenderer.Draw(_origin, _box, viewProjectionMatrix, color, 0.5f, color, 0.1f);
		if (IsCursorOverSelection || IsResizing || IsMoving)
		{
			Vector3 size = GetSize();
			Vector3 vector = GetBounds().Min + size * 0.5f + positionOffset;
			if (_normal.Y != 0f)
			{
				vector += new Vector3((0f - size.X) / 2f, size.Y / 2f * _normal.Y, (0f - size.Z) / 2f);
				Matrix.CreateScale(size.X, size.Z, 1f, out _matrix);
				Matrix.CreateRotationX((float)System.Math.PI / 2f, out _tempMatrix);
				Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			}
			else if (_normal.X != 0f)
			{
				vector += new Vector3(size.X / 2f * _normal.X, (0f - size.Y) / 2f, (0f - size.Z) / 2f);
				Matrix.CreateScale(size.Z, size.Y, 1f, out _matrix);
				Matrix.CreateRotationY(-(float)System.Math.PI / 2f, out _tempMatrix);
				Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			}
			else if (_normal.Z != 0f)
			{
				vector += new Vector3((0f - size.X) / 2f, (0f - size.Y) / 2f, size.Z / 2f * _normal.Z);
				Matrix.CreateScale(size.X, size.Y, 1f, out _matrix);
			}
			Matrix.CreateTranslation(vector.X, vector.Y, vector.Z, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
			BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
			basicProgram.MVPMatrix.SetValue(ref _matrix);
			basicProgram.Color.SetValue(color);
			basicProgram.Opacity.SetValue(0.4f);
			_quadRenderer.Draw();
		}
	}

	public void Tick(Ray viewRay, bool altDown = false)
	{
		if (!Visible)
		{
			return;
		}
		_ = _box;
		if (HitDetection.CheckRayBoxCollision(GetBounds(), viewRay.Position, viewRay.Direction, out _rayBoxHit))
		{
			IsCursorOverSelection = true;
			if (!IsResizing && !IsMoving)
			{
				_normal = _rayBoxHit.Normal;
			}
		}
		else
		{
			IsCursorOverSelection = false;
		}
		if (IsResizing)
		{
			OnResize(viewRay, !altDown);
		}
		else if (IsMoving)
		{
			OnMove(viewRay, !altDown);
		}
	}

	public void Show(Vector3 origin, BoundingBox box, Vector3[] snapValues = null)
	{
		_origin = origin;
		_box = box;
		_position1 = _box.Min + origin;
		_position2 = _box.Max + origin;
		_localSnapValues = snapValues ?? new Vector3[0];
		Visible = true;
	}

	public void Hide()
	{
		Visible = false;
		IsResizing = false;
		IsMoving = false;
	}

	public bool InUse()
	{
		return IsMoving || IsResizing;
	}

	public void ResetBox()
	{
		_position1 = _startBox.Min + _origin;
		_position2 = _startBox.Max + _origin;
		OnChange();
	}

	public void OnInteract(InteractionType interactionType, Ray viewRay, bool shiftDown, bool altDown)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		if (IsResizing || IsMoving)
		{
			IsResizing = false;
			IsMoving = false;
			if ((int)interactionType == 1)
			{
				ResetBox();
			}
		}
		else if ((int)interactionType == 0)
		{
			_startBox = _box;
			if (shiftDown)
			{
				IsMoving = true;
				_resizeStart = _rayBoxHit.Position;
				_normal = _rayBoxHit.Normal;
			}
			else
			{
				IsResizing = true;
				_resizeStart = _rayBoxHit.Position;
				_normal = _rayBoxHit.Normal;
			}
		}
		else
		{
			Hide();
		}
	}

	private void OnResize(Ray viewRay, bool useValueSnap)
	{
		Vector3 projectedCursorPosition = GetProjectedCursorPosition(viewRay);
		float value = ((_normal == Vector3.Up || _normal == Vector3.Down) ? projectedCursorPosition.Y : ((_normal == Vector3.Left || _normal == Vector3.Right) ? projectedCursorPosition.X : projectedCursorPosition.Z));
		float[] array = new float[_localSnapValues.Length];
		for (int i = 0; i < _localSnapValues.Length; i++)
		{
			if (_normal == Vector3.Up || _normal == Vector3.Down)
			{
				array[i] = _origin.Y + _localSnapValues[i].Y;
			}
			else if (_normal == Vector3.Left || _normal == Vector3.Right)
			{
				array[i] = _origin.X + _localSnapValues[i].X;
			}
			else
			{
				array[i] = _origin.Z + _localSnapValues[i].Z;
			}
		}
		float gridSnapValue = GetGridSnapValue(value, useValueSnap ? MaxGridSnapDistance : MinGridSnapDistance);
		float localSnapValue = GetLocalSnapValue(value, MaxLocalSnapDistance, array);
		value = ((useValueSnap && !float.IsInfinity(localSnapValue)) ? localSnapValue : gridSnapValue);
		if (_normal == Vector3.Up)
		{
			if (_position1.Y > _position2.Y)
			{
				_position1.Y = value;
			}
			else
			{
				_position2.Y = value;
			}
		}
		else if (_normal == Vector3.Down)
		{
			if (_position1.Y < _position2.Y)
			{
				_position1.Y = value;
			}
			else
			{
				_position2.Y = value;
			}
		}
		else if (_normal == Vector3.Left)
		{
			if (_position1.X < _position2.X)
			{
				_position1.X = value;
			}
			else
			{
				_position2.X = value;
			}
		}
		else if (_normal == Vector3.Right)
		{
			if (_position1.X > _position2.X)
			{
				_position1.X = value;
			}
			else
			{
				_position2.X = value;
			}
		}
		else if (_normal == Vector3.Forward)
		{
			if (_position1.Z > _position2.Z)
			{
				_position2.Z = value;
			}
			else
			{
				_position1.Z = value;
			}
		}
		else if (_normal == Vector3.Backward)
		{
			if (_position1.Z < _position2.Z)
			{
				_position2.Z = value;
			}
			else
			{
				_position1.Z = value;
			}
		}
		OnChange();
	}

	private void OnMove(Ray viewRay, bool useValueSnap)
	{
		Vector3 size = GetSize();
		Vector3 projectedCursorPosition = GetProjectedCursorPosition(viewRay);
		float value = ((_normal == Vector3.Up || _normal == Vector3.Down) ? projectedCursorPosition.Y : ((_normal == Vector3.Left || _normal == Vector3.Right) ? projectedCursorPosition.X : projectedCursorPosition.Z));
		float[] array = new float[_localSnapValues.Length];
		for (int i = 0; i < _localSnapValues.Length; i++)
		{
			if (_normal == Vector3.Up || _normal == Vector3.Down)
			{
				array[i] = _origin.Y + _localSnapValues[i].Y;
			}
			else if (_normal == Vector3.Left || _normal == Vector3.Right)
			{
				array[i] = _origin.X + _localSnapValues[i].X;
			}
			else
			{
				array[i] = _origin.Z + _localSnapValues[i].Z;
			}
		}
		float gridSnapValue = GetGridSnapValue(value, useValueSnap ? MaxGridSnapDistance : MinGridSnapDistance);
		float localSnapValue = GetLocalSnapValue(value, MaxLocalSnapDistance, array);
		value = ((useValueSnap && !float.IsInfinity(localSnapValue)) ? localSnapValue : gridSnapValue);
		if (_normal == Vector3.Up)
		{
			if (_position1.Y > _position2.Y)
			{
				_position1.Y = value;
				_position2.Y = _position1.Y - size.Y;
			}
			else
			{
				_position2.Y = value;
				_position1.Y = _position2.Y - size.Y;
			}
		}
		else if (_normal == Vector3.Down)
		{
			if (_position1.Y < _position2.Y)
			{
				_position1.Y = value;
				_position2.Y = _position1.Y + size.Y;
			}
			else
			{
				_position2.Y = value;
				_position1.Y = _position2.Y - size.Y;
			}
		}
		else if (_normal == Vector3.Left)
		{
			if (_position1.X < _position2.X)
			{
				_position1.X = value;
				_position2.X = _position1.X + size.X;
			}
			else
			{
				_position2.X = value;
				_position1.X = _position2.X - size.X;
			}
		}
		else if (_normal == Vector3.Right)
		{
			if (_position1.X > _position2.X)
			{
				_position1.X = value;
				_position2.X = _position1.X - size.X;
			}
			else
			{
				_position2.X = value;
				_position1.X = _position2.X - size.X;
			}
		}
		else if (_normal == Vector3.Forward)
		{
			if (_position1.Z > _position2.Z)
			{
				_position2.Z = value;
				_position1.Z = _position2.Z + size.Z;
			}
			else
			{
				_position1.Z = value;
				_position2.Z = _position1.Z - size.Z;
			}
		}
		else if (_normal == Vector3.Backward)
		{
			if (_position1.Z < _position2.Z)
			{
				_position2.Z = value;
				_position1.Z = _position2.Z - size.Z;
			}
			else
			{
				_position1.Z = value;
				_position2.Z = _position1.Z - size.Z;
			}
		}
		OnChange();
	}

	private void OnChange()
	{
		_box = new BoundingBox(Vector3.Min(_position1, _position2), Vector3.Max(_position1, _position2));
		_box.Translate(-_origin);
		_box.Min.X = (float)System.Math.Round(_box.Min.X, 2);
		_box.Min.Y = (float)System.Math.Round(_box.Min.Y, 2);
		_box.Min.Z = (float)System.Math.Round(_box.Min.Z, 2);
		_box.Max.X = (float)System.Math.Round(_box.Max.X, 2);
		_box.Max.Y = (float)System.Math.Round(_box.Max.Y, 2);
		_box.Max.Z = (float)System.Math.Round(_box.Max.Z, 2);
		_onBoxChange?.Invoke(_box);
	}

	private Vector3 GetSize()
	{
		if (!Visible)
		{
			return Vector3.Zero;
		}
		return Vector3.Max(_position1, _position2) - Vector3.Min(_position1, _position2);
	}

	private Vector3 GetProjectedCursorPosition(Ray viewRay)
	{
		Vector3 vector = _resizeStart - viewRay.Position;
		Vector2 ray1Position;
		Vector2 ray1Direction;
		Vector2 ray2Position;
		Vector2 ray2Direction;
		if (_normal == Vector3.Up || _normal == Vector3.Down)
		{
			float x = vector.X;
			vector.X = vector.Z;
			vector.Z = 0f - x;
			ray1Position = new Vector2(viewRay.Position.X, viewRay.Position.Z);
			ray1Direction = new Vector2(viewRay.Direction.X, viewRay.Direction.Z);
			ray2Position = new Vector2(_resizeStart.X, _resizeStart.Z);
			ray2Direction = new Vector2(vector.X, vector.Z);
		}
		else if (_normal == Vector3.Left || _normal == Vector3.Right)
		{
			float x = vector.Y;
			vector.Y = vector.Z;
			vector.Z = 0f - x;
			ray1Position = new Vector2(viewRay.Position.Y, viewRay.Position.Z);
			ray1Direction = new Vector2(viewRay.Direction.Y, viewRay.Direction.Z);
			ray2Position = new Vector2(_resizeStart.Y, _resizeStart.Z);
			ray2Direction = new Vector2(vector.Y, vector.Z);
		}
		else
		{
			float x = vector.X;
			vector.X = vector.Y;
			vector.Y = 0f - x;
			ray1Position = new Vector2(viewRay.Position.X, viewRay.Position.Y);
			ray1Direction = new Vector2(viewRay.Direction.X, viewRay.Direction.Y);
			ray2Position = new Vector2(_resizeStart.X, _resizeStart.Y);
			ray2Direction = new Vector2(vector.X, vector.Y);
		}
		if (HitDetection.Get2DRayIntersection(ray1Position, ray1Direction, ray2Position, ray2Direction, out var intersection))
		{
			float num;
			if (_normal == Vector3.Up || _normal == Vector3.Down)
			{
				num = (intersection.X - viewRay.Position.X) / viewRay.Direction.X;
				return new Vector3(intersection.X, num * viewRay.Direction.Y + viewRay.Position.Y, intersection.Y);
			}
			if (_normal == Vector3.Left || _normal == Vector3.Right)
			{
				num = (intersection.Y - viewRay.Position.Z) / viewRay.Direction.Z;
				return new Vector3(num * viewRay.Direction.X + viewRay.Position.X, intersection.X, intersection.Y);
			}
			num = (intersection.X - viewRay.Position.X) / viewRay.Direction.X;
			return new Vector3(intersection.X, intersection.Y, num * viewRay.Direction.Z + viewRay.Position.Z);
		}
		return viewRay.Position;
	}

	private BoundingBox GetBounds()
	{
		BoundingBox box = _box;
		box.Translate(_origin);
		return box;
	}

	private static float GetGridSnapValue(float value, float increment)
	{
		float num = value % increment;
		if (num == 0f)
		{
			return value;
		}
		value -= num;
		if (num * 2f >= increment)
		{
			value += increment;
		}
		else if (num * 2f < 0f - increment)
		{
			value -= increment;
		}
		return value;
	}

	private static float GetLocalSnapValue(float value, float maxSnapDistance, float[] snapValues)
	{
		float result = float.PositiveInfinity;
		float num = float.NaN;
		for (int i = 0; i < snapValues.Length; i++)
		{
			float num2 = System.Math.Abs(snapValues[i] - value);
			if (num2 <= maxSnapDistance && (num2 < num || float.IsNaN(num)))
			{
				result = snapValues[i];
				num = num2;
			}
		}
		return result;
	}
}
