using System.Collections.Generic;
using HytaleClient.Data;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Data;

public class AssetTypeConfig
{
	public class Button
	{
		public readonly string TextId;

		public readonly string Action;

		public Button(string textId, string action)
		{
			TextId = textId;
			Action = action;
		}
	}

	public enum EditorFeature
	{
		WeatherDaytimeBar,
		WeatherPreviewLocal
	}

	public enum PreviewType
	{
		None,
		Item,
		Model
	}

	public enum RebuildCacheType
	{
		BlockTextures,
		Models,
		ModelTextures,
		MapGemoetry,
		ItemIcons
	}

	public string Id;

	public string Name;

	public Image IconImage;

	public PatchStyle Icon;

	public bool IsColoredIcon;

	public AssetTreeFolder AssetTree;

	public AssetEditorEditorType EditorType;

	public bool IsVirtual;

	public string IdProvider;

	public string[] InternalAssetIds;

	private string _fileExtension;

	public string FileExtensionLowercase;

	private string _path;

	public string PathLowercase;

	public bool HasIdField;

	public SchemaNode Schema;

	public JObject BaseJsonAsset;

	public List<EditorFeature> EditorFeatures;

	public PreviewType Preview;

	public List<Button> SidebarButtons;

	public List<Button> CreateButtons;

	public List<RebuildCacheType> RebuildCaches;

	public bool IsJson => (int)EditorType == 3 || (int)EditorType == 2 || (int)EditorType == 6 || (int)EditorType == 4;

	public string FileExtension
	{
		get
		{
			return _fileExtension;
		}
		set
		{
			_fileExtension = value;
			FileExtensionLowercase = value.ToLowerInvariant();
		}
	}

	public string Path
	{
		get
		{
			return _path;
		}
		set
		{
			_path = value;
			PathLowercase = value.ToLowerInvariant();
		}
	}

	public bool HasFeature(EditorFeature feature)
	{
		return EditorFeatures != null && EditorFeatures.Contains(feature);
	}
}
