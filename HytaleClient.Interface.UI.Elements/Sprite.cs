#define DEBUG
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class Sprite : Element
{
	[UIMarkupData]
	public class SpriteFrame
	{
		public int Width;

		public int Height;

		public int PerRow;

		public int Count;
	}

	[UIMarkupProperty]
	public UIPath TexturePath;

	private TextureArea _texture;

	private SpriteFrame _frame;

	[UIMarkupProperty]
	public int FramesPerSecond = 20;

	[UIMarkupProperty]
	public float Angle;

	[UIMarkupProperty]
	public int RepeatCount;

	private bool _isPlaying;

	private bool _autoPlay = true;

	private float _frameProgress;

	private Rectangle _sourceRectangle;

	private bool _isAnimationCallbackRegistered;

	[UIMarkupProperty]
	public SpriteFrame Frame
	{
		set
		{
			_frame = value;
			_frameProgress = 0f;
			if (base.IsMounted)
			{
				EnsureAutoPlay();
				UpdateAnimationCallback();
			}
		}
	}

	[UIMarkupProperty]
	public bool AutoPlay
	{
		get
		{
			return _autoPlay;
		}
		set
		{
			_autoPlay = value;
			if (base.IsMounted)
			{
				EnsureAutoPlay();
				UpdateAnimationCallback();
			}
		}
	}

	public Sprite(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		EnsureAutoPlay();
		UpdateAnimationCallback();
	}

	protected override void OnUnmounted()
	{
		if (_isAnimationCallbackRegistered)
		{
			_isAnimationCallbackRegistered = false;
			Desktop.UnregisterAnimationCallback(Animate);
		}
	}

	public void Play()
	{
		if (!_isPlaying)
		{
			_isPlaying = true;
			if (base.IsMounted)
			{
				UpdateAnimationCallback();
			}
		}
	}

	public void Pause()
	{
		if (_isPlaying)
		{
			_isPlaying = false;
			if (base.IsMounted)
			{
				UpdateAnimationCallback();
			}
		}
	}

	public void Stop()
	{
		if (_isPlaying)
		{
			_isPlaying = false;
			_frameProgress = 0f;
			if (base.IsMounted)
			{
				UpdateAnimationCallback();
			}
		}
	}

	public void Reset()
	{
		_frameProgress = 0f;
	}

	private void EnsureAutoPlay()
	{
		if (_frame.Count > 1 && AutoPlay && !_isPlaying)
		{
			_isPlaying = true;
			_frameProgress = 0f;
		}
	}

	private void UpdateAnimationCallback()
	{
		Debug.Assert(base.IsMounted);
		bool flag = _frame.Count > 1 && _isPlaying;
		if (_isAnimationCallbackRegistered && !flag)
		{
			_isAnimationCallbackRegistered = false;
			Desktop.UnregisterAnimationCallback(Animate);
		}
		else if (!_isAnimationCallbackRegistered && flag)
		{
			_isAnimationCallbackRegistered = true;
			Desktop.RegisterAnimationCallback(Animate);
		}
		if (_texture != null)
		{
			SetupSourceRectangle();
		}
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_texture = Desktop.Provider.MakeTextureArea(TexturePath.Value);
		SetupSourceRectangle();
	}

	private void Animate(float deltaTime)
	{
		if (_isPlaying)
		{
			_frameProgress += deltaTime * (float)FramesPerSecond;
			SetupSourceRectangle();
			if (RepeatCount > 0 && _frameProgress >= (float)(RepeatCount * _frame.Count))
			{
				_isPlaying = false;
				UpdateAnimationCallback();
			}
		}
	}

	private void SetupSourceRectangle()
	{
		int num = (int)_frameProgress % _frame.Count;
		int num2 = num / _frame.PerRow;
		int num3 = num - num2 * _frame.PerRow;
		_sourceRectangle = new Rectangle(num3 * _frame.Width * _texture.Scale + _texture.Rectangle.X, num2 * _frame.Height * _texture.Scale + _texture.Rectangle.Y, _frame.Width * _texture.Scale, _frame.Height * _texture.Scale);
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		Desktop.Batcher2D.SetTransformationMatrix(new Vector3(_rectangleAfterPadding.Center.X, _rectangleAfterPadding.Center.Y, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(Angle)), 1f);
		Rectangle destRect = new Rectangle(-_rectangleAfterPadding.Width / 2, -_rectangleAfterPadding.Height / 2, _rectangleAfterPadding.Width, _rectangleAfterPadding.Height);
		Desktop.Batcher2D.RequestDrawPatch(_texture.Texture, _sourceRectangle, 0, 0, _texture.Scale, destRect, Desktop.Scale, UInt32Color.White);
		Desktop.Batcher2D.SetTransformationMatrix(Matrix.Identity);
	}
}
