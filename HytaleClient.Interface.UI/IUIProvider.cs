using System;
using System.Collections.Generic;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI;

public interface IUIProvider
{
	TextureArea WhitePixel { get; }

	TextureArea MissingTexture { get; }

	TextureArea MakeTextureArea(string path);

	bool TryGetDocument(string path, out Document document);

	FontFamily GetFontFamily(string name);

	string GetText(string key, Dictionary<string, string> parameters = null, bool returnFallback = true);

	string FormatNumber(int value);

	string FormatNumber(float value);

	string FormatNumber(double value);

	string FormatRelativeTime(DateTime time);

	void PlaySound(SoundStyle sound);
}
