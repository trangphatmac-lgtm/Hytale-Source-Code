#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Interface.CoherentUI;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

internal class WebCodeEditor : Element
{
	public enum EditorLanguage
	{
		Json,
		Plaintext
	}

	private WebView _webView;

	private TextureArea _textureArea;

	private bool _isEditorReady;

	private string _value;

	public Action ValueChanged;

	private EditorLanguage _language;

	private readonly BaseInterface _interface;

	public bool IsInitialized => _webView != null;

	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
			if (_isEditorReady)
			{
				_webView.TriggerEvent("setValue", value ?? "");
			}
		}
	}

	public EditorLanguage Language
	{
		get
		{
			return _language;
		}
		set
		{
			_language = value;
			if (_isEditorReady)
			{
				_webView.TriggerEvent("setLanguage", value.ToString());
			}
		}
	}

	public WebCodeEditor(BaseInterface @interface, Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_interface = @interface;
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		Animate(0f);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		_textureArea?.Texture.Dispose();
		_textureArea = null;
	}

	public void InitEditor()
	{
		Debug.Assert(!IsInitialized);
		_webView = new WebView(_interface.Engine, _interface.CoUiManager, "coui://monaco-editor/index.html#" + Language, 1, 1, _interface.Engine.Window.ViewportScale);
		_webView.RegisterForEvent("ready", _interface, OnEditorReady);
		_webView.RegisterForEvent<string>("didChangeContent", _interface, OnDidChangeContent);
	}

	public void DisposeEditor()
	{
		_webView.UnregisterFromEvent("ready");
		_webView.UnregisterFromEvent("didChangeContent");
		WebView oldWebView = _webView;
		_webView = null;
		_isEditorReady = false;
		_interface.CoUiManager.RunInThread(delegate
		{
			oldWebView.Destroy();
			_interface.Engine.RunOnMainThread(_interface.Engine, delegate
			{
				oldWebView.Dispose();
			});
		});
	}

	protected override void LayoutSelf()
	{
		base.LayoutSelf();
		if (_webView != null && _webView.IsReady)
		{
			int width = base.AnchoredRectangle.Width;
			int height = base.AnchoredRectangle.Height;
			if (_webView.Width != width || _webView.Height != height)
			{
				_webView.Resize(width, height, _interface.Engine.Window.ViewportScale);
			}
			if (_textureArea == null || _webView.Width != width || _webView.Height != height)
			{
				_textureArea?.Texture.Dispose();
				Texture texture = new Texture(Texture.TextureTypes.Texture2D);
				texture.CreateTexture2D(width, height);
				_textureArea = new TextureArea(texture, 0, 0, width, height, 1);
			}
		}
	}

	private void Animate(float deltaTime)
	{
		if (_textureArea != null)
		{
			Desktop.Graphics.GL.BindTexture(GL.TEXTURE_2D, _textureArea.Texture.GLTexture);
			_webView.RenderToTexture();
		}
	}

	private void OnEditorReady()
	{
		_isEditorReady = true;
		_webView.TriggerEvent("setValue", _value ?? "");
		if (base.IsMounted)
		{
			Layout();
		}
	}

	private void OnDidChangeContent(string value)
	{
		_value = value;
		ValueChanged?.Invoke();
	}

	protected override void PrepareForDrawSelf()
	{
		if (_isEditorReady)
		{
			Desktop.Batcher2D.RequestDrawTexture(_textureArea.Texture, _textureArea.Rectangle, _anchoredRectangle, UInt32Color.White);
		}
	}

	public override Element HitTest(Point position)
	{
		if (_waitingForLayoutAfterMount || !_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		Desktop.FocusElement(this);
		Point mousePosition = Desktop.MousePosition;
		mousePosition.X -= base.AnchoredRectangle.X;
		mousePosition.Y -= base.AnchoredRectangle.Y;
		_interface.CoUiManager.RunInThread(delegate
		{
			CoUIViewInputForwarder.SendMouseEvent(_webView, (SDL_EventType)1025, evt.Button, mousePosition, Point.Zero);
		});
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		Point mousePosition = Desktop.MousePosition;
		mousePosition.X -= base.AnchoredRectangle.X;
		mousePosition.Y -= base.AnchoredRectangle.Y;
		_interface.CoUiManager.RunInThread(delegate
		{
			CoUIViewInputForwarder.SendMouseEvent(_webView, (SDL_EventType)1026, evt.Button, mousePosition, Point.Zero);
		});
	}

	protected override void OnMouseMove()
	{
		Point mousePosition = Desktop.MousePosition;
		mousePosition.X -= base.AnchoredRectangle.X;
		mousePosition.Y -= base.AnchoredRectangle.Y;
		_interface.CoUiManager.RunInThread(delegate
		{
			CoUIViewInputForwarder.SendMouseEvent(_webView, (SDL_EventType)1024, 0, mousePosition, Point.Zero);
		});
	}

	protected internal override bool OnMouseWheel(Point offset)
	{
		Point mousePosition = Desktop.MousePosition;
		mousePosition.X -= base.AnchoredRectangle.X;
		mousePosition.Y -= base.AnchoredRectangle.Y;
		_interface.CoUiManager.RunInThread(delegate
		{
			CoUIViewInputForwarder.SendMouseEvent(_webView, (SDL_EventType)1027, 0, mousePosition, offset);
		});
		return true;
	}

	protected internal override void OnKeyDown(SDL_Keycode keyCode, int repeat)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		SDL_Scancode scanCode = SDL.SDL_GetScancodeFromKey(keyCode);
		SDL_Keymod keymod = (SDL_Keymod)0;
		if (Desktop.IsCtrlKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 0xC0);
		}
		if (Desktop.IsShiftKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 3);
		}
		if (Desktop.IsAltKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 0x100);
		}
		if (Desktop.IsGuiKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 0xC00);
		}
		_interface.CoUiManager.RunInThread(delegate
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			CoUIViewInputForwarder.SendKeyboardEvent(_webView, (SDL_EventType)768, keyCode, scanCode, 0, keymod);
		});
	}

	protected internal override void OnKeyUp(SDL_Keycode keyCode)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		SDL_Scancode scanCode = SDL.SDL_GetScancodeFromKey(keyCode);
		SDL_Keymod keymod = (SDL_Keymod)0;
		if (Desktop.IsCtrlKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 0xC0);
		}
		if (Desktop.IsShiftKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 3);
		}
		if (Desktop.IsAltKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 0x100);
		}
		if (Desktop.IsGuiKeyDown)
		{
			keymod = (SDL_Keymod)(keymod | 0xC00);
		}
		_interface.CoUiManager.RunInThread(delegate
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			CoUIViewInputForwarder.SendKeyboardEvent(_webView, (SDL_EventType)769, keyCode, scanCode, 0, keymod);
		});
	}

	protected internal override void OnTextInput(string text)
	{
		_interface.CoUiManager.RunInThread(delegate
		{
			CoUIViewInputForwarder.SendTextInputEvent(_webView, text);
		});
	}

	protected internal override void OnBlur()
	{
		_webView?.TriggerEvent("blur");
	}
}
