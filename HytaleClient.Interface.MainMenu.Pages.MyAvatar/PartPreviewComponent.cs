using HytaleClient.Application;
using HytaleClient.Data.Characters;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Interface.MainMenu.Pages.MyAvatar;

internal class PartPreviewComponent : Button
{
	private CharacterPartId _id;

	private PlayerSkinProperty _property;

	private UInt32Color _backgroundColor;

	private UInt32Color _backgroundColorHovered;

	public readonly int Row;

	public bool IsInView;

	public Texture Texture;

	public bool IsSelected;

	private readonly MyAvatarPage _myAvatarPage;

	public PartPreviewComponent(MyAvatarPage myAvatarPage, Element parent, int row)
		: base(myAvatarPage.Desktop, parent)
	{
		_myAvatarPage = myAvatarPage;
		Row = row;
		base.TextTooltipShowDelay = 0.2f;
	}

	public void Setup(PlayerSkinProperty property, CharacterPart part, CharacterPartId id, UInt32Color backgroundColor, UInt32Color backgroundColorHovered, bool updateRender = true)
	{
		base.TooltipText = part.Name;
		_id = id;
		_property = property;
		_backgroundColor = backgroundColor;
		_backgroundColorHovered = backgroundColorHovered;
		if (updateRender)
		{
			Update();
		}
	}

	public void Update()
	{
		if (IsInView)
		{
			_myAvatarPage.RenderCharacterPartPreviewCommandQueue.Add(new AppMainMenu.RenderCharacterPartPreviewCommand
			{
				Id = _id,
				Property = _property,
				Selected = IsSelected,
				BackgroundColor = new ColorRgba(base.IsHovered ? _backgroundColorHovered.ABGR : _backgroundColor.ABGR)
			});
		}
	}

	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();
		Update();
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();
		Update();
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (Texture != null)
		{
			Rectangle rectangle = new TextureArea(Texture, 0, 0, Texture.Width, Texture.Height, 1).Rectangle;
			Desktop.Batcher2D.RequestDrawTexture(Texture, rectangle, _anchoredRectangle, UInt32Color.White);
		}
	}
}
