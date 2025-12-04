using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Data.Items;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Map;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.InterfaceRenderPreview;

internal class EntityModelPreview : Preview
{
	private Model _model;

	private string _itemInHand;

	private ModelRenderer _modelRenderer;

	public override ModelRenderer ModelRenderer => _modelRenderer;

	public override AnimatedBlockRenderer AnimatedBlockRenderer => null;

	public EntityModelPreview(Model model, string itemInHand, GameInstance gameInstance)
		: base(gameInstance)
	{
		UpdateModelRenderer(model, itemInHand);
	}

	public override void UpdateRenderer()
	{
		UpdateModelRenderer();
	}

	public virtual void UpdateModelRenderer()
	{
		UpdateModelRenderer(null, null);
	}

	public void UpdateModelRenderer(Model model, string itemInHand)
	{
		if (_modelRenderer != null)
		{
			_modelRenderer.Dispose();
			_modelRenderer = null;
		}
		if (model != null)
		{
			_model = model;
			_itemInHand = itemInHand;
		}
		if (_model == null || _model.Model_ == null || _model.Texture == null || !_gameInstance.HashesByServerAssetPath.TryGetValue(_model.Model_, out var value) || !_gameInstance.EntityStoreModule.GetModel(value, out var model2) || !_gameInstance.HashesByServerAssetPath.TryGetValue(_model.Texture, out var value2))
		{
			return;
		}
		CharacterPartStore characterPartStore = _gameInstance.App.CharacterPartStore;
		byte atlasIndex;
		if (characterPartStore.ImageLocations.TryGetValue(_model.Texture, out var value3))
		{
			atlasIndex = 2;
		}
		else
		{
			if (!_gameInstance.EntityStoreModule.ImageLocations.TryGetValue(value2, out value3))
			{
				return;
			}
			atlasIndex = 1;
		}
		BlockyModel blockyModel = model2.Clone();
		blockyModel.SetAtlasIndex(atlasIndex);
		blockyModel.OffsetUVs(value3);
		if (_model.GradientSet != null && _model.GradientId != null && characterPartStore.GradientSets.TryGetValue(_model.GradientSet, out var value4) && value4.Gradients.TryGetValue(_model.GradientId, out var value5))
		{
			blockyModel.SetGradientId(value5.GradientId);
		}
		if (_model.Attachments != null)
		{
			ModelAttachment[] attachments = _model.Attachments;
			foreach (ModelAttachment val in attachments)
			{
				if (LoadAttachmentModel(val.Model, val.Texture, out var model3, out var atlasIndex2, out var uvOffset))
				{
					if (val.GradientSet != null && val.GradientId != null && characterPartStore.GradientSets.TryGetValue(val.GradientSet, out value4) && value4.Gradients.TryGetValue(val.GradientId, out value5))
					{
						model3.GradientId = value5.GradientId;
					}
					blockyModel.Attach(model3, _gameInstance.EntityStoreModule.NodeNameManager, atlasIndex2, uvOffset);
				}
			}
		}
		if (_itemInHand != null)
		{
			ClientItemBase item = _gameInstance.ItemLibraryModule.GetItem(_itemInHand);
			if (item != null)
			{
				AttachItem(item, blockyModel, CharacterPartStore.RightAttachmentNodeNameId);
			}
		}
		_modelRenderer = new ModelRenderer(blockyModel, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, 0u, selfManageNodeBuffer: true);
		_modelRenderer.UpdatePose();
		_modelRenderer.SendDataToGPU();
	}

	private void AttachItem(ClientItemBase item, BlockyModel parentModel, int defaultTargetNodeNameId)
	{
		BlockyModel blockyModel = ((item.BlockId != 0) ? _gameInstance.MapModule.ClientBlockTypes[item.BlockId].FinalBlockyModel : item.Model);
		int forcedTargetNodeNameId = ((item.Armor != null) ? defaultTargetNodeNameId : ((blockyModel.RootNodes.Count == 1 && blockyModel.AllNodes[blockyModel.RootNodes[0]].IsPiece) ? blockyModel.AllNodes[blockyModel.RootNodes[0]].NameId : defaultTargetNodeNameId));
		parentModel.Attach(blockyModel, _gameInstance.EntityStoreModule.NodeNameManager, null, null, forcedTargetNodeNameId);
	}

	private bool LoadAttachmentModel(string modelPath, string texturePath, out BlockyModel model, out byte atlasIndex, out Point uvOffset)
	{
		model = null;
		atlasIndex = 0;
		uvOffset = Point.Zero;
		if (modelPath == null)
		{
			return false;
		}
		if (!_gameInstance.App.CharacterPartStore.Models.TryGetValue("Common/" + modelPath, out model) && (!_gameInstance.HashesByServerAssetPath.TryGetValue(modelPath, out var value) || !_gameInstance.EntityStoreModule.GetModel(value, out model)))
		{
			return false;
		}
		if (texturePath == null)
		{
			return false;
		}
		if (_gameInstance.App.CharacterPartStore.ImageLocations.TryGetValue(texturePath, out uvOffset))
		{
			atlasIndex = 2;
		}
		else
		{
			if (!_gameInstance.HashesByServerAssetPath.TryGetValue(texturePath, out var value2))
			{
				return false;
			}
			if (!_gameInstance.EntityStoreModule.ImageLocations.TryGetValue(value2, out uvOffset))
			{
				return false;
			}
			atlasIndex = 1;
		}
		return true;
	}

	private void AttachItem(BlockyModel model, ClientItemBase item, int defaultTargetAttachmentNameId)
	{
		Entity.EntityItem entityItem = new Entity.EntityItem(_gameInstance);
		BlockyModel blockyModel = ((item.BlockId != 0) ? _gameInstance.MapModule.ClientBlockTypes[item.BlockId].FinalBlockyModel : item.Model);
		if (item.Armor == null && blockyModel.RootNodes.Count == 1 && blockyModel.AllNodes[blockyModel.RootNodes[0]].IsPiece)
		{
			entityItem.TargetNodeNameId = blockyModel.AllNodes[blockyModel.RootNodes[0]].NameId;
			entityItem.SetRootOffsets(Vector3.Negate(blockyModel.AllNodes[blockyModel.RootNodes[0]].Position), Quaternion.Inverse(blockyModel.AllNodes[blockyModel.RootNodes[0]].Orientation));
		}
		else
		{
			entityItem.TargetNodeNameId = defaultTargetAttachmentNameId;
		}
		if (!model.NodeIndicesByNameId.TryGetValue(entityItem.TargetNodeNameId, out entityItem.TargetNodeIndex))
		{
			entityItem.TargetNodeIndex = 0;
		}
		entityItem.ModelRenderer = new ModelRenderer(blockyModel, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, 0u, selfManageNodeBuffer: true);
		BlockyAnimation animation = ((item.BlockId != 0) ? _gameInstance.MapModule.ClientBlockTypes[item.BlockId].BlockyAnimation : item?.Animation);
		entityItem.ModelRenderer.SetSlotAnimation(0, animation);
	}
}
