using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Data;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class AssetFileSelectorEditor : ValueEditor
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	protected FileDropdownBox _dropdown;

	private CancellationTokenSource _previewCancellationToken;

	public AssetFileSelectorEditor(Desktop desktop, Element parent, SchemaNode schema, PropertyPath path, PropertyEditor parentPropertyEditor, SchemaNode parentSchema, ConfigEditor configEditor, JToken value)
		: base(desktop, parent, schema, path, parentPropertyEditor, parentSchema, configEditor, value)
	{
	}

	protected override void Build()
	{
		_dropdown = new FileDropdownBox(Desktop, this, "AssetEditor/FileSelector.ui", () => GetFiles())
		{
			SelectedFiles = ((base.Value != null) ? new HashSet<string> { (string)base.Value } : null),
			AllowedDirectories = Schema.AllowedDirectories,
			SupportsUITextures = Schema.IsUITexture,
			ValueChanged = OnSelectFile,
			DropdownToggled = OnToggleDropdown,
			SelectingInList = UpdatePreview,
			Style = ConfigEditor.FileDropdownBoxStyle
		};
	}

	private void OnSelectFile()
	{
		HashSet<string> selectedFiles = _dropdown.SelectedFiles;
		if (selectedFiles != null && selectedFiles.Count != 0)
		{
			HandleChangeValue(JToken.op_Implicit(selectedFiles.First()));
			Validate();
		}
	}

	private void FetchTextureAsset(string path, Action<Image> action, CancellationToken cancellationToken)
	{
		ConfigEditor.AssetEditorOverlay.Backend.FetchAsset(new AssetReference("Texture", AssetPathUtils.GetAssetPathWithCommon(path)), delegate(object res, FormattedMessage error)
		{
			if (_dropdown.IsMounted && !cancellationToken.IsCancellationRequested)
			{
				_previewCancellationToken = null;
				if (error != null || !(res is Image obj))
				{
					Logger.Info("Failed to fetch preview for " + path);
					action(null);
				}
				else
				{
					action(obj);
				}
			}
		});
	}

	private void UpdatePreview()
	{
		if (!_dropdown.IsMounted)
		{
			return;
		}
		HashSet<string> selectedFilesInList = _dropdown.SelectedFilesInList;
		if (selectedFilesInList == null || selectedFilesInList.Count != 1)
		{
			return;
		}
		string selectedFile = selectedFilesInList.First();
		if (!selectedFile.EndsWith(".png"))
		{
			return;
		}
		_previewCancellationToken?.Cancel();
		_previewCancellationToken = new CancellationTokenSource();
		CancellationToken cancellationToken = _previewCancellationToken.Token;
		FetchTextureAsset(selectedFile, delegate(Image image)
		{
			if (image == null)
			{
				if (_dropdown.SupportsUITextures && !selectedFile.EndsWith("@2x.png"))
				{
					FetchTextureAsset(selectedFile.Substring(0, selectedFile.Length - ".png".Length) + "@2x.png", delegate(Image scaledImage)
					{
						if (scaledImage != null)
						{
							_dropdown.SetPreviewImage(scaledImage);
						}
					}, cancellationToken);
				}
			}
			else
			{
				_dropdown.SetPreviewImage(image);
			}
		}, cancellationToken);
	}

	private void OnToggleDropdown()
	{
		if (!_dropdown.IsOpen)
		{
			return;
		}
		string text = null;
		if (base.Value != null)
		{
			string[] array = ((string)base.Value).Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			string text2 = string.Join("/", array.Take(array.Length - 1));
			if (ConfigEditor.AssetEditorOverlay.Assets.TryGetDirectory(AssetPathUtils.CombinePaths("Common", text2), out var _))
			{
				text = "/" + text2;
			}
		}
		if (text == null && ConfigEditor.LastOpenedFileSelectorDirectory != null)
		{
			string text3 = ConfigEditor.LastOpenedFileSelectorDirectory;
			if (text3 != "")
			{
				text3 += "/";
			}
			if (Schema.AllowedDirectories != null && Schema.AllowedDirectories.Length != 0)
			{
				string[] allowedDirectories = Schema.AllowedDirectories;
				foreach (string value in allowedDirectories)
				{
					if (text3.StartsWith(value))
					{
						text = ConfigEditor.LastOpenedFileSelectorDirectory;
						break;
					}
				}
			}
			else
			{
				text = ConfigEditor.LastOpenedFileSelectorDirectory;
			}
		}
		if (text == null && Schema.AllowedDirectories != null && Schema.AllowedDirectories.Length != 0)
		{
			string text4 = Schema.AllowedDirectories[0];
			if (text4 != "/")
			{
				text4 = text4.TrimEnd(new char[1] { '/' });
			}
			if (ConfigEditor.AssetEditorOverlay.Assets.TryGetDirectory(AssetPathUtils.CombinePaths("Common", text4), out var _))
			{
				text = text4;
			}
		}
		if (text == null)
		{
			text = _dropdown.CurrentPath;
		}
		_dropdown.Setup(text, GetFiles(text));
		UpdatePreview();
	}

	private List<FileSelector.File> GetFiles(string currentPath = null)
	{
		string text = _dropdown.SearchQuery.Trim();
		if (text != "" && text.Length < 3)
		{
			return new List<FileSelector.File>();
		}
		ConfigEditor.LastOpenedFileSelectorDirectory = currentPath ?? _dropdown.CurrentPath;
		return ConfigEditor.AssetEditorOverlay.GetCommonFileSelectorFiles(AssetPathUtils.CombinePaths("Common", currentPath ?? _dropdown.CurrentPath), text, Schema.AllowedFileExtensions, Schema.AllowedDirectories, 1000);
	}

	public override void Focus()
	{
		_dropdown.Open();
	}

	protected internal override void UpdateDisplayedValue()
	{
		_dropdown.SelectedFiles = ((base.Value != null) ? new HashSet<string> { (string)base.Value } : null);
	}

	protected override bool ValidateType(JToken value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)value.Type == 8;
	}
}
