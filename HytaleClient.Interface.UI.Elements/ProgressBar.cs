using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class ProgressBar : Element
{
	public enum ProgressBarAlignment
	{
		Vertical,
		Horizontal
	}

	public enum ProgressBarDirection
	{
		Start,
		End
	}

	[UIMarkupProperty]
	public UIPath BarTexturePath;

	[UIMarkupProperty]
	public UIPath EffectTexturePath;

	[UIMarkupProperty]
	public int EffectWidth;

	[UIMarkupProperty]
	public int EffectHeight;

	[UIMarkupProperty]
	public int EffectOffset;

	[UIMarkupProperty]
	public ProgressBarAlignment Alignment = ProgressBarAlignment.Horizontal;

	[UIMarkupProperty]
	public ProgressBarDirection Direction = ProgressBarDirection.End;

	private TexturePatch _barPatch;

	private TexturePatch _effectPatch;

	private Rectangle _visibleBarRectangle;

	private Rectangle _effectRectangle;

	[UIMarkupProperty]
	public float Value;

	public ProgressBar(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_barPatch = ((BarTexturePath != null) ? Desktop.MakeTexturePatch(new PatchStyle(BarTexturePath.Value)) : null);
		_effectPatch = ((EffectTexturePath != null) ? Desktop.MakeTexturePatch(new PatchStyle(EffectTexturePath.Value)) : null);
	}

	protected override void LayoutSelf()
	{
		base.LayoutSelf();
		_visibleBarRectangle = _anchoredRectangle;
		if (Alignment == ProgressBarAlignment.Horizontal)
		{
			_visibleBarRectangle.Width = (int)((float)_visibleBarRectangle.Width * Value);
			if (Direction == ProgressBarDirection.Start)
			{
				_visibleBarRectangle.X += _anchoredRectangle.Width - _visibleBarRectangle.Width;
			}
			_effectRectangle = new Rectangle(((Direction == ProgressBarDirection.End) ? _visibleBarRectangle.Right : _visibleBarRectangle.Left) - Desktop.ScaleRound(EffectOffset), (int)((float)(_anchoredRectangle.Top - Desktop.ScaleRound((float)EffectHeight / 2f)) + (float)_anchoredRectangle.Height / 2f), Desktop.ScaleRound(EffectWidth), Desktop.ScaleRound(EffectHeight));
		}
		else
		{
			_visibleBarRectangle.Height = (int)((float)_visibleBarRectangle.Height * Value);
			if (Direction == ProgressBarDirection.Start)
			{
				_visibleBarRectangle.Y += _anchoredRectangle.Height - _visibleBarRectangle.Height;
			}
			_effectRectangle = new Rectangle((int)((float)(_anchoredRectangle.Left - Desktop.ScaleRound((float)EffectWidth / 2f)) + (float)_anchoredRectangle.Width / 2f), ((Direction == ProgressBarDirection.End) ? _visibleBarRectangle.Bottom : _visibleBarRectangle.Top) - Desktop.ScaleRound(EffectOffset), Desktop.ScaleRound(EffectWidth), Desktop.ScaleRound(EffectHeight));
		}
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		Desktop.Batcher2D.PushScissor(_visibleBarRectangle);
		Desktop.Batcher2D.RequestDrawPatch(_barPatch, _anchoredRectangle, Desktop.Scale);
		Desktop.Batcher2D.PopScissor();
		if (_effectPatch != null)
		{
			byte a = byte.MaxValue;
			if (Value < 0.2f)
			{
				a = (byte)(Value / 0.2f * 255f);
			}
			else if (Value > 0.8f)
			{
				a = (byte)((1f - Value) / 0.19999999f * 255f);
			}
			Desktop.Batcher2D.RequestDrawPatch(_effectPatch.TextureArea.Texture, _effectPatch.TextureArea.Rectangle, _effectPatch.HorizontalBorder, _effectPatch.VerticalBorder, _effectPatch.TextureArea.Scale, _effectRectangle, 0f, UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, a));
		}
	}
}
