using System;
using Coherent.UI;
using NLog;

namespace HytaleClient.Interface.CoherentUI.Internals;

internal class CoUIContextListener : ContextListener
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly Action _onContextReady;

	public CoUIContextListener(Action onContextReady)
	{
		_onContextReady = onContextReady;
	}

	public override void ContextReady()
	{
		Logger.Info("CoUIContextListener.ContextReady", "CoherentUI");
		_onContextReady();
	}

	public override void OnError(ContextError contextError)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Logger.Info<string, ContextErrorType>("CoUIContextListener.OnError: {0} (#{1})", contextError.Error, contextError.ErrorCode);
	}
}
