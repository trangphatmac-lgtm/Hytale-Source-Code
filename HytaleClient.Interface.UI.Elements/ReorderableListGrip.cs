#define DEBUG
using System.Diagnostics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class ReorderableListGrip : Group
{
	private bool _isDragging;

	private Element _listElement;

	private int _targetIndex;

	private Point _mouseDownPosition;

	protected bool _wasDragging;

	private float _scrollDeltaTime;

	[UIMarkupProperty]
	public bool IsDragEnabled = true;

	public ReorderableListGrip(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnUnmounted()
	{
		base.OnUnmounted();
		if (_isDragging)
		{
			_isDragging = false;
			_listElement = null;
			Desktop.ClearMouseDrag();
		}
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected override void OnMounted()
	{
		base.OnMounted();
		Desktop.RegisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (!_isDragging)
		{
			return;
		}
		UpdateTargetIndex();
		_scrollDeltaTime += deltaTime;
		if (_scrollDeltaTime < 0.005f)
		{
			return;
		}
		int num = (int)(_scrollDeltaTime / 0.005f);
		_scrollDeltaTime %= 0.005f;
		float num2 = 20f * Desktop.Scale;
		Element element = Desktop.GetInteractiveLayer();
		while (element != null)
		{
			if ((element.LayoutMode == LayoutMode.TopScrolling || element.LayoutMode == LayoutMode.BottomScrolling) && (float)element.RectangleAfterPadding.Height >= num2 * 3f)
			{
				if ((float)element.RectangleAfterPadding.Top + Desktop.Scale * num2 >= (float)Desktop.MousePosition.Y)
				{
					if (element.Scroll(0f, num))
					{
						break;
					}
				}
				else if ((float)element.RectangleAfterPadding.Bottom - Desktop.Scale * num2 <= (float)Desktop.MousePosition.Y && element.Scroll(0f, -num))
				{
					break;
				}
			}
			else if ((element.LayoutMode == LayoutMode.LeftScrolling || element.LayoutMode == LayoutMode.RightScrolling) && (float)element.RectangleAfterPadding.Width >= num2 * 3f)
			{
				if ((float)element.RectangleAfterPadding.Left + Desktop.Scale * num2 >= (float)Desktop.MousePosition.X)
				{
					if (element.Scroll(num, 0f))
					{
						break;
					}
				}
				else if ((float)element.RectangleAfterPadding.Right - Desktop.Scale * num2 <= (float)Desktop.MousePosition.X && element.Scroll(-num, 0f))
				{
					break;
				}
			}
			bool flag = false;
			for (int num3 = element.Children.Count - 1; num3 >= 0; num3--)
			{
				Element element2 = element.Children[num3];
				if (element2.IsMounted && element2.AnchoredRectangle.Contains(Desktop.MousePosition))
				{
					flag = true;
					element = element2;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if ((long)evt.Button == 1 && IsDragEnabled)
		{
			Element element = this;
			while (element != null && !(element.Parent is ReorderableList))
			{
				element = element.Parent;
			}
			if (element != null)
			{
				_mouseDownPosition = Desktop.MousePosition;
				_wasDragging = false;
				_listElement = element;
			}
		}
	}

	protected override void OnMouseMove()
	{
		if (!_isDragging && _listElement != null && base.CapturedMouseButton.HasValue && (long)base.CapturedMouseButton.Value == 1)
		{
			float num = new Vector2(Desktop.MousePosition.X - _mouseDownPosition.X, Desktop.MousePosition.Y - _mouseDownPosition.Y).Length();
			if (!(num < 3f))
			{
				_isDragging = true;
				_wasDragging = true;
				Desktop.FocusElement(this);
				Desktop.StartMouseDrag(null, this, _listElement);
				SDL.SDL_SetCursor(Desktop.Cursors.Move);
				OnMouseStartDrag();
			}
		}
	}

	protected internal override void Dismiss()
	{
		if (_isDragging)
		{
			SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
			((ReorderableList)_listElement.Parent).SetDropTargetIndex(-1);
			_isDragging = false;
			_listElement = null;
			Desktop.ClearMouseDrag();
		}
		else
		{
			base.Dismiss();
		}
	}

	protected virtual void OnMouseStartDrag()
	{
	}

	protected internal override void OnMouseDragComplete(Element element, object data)
	{
		OnMouseDragCancel(data);
	}

	protected internal override void OnMouseDragCancel(object data)
	{
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
		UpdateTargetIndex();
		Element listElement = _listElement;
		_isDragging = false;
		_listElement = null;
		((ReorderableList)listElement.Parent).SetDropTargetIndex(-1);
		if (!listElement.IsMounted)
		{
			return;
		}
		ReorderableList reorderableList = (ReorderableList)listElement.Parent;
		int num = -1;
		for (int i = 0; i < reorderableList.Children.Count; i++)
		{
			if (reorderableList.Children[i] == listElement)
			{
				num = i;
				break;
			}
		}
		if (_targetIndex != num && _targetIndex != num + 1)
		{
			int num2 = ((_targetIndex > num) ? (_targetIndex - 1) : _targetIndex);
			reorderableList.Reorder(listElement, num2);
			reorderableList.Layout();
			reorderableList.ElementReordered?.Invoke(num, num2);
		}
	}

	private void UpdateTargetIndex()
	{
		if (!_isDragging)
		{
			return;
		}
		ReorderableList reorderableList = (ReorderableList)_listElement.Parent;
		if (reorderableList.LayoutMode == LayoutMode.Full)
		{
			_targetIndex = -1;
			reorderableList.SetDropTargetIndex(-1);
			return;
		}
		bool flag = reorderableList.LayoutMode == LayoutMode.Top || reorderableList.LayoutMode == LayoutMode.Bottom || reorderableList.LayoutMode == LayoutMode.Middle || reorderableList.LayoutMode == LayoutMode.MiddleCenter || reorderableList.LayoutMode == LayoutMode.TopScrolling || reorderableList.LayoutMode == LayoutMode.BottomScrolling;
		_targetIndex = -1;
		Element element = null;
		for (int i = 0; i < reorderableList.Children.Count; i++)
		{
			Element element2 = reorderableList.Children[i];
			if (!element2.IsMounted)
			{
				continue;
			}
			if (flag)
			{
				if (element != null)
				{
					if (element.AnchoredRectangle.Center.Y <= Desktop.MousePosition.Y && Desktop.MousePosition.Y < element.AnchoredRectangle.Center.Y + (element2.AnchoredRectangle.Center.Y - element.AnchoredRectangle.Center.Y))
					{
						_targetIndex = i;
						break;
					}
				}
				else if (element2.AnchoredRectangle.Y <= Desktop.MousePosition.Y && Desktop.MousePosition.Y < element2.AnchoredRectangle.Y + element2.AnchoredRectangle.Height / 2)
				{
					_targetIndex = i;
					break;
				}
			}
			else if (element != null)
			{
				if (element.AnchoredRectangle.Center.X <= Desktop.MousePosition.X && Desktop.MousePosition.X < element.AnchoredRectangle.Center.X + (element2.AnchoredRectangle.Center.X - element.AnchoredRectangle.Center.X))
				{
					_targetIndex = i;
					break;
				}
			}
			else if (element2.AnchoredRectangle.X <= Desktop.MousePosition.X && Desktop.MousePosition.X < element2.AnchoredRectangle.X + element2.AnchoredRectangle.Width / 2)
			{
				_targetIndex = i;
				break;
			}
			element = element2;
		}
		if (_targetIndex == -1)
		{
			if (flag)
			{
				_targetIndex = ((element != null && Desktop.MousePosition.Y > element.AnchoredRectangle.Center.Y) ? reorderableList.Children.Count : 0);
			}
			else
			{
				_targetIndex = ((element != null && Desktop.MousePosition.X > element.AnchoredRectangle.Center.X) ? reorderableList.Children.Count : 0);
			}
		}
		reorderableList.SetDropTargetIndex(_targetIndex);
	}
}
