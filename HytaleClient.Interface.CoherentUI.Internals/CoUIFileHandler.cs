#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Coherent.UI;
using HytaleClient.Core;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Interface.CoherentUI.Internals;

internal class CoUIFileHandler : FileHandler
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static readonly char[] FilePathEndMarkers = new char[2] { '?', '#' };

	private const string BuiltInAssetsPrefix = "coui://builtin-assets/";

	private const string InterfacePrefix = "coui://interface/";

	private const string MonacoEditorPrefix = "coui://monaco-editor/";

	private const string WorldPreviewsPrefix = "coui://world-previews/";

	private static readonly IDictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ ".html", "text/html" },
		{ ".js", "text/javascript" },
		{ ".jsx", "text/jsx" },
		{ ".json", "application/json" },
		{ ".ogg", "audio/ogg" },
		{ ".css", "text/css" },
		{ ".png", "image/png" },
		{ ".ttf", "application/x-font-ttf" },
		{ ".otf", "application/x-font-opentype" },
		{ ".woff", "application/font-woff" },
		{ ".svg", "image/svg+xml" }
	};

	protected virtual byte[] GetFile(string filePath)
	{
		if (filePath.StartsWith("coui://builtin-assets/"))
		{
			try
			{
				return AssetManager.GetBuiltInAsset(filePath.Substring("coui://builtin-assets/".Length));
			}
			catch (FileNotFoundException ex)
			{
				Logger.Error((Exception)ex, "UI requested built-in file which doesn't exist \"{0}\", {1}", new object[2] { ex.FileName, filePath });
			}
		}
		else if (filePath.StartsWith("coui://interface/"))
		{
			filePath = filePath.Substring("coui://interface/".Length);
			try
			{
				return File.ReadAllBytes(Path.Combine(Paths.CoherentUI, filePath));
			}
			catch
			{
			}
		}
		else if (filePath.StartsWith("coui://monaco-editor/"))
		{
			filePath = filePath.Substring("coui://monaco-editor/".Length);
			try
			{
				return File.ReadAllBytes(Path.Combine(Paths.MonacoEditor, filePath));
			}
			catch
			{
			}
		}
		else if (filePath.StartsWith("coui://world-previews/"))
		{
			filePath = filePath.Substring("coui://world-previews/".Length);
			string path = Uri.UnescapeDataString(filePath.Substring(0, filePath.Length - ".png".Length)).Replace("/", string.Empty).Replace("\\", string.Empty);
			return File.ReadAllBytes(Path.Combine(Paths.Saves, path, "preview.png"));
		}
		return null;
	}

	public override void ReadFile(string url, URLRequestBase request, ResourceResponse response)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		int num = url.LastIndexOfAny(FilePathEndMarkers);
		string text = ((num > -1) ? url.Substring(0, num) : url);
		byte[] file = GetFile(text);
		if (file == null)
		{
			response.SignalFailure();
			return;
		}
		IntPtr buffer = response.GetBuffer((uint)file.Length);
		Marshal.Copy(file, 0, buffer, file.Length);
		SetResponseHeaders(response, GetMimeType(Path.GetExtension(text)), file.Length);
		response.SignalSuccess();
	}

	public override void WriteFile(string url, ResourceData resource)
	{
		resource.SignalFailure();
	}

	private static void SetResponseHeaders(ResourceResponse response, string contentType, int size)
	{
		response.SetResponseHeader("Content-Type", contentType);
		response.SetResponseHeader("Cache-Control", "max-age=86400");
		response.SetResponseHeader("Content-Length", size.ToString());
	}

	private static string GetMimeType(string extension)
	{
		string value;
		return MimeTypes.TryGetValue(extension, out value) ? value : "application/octet-stream";
	}
}
