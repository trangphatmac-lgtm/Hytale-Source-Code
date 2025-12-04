#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal class FileDropdownLayer : Element
{
	public readonly FileSelector FileSelector;

	private FileDropdownBox _fileDropdownBox;

	public FileDropdownLayer(FileDropdownBox dropdownBox, string templatePath, Func<List<FileSelector.File>> fileGetter)
		: base(dropdownBox.Desktop, null)
	{
		_fileDropdownBox = dropdownBox;
		FileSelector = new FileSelector(dropdownBox.Desktop, this, templatePath, fileGetter)
		{
			Cancelling = dropdownBox.CloseDropdown
		};
	}

	protected override void OnUnmounted()
	{
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		int num = Desktop.UnscaleRound(_fileDropdownBox.AnchoredRectangle.X);
		int num2 = Desktop.UnscaleRound(_fileDropdownBox.AnchoredRectangle.Top);
		int num3 = Desktop.UnscaleRound(_fileDropdownBox.AnchoredRectangle.Width);
		int num4 = Desktop.UnscaleRound(_fileDropdownBox.AnchoredRectangle.Height);
		FileSelector fileSelector = FileSelector;
		fileSelector.Anchor.Width = fileSelector.Children[0].Anchor.Width;
		fileSelector.Anchor.Height = fileSelector.Children[0].Anchor.Height;
		int num5 = Desktop.UnscaleRound(Desktop.ViewportRectangle.Height);
		int num6 = Desktop.UnscaleRound(Desktop.ViewportRectangle.Width);
		FileDropdownBoxStyle style = _fileDropdownBox.Style;
		fileSelector.Anchor.Top = num2 + num4 + style.PanelOffset;
		if (fileSelector.Anchor.Top + fileSelector.Anchor.Height > num5)
		{
			fileSelector.Anchor.Top = System.Math.Max((num2 - fileSelector.Anchor.Height - style.PanelOffset).Value, 0);
		}
		fileSelector.Anchor.Left = num;
		if (fileSelector.Anchor.Left + fileSelector.Anchor.Width > num6)
		{
			fileSelector.Anchor.Left = num + num3 - fileSelector.Anchor.Width;
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

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (activate && (long)evt.Button == 1)
		{
			_fileDropdownBox.CloseDropdown();
		}
	}

	protected internal override void OnKeyUp(SDL_Keycode keycode)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		FileSelector.OnKeyUp(keycode);
	}

	protected internal override void Dismiss()
	{
		_fileDropdownBox.CloseDropdown();
	}
}
