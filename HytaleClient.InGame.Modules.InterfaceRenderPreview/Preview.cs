using System;
using HytaleClient.Core;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Map;
using HytaleClient.Math;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.InGame.Modules.InterfaceRenderPreview;

internal abstract class Preview : Disposable
{
	private float _scale;

	private Vector3 _translate;

	private Vector3 _rotation;

	private float _minZoom = -1f;

	private float _maxZoom = -1f;

	private float _zoom = 1f;

	private bool _isOrtho;

	private bool _isRotatable = true;

	private bool _isMouseOver;

	private float _lerpModelAngleY;

	private float _dragStartModelAngleY;

	private bool _isMouseDragging;

	private int _dragStartMouseX;

	protected GameInstance _gameInstance;

	public Rectangle Viewport { get; private set; }

	public Matrix ProjectionMatrix { get; private set; }

	public abstract ModelRenderer ModelRenderer { get; }

	public abstract AnimatedBlockRenderer AnimatedBlockRenderer { get; }

	protected Preview(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public void SetBaseParams(InterfaceRenderPreviewModule.PreviewParams parameters)
	{
		_isOrtho = parameters.Ortho;
		_translate = new Vector3(parameters.Translation[0], parameters.Translation[1], (parameters.Translation.Length >= 2) ? parameters.Translation[2] : 0f);
		_rotation = new Vector3(parameters.Rotation[0], parameters.Rotation[1], parameters.Rotation[2]);
		_scale = parameters.Scale;
		_isRotatable = parameters.Rotatable;
		Viewport = new Rectangle(parameters.Viewport.X, parameters.Viewport.Y, parameters.Viewport.Width, parameters.Viewport.Height);
		_lerpModelAngleY = _rotation.Y;
		if (parameters.ZoomRange != null)
		{
			_minZoom = parameters.ZoomRange[0];
			_maxZoom = parameters.ZoomRange[1];
		}
		else
		{
			_minZoom = (_maxZoom = -1f);
		}
		float aspectRatio = (float)Viewport.Width / (float)Viewport.Height;
		if (_isOrtho)
		{
			ProjectionMatrix = Matrix.CreateTranslation(0f, 0f, -500f) * Matrix.CreateOrthographic(1f, (float)Viewport.Height / (float)Viewport.Width, 0.1f, 1000f);
			return;
		}
		_translate.Z -= 1.5f;
		_gameInstance.Engine.Graphics.CreatePerspectiveMatrix((float)System.Math.PI / 4f, aspectRatio, 0.1f, 1000f, out var result);
		ProjectionMatrix = result;
	}

	public void OnUserInput(SDL_Event evt)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected I4, but got Unknown
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		bool flag = _minZoom > 0f && _maxZoom > 0f;
		SDL_EventType type = evt.type;
		SDL_EventType val = type;
		switch (val - 1024)
		{
		case 1:
			if ((_isRotatable || flag) && evt.button.button == 1 && IsInsideViewport(evt.button.x, evt.button.y))
			{
				_isMouseDragging = true;
				_dragStartMouseX = evt.button.x;
				_dragStartModelAngleY = _rotation.Y;
			}
			break;
		case 2:
			if ((_isRotatable || flag) && evt.button.button == 1 && _isMouseDragging)
			{
				_isMouseDragging = false;
			}
			break;
		case 0:
			if (_isRotatable || flag)
			{
				_isMouseOver = IsInsideViewport(evt.motion.x, evt.motion.y);
				if (_isMouseDragging)
				{
					_rotation.Y = _dragStartModelAngleY + (float)(evt.motion.x - _dragStartMouseX);
				}
			}
			break;
		case 3:
			if (_isMouseOver && flag && evt.wheel.y != 0)
			{
				_zoom = MathHelper.Clamp(_zoom + (float)evt.wheel.y / 120f, _minZoom, _maxZoom);
			}
			break;
		}
	}

	private bool IsInsideViewport(int x, int y)
	{
		Engine engine = _gameInstance.Engine;
		return Viewport.Contains(engine.Window.TransformSDLToViewportCoords(x, y));
	}

	public void Update(float deltaTime)
	{
		_lerpModelAngleY = MathHelper.Lerp(_lerpModelAngleY, _rotation.Y, MathHelper.Min(1f, 10f * deltaTime));
	}

	public virtual void PrepareModelMatrix(ref Matrix modelMatrix)
	{
		Matrix matrix = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(_lerpModelAngleY), MathHelper.ToRadians(_rotation.X), MathHelper.ToRadians(_rotation.Z));
		Matrix matrix2 = Matrix.CreateScale(_scale * _zoom / 32f);
		Matrix matrix3 = Matrix.CreateTranslation(_translate);
		modelMatrix = matrix * matrix3 * matrix2;
	}

	public virtual void PrepareForDraw(ref int blockyModelDrawTaskCount, ref int animatedBlockDrawTaskCount, ref InterfaceRenderPreviewModule.BlockyModelDrawTask[] blockyModelDrawTasks, ref InterfaceRenderPreviewModule.AnimatedBlockDrawTask[] animatedBlockDrawTasks)
	{
		if (AnimatedBlockRenderer != null)
		{
			ArrayUtils.GrowArrayIfNecessary(ref animatedBlockDrawTasks, animatedBlockDrawTaskCount, 10);
			int num = animatedBlockDrawTaskCount;
			animatedBlockDrawTasks[num].Viewport = Viewport;
			animatedBlockDrawTasks[num].ProjectionMatrix = ProjectionMatrix;
			PrepareModelMatrix(ref animatedBlockDrawTasks[num].ModelMatrix);
			animatedBlockDrawTasks[num].AnimationData = AnimatedBlockRenderer.NodeBuffer;
			animatedBlockDrawTasks[num].AnimationDataOffset = AnimatedBlockRenderer.NodeBufferOffset;
			animatedBlockDrawTasks[num].AnimationDataSize = (ushort)(AnimatedBlockRenderer.NodeCount * 64);
			animatedBlockDrawTasks[num].VertexArray = AnimatedBlockRenderer.VertexArray;
			animatedBlockDrawTasks[num].DataCount = AnimatedBlockRenderer.IndicesCount;
			animatedBlockDrawTaskCount++;
		}
		else if (ModelRenderer != null)
		{
			ArrayUtils.GrowArrayIfNecessary(ref blockyModelDrawTasks, blockyModelDrawTaskCount, 10);
			int num2 = blockyModelDrawTaskCount;
			blockyModelDrawTasks[num2].Viewport = Viewport;
			blockyModelDrawTasks[num2].ProjectionMatrix = ProjectionMatrix;
			PrepareModelMatrix(ref blockyModelDrawTasks[num2].ModelMatrix);
			blockyModelDrawTasks[num2].AnimationData = ModelRenderer.NodeBuffer;
			blockyModelDrawTasks[num2].AnimationDataOffset = ModelRenderer.NodeBufferOffset;
			blockyModelDrawTasks[num2].AnimationDataSize = (ushort)(ModelRenderer.NodeCount * 64);
			blockyModelDrawTasks[num2].VertexArray = ModelRenderer.VertexArray;
			blockyModelDrawTasks[num2].DataCount = ModelRenderer.IndicesCount;
			blockyModelDrawTaskCount++;
		}
	}

	protected override void DoDispose()
	{
		AnimatedBlockRenderer?.Dispose();
		ModelRenderer?.Dispose();
	}

	public abstract void UpdateRenderer();
}
