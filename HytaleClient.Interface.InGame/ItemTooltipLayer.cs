using System;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame;

internal class ItemTooltipLayer : BaseTooltipLayer
{
	private readonly InGameView _inGameView;

	private Point _centerPoint;

	private Group _group;

	private Group _arrow;

	private Group _header;

	private Group _description;

	private Group _footer;

	private Label _idLabel;

	private Label _typeLabel;

	private int _backgroundBorder;

	public bool ShowArrow = true;

	public ItemTooltipLayer(InGameView inGameView)
		: base(inGameView.Desktop)
	{
		_inGameView = inGameView;
		inGameView.Interface.TryGetDocument("InGame/Tooltips/ItemTooltip.ui", out var document);
		_backgroundBorder = document.ResolveNamedValue<int>(Desktop.Provider, "TextureBorder");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_group = uIFragment.Get<Group>("Group");
		_arrow = uIFragment.Get<Group>("Arrow");
		_header = uIFragment.Get<Group>("Header");
		_footer = uIFragment.Get<Group>("Footer");
		_description = uIFragment.Get<Group>("Description");
		_typeLabel = uIFragment.Get<Label>("Type");
		_idLabel = uIFragment.Get<Label>("Id");
	}

	public void UpdateTooltip(Point centerPoint, ClientItemStack stack, string name = null, string description = null, string itemStackId = null)
	{
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Invalid comparison between Unknown and I4
		_centerPoint = centerPoint;
		Label label = _header.Find<Label>("Name");
		Label label2 = _description.Find<Label>("Label");
		Label label3 = _header.Find<Label>("Quality");
		Label label4 = _footer.Find<Label>("Durability");
		ClientItemBase value;
		if (stack == null)
		{
			label.Text = name;
			if (description != null)
			{
				label2.Text = description;
				_description.Visible = true;
			}
			else
			{
				_description.Visible = false;
			}
			if (itemStackId != null)
			{
				_idLabel.Text = Desktop.Provider.GetText("ui.items.idLabel") + " " + itemStackId;
				_idLabel.Visible = true;
			}
			else
			{
				_idLabel.Visible = false;
			}
			_typeLabel.Visible = false;
			_footer.Visible = false;
			label3.Visible = false;
			label4.Visible = false;
		}
		else if (_inGameView.Items.TryGetValue(stack.Id, out value))
		{
			ClientItemQuality clientItemQuality = _inGameView.InGame.Instance.ServerSettings.ItemQualities[value.QualityIndex];
			if (clientItemQuality.ItemTooltipTexture == null || clientItemQuality.ItemTooltipArrowTexture == null)
			{
				throw new Exception($"Missing texture patches for ItemQuality index {value.QualityIndex} tooltips.");
			}
			if (value.Utility != null && value.Utility.Usable)
			{
				_typeLabel.Text = Desktop.Provider.GetText("ui.items.utility");
				_typeLabel.Visible = true;
			}
			else if (value.Consumable)
			{
				_typeLabel.Text = Desktop.Provider.GetText("ui.items.consumable");
				_typeLabel.Visible = true;
			}
			else
			{
				_typeLabel.Visible = false;
			}
			if (_inGameView.TryMountAssetTexture(clientItemQuality.ItemTooltipTexture, out var textureArea))
			{
				_group.Background = new PatchStyle(textureArea)
				{
					Border = _backgroundBorder
				};
			}
			else
			{
				_group.Background = new PatchStyle(Desktop.Provider.MissingTexture);
			}
			if (ShowArrow)
			{
				if (_inGameView.TryMountAssetTexture(clientItemQuality.ItemTooltipArrowTexture, out var textureArea2))
				{
					_arrow.Background = new PatchStyle(textureArea2);
				}
				else
				{
					_arrow.Background = new PatchStyle(Desktop.Provider.MissingTexture);
				}
				_arrow.Visible = true;
			}
			else
			{
				_arrow.Visible = false;
			}
			label.Text = Desktop.Provider.GetText("items." + stack.Id + ".name");
			label.Style.TextColor = clientItemQuality.TextColor;
			if (clientItemQuality.VisibleQualityLabel)
			{
				label3.Visible = true;
				label3.Text = Desktop.Provider.GetText(clientItemQuality.LocalizationKey);
				label3.Style.TextColor = clientItemQuality.TextColor;
			}
			else
			{
				label3.Visible = false;
			}
			description = Desktop.Provider.GetText("items." + stack.Id + ".description", null, returnFallback: false);
			if (description != null)
			{
				label2.Text = description;
				_description.Visible = true;
			}
			else
			{
				_description.Visible = false;
			}
			if (stack.MaxDurability > 0.0 && stack.Durability >= 0.0)
			{
				label4.Text = Desktop.Provider.FormatNumber((int)stack.Durability) + "/" + Desktop.Provider.FormatNumber((int)stack.MaxDurability);
				_footer.Visible = true;
			}
			else
			{
				_footer.Visible = false;
			}
			if ((int)_inGameView.InGame.Instance.GameMode == 1)
			{
				_idLabel.Text = Desktop.Provider.GetText("ui.items.idLabel") + " " + stack.Id;
				_idLabel.Visible = true;
			}
			else
			{
				_idLabel.Visible = false;
			}
		}
		else
		{
			label.Text = "Invalid Item";
			label.Style.TextColor = UInt32Color.FromRGBA(byte.MaxValue, 0, 0, byte.MaxValue);
			_description.Visible = false;
			_footer.Visible = false;
			_typeLabel.Visible = false;
		}
		Layout();
	}

	protected override void UpdatePosition()
	{
		int num = (ShowArrow ? 30 : 15);
		int num2 = _group.ContainerRectangle.Width / 2;
		Anchor.Left = Desktop.UnscaleRound(_centerPoint.X - num2);
		Anchor.Top = Desktop.UnscaleRound(_centerPoint.Y - _group.ContainerRectangle.Height) - num;
		int? left = Anchor.Left;
		int? num3 = Anchor.Right + Desktop.UnscaleRound(_group.ContainerRectangle.Width);
		if (left < 0)
		{
			Anchor.Left = 0;
		}
		else if (num3 > Desktop.RootLayoutRectangle.Width)
		{
			Anchor.Left -= num3;
		}
	}
}
