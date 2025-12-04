using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Utils;

namespace HytaleClient.Interface.Settings;

internal class FolderSelectorDropdownComponent : SettingComponent<string>
{
	private readonly FileDropdownBox _fileDropdownBox;

	public FolderSelectorDropdownComponent(Desktop desktop, Group parent, string name, ISettingView settings)
		: base(desktop, parent, name, settings)
	{
		Document doc;
		UIFragment uIFragment = Build("FileSelectorDropdownSetting.ui", out doc);
		Group parent2 = uIFragment.Get<Group>("FileSelectorContainer");
		UIPath uIPath = doc.ResolveNamedValue<UIPath>(Desktop.Provider, "FileDropdownTemplate");
		_fileDropdownBox = new FileDropdownBox(Desktop, parent2, uIPath.Value, () => GetFiles(_fileDropdownBox.CurrentPath));
		_fileDropdownBox.Style = doc.ResolveNamedValue<FileDropdownBoxStyle>(Desktop.Provider, "FileDropdownBoxStyle");
		_fileDropdownBox.AllowDirectorySelection = true;
		_fileDropdownBox.IsSearchEnabled = false;
		_fileDropdownBox.ValueChanged = delegate
		{
			string text2 = NormalizeDropdownSelection(_fileDropdownBox.SelectedFiles?.FirstOrDefault());
			SetValue(text2);
			OnChange(text2);
		};
		_fileDropdownBox.DropdownToggled = delegate
		{
			if (_fileDropdownBox.IsOpen)
			{
				string file = _fileDropdownBox.SelectedFiles?.FirstOrDefault();
				string text = NormalizePath(file);
				_fileDropdownBox.Setup(text, GetFiles(text));
			}
		};
	}

	public override void SetValue(string value)
	{
		value = UnixPathUtil.ConvertToUnixPath(value);
		_fileDropdownBox.SelectedFiles = new HashSet<string> { value?.Trim(new char[1] { '/' }) };
	}

	private static string NormalizeDropdownSelection(string path)
	{
		if (BuildInfo.Platform == Platform.Windows && path == string.Empty)
		{
			path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		}
		else if (BuildInfo.Platform != 0 && path != null)
		{
			path = "/" + path;
		}
		return path;
	}

	private static string NormalizePath(string file)
	{
		string text = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		if (BuildInfo.Platform == Platform.Windows)
		{
			if (file != null && Directory.Exists(file))
			{
				text = Path.GetDirectoryName(file)?.TrimEnd(new char[1] { Path.DirectorySeparatorChar }) ?? "";
			}
			text = UnixPathUtil.ConvertToUnixPath(text);
		}
		else if (file != null && Directory.Exists("/" + file))
		{
			text = Path.GetDirectoryName("/" + file) ?? '/'.ToString();
		}
		return text;
	}

	private static List<FileSelector.File> GetFiles(string path)
	{
		if (BuildInfo.Platform == Platform.Windows)
		{
			path = path.TrimStart(new char[1] { '/' }) + "/";
		}
		List<FileSelector.File> list = new List<FileSelector.File>();
		try
		{
			if (BuildInfo.Platform == Platform.Windows && path == '/'.ToString())
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo driveInfo in drives)
				{
					list.Add(new FileSelector.File
					{
						Name = driveInfo.Name.TrimEnd(new char[1] { '\\' }),
						IsDirectory = true
					});
				}
			}
			else
			{
				path = Path.GetFullPath(path);
				string[] directories = Directory.GetDirectories(path);
				foreach (string path2 in directories)
				{
					if ((File.GetAttributes(path2) & FileAttributes.Hidden) != FileAttributes.Hidden)
					{
						list.Add(new FileSelector.File
						{
							Name = Path.GetFileName(path2),
							IsDirectory = true
						});
					}
				}
			}
		}
		catch (IOException ex)
		{
			Interface.Logger.Error((Exception)ex, "Failed to fetch files for component in {0}", new object[1] { path });
		}
		list.Sort((FileSelector.File a, FileSelector.File b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
		return list;
	}
}
