#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Coherent.UI;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Interface.CoherentUI.Internals;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.Interface.CoherentUI;

internal class WebView : Disposable
{
	private class WebViewEventHandler
	{
		public Delegate Action;

		public BoundEventHandle? CoherentHandle;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly CoUIViewListener _viewListener;

	private CoUIViewDebugWrapper _view;

	private readonly Engine _engine;

	private readonly CoUIManager _coUiManager;

	private readonly object _coherentMemoryLock = new object();

	private int _coherentMemoryHandleValue;

	private IntPtr _coherentMemorySharedPointer;

	private int _coherentMemoryWidth;

	private int _coherentMemoryHeight;

	private bool _textureNeedsUpdate;

	private readonly List<Tuple<string, object, object, object, object, object>> _eventQueue = new List<Tuple<string, object, object, object, object, object>>();

	private readonly Dictionary<string, WebViewEventHandler> _eventHandlers = new Dictionary<string, WebViewEventHandler>();

	public bool IsReady { get; private set; }

	public bool IsReloading { get; private set; }

	public string URL { get; private set; }

	public int Width { get; private set; }

	public int Height { get; private set; }

	public float Scale { get; private set; }

	public WebView(Engine engine, CoUIManager coUiManager, string url, int width, int height, float scale)
	{
		_engine = engine;
		_coUiManager = coUiManager;
		URL = url;
		Width = width;
		Height = height;
		Scale = scale;
		_viewListener = new CoUIViewListener(this, coUiManager);
		coUiManager.RegisterWebView(this);
	}

	public void Destroy()
	{
		_view?.Destroy();
		_view = null;
	}

	protected override void DoDispose()
	{
		if (_view != null)
		{
			throw new Exception("WebView must be destroyed from the CoUIManager thread before being disposed.");
		}
		if (_eventHandlers.Count <= 0)
		{
			return;
		}
		foreach (string key in _eventHandlers.Keys)
		{
			Logger.Info("Left-over event handler for: {0}", key);
		}
		throw new Exception("Found " + _eventHandlers.Count + " left-over event handlers while disposing WebView.");
	}

	public void SetFocus()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		if (_view != null)
		{
			_view.SetFocus();
		}
	}

	public void Resize(int width, int height, float scale)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		Width = width;
		Height = height;
		Scale = scale;
		lock (_coherentMemoryLock)
		{
			_coherentMemoryHandleValue = 0;
			_coherentMemorySharedPointer = IntPtr.Zero;
		}
		if (_view != null)
		{
			_coUiManager.RunInThread(delegate
			{
				_view.Resize((uint)Width, (uint)Height);
				_view.SetZoomLevel(System.Math.Log(Scale, 1.2));
				_view.Redraw();
			});
		}
	}

	public bool IsResizing()
	{
		lock (_coherentMemoryLock)
		{
			return _coherentMemorySharedPointer == IntPtr.Zero;
		}
	}

	public void RenderToTexture()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		if (!_textureNeedsUpdate)
		{
			return;
		}
		lock (_coherentMemoryLock)
		{
			IntPtr coherentMemorySharedPointer = _coherentMemorySharedPointer;
			if (coherentMemorySharedPointer != IntPtr.Zero)
			{
				_engine.Graphics.GL.TexImage2D(GL.TEXTURE_2D, 0, 6408, _coherentMemoryWidth, _coherentMemoryHeight, 0, GL.BGRA, GL.UNSIGNED_BYTE, coherentMemorySharedPointer);
				_textureNeedsUpdate = false;
			}
		}
	}

	public void Reload()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		IsReady = false;
		IsReloading = true;
		_coUiManager.RunInThread(delegate
		{
			_view.Reload(ignoreCache: true);
		});
	}

	public void SetVolume(double volume)
	{
		if (_view != null)
		{
			_coUiManager.RunInThread(delegate
			{
				_view.SetMasterVolume(volume);
			});
		}
	}

	public void LoadURL(string url)
	{
		if (_view != null && !(url == URL))
		{
			URL = url;
			_coUiManager.RunInThread(delegate
			{
				_view.Load(URL);
			});
		}
	}

	public void RegisterForEvent(string name, Disposable disposeGate, Action action)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		lock (_eventHandlers)
		{
			if (_eventHandlers.ContainsKey(name))
			{
				throw new Exception("There's already an event handler registered for " + name);
			}
			Action runActionOnMainThread = delegate
			{
				_engine.RunOnMainThread(disposeGate, action);
			};
			WebViewEventHandler webViewEventHandler2 = (_eventHandlers[name] = new WebViewEventHandler
			{
				Action = runActionOnMainThread,
				CoherentHandle = null
			});
			WebViewEventHandler handler = webViewEventHandler2;
			if (IsReady)
			{
				_coUiManager.RunInThread(delegate
				{
					//IL_0027: Unknown result type (might be due to invalid IL or missing references)
					handler.CoherentHandle = _view.RegisterForEvent(name, runActionOnMainThread);
				});
			}
		}
	}

	public void RegisterForEvent<T>(string name, Disposable disposeGate, Action<T> action)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		lock (_eventHandlers)
		{
			if (_eventHandlers.ContainsKey(name))
			{
				throw new Exception("There's already an event handler registered for " + name);
			}
			Action<T> runActionOnMainThread = delegate(T obj)
			{
				_engine.RunOnMainThread(disposeGate, delegate
				{
					action(obj);
				});
			};
			WebViewEventHandler webViewEventHandler2 = (_eventHandlers[name] = new WebViewEventHandler
			{
				Action = runActionOnMainThread,
				CoherentHandle = null
			});
			WebViewEventHandler handler = webViewEventHandler2;
			if (IsReady)
			{
				_coUiManager.RunInThread(delegate
				{
					//IL_0027: Unknown result type (might be due to invalid IL or missing references)
					handler.CoherentHandle = _view.RegisterForEvent(name, runActionOnMainThread);
				});
			}
		}
	}

	public void RegisterForEvent<T1, T2>(string name, Disposable disposeGate, Action<T1, T2> action)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		lock (_eventHandlers)
		{
			if (_eventHandlers.ContainsKey(name))
			{
				throw new Exception("There's already an event handler registered for " + name);
			}
			Action<T1, T2> runActionOnMainThread = delegate(T1 obj1, T2 obj2)
			{
				_engine.RunOnMainThread(disposeGate, delegate
				{
					action(obj1, obj2);
				});
			};
			WebViewEventHandler webViewEventHandler2 = (_eventHandlers[name] = new WebViewEventHandler
			{
				Action = runActionOnMainThread,
				CoherentHandle = null
			});
			WebViewEventHandler handler = webViewEventHandler2;
			if (IsReady)
			{
				_coUiManager.RunInThread(delegate
				{
					//IL_0027: Unknown result type (might be due to invalid IL or missing references)
					handler.CoherentHandle = _view.RegisterForEvent(name, runActionOnMainThread);
				});
			}
		}
	}

	public void RegisterForEvent<T1, T2, T3>(string name, Disposable disposeGate, Action<T1, T2, T3> action)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		lock (_eventHandlers)
		{
			if (_eventHandlers.ContainsKey(name))
			{
				throw new Exception("There's already an event handler registered for " + name);
			}
			Action<T1, T2, T3> runActionOnMainThread = delegate(T1 obj1, T2 obj2, T3 obj3)
			{
				_engine.RunOnMainThread(disposeGate, delegate
				{
					action(obj1, obj2, obj3);
				});
			};
			WebViewEventHandler webViewEventHandler2 = (_eventHandlers[name] = new WebViewEventHandler
			{
				Action = runActionOnMainThread,
				CoherentHandle = null
			});
			WebViewEventHandler handler = webViewEventHandler2;
			if (IsReady)
			{
				_coUiManager.RunInThread(delegate
				{
					//IL_0027: Unknown result type (might be due to invalid IL or missing references)
					handler.CoherentHandle = _view.RegisterForEvent(name, runActionOnMainThread);
				});
			}
		}
	}

	public void UnregisterFromEvent(string name)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		lock (_eventHandlers)
		{
			if (!_eventHandlers.TryGetValue(name, out var handler))
			{
				throw new Exception("There's no event handler registered for " + name);
			}
			_eventHandlers.Remove(name);
			if (IsReady)
			{
				_coUiManager.RunInThread(delegate
				{
					//IL_001c: Unknown result type (might be due to invalid IL or missing references)
					_view?.UnregisterFromEvent(handler.CoherentHandle.Value);
				});
			}
		}
	}

	public void TriggerEvent(string name, object data1 = null, object data2 = null, object data3 = null, object data4 = null, object data5 = null)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(WebView).FullName);
		}
		if (!IsReady || IsReloading)
		{
			_eventQueue.Add(Tuple.Create(name, data1, data2, data3, data4, data5));
			return;
		}
		_coUiManager.RunInThread(delegate
		{
			_view?.TriggerEvent(name, data1, data2, data3, data4, data5);
		});
	}

	public ImageData CreateImageData(string name, int width, int height, IntPtr data, bool flipY)
	{
		return _view.CreateImageData(name, width, height, data, flipY);
	}

	public void Initialize(ViewContext viewContext)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		if (_view == null)
		{
			ViewInfo val = new ViewInfo
			{
				Width = Width,
				Height = Height,
				IsTransparent = true,
				SupportClickThrough = false,
				ForceSoftwareRendering = (BuildInfo.Platform != Platform.Linux),
				TargetFrameRate = 1000,
				UsesSharedMemory = true
			};
			viewContext.CreateView(val, URL, (ViewListenerBase)(object)_viewListener);
		}
	}

	public void OnCoherentViewCreated(View view)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		_view = new CoUIViewDebugWrapper(view);
		Logger.Info("Coherent view has been created");
		_view.SetZoomLevel(System.Math.Log(Scale, 1.2));
		if (_coUiManager.FocusedWebView == this)
		{
			_view.SetFocus();
		}
	}

	public void OnCoherentReadyForBindings(int frameId, string path, bool isMainFrame)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(!ThreadHelper.IsMainThread());
		if (IsReady)
		{
			throw new Exception("Entering WebView.OnCoherentReadyForBindings but IsReady is true already");
		}
		if (!isMainFrame)
		{
			return;
		}
		lock (_eventHandlers)
		{
			IsReady = true;
			IsReloading = false;
			foreach (KeyValuePair<string, WebViewEventHandler> eventHandler in _eventHandlers)
			{
				eventHandler.Value.CoherentHandle = _view.RegisterForEvent(eventHandler.Key, eventHandler.Value.Action);
			}
		}
		_engine.RunOnMainThread(this, delegate
		{
			foreach (Tuple<string, object, object, object, object, object> item in _eventQueue)
			{
				TriggerEvent(item.Item1, item.Item2, item.Item3, item.Item4, item.Item5, item.Item6);
			}
			_eventQueue.Clear();
		});
	}

	public void OnCoherentScriptMessage(MessageLevel level, string message, string sourceId, int line)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(!ThreadHelper.IsMainThread());
		Logger.Info<MessageLevel, int, string>("({0}) {1} - {2}", level, line, message);
	}

	public void OnCoherentError(ViewError error)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Logger.Error<string, ViewErrorType>("A Coherent UI error occurred: {0} ({1})", error.Error, error.ErrorCode);
	}

	public void OnCoherentFailLoad(int frameId, string validatedPath, bool isMainFrame, string error)
	{
		Logger.Error("Failed to load Coherent UI site, Error={0}, FrameId={1}, ValidatedPath={2}, IsMainFrame={3}", new object[4] { error, frameId, validatedPath, isMainFrame });
	}

	public void OnCoherentFinishLoad(int frameId, string validatedPath, bool isMainFrame, int status, HTTPHeader[] headers)
	{
		Logger.Info("Successfully loaded Coherent UI site, Status={0}, FrameId={1}, ValidatedPath={2}, IsMainFrame={3}", new object[4] { status, frameId, validatedPath, isMainFrame });
	}

	public void OnCoherentDraw(CoherentHandle handle, bool usesSharedMemory, int width, int height)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		if (_coherentMemoryHandleValue != handle.HandleValue)
		{
			lock (_coherentMemoryLock)
			{
				IntPtr coherentMemorySharedPointer = _coherentMemorySharedPointer;
				if (coherentMemorySharedPointer != IntPtr.Zero)
				{
					_coherentMemorySharedPointer = IntPtr.Zero;
					if (BuildInfo.Platform == Platform.Windows)
					{
						SharedMemoryMapHelper.FreeMapSharedMemoryWindows(coherentMemorySharedPointer);
					}
					else if (BuildInfo.Platform == Platform.Linux)
					{
						SharedMemoryMapHelper.FreeMapSharedMemoryLinux(coherentMemorySharedPointer);
					}
					else
					{
						SharedMemoryMapHelper.FreeMapSharedMemoryMacOS(coherentMemorySharedPointer, width * height * 4);
					}
				}
				_coherentMemoryHandleValue = handle.HandleValue;
				_coherentMemoryWidth = width;
				_coherentMemoryHeight = height;
				if (BuildInfo.Platform == Platform.Windows)
				{
					_coherentMemorySharedPointer = SharedMemoryMapHelper.DoMapSharedMemoryWindows(handle.HandleValue, width * height * 4);
				}
				else if (BuildInfo.Platform == Platform.Linux)
				{
					_coherentMemorySharedPointer = SharedMemoryMapHelper.DoMapSharedMemoryLinux(handle.HandleValue);
				}
				else
				{
					_coherentMemorySharedPointer = SharedMemoryMapHelper.DoMapSharedMemoryMacOS(handle.HandleValue, width * height * 4);
				}
			}
		}
		_textureNeedsUpdate = true;
	}

	public void OnCursorChanged(CursorTypes cursorType)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		_engine.RunOnMainThread(_engine, delegate
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Invalid comparison between Unknown and I4
			IntPtr intPtr = (((int)cursorType != 29) ? SDL.SDL_CreateSystemCursor((SDL_SystemCursor)0) : SDL.SDL_CreateSystemCursor((SDL_SystemCursor)3));
			SDL.SDL_SetCursor(intPtr);
		});
	}

	public void OnNavigateTo(string path)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		if (IsReady)
		{
			IsReloading = true;
		}
		IsReady = false;
	}

	public void KeyEvent(KeyEventData arg0)
	{
		_view?.KeyEvent(arg0);
	}

	public void MouseEvent(MouseEventData arg0)
	{
		_view?.MouseEvent(arg0);
	}
}
