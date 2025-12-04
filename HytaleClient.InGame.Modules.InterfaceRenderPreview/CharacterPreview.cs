using HytaleClient.Graphics;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Map;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.InterfaceRenderPreview;

internal class CharacterPreview : Preview
{
	private struct ItemModelData
	{
		public float Scale;

		public int TargetNodeIndex;

		public Matrix RootOffsetMatrix;
	}

	private readonly Matrix[] _itemMatrices = new Matrix[2];

	private ModelRenderer _playerModelRenderer;

	private ModelRenderer[] _playerItemModelRenderer = new ModelRenderer[2];

	private ItemModelData[] _playerItemModelData = new ItemModelData[2];

	private int _playerItemCount;

	public override ModelRenderer ModelRenderer => _playerModelRenderer;

	public override AnimatedBlockRenderer AnimatedBlockRenderer => null;

	public CharacterPreview(GameInstance gameInstance)
		: base(gameInstance)
	{
	}

	protected override void DoDispose()
	{
		DisposeModelRenderers();
	}

	public void DisposeModelRenderers()
	{
		_playerModelRenderer?.Dispose();
		_playerModelRenderer = null;
		for (int i = 0; i < _playerItemModelRenderer.Length; i++)
		{
			_playerItemModelRenderer[i]?.Dispose();
			_playerItemModelRenderer[i] = null;
		}
		_playerItemCount = 0;
	}

	private void PrepareItemMatrix(ref ItemModelData item, ref Matrix baseModelMatrix, ref Matrix modelMatrix)
	{
		ref AnimatedRenderer.NodeTransform reference = ref ModelRenderer.NodeTransforms[item.TargetNodeIndex];
		Matrix.Compose(reference.Orientation, reference.Position, out modelMatrix);
		Matrix.Multiply(ref item.RootOffsetMatrix, ref modelMatrix, out modelMatrix);
		Matrix.Multiply(ref modelMatrix, ref baseModelMatrix, out modelMatrix);
		Matrix.ApplyScale(ref modelMatrix, item.Scale);
	}

	public override void PrepareForDraw(ref int blockyModelDrawTaskCount, ref int animatedBlockDrawTaskCount, ref InterfaceRenderPreviewModule.BlockyModelDrawTask[] blockyModelDrawTasks, ref InterfaceRenderPreviewModule.AnimatedBlockDrawTask[] animatedBlockDrawTasks)
	{
		UpdateRenderer();
		_playerModelRenderer.CopyAllSlotAnimations(_gameInstance.LocalPlayer.ModelRenderer);
		_playerModelRenderer.UpdatePose();
		_playerModelRenderer.SendDataToGPU();
		int num = blockyModelDrawTaskCount;
		base.PrepareForDraw(ref blockyModelDrawTaskCount, ref animatedBlockDrawTaskCount, ref blockyModelDrawTasks, ref animatedBlockDrawTasks);
		for (int i = 0; i < _playerItemCount; i++)
		{
			ArrayUtils.GrowArrayIfNecessary(ref blockyModelDrawTasks, blockyModelDrawTaskCount, 10);
			int num2 = blockyModelDrawTaskCount;
			ref ItemModelData item = ref _playerItemModelData[i];
			ModelRenderer modelRenderer = _playerItemModelRenderer[i];
			blockyModelDrawTasks[num2].Viewport = blockyModelDrawTasks[num].Viewport;
			blockyModelDrawTasks[num2].ProjectionMatrix = blockyModelDrawTasks[num].ProjectionMatrix;
			PrepareItemMatrix(ref item, ref blockyModelDrawTasks[num].ModelMatrix, ref blockyModelDrawTasks[num2].ModelMatrix);
			blockyModelDrawTasks[num2].AnimationData = modelRenderer.NodeBuffer;
			blockyModelDrawTasks[num2].AnimationDataOffset = modelRenderer.NodeBufferOffset;
			blockyModelDrawTasks[num2].AnimationDataSize = (ushort)(modelRenderer.NodeCount * 64);
			blockyModelDrawTasks[num2].VertexArray = modelRenderer.VertexArray;
			blockyModelDrawTasks[num2].DataCount = modelRenderer.IndicesCount;
			modelRenderer.UpdatePose();
			modelRenderer.SendDataToGPU();
			blockyModelDrawTaskCount++;
		}
	}

	public override void UpdateRenderer()
	{
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		if (_playerModelRenderer == null || _playerModelRenderer.Timestamp != localPlayer.ModelRenderer.Timestamp)
		{
			_playerModelRenderer?.Dispose();
			_playerModelRenderer = new ModelRenderer(localPlayer.ModelRenderer.Model, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, localPlayer.ModelRenderer.Timestamp, selfManageNodeBuffer: true);
		}
		for (int i = 0; i < localPlayer.EntityItems.Count; i++)
		{
			Entity.EntityItem entityItem = localPlayer.EntityItems[i];
			if (_playerItemModelRenderer[i] == null || _playerItemModelRenderer[i].Timestamp != entityItem.ModelRenderer.Timestamp)
			{
				_playerItemModelRenderer[i]?.Dispose();
				_playerItemModelRenderer[i] = new ModelRenderer(entityItem.ModelRenderer.Model, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, entityItem.ModelRenderer.Timestamp, selfManageNodeBuffer: true);
				_playerItemModelData[i].Scale = entityItem.Scale;
				_playerItemModelData[i].TargetNodeIndex = entityItem.TargetNodeIndex;
				_playerItemModelData[i].RootOffsetMatrix = entityItem.RootOffsetMatrix;
			}
		}
		_playerItemCount = localPlayer.EntityItems.Count;
	}
}
