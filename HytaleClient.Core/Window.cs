#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Data;
using HytaleClient.Math;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.Core;

public sealed class Window : Disposable
{
	public enum WindowState
	{
		Normal,
		Minimized,
		Maximized,
		Fullscreen
	}

	public class WindowSettings
	{
		public string Title;

		public Image Icon;

		public bool Resizable;

		public bool Borderless;

		public WindowState InitialState;

		public Point MinimumSize;

		public Point InitialSize;

		public float MinAspectRatio = 0f;

		public float MaxAspectRatio = float.PositiveInfinity;
	}

	public readonly IntPtr Handle;

	private readonly IntPtr _win32Handle;

	public readonly uint Id;

	public readonly double MinAspectRatio;

	public readonly double MaxAspectRatio;

	private bool _borderless;

	private Point _minimumSize;

	private double _drawableScale = 1.0;

	private double _monitorZoom = 1.0;

	private Point _zoomedMinimumSize;

	private Point _zoomedNormalSize;

	public float ViewportScale { get; private set; }

	public Rectangle Viewport { get; private set; }

	public double AspectRatio { get; private set; }

	public bool IsMouseLocked { get; private set; }

	public bool IsFocused { get; private set; } = true;


	public bool IsCursorVisible { get; private set; }

	public Window(WindowSettings settings)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		_zoomedMinimumSize = (_minimumSize = settings.MinimumSize);
		_zoomedNormalSize = settings.InitialSize;
		_borderless = settings.Borderless;
		MinAspectRatio = settings.MinAspectRatio;
		MaxAspectRatio = settings.MaxAspectRatio;
		if (MaxAspectRatio < MinAspectRatio)
		{
			throw new ArgumentException("MaxAspectRatio must be >= MinAspectRatio");
		}
		SDL_WindowFlags val = (SDL_WindowFlags)8202;
		if (settings.Resizable)
		{
			val = (SDL_WindowFlags)(val | 0x20);
		}
		if (settings.Borderless)
		{
			val = (SDL_WindowFlags)(val | 0x10);
		}
		Handle = SDL.SDL_CreateWindow(settings.Title, 805240832, 805240832, settings.InitialSize.X, settings.InitialSize.Y, val);
		if (Handle == IntPtr.Zero)
		{
			throw new Exception("Failed to create window: " + SDL.SDL_GetError());
		}
		if (settings.Icon != null)
		{
			settings.Icon.DoWithSurface(settings.Icon.Width, settings.Icon.Height, delegate(IntPtr surface)
			{
				SDL.SDL_SetWindowIcon(Handle, surface);
			});
		}
		Id = SDL.SDL_GetWindowID(Handle);
		SDL_SysWMinfo val2 = default(SDL_SysWMinfo);
		SDL.SDL_GetWindowWMInfo(Handle, ref val2);
		_win32Handle = val2.info.win.window;
		SetState(settings.InitialState, _borderless, recalculateZoom: false);
		SDL.SDL_SetWindowPosition(Handle, 805240832, 805240832);
	}

	protected override void DoDispose()
	{
		SDL.SDL_DestroyWindow(Handle);
	}

	public void Show()
	{
		SDL.SDL_ShowWindow(Handle);
	}

	public void Raise()
	{
		SDL.SDL_RaiseWindow(Handle);
	}

	public WindowState GetState()
	{
		uint num = SDL.SDL_GetWindowFlags(Handle);
		if ((num & (true ? 1u : 0u)) != 0)
		{
			return WindowState.Fullscreen;
		}
		if ((num & 0x80u) != 0)
		{
			return WindowState.Maximized;
		}
		if ((num & 0x40u) != 0)
		{
			return WindowState.Minimized;
		}
		return WindowState.Normal;
	}

	public Point GetSize()
	{
		int x = default(int);
		int y = default(int);
		SDL.SDL_GetWindowSize(Handle, ref x, ref y);
		return new Point(x, y);
	}

	public void SetState(WindowState state, bool borderless, bool recalculateZoom)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		_borderless = borderless;
		if (state == WindowState.Fullscreen)
		{
			SDL.SDL_RestoreWindow(Handle);
			if (_borderless)
			{
				SDL.SDL_SetWindowFullscreen(Handle, 4097u);
			}
			else
			{
				SDL_DisplayMode val = default(SDL_DisplayMode);
				SDL.SDL_GetDesktopDisplayMode(0, ref val);
				SDL.SDL_SetWindowSize(Handle, val.w, val.h);
				SDL.SDL_SetWindowFullscreen(Handle, 1u);
			}
		}
		else
		{
			SDL.SDL_SetWindowFullscreen(Handle, 0u);
			if (state == WindowState.Maximized && _borderless)
			{
				state = WindowState.Normal;
			}
			if (state == WindowState.Normal)
			{
				SDL.SDL_RestoreWindow(Handle);
			}
			SDL.SDL_SetWindowBordered(Handle, (SDL_bool)(!_borderless));
			SDL.SDL_SetWindowMinimumSize(Handle, _zoomedMinimumSize.X, _zoomedMinimumSize.Y);
			SDL.SDL_SetWindowSize(Handle, _zoomedNormalSize.X, _zoomedNormalSize.Y);
			SDL.SDL_SetWindowPosition(Handle, 805240832, 805240832);
			switch (state)
			{
			case WindowState.Maximized:
				SDL.SDL_MaximizeWindow(Handle);
				break;
			case WindowState.Minimized:
				SDL.SDL_MinimizeWindow(Handle);
				break;
			}
		}
		SetupViewport(recalculateZoom);
	}

	public void SetupViewport(bool recalculateZoom = false)
	{
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		WindowState state = GetState();
		int num = default(int);
		int num2 = default(int);
		SDL.SDL_GetWindowSize(Handle, ref num, ref num2);
		if (state == WindowState.Normal || state == WindowState.Minimized)
		{
			_zoomedNormalSize = new Point(num, num2);
		}
		int num3 = SDL.SDL_GetWindowDisplayIndex(Handle);
		SDL_Rect val = default(SDL_Rect);
		SDL.SDL_GetDisplayUsableBounds(num3, ref val);
		double num4 = 1.0;
		int num5 = default(int);
		int num6 = default(int);
		SDL.SDL_GL_GetDrawableSize(Handle, ref num5, ref num6);
		_drawableScale = (double)num6 / (double)num2;
		uint dpi;
		if (num6 != num2)
		{
			num4 = 1.0;
		}
		else if (WindowsDPIHelper.TryGetDpiForWindow(_win32Handle, out dpi))
		{
			num4 = (double)dpi / 96.0;
			_zoomedMinimumSize = new Point((int)((double)_minimumSize.X * num4), (int)((double)_minimumSize.Y * num4));
			if (_zoomedMinimumSize.X >= val.w || _zoomedMinimumSize.Y >= val.h)
			{
				num4 = 1.0;
				_zoomedMinimumSize = _minimumSize;
			}
		}
		Debug.Assert(_drawableScale == 1.0 || num4 == 1.0);
		if (recalculateZoom)
		{
			RecalculateZoom(calculateViaCompare: false, num4, _minimumSize.X, _minimumSize.Y);
		}
		else if (num4 != _monitorZoom)
		{
			RecalculateZoom(calculateViaCompare: true, num4, _zoomedNormalSize.X, _zoomedNormalSize.Y);
		}
		ViewportScale = (float)(_drawableScale * _monitorZoom);
		AspectRatio = System.Math.Min(System.Math.Max((double)num / (double)num2, MinAspectRatio), MaxAspectRatio);
		int num7 = (int)((double)num6 * AspectRatio);
		int num8 = (int)((double)num5 / AspectRatio);
		if (num8 > num6)
		{
			num8 = num6;
			num7 = (int)((double)num8 * AspectRatio);
		}
		if (num7 > num5)
		{
			num7 = num5;
			num8 = (int)((double)num7 / AspectRatio);
		}
		Viewport = new Rectangle((num5 - num7) / 2, (num6 - num8) / 2, num7, num8);
	}

	public void RecalculateZoom(bool calculateViaCompare, double newMonitorZoom, int width, int height)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		WindowState state = GetState();
		SDL_Rect val = default(SDL_Rect);
		SDL.SDL_GetDisplayBounds(SDL.SDL_GetWindowDisplayIndex(Handle), ref val);
		int num = default(int);
		int num2 = default(int);
		SDL.SDL_GL_GetDrawableSize(Handle, ref num, ref num2);
		if (calculateViaCompare)
		{
			double num3 = newMonitorZoom / _monitorZoom;
			_monitorZoom = newMonitorZoom;
			_zoomedNormalSize = new Point(MathHelper.Clamp((int)((double)width * num3), _zoomedMinimumSize.X, val.w), MathHelper.Clamp((int)((double)height * num3), _zoomedMinimumSize.Y, val.h));
		}
		else
		{
			_zoomedNormalSize = new Point(MathHelper.Clamp((int)((double)width * _monitorZoom), _zoomedMinimumSize.X, val.w), MathHelper.Clamp((int)((double)height * _monitorZoom), _zoomedMinimumSize.Y, val.h));
		}
		SDL.SDL_SetWindowMinimumSize(Handle, _zoomedMinimumSize.X, _zoomedMinimumSize.Y);
		if (state == WindowState.Normal || state == WindowState.Minimized)
		{
			SDL.SDL_SetWindowSize(Handle, _zoomedNormalSize.X, _zoomedNormalSize.Y);
			int num4 = default(int);
			int num5 = default(int);
			SDL.SDL_GetWindowPosition(Handle, ref num4, ref num5);
			SDL.SDL_SetWindowPosition(Handle, num4, num5);
			width = (num = _zoomedNormalSize.X);
			height = (num2 = _zoomedNormalSize.Y);
		}
	}

	public Point TransformSDLToViewportCoords(int x, int y)
	{
		return new Point((int)((double)x * _drawableScale) - Viewport.X, (int)((double)y * _drawableScale) - Viewport.Y);
	}

	public void UpdateSize(ScreenResolution resolution)
	{
		_minimumSize.X = resolution.Width;
		_minimumSize.Y = resolution.Height;
		_zoomedMinimumSize.X = resolution.Width;
		_zoomedMinimumSize.Y = resolution.Height;
		_zoomedNormalSize.X = resolution.Width;
		_zoomedNormalSize.Y = resolution.Height;
	}

	public void OnFocusChanged(bool isFocused)
	{
		IsFocused = isFocused;
		if (IsMouseLocked)
		{
			ApplyMouseSettings();
		}
	}

	public void SetCursorVisible(bool visible)
	{
		IsCursorVisible = visible;
		ApplyMouseSettings();
	}

	public void SetMouseLock(bool enabled)
	{
		bool isMouseLocked = IsMouseLocked;
		IsMouseLocked = enabled;
		ApplyMouseSettings();
		int num = default(int);
		int num2 = default(int);
		SDL.SDL_GetWindowSize(Handle, ref num, ref num2);
		if (!enabled && isMouseLocked && IsFocused)
		{
			SDL.SDL_WarpMouseInWindow(Handle, num / 2, num2 / 2);
		}
	}

	private void ApplyMouseSettings()
	{
		SDL.SDL_SetRelativeMouseMode((SDL_bool)((IsMouseLocked && IsFocused && !IsCursorVisible) ? 1 : 0));
	}

	public Vector2 SDLToNormalizedScreenCenterCoords(int x, int y)
	{
		Point point = TransformSDLToViewportCoords(x, y);
		float num = (float)Viewport.Width / 2f;
		float num2 = (float)Viewport.Height / 2f;
		return new Vector2(((float)point.X - num) / num, ((float)point.Y - num2) / num2);
	}
}
