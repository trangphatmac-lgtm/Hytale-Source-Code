using System;
using System.Collections.Generic;
using System.IO;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Graphics.Gizmos;

internal class RotationGizmo : Disposable
{
	public delegate void OnRotationChange(Vector3 rotation);

	private enum Axis
	{
		X,
		Y,
		Z,
		None
	}

	private const float DefaultSnapAngle = (float)System.Math.PI / 12f;

	private readonly GraphicsDevice _graphics;

	private readonly Font _font;

	private readonly QuadRenderer _quadRenderer;

	private readonly GLTexture _texture;

	private readonly TextRenderer _textRenderer;

	private readonly LineRenderer _lineRenderer;

	private readonly float snapAngle;

	public bool Visible = false;

	private OnRotationChange _onChange;

	private Vector3 _position = Vector3.Zero;

	private Vector3 _rotation = Vector3.Zero;

	private Vector3 _rotationOffset = Vector3.Zero;

	private Vector3 _size = new Vector3(2f, 2f, 2f);

	private Vector3 _lastHitPosition = Vector3.Zero;

	private Vector3 _lastRotation = Vector3.Zero;

	private Vector3 _textPosition = Vector3.Zero;

	private float _fillBlurThreshold;

	private float _axisOffsetAngle;

	private Matrix _tempMatrix;

	private Matrix _drawMatrix;

	private Matrix _textMatrix;

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
		}
	};

	public unsafe RotationGizmo(GraphicsDevice graphics, Font font, OnRotationChange onChange, float snapAngle = (float)System.Math.PI / 12f)
	{
		_graphics = graphics;
		_font = font;
		_onChange = onChange;
		_quadRenderer = new QuadRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram.AttribPosition, _graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
		_textRenderer = new TextRenderer(_graphics, _font, "Rotation");
		_lineRenderer = new LineRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_lineRenderer.UpdateLineData(new Vector3[2]
		{
			new Vector3(0f, 0f, -500f),
			new Vector3(0f, 0f, 500f)
		});
		GLFunctions gL = _graphics.GL;
		_texture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, _texture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
		Image image = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "Tools/RotateGizmo.png")));
		fixed (byte* ptr = image.Pixels)
		{
			gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, image.Width, image.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
		}
		this.snapAngle = snapAngle;
	}

	protected override void DoDispose()
	{
		_graphics.GL.DeleteTexture(_texture);
		_quadRenderer.Dispose();
		_textRenderer.Dispose();
		_lineRenderer.Dispose();
	}

	public void PrepareForDraw(ref Matrix viewProjectionMatrix, ICameraController cameraController)
	{
		if (!Visible)
		{
			throw new Exception("PrepareForDraw called when it was not required. Please check with Visible first before calling this.");
		}
		float scale = 0.2f / (float)_font.BaseSize;
		int spread = _font.Spread;
		float num = 1f / (float)spread;
		Vector3 position = cameraController.Position;
		_textPosition = _position - position;
		float num2 = Vector3.Distance(_position, position);
		_fillBlurThreshold = MathHelper.Clamp(2f * num2 * 0.1f, 1f, spread) * num;
		Matrix.CreateTranslation(0f - _textRenderer.GetHorizontalOffset(TextRenderer.TextAlignment.Center), 0f - _textRenderer.GetVerticalOffset(TextRenderer.TextVerticalAlignment.Middle), 0f, out _tempMatrix);
		Matrix.CreateScale(scale, out _drawMatrix);
		Matrix.Multiply(ref _tempMatrix, ref _drawMatrix, out _drawMatrix);
		Vector3 rotation = cameraController.Rotation;
		Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f, out _tempMatrix);
		Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
		Matrix.AddTranslation(ref _drawMatrix, _position.X, _position.Y, _position.Z);
		Matrix.Multiply(ref _drawMatrix, ref viewProjectionMatrix, out _textMatrix);
	}

	public void Draw(ref Matrix viewProjectionMatrix, ICameraController cameraController, Vector3 renderPositionOffset)
	{
		if (!Visible)
		{
			throw new Exception("PrepareForDraw called when it was not required. Please check with Visible first before calling this.");
		}
		GLFunctions gL = _graphics.GL;
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.BindTexture(GL.TEXTURE_2D, _texture);
		basicProgram.AssertInUse();
		gL.AssertTextureBound(GL.TEXTURE0, _texture);
		Vector3 vector = Vector3.One;
		foreach (KeyValuePair<Axis, float> axisDirection in _axisDirections)
		{
			if (_selectedAxis == Axis.None || _selectedAxis == axisDirection.Key)
			{
				basicProgram.Opacity.SetValue((_highlightedAxis == axisDirection.Key) ? 0.7f : 0.4f);
				_tempMatrix = Matrix.CreateTranslation((0f - _size.X) / 2f, (0f - _size.Y) / 2f, 0f);
				_drawMatrix = Matrix.CreateScale(_size.X, _size.Y, _size.Z);
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				switch (axisDirection.Key)
				{
				case Axis.X:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + _rotationOffset.Y + (float)System.Math.PI / 2f, 0f, 0f);
					vector = _graphics.RedColor;
					break;
				case Axis.Y:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotationOffset.Y, (float)System.Math.PI / 2f, 0f);
					vector = _graphics.GreenColor;
					break;
				case Axis.Z:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + _rotationOffset.Y + (float)System.Math.PI, 0f, 0f);
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
				gL.DepthFunc(GL.ALWAYS);
				_quadRenderer.Draw();
				if (_selectedAxis == axisDirection.Key)
				{
					gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
				}
			}
		}
		gL.BindTexture(GL.TEXTURE_2D, _graphics.WhitePixelTexture.GLTexture);
		foreach (KeyValuePair<Axis, float> axisDirection2 in _axisDirections)
		{
			if (_selectedAxis == Axis.None || _selectedAxis == axisDirection2.Key)
			{
				basicProgram.Opacity.SetValue((_highlightedAxis == axisDirection2.Key) ? 0.7f : 0.4f);
				_tempMatrix = Matrix.CreateTranslation(1f / 64f, 0f, 0.02f);
				_drawMatrix = Matrix.CreateScale(-1f / 32f, 1.1f, 1f);
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				switch (axisDirection2.Key)
				{
				case Axis.X:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + _rotationOffset.Y + (float)System.Math.PI / 2f, 0f, _rotation.X + _rotationOffset.X - (float)System.Math.PI / 2f);
					vector = _graphics.RedColor;
					break;
				case Axis.Y:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + _rotationOffset.Y + (float)System.Math.PI, (float)System.Math.PI / 2f, 0f);
					vector = _graphics.GreenColor;
					break;
				case Axis.Z:
					_tempMatrix = Matrix.CreateFromYawPitchRoll(_rotation.Y + _rotationOffset.Y + (float)System.Math.PI, 0f, 0f - _rotation.Z + _rotationOffset.Z);
					vector = _graphics.BlueColor;
					break;
				}
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				Vector3 position2 = _position + renderPositionOffset;
				_tempMatrix = Matrix.CreateTranslation(position2);
				Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
				Matrix.Multiply(ref _drawMatrix, ref viewProjectionMatrix, out _drawMatrix);
				basicProgram.MVPMatrix.SetValue(ref _drawMatrix);
				basicProgram.Color.SetValue(vector);
				if (_selectedAxis == axisDirection2.Key)
				{
					gL.DepthFunc(GL.ALWAYS);
				}
				_quadRenderer.Draw();
				if (_selectedAxis == axisDirection2.Key)
				{
					_lineRenderer.Draw(ref _drawMatrix, vector, 0.5f);
				}
			}
		}
		PrepareForDraw(ref viewProjectionMatrix, cameraController);
	}

	public void DrawText()
	{
		GLFunctions gL = _graphics.GL;
		TextProgram textProgram = _graphics.GPUProgramStore.TextProgram;
		if (!Visible)
		{
			throw new Exception("Draw called when it was not required. Please check with Visible first before calling this.");
		}
		textProgram.AssertInUse();
		textProgram.Position.SetValue(_textPosition);
		textProgram.FillBlurThreshold.SetValue(_fillBlurThreshold);
		textProgram.MVPMatrix.SetValue(ref _textMatrix);
		gL.DepthFunc(GL.ALWAYS);
		_textRenderer.Draw();
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}

	public void Tick(Ray playerViewRay, float targetBlockHitDistance)
	{
		if (!Visible)
		{
			return;
		}
		float num = float.PositiveInfinity;
		_highlightedAxis = Axis.None;
		foreach (Axis key in _axisDirections.Keys)
		{
			if ((_selectedAxis != Axis.None && key != _selectedAxis) || !CheckRayIntersection(playerViewRay, key, out var intersection, _selectedAxis != Axis.None))
			{
				continue;
			}
			float num2 = Vector3.Distance(intersection, playerViewRay.Position);
			if (num != float.PositiveInfinity && !(num > num2) && key != _selectedAxis)
			{
				continue;
			}
			float num3 = Vector3.Distance(intersection, _position);
			if (key == _selectedAxis || (num3 > 0.77f && num3 < 1f))
			{
				_highlightedAxis = key;
				num = num2;
				_lastHitPosition = intersection;
				if (float.IsNaN(_axisOffsetAngle))
				{
					_axisOffsetAngle = key switch
					{
						Axis.Y => _rotation.Y, 
						Axis.X => _rotation.X, 
						_ => _rotation.Z, 
					} - GetAngleFromAxisHit(_lastHitPosition, key);
				}
			}
		}
		_textRenderer.Text = GetDisplayText();
	}

	public void Show(Vector3 position, Vector3? rotation = null, OnRotationChange onChange = null, Vector3? rotationOffset = null)
	{
		_position = position;
		if (rotation.HasValue)
		{
			_rotation = rotation.Value;
		}
		Visible = true;
		_rotationOffset = (rotationOffset.HasValue ? rotationOffset.Value : Vector3.Zero);
		if (onChange != null)
		{
			_onChange = onChange;
		}
	}

	public void Hide()
	{
		Visible = false;
	}

	public void OnInteract(InteractionType interactionType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)interactionType == 0)
		{
			if (_highlightedAxis != Axis.None && _selectedAxis == Axis.None)
			{
				_selectedAxis = _highlightedAxis;
				_lastRotation = _rotation;
				_axisOffsetAngle = float.NaN;
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
			_rotation = _lastRotation;
			_selectedAxis = Axis.None;
			_onChange(_rotation);
		}
	}

	public void UpdateRotation(bool snapValue)
	{
		switch (_selectedAxis)
		{
		case Axis.None:
			return;
		case Axis.X:
			_rotation.X = GetAngleFromAxisHit(_lastHitPosition, Axis.X);
			if (snapValue)
			{
				_rotation.X = GetSnapValue(_rotation.X, snapAngle);
			}
			break;
		case Axis.Y:
			_rotation.Y = GetAngleFromAxisHit(_lastHitPosition, Axis.Y);
			if (snapValue)
			{
				_rotation.Y = GetSnapValue(_rotation.Y, snapAngle);
			}
			break;
		case Axis.Z:
			_rotation.Z = GetAngleFromAxisHit(_lastHitPosition, Axis.Z);
			if (snapValue)
			{
				_rotation.Z = GetSnapValue(_rotation.Z, snapAngle);
			}
			break;
		}
		_onChange(_rotation);
	}

	private bool CheckRayIntersection(Ray viewRay, Axis axis, out Vector3 intersection, bool force = false)
	{
		Vector3 position = viewRay.Position - _position;
		Vector3 position2 = new Vector3(_size.X / 2f, _size.Y / 2f, 0f);
		float num = ((axis == Axis.Z) ? ((float)System.Math.PI) : ((float)System.Math.PI / 2f));
		if (axis == Axis.Y)
		{
			Matrix.CreateFromYawPitchRoll(0f, 0f - num, 0f, out _tempMatrix);
		}
		else
		{
			Matrix.CreateFromYawPitchRoll(0f - (_rotation.Y + _rotationOffset.Y + num), 0f, 0f, out _tempMatrix);
		}
		Matrix.CreateTranslation(ref position, out _drawMatrix);
		Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
		Vector3 direction = Vector3.Transform(viewRay.Direction, _tempMatrix);
		Matrix.CreateTranslation(ref position2, out _tempMatrix);
		Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
		Ray ray = new Ray(_drawMatrix.Translation + _position, direction);
		float num2 = ((ray.Direction.Z >= 0f) ? ((ray.Position.Z - _position.Z) / (0f - ray.Direction.Z)) : ((_position.Z - ray.Position.Z) / ray.Direction.Z));
		Vector3 vector = ray.Position + ray.Direction * num2;
		if (force || (vector.Y >= _position.Y && vector.Y <= _position.Y + _size.Y && vector.X >= _position.X && vector.X <= _position.X + _size.X))
		{
			intersection = viewRay.Position + viewRay.Direction * num2;
			return true;
		}
		intersection = Vector3.Zero;
		return false;
	}

	private float GetAngleFromAxisHit(Vector3 hitPosition, Axis axis)
	{
		Vector3 position = _lastHitPosition - _position;
		switch (axis)
		{
		case Axis.X:
		{
			Matrix.CreateFromYawPitchRoll(0f - _rotation.Y - _rotationOffset.Y + (float)System.Math.PI / 2f + (float)System.Math.PI, 0f, 0f, out _tempMatrix);
			Vector3 vector = Vector3.Transform(position, _tempMatrix);
			return MathHelper.WrapAngle((float)System.Math.Atan2(vector.Y, vector.X) + (float.IsNaN(_axisOffsetAngle) ? 0f : _axisOffsetAngle));
		}
		case Axis.Y:
		{
			Matrix.CreateFromYawPitchRoll(0f, -(float)System.Math.PI / 2f, 0f, out _tempMatrix);
			Vector3 vector = Vector3.Transform(position, _tempMatrix);
			return MathHelper.WrapAngle((float)System.Math.Atan2(vector.X, vector.Y) - (float)System.Math.PI + (float.IsNaN(_axisOffsetAngle) ? 0f : _axisOffsetAngle));
		}
		case Axis.Z:
		{
			Matrix.CreateFromYawPitchRoll(0f - _rotation.Y - _rotationOffset.Y + (float)System.Math.PI, 0f, 0f, out _tempMatrix);
			Vector3 vector = Vector3.Transform(position, _tempMatrix);
			return 0f - MathHelper.WrapAngle((float)System.Math.Atan2(vector.Y, vector.X) - (float)System.Math.PI / 2f - (float.IsNaN(_axisOffsetAngle) ? 0f : _axisOffsetAngle));
		}
		default:
			return 0f;
		}
	}

	private static float GetSnapValue(float value, float increment)
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

	private string GetDisplayText()
	{
		string result = "";
		if (_highlightedAxis != Axis.None && _selectedAxis != _highlightedAxis)
		{
			float num = 0f;
			switch (_highlightedAxis)
			{
			case Axis.X:
				num = (float)(System.Math.Round(MathHelper.ToDegrees(_rotation.X) * 100f) / 100.0);
				result = $"Pitch: {num}°";
				break;
			case Axis.Y:
				num = (float)(System.Math.Round(MathHelper.ToDegrees(_rotation.Y) * 100f) / 100.0);
				result = $"Yaw: {num}°";
				break;
			case Axis.Z:
				num = (float)(System.Math.Round(MathHelper.ToDegrees(_rotation.Z) * 100f) / 100.0);
				result = $"Roll: {num}°";
				break;
			}
		}
		else if (_highlightedAxis == Axis.None)
		{
			_textRenderer.Text = "";
		}
		else if (_selectedAxis != Axis.None)
		{
			float num2 = 0f;
			switch (_selectedAxis)
			{
			case Axis.X:
				num2 = (float)(System.Math.Round(MathHelper.ToDegrees(_rotation.X) * 100f) / 100.0);
				result = $"{num2}°";
				break;
			case Axis.Y:
				num2 = (float)(System.Math.Round(MathHelper.ToDegrees(_rotation.Y) * 100f) / 100.0);
				result = $"{num2}°";
				break;
			case Axis.Z:
				num2 = (float)(System.Math.Round(MathHelper.ToDegrees(_rotation.Z) * 100f) / 100.0);
				result = $"{num2}°";
				break;
			}
		}
		return result;
	}

	public bool InUse()
	{
		return _selectedAxis != Axis.None;
	}
}
