using System;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Hud;

internal abstract class BaseItemSlotSelector : InterfaceComponent
{
	private const int DeadZone = 25;

	private PatchStyle _containerBackgroundTexture;

	protected Anchor _containerBackgroundAnchor;

	private PatchStyle _hoveredBackgroundTexture;

	private Anchor _hoveredBackgroundAnchor;

	private PatchStyle _selectedBackgroundTexture;

	private Anchor _selectedBackgroundAnchor;

	private SoundStyle _hoverSound;

	private TexturePatch _containerBackgroundPatch;

	private Rectangle _containerBackgroundRectangle;

	private TexturePatch _hoveredBackgroundPatch;

	private Rectangle _hoveredBackgroundRectangle;

	private TexturePatch _selectedBackgroundPatch;

	private Rectangle _selectedBackgroundRectangle;

	private Font _quantityFont;

	private int _itemIconSize;

	private int _itemIconOffset;

	private float _sliceAngle;

	protected float _mousePointerAngle;

	protected bool _enableEmptySlot;

	private int _capacity;

	public int SelectedSlot = 0;

	protected ClientItemStack[] _itemStacks;

	private TextureArea[] _itemIcons;

	protected bool _doneChange = false;

	protected int _hoveredSlot = 0;

	private float _hoveredRotation = 0f;

	protected float _quantityFontSize = 18f;

	protected readonly InGameView _inGameView;

	public BaseItemSlotSelector(InGameView inGameView, Element parent, bool enableEmptySlot)
		: base(inGameView.Interface, parent)
	{
		_inGameView = inGameView;
		_enableEmptySlot = enableEmptySlot;
		_itemStacks = new ClientItemStack[enableEmptySlot ? 4 : 5];
		_itemIcons = new TextureArea[enableEmptySlot ? 4 : 5];
	}

	protected override void OnMounted()
	{
		_hoveredSlot = SelectedSlot;
		UpdateHoveredRotation();
		_doneChange = false;
	}

	protected override void OnUnmounted()
	{
		if (!_doneChange)
		{
			OnSlotSelected(_enableEmptySlot ? (_hoveredSlot - 1) : _hoveredSlot, click: false);
		}
	}

	public ClientItemStack GetItemStack(int index)
	{
		if (index >= _itemStacks.Length)
		{
			return null;
		}
		return _itemStacks[index];
	}

	public void SetItemStacks(ClientItemStack[] stacks)
	{
		_itemStacks = stacks;
		for (int i = 0; i < _itemStacks.Length; i++)
		{
			ClientItemStack clientItemStack = stacks[i];
			if (clientItemStack == null || !_inGameView.Items.TryGetValue(clientItemStack.Id, out var value) || value.Icon == null)
			{
				_itemIcons[i] = null;
			}
			else
			{
				_itemIcons[i] = _inGameView.GetTextureAreaForItemIcon(value.Icon);
			}
		}
		ItemStacksChanged();
	}

	protected UIFragment Build(Document doc)
	{
		Clear();
		Background = doc.ResolveNamedValue<PatchStyle>(Desktop.Provider, "Overlay");
		_containerBackgroundTexture = doc.ResolveNamedValue<PatchStyle>(Desktop.Provider, "Background");
		_containerBackgroundAnchor = doc.ResolveNamedValue<Anchor>(Desktop.Provider, "Anchor");
		_hoveredBackgroundTexture = doc.ResolveNamedValue<PatchStyle>(Desktop.Provider, "HoveredBackground");
		_hoveredBackgroundAnchor = doc.ResolveNamedValue<Anchor>(Desktop.Provider, "HoveredBackgroundAnchor");
		_selectedBackgroundTexture = doc.ResolveNamedValue<PatchStyle>(Desktop.Provider, "SelectedBackground");
		_selectedBackgroundAnchor = doc.ResolveNamedValue<Anchor>(Desktop.Provider, "SelectedBackgroundAnchor");
		_itemIconSize = doc.ResolveNamedValue<int>(Desktop.Provider, "ItemIconSize");
		_itemIconOffset = doc.ResolveNamedValue<int>(Desktop.Provider, "ItemIconOffset");
		_sliceAngle = doc.ResolveNamedValue<int>(Desktop.Provider, "SliceAngle");
		doc.TryResolveNamedValue<SoundStyle>(Desktop.Provider, "HoverSound", out _hoverSound);
		return doc.Instantiate(Desktop, this);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_quantityFont = Desktop.Provider.GetFontFamily("Default").BoldFont;
		_containerBackgroundPatch = Desktop.MakeTexturePatch(_containerBackgroundTexture);
		_hoveredBackgroundPatch = Desktop.MakeTexturePatch(_hoveredBackgroundTexture);
		_selectedBackgroundPatch = Desktop.MakeTexturePatch(_selectedBackgroundTexture);
	}

	protected override void LayoutSelf()
	{
		int num = Desktop.ScaleRound(_containerBackgroundAnchor.Width.GetValueOrDefault());
		int num2 = Desktop.ScaleRound(_containerBackgroundAnchor.Height.GetValueOrDefault());
		float num3 = (float)_rectangleAfterPadding.Left + (float)_rectangleAfterPadding.Width / 2f - (float)num / 2f;
		float num4 = (float)_rectangleAfterPadding.Top + (float)_rectangleAfterPadding.Height / 2f - (float)num2 / 2f;
		_containerBackgroundRectangle = new Rectangle((int)num3, (int)num4, num, num2);
		int num5 = Desktop.ScaleRound(_hoveredBackgroundAnchor.Width.Value);
		int x = _rectangleAfterPadding.Left + _rectangleAfterPadding.Width / 2 - num5 / 2;
		int y = _rectangleAfterPadding.Top + _rectangleAfterPadding.Height / 2 + Desktop.ScaleRound(_hoveredBackgroundAnchor.Top.Value);
		_hoveredBackgroundRectangle = new Rectangle(x, y, num5, Desktop.ScaleRound(_hoveredBackgroundAnchor.Height.Value));
		int num6 = Desktop.ScaleRound(_selectedBackgroundAnchor.Width.Value);
		int x2 = _rectangleAfterPadding.Left + _rectangleAfterPadding.Width / 2 - num6 / 2;
		int y2 = _rectangleAfterPadding.Top + _rectangleAfterPadding.Height / 2 + Desktop.ScaleRound(_selectedBackgroundAnchor.Top.Value);
		_selectedBackgroundRectangle = new Rectangle(x2, y2, num6, Desktop.ScaleRound(_selectedBackgroundAnchor.Height.Value));
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected void RefreshHoveredSlot()
	{
		Point point = new Point(Desktop.MousePosition.X - _rectangleAfterPadding.Left, Desktop.MousePosition.Y - _rectangleAfterPadding.Top);
		Point point2 = new Point(point.X - _rectangleAfterPadding.Width / 2, point.Y - _rectangleAfterPadding.Height / 2);
		if (point2.X * point2.X + point2.Y * point2.Y >= 625)
		{
			_mousePointerAngle = MathHelper.ToDegrees((float)System.Math.Atan2(point2.Y, point2.X));
			float num = _mousePointerAngle - (90f - _sliceAngle / 2f);
			if (num < 0f)
			{
				num += 360f;
			}
			num %= 360f;
			int hoveredSlot = _hoveredSlot;
			_hoveredSlot = ((!(num < _sliceAngle)) ? ((int)((num - _sliceAngle) / _sliceAngle + 1f)) : 0);
			if (_hoveredSlot != hoveredSlot)
			{
				OnSlotHovered();
				UpdateHoveredRotation();
			}
		}
	}

	protected override void OnMouseMove()
	{
		RefreshHoveredSlot();
	}

	private void UpdateHoveredRotation()
	{
		_hoveredRotation = ((_hoveredSlot == 0) ? 0f : ((float)_hoveredSlot * _sliceAngle));
	}

	public void ResetState()
	{
		for (int i = 0; i < _itemStacks.Length; i++)
		{
			_itemIcons[i] = null;
			_itemStacks[i] = null;
		}
	}

	protected static void CreateRotationMatrixAroundPoint(int x, int y, int width, float yaw, out Matrix matrix)
	{
		yaw = MathHelper.ToRadians(yaw);
		Matrix.CreateTranslation((float)x + (float)width / 2f, y, 0f, out matrix);
		Matrix.CreateRotationZ(yaw, out var result);
		Matrix.Multiply(ref result, ref matrix, out matrix);
		Matrix.CreateTranslation((float)(-x) - (float)width / 2f, -y, 0f, out result);
		Matrix.Multiply(ref result, ref matrix, out matrix);
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		Desktop.Batcher2D.RequestDrawPatch(_containerBackgroundPatch, _containerBackgroundRectangle, Desktop.Scale);
		int num = Desktop.ScaleRound(_itemIconSize);
		int num2 = Desktop.ScaleRound((float)_itemIconSize * 1.1f);
		float yaw = ((SelectedSlot == 0) ? 0f : ((float)SelectedSlot * _sliceAngle));
		Matrix matrix;
		if (SelectedSlot == 0)
		{
			Desktop.Batcher2D.RequestDrawPatch(_selectedBackgroundPatch, _selectedBackgroundRectangle, Desktop.Scale);
		}
		else
		{
			CreateRotationMatrixAroundPoint(_selectedBackgroundRectangle.X, _rectangleAfterPadding.Y + _rectangleAfterPadding.Height / 2, _selectedBackgroundRectangle.Width, yaw, out matrix);
			Desktop.Batcher2D.SetTransformationMatrix(matrix);
			Desktop.Batcher2D.RequestDrawPatch(_selectedBackgroundPatch, _selectedBackgroundRectangle, Desktop.Scale);
			Desktop.Batcher2D.SetTransformationMatrix(Matrix.Identity);
		}
		if (_hoveredSlot != 0)
		{
			CreateRotationMatrixAroundPoint(_hoveredBackgroundRectangle.X, _rectangleAfterPadding.Y + _rectangleAfterPadding.Height / 2, _hoveredBackgroundRectangle.Width, _hoveredRotation, out matrix);
			Desktop.Batcher2D.SetTransformationMatrix(matrix);
			Desktop.Batcher2D.RequestDrawPatch(_hoveredBackgroundPatch, _hoveredBackgroundRectangle, Desktop.Scale);
			Desktop.Batcher2D.SetTransformationMatrix(Matrix.Identity);
		}
		else
		{
			Desktop.Batcher2D.SetTransformationMatrix(Matrix.Identity);
			Desktop.Batcher2D.RequestDrawPatch(_hoveredBackgroundPatch, _hoveredBackgroundRectangle, Desktop.Scale);
		}
		int num3 = Desktop.ScaleRound(_itemIconOffset);
		for (int i = 0; i < _itemStacks.Length; i++)
		{
			if (_itemStacks[i] == null)
			{
				continue;
			}
			float num4 = 90f + (float)i * _sliceAngle;
			if (_enableEmptySlot)
			{
				num4 += _sliceAngle;
			}
			float num5 = MathHelper.ToRadians(num4);
			double num6 = (double)((float)_rectangleAfterPadding.Left + (float)_rectangleAfterPadding.Width / 2f) + System.Math.Cos(num5) * (double)num3 - (double)((float)num / 2f);
			double num7 = (double)((float)_rectangleAfterPadding.Top + (float)_rectangleAfterPadding.Height / 2f) + System.Math.Sin(num5) * (double)num3 - (double)((float)num / 2f);
			if (_itemIcons[i] != null)
			{
				Rectangle destRect;
				if (i == (_enableEmptySlot ? (_hoveredSlot - 1) : _hoveredSlot))
				{
					int num8 = (num2 - num) / 2;
					destRect = new Rectangle((int)num6 - num8, (int)num7 - num8, num2, num2);
				}
				else
				{
					destRect = new Rectangle((int)num6, (int)num7, num, num);
				}
				Desktop.Batcher2D.RequestDrawTexture(_itemIcons[i].Texture, _itemIcons[i].Rectangle, destRect, UInt32Color.White);
			}
			if (_itemStacks[i].Quantity > 1)
			{
				string text = _itemStacks[i].Quantity.ToString();
				float x = (float)num6 + (float)num - (float)Desktop.ScaleRound(_quantityFont.CalculateTextWidth(text) * _quantityFontSize / (float)_quantityFont.BaseSize);
				float y = (float)num7 + (float)num - 26f * Desktop.Scale;
				Desktop.Batcher2D.RequestDrawText(_quantityFont, _quantityFontSize * Desktop.Scale, text, new Vector3(x, y, 0f), UInt32Color.White);
			}
		}
	}

	protected virtual void OnSlotHovered()
	{
		if (_hoverSound != null)
		{
			Desktop.Provider.PlaySound(_hoverSound);
		}
	}

	protected virtual void ItemStacksChanged()
	{
	}

	protected virtual void OnSlotSelected(int slot, bool click)
	{
	}
}
