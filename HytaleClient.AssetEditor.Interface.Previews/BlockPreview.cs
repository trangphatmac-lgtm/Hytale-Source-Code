using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Data.Characters;
using HytaleClient.Data.Map;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Previews;

internal class BlockPreview : AssetPreview
{
	private BlockType _blockData;

	public BlockPreview(AssetEditorOverlay assetEditorOverlay, Element parent)
		: base(assetEditorOverlay, parent)
	{
	}

	public void Setup(BlockType blockType, AssetEditorPreviewCameraSettings cameraSettings)
	{
		_blockData = blockType;
		_cameraSettings = cameraSettings;
		TrySetupRenderer();
	}

	protected override bool AreMinimumRequiredAssetsAvailable()
	{
		if (_blockData.Model == null)
		{
			return true;
		}
		TrackedAsset value;
		return _assetEditorOverlay.TrackedAssets.TryGetValue(AssetPathUtils.GetAssetPathWithCommon(_blockData.Model), out value) && value.IsAvailable;
	}

	protected override void SetupModelData()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		BlockType blockData = _blockData;
		JObject modelJson = null;
		if (blockData.Model != null)
		{
			modelJson = (JObject)_assetEditorOverlay.TrackedAssets[AssetPathUtils.GetAssetPathWithCommon(blockData.Model)].Data;
		}
		ClientBlockType clientBlockType = ItemPreviewUtils.ToClientBlockType(blockData, modelJson, _assetEditorOverlay.Interface.App.CharacterPartStore.CharacterNodeNameManager);
		ClientBlockType.CubeTexture[] cubeTextures = clientBlockType.CubeTextures;
		foreach (ClientBlockType.CubeTexture cubeTexture in cubeTextures)
		{
			for (int j = 0; j < cubeTexture.Names.Length; j++)
			{
				string key = cubeTexture.Names[j];
				if (_textureLocations.TryGetValue(key, out var value))
				{
					cubeTexture.TileLinearPositionsInAtlas[j] = value.X / 32;
				}
			}
		}
		if (clientBlockType.BlockyTextures != null && clientBlockType.BlockyTextures.Length != 0)
		{
			if (blockData.ModelTexture_ == null || blockData.ModelTexture_.Length == 0 || !_textureSizes.TryGetValue(blockData.ModelTexture_[0].Texture, out var value2) || !_textureLocations.TryGetValue(clientBlockType.BlockyTextures[0].Name, out var value3))
			{
				value2 = Point.Zero;
				value3 = Point.Zero;
			}
			clientBlockType.OriginalBlockyModel.OffsetUVs(value3);
			clientBlockType.RenderedBlockyModel.PrepareUVs(clientBlockType.OriginalBlockyModel, value2, new Point(_textureAtlas.Width, _textureAtlas.Height));
			clientBlockType.RenderedBlockyModelTextureOrigins = new Vector2[clientBlockType.BlockyTextures.Length];
			for (int k = 0; k < clientBlockType.BlockyTextures.Length; k++)
			{
				clientBlockType.RenderedBlockyModelTextureOrigins[k] = new Vector2((float)value3.X / 32f, 0f);
			}
		}
		if (blockData.CubeSideMaskTexture != null && _textureLocations.TryGetValue(blockData.CubeSideMaskTexture, out var value4))
		{
			clientBlockType.CubeSideMaskTextureAtlasIndex = value4.X / 32;
		}
		if (clientBlockType.ShouldRenderCube)
		{
			clientBlockType.FinalBlockyModel.AddMapBlockNode(clientBlockType, CharacterPartStore.BlockNameId, CharacterPartStore.SideMaskNameId, _textureAtlas.Width);
		}
		ItemPreviewUtils.CreateBlockGeometry(clientBlockType, _textureAtlas);
		_model = clientBlockType.OriginalBlockyModel;
		_blockVertexData = clientBlockType.VertexData;
	}

	protected override void GatherRequiredAssets(HashSet<string> texturePaths, HashSet<string> modelPaths)
	{
		BlockType blockData = _blockData;
		texturePaths.Add("BlockTextures/Unknown.png");
		BlockTextures[] cubeTextures = blockData.CubeTextures;
		foreach (BlockTextures val in cubeTextures)
		{
			if (val.Back != null)
			{
				texturePaths.Add(val.Back);
			}
			if (val.Bottom != null)
			{
				texturePaths.Add(val.Bottom);
			}
			if (val.Front != null)
			{
				texturePaths.Add(val.Front);
			}
			if (val.Left != null)
			{
				texturePaths.Add(val.Left);
			}
			if (val.Right != null)
			{
				texturePaths.Add(val.Right);
			}
			if (val.Top != null)
			{
				texturePaths.Add(val.Top);
			}
		}
		if (blockData.CubeSideMaskTexture != null && !texturePaths.Contains(blockData.CubeSideMaskTexture))
		{
			texturePaths.Add(blockData.CubeSideMaskTexture);
		}
		if (blockData.ModelTexture_ != null)
		{
			ModelTexture[] modelTexture_ = blockData.ModelTexture_;
			foreach (ModelTexture val2 in modelTexture_)
			{
				if (val2 != null)
				{
					texturePaths.Add(val2.Texture);
				}
			}
		}
		if (blockData.Model != null)
		{
			modelPaths.Add(blockData.Model);
		}
	}
}
