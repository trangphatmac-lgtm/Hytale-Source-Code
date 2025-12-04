using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal class FileSelectorList : Element
{
	private class FileListEntry
	{
		public LabelSpanPortion[] Text;

		public TexturePatch Icon;
	}

	private class LabelSpanPortion
	{
		public readonly string Text;

		public UInt32Color Color = UInt32Color.White;

		public bool IsBold;

		public int X;

		public LabelSpanPortion(string text)
		{
			Text = text;
		}
	}

	private static readonly UInt32Color DisabledColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 80);

	private const int FontSize = 13;

	private const int RowHeight = 25;

	private readonly FileSelector _fileSelector;

	private FileListEntry[] _entries;

	private int _hoveredEntryIndex = -1;

	private int _focusedEntryIndex = -1;

	private int[] _activeEntryIndexes = new int[0];

	private FontFamily _fontFamily;

	private TexturePatch _folderIcon;

	private TexturePatch _fileIcon;

	private TexturePatch _textureIcon;

	private TexturePatch _modelIcon;

	private TexturePatch _animationIcon;

	private TexturePatch _audioIcon;

	public bool AreAllSelectedFilesInList => _activeEntryIndexes.Length == _fileSelector.SelectedFiles.Count;

	public ScrollbarStyle ScrollbarStyle
	{
		set
		{
			_scrollbarStyle = value;
		}
	}

	public FileSelectorList(Desktop desktop, Element parent, FileSelector fileSelector)
		: base(desktop, parent)
	{
		_fileSelector = fileSelector;
	}

	protected override void OnUnmounted()
	{
		_hoveredEntryIndex = -1;
		_focusedEntryIndex = -1;
		_activeEntryIndexes = new int[0];
		_entries = null;
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_folderIcon = Desktop.MakeTexturePatch(new PatchStyle("AssetEditor/AssetIcons/Folder.png")
		{
			Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 150)
		});
		_fileIcon = Desktop.MakeTexturePatch(new PatchStyle("AssetEditor/AssetIcons/File.png"));
		_textureIcon = Desktop.MakeTexturePatch(new PatchStyle("AssetEditor/AssetIcons/Texture.png"));
		_modelIcon = Desktop.MakeTexturePatch(new PatchStyle("AssetEditor/AssetIcons/Model.png"));
		_animationIcon = Desktop.MakeTexturePatch(new PatchStyle("AssetEditor/AssetIcons/Animation.png"));
		_audioIcon = Desktop.MakeTexturePatch(new PatchStyle("AssetEditor/AssetIcons/Audio.png"));
		_fontFamily = Desktop.Provider.GetFontFamily("Default");
	}

	private TexturePatch GetIcon(FileSelector.File file)
	{
		if (file.IsDirectory)
		{
			return _folderIcon;
		}
		if (file.Name.EndsWith(".png"))
		{
			return _textureIcon;
		}
		if (file.Name.EndsWith(".ogg"))
		{
			return _audioIcon;
		}
		if (file.Name.EndsWith(".blockymodel"))
		{
			return _modelIcon;
		}
		if (file.Name.EndsWith(".blockyanim"))
		{
			return _animationIcon;
		}
		return _fileIcon;
	}

	protected override void LayoutSelf()
	{
		_entries = new FileListEntry[_fileSelector.Files.Count];
		List<int> list = new List<int>();
		string text = _fileSelector.SearchQuery.ToLowerInvariant().Trim();
		string[] array = (from k in text.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
			select k.Trim()).ToArray();
		Regex regex = new Regex("(" + string.Join("|", array) + ")", RegexOptions.IgnoreCase);
		for (int i = 0; i < _fileSelector.Files.Count; i++)
		{
			FileSelector.File file = _fileSelector.Files[i];
			if (_fileSelector.SearchQuery != "")
			{
				string[] array2 = regex.Split(file.Name);
				FileListEntry fileListEntry = new FileListEntry();
				fileListEntry.Text = new LabelSpanPortion[array2.Length];
				fileListEntry.Icon = GetIcon(file);
				FileListEntry fileListEntry2 = fileListEntry;
				int num = 0;
				for (int j = 0; j < array2.Length; j++)
				{
					string text2 = array2[j];
					bool flag = array.Contains(text2.ToLowerInvariant());
					fileListEntry2.Text[j] = new LabelSpanPortion(text2)
					{
						Color = (flag ? UInt32Color.White : UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 200)),
						IsBold = flag,
						X = num
					};
					Font font = (flag ? _fontFamily.BoldFont : _fontFamily.RegularFont);
					num += (int)(font.CalculateTextWidth(text2) * 13f / (float)font.BaseSize * Desktop.Scale);
				}
				_entries[i] = fileListEntry2;
			}
			else
			{
				_entries[i] = new FileListEntry
				{
					Text = new LabelSpanPortion[1]
					{
						new LabelSpanPortion(file.Name)
						{
							Color = (_fileSelector.IsFileInAllowedDirectory(file) ? UInt32Color.White : DisabledColor)
						}
					},
					Icon = GetIcon(file)
				};
			}
			if (_fileSelector.SelectedFiles != null && _fileSelector.SelectedFiles.Contains(_fileSelector.GetFullPathOfFile(file.Name)))
			{
				list.Add(i);
			}
		}
		_activeEntryIndexes = list.ToArray();
		ContentHeight = _entries.Length * 25;
	}

	public override Element HitTest(Point position)
	{
		if (!base.Visible || !_rectangleAfterPadding.Contains(position))
		{
			return null;
		}
		return this;
	}

	private void RefreshHoveredEntry()
	{
		if (!_viewRectangle.Contains(Desktop.MousePosition))
		{
			_hoveredEntryIndex = -1;
			return;
		}
		int num = Desktop.ScaleRound(25f);
		int num2 = (int)((float)(Desktop.MousePosition.Y - _rectangleAfterPadding.Y + _scaledScrollOffset.Y) / (float)num);
		_hoveredEntryIndex = ((num2 < _entries.Length) ? num2 : (-1));
	}

	private void ScrollEntryIntoView(int index)
	{
		int num = Desktop.ScaleRound(25f);
		if (num * (index + 1) > _anchoredRectangle.Height + _scaledScrollOffset.Y)
		{
			SetScroll(0, num * (index + 1) - _anchoredRectangle.Height);
		}
		else if (num * index < _scaledScrollOffset.Y)
		{
			SetScroll(0, num * index);
		}
	}

	public void ScrollFirstActiveEntryIntoView()
	{
		if (_activeEntryIndexes.Length != 0)
		{
			ScrollEntryIntoView(_activeEntryIndexes[0]);
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (!activate || _hoveredEntryIndex == -1)
		{
			return;
		}
		FileSelector.File file = _fileSelector.Files[_hoveredEntryIndex];
		if (_fileSelector.IsFileInAllowedDirectory(file))
		{
			if (evt.Clicks == 1 || Desktop.IsShortcutKeyDown || Desktop.IsShiftKeyDown)
			{
				_focusedEntryIndex = _hoveredEntryIndex;
				_fileSelector.OnSelectFile(file);
			}
			else if (evt.Clicks == 2 && _hoveredEntryIndex == _focusedEntryIndex)
			{
				_focusedEntryIndex = -1;
				_fileSelector.OnOpenFile(file);
			}
		}
	}

	protected override void OnMouseMove()
	{
		RefreshHoveredEntry();
	}

	protected override void OnMouseEnter()
	{
		RefreshHoveredEntry();
	}

	protected override void OnMouseLeave()
	{
		_focusedEntryIndex = -1;
		RefreshHoveredEntry();
	}

	protected override void PrepareForDrawSelf()
	{
		UInt32Color color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 140);
		UInt32Color color2 = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 64);
		TextureArea whitePixel = Desktop.Provider.WhitePixel;
		int num = Desktop.ScaleRound(25f);
		int num2 = Desktop.ScaleRound(5f);
		int num3 = Desktop.ScaleRound(4f);
		Desktop.Batcher2D.PushScissor(_rectangleAfterPadding);
		for (int i = 0; i < _activeEntryIndexes.Length; i++)
		{
			int num4 = _rectangleAfterPadding.Y + num * _activeEntryIndexes[i] - _scaledScrollOffset.Y;
			if (num4 <= _rectangleAfterPadding.Bottom)
			{
				int num5 = num4 + num;
				if (num5 >= _rectangleAfterPadding.Top)
				{
					Desktop.Batcher2D.RequestDrawTexture(whitePixel.Texture, whitePixel.Rectangle, new Rectangle(_rectangleAfterPadding.X, num4, _rectangleAfterPadding.Width, num), color);
				}
			}
		}
		for (int j = 0; j < _entries.Length; j++)
		{
			FileListEntry fileListEntry = _entries[j];
			int num6 = _rectangleAfterPadding.Y + num * j - _scaledScrollOffset.Y;
			int x = _rectangleAfterPadding.X + num2;
			if (num6 > _rectangleAfterPadding.Bottom)
			{
				continue;
			}
			int num7 = num6 + num;
			if (num7 >= _rectangleAfterPadding.Top)
			{
				if (j == _hoveredEntryIndex)
				{
					Desktop.Batcher2D.RequestDrawTexture(whitePixel.Texture, whitePixel.Rectangle, new Rectangle(_rectangleAfterPadding.X, num6, _rectangleAfterPadding.Width, num), color2);
				}
				if (fileListEntry.Icon != null)
				{
					Rectangle destRect = new Rectangle(x, num6, num, num);
					Desktop.Batcher2D.RequestDrawPatch(fileListEntry.Icon, destRect, Desktop.Scale);
				}
			}
		}
		for (int k = 0; k < _entries.Length; k++)
		{
			FileListEntry fileListEntry2 = _entries[k];
			int num8 = _rectangleAfterPadding.Y + num * k - _scaledScrollOffset.Y;
			int num9 = _rectangleAfterPadding.X + num2;
			if (num8 > _rectangleAfterPadding.Bottom)
			{
				continue;
			}
			int num10 = num8 + num;
			if (num10 >= _rectangleAfterPadding.Top)
			{
				float num11 = 13f * Desktop.Scale;
				float num12 = (float)num8 + (float)num / 2f;
				LabelSpanPortion[] text = fileListEntry2.Text;
				foreach (LabelSpanPortion labelSpanPortion in text)
				{
					Font font = (labelSpanPortion.IsBold ? _fontFamily.BoldFont : _fontFamily.RegularFont);
					float num13 = num11 / (float)font.BaseSize;
					float y = num12 - (float)(int)((float)font.Height * num13 / 2f);
					Desktop.Batcher2D.RequestDrawText(font, num11, labelSpanPortion.Text, new Vector3(num9 + num + labelSpanPortion.X + num3, y, 0f), labelSpanPortion.Color);
				}
			}
		}
		Desktop.Batcher2D.PopScissor();
	}
}
