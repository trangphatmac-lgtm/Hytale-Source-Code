using System;
using System.Collections.Generic;
using HytaleClient.Application;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.InGame.Modules.InterfaceRenderPreview;

internal class InterfaceRenderPreviewModule : Module
{
	public struct BlockyModelDrawTask
	{
		public Rectangle Viewport;

		public Matrix ProjectionMatrix;

		public Matrix ModelMatrix;

		public GLBuffer AnimationData;

		public uint AnimationDataOffset;

		public ushort AnimationDataSize;

		public GLVertexArray VertexArray;

		public int DataCount;
	}

	public struct AnimatedBlockDrawTask
	{
		public Rectangle Viewport;

		public Matrix ProjectionMatrix;

		public Matrix ModelMatrix;

		public GLBuffer AnimationData;

		public uint AnimationDataOffset;

		public ushort AnimationDataSize;

		public GLVertexArray VertexArray;

		public int DataCount;
	}

	public class ModelPreviewParams : PreviewParams
	{
		public string Model;

		public string Texture;

		public string[][] Attachments;

		public string Animation;

		public string ItemInHand;
	}

	public class ItemPreviewParams : PreviewParams
	{
		public string ItemId;
	}

	public class PreviewParams
	{
		public int Id;

		public Rectangle Viewport;

		public bool Rotatable = true;

		public float Scale;

		public float[] Translation;

		public float[] Rotation;

		public bool Ortho;

		public float[] ZoomRange = new float[2] { -1f, -1f };
	}

	private readonly Dictionary<int, Preview> _previews = new Dictionary<int, Preview>();

	private bool _useFXAA;

	private RenderTarget _inventoryRenderTarget;

	private const int BlockyModelTasksDefaultSize = 10;

	public const int BlockyModelTasksGrowth = 10;

	private BlockyModelDrawTask[] _blockyModelDrawTasks = new BlockyModelDrawTask[10];

	private int _blockyModelDrawTaskCount;

	public const int AnimatedBlockTasksDefaultSize = 10;

	public const int AnimatedBlockTasksGrowth = 10;

	private AnimatedBlockDrawTask[] _animatedBlockDrawTasks = new AnimatedBlockDrawTask[10];

	private int _animatedBlockDrawTaskCount;

	private PostEffectRenderer _postEffectRenderer;

	public InterfaceRenderPreviewModule(GameInstance gameInstance, bool useFXAA = false)
		: base(gameInstance)
	{
		PostEffectProgram inventoryPostEffectProgram = _gameInstance.Engine.Graphics.GPUProgramStore.InventoryPostEffectProgram;
		_postEffectRenderer = new PostEffectRenderer(gameInstance.Engine.Graphics, gameInstance.Engine.Profiling, inventoryPostEffectProgram);
		_useFXAA = useFXAA;
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		int width = _gameInstance.Engine.Window.Viewport.Width;
		int height = _gameInstance.Engine.Window.Viewport.Height;
		_inventoryRenderTarget = new RenderTarget(width, height, "_inventoryRenderTarget");
		_inventoryRenderTarget.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST);
		_inventoryRenderTarget.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		_inventoryRenderTarget.FinalizeSetup();
	}

	protected override void DoDispose()
	{
		_inventoryRenderTarget.Dispose();
		foreach (Preview value in _previews.Values)
		{
			value.Dispose();
		}
	}

	public void Resize(int width, int height)
	{
		_inventoryRenderTarget.Resize(width, height);
	}

	public void OnUserInput(SDL_Event evt)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!ArePreviewsEnabled())
		{
			return;
		}
		foreach (Preview value in _previews.Values)
		{
			value.OnUserInput(evt);
		}
	}

	public void HandleAssetsChanged()
	{
		foreach (Preview value in _previews.Values)
		{
			value.UpdateRenderer();
		}
	}

	public void RemovePreview(int id)
	{
		if (_previews.TryGetValue(id, out var value))
		{
			value.Dispose();
			_previews.Remove(id);
		}
	}

	public void AddEntityModelPreview(ModelPreviewParams parameters)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		Model val = new Model();
		val.Model_ = parameters.Model;
		val.Texture = parameters.Texture;
		if (parameters.Attachments != null)
		{
			val.Attachments = (ModelAttachment[])(object)new ModelAttachment[parameters.Attachments.Length];
			for (int i = 0; i < val.Attachments.Length; i++)
			{
				val.Attachments[i] = new ModelAttachment(parameters.Attachments[i][0], parameters.Attachments[i][1], parameters.Attachments[i][2], parameters.Attachments[i][3]);
			}
		}
		if (_previews.TryGetValue(parameters.Id, out var value))
		{
			((EntityModelPreview)value).UpdateModelRenderer(val, parameters.ItemInHand);
		}
		else
		{
			EntityModelPreview value2 = new EntityModelPreview(val, parameters.ItemInHand, _gameInstance);
			_previews.Add(parameters.Id, value2);
		}
		_previews[parameters.Id].SetBaseParams(parameters);
	}

	public void AddItemPreview(ItemPreviewParams parameters)
	{
		if (_previews.ContainsKey(parameters.Id))
		{
			((ItemPreview)_previews[parameters.Id]).UpdateItemRenderer(parameters.ItemId);
		}
		else
		{
			ItemPreview value = new ItemPreview(parameters.ItemId, _gameInstance);
			_previews.Add(parameters.Id, value);
		}
		_previews[parameters.Id].SetBaseParams(parameters);
	}

	public void AddCharacterPreview(PreviewParams parameters)
	{
		if (_previews.ContainsKey(parameters.Id))
		{
			((CharacterPreview)_previews[parameters.Id]).SetBaseParams(parameters);
			return;
		}
		CharacterPreview characterPreview = new CharacterPreview(_gameInstance);
		characterPreview.SetBaseParams(parameters);
		_previews.Add(parameters.Id, characterPreview);
	}

	public void Update(float deltaTime)
	{
		foreach (Preview value in _previews.Values)
		{
			value.Update(deltaTime);
		}
	}

	public void PrepareForDraw()
	{
		_blockyModelDrawTaskCount = 0;
		_animatedBlockDrawTaskCount = 0;
		if (!ArePreviewsEnabled())
		{
			return;
		}
		foreach (Preview value in _previews.Values)
		{
			value.PrepareForDraw(ref _blockyModelDrawTaskCount, ref _animatedBlockDrawTaskCount, ref _blockyModelDrawTasks, ref _animatedBlockDrawTasks);
		}
	}

	public bool ArePreviewsEnabled()
	{
		return _gameInstance.App.InGame.CurrentOverlay == AppInGame.InGameOverlay.None;
	}

	public bool NeedsDrawing()
	{
		return _animatedBlockDrawTaskCount > 0 || _blockyModelDrawTaskCount > 0;
	}

	public void Draw()
	{
		if (!NeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		gL.ActiveTexture(GL.TEXTURE3);
		gL.BindTexture(GL.TEXTURE_2D, _gameInstance.App.CharacterPartStore.CharacterGradientAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE2);
		gL.BindTexture(GL.TEXTURE_2D, _gameInstance.App.CharacterPartStore.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE1);
		gL.BindTexture(GL.TEXTURE_2D, _gameInstance.EntityStoreModule.TextureAtlas.GLTexture);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, _gameInstance.MapModule.TextureAtlas.GLTexture);
		gL.Disable(GL.BLEND);
		if (_useFXAA)
		{
			_inventoryRenderTarget.Bind(clear: true, setupViewport: false);
		}
		BlockyModelProgram blockyModelForwardProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BlockyModelForwardProgram;
		gL.UseProgram(blockyModelForwardProgram);
		blockyModelForwardProgram.NearScreendoorThreshold.SetValue(0.1f);
		int height = _gameInstance.Engine.Window.Viewport.Height;
		for (int i = 0; i < _blockyModelDrawTaskCount; i++)
		{
			ref BlockyModelDrawTask reference = ref _blockyModelDrawTasks[i];
			gL.Viewport(reference.Viewport.X, height - reference.Viewport.Y - reference.Viewport.Height, reference.Viewport.Width, reference.Viewport.Height);
			blockyModelForwardProgram.ViewProjectionMatrix.SetValue(ref reference.ProjectionMatrix);
			blockyModelForwardProgram.ModelMatrix.SetValue(ref reference.ModelMatrix);
			blockyModelForwardProgram.NodeBlock.SetBufferRange(reference.AnimationData, reference.AnimationDataOffset, reference.AnimationDataSize);
			gL.BindVertexArray(reference.VertexArray);
			gL.DrawElements(GL.TRIANGLES, reference.DataCount, GL.UNSIGNED_SHORT, (IntPtr)0);
		}
		MapBlockAnimatedProgram mapBlockAnimatedForwardProgram = _gameInstance.Engine.Graphics.GPUProgramStore.MapBlockAnimatedForwardProgram;
		gL.UseProgram(mapBlockAnimatedForwardProgram);
		for (int j = 0; j < _animatedBlockDrawTaskCount; j++)
		{
			ref AnimatedBlockDrawTask reference2 = ref _animatedBlockDrawTasks[j];
			gL.Viewport(reference2.Viewport.X, height - reference2.Viewport.Y - reference2.Viewport.Height, reference2.Viewport.Width, reference2.Viewport.Height);
			mapBlockAnimatedForwardProgram.ViewProjectionMatrix.SetValue(ref reference2.ProjectionMatrix);
			mapBlockAnimatedForwardProgram.ModelMatrix.SetValue(ref reference2.ModelMatrix);
			mapBlockAnimatedForwardProgram.NodeBlock.SetBufferRange(reference2.AnimationData, reference2.AnimationDataOffset, reference2.AnimationDataSize);
			gL.BindVertexArray(reference2.VertexArray);
			gL.DrawElements(GL.TRIANGLES, reference2.DataCount, GL.UNSIGNED_INT, (IntPtr)0);
		}
		if (_useFXAA)
		{
			_inventoryRenderTarget.Unbind();
		}
		gL.Viewport(_gameInstance.Engine.Window.Viewport);
		if (_useFXAA)
		{
			_postEffectRenderer.Draw(_inventoryRenderTarget.GetTexture(RenderTarget.Target.Color0), GLTexture.None, _inventoryRenderTarget.Width, _inventoryRenderTarget.Height, 1f);
		}
		gL.Enable(GL.BLEND);
	}
}
