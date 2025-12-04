using System;
using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Data;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Map;
using HytaleClient.Graphics.Programs;
using HytaleClient.Interface.AssetEditor.Utils;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using HytaleClient.Protocol;
using NLog;

namespace HytaleClient.AssetEditor.Interface.Previews;

internal abstract class AssetPreview : RendererPreviewElement
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	protected readonly AssetEditorOverlay _assetEditorOverlay;

	private AnimatedBlockRenderer _animatedBlockRenderer;

	private ModelRenderer _modelRenderer;

	private float _lerpModelAngleY;

	private float _dragStartModelAngleY;

	private float _targetModelAngleY;

	private bool _isMouseDragging;

	private int _dragStartMouseX;

	private bool _updateUserRotation;

	protected Texture _textureAtlas;

	protected Texture _fallbackTexture;

	protected Texture _gradientTexture;

	protected BlockyModel _model;

	protected ChunkGeometryData _blockVertexData;

	protected AssetEditorPreviewCameraSettings _cameraSettings;

	protected Dictionary<string, Point> _textureLocations = new Dictionary<string, Point>();

	protected Dictionary<string, Point> _textureSizes = new Dictionary<string, Point>();

	private readonly List<string> _assetPaths = new List<string>();

	protected bool _needsUpdateAfterRendererDisposal;

	private bool EnsureAssetsFetched(HashSet<string> requiredTextures, HashSet<string> requiredModels)
	{
		GatherRequiredAssets(requiredTextures, requiredModels);
		_assetPaths.Clear();
		List<AssetReference> list = new List<AssetReference>();
		foreach (string requiredTexture in requiredTextures)
		{
			list.Add(new AssetReference("Texture", AssetPathUtils.GetAssetPathWithCommon(requiredTexture)));
			_assetPaths.Add(AssetPathUtils.GetAssetPathWithCommon(requiredTexture));
		}
		foreach (string requiredModel in requiredModels)
		{
			list.Add(new AssetReference("Model", AssetPathUtils.GetAssetPathWithCommon(requiredModel)));
			_assetPaths.Add(AssetPathUtils.GetAssetPathWithCommon(requiredModel));
		}
		bool flag = false;
		foreach (AssetReference item in list)
		{
			if (!_assetEditorOverlay.TrackedAssets.TryGetValue(item.FilePath, out var value))
			{
				flag = true;
				_assetEditorOverlay.FetchTrackedAsset(item);
			}
			else if (value.IsLoading)
			{
				flag = true;
			}
		}
		return !flag;
	}

	private void CreateTextureAtlas(HashSet<string> texturePaths, out Texture texture)
	{
		Dictionary<string, Image> dictionary = new Dictionary<string, Image>();
		foreach (string texturePath in texturePaths)
		{
			string assetPathWithCommon = AssetPathUtils.GetAssetPathWithCommon(texturePath);
			if (!dictionary.ContainsKey(assetPathWithCommon) && _assetEditorOverlay.TrackedAssets.TryGetValue(assetPathWithCommon, out var value) && value.IsAvailable)
			{
				dictionary[texturePath] = (Image)value.Data;
			}
		}
		texture = TextureAtlasUtils.CreateTextureAtlas(dictionary, out _textureLocations);
		_textureSizes.Clear();
		foreach (KeyValuePair<string, Image> item in dictionary)
		{
			_textureSizes.Add(item.Key, new Point(item.Value.Width, item.Value.Height));
		}
	}

	protected AssetPreview(AssetEditorOverlay assetEditorOverlay, Element parent)
		: base(assetEditorOverlay.Desktop, parent)
	{
		_assetEditorOverlay = assetEditorOverlay;
		CameraType = CameraType.Camera2D;
		RenderScene = Render;
		EnableCameraPositionControls = false;
		EnableCameraOrientationControls = true;
		EnableCameraScaleControls = false;
	}

	protected override void OnUnmounted()
	{
		base.OnUnmounted();
		DisposeRenderer();
	}

	private void DisposeRenderer()
	{
		_needsUpdateAfterRendererDisposal = true;
		_model = null;
		_textureAtlas?.Dispose();
		_textureAtlas = null;
		_fallbackTexture = null;
		_blockVertexData = null;
		_gradientTexture = null;
		_textureSizes.Clear();
		_textureLocations.Clear();
		_animatedBlockRenderer?.Dispose();
		_animatedBlockRenderer = null;
		_modelRenderer?.Dispose();
		_modelRenderer = null;
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		base.OnMouseButtonDown(evt);
		if ((long)evt.Button == 1)
		{
			_isMouseDragging = true;
			_dragStartMouseX = Desktop.MousePosition.X;
			_dragStartModelAngleY = _targetModelAngleY;
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if ((long)evt.Button == 1)
		{
			_isMouseDragging = false;
		}
	}

	protected override void OnMouseMove()
	{
		base.OnMouseMove();
		if (_isMouseDragging)
		{
			_targetModelAngleY = _dragStartModelAngleY + (float)(Desktop.MousePosition.X - _dragStartMouseX);
			_updateUserRotation = true;
		}
	}

	protected override void Animate(float deltaTime)
	{
		base.Animate(deltaTime);
		if (_updateUserRotation)
		{
			_lerpModelAngleY = MathHelper.Lerp(_lerpModelAngleY, _targetModelAngleY, MathHelper.Min(1f, 10f * deltaTime));
			CameraOrientation.Y = 0f - MathHelper.ToRadians(_lerpModelAngleY);
			UpdateViewMatrices();
			if ((double)System.Math.Abs(_lerpModelAngleY - _targetModelAngleY) < 0.1)
			{
				_updateUserRotation = false;
			}
		}
	}

	private void Render()
	{
		GLFunctions gL = Desktop.Graphics.GL;
		if (_animatedBlockRenderer != null)
		{
			MapBlockAnimatedProgram mapBlockAnimatedForwardProgram = Desktop.Graphics.GPUProgramStore.MapBlockAnimatedForwardProgram;
			Matrix matrix = Matrix.Identity;
			gL.Enable(GL.DEPTH_TEST);
			gL.Disable(GL.BLEND);
			gL.UseProgram(mapBlockAnimatedForwardProgram);
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BindTexture(GL.TEXTURE_2D, _textureAtlas.GLTexture);
			gL.ActiveTexture(GL.TEXTURE1);
			gL.BindTexture(GL.TEXTURE_2D, _fallbackTexture.GLTexture);
			mapBlockAnimatedForwardProgram.AssertInUse();
			gL.AssertEnabled(GL.DEPTH_TEST);
			mapBlockAnimatedForwardProgram.ViewProjectionMatrix.SetValue(ref ViewProjectionMatrix);
			mapBlockAnimatedForwardProgram.ModelMatrix.SetValue(ref matrix);
			mapBlockAnimatedForwardProgram.NodeBlock.SetBuffer(_animatedBlockRenderer.NodeBuffer);
			gL.BindVertexArray(_animatedBlockRenderer.VertexArray);
			gL.DrawElements(GL.TRIANGLES, _animatedBlockRenderer.IndicesCount, GL.UNSIGNED_INT, (IntPtr)0);
		}
		else if (_modelRenderer != null)
		{
			BlockyModelProgram blockyModelForwardProgram = Desktop.Graphics.GPUProgramStore.BlockyModelForwardProgram;
			Matrix matrix2 = Matrix.Identity;
			gL.Enable(GL.DEPTH_TEST);
			gL.Disable(GL.BLEND);
			gL.UseProgram(blockyModelForwardProgram);
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BindTexture(GL.TEXTURE_2D, _textureAtlas.GLTexture);
			gL.ActiveTexture(GL.TEXTURE1);
			gL.BindTexture(GL.TEXTURE_2D, _fallbackTexture.GLTexture);
			gL.ActiveTexture(GL.TEXTURE3);
			gL.BindTexture(GL.TEXTURE_2D, _gradientTexture.GLTexture);
			blockyModelForwardProgram.AssertInUse();
			gL.AssertEnabled(GL.DEPTH_TEST);
			blockyModelForwardProgram.ViewProjectionMatrix.SetValue(ref ViewProjectionMatrix);
			blockyModelForwardProgram.ModelMatrix.SetValue(ref matrix2);
			blockyModelForwardProgram.NodeBlock.SetBuffer(_modelRenderer.NodeBuffer);
			_modelRenderer.Draw();
		}
		gL.Disable(GL.DEPTH_TEST);
		gL.Enable(GL.BLEND);
	}

	private void InitializeRenderer()
	{
		_lerpModelAngleY = (_targetModelAngleY = 0f - MathHelper.ToDegrees(CameraOrientation.Y));
		if (_animatedBlockRenderer != null)
		{
			_animatedBlockRenderer.Dispose();
			_animatedBlockRenderer = null;
		}
		if (_modelRenderer != null)
		{
			_modelRenderer.Dispose();
			_modelRenderer = null;
		}
		Point[] atlasSizes = new Point[2]
		{
			new Point(_textureAtlas.Width, _textureAtlas.Height),
			new Point(_fallbackTexture.Width, _fallbackTexture.Height)
		};
		if (_blockVertexData != null)
		{
			_animatedBlockRenderer = new AnimatedBlockRenderer(_model, atlasSizes, _blockVertexData, Desktop.Graphics, selfManageNodeBuffer: true);
			_animatedBlockRenderer.UpdatePose();
			_animatedBlockRenderer.SendDataToGPU();
		}
		else if (_model != null)
		{
			_modelRenderer = new ModelRenderer(_model, atlasSizes, Desktop.Graphics, 0u, selfManageNodeBuffer: true);
			_modelRenderer.UpdatePose();
			_modelRenderer.SendDataToGPU();
		}
		NeedsRendering = true;
	}

	public byte[] Capture(int width, int height)
	{
		_renderTarget.Resize(width, height);
		byte[] result = RenderIntoRgbaByteArray();
		_renderTarget.Resize(base.AnchoredRectangle.Width, base.AnchoredRectangle.Height);
		return result;
	}

	public void OnTrackedAssetChanged(TrackedAsset trackedAsset)
	{
		if (_assetPaths.Contains(trackedAsset.Reference.FilePath))
		{
			TrySetupRenderer();
		}
	}

	public void UpdateCameraSettings(AssetEditorPreviewCameraSettings cameraSettings)
	{
		_cameraSettings = cameraSettings;
		ApplyCameraSettings();
		UpdateViewMatrices();
	}

	private void ApplyCameraSettings()
	{
		CameraScale = _cameraSettings.ModelScale / 32f;
		CameraPosition = new Vector3(_cameraSettings.CameraPosition.X, _cameraSettings.CameraPosition.Y, _cameraSettings.CameraPosition.Z);
		CameraOrientation = new Vector3(_cameraSettings.CameraOrientation.X, _cameraSettings.CameraOrientation.Y, _cameraSettings.CameraOrientation.Z);
	}

	protected void TrySetupRenderer()
	{
		DisposeRenderer();
		try
		{
			SetupRenderer();
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to set up preview.");
			DisposeRenderer();
		}
		Layout();
		_needsUpdateAfterRendererDisposal = false;
	}

	private void SetupRenderer()
	{
		if (IsAssetValid())
		{
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<string> requiredModels = new HashSet<string>();
			if (EnsureAssetsFetched(hashSet, requiredModels) && AreMinimumRequiredAssetsAvailable())
			{
				CreateTextureAtlas(hashSet, out _textureAtlas);
				SetupModelData();
				_fallbackTexture = Desktop.Graphics.WhitePixelTexture;
				_gradientTexture = _assetEditorOverlay.Interface.App.CharacterPartStore.CharacterGradientAtlas;
				ApplyCameraSettings();
				InitializeRenderer();
			}
		}
	}

	protected virtual bool AreMinimumRequiredAssetsAvailable()
	{
		return true;
	}

	protected virtual bool IsAssetValid()
	{
		return true;
	}

	protected abstract void SetupModelData();

	protected abstract void GatherRequiredAssets(HashSet<string> requiredTextures, HashSet<string> requiredModels);
}
