using System;
using HytaleClient.Core;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Trails;

internal class Trail : Disposable
{
	private const int DefaultLifeSpan = 10;

	private const int MaxNewSegments = 5;

	private const float SmoothStep = 0.25f;

	private const int MaxLifeSpan = 200;

	private const int MaxSmoothLifeSpan = 40;

	private GraphicsDevice _graphics;

	private TrailFXSystem _trailFXSystem;

	private readonly TrailSettings _trailSettings;

	public readonly int Id;

	public Vector3 Position;

	public Quaternion Rotation = Quaternion.Identity;

	private Vector3 _lastPosition;

	private Vector4 _intersectionHighlight;

	private int _segmentBufferStartIndex;

	private int _segmentCount;

	private readonly SegmentBuffers _segmentBuffer;

	private ushort _drawId;

	private int _particleVertexDataStartIndex;

	private int _lastSegment;

	private float _trailTailLength = 0f;

	private Vector4 _staticLightColorAndInfluence;

	private float _startWidth = 1f;

	private float _endWidth = 1f;

	public bool IsExpired = false;

	private bool _hasAnimation = false;

	private Vector2 _textureAltasInverseSize;

	private Point _frameSize;

	private int _tilesPerRow;

	private int _targetTextureIndex = 0;

	private int _frameTimer = 0;

	private Vector4 _textureCoords;

	private Vector3[] _lastRealPositions = new Vector3[2];

	public bool Visible;

	public bool IsFirstPerson = false;

	private bool _wasFirstPerson = false;

	private int _lifeSpan;

	public int ParticleVertexDataStartIndex => _particleVertexDataStartIndex;

	public int ParticleCount => _segmentCount - 1;

	public bool IsDistortion => RenderMode == FXSystem.RenderMode.Distortion;

	public float LightInfluence => _trailSettings.LightInfluence;

	public bool IsSpawned { get; private set; } = false;


	public string SettingsId => _trailSettings.Id;

	public FXSystem.RenderMode RenderMode => _trailSettings.RenderMode;

	public bool NeedsUpdating()
	{
		return IsSpawned && !IsExpired;
	}

	public Trail(GraphicsDevice graphics, TrailFXSystem trailFXSystem, TrailSettings trailSettings, Vector2 textureAltasInverseSize, int id)
	{
		_graphics = graphics;
		_trailFXSystem = trailFXSystem;
		_segmentBuffer = _trailFXSystem.SegmentBuffer;
		_trailSettings = trailSettings;
		Id = id;
		_lifeSpan = ((_trailSettings.LifeSpan != 0) ? _trailSettings.LifeSpan : 10);
		if (_trailSettings.Smooth)
		{
			_lifeSpan = (int)MathHelper.Min(_lifeSpan, 40f);
			_segmentCount = _lifeSpan * 5;
		}
		else
		{
			_lifeSpan = (int)MathHelper.Min(_lifeSpan, 200f);
			_segmentCount = _lifeSpan;
		}
		_segmentCount++;
		_startWidth = _trailSettings.Start.Width;
		_endWidth = _trailSettings.End.Width;
		_intersectionHighlight = new Vector4(_trailSettings.IntersectionHighlightColor.X, _trailSettings.IntersectionHighlightColor.Y, _trailSettings.IntersectionHighlightColor.Z, _trailSettings.IntersectionHighlightThreshold);
		UpdateTexture(textureAltasInverseSize);
	}

	public bool Initialize()
	{
		_segmentBufferStartIndex = _trailFXSystem.RequestSegmentBufferStorage(_segmentCount);
		bool flag = _segmentBufferStartIndex >= 0 && _segmentBufferStartIndex < _trailFXSystem.SegmentBufferStorageMaxCount;
		if (flag)
		{
			_segmentBuffer.Life[_segmentBufferStartIndex] = _lifeSpan;
		}
		return flag;
	}

	private void UpdateTexture(Vector2 textureAltasInverseSize)
	{
		_textureAltasInverseSize = textureAltasInverseSize;
		Rectangle imageLocation = _trailSettings.ImageLocation;
		Point frameSize = _trailSettings.FrameSize;
		_frameSize = ((frameSize.X == 0 || frameSize.Y == 0) ? new Point(imageLocation.Width, imageLocation.Height) : new Point(frameSize.X, frameSize.Y));
		_tilesPerRow = imageLocation.Width / _frameSize.X;
		Point frameRange = _trailSettings.FrameRange;
		_hasAnimation = frameRange.X != frameRange.Y;
		_targetTextureIndex = frameRange.X;
		_textureCoords = new Vector4((float)(imageLocation.X + _frameSize.X * (_targetTextureIndex % _tilesPerRow)) * _textureAltasInverseSize.X, (float)(imageLocation.Y + _frameSize.Y * (_targetTextureIndex / _tilesPerRow)) * _textureAltasInverseSize.Y, (float)(imageLocation.X + (_frameSize.X * (_targetTextureIndex % _tilesPerRow) + _frameSize.X)) * _textureAltasInverseSize.X, (float)(imageLocation.Y + (_frameSize.Y * (_targetTextureIndex / _tilesPerRow) + _frameSize.Y)) * _textureAltasInverseSize.Y);
	}

	public void SetScale(float scale)
	{
		_startWidth = _trailSettings.Start.Width * scale;
		_endWidth = _trailSettings.End.Width * scale;
	}

	protected override void DoDispose()
	{
		Release();
		_trailFXSystem = null;
		_graphics = null;
	}

	public void SetSpawn()
	{
		for (int i = 0; i < _segmentCount; i++)
		{
			_segmentBuffer.TrailPosition[_segmentBufferStartIndex + i] = Position;
			_segmentBuffer.Rotation[_segmentBufferStartIndex + i] = Rotation * Quaternion.CreateFromYawPitchRoll(0f, 0f, MathHelper.ToRadians(_trailSettings.Roll));
		}
		IsSpawned = true;
	}

	public void UpdateLight(Vector4 staticLightColor)
	{
		staticLightColor.W = _trailSettings.LightInfluence;
		_staticLightColorAndInfluence = staticLightColor;
	}

	public void LightUpdate()
	{
		if (_wasFirstPerson != IsFirstPerson)
		{
			SetSpawn();
		}
		ref Vector3 reference = ref _segmentBuffer.TrailPosition[_segmentBufferStartIndex];
		reference = Position;
		_segmentBuffer.Rotation[_segmentBufferStartIndex] = Rotation * Quaternion.CreateFromYawPitchRoll(0f, 0f, MathHelper.ToRadians(_trailSettings.Roll));
		_segmentBuffer.Length[_segmentBufferStartIndex + 1] = Vector3.Distance(reference, _segmentBuffer.TrailPosition[_segmentBufferStartIndex + 1]);
		_wasFirstPerson = IsFirstPerson;
		_lastPosition = Position;
	}

	public void Update()
	{
		_trailTailLength = 0f;
		_lastSegment = _segmentCount - 1;
		ref Vector3 reference = ref _segmentBuffer.TrailPosition[_segmentBufferStartIndex];
		ref Quaternion reference2 = ref _segmentBuffer.Rotation[_segmentBufferStartIndex];
		ref Vector3 reference3 = ref _segmentBuffer.TrailPosition[_segmentBufferStartIndex + 1];
		ref float reference4 = ref _segmentBuffer.Length[_segmentBufferStartIndex + 1];
		if (_lastPosition != Position)
		{
			int num = ((!_trailSettings.Smooth || reference4 <= 0.25f) ? 1 : System.Math.Min((int)(reference4 / 0.25f), 5));
			for (int num2 = _segmentCount - 1; num2 > num; num2--)
			{
				ref int reference5 = ref _segmentBuffer.Life[_segmentBufferStartIndex + num2];
				ref float reference6 = ref _segmentBuffer.Length[_segmentBufferStartIndex + num2];
				reference5 = _segmentBuffer.Life[_segmentBufferStartIndex + num2 - num];
				if (reference5 > 0)
				{
					reference5--;
				}
				if (reference5 == 0)
				{
					_lastSegment = num2 - 1;
				}
				else
				{
					_segmentBuffer.TrailPosition[_segmentBufferStartIndex + num2] = _segmentBuffer.TrailPosition[_segmentBufferStartIndex + num2 - num];
					reference6 = _segmentBuffer.Length[_segmentBufferStartIndex + num2 - num];
					_segmentBuffer.Rotation[_segmentBufferStartIndex + num2] = _segmentBuffer.Rotation[_segmentBufferStartIndex + num2 - num];
					if (num == 1 || num2 != num + 1)
					{
						_trailTailLength += reference6;
					}
				}
			}
			ref int reference7 = ref _segmentBuffer.Life[_segmentBufferStartIndex];
			if (num > 1)
			{
				ref Vector3 reference8 = ref _segmentBuffer.TrailPosition[_segmentBufferStartIndex + num + 1];
				ref Quaternion reference9 = ref _segmentBuffer.Rotation[_segmentBufferStartIndex + num + 1];
				Vector3 vector = _lastRealPositions[1] - _lastRealPositions[0];
				Vector3 vector2 = ((vector != Vector3.Zero) ? Vector3.Normalize(vector) : Vector3.Zero);
				Vector3 vector3 = ((vector != Vector3.Zero) ? Vector3.Normalize(-vector) : Vector3.Zero);
				Vector3 normal = ((_segmentBuffer.TrailPosition[_segmentBufferStartIndex] != _segmentBuffer.TrailPosition[_segmentBufferStartIndex + 1]) ? Vector3.Normalize(_segmentBuffer.TrailPosition[_segmentBufferStartIndex] - _segmentBuffer.TrailPosition[_segmentBufferStartIndex + 1]) : Vector3.Zero);
				Vector3 tangent = vector2 * reference4 * 0.75f;
				Vector3 tangent2 = Vector3.Reflect(vector3 * reference4 * 0.75f, normal);
				for (int num3 = num; num3 > 1; num3--)
				{
					float amount = (float)(num3 - 1) / (float)num;
					Vector3 vector4 = Vector3.Hermite(reference, tangent2, reference8, tangent, amount);
					Quaternion quaternion = Quaternion.Slerp(reference2, reference9, amount);
					_segmentBuffer.Life[_segmentBufferStartIndex + num3] = reference7;
					_segmentBuffer.TrailPosition[_segmentBufferStartIndex + num3] = vector4;
					_segmentBuffer.Rotation[_segmentBufferStartIndex + num3] = quaternion;
					_segmentBuffer.Length[_segmentBufferStartIndex + num3 + 1] = Vector3.Distance(_segmentBuffer.TrailPosition[_segmentBufferStartIndex + num3], _segmentBuffer.TrailPosition[_segmentBufferStartIndex + num3 + 1]);
					_trailTailLength += _segmentBuffer.Length[_segmentBufferStartIndex + num3 + 1];
				}
				_segmentBuffer.Length[_segmentBufferStartIndex + 2] = Vector3.Distance(_segmentBuffer.TrailPosition[_segmentBufferStartIndex + 2], reference);
				_trailTailLength += _segmentBuffer.Length[_segmentBufferStartIndex + 2];
			}
			reference4 = 0f;
			_segmentBuffer.Life[_segmentBufferStartIndex + 1] = reference7;
			reference3 = reference;
			_segmentBuffer.Rotation[_segmentBufferStartIndex + 1] = reference2;
			_lastRealPositions[1] = _lastRealPositions[0];
			_lastRealPositions[0] = reference3;
		}
		else
		{
			for (int num4 = _segmentCount - 1; num4 > 0; num4--)
			{
				ref int reference10 = ref _segmentBuffer.Life[_segmentBufferStartIndex + num4];
				if (reference10 > 0)
				{
					reference10--;
				}
				if (reference10 == 0)
				{
					_lastSegment = num4 - 1;
				}
				else if (num4 > 1)
				{
					_trailTailLength += _segmentBuffer.Length[_segmentBufferStartIndex + num4];
				}
			}
		}
		if (_hasAnimation)
		{
			_frameTimer++;
			if (_frameTimer > _trailSettings.FrameLifeSpan)
			{
				_targetTextureIndex++;
				if (_targetTextureIndex > _trailSettings.FrameRange.Y)
				{
					_targetTextureIndex = _trailSettings.FrameRange.X;
				}
				ref Rectangle imageLocation = ref _trailSettings.ImageLocation;
				_textureCoords.X = (float)(imageLocation.X + _frameSize.X * (_targetTextureIndex % _tilesPerRow)) * _textureAltasInverseSize.X;
				_textureCoords.Y = (float)(imageLocation.Y + _frameSize.Y * (_targetTextureIndex / _tilesPerRow)) * _textureAltasInverseSize.Y;
				_textureCoords.Z = (float)(imageLocation.X + (_frameSize.X * (_targetTextureIndex % _tilesPerRow) + _frameSize.X)) * _textureAltasInverseSize.X;
				_textureCoords.W = (float)(imageLocation.Y + (_frameSize.Y * (_targetTextureIndex / _tilesPerRow) + _frameSize.Y)) * _textureAltasInverseSize.Y;
				_frameTimer = 0;
			}
		}
		if (_wasFirstPerson != IsFirstPerson)
		{
			SetSpawn();
		}
		reference = Position;
		reference2 = Rotation * Quaternion.CreateFromYawPitchRoll(0f, 0f, MathHelper.ToRadians(_trailSettings.Roll));
		reference4 = Vector3.Distance(reference, reference3);
		_wasFirstPerson = IsFirstPerson;
		_lastPosition = Position;
	}

	public void ReserveVertexDataStorage(ref FXVertexBuffer vertexBuffer, ushort drawId)
	{
		_drawId = drawId;
		_particleVertexDataStartIndex = vertexBuffer.ReserveVertexDataStorage(ParticleCount);
	}

	public unsafe void PrepareForDraw(Vector3 cameraPosition, ref FXVertexBuffer vertexBuffer, IntPtr gpuDrawDataPtr)
	{
		float num = 0f;
		float num2 = 1f / (_segmentBuffer.Length[_segmentBufferStartIndex + 1] + _trailTailLength);
		uint num3 = (uint)((int)_trailSettings.RenderMode << FXVertex.ConfigBitShiftBlendMode);
		num3 |= (uint)((IsFirstPerson ? 1 : 0) << FXVertex.ConfigBitShiftIsFirstPerson);
		num3 |= (uint)(_drawId << FXVertex.ConfigBitShiftDrawId);
		vertexBuffer.SetVertexDataConfig(_particleVertexDataStartIndex, num3);
		vertexBuffer.SetTrailFirstSegmentVertexPosition(_particleVertexDataStartIndex, Vector3.Zero, _segmentBuffer.Rotation[_segmentBufferStartIndex], MathHelper.Lerp(_startWidth, _endWidth, num * num2));
		vertexBuffer.SetTrailFirstSegmentVertexLength(_particleVertexDataStartIndex, 0f);
		for (int i = 1; i < _segmentCount - 1; i++)
		{
			int num4 = ((i <= _lastSegment) ? i : _lastSegment);
			if (i <= _lastSegment)
			{
				num += _segmentBuffer.Length[_segmentBufferStartIndex + num4];
			}
			vertexBuffer.SetTrailSegmentVertexPosition(_particleVertexDataStartIndex + i, _segmentBuffer.TrailPosition[_segmentBufferStartIndex + num4] - Position, _segmentBuffer.Rotation[_segmentBufferStartIndex + num4], MathHelper.Lerp(_startWidth, _endWidth, num * num2));
			vertexBuffer.SetTrailSegmentVertexLength(_particleVertexDataStartIndex + i, num * num2);
			vertexBuffer.SetVertexDataConfig(_particleVertexDataStartIndex + i, num3);
		}
		vertexBuffer.SetTrailLastSegmentVertexPosition(_particleVertexDataStartIndex + _segmentCount, _segmentBuffer.TrailPosition[_segmentBufferStartIndex + _lastSegment] - Position, _segmentBuffer.Rotation[_segmentBufferStartIndex + _lastSegment], MathHelper.Lerp(_startWidth, _endWidth, num * num2));
		vertexBuffer.SetTrailLastSegmentVertexLength(_particleVertexDataStartIndex + _segmentCount, 1f);
		Matrix result;
		if (IsFirstPerson)
		{
			Matrix.CreateTranslation(ref Position, out result);
		}
		else
		{
			Vector3 position = Position - cameraPosition;
			Matrix.CreateTranslation(ref position, out result);
		}
		IntPtr pointer = IntPtr.Add(gpuDrawDataPtr, _drawId * FXRenderer.DrawDataSize);
		Matrix* ptr = (Matrix*)pointer.ToPointer();
		*ptr = result;
		Vector4* ptr2 = (Vector4*)IntPtr.Add(pointer, sizeof(Matrix)).ToPointer();
		*ptr2 = _staticLightColorAndInfluence;
		ptr2[1] = _trailSettings.Start.Color;
		ptr2[2] = _textureCoords;
		ptr2[3] = new Vector4(0f, 0f, 0f, 1f);
		ptr2[4] = _trailSettings.End.Color;
		ptr2[5] = _intersectionHighlight;
		ptr2[6] = Vector4.Zero;
		ptr2[7] = Vector4.Zero;
		_wasFirstPerson = IsFirstPerson;
	}

	private void Release()
	{
		if (_segmentCount > 0)
		{
			_trailFXSystem.ReleaseSegmentBufferStorage(_segmentBufferStartIndex, _segmentCount);
		}
	}
}
