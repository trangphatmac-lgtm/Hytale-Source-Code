using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Map;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.InterfaceRenderPreview;

internal class ItemPreview : Preview
{
	private string _itemId;

	private float _itemScale;

	private AnimatedBlockRenderer _animatedBlockRenderer;

	private ModelRenderer _modelRenderer;

	public override AnimatedBlockRenderer AnimatedBlockRenderer => _animatedBlockRenderer;

	public override ModelRenderer ModelRenderer => _modelRenderer;

	public ItemPreview(string itemId, GameInstance gameInstance)
		: base(gameInstance)
	{
		UpdateItemRenderer(itemId);
	}

	public override void UpdateRenderer()
	{
		UpdateItemRenderer();
	}

	public void UpdateItemRenderer(string itemId = null)
	{
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
		if (itemId != null)
		{
			_itemId = itemId;
		}
		ClientItemBase item = _gameInstance.ItemLibraryModule.GetItem(_itemId);
		if (item != null)
		{
			_itemScale = item.Scale;
			if (item.BlockId != 0)
			{
				ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[item.BlockId];
				_itemScale *= clientBlockType.BlockyModelScale;
				_animatedBlockRenderer = new AnimatedBlockRenderer(clientBlockType.FinalBlockyModel, _gameInstance.AtlasSizes, clientBlockType.VertexData, _gameInstance.Engine.Graphics, selfManageNodeBuffer: true);
				_animatedBlockRenderer.UpdatePose();
				_animatedBlockRenderer.SendDataToGPU();
			}
			else
			{
				_modelRenderer = new ModelRenderer(item.Model, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, 0u, selfManageNodeBuffer: true);
				_modelRenderer.UpdatePose();
				_modelRenderer.SendDataToGPU();
			}
		}
	}

	public override void PrepareModelMatrix(ref Matrix modelMatrix)
	{
		base.PrepareModelMatrix(ref modelMatrix);
		Matrix matrix = Matrix.CreateScale(_itemScale);
		Matrix.Multiply(ref modelMatrix, ref matrix, out modelMatrix);
	}
}
