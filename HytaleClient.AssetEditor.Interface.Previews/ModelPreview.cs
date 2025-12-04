using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Previews;

internal class ModelPreview : AssetPreview
{
	private Model _modelData;

	public ModelPreview(AssetEditorOverlay assetEditorOverlay, Element parent)
		: base(assetEditorOverlay, parent)
	{
	}

	public void Setup(Model model, AssetEditorPreviewCameraSettings cameraSettings)
	{
		bool flag = _needsUpdateAfterRendererDisposal || NeedsUpdate(model);
		_modelData = model;
		_cameraSettings = cameraSettings;
		if (flag)
		{
			TrySetupRenderer();
		}
	}

	private bool NeedsUpdate(Model model)
	{
		Model modelData = _modelData;
		if (model == null && modelData == null)
		{
			return false;
		}
		if (model != modelData && (model == null || modelData == null))
		{
			return true;
		}
		if (!string.Equals(model.Model_, modelData.Model_))
		{
			return true;
		}
		if (!string.Equals(model.Texture, modelData.Texture))
		{
			return true;
		}
		if (!string.Equals(model.GradientId, modelData.GradientId))
		{
			return true;
		}
		if (!string.Equals(model.GradientSet, modelData.GradientSet))
		{
			return true;
		}
		if (model.Attachments != modelData.Attachments && (model.Attachments == null || modelData.Attachments == null))
		{
			return true;
		}
		if (model.Attachments != null)
		{
			if (model.Attachments.Length != modelData.Attachments.Length)
			{
				return true;
			}
			for (int i = 0; i < model.Attachments.Length; i++)
			{
				ModelAttachment val = model.Attachments[i];
				ModelAttachment val2 = modelData.Attachments[i];
				if (!string.Equals(val.Model, val2.Model))
				{
					return true;
				}
				if (!string.Equals(val.Texture, val2.Texture))
				{
					return true;
				}
				if (!string.Equals(val.GradientId, val2.GradientId))
				{
					return true;
				}
				if (!string.Equals(val.GradientSet, val2.GradientSet))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected override bool AreMinimumRequiredAssetsAvailable()
	{
		TrackedAsset trackedAsset = _assetEditorOverlay.TrackedAssets[AssetPathUtils.GetAssetPathWithCommon(_modelData.Model_)];
		return trackedAsset.IsAvailable;
	}

	protected override bool IsAssetValid()
	{
		return _modelData.Model_ != null;
	}

	protected override void GatherRequiredAssets(HashSet<string> requiredTextures, HashSet<string> requiredModels)
	{
		requiredModels.Add(_modelData.Model_);
		if (_modelData.Texture != null)
		{
			requiredTextures.Add(_modelData.Texture);
		}
		if (_modelData.Attachments == null)
		{
			return;
		}
		ModelAttachment[] attachments = _modelData.Attachments;
		foreach (ModelAttachment val in attachments)
		{
			if (val.Model != null)
			{
				requiredModels.Add(val.Model);
				if (val.Texture != null)
				{
					requiredTextures.Add(val.Texture);
				}
			}
		}
	}

	protected override void SetupModelData()
	{
		AssetEditorApp app = _assetEditorOverlay.Interface.App;
		CharacterPartStore characterPartStore = app.CharacterPartStore;
		Model modelData = _modelData;
		if (modelData?.Model_ == null)
		{
			return;
		}
		TryGetModelAndTexture(modelData.Model_, modelData.Texture, out var blockyModel, out var uvOffset, out var textureAtlasIndex);
		if (modelData.GradientSet != null && modelData.GradientId != null && characterPartStore.GradientSets.TryGetValue(modelData.GradientSet, out var value) && value.Gradients.TryGetValue(modelData.GradientId, out var value2))
		{
			blockyModel.SetGradientId(value2.GradientId);
		}
		blockyModel.SetAtlasIndex(textureAtlasIndex);
		blockyModel.OffsetUVs(uvOffset);
		if (modelData.Attachments != null)
		{
			ModelAttachment[] attachments = modelData.Attachments;
			foreach (ModelAttachment val in attachments)
			{
				if (TryGetModelAndTexture(val.Model, val.Texture, out var blockyModel2, out var uvOffset2, out var textureAtlasIndex2))
				{
					if (val.GradientSet != null && val.GradientId != null && characterPartStore.GradientSets.TryGetValue(val.GradientSet, out value) && value.Gradients.TryGetValue(val.GradientId, out value2))
					{
						blockyModel2.GradientId = value2.GradientId;
					}
					blockyModel.Attach(blockyModel2, characterPartStore.CharacterNodeNameManager, uvOffset: uvOffset2, atlasIndex: textureAtlasIndex2);
				}
			}
		}
		_model = blockyModel;
	}

	private bool TryGetModelAndTexture(string modelPath, string texturePath, out BlockyModel blockyModel, out Point uvOffset, out byte textureAtlasIndex)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		uvOffset = Point.Zero;
		textureAtlasIndex = 0;
		blockyModel = null;
		if (modelPath == null)
		{
			return false;
		}
		TrackedAsset trackedAsset = _assetEditorOverlay.TrackedAssets[AssetPathUtils.GetAssetPathWithCommon(modelPath)];
		if (!trackedAsset.IsAvailable)
		{
			return false;
		}
		blockyModel = new BlockyModel(BlockyModel.MaxNodeCount);
		BlockyModelInitializer.Parse((JObject)trackedAsset.Data, _assetEditorOverlay.Interface.App.CharacterPartStore.CharacterNodeNameManager, ref blockyModel);
		if (texturePath != null && _textureLocations.TryGetValue(texturePath, out var value))
		{
			uvOffset = value;
		}
		else
		{
			textureAtlasIndex = 1;
		}
		return true;
	}
}
