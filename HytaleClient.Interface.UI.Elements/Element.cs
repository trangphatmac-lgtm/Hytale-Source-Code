#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

public class Element
{
	private enum ScrollbarState
	{
		Default,
		Hovered,
		Dragged
	}

	public enum MouseWheelScrollBehaviourType
	{
		Default,
		VerticalOnly,
		HorizontalOnly
	}

	public readonly Desktop Desktop;

	public Element Parent;

	protected bool _waitingForLayoutAfterMount;

	private bool _visible = true;

	[UIMarkupProperty]
	public Anchor Anchor;

	[UIMarkupProperty]
	public Padding Padding;

	[UIMarkupProperty]
	public int FlexWeight = 0;

	protected Rectangle _containerRectangle;

	protected Rectangle _anchoredRectangle;

	protected Rectangle _rectangleAfterPadding;

	protected Rectangle _backgroundRectangle;

	protected LayoutMode _layoutMode = LayoutMode.Full;

	[UIMarkupProperty]
	public int? ContentWidth;

	[UIMarkupProperty]
	public int? ContentHeight;

	[UIMarkupProperty]
	public bool AutoScrollDown;

	[UIMarkupProperty]
	public bool KeepScrollPosition = false;

	protected Action _scrolled;

	protected ScrollbarStyle _scrollbarStyle = ScrollbarStyle.MakeDefault();

	private TexturePatch _scrollbarBackgroundPatch;

	private TexturePatch _scrollbarHandlePatch;

	private TexturePatch _scrollbarHoveredHandlePatch;

	private TexturePatch _scrollbarDraggedHandlePatch;

	private Point _scaledScrollArea;

	protected Point _scaledScrollOffset;

	private Point _scaledScrollSize;

	private ScrollbarState _horizontalScrollbarState;

	private ScrollbarState _verticalScrollbarState;

	[UIMarkupProperty]
	public MouseWheelScrollBehaviourType MouseWheelScrollBehaviour = MouseWheelScrollBehaviourType.Default;

	private int _horizontalScrollbarLength;

	private int _verticalScrollbarLength;

	private Rectangle _horizontalScrollbarHandleRectangle;

	private Rectangle _verticalScrollbarHandleRectangle;

	private Point _scrollbarOffsets;

	private int _scrollbarDragOffset;

	protected Rectangle _viewRectangle;

	protected Rectangle _contentRectangle;

	[UIMarkupProperty]
	public PatchStyle Background;

	protected TexturePatch _backgroundPatch;

	[UIMarkupProperty]
	public UIPath MaskTexturePath;

	[UIMarkupProperty]
	public UInt32Color OutlineColor;

	[UIMarkupProperty]
	public float OutlineSize;

	protected bool _hasTooltipText;

	private TextTooltipLayer _tooltip;

	[UIMarkupProperty]
	public bool Overscroll;

	public string Name;

	protected readonly List<Element> _children = new List<Element>();

	public bool IsMounted { get; private set; }

	public bool IsHovered { get; private set; }

	public int? CapturedMouseButton { get; private set; }

	[UIMarkupProperty]
	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			_visible = value;
			if (!_visible)
			{
				if (IsMounted)
				{
					Unmount();
				}
			}
			else if (!IsMounted)
			{
				Element parent = Parent;
				if (parent != null && parent.IsMounted)
				{
					Mount();
				}
			}
		}
	}

	public Rectangle ContainerRectangle => _containerRectangle;

	public Rectangle AnchoredRectangle => _anchoredRectangle;

	public Rectangle RectangleAfterPadding => _rectangleAfterPadding;

	public LayoutMode LayoutMode => _layoutMode;

	public Point ScaledScrollOffset => _scaledScrollOffset;

	public Point ScaledScrollSize => _scaledScrollSize;

	protected TextureArea _maskTextureArea { get; private set; }

	[UIMarkupProperty]
	public string TooltipText
	{
		set
		{
			if (_tooltip == null)
			{
				_tooltip = new TextTooltipLayer(Desktop);
			}
			_hasTooltipText = value != null;
			_tooltip.Text = value;
		}
	}

	[UIMarkupProperty]
	public TextTooltipStyle TextTooltipStyle
	{
		get
		{
			return _tooltip?.Style;
		}
		set
		{
			if (_tooltip == null)
			{
				_tooltip = new TextTooltipLayer(Desktop);
			}
			_tooltip.Style = value;
		}
	}

	[UIMarkupProperty]
	public float? TextTooltipShowDelay
	{
		get
		{
			return _tooltip?.ShowDelay;
		}
		set
		{
			if (_tooltip == null)
			{
				_tooltip = new TextTooltipLayer(Desktop);
			}
			if (value.HasValue)
			{
				_tooltip.ShowDelay = value.Value;
			}
		}
	}

	public IReadOnlyList<Element> Children => _children;

	public string GetPathInTree()
	{
		string text = GetType().Name;
		if (Name != null)
		{
			text = text + "#" + Name;
		}
		return (Parent == null) ? text : (Parent.GetPathInTree() + " > " + text);
	}

	public override string ToString()
	{
		return GetPathInTree();
	}

	internal virtual void AddFromMarkup(Element child)
	{
		Add(child);
	}

	public Element(Desktop desktop, Element parent)
	{
		Desktop = desktop;
		parent?.Add(this);
	}

	public void Add(Element child, int index = -1)
	{
		Debug.Assert(child.Parent == null, "Can't add element as child, it already has a parent.");
		child.Parent = this;
		if (index == -1)
		{
			index = _children.Count;
		}
		_children.Insert(index, child);
		if (IsMounted && child.Visible)
		{
			child.Mount();
		}
	}

	public void Add(Element child, Element before)
	{
		Add(child, _children.IndexOf(before));
	}

	public void Remove(Element child)
	{
		Debug.Assert(child.Parent == this, "Element isn't a child, can't unparent.");
		_children.Remove(child);
		child.Parent = null;
		if (child.IsMounted)
		{
			child.Unmount();
		}
	}

	public void RemoveAt(int index)
	{
		if ((uint)index >= (uint)_children.Count)
		{
			throw new IndexOutOfRangeException(index.ToString());
		}
		Element element = _children[index];
		Debug.Assert(element != null, "Element doesn't exist from index " + index + ", can't remove.");
		Remove(element);
	}

	public void Reorder(Element child, int index = -1)
	{
		Debug.Assert(child.Parent == this, "Element isn't a child, can't reorder.");
		_children.Remove(child);
		if (index == -1)
		{
			index = _children.Count;
		}
		_children.Insert(index, child);
	}

	public void Clear()
	{
		foreach (Element child in _children)
		{
			if (child.IsMounted)
			{
				child.Unmount();
			}
			child.Parent = null;
		}
		_children.Clear();
	}

	public T Find<T>(string name) where T : Element
	{
		foreach (Element child in _children)
		{
			if (child.Name == name)
			{
				return child as T;
			}
			T val = child.Find<T>(name);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	internal void Mount()
	{
		Debug.Assert(!IsMounted);
		IsMounted = true;
		_waitingForLayoutAfterMount = true;
		foreach (Element child in _children)
		{
			if (!child.IsMounted && child.Visible)
			{
				child.Mount();
			}
		}
		OnMounted();
	}

	internal void Unmount()
	{
		Debug.Assert(IsMounted);
		IsMounted = false;
		if (!KeepScrollPosition)
		{
			_scaledScrollOffset = Point.Zero;
		}
		_horizontalScrollbarState = ScrollbarState.Default;
		_verticalScrollbarState = ScrollbarState.Default;
		foreach (Element child in _children)
		{
			if (child.IsMounted)
			{
				child.Unmount();
			}
		}
		if (IsHovered)
		{
			Desktop.RefreshHover();
		}
		if (CapturedMouseButton.HasValue)
		{
			Desktop.ClearMouseCapture();
		}
		_tooltip?.Stop();
		OnUnmounted();
	}

	protected virtual void OnMounted()
	{
	}

	protected virtual void OnUnmounted()
	{
	}

	internal void Hover()
	{
		Debug.Assert(!IsHovered);
		IsHovered = true;
		OnMouseEnter();
		if (_hasTooltipText)
		{
			_tooltip.Start();
		}
	}

	internal void Unhover()
	{
		Debug.Assert(IsHovered);
		IsHovered = false;
		_horizontalScrollbarState = ScrollbarState.Default;
		_verticalScrollbarState = ScrollbarState.Default;
		OnMouseLeave();
		_tooltip?.Stop();
	}

	protected virtual void OnMouseEnter()
	{
	}

	protected virtual void OnMouseLeave()
	{
	}

	protected internal virtual void OnFocus()
	{
	}

	protected internal virtual void OnBlur()
	{
	}

	protected internal virtual void Validate()
	{
		Parent?.Validate();
	}

	protected internal virtual void Dismiss()
	{
		Parent?.Dismiss();
	}

	internal void PressMouseButton(int button, int clicks)
	{
		Debug.Assert(!CapturedMouseButton.HasValue);
		CapturedMouseButton = button;
		if ((long)button == 1)
		{
			if (_scaledScrollArea.X > 0 && _horizontalScrollbarHandleRectangle.Contains(Desktop.MousePosition))
			{
				_horizontalScrollbarState = ScrollbarState.Dragged;
				_scrollbarDragOffset = _scrollbarOffsets.X - Desktop.MousePosition.X;
				return;
			}
			if (_scaledScrollArea.Y > 0 && _verticalScrollbarHandleRectangle.Contains(Desktop.MousePosition))
			{
				_verticalScrollbarState = ScrollbarState.Dragged;
				_scrollbarDragOffset = _scrollbarOffsets.Y - Desktop.MousePosition.Y;
				return;
			}
		}
		OnMouseButtonDown(new MouseButtonEvent(button, clicks));
	}

	internal void ReleaseMouseButton(int button, int clicks, bool activate)
	{
		Debug.Assert(CapturedMouseButton == button);
		CapturedMouseButton = null;
		if ((long)button == 1)
		{
			if (_horizontalScrollbarState == ScrollbarState.Dragged)
			{
				_horizontalScrollbarState = ((_scaledScrollArea.X > 0 && _horizontalScrollbarHandleRectangle.Contains(Desktop.MousePosition)) ? ScrollbarState.Hovered : ScrollbarState.Default);
				return;
			}
			if (_verticalScrollbarState == ScrollbarState.Dragged)
			{
				_verticalScrollbarState = ((_scaledScrollArea.Y > 0 && _verticalScrollbarHandleRectangle.Contains(Desktop.MousePosition)) ? ScrollbarState.Hovered : ScrollbarState.Default);
				return;
			}
		}
		OnMouseButtonUp(new MouseButtonEvent(button, clicks), activate);
	}

	internal void MoveMouse()
	{
		if (_horizontalScrollbarState == ScrollbarState.Dragged)
		{
			int x = _scaledScrollOffset.X;
			int num = Desktop.MousePosition.X + _scrollbarDragOffset;
			_scaledScrollOffset.X = _scaledScrollArea.X * num / _viewRectangle.Width;
			ComputeScrollbars();
			Point point = new Point(_scaledScrollOffset.X - x, 0);
			if (point == Point.Zero)
			{
				return;
			}
			{
				foreach (Element child in _children)
				{
					child.ApplyParentScroll(point);
				}
				return;
			}
		}
		_horizontalScrollbarState = ((_scaledScrollArea.X > 0 && _horizontalScrollbarHandleRectangle.Contains(Desktop.MousePosition)) ? ScrollbarState.Hovered : ScrollbarState.Default);
		if (_verticalScrollbarState == ScrollbarState.Dragged)
		{
			int y = _scaledScrollOffset.Y;
			int num2 = Desktop.MousePosition.Y + _scrollbarDragOffset;
			_scaledScrollOffset.Y = _scaledScrollArea.Y * num2 / _viewRectangle.Height;
			ComputeScrollbars();
			Point point2 = new Point(0, _scaledScrollOffset.Y - y);
			if (point2 == Point.Zero)
			{
				return;
			}
			{
				foreach (Element child2 in _children)
				{
					child2.ApplyParentScroll(point2);
				}
				return;
			}
		}
		_verticalScrollbarState = ((_scaledScrollArea.Y > 0 && _verticalScrollbarHandleRectangle.Contains(Desktop.MousePosition)) ? ScrollbarState.Hovered : ScrollbarState.Default);
		OnMouseMove();
	}

	public virtual void OnMouseOut()
	{
	}

	public virtual void OnMouseIn()
	{
	}

	protected virtual void OnMouseButtonDown(MouseButtonEvent evt)
	{
	}

	protected virtual void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
	}

	protected virtual void OnMouseMove()
	{
	}

	protected internal virtual bool OnMouseWheel(Point offset)
	{
		return MouseWheelScrollBehaviour switch
		{
			MouseWheelScrollBehaviourType.HorizontalOnly => Scroll((float)((offset.X != 0) ? offset.X : offset.Y) * 30f, 0f), 
			MouseWheelScrollBehaviourType.VerticalOnly => Scroll(0f, (float)((offset.Y != 0) ? offset.Y : offset.X) * 30f), 
			_ => Scroll((float)offset.X * 30f, (float)offset.Y * 30f), 
		};
	}

	public bool Scroll(float x, float y)
	{
		if (_scaledScrollArea == Point.Zero)
		{
			return false;
		}
		Point scaledScrollOffset = _scaledScrollOffset;
		_scaledScrollOffset.X -= Desktop.ScaleRound(x);
		_scaledScrollOffset.Y -= Desktop.ScaleRound(y);
		ComputeScrollbars();
		Point point = _scaledScrollOffset - scaledScrollOffset;
		if (point == Point.Zero)
		{
			return false;
		}
		foreach (Element child in _children)
		{
			child.ApplyParentScroll(point);
		}
		return true;
	}

	public void ScrollChildElementIntoView(Element element)
	{
		Debug.Assert(IsMounted);
		Debug.Assert(!_waitingForLayoutAfterMount);
		Debug.Assert(!element._waitingForLayoutAfterMount);
		if (LayoutMode == LayoutMode.TopScrolling || LayoutMode == LayoutMode.BottomScrolling)
		{
			if (_viewRectangle.Top > element.AnchoredRectangle.Top)
			{
				int value = element.AnchoredRectangle.Top - _viewRectangle.Top + _scaledScrollOffset.Y;
				int? y = value;
				SetScroll(null, y);
			}
			else if (element.AnchoredRectangle.Bottom > _viewRectangle.Bottom)
			{
				int value2 = element.AnchoredRectangle.Bottom - _viewRectangle.Bottom + _scaledScrollOffset.Y;
				int? y = value2;
				SetScroll(null, y);
			}
		}
		else if (LayoutMode == LayoutMode.LeftScrolling || LayoutMode == LayoutMode.RightScrolling)
		{
			if (_viewRectangle.Left > element.AnchoredRectangle.Left)
			{
				int value3 = element.AnchoredRectangle.Left - _viewRectangle.Left + _scaledScrollOffset.X;
				SetScroll(value3);
			}
			else if (element.AnchoredRectangle.Right > _viewRectangle.Right)
			{
				int value4 = element.AnchoredRectangle.Right - _viewRectangle.Right + _scaledScrollOffset.X;
				SetScroll(value4);
			}
		}
		else
		{
			Debug.Fail("Incompatible LayoutMode: " + LayoutMode);
		}
	}

	protected internal virtual void OnMouseDragEnter(object data, Element sourceElement)
	{
	}

	protected internal virtual void OnMouseDragExit(object data, Element sourceElement)
	{
	}

	protected internal virtual void OnMouseDragMove()
	{
	}

	protected internal virtual void OnMouseDrop(object data, Element sourceElement, out bool accepted)
	{
		accepted = false;
	}

	protected internal virtual void OnMouseDragCancel(object data)
	{
	}

	protected internal virtual void OnMouseDragComplete(Element element, object data)
	{
	}

	protected internal virtual void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		if ((int)keycode == 13 || (int)keycode == 1073741912)
		{
			Validate();
		}
		else if ((int)keycode == 27)
		{
			Dismiss();
		}
	}

	protected internal virtual void OnKeyUp(SDL_Keycode keycode)
	{
	}

	protected internal virtual void OnTextInput(string text)
	{
	}

	public void PrepareForDraw()
	{
		if (!Visible)
		{
			return;
		}
		Debug.Assert(!_waitingForLayoutAfterMount, "Element was mounted but not laid out", "{0}", this);
		if (_maskTextureArea != null)
		{
			Desktop.Batcher2D.PushMask(_maskTextureArea, _anchoredRectangle, Desktop.ViewportRectangle);
		}
		PrepareForDrawSelf();
		if (_scaledScrollArea != Point.Zero)
		{
			Desktop.Batcher2D.PushScissor(_rectangleAfterPadding);
		}
		PrepareForDrawContent();
		if (_scaledScrollArea != Point.Zero)
		{
			Desktop.Batcher2D.PopScissor();
		}
		if (_scaledScrollArea != Point.Zero && (!_scrollbarStyle.OnlyVisibleWhenHovered || IsHovered))
		{
			int num = Desktop.ScaleRound(_scrollbarStyle.Size);
			if (_scaledScrollArea.X > 0)
			{
				int num2 = _viewRectangle.Width - _horizontalScrollbarLength;
				if (_scrollbarBackgroundPatch != null)
				{
					Desktop.Batcher2D.RequestDrawPatch(_scrollbarBackgroundPatch, new Rectangle(_rectangleAfterPadding.X, _rectangleAfterPadding.Bottom - num, _rectangleAfterPadding.Width, num), Desktop.Scale);
				}
				if (num2 > 0)
				{
					TexturePatch scrollbarStatePatch = GetScrollbarStatePatch(_horizontalScrollbarState);
					if (scrollbarStatePatch != null)
					{
						Desktop.Batcher2D.RequestDrawPatch(scrollbarStatePatch, _horizontalScrollbarHandleRectangle, Desktop.Scale);
					}
				}
			}
			if (_scaledScrollArea.Y > 0)
			{
				int num3 = _viewRectangle.Height - _verticalScrollbarLength;
				if (_scrollbarBackgroundPatch != null)
				{
					Desktop.Batcher2D.RequestDrawPatch(_scrollbarBackgroundPatch, new Rectangle(_rectangleAfterPadding.Right - num, _rectangleAfterPadding.Y, num, _rectangleAfterPadding.Height), Desktop.Scale);
				}
				if (num3 > 0)
				{
					TexturePatch scrollbarStatePatch2 = GetScrollbarStatePatch(_verticalScrollbarState);
					if (scrollbarStatePatch2 != null)
					{
						Desktop.Batcher2D.RequestDrawPatch(scrollbarStatePatch2, _verticalScrollbarHandleRectangle, Desktop.Scale);
					}
				}
			}
		}
		if (_maskTextureArea != null)
		{
			Desktop.Batcher2D.PopMask();
		}
	}

	protected virtual void PrepareForDrawContent()
	{
		foreach (Element child in _children)
		{
			child.PrepareForDraw();
		}
	}

	protected virtual void PrepareForDrawSelf()
	{
		if (_backgroundPatch != null)
		{
			Desktop.Batcher2D.RequestDrawPatch(_backgroundPatch, _backgroundRectangle, Desktop.Scale);
		}
		if (OutlineSize > 0f)
		{
			TextureArea whitePixel = Desktop.Provider.WhitePixel;
			Desktop.Batcher2D.RequestDrawOutline(whitePixel.Texture, whitePixel.Rectangle, _anchoredRectangle, OutlineSize * Desktop.Scale, OutlineColor);
		}
	}

	private TexturePatch GetScrollbarStatePatch(ScrollbarState state)
	{
		return state switch
		{
			ScrollbarState.Hovered => _scrollbarHoveredHandlePatch ?? _scrollbarHandlePatch, 
			ScrollbarState.Dragged => _scrollbarDraggedHandlePatch ?? _scrollbarHandlePatch, 
			_ => _scrollbarHandlePatch, 
		};
	}

	public virtual void PrepareForDrawOutline()
	{
		if (!Visible)
		{
			return;
		}
		UInt32Color color = UInt32Color.FromRGBA(4278190144u);
		if (CapturedMouseButton.HasValue)
		{
			color = UInt32Color.FromRGBA(16711808u);
		}
		else if (IsHovered)
		{
			color = UInt32Color.FromRGBA(65408u);
		}
		TextureArea whitePixel = Desktop.Provider.WhitePixel;
		Desktop.Batcher2D.RequestDrawOutline(whitePixel.Texture, whitePixel.Rectangle, _anchoredRectangle, 1f, color);
		foreach (Element child in _children)
		{
			child.PrepareForDrawOutline();
		}
	}

	protected Point ComputeScaledAnchorAndPaddingSize(int? maxWidth)
	{
		int num = Anchor.Left.GetValueOrDefault() + Anchor.Right.GetValueOrDefault();
		num = ((!Anchor.MaxWidth.HasValue) ? (num + (Anchor.Width ?? (Padding.Left.GetValueOrDefault() + Padding.Right.GetValueOrDefault()))) : (num + System.Math.Min(Anchor.MaxWidth.Value, maxWidth.GetValueOrDefault(int.MaxValue))));
		int num2 = Anchor.Top.GetValueOrDefault() + Anchor.Bottom.GetValueOrDefault();
		num2 += Anchor.Height ?? (Padding.Top.GetValueOrDefault() + Padding.Bottom.GetValueOrDefault());
		return new Point(Desktop.ScaleRound(num), Desktop.ScaleRound(num2));
	}

	public virtual Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		Point zero = Point.Zero;
		int num = Desktop.ScaleRound(_scrollbarStyle.Size + _scrollbarStyle.Spacing);
		bool flag = _layoutMode == LayoutMode.LeftScrolling || _layoutMode == LayoutMode.RightScrolling;
		bool flag2 = _layoutMode == LayoutMode.TopScrolling || _layoutMode == LayoutMode.BottomScrolling;
		int? num2 = (flag ? null : maxWidth);
		if (!flag && (Anchor.MaxWidth.HasValue || Anchor.Width.HasValue))
		{
			num2 = Desktop.ScaleRound(Anchor.MaxWidth ?? Anchor.Width.Value);
		}
		else if (num2.HasValue && flag2)
		{
			num2 -= num;
		}
		int? maxHeight2 = (flag2 ? null : maxHeight);
		if (!flag2 && Anchor.Height.HasValue)
		{
			maxHeight2 = Desktop.ScaleRound(Anchor.Height.Value);
		}
		if (num2.HasValue)
		{
			int num3 = Desktop.ScaleRound(Padding.Left.GetValueOrDefault() + Padding.Right.GetValueOrDefault());
			num2 -= num3;
		}
		switch (_layoutMode)
		{
		case LayoutMode.Full:
			foreach (Element child in _children)
			{
				if (child.Visible)
				{
					Point point3 = child.ComputeScaledMinSize(num2, maxHeight2);
					if (point3.X > zero.X)
					{
						zero.X = point3.X;
					}
					if (point3.Y > zero.Y)
					{
						zero.Y = point3.Y;
					}
				}
			}
			break;
		case LayoutMode.Left:
		case LayoutMode.Center:
		case LayoutMode.Right:
		case LayoutMode.LeftScrolling:
		case LayoutMode.RightScrolling:
		case LayoutMode.CenterMiddle:
			foreach (Element child2 in _children)
			{
				if (child2.Visible)
				{
					Point point2 = child2.ComputeScaledMinSize(num2, maxHeight2);
					zero.X += point2.X;
					if (point2.Y > zero.Y)
					{
						zero.Y = point2.Y;
					}
				}
			}
			break;
		case LayoutMode.Top:
		case LayoutMode.Middle:
		case LayoutMode.Bottom:
		case LayoutMode.TopScrolling:
		case LayoutMode.BottomScrolling:
		case LayoutMode.MiddleCenter:
			foreach (Element child3 in _children)
			{
				if (child3.Visible)
				{
					Point point = child3.ComputeScaledMinSize(num2, maxHeight2);
					zero.Y += point.Y;
					if (point.X > zero.X)
					{
						zero.X = point.X;
					}
				}
			}
			break;
		default:
			throw new NotImplementedException();
		}
		Point result = ComputeScaledAnchorAndPaddingSize(maxWidth);
		if (!Anchor.Width.HasValue && !Anchor.MaxWidth.HasValue)
		{
			result.X += zero.X;
			if (_layoutMode == LayoutMode.TopScrolling || _layoutMode == LayoutMode.BottomScrolling)
			{
				result.X += num;
			}
			if (maxWidth.HasValue)
			{
				result.X = System.Math.Min(result.X, maxWidth.Value);
			}
		}
		if (Anchor.MinWidth.HasValue)
		{
			result.X = System.Math.Max(Desktop.ScaleRound(Anchor.MinWidth.Value), result.X);
		}
		if (!Anchor.Height.HasValue)
		{
			result.Y += zero.Y;
			if (_layoutMode == LayoutMode.LeftScrolling || _layoutMode == LayoutMode.RightScrolling)
			{
				result.Y += num;
			}
			if (maxHeight.HasValue)
			{
				result.Y = System.Math.Min(result.Y, maxHeight.Value);
			}
		}
		return result;
	}

	internal void Rescale(float scaleRatio)
	{
		_scaledScrollOffset = new Point(MathHelper.Round((float)_scaledScrollOffset.X * scaleRatio), MathHelper.Round((float)_scaledScrollOffset.Y * scaleRatio));
		foreach (Element child in _children)
		{
			child.Rescale(scaleRatio);
		}
	}

	protected virtual float? GetScaledWidth()
	{
		if (Anchor.MaxWidth.HasValue)
		{
			return System.Math.Min((float)Anchor.MaxWidth.Value * Desktop.Scale, _containerRectangle.Width);
		}
		if (Anchor.Width.HasValue)
		{
			return (float)Anchor.Width.Value * Desktop.Scale;
		}
		return null;
	}

	protected virtual float? GetScaledHeight()
	{
		if (Anchor.Height.HasValue)
		{
			return (float)Anchor.Height.Value * Desktop.Scale;
		}
		return null;
	}

	public void Layout(Rectangle? containerRectangle = null, bool layoutChildren = true)
	{
		if (containerRectangle.HasValue)
		{
			_containerRectangle = containerRectangle.Value;
			_waitingForLayoutAfterMount = false;
		}
		if (!IsMounted || _waitingForLayoutAfterMount)
		{
			return;
		}
		ApplyStyles();
		float num = (float)_containerRectangle.Left + (float)Anchor.Left.GetValueOrDefault() * Desktop.Scale;
		float num2 = (float)_containerRectangle.Right - (float)Anchor.Right.GetValueOrDefault() * Desktop.Scale;
		float num3 = (float)_containerRectangle.Top + (float)Anchor.Top.GetValueOrDefault() * Desktop.Scale;
		float num4 = (float)_containerRectangle.Bottom - (float)Anchor.Bottom.GetValueOrDefault() * Desktop.Scale;
		float? scaledWidth = GetScaledWidth();
		if (scaledWidth.HasValue)
		{
			float value = scaledWidth.Value;
			if (Anchor.Left.HasValue)
			{
				if (!Anchor.Right.HasValue)
				{
					num2 = num + value;
				}
			}
			else if (Anchor.Right.HasValue)
			{
				num = num2 - value;
			}
			else
			{
				num = (float)_containerRectangle.Center.X - value / 2f;
				num2 = num + value;
			}
		}
		float? scaledHeight = GetScaledHeight();
		if (scaledHeight.HasValue)
		{
			if (Anchor.Top.HasValue)
			{
				if (!Anchor.Bottom.HasValue)
				{
					num4 = num3 + scaledHeight.Value;
				}
			}
			else if (Anchor.Bottom.HasValue)
			{
				num3 = num4 - scaledHeight.Value;
			}
			else
			{
				num3 = (float)_containerRectangle.Center.Y - scaledHeight.Value / 2f;
				num4 = num3 + scaledHeight.Value;
			}
		}
		int num5 = MathHelper.Round(num);
		int num6 = MathHelper.Round(num2);
		int num7 = MathHelper.Round(num3);
		int num8 = MathHelper.Round(num4);
		int num9 = MathHelper.Round(num + (float)Padding.Left.GetValueOrDefault() * Desktop.Scale);
		int num10 = MathHelper.Round(num2 - (float)Padding.Right.GetValueOrDefault() * Desktop.Scale);
		int num11 = MathHelper.Round(num3 + (float)Padding.Top.GetValueOrDefault() * Desktop.Scale);
		int num12 = MathHelper.Round(num4 - (float)Padding.Bottom.GetValueOrDefault() * Desktop.Scale);
		_anchoredRectangle = new Rectangle(num5, num7, num6 - num5, num8 - num7);
		_rectangleAfterPadding = new Rectangle(num9, num11, num10 - num9, num12 - num11);
		PatchStyle background = Background;
		if (background != null && background.Anchor.HasValue)
		{
			Anchor value2 = Background.Anchor.Value;
			float num13 = (float)_anchoredRectangle.Left + (float)value2.Left.GetValueOrDefault() * Desktop.Scale;
			float num14 = (float)_anchoredRectangle.Right - (float)value2.Right.GetValueOrDefault() * Desktop.Scale;
			float num15 = (float)_anchoredRectangle.Top + (float)value2.Top.GetValueOrDefault() * Desktop.Scale;
			float num16 = (float)_anchoredRectangle.Bottom - (float)value2.Bottom.GetValueOrDefault() * Desktop.Scale;
			if (value2.Width.HasValue)
			{
				float num17 = (float)value2.Width.Value * Desktop.Scale;
				if (value2.Left.HasValue)
				{
					if (!value2.Right.HasValue)
					{
						num14 = num13 + num17;
					}
				}
				else if (value2.Right.HasValue)
				{
					num13 = num14 - num17;
				}
				else
				{
					num13 = (float)_anchoredRectangle.Center.X - num17 / 2f;
					num14 = num13 + num17;
				}
			}
			if (value2.Height.HasValue)
			{
				float num18 = (float)value2.Height.Value * Desktop.Scale;
				if (value2.Top.HasValue)
				{
					if (!value2.Bottom.HasValue)
					{
						num16 = num15 + num18;
					}
				}
				else if (value2.Bottom.HasValue)
				{
					num15 = num16 - num18;
				}
				else
				{
					num15 = (float)_anchoredRectangle.Center.Y - num18 / 2f;
					num16 = num15 + num18;
				}
			}
			_backgroundRectangle = new Rectangle(MathHelper.Round(num13), MathHelper.Round(num15), MathHelper.Round(num14 - num13), MathHelper.Round(num16 - num15));
		}
		else
		{
			_backgroundRectangle = _anchoredRectangle;
		}
		LayoutSelf();
		if (!(_children.Count == 0 && !ContentWidth.HasValue && !ContentHeight.HasValue && layoutChildren))
		{
			LayoutChildren();
			AfterChildrenLayout();
		}
	}

	protected void LayoutChildren()
	{
		int? num = _rectangleAfterPadding.Width;
		int? num2 = _rectangleAfterPadding.Height;
		int num3 = Desktop.ScaleRound(_scrollbarStyle.Size + _scrollbarStyle.Spacing);
		_scaledScrollArea = new Point(Desktop.ScaleRound(ContentWidth.GetValueOrDefault()), Desktop.ScaleRound(ContentHeight.GetValueOrDefault()));
		bool flag = false;
		switch (_layoutMode)
		{
		case LayoutMode.LeftScrolling:
		case LayoutMode.RightScrolling:
		{
			flag = true;
			num = null;
			num2 -= num3;
			int num5 = 0;
			foreach (Element child in _children)
			{
				num5 += (child.Visible ? child.ComputeScaledMinSize(null, num2).X : 0);
			}
			_scaledScrollArea.X = num5;
			if (Overscroll)
			{
				_scaledScrollArea.Y += _rectangleAfterPadding.Height;
			}
			break;
		}
		case LayoutMode.TopScrolling:
		case LayoutMode.BottomScrolling:
		{
			flag = true;
			num2 = null;
			num -= num3;
			int num4 = 0;
			foreach (Element child2 in _children)
			{
				num4 += (child2.Visible ? child2.ComputeScaledMinSize(num, null).Y : 0);
			}
			_scaledScrollArea.Y = num4;
			if (Overscroll)
			{
				_scaledScrollArea.Y += _rectangleAfterPadding.Height;
			}
			break;
		}
		}
		_viewRectangle = _rectangleAfterPadding;
		if (_scaledScrollArea != Point.Zero)
		{
			if (_scaledScrollArea.X > 0)
			{
				_viewRectangle.Height -= num3;
			}
			if (_scaledScrollArea.Y > 0)
			{
				_viewRectangle.Width -= num3;
			}
			if (_scaledScrollArea.X > 0)
			{
				_horizontalScrollbarLength = System.Math.Max(10, MathHelper.Round((float)_viewRectangle.Width * System.Math.Min(1f, (float)_viewRectangle.Width / (float)_scaledScrollArea.X)));
			}
			if (_scaledScrollArea.Y > 0)
			{
				_verticalScrollbarLength = System.Math.Max(10, MathHelper.Round((float)_viewRectangle.Height * System.Math.Min(1f, (float)_viewRectangle.Height / (float)_scaledScrollArea.Y)));
			}
			ComputeScrollbars();
		}
		_contentRectangle = _rectangleAfterPadding;
		if (_scaledScrollArea.X > 0)
		{
			if (_layoutMode == LayoutMode.LeftScrolling)
			{
				_contentRectangle.Width = _scaledScrollArea.X;
			}
			else if (_layoutMode == LayoutMode.RightScrolling)
			{
				_contentRectangle.Width = System.Math.Max(_rectangleAfterPadding.Width, _scaledScrollArea.X);
			}
		}
		if (_scaledScrollArea.Y > 0)
		{
			if (_layoutMode == LayoutMode.TopScrolling)
			{
				_contentRectangle.Height = _scaledScrollArea.Y;
			}
			else if (_layoutMode == LayoutMode.BottomScrolling)
			{
				_contentRectangle.Height = System.Math.Max(_rectangleAfterPadding.Height, _scaledScrollArea.Y);
			}
		}
		if (_scaledScrollArea.X > 0)
		{
			_contentRectangle.Height -= num3;
		}
		if (_scaledScrollArea.Y > 0)
		{
			_contentRectangle.Width -= num3;
		}
		_contentRectangle.Offset(-_scaledScrollOffset.X, -_scaledScrollOffset.Y);
		switch (_layoutMode)
		{
		case LayoutMode.Full:
		{
			foreach (Element child3 in _children)
			{
				if (child3.Visible)
				{
					child3.Layout(_contentRectangle);
				}
			}
			break;
		}
		case LayoutMode.Left:
		case LayoutMode.Center:
		case LayoutMode.LeftScrolling:
		case LayoutMode.CenterMiddle:
		{
			float num25 = 0f;
			int num26 = 0;
			bool flag3 = false;
			foreach (Element child4 in _children)
			{
				if (!child4.Visible)
				{
					continue;
				}
				if (!flag && child4.FlexWeight > 0)
				{
					num26 += child4.FlexWeight;
					if (child4.Anchor.MinWidth.HasValue)
					{
						flag3 = true;
					}
				}
				else
				{
					num25 += (float)child4.ComputeScaledMinSize(num, num2).X;
				}
			}
			float num27 = System.Math.Max(0f, (float)_contentRectangle.Width - num25);
			if (flag3)
			{
				foreach (Element child5 in _children)
				{
					if (child5.Visible && child5.FlexWeight > 0 && child5.Anchor.MinWidth.HasValue)
					{
						int num28 = (int)(num27 * (float)child5.FlexWeight / (float)num26);
						int num29 = Desktop.ScaleRound(child5.Anchor.MinWidth.Value);
						if (num28 < num29)
						{
							num26 -= child5.FlexWeight;
							num27 -= (float)num29;
						}
					}
				}
			}
			int num30 = _contentRectangle.X;
			if ((_layoutMode == LayoutMode.Center || _layoutMode == LayoutMode.CenterMiddle) && num26 == 0)
			{
				num30 += (_contentRectangle.Width - (int)num25) / 2;
			}
			{
				foreach (Element child6 in _children)
				{
					if (!child6.Visible)
					{
						continue;
					}
					Point point3 = child6.ComputeScaledMinSize(num, num2);
					if (!flag && child6.FlexWeight > 0)
					{
						int num31 = (int)(num27 * (float)child6.FlexWeight / (float)num26);
						if (child6.Anchor.MinWidth.HasValue)
						{
							int num32 = Desktop.ScaleRound(child6.Anchor.MinWidth.Value);
							point3.X = ((num31 > num32) ? num31 : num32);
						}
						else
						{
							point3.X = num31;
						}
					}
					if (_layoutMode != LayoutMode.CenterMiddle)
					{
						point3.Y = _contentRectangle.Height;
					}
					child6.Layout(new Rectangle(num30, _contentRectangle.Center.Y - point3.Y / 2, point3.X, point3.Y));
					num30 += point3.X;
				}
				break;
			}
		}
		case LayoutMode.Right:
		case LayoutMode.RightScrolling:
		{
			float num12 = 0f;
			int num13 = 0;
			bool flag2 = false;
			foreach (Element child7 in _children)
			{
				if (child7.Visible)
				{
					if (!flag && child7.FlexWeight > 0)
					{
						num13 += child7.FlexWeight;
					}
					else
					{
						num12 += (float)child7.ComputeScaledMinSize(num, num2).X;
					}
				}
			}
			float num14 = System.Math.Max(0f, (float)_contentRectangle.Width - num12);
			if (flag2)
			{
				foreach (Element child8 in _children)
				{
					if (child8.Visible && child8.FlexWeight > 0 && child8.Anchor.MinWidth.HasValue)
					{
						int num15 = (int)(num14 * (float)child8.FlexWeight / (float)num13);
						int num16 = Desktop.ScaleRound(child8.Anchor.MinWidth.Value);
						if (num15 < num16)
						{
							num13 -= child8.FlexWeight;
							num14 -= (float)num16;
						}
					}
				}
			}
			int num17 = _contentRectangle.Right;
			for (int num18 = _children.Count - 1; num18 >= 0; num18--)
			{
				Element element2 = _children[num18];
				if (element2.Visible)
				{
					Point point = element2.ComputeScaledMinSize(num, num2);
					if (!flag && element2.FlexWeight > 0)
					{
						int num19 = (int)(num14 * (float)element2.FlexWeight / (float)num13);
						if (element2.Anchor.MinWidth.HasValue)
						{
							int num20 = Desktop.ScaleRound(element2.Anchor.MinWidth.Value);
							point.X = ((num19 > num20) ? num19 : num20);
						}
						else
						{
							point.X = num19;
						}
					}
					num17 -= point.X;
					element2.Layout(new Rectangle(num17, _contentRectangle.Y, point.X, _contentRectangle.Height));
				}
			}
			break;
		}
		case LayoutMode.Top:
		case LayoutMode.Middle:
		case LayoutMode.TopScrolling:
		case LayoutMode.MiddleCenter:
		{
			float num21 = 0f;
			int num22 = 0;
			foreach (Element child9 in _children)
			{
				if (child9.Visible)
				{
					if (!flag && child9.FlexWeight > 0)
					{
						num22 += child9.FlexWeight;
					}
					else
					{
						num21 += (float)child9.ComputeScaledMinSize(num, num2).Y;
					}
				}
			}
			float num23 = System.Math.Max(0f, (float)_contentRectangle.Height - num21);
			int num24 = _contentRectangle.Y;
			if ((_layoutMode == LayoutMode.Middle || _layoutMode == LayoutMode.MiddleCenter) && num22 == 0)
			{
				num24 += (_contentRectangle.Height - (int)num21) / 2;
			}
			{
				foreach (Element child10 in _children)
				{
					if (child10.Visible)
					{
						Point point2 = child10.ComputeScaledMinSize(num, num2);
						if (!flag && child10.FlexWeight > 0)
						{
							point2.Y = (int)(num23 * (float)child10.FlexWeight / (float)num22);
						}
						if (_layoutMode != LayoutMode.MiddleCenter)
						{
							point2.X = _contentRectangle.Width;
						}
						child10.Layout(new Rectangle(_contentRectangle.Center.X - point2.X / 2, num24, point2.X, point2.Y));
						num24 += point2.Y;
					}
				}
				break;
			}
		}
		case LayoutMode.Bottom:
		case LayoutMode.BottomScrolling:
		{
			float num6 = 0f;
			int num7 = 0;
			foreach (Element child11 in _children)
			{
				if (child11.Visible)
				{
					if (!flag && child11.FlexWeight > 0)
					{
						num7 += child11.FlexWeight;
					}
					else
					{
						num6 += (float)child11.ComputeScaledMinSize(num, num2).Y;
					}
				}
			}
			float num8 = System.Math.Max(0f, (float)_contentRectangle.Height - num6);
			int num9 = _contentRectangle.Bottom;
			for (int num10 = _children.Count - 1; num10 >= 0; num10--)
			{
				Element element = _children[num10];
				if (element.Visible)
				{
					int num11 = ((flag || element.FlexWeight <= 0) ? element.ComputeScaledMinSize(num, num2).Y : ((int)(num8 * (float)element.FlexWeight / (float)num7)));
					num9 -= num11;
					element.Layout(new Rectangle(_contentRectangle.X, num9, _contentRectangle.Width, num11));
				}
			}
			break;
		}
		}
	}

	protected virtual void ApplyStyles()
	{
		_backgroundPatch = ((Background != null) ? Desktop.MakeTexturePatch(Background) : null);
		_maskTextureArea = ((MaskTexturePath != null) ? Desktop.Provider.MakeTextureArea(MaskTexturePath.Value) : null);
		_scrollbarBackgroundPatch = ((_scrollbarStyle.Background != null) ? Desktop.MakeTexturePatch(_scrollbarStyle.Background) : null);
		_scrollbarHandlePatch = ((_scrollbarStyle.Handle != null) ? Desktop.MakeTexturePatch(_scrollbarStyle.Handle) : null);
		_scrollbarHoveredHandlePatch = ((_scrollbarStyle.HoveredHandle != null) ? Desktop.MakeTexturePatch(_scrollbarStyle.HoveredHandle) : null);
		_scrollbarDraggedHandlePatch = ((_scrollbarStyle.DraggedHandle != null) ? Desktop.MakeTexturePatch(_scrollbarStyle.DraggedHandle) : null);
	}

	protected virtual void LayoutSelf()
	{
	}

	protected virtual void AfterChildrenLayout()
	{
	}

	public void SetScroll(int? x = null, int? y = null)
	{
		Point scaledScrollOffset = _scaledScrollOffset;
		_scaledScrollOffset = new Point(x ?? _scaledScrollOffset.X, y ?? _scaledScrollOffset.Y);
		ComputeScrollbars();
		Point point = new Point(_scaledScrollOffset.X - scaledScrollOffset.X, _scaledScrollOffset.Y - scaledScrollOffset.Y);
		if (point == Point.Zero)
		{
			return;
		}
		foreach (Element child in _children)
		{
			child.ApplyParentScroll(point);
		}
	}

	protected void ComputeScrollbars()
	{
		bool flag = _verticalScrollbarState != ScrollbarState.Dragged && _scaledScrollOffset.Y >= _scaledScrollSize.Y;
		_scaledScrollSize = new Point(System.Math.Max(0, _scaledScrollArea.X - _viewRectangle.Width), System.Math.Max(0, _scaledScrollArea.Y - _viewRectangle.Height));
		_scaledScrollOffset = new Point(MathHelper.Clamp(_scaledScrollOffset.X, 0, _scaledScrollSize.X), MathHelper.Clamp(_scaledScrollOffset.Y, 0, _scaledScrollSize.Y));
		if (AutoScrollDown && flag)
		{
			_scaledScrollOffset.Y = _scaledScrollSize.Y;
		}
		int num = Desktop.ScaleRound(_scrollbarStyle.Size);
		Point scrollbarOffsets = _scrollbarOffsets;
		int num2 = _viewRectangle.Width - _horizontalScrollbarLength;
		_scrollbarOffsets.X = MathHelper.Round((float)num2 * (float)_scaledScrollOffset.X / (float)_scaledScrollSize.X);
		_horizontalScrollbarHandleRectangle = new Rectangle(_rectangleAfterPadding.X + _scrollbarOffsets.X, _rectangleAfterPadding.Bottom - num, _horizontalScrollbarLength, num);
		int num3 = _viewRectangle.Height - _verticalScrollbarLength;
		_scrollbarOffsets.Y = MathHelper.Round((float)num3 * (float)_scaledScrollOffset.Y / (float)_scaledScrollSize.Y);
		_verticalScrollbarHandleRectangle = new Rectangle(_rectangleAfterPadding.Right - num, _rectangleAfterPadding.Y + _scrollbarOffsets.Y, num, _verticalScrollbarLength);
		if (scrollbarOffsets != _scrollbarOffsets)
		{
			_scrolled?.Invoke();
		}
	}

	protected virtual void ApplyParentScroll(Point scaledParentScroll)
	{
		_containerRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_anchoredRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_backgroundRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_rectangleAfterPadding.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_viewRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_contentRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_horizontalScrollbarHandleRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		_verticalScrollbarHandleRectangle.Offset(-scaledParentScroll.X, -scaledParentScroll.Y);
		foreach (Element child in _children)
		{
			child.ApplyParentScroll(scaledParentScroll);
		}
	}

	public virtual Element HitTest(Point position)
	{
		Debug.Assert(IsMounted);
		if (_waitingForLayoutAfterMount || !_anchoredRectangle.Contains(position))
		{
			return null;
		}
		for (int num = _children.Count - 1; num >= 0; num--)
		{
			Element element = _children[num];
			if (element.IsMounted)
			{
				Element element2 = element.HitTest(position);
				if (element2 != null)
				{
					return element2;
				}
			}
		}
		if (_scaledScrollArea != Point.Zero)
		{
			return this;
		}
		if (_hasTooltipText)
		{
			return this;
		}
		return null;
	}
}
