#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Coherent.UI;
using HytaleClient.Core;
using HytaleClient.Interface.CoherentUI.Internals;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Interface.CoherentUI;

internal class CoUIManager : Disposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string LicenseKey = "AAEAADx/j4VGlJfs90rFyPnshqewq2iW5BFkrivHU6Uf/E16e/vUMH8b09vmf8stulr41/KrbNahUElWMGZneOz8zjIkGIm7EhTLwApzOtH0ukwbDGkLqGOIUw7SB5MtUdGiefYIrH6rPt1yGaPaMBZFbKlXXwx6uQwDPy/HUyZupyTgfdFquzjLFYAN51ZhabzaHKew+LafEgPt69DhMKBBfjp/pq7KM8gEsAsk9nUH31eSROuP9hFLOV1+O/SpFHtrKQ1PoWk5eA7pODReLSKUzHJdNoQiyCTWwJERCC44YtJyEREjQs8YpzCyQMA8QcKKWjWpdTwvYEQcMU24QjRB+zMBK9oAYDWAi6oAPKIMOLJXtE+OtBkhInmMron8uSxF8hwjYgvuN6wgK53mXjwODd3VzJ+2p7iGX4DsQwYkPvT0mcVe19JouczZrBelXNibpJ993+RCBb3Z/1fFYepyM2SI7MTjuQ4HVW+zH+Im89vz92bcWzZTy3bUZ7rLSWo42/zxIeR2RKSuO29dcg3ZjaebcmMXh6H6xvbQwO0I5yQdJcFF1bbDzTAJWlFyGKQuWenIpRaq6O07zii/1rjsd8eusEUm35OYulqGqwMAGZwy1ZUmtzTg6GSm8FaILSLq8bA8KJh8bDSacrAwePpiSqT+qjtYzJOOXgcKSjuXcLSf";

	public readonly TextureBufferHelper TextureBufferHelper;

	private readonly Engine _engine;

	private readonly ViewContextFactory _contextFactory;

	private readonly CancellationTokenSource _threadCancellationTokenSource = new CancellationTokenSource();

	private readonly CancellationToken _threadCancellationToken;

	private readonly BlockingCollection<Action> _threadActions;

	private readonly CoUIFileHandler _fileHandler;

	private readonly CoUIContextListener _contextListener;

	private readonly ViewContext _viewContext;

	private bool _viewContextReady;

	private readonly List<WebView> _webViews = new List<WebView>();

	private readonly Thread _thread;

	public WebView FocusedWebView { get; private set; }

	public bool IsInitialized => _viewContext != null;

	public void SetFocusedWebView(WebView webView)
	{
		FocusedWebView = webView;
		if (webView != null)
		{
			RunInThread(webView.SetFocus);
		}
	}

	public CoUIManager(Engine engine, CoUIFileHandler fileHandler)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		_engine = engine;
		if (!CoherentUI_Native.CheckUISDKVersionCompatible(Versioning.SDKVersion))
		{
			Logger.Warn("SDK versions for .NET and native DLLs are not compatible.");
		}
		else if (!CoherentUI_Native.CheckUISDKVersionExact(Versioning.SDKVersion))
		{
			Logger.Warn("SDK versions for .NET and native DLLs are compatible but different.");
		}
		_threadCancellationToken = _threadCancellationTokenSource.Token;
		_threadActions = new BlockingCollection<Action>();
		TextureBufferHelper = new TextureBufferHelper();
		FactorySettings val = new FactorySettings
		{
			EnableSupportForProprietaryCodecs = true
		};
		_contextFactory = CoherentUI_Native.InitializeCoherentUI(Versioning.SDKVersion, "AAEAADx/j4VGlJfs90rFyPnshqewq2iW5BFkrivHU6Uf/E16e/vUMH8b09vmf8stulr41/KrbNahUElWMGZneOz8zjIkGIm7EhTLwApzOtH0ukwbDGkLqGOIUw7SB5MtUdGiefYIrH6rPt1yGaPaMBZFbKlXXwx6uQwDPy/HUyZupyTgfdFquzjLFYAN51ZhabzaHKew+LafEgPt69DhMKBBfjp/pq7KM8gEsAsk9nUH31eSROuP9hFLOV1+O/SpFHtrKQ1PoWk5eA7pODReLSKUzHJdNoQiyCTWwJERCC44YtJyEREjQs8YpzCyQMA8QcKKWjWpdTwvYEQcMU24QjRB+zMBK9oAYDWAi6oAPKIMOLJXtE+OtBkhInmMron8uSxF8hwjYgvuN6wgK53mXjwODd3VzJ+2p7iGX4DsQwYkPvT0mcVe19JouczZrBelXNibpJ993+RCBb3Z/1fFYepyM2SI7MTjuQ4HVW+zH+Im89vz92bcWzZTy3bUZ7rLSWo42/zxIeR2RKSuO29dcg3ZjaebcmMXh6H6xvbQwO0I5yQdJcFF1bbDzTAJWlFyGKQuWenIpRaq6O07zii/1rjsd8eusEUm35OYulqGqwMAGZwy1ZUmtzTg6GSm8FaILSLq8bA8KJh8bDSacrAwePpiSqT+qjtYzJOOXgcKSjuXcLSf", (FactorySettingsBase)(object)val, new RenderingParameters(), (Severity)3, (ILogHandler)null);
		ContextSettings val2 = new ContextSettings
		{
			DebuggerPort = 9999,
			DisableWebSecurity = true
		};
		_fileHandler = fileHandler;
		_contextListener = new CoUIContextListener(OnContextReady);
		_viewContext = _contextFactory.CreateViewContext((ContextSettingsBase)(object)val2, (ContextListener)(object)_contextListener, (FileHandler)(object)_fileHandler);
		if (_viewContext == null)
		{
			Logger.Warn("Could not create CoherentUI view context, Web views will not be available.");
			return;
		}
		_thread = new Thread(CoherentUIManagerThreadStart)
		{
			Name = "CoherentUIManager",
			IsBackground = true
		};
		_thread.Start();
	}

	public void RegisterWebView(WebView webView)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(CoUIManager).FullName);
		}
		_webViews.Add(webView);
		if (_viewContextReady)
		{
			webView.Initialize(_viewContext);
		}
	}

	public void RunInThread(Action action)
	{
		if (_thread != null)
		{
			Debug.Assert(!ThreadHelper.IsOnThread(_thread));
			if (base.Disposed)
			{
				throw new ObjectDisposedException(typeof(CoUIManager).FullName);
			}
			_threadActions.Add(action);
		}
	}

	public void Update()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(CoUIManager).FullName);
		}
		bool fetchSurfaces = _engine.Window.GetState() != Window.WindowState.Minimized;
		RunInThread(delegate
		{
			_viewContext.Update();
			if (fetchSurfaces)
			{
				_viewContext.FetchSurfaces();
			}
		});
	}

	private void CoherentUIManagerThreadStart()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(CoUIManager).FullName);
		}
		while (true)
		{
			CancellationToken threadCancellationToken = _threadCancellationToken;
			if (threadCancellationToken.IsCancellationRequested)
			{
				break;
			}
			Action action;
			try
			{
				action = _threadActions.Take(_threadCancellationToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			action();
		}
		foreach (WebView webView in _webViews)
		{
			webView.Destroy();
		}
	}

	private void OnContextReady()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(CoUIManager).FullName);
		}
		_viewContextReady = true;
		foreach (WebView webView in _webViews)
		{
			webView.Initialize(_viewContext);
		}
	}

	protected override void DoDispose()
	{
		_threadCancellationTokenSource.Cancel();
		_thread?.Join();
		_threadCancellationTokenSource.Dispose();
		foreach (WebView webView in _webViews)
		{
			webView.Dispose();
		}
		if (_viewContext != null)
		{
			_viewContext.Uninitialize();
			_viewContext.Dispose();
		}
		((ContextListener)_contextListener).Dispose();
		((FileHandler)_fileHandler).Dispose();
		_contextFactory.Destroy();
		_contextFactory.Dispose();
	}
}
