using System;
using System.Collections.Generic;
using HytaleClient.Data;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal class FileDropdownBox : Element
{
	public Action ValueChanged;

	public Action DropdownToggled;

	public FileDropdownBoxStyle Style;

	private HashSet<string> _selectedFiles;

	private readonly FileDropdownLayer _fileDropdownLayer;

	private readonly Label _label;

	private readonly Element _arrow;

	public Action<string, Action<FormattedMessage>> CreatingDirectory
	{
		set
		{
			_fileDropdownLayer.FileSelector.CreatingDirectory = value;
		}
	}

	public Action SelectingInList
	{
		set
		{
			_fileDropdownLayer.FileSelector.Selecting = value;
		}
	}

	public bool IsOpen => _fileDropdownLayer.IsMounted;

	public bool AllowMultipleFileSelection
	{
		get
		{
			return _fileDropdownLayer.FileSelector.AllowMultipleFileSelection;
		}
		set
		{
			_fileDropdownLayer.FileSelector.AllowMultipleFileSelection = value;
		}
	}

	public HashSet<string> SelectedFiles
	{
		get
		{
			return _selectedFiles;
		}
		set
		{
			_selectedFiles = value;
			_fileDropdownLayer.FileSelector.SelectedFiles = value;
			_label.Text = ((value == null || value.Count == 0) ? "" : string.Join(", ", value));
			if (base.IsMounted)
			{
				_label.Layout();
			}
		}
	}

	public HashSet<string> SelectedFilesInList => _fileDropdownLayer.FileSelector.SelectedFiles;

	public string CurrentPath => _fileDropdownLayer.FileSelector.CurrentPath;

	public string[] AllowedDirectories
	{
		get
		{
			return _fileDropdownLayer.FileSelector.AllowedDirectories;
		}
		set
		{
			_fileDropdownLayer.FileSelector.AllowedDirectories = value;
		}
	}

	public string SearchQuery => _fileDropdownLayer.FileSelector.SearchQuery;

	public bool AllowDirectorySelection
	{
		get
		{
			return _fileDropdownLayer.FileSelector.AllowDirectorySelection;
		}
		set
		{
			_fileDropdownLayer.FileSelector.AllowDirectorySelection = value;
		}
	}

	public bool IsSearchEnabled
	{
		get
		{
			return _fileDropdownLayer.FileSelector.IsSearchEnabled;
		}
		set
		{
			_fileDropdownLayer.FileSelector.IsSearchEnabled = value;
		}
	}

	public bool AllowDirectoryCreation
	{
		get
		{
			return _fileDropdownLayer.FileSelector.AllowDirectoryCreation;
		}
		set
		{
			_fileDropdownLayer.FileSelector.AllowDirectoryCreation = value;
		}
	}

	public bool SupportsUITextures
	{
		get
		{
			return _fileDropdownLayer.FileSelector.SupportsUiTextures;
		}
		set
		{
			_fileDropdownLayer.FileSelector.SupportsUiTextures = value;
		}
	}

	public FileDropdownBox(Desktop desktop, Element parent, string templatePath, Func<List<FileSelector.File>> fileGetter)
		: base(desktop, parent)
	{
		_fileDropdownLayer = new FileDropdownLayer(this, templatePath, fileGetter);
		_fileDropdownLayer.FileSelector.ActivatingSelection = delegate
		{
			_selectedFiles = new HashSet<string>();
			foreach (string selectedFile in _fileDropdownLayer.FileSelector.SelectedFiles)
			{
				if (SupportsUITextures && selectedFile.EndsWith("@2x.png"))
				{
					_selectedFiles.Add(selectedFile.Substring(0, selectedFile.Length - "@2x.png".Length) + ".png");
				}
				else
				{
					_selectedFiles.Add(selectedFile);
				}
			}
			ValueChanged?.Invoke();
			if (base.IsMounted)
			{
				HashSet<string> selectedFiles = _selectedFiles;
				_label.Text = ((selectedFiles == null || selectedFiles.Count == 0) ? "" : string.Join(", ", selectedFiles));
				_label.Layout();
				CloseDropdown();
			}
		};
		_layoutMode = LayoutMode.Left;
		_label = new Label(Desktop, this)
		{
			FlexWeight = 1
		};
		_arrow = new Element(Desktop, this);
	}

	protected override void OnUnmounted()
	{
		if (_fileDropdownLayer.IsMounted)
		{
			CloseDropdown();
		}
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void ApplyStyles()
	{
		if (IsOpen || (base.CapturedMouseButton.HasValue && (long)base.CapturedMouseButton.Value == 1))
		{
			Background = Style.PressedBackground ?? Style.HoveredBackground ?? Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = (Style.PressedArrowTexturePath ?? Style.HoveredArrowTexturePath ?? Style.DefaultArrowTexturePath)
			};
		}
		else if (base.IsHovered)
		{
			Background = Style.HoveredBackground ?? Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = (Style.HoveredArrowTexturePath ?? Style.DefaultArrowTexturePath)
			};
		}
		else
		{
			Background = Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = Style.DefaultArrowTexturePath
			};
		}
		base.ApplyStyles();
		_arrow.Anchor.Width = Style.ArrowWidth;
		_arrow.Anchor.Height = Style.ArrowHeight;
		_arrow.Anchor.Right = Style.HorizontalPadding;
		_label.Style = Style.LabelStyle ?? new LabelStyle();
		_label.Anchor.Left = (_label.Anchor.Right = Style.HorizontalPadding);
	}

	protected override void AfterChildrenLayout()
	{
		FontFamily fontFamily = Desktop.Provider.GetFontFamily(_label.Style.FontName.Value);
		Font font = (_label.Style.RenderBold ? fontFamily.BoldFont : fontFamily.RegularFont);
		string text = "";
		float num = (float)_label.RectangleAfterPadding.Width / Desktop.Scale - font.GetCharacterAdvance(8230) * _label.Style.FontSize / (float)font.BaseSize;
		string text2 = ((SelectedFiles == null || SelectedFiles.Count == 0) ? "" : string.Join(", ", SelectedFiles));
		for (int num2 = text2.Length - 1; num2 >= 0; num2--)
		{
			float num3 = font.GetCharacterAdvance(text2[num2]) * _label.Style.FontSize / (float)font.BaseSize;
			num -= num3;
			if (num <= 0f)
			{
				text = "…" + text;
				break;
			}
			text = text2[num2] + text;
			num -= _label.Style.LetterSpacing;
		}
		_label.Text = text;
		_label.Layout();
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		Layout();
		if (activate && (long)evt.Button == 1)
		{
			Open();
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		Layout();
	}

	protected override void OnMouseEnter()
	{
		Layout();
	}

	protected override void OnMouseLeave()
	{
		Layout();
	}

	internal void CloseDropdown()
	{
		Desktop.SetTransientLayer(null);
		if (base.IsMounted)
		{
			DropdownToggled?.Invoke();
			Layout();
		}
	}

	public void Open()
	{
		Desktop.SetTransientLayer(_fileDropdownLayer);
		_fileDropdownLayer.FileSelector.SelectedFiles = _selectedFiles;
		if (_fileDropdownLayer.FileSelector.IsSearchEnabled)
		{
			_fileDropdownLayer.FileSelector.FocusSearch();
		}
		DropdownToggled?.Invoke();
		_fileDropdownLayer.FileSelector.List.ScrollFirstActiveEntryIntoView();
		Layout();
	}

	public void Setup(string currentDirectory, List<FileSelector.File> files)
	{
		if (!currentDirectory.StartsWith("/"))
		{
			currentDirectory = "/" + currentDirectory;
		}
		FileSelector fileSelector = _fileDropdownLayer.FileSelector;
		fileSelector.SetCurrentPath(currentDirectory, clearForwardStack: true);
		fileSelector.Files = files;
		fileSelector.Layout();
	}

	public void SetPreviewImage(Image image)
	{
		_fileDropdownLayer.FileSelector.SetPreviewImage(image);
	}

	protected override void LayoutSelf()
	{
		base.LayoutSelf();
		if (_fileDropdownLayer.IsMounted)
		{
			_fileDropdownLayer.Layout();
		}
	}
}
