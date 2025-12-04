using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HytaleClient.Core;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using NLog;

namespace HytaleClient.Graphics.Batcher2D;

public class Batcher2D : Disposable
{
	private class MaskSetup
	{
		public UShortVector4 Bounds;

		public TextureArea Area;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly int _maxQuads = 8192;

	private readonly GraphicsDevice _graphics;

	private readonly GLVertexArray _vertexArray;

	private readonly GLBuffer _verticesBuffer;

	private readonly GLBuffer _indicesBuffer;

	private Batcher2DVertex[] _vertices;

	private GLTexture[] _texturesPerQuad;

	private GLTexture[] _maskTexturesPerQuad;

	private int _usedQuads = 0;

	private bool _allowBatcher2dToGrow;

	private Matrix _transformationMatrix = Matrix.Identity;

	private byte? _opacityOverride = null;

	private readonly Stack<MaskSetup> _maskStack = new Stack<MaskSetup>();

	private readonly Stack<Rectangle> _scissorStack = new Stack<Rectangle>();

	public unsafe Batcher2D(GraphicsDevice graphics, bool allowBatcher2dToGrow = false)
	{
		_graphics = graphics;
		_transformationMatrix = Matrix.Identity;
		_vertices = new Batcher2DVertex[_maxQuads * 4];
		_texturesPerQuad = new GLTexture[_maxQuads];
		_maskTexturesPerQuad = new GLTexture[_maxQuads];
		GLFunctions gL = _graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		ushort[] array = new ushort[_maxQuads * 6];
		for (int i = 0; i < _maxQuads; i++)
		{
			array[i * 6] = (ushort)(i * 4);
			array[i * 6 + 1] = (ushort)(i * 4 + 1);
			array[i * 6 + 2] = (ushort)(i * 4 + 2);
			array[i * 6 + 3] = (ushort)(i * 4);
			array[i * 6 + 4] = (ushort)(i * 4 + 2);
			array[i * 6 + 5] = (ushort)(i * 4 + 3);
		}
		fixed (ushort* ptr = array)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(array.Length * 2), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		Batcher2DProgram batcher2DProgram = _graphics.GPUProgramStore.Batcher2DProgram;
		IntPtr zero = IntPtr.Zero;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribPosition.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, Batcher2DVertex.Size, zero);
		zero += 12;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribTexCoords.Index, 2, GL.UNSIGNED_SHORT, normalized: true, Batcher2DVertex.Size, zero);
		zero += 4;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribScissor.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribScissor.Index, 4, GL.UNSIGNED_SHORT, normalized: false, Batcher2DVertex.Size, zero);
		zero += 8;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribMaskTextureArea.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribMaskTextureArea.Index, 4, GL.FLOAT, normalized: false, Batcher2DVertex.Size, zero);
		zero += 16;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribMaskBounds.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribMaskBounds.Index, 4, GL.UNSIGNED_SHORT, normalized: false, Batcher2DVertex.Size, zero);
		zero += 8;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribFillColor.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribFillColor.Index, 4, GL.UNSIGNED_BYTE, normalized: true, Batcher2DVertex.Size, zero);
		zero += 4;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribOutlineColor.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribOutlineColor.Index, 4, GL.UNSIGNED_BYTE, normalized: true, Batcher2DVertex.Size, zero);
		zero += 4;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribSDFSettings.Index);
		gL.VertexAttribPointer(batcher2DProgram.AttribSDFSettings.Index, 4, GL.UNSIGNED_BYTE, normalized: true, Batcher2DVertex.Size, zero);
		zero += 4;
		gL.EnableVertexAttribArray(batcher2DProgram.AttribFontId.Index);
		gL.VertexAttribIPointer(batcher2DProgram.AttribFontId.Index, 1, GL.UNSIGNED_INT, Batcher2DVertex.Size, zero);
		zero += 4;
		_allowBatcher2dToGrow = allowBatcher2dToGrow;
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteBuffer(_indicesBuffer);
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteVertexArray(_vertexArray);
	}

	public unsafe void Draw()
	{
		if (_usedQuads == 0)
		{
			return;
		}
		GLFunctions gL = _graphics.GL;
		_graphics.GPUProgramStore.Batcher2DProgram.AssertInUse();
		gL.BindVertexArray(_vertexArray);
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (Batcher2DVertex* ptr = _vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertices.Length * Batcher2DVertex.Size), (IntPtr)ptr, GL.DYNAMIC_DRAW);
		}
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		GLTexture gLTexture = _maskTexturesPerQuad[0];
		if (gLTexture != GLTexture.None)
		{
			gL.ActiveTexture(GL.TEXTURE1);
			gL.BindTexture(GL.TEXTURE_2D, gLTexture);
			gL.ActiveTexture(GL.TEXTURE0);
		}
		GLTexture gLTexture2 = _texturesPerQuad[0];
		gL.BindTexture(GL.TEXTURE_2D, gLTexture2);
		int num = 0;
		for (int i = 0; i < _usedQuads; i++)
		{
			GLTexture gLTexture3 = _maskTexturesPerQuad[i];
			if (gLTexture3 != GLTexture.None && gLTexture3 != gLTexture)
			{
				gL.ActiveTexture(GL.TEXTURE1);
				gL.BindTexture(GL.TEXTURE_2D, gLTexture3);
				gL.ActiveTexture(GL.TEXTURE0);
			}
			GLTexture gLTexture4 = _texturesPerQuad[i];
			if (gLTexture4 != GLTexture.None && gLTexture4 != gLTexture2)
			{
				gL.DrawElements(GL.TRIANGLES, (i - num) * 6, GL.UNSIGNED_SHORT, (IntPtr)(num * 6 * 2));
				num = i;
				gLTexture2 = gLTexture4;
				gL.BindTexture(GL.TEXTURE_2D, gLTexture2);
			}
		}
		gL.DrawElements(GL.TRIANGLES, (_usedQuads - num) * 6, GL.UNSIGNED_SHORT, (IntPtr)(num * 6 * 2));
		_usedQuads = 0;
	}

	public void PushScissor(Rectangle scissor)
	{
		Rectangle item = scissor;
		if (_scissorStack.Count > 0)
		{
			Rectangle rectangle = _scissorStack.Peek();
			int num = item.X - rectangle.X;
			if (num < 0)
			{
				item.X -= num;
				item.Width += num;
			}
			int num2 = rectangle.Right - item.Right;
			if (num2 < 0)
			{
				item.Width += num2;
			}
			int num3 = item.Y - rectangle.Y;
			if (num3 < 0)
			{
				item.Y -= num3;
				item.Height += num3;
			}
			int num4 = rectangle.Bottom - item.Bottom;
			if (num4 < 0)
			{
				item.Height += num4;
			}
		}
		_scissorStack.Push(item);
	}

	public void PopScissor()
	{
		_scissorStack.Pop();
	}

	public void PushMask(TextureArea area, Rectangle rectangle, Rectangle viewportRectangle)
	{
		_maskStack.Push(new MaskSetup
		{
			Area = area,
			Bounds = new UShortVector4((ushort)(viewportRectangle.X + rectangle.X), (ushort)(viewportRectangle.Y + rectangle.Y), (ushort)rectangle.Width, (ushort)rectangle.Height)
		});
	}

	public void PopMask()
	{
		_maskStack.Pop();
	}

	public void SetTransformationMatrix(Vector3 position, Quaternion orientation, float scale)
	{
		Matrix.Compose(scale, orientation, position, out _transformationMatrix);
	}

	public void SetTransformationMatrix(Matrix worldMatrix)
	{
		_transformationMatrix = worldMatrix;
	}

	public void SetOpacityOverride(byte? opacity)
	{
		_opacityOverride = opacity;
	}

	public void RequestDrawTexture(GLTexture glTexture, int textureWidth, int textureHeight, Rectangle sourceRect, Vector3 position, float width, float height, UInt32Color color, bool flip = false)
	{
		int usedQuads = _usedQuads;
		if (usedQuads >= _texturesPerQuad.Length)
		{
			Logger.Warn("Maximum quads {0} for UI reached!", _vertices.Length);
			if (!GrowArraysIfAllowed())
			{
				return;
			}
		}
		ushort x = (ushort)MathHelper.Round(65535 * sourceRect.Left / textureWidth);
		ushort x2 = (ushort)MathHelper.Round(65535 * sourceRect.Right / textureWidth);
		ushort num = (ushort)MathHelper.Round(65535 * sourceRect.Top / textureHeight);
		ushort num2 = (ushort)MathHelper.Round(65535 * sourceRect.Bottom / textureHeight);
		Vector3 position2 = new Vector3(position.X + width, position.Y, position.Z);
		Vector3 position3 = new Vector3(position.X, position.Y, position.Z);
		Vector3 position4 = new Vector3(position.X, position.Y + height, position.Z);
		Vector3 position5 = new Vector3(position.X + width, position.Y + height, position.Z);
		Rectangle rectangle = ((_scissorStack.Count > 0) ? _scissorStack.Peek() : new Rectangle(0, 0, 65535, 65535));
		Vector3 position6 = new Vector3(rectangle.X, rectangle.Y, 0f);
		Vector3.Transform(ref position6, ref _transformationMatrix, out position6);
		UShortVector4 scissor = new UShortVector4((ushort)System.Math.Max(0f, position6.X), (ushort)System.Math.Max(0f, position6.Y), (ushort)System.Math.Max(0, rectangle.Width), (ushort)System.Math.Max(0, rectangle.Height));
		Vector3 position7 = Vector3.Transform(position2, _transformationMatrix);
		Vector3 position8 = Vector3.Transform(position3, _transformationMatrix);
		Vector3 position9 = Vector3.Transform(position4, _transformationMatrix);
		Vector3 position10 = Vector3.Transform(position5, _transformationMatrix);
		if (!(position7.X < (float)(int)scissor.X) && !(position10.Y < (float)(int)scissor.Y) && !(position8.X > (float)(scissor.X + scissor.Z)) && !(position7.Y > (float)(scissor.Y + scissor.W)))
		{
			MaskSetup maskSetup = ((_maskStack.Count > 0) ? _maskStack.Peek() : null);
			UShortVector4 maskBounds = maskSetup?.Bounds ?? UShortVector4.Zero;
			Vector4 maskTextureArea = Vector4.Zero;
			if (maskSetup != null)
			{
				Texture texture = maskSetup.Area.Texture;
				_maskTexturesPerQuad[usedQuads] = texture.GLTexture;
				Rectangle rectangle2 = maskSetup.Area.Rectangle;
				float x3 = (float)rectangle2.X / (float)texture.Width;
				float y = (float)rectangle2.Y / (float)texture.Height;
				float z = (float)rectangle2.Width / (float)texture.Width;
				float w = (float)rectangle2.Height / (float)texture.Height;
				maskTextureArea = new Vector4(x3, y, z, w);
			}
			if (flip)
			{
				ushort num3 = num;
				num = num2;
				num2 = num3;
			}
			if (_opacityOverride.HasValue)
			{
				color.SetA(_opacityOverride.GetValueOrDefault());
			}
			_vertices[usedQuads * 4] = new Batcher2DVertex
			{
				Position = position7,
				TextureCoordinates = new UShortVector2(x2, num),
				Scissor = scissor,
				MaskTextureArea = maskTextureArea,
				MaskBounds = maskBounds,
				FillColor = color
			};
			_vertices[usedQuads * 4 + 1] = new Batcher2DVertex
			{
				Position = position8,
				TextureCoordinates = new UShortVector2(x, num),
				Scissor = scissor,
				MaskTextureArea = maskTextureArea,
				MaskBounds = maskBounds,
				FillColor = color
			};
			_vertices[usedQuads * 4 + 2] = new Batcher2DVertex
			{
				Position = position9,
				TextureCoordinates = new UShortVector2(x, num2),
				Scissor = scissor,
				MaskTextureArea = maskTextureArea,
				MaskBounds = maskBounds,
				FillColor = color
			};
			_vertices[usedQuads * 4 + 3] = new Batcher2DVertex
			{
				Position = position10,
				TextureCoordinates = new UShortVector2(x2, num2),
				Scissor = scissor,
				MaskTextureArea = maskTextureArea,
				MaskBounds = maskBounds,
				FillColor = color
			};
			_texturesPerQuad[usedQuads] = glTexture;
			_usedQuads++;
		}
	}

	public void RequestDrawTextureTriangle(GLTexture glTexture, int textureWidth, int textureHeight, Rectangle sourceRect, Vector3 topLeft, Vector3 bottomLeft, Vector3 bottomRight, UInt32Color color, bool flip)
	{
		int usedQuads = _usedQuads;
		if (usedQuads == _maxQuads)
		{
			Logger.Warn("Maximum quads {0} for UI reached!", _maxQuads);
			return;
		}
		ushort x = (ushort)MathHelper.Round(65535 * sourceRect.Left / textureWidth);
		ushort x2 = (ushort)MathHelper.Round(65535 * sourceRect.Right / textureWidth);
		ushort num = (ushort)MathHelper.Round(65535 * sourceRect.Top / textureHeight);
		ushort num2 = (ushort)MathHelper.Round(65535 * sourceRect.Bottom / textureHeight);
		MaskSetup maskSetup = ((_maskStack.Count > 0) ? _maskStack.Peek() : null);
		UShortVector4 maskBounds = maskSetup?.Bounds ?? UShortVector4.Zero;
		Vector4 maskTextureArea = Vector4.Zero;
		if (maskSetup != null)
		{
			Texture texture = maskSetup.Area.Texture;
			_maskTexturesPerQuad[usedQuads] = texture.GLTexture;
			Rectangle rectangle = maskSetup.Area.Rectangle;
			float x3 = (float)rectangle.X / (float)texture.Width;
			float y = (float)rectangle.Y / (float)texture.Height;
			float z = (float)rectangle.Width / (float)texture.Width;
			float w = (float)rectangle.Height / (float)texture.Height;
			maskTextureArea = new Vector4(x3, y, z, w);
		}
		Rectangle rectangle2 = ((_scissorStack.Count > 0) ? _scissorStack.Peek() : new Rectangle(0, 0, 65535, 65535));
		UShortVector4 scissor = new UShortVector4((ushort)System.Math.Max(0, rectangle2.X), (ushort)System.Math.Max(0, rectangle2.Y), (ushort)System.Math.Max(0, rectangle2.Width), (ushort)System.Math.Max(0, rectangle2.Height));
		if (flip)
		{
			ushort num3 = num;
			num = num2;
			num2 = num3;
		}
		_vertices[usedQuads * 4] = new Batcher2DVertex
		{
			Position = Vector3.Transform(bottomRight, _transformationMatrix),
			TextureCoordinates = new UShortVector2(x2, num2),
			Scissor = scissor,
			MaskTextureArea = maskTextureArea,
			MaskBounds = maskBounds,
			FillColor = color
		};
		_vertices[usedQuads * 4 + 1] = new Batcher2DVertex
		{
			Position = Vector3.Transform(topLeft, _transformationMatrix),
			TextureCoordinates = new UShortVector2(x, num),
			Scissor = scissor,
			MaskTextureArea = maskTextureArea,
			MaskBounds = maskBounds,
			FillColor = color
		};
		_vertices[usedQuads * 4 + 2] = new Batcher2DVertex
		{
			Position = Vector3.Transform(bottomLeft, _transformationMatrix),
			TextureCoordinates = new UShortVector2(x, num2),
			Scissor = scissor,
			MaskTextureArea = maskTextureArea,
			MaskBounds = maskBounds,
			FillColor = color
		};
		_vertices[usedQuads * 4 + 3] = new Batcher2DVertex
		{
			Position = Vector3.Transform(bottomRight, _transformationMatrix),
			TextureCoordinates = new UShortVector2(x2, num2),
			Scissor = scissor,
			MaskTextureArea = maskTextureArea,
			MaskBounds = maskBounds,
			FillColor = color
		};
		_texturesPerQuad[usedQuads] = glTexture;
		_usedQuads++;
	}

	private unsafe bool GrowArraysIfAllowed()
	{
		if (!_allowBatcher2dToGrow)
		{
			return false;
		}
		Array.Resize(ref _vertices, _vertices.Length * 2 * 4);
		Array.Resize(ref _texturesPerQuad, _texturesPerQuad.Length * 2);
		Array.Resize(ref _maskTexturesPerQuad, _maskTexturesPerQuad.Length * 2);
		ushort[] array = new ushort[_texturesPerQuad.Length * 6];
		for (int i = 0; i < _texturesPerQuad.Length; i++)
		{
			array[i * 6] = (ushort)(i * 4);
			array[i * 6 + 1] = (ushort)(i * 4 + 1);
			array[i * 6 + 2] = (ushort)(i * 4 + 2);
			array[i * 6 + 3] = (ushort)(i * 4);
			array[i * 6 + 4] = (ushort)(i * 4 + 2);
			array[i * 6 + 5] = (ushort)(i * 4 + 3);
		}
		_graphics.GL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (ushort* ptr = array)
		{
			_graphics.GL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(array.Length * 2), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawTexture(Texture texture, Rectangle sourceRect, Vector3 position, float width, float height, UInt32Color color)
	{
		RequestDrawTexture(texture.GLTexture, texture.Width, texture.Height, sourceRect, position, width, height, color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawTextureTriangle(Texture texture, Rectangle sourceRect, Vector3 topLeft, Vector3 bottomLeft, Vector3 bottonRight, UInt32Color color)
	{
		RequestDrawTextureTriangle(texture.GLTexture, texture.Width, texture.Height, sourceRect, topLeft, bottomLeft, bottonRight, color, flip: false);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawTexture(Texture texture, Rectangle sourceRect, Rectangle destRect, UInt32Color color)
	{
		RequestDrawTexture(texture.GLTexture, texture.Width, texture.Height, sourceRect, new Vector3(destRect.X, destRect.Y, 0f), destRect.Width, destRect.Height, color);
	}

	public void RequestDrawPatch(Texture texture, Rectangle sourceRect, int sourceHorizontalBorder, int sourceVerticalBorder, int sourceScale, Vector3 position, float width, float height, float borderScale, UInt32Color color)
	{
		float num = (float)sourceHorizontalBorder * borderScale;
		float num2 = (float)sourceVerticalBorder * borderScale;
		sourceHorizontalBorder *= sourceScale;
		sourceVerticalBorder *= sourceScale;
		Rectangle sourceRect2 = new Rectangle(sourceRect.X + sourceHorizontalBorder, sourceRect.Y + sourceVerticalBorder, sourceRect.Width - sourceHorizontalBorder * 2, sourceRect.Height - sourceVerticalBorder * 2);
		Vector3 position2 = new Vector3(position.X + num, position.Y + num2, position.Z);
		float num3 = width - num * 2f;
		float num4 = height - num2 * 2f;
		if (num * 2f < width && num2 * 2f < height)
		{
			RequestDrawTexture(texture, sourceRect2, position2, num3, num4, color);
		}
		float num5 = System.Math.Min(num, width);
		float num6 = System.Math.Max(0f, System.Math.Min(num * 2f, width) - num);
		float num7 = System.Math.Min(num2, height);
		float num8 = System.Math.Max(0f, System.Math.Min(num2 * 2f, height) - num2);
		if (_opacityOverride.HasValue)
		{
			color.SetA(_opacityOverride.GetValueOrDefault());
		}
		if (num != 0f && num2 != 0f)
		{
			Rectangle sourceRect3 = new Rectangle(sourceRect.X, sourceRect.Y, sourceHorizontalBorder, sourceVerticalBorder);
			Vector3 vector = position;
			RequestDrawTexture(texture, sourceRect3, position, num5, num7, color);
			Rectangle sourceRect4 = new Rectangle(sourceRect.Right - sourceHorizontalBorder, sourceRect.Y, sourceHorizontalBorder, sourceVerticalBorder);
			Vector3 position3 = new Vector3(position.X + width - num6, position.Y, position.Z);
			RequestDrawTexture(texture, sourceRect4, position3, num6, num7, color);
			Rectangle sourceRect5 = new Rectangle(sourceRect.X, sourceRect.Bottom - sourceVerticalBorder, sourceHorizontalBorder, sourceVerticalBorder);
			Vector3 position4 = new Vector3(position.X, position.Y + height - num8, position.Z);
			RequestDrawTexture(texture, sourceRect5, position4, num5, num8, color);
			Rectangle sourceRect6 = new Rectangle(sourceRect.Right - sourceHorizontalBorder, sourceRect.Bottom - sourceVerticalBorder, sourceHorizontalBorder, sourceVerticalBorder);
			Vector3 position5 = new Vector3(position.X + width - num6, position.Y + height - num8, position.Z);
			RequestDrawTexture(texture, sourceRect6, position5, num6, num8, color);
		}
		if (num != 0f && num4 != 0f)
		{
			if (num5 > 0f)
			{
				Rectangle sourceRect7 = new Rectangle(sourceRect.X, sourceRect.Y + sourceVerticalBorder, sourceHorizontalBorder, sourceRect2.Height);
				Vector3 position6 = new Vector3(position.X, position.Y + num2, position.Z);
				RequestDrawTexture(texture, sourceRect7, position6, num5, num4, color);
			}
			if (num6 > 0f)
			{
				Rectangle sourceRect8 = new Rectangle(sourceRect.Right - sourceHorizontalBorder, sourceRect.Y + sourceVerticalBorder, sourceHorizontalBorder, sourceRect2.Height);
				Vector3 position7 = new Vector3(position.X + width - num6, position.Y + num2, position.Z);
				RequestDrawTexture(texture, sourceRect8, position7, num6, num4, color);
			}
		}
		if (num2 != 0f && num3 != 0f)
		{
			if (num7 > 0f)
			{
				Rectangle sourceRect9 = new Rectangle(sourceRect.X + sourceHorizontalBorder, sourceRect.Y, sourceRect2.Width, sourceVerticalBorder);
				Vector3 position8 = new Vector3(position.X + num, position.Y, position.Z);
				RequestDrawTexture(texture, sourceRect9, position8, num3, num7, color);
			}
			if (num8 > 0f)
			{
				Rectangle sourceRect10 = new Rectangle(sourceRect.X + sourceHorizontalBorder, sourceRect.Bottom - sourceVerticalBorder, sourceRect2.Width, sourceVerticalBorder);
				Vector3 position9 = new Vector3(position.X + num, position.Y + height - num8, position.Z);
				RequestDrawTexture(texture, sourceRect10, position9, num3, num8, color);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawPatch(TexturePatch patch, Vector3 position, float width, float height, float borderScale)
	{
		RequestDrawPatch(patch.TextureArea.Texture, patch.TextureArea.Rectangle, patch.HorizontalBorder, patch.VerticalBorder, patch.TextureArea.Scale, position, width, height, borderScale, patch.Color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawPatch(TexturePatch patch, Rectangle destRect, float borderScale)
	{
		RequestDrawPatch(patch.TextureArea.Texture, patch.TextureArea.Rectangle, patch.HorizontalBorder, patch.VerticalBorder, patch.TextureArea.Scale, new Vector3(destRect.X, destRect.Y, 0f), destRect.Width, destRect.Height, borderScale, patch.Color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawPatch(TexturePatch patch, Rectangle destRect, float borderScale, UInt32Color colorOverride)
	{
		RequestDrawPatch(patch.TextureArea.Texture, patch.TextureArea.Rectangle, patch.HorizontalBorder, patch.VerticalBorder, patch.TextureArea.Scale, new Vector3(destRect.X, destRect.Y, 0f), destRect.Width, destRect.Height, borderScale, colorOverride);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawPatch(Texture texture, Rectangle sourceRect, int sourceHorizontalBorder, int sourceVerticalBorder, int sourceScale, Rectangle destRect, float borderScale, UInt32Color color)
	{
		RequestDrawPatch(texture, sourceRect, sourceHorizontalBorder, sourceVerticalBorder, sourceScale, new Vector3(destRect.X, destRect.Y, 0f), destRect.Width, destRect.Height, borderScale, color);
	}

	public void RequestDrawOutline(Texture texture, Rectangle sourceRect, Vector3 position, float width, float height, float borderSize, UInt32Color color)
	{
		RequestDrawTexture(texture, sourceRect, new Vector3(position.X, position.Y, position.Z), width, borderSize, color);
		RequestDrawTexture(texture, sourceRect, new Vector3(position.X, position.Y + height - borderSize, position.Z), width, borderSize, color);
		RequestDrawTexture(texture, sourceRect, new Vector3(position.X, position.Y, position.Z), borderSize, height, color);
		RequestDrawTexture(texture, sourceRect, new Vector3(position.X + width - borderSize, position.Y, position.Z), borderSize, height, color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestDrawOutline(Texture texture, Rectangle sourceRect, Rectangle destRect, float borderSize, UInt32Color color)
	{
		RequestDrawOutline(texture, sourceRect, new Vector3(destRect.X, destRect.Y, 0f), destRect.Width, destRect.Height, borderSize, color);
	}

	public void RequestDrawText(Font font, float size, string text, Vector3 position, UInt32Color color, bool isBold = false, bool isItalics = false, float letterSpacing = 0f)
	{
		if (_usedQuads + text.Length >= _texturesPerQuad.Length)
		{
			Logger.Warn("Maximum quads {0} for UI reached!", _vertices.Length);
			if (!GrowArraysIfAllowed())
			{
				return;
			}
		}
		int width = font.TextureAtlas.Width;
		int height = font.TextureAtlas.Height;
		float num = size / (float)font.BaseSize;
		byte fillThreshold = (byte)(isBold ? 32 : 0);
		byte fillBlurAmount = (byte)(255f / ((float)font.Spread * num));
		byte outlineThreshold = 0;
		byte outlineBlurAmount = 0;
		float num2 = (isItalics ? (size / 4f) : 0f);
		MaskSetup maskSetup = ((_maskStack.Count > 0) ? _maskStack.Peek() : null);
		UShortVector4 maskBounds = maskSetup?.Bounds ?? UShortVector4.Zero;
		Vector4 maskTextureArea = Vector4.Zero;
		GLTexture gLTexture = GLTexture.None;
		if (maskSetup != null)
		{
			Texture texture = maskSetup.Area.Texture;
			gLTexture = texture.GLTexture;
			Rectangle rectangle = maskSetup.Area.Rectangle;
			float x = (float)rectangle.X / (float)texture.Width;
			float y = (float)rectangle.Y / (float)texture.Height;
			float z = (float)rectangle.Width / (float)texture.Width;
			float w = (float)rectangle.Height / (float)texture.Height;
			maskTextureArea = new Vector4(x, y, z, w);
		}
		Rectangle rectangle2 = ((_scissorStack.Count > 0) ? _scissorStack.Peek() : new Rectangle(0, 0, 65535, 65535));
		Vector3 position2 = new Vector3(rectangle2.X, rectangle2.Y, 0f);
		Vector3.Transform(ref position2, ref _transformationMatrix, out position2);
		UShortVector4 scissor = new UShortVector4((ushort)System.Math.Max(0f, position2.X), (ushort)System.Math.Max(0f, position2.Y), (ushort)System.Math.Max(0, rectangle2.Width), (ushort)System.Math.Max(0, rectangle2.Height));
		int num3 = _usedQuads;
		position.X -= (float)font.Spread * num;
		position.Y -= (float)font.Spread * num;
		byte fontId = (byte)font.FontId;
		if (_opacityOverride.HasValue)
		{
			color.SetA(_opacityOverride.GetValueOrDefault());
		}
		foreach (ushort key in text)
		{
			if (!font.GlyphAtlasRectangles.TryGetValue(key, out var value))
			{
				value = font.FallbackGlyphAtlasRectangle;
			}
			if (!font.GlyphAdvances.TryGetValue(key, out var value2))
			{
				value2 = font.FallbackGlyphAdvance;
			}
			Vector3 position3 = new Vector3(position.X + (float)value.Width * num + num2, position.Y, position.Z);
			Vector3 position4 = new Vector3(position.X + num2, position.Y, position.Z);
			Vector3 position5 = new Vector3(position.X - num2, position.Y + (float)value.Height * num, position.Z);
			Vector3 position6 = new Vector3(position.X + (float)value.Width * num - num2, position.Y + (float)value.Height * num, position.Z);
			position.X += value2 * num + letterSpacing;
			if (!(position3.X < (float)(int)scissor.X) && !(position6.Y < (float)(int)scissor.Y) && !(position4.X > (float)(scissor.X + scissor.Z)) && !(position4.Y > (float)(scissor.Y + scissor.W)))
			{
				ushort x2 = (ushort)((float)(65535 * value.X) / (float)width);
				ushort x3 = (ushort)((float)(65535 * (value.X + value.Width)) / (float)width);
				ushort y2 = (ushort)((float)(65535 * value.Y) / (float)height);
				ushort y3 = (ushort)((float)(65535 * (value.Y + value.Height)) / (float)height);
				_vertices[num3 * 4].TextureCoordinates = new UShortVector2(x3, y2);
				_vertices[num3 * 4 + 1].TextureCoordinates = new UShortVector2(x2, y2);
				_vertices[num3 * 4 + 2].TextureCoordinates = new UShortVector2(x2, y3);
				_vertices[num3 * 4 + 3].TextureCoordinates = new UShortVector2(x3, y3);
				_vertices[num3 * 4].Scissor = scissor;
				_vertices[num3 * 4 + 1].Scissor = scissor;
				_vertices[num3 * 4 + 2].Scissor = scissor;
				_vertices[num3 * 4 + 3].Scissor = scissor;
				_vertices[num3 * 4].MaskTextureArea = maskTextureArea;
				_vertices[num3 * 4 + 1].MaskTextureArea = maskTextureArea;
				_vertices[num3 * 4 + 2].MaskTextureArea = maskTextureArea;
				_vertices[num3 * 4 + 3].MaskTextureArea = maskTextureArea;
				_vertices[num3 * 4].MaskBounds = maskBounds;
				_vertices[num3 * 4 + 1].MaskBounds = maskBounds;
				_vertices[num3 * 4 + 2].MaskBounds = maskBounds;
				_vertices[num3 * 4 + 3].MaskBounds = maskBounds;
				_vertices[num3 * 4].FontId = fontId;
				_vertices[num3 * 4 + 1].FontId = fontId;
				_vertices[num3 * 4 + 2].FontId = fontId;
				_vertices[num3 * 4 + 3].FontId = fontId;
				_vertices[num3 * 4].Position = Vector3.Transform(position3, _transformationMatrix);
				_vertices[num3 * 4 + 1].Position = Vector3.Transform(position4, _transformationMatrix);
				_vertices[num3 * 4 + 2].Position = Vector3.Transform(position5, _transformationMatrix);
				_vertices[num3 * 4 + 3].Position = Vector3.Transform(position6, _transformationMatrix);
				_vertices[num3 * 4].FillColor = color;
				_vertices[num3 * 4 + 1].FillColor = color;
				_vertices[num3 * 4 + 2].FillColor = color;
				_vertices[num3 * 4 + 3].FillColor = color;
				_vertices[num3 * 4].OutlineColor = color;
				_vertices[num3 * 4 + 1].OutlineColor = color;
				_vertices[num3 * 4 + 2].OutlineColor = color;
				_vertices[num3 * 4 + 3].OutlineColor = color;
				_vertices[num3 * 4].FillThreshold = fillThreshold;
				_vertices[num3 * 4].FillBlurAmount = fillBlurAmount;
				_vertices[num3 * 4].OutlineThreshold = outlineThreshold;
				_vertices[num3 * 4].OutlineBlurAmount = outlineBlurAmount;
				_vertices[num3 * 4 + 1].FillThreshold = fillThreshold;
				_vertices[num3 * 4 + 1].FillBlurAmount = fillBlurAmount;
				_vertices[num3 * 4 + 1].OutlineThreshold = outlineThreshold;
				_vertices[num3 * 4 + 1].OutlineBlurAmount = outlineBlurAmount;
				_vertices[num3 * 4 + 2].FillThreshold = fillThreshold;
				_vertices[num3 * 4 + 2].FillBlurAmount = fillBlurAmount;
				_vertices[num3 * 4 + 2].OutlineThreshold = outlineThreshold;
				_vertices[num3 * 4 + 2].OutlineBlurAmount = outlineBlurAmount;
				_vertices[num3 * 4 + 3].FillThreshold = fillThreshold;
				_vertices[num3 * 4 + 3].FillBlurAmount = fillBlurAmount;
				_vertices[num3 * 4 + 3].OutlineThreshold = outlineThreshold;
				_vertices[num3 * 4 + 3].OutlineBlurAmount = outlineBlurAmount;
				_texturesPerQuad[num3] = GLTexture.None;
				_maskTexturesPerQuad[num3] = gLTexture;
				num3 = (_usedQuads = num3 + 1);
			}
		}
	}
}
