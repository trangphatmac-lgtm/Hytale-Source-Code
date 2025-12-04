using System;
using System.Collections.Generic;
using HytaleClient.Core;
using HytaleClient.Graphics.Gizmos.Models;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Graphics.Gizmos;

internal class TranslationGizmo : Disposable
{
	public delegate void OnPositionChange(Vector3 position);

	private enum Axis
	{
		X,
		Y,
		Z,
		PlaneX,
		PlaneY,
		PlaneZ,
		None
	}

	private static PrimitiveModelData _modelData;

	private readonly GraphicsDevice _graphics;

	private readonly QuadRenderer _quadRenderer;

	private readonly PrimitiveModelRenderer _modelRenderer;

	private readonly LineRenderer _lineRenderer;

	public bool Visible = false;

	private OnPositionChange _onChange;

	private Vector3 _position = Vector3.Zero;

	private Vector3 _rotation = Vector3.Zero;

	private Vector3 _size = Vector3.One;

	private Vector3 _lastHitPosition = Vector3.Zero;

	private Vector3 _lastPosition = Vector3.Zero;

	private Vector3 _startPosition = Vector3.Zero;

	private Vector3 _startOffset = Vector3.Zero;

	private Matrix _tempMatrix;

	private Matrix _drawMatrix;

	private Axis _highlightedAxis = Axis.None;

	private Axis _selectedAxis = Axis.None;

	private static readonly Dictionary<Axis, float> _axisDirections = new Dictionary<Axis, float>
	{
		{
			Axis.X,
			(float)System.Math.PI / 2f
		},
		{
			Axis.Y,
			(float)System.Math.PI / 2f
		},
		{
			Axis.Z,
			(float)System.Math.PI
		},
		{
			Axis.PlaneX,
			(float)System.Math.PI / 2f
		},
		{
			Axis.PlaneY,
			(float)System.Math.PI / 2f
		},
		{
			Axis.PlaneZ,
			(float)System.Math.PI
		}
	};

	private long _timeOfLastToolInteraction = 0L;

	public TranslationGizmo(GraphicsDevice graphics, OnPositionChange onChange)
	{
		_graphics = graphics;
		_onChange = onChange;
		_quadRenderer = new QuadRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram.AttribPosition, _graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
		_modelRenderer = new PrimitiveModelRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_lineRenderer = new LineRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_lineRenderer.UpdateLineData(new Vector3[2]
		{
			new Vector3(0f, -500f, 0f),
			new Vector3(0f, 500f, 0f)
		});
		if (_modelData == null)
		{
			BuildModelData();
		}
		_modelRenderer.UpdateModelData(_modelData);
		_size *= 0.45f;
	}

	protected override void DoDispose()
	{
		_quadRenderer.Dispose();
		_modelRenderer.Dispose();
		_lineRenderer.Dispose();
	}

	public void Draw(ref Matrix viewProjectionMatrix, Vector3 renderPositionOffset)
	{
		if (!Visible)
		{
			return;
		}
		GLFunctions gL = _graphics.GL;
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.BindTexture(GL.TEXTURE_2D, _graphics.WhitePixelTexture.GLTexture);
		basicProgram.AssertInUse();
		gL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		gL.DepthFunc(GL.ALWAYS);
		Vector3 vector = Vector3.One;
		foreach (KeyValuePair<Axis, float> axisDirection in _axisDirections)
		{
			if ((axisDirection.Key == Axis.PlaneX || axisDirection.Key == Axis.PlaneY || axisDirection.Key == Axis.PlaneZ) && (_selectedAxis == Axis.None || _selectedAxis == axisDirection.Key))
			{
				basicProgram.Opacity.SetValue((_highlightedAxis == axisDirection.Key) ? 0.7f : 0.3f);
				_tempMatrix = Matrix.CreateTranslation(0f, 0f, 0f);
				_drawMatrix = Matrix.CreateScale(_size.X, _size.Y, _size.Z);
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				switch (axisDirection.Key)
				{
				case Axis.PlaneX:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + (float)System.Math.PI / 2f, 0f, 0f);
					vector = _graphics.RedColor;
					break;
				case Axis.PlaneY:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + (float)System.Math.PI, (float)System.Math.PI / 2f, 0f);
					vector = _graphics.GreenColor;
					break;
				case Axis.PlaneZ:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + (float)System.Math.PI, 0f, 0f);
					vector = _graphics.BlueColor;
					break;
				}
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				Vector3 position = _position + renderPositionOffset;
				position.Y += 0.01f;
				_tempMatrix = Matrix.CreateTranslation(position);
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				Matrix.Multiply(ref _drawMatrix, ref viewProjectionMatrix, out _drawMatrix);
				basicProgram.MVPMatrix.SetValue(ref _drawMatrix);
				basicProgram.Color.SetValue(vector);
				_quadRenderer.Draw();
			}
		}
		foreach (KeyValuePair<Axis, float> axisDirection2 in _axisDirections)
		{
			if ((axisDirection2.Key == Axis.X || axisDirection2.Key == Axis.Y || axisDirection2.Key == Axis.Z) && (_selectedAxis == Axis.None || _selectedAxis == axisDirection2.Key))
			{
				float opacity = ((_highlightedAxis == axisDirection2.Key) ? 0.7f : 0.3f);
				_tempMatrix = Matrix.CreateTranslation(0f, 0f, 0f);
				_drawMatrix = Matrix.CreateScale(0.1f, 0.3f, 0.1f);
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				switch (axisDirection2.Key)
				{
				case Axis.X:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y - (float)System.Math.PI / 2f, (float)System.Math.PI / 2f, 0f);
					vector = _graphics.RedColor;
					break;
				case Axis.Y:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + (float)System.Math.PI, 0f, 0f);
					vector = _graphics.GreenColor;
					break;
				case Axis.Z:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + (float)System.Math.PI, (float)System.Math.PI / 2f, 0f);
					vector = _graphics.BlueColor;
					break;
				}
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				Vector3 position2 = _position + renderPositionOffset;
				_tempMatrix = Matrix.CreateTranslation(position2);
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				basicProgram.Color.SetValue(vector);
				_modelRenderer.Draw(viewProjectionMatrix, _drawMatrix, vector, opacity, GL.TRIANGLES);
				if (_selectedAxis == axisDirection2.Key)
				{
					Matrix.Multiply(ref _drawMatrix, ref viewProjectionMatrix, out _drawMatrix);
					_lineRenderer.Draw(ref _drawMatrix, vector, 0.5f);
				}
			}
		}
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}

	public void Tick(Ray playerViewRay)
	{
		if (!Visible)
		{
			return;
		}
		float num = float.PositiveInfinity;
		_highlightedAxis = Axis.None;
		foreach (Axis key in _axisDirections.Keys)
		{
			if (CheckRayIntersection(playerViewRay, key, out var intersection) && (_selectedAxis == Axis.None || key == _selectedAxis))
			{
				float num2 = Vector3.Distance(intersection, playerViewRay.Position);
				if (num == float.PositiveInfinity || num > num2)
				{
					_highlightedAxis = key;
					num = num2;
					_lastHitPosition = intersection;
				}
			}
		}
		if (_selectedAxis != Axis.None)
		{
			UpdatePosition(playerViewRay, _selectedAxis);
		}
	}

	public void Show(Vector3 position, Vector3 rotation, OnPositionChange onChange = null)
	{
		_position = position;
		_rotation = rotation;
		Visible = true;
		if (onChange != null)
		{
			_onChange = onChange;
		}
	}

	public void Hide()
	{
		Visible = false;
	}

	public void OnInteract(Ray playerViewRay, InteractionType interactionType)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		long num = DateTime.UtcNow.Ticks / 10000;
		if (num - _timeOfLastToolInteraction < 250)
		{
			return;
		}
		_timeOfLastToolInteraction = num;
		if ((int)interactionType == 0)
		{
			if (_highlightedAxis != Axis.None && _selectedAxis == Axis.None)
			{
				_selectedAxis = _highlightedAxis;
				_lastPosition = _position;
				_startPosition = _lastHitPosition;
				_startPosition += _position - GetProjectedCursorPosition(playerViewRay, _selectedAxis);
				_startPosition = GetProjectedCursorPosition(playerViewRay, _selectedAxis);
				_startOffset = _position - _startPosition;
			}
			else if (_selectedAxis != Axis.None)
			{
				_selectedAxis = Axis.None;
			}
		}
		else if (_selectedAxis == Axis.None)
		{
			Visible = false;
		}
		else
		{
			_position = _lastPosition;
			_selectedAxis = Axis.None;
			_onChange(_position);
		}
	}

	private void UpdatePosition(Ray playerViewRay, Axis axis)
	{
		_position = GetProjectedCursorPosition(playerViewRay, axis) + _startOffset;
		_onChange(_position);
	}

	private bool CheckRayIntersection(Ray viewRay, Axis axis, out Vector3 intersection)
	{
		Vector3 position = viewRay.Position - _position;
		float num = ((axis == Axis.PlaneZ) ? ((float)System.Math.PI) : ((float)System.Math.PI / 2f));
		if (axis == Axis.PlaneY)
		{
			Matrix.CreateFromYawPitchRoll((float)System.Math.PI - _rotation.Y, 0f, 0f, out _tempMatrix);
			Matrix.CreateFromYawPitchRoll(0f, 0f - num, 0f, out _drawMatrix);
			Matrix.Multiply(ref _tempMatrix, ref _drawMatrix, out _tempMatrix);
		}
		else
		{
			Matrix.CreateFromYawPitchRoll(0f - (_rotation.Y + num), 0f, 0f, out _tempMatrix);
		}
		Matrix.CreateTranslation(ref position, out _drawMatrix);
		Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
		Vector3 direction = Vector3.Transform(viewRay.Direction, _tempMatrix);
		Ray ray = new Ray(_drawMatrix.Translation + _position, direction);
		if (axis == Axis.PlaneX || axis == Axis.PlaneY || axis == Axis.PlaneZ)
		{
			float num2 = ((ray.Direction.Z >= 0f) ? ((ray.Position.Z - _position.Z) / (0f - ray.Direction.Z)) : ((_position.Z - ray.Position.Z) / ray.Direction.Z));
			Vector3 vector = ray.Position + ray.Direction * num2;
			if (vector.Y >= _position.Y && vector.Y <= _position.Y + _size.Y && vector.X >= _position.X && vector.X <= _position.X + _size.X)
			{
				intersection = viewRay.Position + viewRay.Direction * num2;
				return true;
			}
		}
		else
		{
			BoundingBox box = axis switch
			{
				Axis.X => new BoundingBox(new Vector3(-0.3f, -0.3f, -1.4f), new Vector3(0.3f, 0.3f, -0.5f)), 
				Axis.Y => new BoundingBox(new Vector3(-0.3f, 0.5f, -0.3f), new Vector3(0.3f, 1.4f, 0.3f)), 
				_ => new BoundingBox(new Vector3(0.5f, -0.3f, -0.3f), new Vector3(1.4f, 0.3f, 0.3f)), 
			};
			box.Translate(_position);
			if (HitDetection.CheckRayBoxCollision(box, ray.Position, ray.Direction, out var collision))
			{
				intersection = collision.Position;
				return true;
			}
		}
		intersection = Vector3.Zero;
		return false;
	}

	private Vector3 GetProjectedCursorPosition(Ray playerViewRay, Axis axis)
	{
		Vector3 vector = _startPosition - playerViewRay.Position;
		Vector2 ray1Position;
		Vector2 ray1Direction;
		Vector2 ray2Position;
		Vector2 ray2Direction;
		if (axis == Axis.Y)
		{
			float x = vector.X;
			vector.X = vector.Z;
			vector.Z = 0f - x;
			ray1Position = new Vector2(playerViewRay.Position.X, playerViewRay.Position.Z);
			ray1Direction = new Vector2(playerViewRay.Direction.X, playerViewRay.Direction.Z);
			ray2Position = new Vector2(_startPosition.X, _startPosition.Z);
			ray2Direction = new Vector2(vector.X, vector.Z);
		}
		else if (axis == Axis.PlaneY)
		{
			vector = Vector3.Transform(Vector3.Forward, Quaternion.CreateFromYawPitchRoll(_rotation.Y + (float)System.Math.PI, 0f, 0f));
			ray1Position = new Vector2(playerViewRay.Position.X, playerViewRay.Position.Y);
			ray1Direction = new Vector2(playerViewRay.Direction.X, playerViewRay.Direction.Y);
			ray2Position = new Vector2(_startPosition.X, _startPosition.Y);
			ray2Direction = new Vector2(vector.X, vector.Y);
		}
		else
		{
			vector = ((axis != 0 && axis != Axis.PlaneZ) ? Vector3.Transform(Vector3.Forward, Quaternion.CreateFromYawPitchRoll(_rotation.Y + (float)System.Math.PI, 0f, 0f)) : Vector3.Transform(Vector3.Forward, Quaternion.CreateFromYawPitchRoll(_rotation.Y - (float)System.Math.PI / 2f, 0f, 0f)));
			ray1Position = new Vector2(playerViewRay.Position.X, playerViewRay.Position.Z);
			ray1Direction = new Vector2(playerViewRay.Direction.X, playerViewRay.Direction.Z);
			ray2Position = new Vector2(_startPosition.X, _startPosition.Z);
			ray2Direction = new Vector2(vector.X, vector.Z);
		}
		if (HitDetection.Get2DRayIntersection(ray1Position, ray1Direction, ray2Position, ray2Direction, out var intersection))
		{
			float num = (intersection.X - playerViewRay.Position.X) / playerViewRay.Direction.X;
			switch (axis)
			{
			case Axis.Y:
				return new Vector3(_startPosition.X, num * playerViewRay.Direction.Y + playerViewRay.Position.Y, _startPosition.Z);
			case Axis.PlaneY:
				return new Vector3(intersection.X, _startPosition.Y, num * playerViewRay.Direction.Z + playerViewRay.Position.Z);
			case Axis.X:
			case Axis.Z:
				return new Vector3(intersection.X, _startPosition.Y, intersection.Y);
			case Axis.PlaneX:
			case Axis.PlaneZ:
				return new Vector3(intersection.X, num * playerViewRay.Direction.Y + playerViewRay.Position.Y, intersection.Y);
			}
		}
		return playerViewRay.Position;
	}

	public bool InUse()
	{
		return _selectedAxis != Axis.None;
	}

	private static void BuildModelData()
	{
		PrimitiveModelData primitiveModelData = CylinderModel.BuildModelData(0.125f, 4f, 8);
		primitiveModelData.OffsetVertices(new Vector3(0f, 2f, 0f));
		PrimitiveModelData primitiveModelData2 = ConeModel.BuildModelData(1f, 1f, 8);
		primitiveModelData2.OffsetVertices(new Vector3(0f, 4.25f, 0f));
		_modelData = PrimitiveModelData.CombineData(primitiveModelData2, primitiveModelData);
	}
}
