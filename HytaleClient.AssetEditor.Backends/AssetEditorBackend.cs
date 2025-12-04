using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Config;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Backends;

internal abstract class AssetEditorBackend : Disposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const string GradientSetsDatasetId = "GradientSets";

	public const string GradientIdsDatasetId = "GradientIds";

	protected readonly AssetEditorOverlay AssetEditorOverlay;

	public AssetTreeFolder[] SupportedAssetTreeFolders { get; protected set; } = new AssetTreeFolder[0];


	public bool IsEditingRemotely { get; protected set; }

	public bool IsExportingAssets { get; protected set; }

	protected AssetEditorBackend(AssetEditorOverlay assetEditorOverlay)
	{
		AssetEditorOverlay = assetEditorOverlay;
	}

	public abstract void Initialize();

	public abstract void CreateDirectory(string path, bool applyLocally, Action<string, FormattedMessage> callback);

	public abstract void DeleteDirectory(string path, bool applyLocally, Action<string, FormattedMessage> callback);

	public abstract void RenameDirectory(string path, string newPath, bool applyLocally, Action<string, FormattedMessage> callback);

	public abstract void FetchAsset(AssetReference assetReference, Action<object, FormattedMessage> action, bool trackUpdates = false);

	public abstract void FetchJsonAssetWithParents(AssetReference assetReference, Action<Dictionary<string, TrackedAsset>, FormattedMessage> callback, bool trackUpdates = false);

	public abstract void SetOpenEditorAsset(AssetReference assetReference);

	public abstract void FetchAutoCompleteData(string dataset, string query, Action<HashSet<string>, FormattedMessage> callback);

	public virtual bool TryGetDropdownEntriesOrFetch(string dataset, out List<string> entries, object extraValue = null)
	{
		if (dataset == "GradientSets")
		{
			entries = new List<string>(AssetEditorOverlay.Interface.App.CharacterPartStore.GradientSets.Keys);
			entries.Sort();
			return true;
		}
		if (dataset == "GradientIds")
		{
			if (extraValue != null && extraValue is string && AssetEditorOverlay.Interface.App.CharacterPartStore.GradientSets.TryGetValue((string)extraValue, out var value))
			{
				entries = new List<string>(value.Gradients.Keys);
				entries.Sort();
			}
			else
			{
				entries = new List<string>();
			}
			return true;
		}
		entries = null;
		return false;
	}

	public abstract void CreateAsset(AssetReference assetReference, object data, string buttonId = null, bool openInTab = false, Action<FormattedMessage> callback = null);

	public abstract void UpdateJsonAsset(AssetReference assetReference, List<ClientJsonUpdateCommand> jsonUpdateCommands, Action<FormattedMessage> callback = null);

	public abstract void UpdateAsset(AssetReference assetReference, object data, Action<FormattedMessage> callback = null);

	public abstract void DeleteAsset(AssetReference assetReference, bool applyLocally);

	public abstract void RenameAsset(AssetReference assetReference, string newAssetPath, bool applyLocally);

	public virtual void OnValueChanged(PropertyPath path, JToken value)
	{
	}

	public virtual void SaveUnsavedChanges()
	{
	}

	public abstract void UndoChanges(AssetReference assetReference);

	public abstract void RedoChanges(AssetReference assetReference);

	public virtual void OnEditorOpen(bool isOpen)
	{
	}

	public virtual void OnSidebarButtonActivated(string action)
	{
	}

	public virtual string GetButtonText(string messageId)
	{
		return AssetEditorOverlay.Interface.GetText(messageId);
	}

	public virtual void ExportAssets(List<AssetReference> assetReferences, Action<List<TimestampedAssetReference>> callback = null)
	{
	}

	public virtual void SetGameTime(DateTime time, bool paused)
	{
	}

	public virtual void SetWeatherAndTimeLock(bool locked)
	{
	}

	public void ExportAndDiscardAssets(List<AssetReference> assetReferences)
	{
		ExportAssets(assetReferences, delegate(List<TimestampedAssetReference> exportedAssets)
		{
			List<TimestampedAssetReference> list = new List<TimestampedAssetReference>();
			foreach (TimestampedAssetReference exportedAsset in exportedAssets)
			{
				if (exportedAsset.Timestamp != null)
				{
					list.Add(exportedAsset);
				}
			}
			DiscardChanges(list);
		});
	}

	public void DiscardChanges(TimestampedAssetReference assetToDiscard)
	{
		DiscardChanges(new List<TimestampedAssetReference> { assetToDiscard });
	}

	public virtual void DiscardChanges(List<TimestampedAssetReference> assetsToDiscard)
	{
	}

	public virtual void FetchLastModifiedAssets()
	{
	}

	public virtual void OnLanguageChanged()
	{
	}

	public virtual void UpdateSubscriptionToModifiedAssetsUpdates(bool subscribe)
	{
	}

	public virtual AssetInfo[] GetLastModifiedAssets()
	{
		return null;
	}

	protected SchemaNode LoadSchema(JObject jObject, Dictionary<string, SchemaNode> schemas)
	{
		Dictionary<string, SchemaNode> dictionary = new Dictionary<string, SchemaNode>();
		SchemaParser schemaParser = new SchemaParser(AssetEditorOverlay.Interface.App.Settings.DiagnosticMode);
		SchemaNode schemaNode;
		try
		{
			schemaNode = schemaParser.Parse(jObject, dictionary);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to parse schema at path " + schemaParser.CurrentPath, innerException);
		}
		if (schemaNode.Properties != null)
		{
			if (schemaNode.Properties.TryGetValue("$Comment", out var value))
			{
				value.IsHidden = false;
			}
			if (schemaNode.Properties.TryGetValue("Parent", out var value2) && value2.IsParentProperty)
			{
				KeyValuePair<string, SchemaNode> keyValuePair = schemaNode.Properties.FirstOrDefault();
				if (keyValuePair.Value != null && keyValuePair.Value.SectionStart == null)
				{
					keyValuePair.Value.SectionStart = "General";
				}
			}
		}
		Logger.Info("Loaded schema with id {0}", schemaNode.Id);
		schemas[schemaNode.Id] = schemaNode;
		foreach (KeyValuePair<string, SchemaNode> item in dictionary)
		{
			if (item.Value.Properties != null && item.Value.Properties.TryGetValue("Parent", out var value3) && value3.IsParentProperty)
			{
				KeyValuePair<string, SchemaNode> keyValuePair2 = item.Value.Properties.FirstOrDefault();
				if (keyValuePair2.Value != null && keyValuePair2.Value.SectionStart == null)
				{
					keyValuePair2.Value.SectionStart = "General";
				}
			}
			schemas[schemaNode.Id + item.Key] = item.Value;
			Logger.Info("Loaded definition schema with id {0}", schemaNode.Id + item.Key);
		}
		return schemaNode;
	}

	protected void ApplySchemaMetadata(AssetTypeConfig assetTypeConfig, JObject meta)
	{
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Expected O, but got Unknown
		if (meta.ContainsKey("extension"))
		{
			assetTypeConfig.FileExtension = (string)meta["extension"];
		}
		else
		{
			assetTypeConfig.FileExtension = ".json";
		}
		if (meta.ContainsKey("uiTypeIcon"))
		{
			string fullPath = Path.GetFullPath(Path.Combine(Paths.BuiltInAssets, "Schema", "Icons"));
			string fullPath2 = Path.GetFullPath(Path.Combine(fullPath, (string)meta["uiTypeIcon"]));
			if (Paths.IsSubPathOf(fullPath2, fullPath) && File.Exists(fullPath2))
			{
				assetTypeConfig.IconImage = new Image(File.ReadAllBytes(fullPath2));
			}
		}
		if (meta.ContainsKey("idProvider"))
		{
			assetTypeConfig.IdProvider = (string)meta["idProvider"];
		}
		if (assetTypeConfig.IconImage == null)
		{
			assetTypeConfig.Icon = new PatchStyle("AssetEditor/AssetIcons/File.png");
		}
		if (meta.ContainsKey("internalKeys"))
		{
			JArray val = (JArray)meta["internalKeys"];
			assetTypeConfig.InternalAssetIds = new string[((JContainer)val).Count];
			for (int i = 0; i < ((JContainer)val).Count; i++)
			{
				assetTypeConfig.InternalAssetIds[i] = (string)val[i];
			}
		}
		if (meta.ContainsKey("uiEditorFeatures"))
		{
			assetTypeConfig.EditorFeatures = new List<AssetTypeConfig.EditorFeature>();
			foreach (JToken item in (JArray)meta["uiEditorFeatures"])
			{
				if (Enum.TryParse<AssetTypeConfig.EditorFeature>((string)item, out var result))
				{
					assetTypeConfig.EditorFeatures.Add(result);
				}
			}
		}
		if (meta.ContainsKey("uiRebuildCaches"))
		{
			JArray val2 = (JArray)meta["uiRebuildCaches"];
			assetTypeConfig.RebuildCaches = new List<AssetTypeConfig.RebuildCacheType>();
			foreach (JToken item2 in val2)
			{
				if (Enum.TryParse<AssetTypeConfig.RebuildCacheType>((string)item2, out var result2))
				{
					assetTypeConfig.RebuildCaches.Add(result2);
				}
			}
		}
		if (meta.ContainsKey("uiEditorPreview"))
		{
			assetTypeConfig.Preview = (AssetTypeConfig.PreviewType)Enum.Parse(typeof(AssetTypeConfig.PreviewType), (string)meta["uiEditorPreview"]);
		}
		if (meta.ContainsKey("uiSidebarButtons"))
		{
			assetTypeConfig.SidebarButtons = new List<AssetTypeConfig.Button>();
			foreach (JToken item3 in (IEnumerable<JToken>)meta["uiSidebarButtons"])
			{
				assetTypeConfig.SidebarButtons.Add(new AssetTypeConfig.Button((string)item3[(object)"textId"], (string)item3[(object)"buttonId"]));
			}
		}
		if (!meta.ContainsKey("uiCreateButtons"))
		{
			return;
		}
		assetTypeConfig.CreateButtons = new List<AssetTypeConfig.Button>();
		foreach (JToken item4 in (IEnumerable<JToken>)meta["uiCreateButtons"])
		{
			assetTypeConfig.CreateButtons.Add(new AssetTypeConfig.Button((string)item4[(object)"textId"], (string)item4[(object)"buttonId"]));
		}
	}
}
