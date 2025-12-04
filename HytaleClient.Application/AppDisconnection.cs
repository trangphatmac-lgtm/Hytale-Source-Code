namespace HytaleClient.Application;

internal class AppDisconnection
{
	private readonly App _app;

	private string _hostname;

	private int _port;

	public string ExceptionMessage { get; private set; }

	public string Reason { get; private set; }

	public bool DisconnectedOnLoadingScreen { get; private set; }

	public void SetReason(string reason)
	{
		Reason = reason;
	}

	public AppDisconnection(App app)
	{
		_app = app;
	}

	public void Open(string exceptionMessage, string hostname = null, int port = 0)
	{
		ExceptionMessage = exceptionMessage;
		DisconnectedOnLoadingScreen = _app.Stage == App.AppStage.GameLoading;
		_hostname = hostname;
		_port = port;
		_app.SetStage(App.AppStage.Disconnection);
	}

	public void CleanUp()
	{
		Reason = null;
		ExceptionMessage = null;
		_hostname = null;
		_port = 0;
	}

	public void Reconnect()
	{
		if (_app.SingleplayerWorldName != null)
		{
			_app.GameLoading.Open(_app.SingleplayerWorldName);
		}
		else
		{
			_app.GameLoading.Open(_hostname, _port);
		}
	}
}
