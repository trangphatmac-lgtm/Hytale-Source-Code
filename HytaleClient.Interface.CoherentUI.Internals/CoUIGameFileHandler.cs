using System;
using System.IO;
using HytaleClient.Application;
using NLog;

namespace HytaleClient.Interface.CoherentUI.Internals;

internal class CoUIGameFileHandler : CoUIFileHandler
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string AssetsPrefix = "coui://assets/";

	private readonly App _app;

	public CoUIGameFileHandler(App app)
	{
		_app = app;
	}

	protected override byte[] GetFile(string filePath)
	{
		if (filePath.StartsWith("coui://assets/"))
		{
			filePath = filePath.Substring("coui://assets/".Length);
			if (_app.Stage == App.AppStage.InGame)
			{
				try
				{
					return _app.InGame.GetAsset(filePath);
				}
				catch (FileNotFoundException ex)
				{
					Logger.Error((Exception)ex, "UI requested asset file which doesn't exist \"{0}\", {1}", new object[2] { ex.FileName, filePath });
				}
			}
		}
		return base.GetFile(filePath);
	}
}
