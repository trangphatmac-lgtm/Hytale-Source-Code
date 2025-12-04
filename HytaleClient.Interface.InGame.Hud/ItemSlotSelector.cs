using HytaleClient.Application;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Hud;

internal abstract class ItemSlotSelector : BaseItemSlotSelector
{
	private Label _itemNameLabel;

	private PatchStyle _pointerBackgroundTexture;

	private Anchor _pointerBackgroundAnchor;

	private TexturePatch _pointerBackgroundPatch;

	private Rectangle _pointerBackgroundRectangle;

	private Group _emptyIcon;

	protected ItemSlotSelector(InGameView inGameView, bool enableEmptySlot = true)
		: base(inGameView, inGameView.HudContainer, enableEmptySlot)
	{
	}

	public virtual void Build()
	{
		string path = "InGame/Hud/ItemSlotSelector.ui";
		Interface.TryGetDocument(path, out var document);
		_pointerBackgroundTexture = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "PointerBackground");
		_pointerBackgroundAnchor = document.ResolveNamedValue<Anchor>(Desktop.Provider, "PointerBackgroundAnchor");
		UIFragment uIFragment = Build(document);
		_itemNameLabel = uIFragment.Get<Label>("ItemName");
		_emptyIcon = uIFragment.Get<Group>("EmptyIcon");
		_emptyIcon.Visible = _enableEmptySlot;
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_pointerBackgroundPatch = Desktop.MakeTexturePatch(_pointerBackgroundTexture);
	}

	protected override void LayoutSelf()
	{
		base.LayoutSelf();
		int num = Desktop.ScaleRound(_pointerBackgroundAnchor.Width.GetValueOrDefault());
		int height = Desktop.ScaleRound(_pointerBackgroundAnchor.Height.GetValueOrDefault());
		float num2 = (float)_rectangleAfterPadding.Left + (float)_rectangleAfterPadding.Width / 2f - (float)num / 2f;
		float num3 = (float)_rectangleAfterPadding.Top + (float)_rectangleAfterPadding.Height / 2f + (float)Desktop.ScaleRound(_pointerBackgroundAnchor.Top.Value);
		_pointerBackgroundRectangle = new Rectangle((int)num2, (int)num3, num, height);
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (activate && (long)evt.Button == 1)
		{
			_doneChange = true;
			OnSlotSelected(_enableEmptySlot ? (_hoveredSlot - 1) : _hoveredSlot, click: true);
			_inGameView.InGame.SetActiveItemSelector(AppInGame.ItemSelector.None);
		}
	}

	protected override void OnSlotHovered()
	{
		UpdateItemNameLabel();
	}

	protected override void ItemStacksChanged()
	{
		UpdateItemNameLabel();
	}

	private void UpdateItemNameLabel()
	{
		if (_enableEmptySlot && _hoveredSlot == 0)
		{
			_itemNameLabel.Text = Desktop.Provider.GetText("ui.hud.itemSlotSelector.noItem");
		}
		else
		{
			int num = (_enableEmptySlot ? (_hoveredSlot - 1) : _hoveredSlot);
			ClientItemStack clientItemStack = ((num >= _itemStacks.Length) ? null : _itemStacks[num]);
			_itemNameLabel.Text = ((clientItemStack == null) ? "" : Desktop.Provider.GetText("items." + clientItemStack.Id + ".name"));
		}
		_itemNameLabel.Layout();
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		BaseItemSlotSelector.CreateRotationMatrixAroundPoint(_pointerBackgroundRectangle.X, _rectangleAfterPadding.Y + _rectangleAfterPadding.Height / 2, _pointerBackgroundRectangle.Width, _mousePointerAngle - 90f, out var matrix);
		Desktop.Batcher2D.SetTransformationMatrix(matrix);
		Desktop.Batcher2D.RequestDrawPatch(_pointerBackgroundPatch, _pointerBackgroundRectangle, Desktop.Scale);
		Desktop.Batcher2D.SetTransformationMatrix(Matrix.Identity);
	}
}
