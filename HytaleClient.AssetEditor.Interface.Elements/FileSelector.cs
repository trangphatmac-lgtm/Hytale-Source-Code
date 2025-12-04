#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal class FileSelector : Element
{
	[UIMarkupData]
	public class File
	{
		public string Name;

		public bool IsDirectory;
	}

	private List<File> _files = new List<File>();

	private HashSet<string> _selectedFiles = new HashSet<string>();

	public bool AllowMultipleFileSelection;

	public bool AllowDirectorySelection = false;

	public bool SupportsUiTextures = false;

	private bool _allowDirectoryCreation;

	public string[] AllowedDirectories;

	public Action Cancelling;

	public Action ActivatingSelection;

	public Action Selecting;

	public Action<string, Action<FormattedMessage>> CreatingDirectory;

	private readonly Func<List<File>> _fileGetter;

	public bool IsSearchEnabled = true;

	private TextField _searchInput;

	private Group _currentDirectoryInfo;

	private TextButton _selectButton;

	private TextButton _createDirectoryButton;

	private TextField _createDirectoryField;

	private FileSelectorList _fileList;

	private TextButton _backButton;

	private TextButton _forwardButton;

	private Group _previewContainer;

	private Label _selectedFileLabel;

	private Label _errorLabel;

	private TextButton.TextButtonStyle _breadcrumbButtonStyle;

	private LabelStyle _breadcrumbArrowLabelStyle;

	private LabelStyle _breadcrumbCurrentLabelStyle;

	private Stack<string> _forwardStack = new Stack<string>();

	private bool _isSingleDirectoryForNavigationSelected;

	public List<File> Files
	{
		get
		{
			return _files;
		}
		set
		{
			if (_isSingleDirectoryForNavigationSelected)
			{
				_isSingleDirectoryForNavigationSelected = false;
			}
			_files = value;
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
			_selectedFiles = ((value != null) ? new HashSet<string>(value) : new HashSet<string>());
		}
	}

	public bool AllowDirectoryCreation
	{
		get
		{
			return _allowDirectoryCreation;
		}
		set
		{
			_allowDirectoryCreation = value;
			_createDirectoryButton.Visible = value;
			_createDirectoryField.Visible = value;
		}
	}

	public string SearchQuery => _searchInput.Value;

	public string CurrentPath { get; private set; } = "/";


	public FileSelectorList List => _fileList;

	public FileSelector(Desktop desktop, Element parent, string template, Func<List<File>> fileGetter)
		: base(desktop, parent)
	{
		_fileGetter = fileGetter;
		Build(template);
	}

	private void Build(string templatePath)
	{
		Desktop.Provider.TryGetDocument(templatePath, out var document);
		_breadcrumbButtonStyle = document.ResolveNamedValue<TextButton.TextButtonStyle>(Desktop.Provider, "BreadcrumbButtonStyle");
		_breadcrumbArrowLabelStyle = document.ResolveNamedValue<LabelStyle>(Desktop.Provider, "BreadcrumbArrowLabelStyle");
		_breadcrumbCurrentLabelStyle = document.ResolveNamedValue<LabelStyle>(Desktop.Provider, "BreadcrumbCurrentLabelStyle");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<TextButton>("CancelButton").Activating = delegate
		{
			Cancelling?.Invoke();
		};
		_selectButton = uIFragment.Get<TextButton>("SelectButton");
		_selectButton.Activating = OpenSelectedFile;
		_createDirectoryButton = uIFragment.Get<TextButton>("CreateDirectoryButton");
		_createDirectoryButton.Visible = AllowDirectoryCreation;
		_createDirectoryButton.Activating = OnCreateDirectoryActivating;
		_createDirectoryField = uIFragment.Get<TextField>("CreateDirectoryField");
		_createDirectoryField.Visible = AllowDirectoryCreation;
		_createDirectoryField.Validating = OnCreateDirectoryActivating;
		_fileList = new FileSelectorList(Desktop, uIFragment.Get<Group>("FileList"), this);
		if (document.TryResolveNamedValue<ScrollbarStyle>(Desktop.Provider, "ScrollbarStyle", out var value))
		{
			_fileList.ScrollbarStyle = value;
		}
		_currentDirectoryInfo = uIFragment.Get<Group>("CurrentDirectory");
		_searchInput = uIFragment.Get<TextField>("SearchInput");
		_searchInput.KeyDown = HandleCommonKeyEvent;
		_searchInput.ValueChanged = delegate
		{
			SelectedFiles = null;
			Files = _fileGetter();
			UpdateDirectoryLabel();
			Layout();
		};
		_errorLabel = uIFragment.Get<Label>("ErrorLabel");
		_errorLabel.Visible = false;
		_previewContainer = uIFragment.Get<Group>("PreviewContainer");
		_selectedFileLabel = uIFragment.Get<Label>("SelectedFileLabel");
		_backButton = uIFragment.Get<TextButton>("BackButton");
		_backButton.Activating = OnGoBack;
		_forwardButton = uIFragment.Get<TextButton>("ForwardButton");
		_forwardButton.Activating = OnGoForward;
	}

	protected override void OnMounted()
	{
		_searchInput.Value = "";
	}

	protected override void OnUnmounted()
	{
		_errorLabel.Visible = false;
		SelectedFiles = new HashSet<string>();
		_forwardStack.Clear();
		_searchInput.Value = "";
		_files.Clear();
		ClearPreview(doLayout: false);
		if (_isSingleDirectoryForNavigationSelected)
		{
			_isSingleDirectoryForNavigationSelected = false;
			SelectedFiles = null;
		}
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	private void OnCreateDirectoryActivating()
	{
		_errorLabel.Visible = false;
		if (SearchQuery != "")
		{
			return;
		}
		string text = _createDirectoryField.Value.Trim();
		if (text == "")
		{
			return;
		}
		foreach (File file in _files)
		{
			if (file.Name.ToLowerInvariant() == text.ToLowerInvariant())
			{
				_errorLabel.Text = Desktop.Provider.GetText("ui.assetEditor.errors.createDirectoryExists");
				_errorLabel.Visible = true;
				_errorLabel.Parent.Layout();
				return;
			}
		}
		if (text.Contains("/") || text.Contains("\\"))
		{
			_errorLabel.Text = Desktop.Provider.GetText("ui.assetEditor.errors.directoryInvalidCharacters");
			_errorLabel.Visible = true;
			_errorLabel.Parent.Layout();
			return;
		}
		_createDirectoryField.Value = "";
		CreatingDirectory?.Invoke(text, delegate(FormattedMessage errorMessage)
		{
			if (errorMessage != null)
			{
				_errorLabel.TextSpans = FormattedMessageConverter.GetLabelSpans(errorMessage, Desktop.Provider, new SpanStyle
				{
					Color = _errorLabel.Style.TextColor
				}, allowFormatting: false);
				_errorLabel.Visible = true;
				_errorLabel.Parent.Layout();
			}
		});
	}

	public string GetFullPathOfFile(string filename)
	{
		if (SearchQuery == "")
		{
			return (CurrentPath + "/" + filename).TrimStart(new char[1] { '/' });
		}
		return filename;
	}

	internal void SetCurrentPath(string path, bool clearForwardStack)
	{
		CurrentPath = path;
		Debug.Assert(CurrentPath.StartsWith("/"));
		Debug.Assert(CurrentPath.Length == 1 || !CurrentPath.EndsWith("/"));
		UpdateDirectoryLabel();
		if (clearForwardStack)
		{
			_forwardStack.Clear();
		}
		_backButton.Disabled = CurrentPath == "/";
		_forwardButton.Disabled = _forwardStack.Count == 0;
	}

	private void SelectNextEntry(bool invert)
	{
		if (Files.Count == 0)
		{
			return;
		}
		string text = SelectedFiles.FirstOrDefault();
		int num = (invert ? (Files.Count - 1) : 0);
		if (text != null)
		{
			for (int i = 0; i < Files.Count; i++)
			{
				File file = Files[i];
				if (!(GetFullPathOfFile(file.Name) != text))
				{
					num = (invert ? (i - 1) : (i + 1));
					if (num >= Files.Count)
					{
						num = 0;
					}
					else if (num < 0)
					{
						num = Files.Count - 1;
					}
					break;
				}
			}
		}
		SelectedFiles = new HashSet<string> { GetFullPathOfFile(Files[num].Name) };
		_isSingleDirectoryForNavigationSelected = Files[num].IsDirectory;
		ClearPreview();
		Selecting?.Invoke();
		UpdateSelectedFileLabel(doLayout: true);
		UpdateSelectButtonState(doLayout: true);
		_fileList.Layout();
		_fileList.ScrollFirstActiveEntryIntoView();
	}

	private void OnGoBack()
	{
		string[] array = CurrentPath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0)
		{
			_forwardStack.Push(array[^1]);
			_searchInput.Value = "";
			string path = "/" + string.Join("/", array.Take(array.Length - 1));
			SetCurrentPath(path, clearForwardStack: false);
			SelectedFiles = null;
			Files = _fileGetter();
			Layout();
		}
	}

	private void OnGoForward()
	{
		if (_forwardStack.Count != 0)
		{
			string path = _forwardStack.Pop();
			string path2 = AssetPathUtils.CombinePaths(CurrentPath, path);
			_searchInput.Value = "";
			SetCurrentPath(path2, clearForwardStack: false);
			SelectedFiles = null;
			Files = _fileGetter();
			Layout();
		}
	}

	private void HandleCommonKeyEvent(SDL_Keycode keyCode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		if ((int)keyCode != 13)
		{
			if ((int)keyCode != 1073741905)
			{
				if ((int)keyCode == 1073741906)
				{
					SelectNextEntry(invert: true);
				}
			}
			else
			{
				SelectNextEntry(invert: false);
			}
		}
		else
		{
			if (SelectedFiles.Count == 0)
			{
				return;
			}
			string first = SelectedFiles.First();
			File file = Files.FirstOrDefault((File sf) => GetFullPathOfFile(sf.Name) == first);
			if (IsFileInAllowedDirectory(file))
			{
				if (SelectedFiles.Count == 1 && file != null && file.IsDirectory)
				{
					OpenDirectory(first);
				}
				else
				{
					OpenSelectedFile();
				}
			}
		}
	}

	protected internal override void OnKeyUp(SDL_Keycode keyCode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		HandleCommonKeyEvent(keyCode);
		if ((int)keyCode <= 102)
		{
			if ((int)keyCode != 97)
			{
				if ((int)keyCode == 102 && Desktop.IsShortcutKeyDown && _searchInput.IsMounted)
				{
					Desktop.FocusElement(_searchInput);
				}
			}
			else
			{
				if (!Desktop.IsShortcutKeyDown || !IsDirectoryAllowed(CurrentPath))
				{
					return;
				}
				HashSet<string> hashSet = new HashSet<string>();
				foreach (File file in Files)
				{
					if (AllowDirectorySelection || !file.IsDirectory)
					{
						hashSet.Add(GetFullPathOfFile(file.Name));
					}
				}
				if (SelectedFiles != null && SelectedFiles.SequenceEqual(hashSet))
				{
					SelectedFiles = null;
				}
				else
				{
					SelectedFiles = hashSet;
				}
				_fileList.Layout();
			}
		}
		else if ((int)keyCode != 1073741903)
		{
			if ((int)keyCode == 1073741904)
			{
				OnGoBack();
			}
		}
		else
		{
			OnGoForward();
		}
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		UpdateSelectButtonState();
		_searchInput.Visible = IsSearchEnabled;
	}

	internal bool IsFileInAllowedDirectory(File file)
	{
		if (AllowedDirectories != null)
		{
			if (file.IsDirectory)
			{
				if (IsDirectoryAllowed(CurrentPath))
				{
					return true;
				}
				string value = AssetPathUtils.CombinePaths(CurrentPath, file.Name) + "/";
				string[] allowedDirectories = AllowedDirectories;
				foreach (string text in allowedDirectories)
				{
					if (text.StartsWith(value))
					{
						return true;
					}
				}
				return false;
			}
			if (!IsDirectoryAllowed(CurrentPath))
			{
				return false;
			}
		}
		return true;
	}

	public void SetPreviewImage(Image image)
	{
		Debug.Assert(base.IsMounted);
		if (base.IsMounted)
		{
			_previewContainer.Background?.TextureArea.Texture.Dispose();
			TextureArea textureArea = ExternalTextureLoader.FromImage(image);
			_previewContainer.Background = new PatchStyle(textureArea);
			_previewContainer.Anchor.Width = image.Width;
			_previewContainer.Anchor.Height = image.Height;
			int num = Desktop.UnscaleRound(_previewContainer.Parent.RectangleAfterPadding.Width);
			int num2 = Desktop.UnscaleRound(_previewContainer.Parent.RectangleAfterPadding.Height);
			if (_previewContainer.Anchor.Width > num)
			{
				_previewContainer.Anchor.Height = (int)((float)_previewContainer.Anchor.Height.Value / (float?)_previewContainer.Anchor.Width * (float)num).Value;
				_previewContainer.Anchor.Width = num;
			}
			if (_previewContainer.Anchor.Height > num2)
			{
				_previewContainer.Anchor.Width = (int)((float)_previewContainer.Anchor.Width.Value / (float?)_previewContainer.Anchor.Height * (float)num2).Value;
				_previewContainer.Anchor.Width = num2;
			}
			_previewContainer.Parent.Layout();
		}
	}

	private void ClearPreview(bool doLayout = true)
	{
		_previewContainer.Background?.TextureArea.Texture.Dispose();
		_previewContainer.Background = null;
		if (doLayout)
		{
			_previewContainer.Parent.Layout();
		}
	}

	private void UpdateSelectedFileLabel(bool doLayout)
	{
		if (SelectedFiles.Count == 0)
		{
			_selectedFileLabel.Text = "";
		}
		else if (SelectedFiles.Count == 1)
		{
			_selectedFileLabel.Text = SelectedFiles.First();
		}
		else
		{
			_selectedFileLabel.Text = Desktop.Provider.GetText("ui.assetEditor.fileSelector.filesSelected", new Dictionary<string, string> { 
			{
				"count",
				Desktop.Provider.FormatNumber(SelectedFiles.Count)
			} });
		}
		if (doLayout)
		{
			_selectedFileLabel.Layout();
		}
	}

	public void OnSelectFile(File file)
	{
		string fullPathOfFile = GetFullPathOfFile(file.Name);
		if (AllowMultipleFileSelection && Desktop.IsShiftKeyDown)
		{
			File[] array = Files.ToArray();
			string[] visibleFileNames = array.Select((File f) => GetFullPathOfFile(f.Name)).ToArray();
			IEnumerable<string> enumerable = new string[1] { fullPathOfFile };
			if (SelectedFiles != null && _fileList.AreAllSelectedFilesInList)
			{
				enumerable = enumerable.Concat(SelectedFiles);
			}
			int num = int.MaxValue;
			int num2 = 0;
			foreach (int item in enumerable.Select((string f) => Array.IndexOf(visibleFileNames, f)))
			{
				if (item < num)
				{
					num = item;
				}
				if (item > num2)
				{
					num2 = item;
				}
			}
			if (num == -1 && num2 == -1)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>();
			for (int i = num; i <= num2; i++)
			{
				if (AllowDirectorySelection || !array[i].IsDirectory)
				{
					hashSet.Add(GetFullPathOfFile(array[i].Name));
				}
			}
			ClearPreview();
			SelectedFiles = hashSet;
			Selecting?.Invoke();
		}
		else if (AllowMultipleFileSelection && Desktop.IsShortcutKeyDown)
		{
			if (file.IsDirectory && !AllowDirectorySelection)
			{
				return;
			}
			ClearPreview();
			if (SelectedFiles != null && _fileList.AreAllSelectedFilesInList)
			{
				if (SelectedFiles.Contains(fullPathOfFile))
				{
					if (SelectedFiles.Count == 1)
					{
						SelectedFiles = null;
					}
					else
					{
						SelectedFiles.Remove(fullPathOfFile);
					}
				}
				else
				{
					SelectedFiles.Add(fullPathOfFile);
				}
			}
			else
			{
				SelectedFiles = new HashSet<string> { fullPathOfFile };
			}
			Selecting?.Invoke();
		}
		else
		{
			if (file.IsDirectory && !AllowDirectorySelection)
			{
				return;
			}
			ClearPreview();
			if (SelectedFiles != null && SelectedFiles.Count > 0 && SelectedFiles.First() == fullPathOfFile)
			{
				SelectedFiles = null;
			}
			else
			{
				SelectedFiles = new HashSet<string> { fullPathOfFile };
			}
			Selecting?.Invoke();
		}
		UpdateSelectedFileLabel(doLayout: true);
		UpdateSelectButtonState(doLayout: true);
		_fileList.Layout();
	}

	public void OnOpenFile(File file)
	{
		if (file.IsDirectory)
		{
			OpenDirectory(GetFullPathOfFile(file.Name));
			return;
		}
		SelectedFiles = new HashSet<string> { GetFullPathOfFile(file.Name) };
		UpdateSelectButtonState(doLayout: true);
		OpenSelectedFile();
	}

	private bool IsDirectoryAllowed(string directoryToCheck)
	{
		if (!directoryToCheck.EndsWith("/"))
		{
			directoryToCheck += "/";
		}
		string[] allowedDirectories = AllowedDirectories;
		foreach (string value in allowedDirectories)
		{
			if (directoryToCheck.StartsWith(value))
			{
				return true;
			}
		}
		return false;
	}

	private void OpenSelectedFile()
	{
		if (_selectButton.Disabled)
		{
			return;
		}
		if (!AllowDirectorySelection)
		{
			foreach (string file in SelectedFiles)
			{
				if (Files.First((File f) => GetFullPathOfFile(f.Name) == file).IsDirectory)
				{
					return;
				}
			}
		}
		_forwardStack.Clear();
		if (SelectedFiles.Count == 0)
		{
			_selectedFiles.Add(CurrentPath.Trim(new char[1] { '/' }));
		}
		ActivatingSelection?.Invoke();
	}

	private void OpenDirectory(string path)
	{
		_searchInput.Value = "";
		SetCurrentPath("/" + path, clearForwardStack: true);
		SelectedFiles = null;
		Files = _fileGetter();
		Layout();
	}

	private void UpdateDirectoryLabel()
	{
		_currentDirectoryInfo.Clear();
		if (SearchQuery == "")
		{
			string[] array = CurrentPath.Trim(new char[1] { '/' }).Split(new char[1] { '/' });
			string text2 = array[^1];
			string text3 = "";
			if (text2 != "")
			{
				CreateButton("Root", text3);
			}
			for (int i = 0; i < array.Length - 1; i++)
			{
				text3 += array[i];
				CreateButton(array[i], text3);
				text3 += "/";
			}
			new Label(Desktop, _currentDirectoryInfo)
			{
				Text = ((text2 == "") ? "Root" : text2),
				Anchor = new Anchor
				{
					Horizontal = 5
				},
				Style = _breadcrumbCurrentLabelStyle
			};
		}
		else
		{
			new Label(Desktop, _currentDirectoryInfo)
			{
				Text = "Search \"" + SearchQuery + "\"",
				Style = _breadcrumbCurrentLabelStyle
			};
		}
		void CreateButton(string text, string path)
		{
			new TextButton(Desktop, _currentDirectoryInfo)
			{
				Text = text,
				Anchor = new Anchor
				{
					Horizontal = 2
				},
				Padding = new Padding
				{
					Horizontal = 3
				},
				Activating = delegate
				{
					OpenDirectory(path);
				},
				Style = _breadcrumbButtonStyle
			};
			new Label(Desktop, _currentDirectoryInfo)
			{
				Text = ">",
				Style = _breadcrumbArrowLabelStyle
			};
		}
	}

	private void UpdateSelectButtonState(bool doLayout = false)
	{
		_createDirectoryButton.Disabled = SearchQuery != "";
		if (doLayout)
		{
			_createDirectoryButton.Layout();
		}
		HashSet<string> selectedFiles = SelectedFiles;
		if (selectedFiles.Count == 0 || _isSingleDirectoryForNavigationSelected)
		{
			_selectButton.Disabled = !AllowDirectorySelection;
			if (doLayout)
			{
				_selectButton.Layout();
			}
			return;
		}
		if (AllowedDirectories != null)
		{
			foreach (string item in selectedFiles)
			{
				bool flag = false;
				string[] allowedDirectories = AllowedDirectories;
				foreach (string text in allowedDirectories)
				{
					if (item.StartsWith(text.TrimStart(new char[1] { '/' })))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					_selectButton.Disabled = true;
					if (doLayout)
					{
						_selectButton.Layout();
					}
					return;
				}
			}
		}
		_selectButton.Disabled = false;
		if (doLayout)
		{
			_selectButton.Layout();
		}
	}

	public void FocusSearch()
	{
		Desktop.FocusElement(_searchInput);
	}
}
