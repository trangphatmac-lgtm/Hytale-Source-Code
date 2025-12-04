#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Batcher2D;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.Interface.UI;

public class Desktop
{
	public readonly IUIProvider Provider;

	public readonly Batcher2D Batcher2D;

	public readonly GraphicsDevice Graphics;

	public bool DrawOutlines;

	private readonly SortedList<int, Element> _layerStack = new SortedList<int, Element>();

	private Element _transientLayer;

	private Element _passiveLayer;

	private readonly List<Action<float>> _animationCallbacks = new List<Action<float>>();

	private readonly List<Tuple<bool, Action<float>>> _animationCallbackChanges = new List<Tuple<bool, Action<float>>>();

	private bool _isFocused = true;

	private readonly List<Element> _hoverStack = new List<Element>();

	private readonly List<Element> _upcomingHoverStack = new List<Element>();

	private readonly List<Element> _dragOverStack = new List<Element>();

	private readonly List<Element> _upcomingDragOverStack = new List<Element>();

	private Element _mouseElement;

	private Element _dragOverElement;

	private int? _mouseCaptureButton;

	private int? _mouseCaptureClicks;

	private bool _isMouseOverCapturedElement;

	public const float UnscaledMouseWheelMultiplier = 30f;

	public const int UnscaledMouseDragStartDistance = 3;

	private object _mouseDragData;

	private Element _mouseDragElement;

	private Element _mouseDragElementToDraw;

	private Vector2 _mouseDragMouseOffset;

	public Cursors Cursors => Graphics.Cursors;

	public Rectangle ViewportRectangle { get; private set; }

	public Rectangle RootLayoutRectangle { get; private set; }

	public float Scale { get; private set; }

	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			_isFocused = value;
			if (!_isFocused)
			{
				ClearInput();
			}
		}
	}

	public bool IsShortcutKeyDown { get; private set; }

	public bool IsGuiKeyDown { get; private set; }

	public bool IsShiftKeyDown { get; private set; }

	public bool IsCtrlKeyDown { get; private set; }

	public bool IsAltKeyDown { get; private set; }

	public bool IsWordSkipDown { get; private set; }

	public bool IsLineSkipDown { get; private set; }

	public Point MousePosition { get; private set; }

	public Element FocusedElement { get; private set; }

	public Element CapturedElement => _isMouseOverCapturedElement ? _mouseElement : null;

	public bool IsMouseDragging => _mouseDragData != null || _mouseDragElement != null;

	public Desktop(IUIProvider provider, GraphicsDevice graphics, Batcher2D batcher2D)
	{
		Graphics = graphics;
		Provider = provider;
		Batcher2D = batcher2D;
	}

	public void SetViewport(Rectangle viewportRectangle, float newScale)
	{
		ClearInput(clearFocus: false);
		ViewportRectangle = viewportRectangle;
		RootLayoutRectangle = new Rectangle(0, 0, viewportRectangle.Width, viewportRectangle.Height);
		if (Scale != 0f)
		{
			float scaleRatio = newScale / Scale;
			foreach (Element value in _layerStack.Values)
			{
				value.Rescale(scaleRatio);
			}
			_transientLayer?.Rescale(scaleRatio);
			_passiveLayer?.Rescale(scaleRatio);
		}
		Scale = newScale;
		Layout();
	}

	public int ScaleRound(float value)
	{
		return MathHelper.Round(value * Scale);
	}

	public float ScaleNoRound(float value)
	{
		return value * Scale;
	}

	public Point ScaleRound(Point value)
	{
		return new Point(ScaleRound(value.X), ScaleRound(value.Y));
	}

	public int UnscaleRound(float value)
	{
		return MathHelper.Round(value / Scale);
	}

	public TexturePatch MakeTexturePatch(PatchStyle style)
	{
		TextureArea textureArea = style.TextureArea ?? ((style.TexturePath != null) ? Provider.MakeTextureArea(style.TexturePath.Value) : Provider.WhitePixel);
		if (style.Area.HasValue)
		{
			Rectangle value = style.Area.Value;
			if (style.TextureArea != null)
			{
				textureArea = textureArea.Clone();
			}
			textureArea.Rectangle = new Rectangle(textureArea.Rectangle.X + value.X * textureArea.Scale, textureArea.Rectangle.Y + value.Y * textureArea.Scale, value.Width * textureArea.Scale, value.Height * textureArea.Scale);
		}
		return new TexturePatch
		{
			TextureArea = textureArea,
			HorizontalBorder = style.HorizontalBorder,
			VerticalBorder = style.VerticalBorder,
			Color = style.Color
		};
	}

	public Element GetLayer(int key)
	{
		if (_layerStack.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	public Element GetTransientLayer()
	{
		return _transientLayer;
	}

	public Element GetInteractiveLayer()
	{
		return _transientLayer ?? ((_layerStack.Count > 0) ? _layerStack.Values[_layerStack.Count - 1] : null);
	}

	public void SetLayer(int key, Element layer)
	{
		if (_layerStack.ContainsKey(key))
		{
			throw new Exception($"Cannot set layer at key {key}, there is alreay one.");
		}
		if (_layerStack.ContainsValue(layer) || layer.Parent != null)
		{
			throw new Exception("Cannot set element as layer, it is already in use.");
		}
		if (!layer.Visible)
		{
			throw new Exception("Cannot set an invisible layer.");
		}
		bool flag = _layerStack.Count == 0 || _layerStack.Keys[_layerStack.Count - 1] < key;
		if (flag)
		{
			if (_transientLayer != null)
			{
				SetTransientLayer(null);
			}
			ClearInput();
		}
		_layerStack.Add(key, layer);
		layer.Mount();
		layer.Layout(RootLayoutRectangle);
		if (flag)
		{
			RefreshHover();
		}
	}

	public void ClearLayer(int key)
	{
		if (!_layerStack.ContainsKey(key))
		{
			throw new Exception($"Cannot clear layer at key {key}, there none.");
		}
		bool flag = _layerStack.Keys[_layerStack.Count - 1] == key;
		if (flag)
		{
			ClearInput();
		}
		Element element = _layerStack[key];
		_layerStack.Remove(key);
		element.Unmount();
		if (flag)
		{
			RefreshHover();
		}
	}

	public void ClearAllLayers()
	{
		ClearInput();
		SetPassiveLayer(null);
		SetTransientLayer(null);
		while (_layerStack.Count > 0)
		{
			Element element = _layerStack.Values[_layerStack.Count - 1];
			_layerStack.RemoveAt(_layerStack.Count - 1);
			element.Unmount();
		}
	}

	public void SetTransientLayer(Element element)
	{
		ClearInput();
		_transientLayer?.Unmount();
		_transientLayer = element;
		if (_transientLayer != null)
		{
			_transientLayer.Mount();
			_transientLayer.Layout(RootLayoutRectangle);
		}
		RefreshHover();
	}

	public void SetPassiveLayer(Element element)
	{
		_passiveLayer?.Unmount();
		_passiveLayer = element;
		if (_passiveLayer != null)
		{
			_passiveLayer.Mount();
			_passiveLayer.Layout(RootLayoutRectangle);
		}
	}

	public void Layout()
	{
		foreach (Element value in _layerStack.Values)
		{
			value.Layout(RootLayoutRectangle);
		}
		_transientLayer?.Layout(RootLayoutRectangle);
		_passiveLayer?.Layout(RootLayoutRectangle);
		RefreshHover();
	}

	public void RegisterAnimationCallback(Action<float> animate)
	{
		_animationCallbackChanges.Add(new Tuple<bool, Action<float>>(item1: true, animate));
	}

	public void UnregisterAnimationCallback(Action<float> animate)
	{
		_animationCallbackChanges.Add(new Tuple<bool, Action<float>>(item1: false, animate));
	}

	public void Update(float deltaTime)
	{
		foreach (Tuple<bool, Action<float>> animationCallbackChange in _animationCallbackChanges)
		{
			bool item = animationCallbackChange.Item1;
			Action<float> item2 = animationCallbackChange.Item2;
			if (item)
			{
				Debug.Assert(!_animationCallbacks.Contains(item2));
				_animationCallbacks.Add(item2);
			}
			else
			{
				Debug.Assert(_animationCallbacks.Contains(item2));
				_animationCallbacks.Remove(item2);
			}
		}
		_animationCallbackChanges.Clear();
		foreach (Action<float> animationCallback in _animationCallbacks)
		{
			animationCallback(deltaTime);
		}
	}

	public void PrepareForDraw()
	{
		if (Scale == 0f)
		{
			throw new Exception("Viewport must be set before drawing.");
		}
		foreach (Element value in _layerStack.Values)
		{
			value.PrepareForDraw();
		}
		_transientLayer?.PrepareForDraw();
		_passiveLayer?.PrepareForDraw();
		if (_mouseDragElementToDraw != null)
		{
			Vector3 position = new Vector3((float)(MousePosition.X - _mouseDragElementToDraw.AnchoredRectangle.X) - _mouseDragMouseOffset.X, (float)(MousePosition.Y - _mouseDragElementToDraw.AnchoredRectangle.Y) - _mouseDragMouseOffset.Y, 0f);
			Graphics.Batcher2D.SetTransformationMatrix(position, Quaternion.Identity, 1f);
			_mouseDragElementToDraw.PrepareForDraw();
			Graphics.Batcher2D.SetTransformationMatrix(Matrix.Identity);
		}
		if (!DrawOutlines)
		{
			return;
		}
		foreach (Element value2 in _layerStack.Values)
		{
			value2.PrepareForDrawOutline();
		}
		_transientLayer?.PrepareForDrawOutline();
		_passiveLayer?.PrepareForDrawOutline();
	}

	public void StartMouseDrag(object data, Element element, Element elementToDraw = null)
	{
		if (data == null && element == null)
		{
			throw new ArgumentException("data and element can't be both null.");
		}
		_mouseDragData = data;
		_mouseDragElement = element;
		if (elementToDraw != null)
		{
			_mouseDragElementToDraw = elementToDraw;
			_mouseDragMouseOffset = new Vector2(MousePosition.X - elementToDraw.AnchoredRectangle.X, MousePosition.Y - elementToDraw.AnchoredRectangle.Y);
		}
		RefreshHover();
	}

	public void ClearMouseDrag()
	{
		foreach (Element item in _dragOverStack)
		{
			item.OnMouseDragExit(_mouseDragData, _mouseDragElement);
		}
		_dragOverStack.Clear();
		_mouseDragData = null;
		_mouseDragElement = null;
		_mouseDragElementToDraw = null;
		RefreshHover();
	}

	public void CancelMouseDrag()
	{
		foreach (Element item in _dragOverStack)
		{
			item.OnMouseDragExit(_mouseDragData, _mouseDragElement);
		}
		_dragOverStack.Clear();
		object mouseDragData = _mouseDragData;
		_mouseDragData = null;
		Element mouseDragElement = _mouseDragElement;
		_mouseDragElement = null;
		_mouseDragElementToDraw = null;
		mouseDragElement.OnMouseDragCancel(mouseDragData);
		RefreshHover();
	}

	public void ClearInput(bool clearFocus = true)
	{
		IsShortcutKeyDown = false;
		IsShiftKeyDown = false;
		IsCtrlKeyDown = false;
		IsWordSkipDown = false;
		IsLineSkipDown = false;
		ClearMouseElement();
		if (clearFocus)
		{
			FocusedElement?.OnBlur();
			FocusedElement = null;
		}
	}

	public void FocusElement(Element element, bool clearMouseCapture = true)
	{
		if (element != null)
		{
			Debug.Assert(element.IsMounted, "Only mounted elements can be focused");
		}
		if (clearMouseCapture && element != _mouseElement)
		{
			ClearMouseCapture();
		}
		FocusedElement?.OnBlur();
		FocusedElement = element;
		FocusedElement?.OnFocus();
	}

	private void ClearMouseElement()
	{
		foreach (Element item in _hoverStack)
		{
			item.Unhover();
		}
		_hoverStack.Clear();
		if (_mouseElement != null)
		{
			ClearMouseCapture();
			_mouseElement.OnMouseOut();
			_mouseElement = null;
		}
	}

	internal void ClearMouseCapture()
	{
		if (_mouseCaptureButton.HasValue)
		{
			_mouseElement.ReleaseMouseButton(_mouseCaptureButton.Value, _mouseCaptureClicks.Value, activate: false);
		}
		_isMouseOverCapturedElement = false;
		_mouseCaptureButton = null;
		_mouseCaptureClicks = null;
	}

	public void OnMouseDown(int button, int clicks)
	{
		if (_layerStack.Count != 0 && !IsMouseDragging)
		{
			Element focusedElement = FocusedElement;
			if (_mouseElement != null && !_mouseCaptureButton.HasValue)
			{
				_mouseCaptureButton = button;
				_mouseCaptureClicks = clicks;
				_isMouseOverCapturedElement = true;
				_mouseElement.PressMouseButton(button, clicks);
			}
			if (FocusedElement != null && focusedElement == FocusedElement && focusedElement != _mouseElement)
			{
				FocusElement(null, clearMouseCapture: false);
			}
		}
	}

	public void OnMouseUp(int button, int clicks)
	{
		if (_layerStack.Count == 0 || (_mouseCaptureButton.HasValue && button != _mouseCaptureButton))
		{
			return;
		}
		if (IsMouseDragging)
		{
			foreach (Element item in _dragOverStack)
			{
				item.OnMouseDragExit(_mouseDragData, _mouseDragElement);
			}
			_dragOverStack.Clear();
			object mouseDragData = _mouseDragData;
			_mouseDragData = null;
			Element mouseDragElement = _mouseDragElement;
			_mouseDragElement = null;
			_mouseDragElementToDraw = null;
			bool accepted = false;
			_dragOverElement?.OnMouseDrop(mouseDragData, mouseDragElement, out accepted);
			if (!accepted)
			{
				mouseDragElement.OnMouseDragCancel(mouseDragData);
			}
			else
			{
				mouseDragElement.OnMouseDragComplete(_mouseElement, mouseDragData);
			}
			RefreshHover();
		}
		if (_mouseCaptureButton.HasValue)
		{
			Element mouseElement = _mouseElement;
			bool isMouseOverCapturedElement = _isMouseOverCapturedElement;
			_mouseCaptureButton = null;
			_mouseCaptureClicks = null;
			_isMouseOverCapturedElement = false;
			RefreshHover();
			mouseElement.ReleaseMouseButton(button, clicks, isMouseOverCapturedElement);
		}
	}

	public void OnMouseMove(Point mousePosition)
	{
		MousePosition = mousePosition;
		RefreshHover();
	}

	internal void RefreshDragOver()
	{
		Debug.Assert(_mouseDragElement != null);
		Element interactiveLayer = GetInteractiveLayer();
		Element element = interactiveLayer.HitTest(MousePosition);
		if (element != null)
		{
			for (Element element2 = element; element2 != null; element2 = element2.Parent)
			{
				_upcomingDragOverStack.Add(element2);
			}
		}
		int num = 0;
		while (num < _dragOverStack.Count)
		{
			if (!_upcomingDragOverStack.Contains(_dragOverStack[num]))
			{
				_dragOverStack[num].OnMouseDragExit(_mouseDragData, _mouseDragElement);
				_dragOverStack.RemoveAt(num);
			}
			else
			{
				num++;
			}
		}
		foreach (Element item in _upcomingDragOverStack)
		{
			if (!_dragOverStack.Contains(item))
			{
				item.OnMouseDragEnter(_mouseDragData, _mouseDragElement);
			}
		}
		_dragOverStack.Clear();
		_dragOverStack.AddRange(_upcomingDragOverStack);
		_upcomingDragOverStack.Clear();
		_dragOverElement = element;
		element?.OnMouseDragMove();
	}

	internal void RefreshHover()
	{
		if (_mouseDragElement != null)
		{
			RefreshDragOver();
		}
		else
		{
			if (_layerStack.Count == 0 || !IsFocused)
			{
				return;
			}
			Element interactiveLayer = GetInteractiveLayer();
			Element element = interactiveLayer.HitTest(MousePosition);
			if (_mouseCaptureButton.HasValue)
			{
				_isMouseOverCapturedElement = _mouseElement == element;
				_mouseElement.MoveMouse();
				return;
			}
			if (element != null)
			{
				for (Element element2 = element; element2 != null; element2 = element2.Parent)
				{
					_upcomingHoverStack.Add(element2);
				}
			}
			int num = 0;
			while (num < _hoverStack.Count)
			{
				if (!_upcomingHoverStack.Contains(_hoverStack[num]))
				{
					_hoverStack[num].Unhover();
					_hoverStack.RemoveAt(num);
				}
				else
				{
					num++;
				}
			}
			foreach (Element item in _upcomingHoverStack)
			{
				if (!_hoverStack.Contains(item))
				{
					item.Hover();
				}
			}
			_hoverStack.Clear();
			_hoverStack.AddRange(_upcomingHoverStack);
			_upcomingHoverStack.Clear();
			if (element != _mouseElement)
			{
				_mouseElement?.OnMouseOut();
				element?.OnMouseIn();
			}
			_mouseElement = element;
			_isMouseOverCapturedElement = _mouseElement != null;
			_mouseElement?.MoveMouse();
		}
	}

	public void OnMouseWheel(Point offset)
	{
		if (_layerStack.Count != 0 && !_mouseCaptureButton.HasValue)
		{
			Element element = _mouseElement;
			while (element != null && !element.OnMouseWheel(offset))
			{
				element = element.Parent;
			}
			if (element != null)
			{
				RefreshHover();
			}
		}
	}

	public void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		UpdateKeys(keycode, isDown: true);
		(FocusedElement ?? GetInteractiveLayer())?.OnKeyDown(keycode, repeat);
	}

	public void OnKeyUp(SDL_Keycode keycode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		UpdateKeys(keycode, isDown: false);
		(FocusedElement ?? GetInteractiveLayer())?.OnKeyUp(keycode);
	}

	private void UpdateKeys(SDL_Keycode keycode, bool isDown)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected I4, but got Unknown
		switch (keycode - 1073742048)
		{
		case 1:
		case 5:
			IsShiftKeyDown = isDown;
			break;
		case 0:
		case 4:
			IsCtrlKeyDown = isDown;
			if (BuildInfo.Platform != Platform.MacOS)
			{
				bool isShortcutKeyDown = (IsWordSkipDown = isDown);
				IsShortcutKeyDown = isShortcutKeyDown;
			}
			break;
		case 3:
		case 7:
			IsGuiKeyDown = isDown;
			if (BuildInfo.Platform == Platform.MacOS)
			{
				IsShortcutKeyDown = isDown;
			}
			break;
		case 2:
		case 6:
			IsAltKeyDown = isDown;
			if (BuildInfo.Platform == Platform.MacOS)
			{
				IsWordSkipDown = isDown;
			}
			break;
		}
	}

	public void OnTextInput(string text)
	{
		(FocusedElement ?? GetInteractiveLayer())?.OnTextInput(text);
	}
}
