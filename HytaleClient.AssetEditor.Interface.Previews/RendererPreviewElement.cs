using System;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Previews;

internal abstract class RendererPreviewElement : Element
{
	[UIMarkupProperty]
	public CameraType CameraType;

	[UIMarkupProperty]
	public bool EnableCameraPositionControls = true;

	[UIMarkupProperty]
	public bool EnableCameraOrientationControls = true;

	[UIMarkupProperty]
	public bool EnableCameraScaleControls = true;

	public Vector3 CameraPosition;

	public Vector3 CameraOrientation;

	public float CameraScale;

	public Matrix ViewMatrix;

	public Matrix ViewProjectionMatrix;

	public Action OnLeftClickDown;

	public Action OnLeftClickUp;

	public Action OnRightClickDown;

	public Action OnRightClickUp;

	public Action OnMousePositionChanged;

	public Action OnMouseLeaved;

	public Action OnCameraMoved;

	public bool NeedsRendering;

	public Action RenderScene;

	protected RenderTarget _renderTarget;

	private TextureArea _textureArea;

	private bool _isFrontPressed;

	private bool _isBackPressed;

	private bool _isLeftPressed;

	private bool _isRightPressed;

	private bool _isUpPressed;

	private bool _isDownPressed;

	private Point _previousMousePosition;

	private bool _isMiddleMouseButtonPressed;

	public Matrix _projectionMatrix;

	public RendererPreviewElement(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		_renderTarget = new RenderTarget(1, 1, "RendererPreviewElement");
		_renderTarget.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST);
		_renderTarget.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		_renderTarget.FinalizeSetup();
		_isFrontPressed = false;
		_isBackPressed = false;
		_isLeftPressed = false;
		_isRightPressed = false;
		_isUpPressed = false;
		_isDownPressed = false;
		_isMiddleMouseButtonPressed = false;
		UpdateViewMatrices();
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		_textureArea?.Texture.Dispose();
		_textureArea = null;
		_renderTarget.Dispose();
	}

	protected override void LayoutSelf()
	{
		base.LayoutSelf();
		int width = base.AnchoredRectangle.Width;
		int height = base.AnchoredRectangle.Height;
		_renderTarget.Resize(width, height);
		_textureArea?.Texture.Dispose();
		Texture texture = new Texture(Texture.TextureTypes.Texture2D);
		texture.CreateTexture2D(width, height);
		_textureArea = new TextureArea(texture, 0, 0, width, height, 1);
		float aspectRatio = (float)width / (float)height;
		if (CameraType == CameraType.Camera3D)
		{
			Desktop.Graphics.CreatePerspectiveMatrix((float)System.Math.PI / 4f, aspectRatio, 0.1f, 1000f, out _projectionMatrix);
		}
		else
		{
			_projectionMatrix = Matrix.CreateTranslation(0f, 0f, -500f) * Matrix.CreateOrthographic(1f, (float)height / (float)width, 0.1f, 1000f);
		}
		UpdateViewMatrices();
	}

	public Vector2 GetProjectedMousePosition2D(Point mousePosition)
	{
		if (CameraType != CameraType.Camera2D)
		{
			throw new Exception("GetProjectedMousePosition2D can only be used with a Camera2D type defined.");
		}
		Vector2 result = default(Vector2);
		result.X = ((float)(mousePosition.X - base.AnchoredRectangle.Left - base.AnchoredRectangle.Width / 2) + CameraPosition.X) / CameraScale;
		result.Y = ((float)(mousePosition.Y - base.AnchoredRectangle.Top - base.AnchoredRectangle.Height / 2) - CameraPosition.Y) / CameraScale;
		return result;
	}

	protected internal override void OnKeyDown(SDL_Keycode keyCode, int repeat)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		if ((int)keyCode == 122 || (int)keyCode == 119)
		{
			_isFrontPressed = true;
		}
		else if ((int)keyCode == 115)
		{
			_isBackPressed = true;
		}
		else if ((int)keyCode == 113 || (int)keyCode == 97)
		{
			_isLeftPressed = true;
		}
		else if ((int)keyCode == 100)
		{
			_isRightPressed = true;
		}
		else if ((int)keyCode == 32)
		{
			_isUpPressed = true;
		}
		else if ((int)keyCode == 99)
		{
			_isDownPressed = true;
		}
		else if ((int)keyCode == 1073742049)
		{
			NeedsRendering = true;
		}
	}

	protected internal override void OnKeyUp(SDL_Keycode keyCode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		if ((int)keyCode == 122 || (int)keyCode == 119)
		{
			_isFrontPressed = false;
		}
		else if ((int)keyCode == 115)
		{
			_isBackPressed = false;
		}
		else if ((int)keyCode == 113 || (int)keyCode == 97)
		{
			_isLeftPressed = false;
		}
		else if ((int)keyCode == 100)
		{
			_isRightPressed = false;
		}
		else if ((int)keyCode == 32)
		{
			_isUpPressed = false;
		}
		else if ((int)keyCode == 99)
		{
			_isDownPressed = false;
		}
		else if ((int)keyCode == 1073742049)
		{
			NeedsRendering = true;
		}
	}

	public override Element HitTest(Point position)
	{
		if (!base.Visible || !_rectangleAfterPadding.Contains(position))
		{
			return null;
		}
		return this;
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		Desktop.FocusElement(this, clearMouseCapture: false);
		if ((long)evt.Button == 1)
		{
			if (evt.Clicks == 1)
			{
				OnLeftClickDown?.Invoke();
			}
		}
		else if ((long)evt.Button == 3)
		{
			if (evt.Clicks == 1)
			{
				OnRightClickDown?.Invoke();
			}
		}
		else if ((long)evt.Button == 2)
		{
			_isMiddleMouseButtonPressed = true;
		}
		_previousMousePosition = Desktop.MousePosition;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if ((long)evt.Button == 1)
		{
			OnLeftClickUp?.Invoke();
		}
		else if ((long)evt.Button == 3)
		{
			OnRightClickUp?.Invoke();
		}
		else if ((long)evt.Button == 2)
		{
			_isMiddleMouseButtonPressed = false;
		}
	}

	protected internal override bool OnMouseWheel(Point offset)
	{
		if (Desktop.IsCtrlKeyDown || !EnableCameraScaleControls)
		{
			return false;
		}
		if (CameraType != 0)
		{
			Vector2 projectedMousePosition2D = GetProjectedMousePosition2D(Desktop.MousePosition);
			CameraScale = MathHelper.Clamp(CameraScale * (1f + (float)offset.Y * 0.2f), 0.1f, 10f);
			Vector2 projectedMousePosition2D2 = GetProjectedMousePosition2D(Desktop.MousePosition);
			CameraPosition.X += (projectedMousePosition2D.X - projectedMousePosition2D2.X) * CameraScale;
			CameraPosition.Y -= (projectedMousePosition2D.Y - projectedMousePosition2D2.Y) * CameraScale;
		}
		UpdateViewMatrices();
		return true;
	}

	protected override void OnMouseEnter()
	{
		OnMousePositionChanged?.Invoke();
		NeedsRendering = true;
	}

	protected override void OnMouseLeave()
	{
		OnMouseLeaved?.Invoke();
		NeedsRendering = true;
	}

	protected override void OnMouseMove()
	{
		Point point = Desktop.MousePosition - _previousMousePosition;
		if (_isMiddleMouseButtonPressed)
		{
			if (CameraType == CameraType.Camera3D)
			{
				if (EnableCameraOrientationControls && point != Point.Zero)
				{
					CameraOrientation.Yaw -= (float)point.X * 0.01f;
					CameraOrientation.Pitch -= (float)point.Y * 0.01f;
					UpdateViewMatrices();
				}
			}
			else if (EnableCameraPositionControls)
			{
				Vector2 projectedMousePosition2D = GetProjectedMousePosition2D(_previousMousePosition);
				Vector2 projectedMousePosition2D2 = GetProjectedMousePosition2D(Desktop.MousePosition);
				CameraPosition.X += (projectedMousePosition2D.X - projectedMousePosition2D2.X) * CameraScale;
				CameraPosition.Y -= (projectedMousePosition2D.Y - projectedMousePosition2D2.Y) * CameraScale;
				UpdateViewMatrices();
			}
			_previousMousePosition = Desktop.MousePosition;
		}
		if (point != Point.Zero)
		{
			OnMousePositionChanged?.Invoke();
		}
	}

	protected override void PrepareForDrawSelf()
	{
		Desktop.Batcher2D.RequestDrawTexture(_textureArea.Texture, _textureArea.Rectangle, _anchoredRectangle, UInt32Color.White);
	}

	private void UpdateCamera3D(float deltaTime)
	{
		Vector3 vector = default(Vector3);
		if (_isFrontPressed)
		{
			vector.Z += 1f;
		}
		if (_isBackPressed)
		{
			vector.Z -= 1f;
		}
		if (_isLeftPressed)
		{
			vector.X += 1f;
		}
		if (_isRightPressed)
		{
			vector.X -= 1f;
		}
		if (_isUpPressed)
		{
			vector.Y += 1f;
		}
		if (_isDownPressed)
		{
			vector.Y -= 1f;
		}
		if (vector != Vector3.Zero)
		{
			Quaternion rotation = Quaternion.CreateFromYawPitchRoll((float)System.Math.PI + CameraOrientation.Yaw, 0f - CameraOrientation.Pitch, CameraOrientation.Roll);
			Vector3 vector2 = Vector3.Transform(vector * deltaTime * 5f, rotation);
			CameraPosition += vector2;
			UpdateViewMatrices();
		}
	}

	private void UpdateCamera2D(float deltaTime)
	{
		if (!_isMiddleMouseButtonPressed)
		{
			Vector3 vector = default(Vector3);
			if (_isFrontPressed)
			{
				vector.Y += 1f;
			}
			if (_isBackPressed)
			{
				vector.Y -= 1f;
			}
			if (_isLeftPressed)
			{
				vector.X -= 1f;
			}
			if (_isRightPressed)
			{
				vector.X += 1f;
			}
			if (vector != Vector3.Zero)
			{
				CameraPosition += vector * 200f * deltaTime;
				UpdateViewMatrices();
			}
		}
	}

	public void UpdateViewMatrices()
	{
		ViewMatrix = Matrix.CreateFromYawPitchRoll(0f - CameraOrientation.Yaw, 0f - CameraOrientation.Pitch, 0f - CameraOrientation.Roll) * Matrix.CreateTranslation(-CameraPosition) * Matrix.CreateScale(CameraScale);
		ViewProjectionMatrix = ViewMatrix * _projectionMatrix;
		NeedsRendering = true;
		OnCameraMoved?.Invoke();
	}

	protected virtual void Animate(float deltaTime)
	{
		if (EnableCameraPositionControls)
		{
			if (CameraType == CameraType.Camera3D)
			{
				UpdateCamera3D(deltaTime);
			}
			else
			{
				UpdateCamera2D(deltaTime);
			}
		}
		if (NeedsRendering)
		{
			NeedsRendering = false;
			_textureArea.Texture.UpdateTexture2D(RenderIntoRgbaByteArray());
		}
	}

	protected byte[] RenderIntoRgbaByteArray()
	{
		GLFunctions gL = Desktop.Graphics.GL;
		_renderTarget.Bind(clear: false, setupViewport: true);
		UInt32Color uInt32Color = Background?.Color ?? UInt32Color.Transparent;
		float red = (float)(int)(byte)(uInt32Color.ABGR & 0xFF) / 255f;
		float green = (float)((byte)(uInt32Color.ABGR >> 8) & 0xFF) / 255f;
		float blue = (float)((byte)(uInt32Color.ABGR >> 16) & 0xFF) / 255f;
		float alpha = (float)((byte)(uInt32Color.ABGR >> 24) & 0xFF) / 255f;
		gL.ClearColor(red, green, blue, alpha);
		gL.Clear((GL)17664u);
		RenderScene?.Invoke();
		_renderTarget.Unbind();
		return _renderTarget.ReadPixels(1, GL.RGBA);
	}
}
