using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
internal class CircularProgressBar : Element
{
	[UIMarkupProperty]
	public float Value = 0f;

	[UIMarkupProperty]
	public UInt32Color Color;

	public CircularProgressBar(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	private void PrepareForDrawTriangle(float rot, float percentage, Rectangle dest)
	{
		TextureArea whitePixel = Desktop.Provider.WhitePixel;
		Desktop.Batcher2D.SetTransformationMatrix(new Vector3(dest.X, dest.Y, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(rot)), 1f);
		Vector3 topLeft = new Vector3(0f, -dest.Height, 0f);
		Vector3 bottomLeft = new Vector3(0f, 0f, 0f);
		Vector3 bottonRight = new Vector3((float)dest.Width * percentage, (float)(-dest.Height) + (float)dest.Height * percentage, 0f);
		Desktop.Batcher2D.RequestDrawTextureTriangle(whitePixel.Texture, whitePixel.Rectangle, topLeft, bottomLeft, bottonRight, Color);
	}

	protected override void PrepareForDrawSelf()
	{
		Rectangle dest = new Rectangle(_anchoredRectangle.X + _anchoredRectangle.Width / 2, _anchoredRectangle.Y + _anchoredRectangle.Height / 2, _anchoredRectangle.Width / 2, _anchoredRectangle.Height / 2);
		PrepareForDrawTriangle(0f, GetPercentage(0, Value), dest);
		PrepareForDrawTriangle(90f, GetPercentage(1, Value), dest);
		PrepareForDrawTriangle(180f, GetPercentage(2, Value), dest);
		PrepareForDrawTriangle(270f, GetPercentage(3, Value), dest);
		Desktop.Batcher2D.SetTransformationMatrix(Matrix.Identity);
	}

	private static float GetPercentage(int index, float Value)
	{
		Value = MathHelper.Clamp(Value, 0f, 1f);
		return MathHelper.Clamp((Value - 0.25f * (float)index) / 0.25f, 0f, 1f);
	}
}
