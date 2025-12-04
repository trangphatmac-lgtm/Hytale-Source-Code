using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Fonts;

internal class TextRenderer : Disposable
{
	public enum TextAlignment
	{
		Left,
		Center,
		Right
	}

	public enum TextVerticalAlignment
	{
		Top,
		Middle,
		Bottom
	}

	private readonly Font _font;

	public const uint WhiteColor = uint.MaxValue;

	public const uint LightGrayColor = 4290822336u;

	public const uint MediumGrayColor = 4288716960u;

	public const uint GrayColor = 4286611584u;

	public const uint BlackColor = 4278190080u;

	public uint FillColor;

	public uint OutlineColor;

	private readonly GraphicsDevice _graphics;

	private string _text;

	private float _textWidth;

	private float _textHeight;

	private TextVertex[] _vertices;

	private ushort[] _indices;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				BuildGeometry();
			}
		}
	}

	public ushort IndicesCount => (ushort)_indices.Length;

	public GLVertexArray VertexArray { get; private set; }

	public TextRenderer(GraphicsDevice graphics, Font font, string text, uint fillColor = uint.MaxValue, uint outlineColor = 4278190080u)
	{
		_graphics = graphics;
		_font = font;
		_text = text;
		FillColor = fillColor;
		OutlineColor = outlineColor;
		GLFunctions gL = _graphics.GL;
		VertexArray = gL.GenVertexArray();
		_verticesBuffer = gL.GenBuffer();
		_indicesBuffer = gL.GenBuffer();
		gL.BindVertexArray(VertexArray);
		gL.BindBuffer(VertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		TextProgram textProgram = _graphics.GPUProgramStore.TextProgram;
		IntPtr zero = IntPtr.Zero;
		gL.EnableVertexAttribArray(textProgram.AttribPosition.Index);
		gL.VertexAttribPointer(textProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, TextVertex.Size, zero);
		zero += 12;
		gL.EnableVertexAttribArray(textProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(textProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, TextVertex.Size, zero);
		zero += 8;
		gL.EnableVertexAttribArray(textProgram.AttribFillColor.Index);
		gL.VertexAttribIPointer(textProgram.AttribFillColor.Index, 1, GL.UNSIGNED_INT, TextVertex.Size, zero);
		zero += 4;
		gL.EnableVertexAttribArray(textProgram.AttribOutlineColor.Index);
		gL.VertexAttribIPointer(textProgram.AttribOutlineColor.Index, 1, GL.UNSIGNED_INT, TextVertex.Size, zero);
		zero += 4;
		BuildGeometry();
	}

	private unsafe void BuildGeometry()
	{
		_vertices = new TextVertex[_text.Length * 4];
		_indices = new ushort[_text.Length * 6];
		for (int i = 0; i < _text.Length; i++)
		{
			_indices[i * 6] = (ushort)(i * 4);
			_indices[i * 6 + 1] = (ushort)(i * 4 + 1);
			_indices[i * 6 + 2] = (ushort)(i * 4 + 2);
			_indices[i * 6 + 3] = (ushort)(i * 4);
			_indices[i * 6 + 4] = (ushort)(i * 4 + 2);
			_indices[i * 6 + 5] = (ushort)(i * 4 + 3);
		}
		int num = 0;
		_textWidth = 0f;
		_textHeight = _font.LineSkip;
		Vector3 vector = new Vector3(-_font.Spread / 2, 0f, 0f);
		string text = _text;
		foreach (ushort key in text)
		{
			if (_font.GlyphAtlasRectangles.TryGetValue(key, out var value))
			{
				float x = (float)value.X / (float)_font.TextureAtlas.Width;
				float x2 = (float)(value.X + value.Width) / (float)_font.TextureAtlas.Width;
				float y = (float)value.Y / (float)_font.TextureAtlas.Height;
				float y2 = (float)(value.Y + value.Height) / (float)_font.TextureAtlas.Height;
				_vertices[num * 4].TextureCoordinates = new Vector2(x2, y);
				_vertices[num * 4 + 1].TextureCoordinates = new Vector2(x, y);
				_vertices[num * 4 + 3].TextureCoordinates = new Vector2(x2, y2);
				_vertices[num * 4 + 2].TextureCoordinates = new Vector2(x, y2);
				_vertices[num * 4].Position = vector + new Vector3(value.Width, value.Height, 0f);
				_vertices[num * 4 + 1].Position = vector + new Vector3(0f, value.Height, 0f);
				_vertices[num * 4 + 2].Position = vector;
				_vertices[num * 4 + 3].Position = vector + new Vector3(value.Width, 0f, 0f);
				_vertices[num * 4].FillColor = FillColor;
				_vertices[num * 4 + 1].FillColor = FillColor;
				_vertices[num * 4 + 2].FillColor = FillColor;
				_vertices[num * 4 + 3].FillColor = FillColor;
				_vertices[num * 4].OutlineColor = OutlineColor;
				_vertices[num * 4 + 1].OutlineColor = OutlineColor;
				_vertices[num * 4 + 2].OutlineColor = OutlineColor;
				_vertices[num * 4 + 3].OutlineColor = OutlineColor;
				float num2 = _font.GlyphAdvances[key];
				vector += new Vector3(num2, 0f, 0f);
				_textWidth += num2;
			}
			num++;
		}
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(VertexArray);
		gL.BindBuffer(VertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		gL.BindBuffer(VertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (TextVertex* ptr = _vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertices.Length * TextVertex.Size), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		fixed (ushort* ptr2 = _indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_indices.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
	}

	public float GetHorizontalOffset(TextAlignment alignment)
	{
		return alignment switch
		{
			TextAlignment.Left => 0f, 
			TextAlignment.Right => _textWidth, 
			TextAlignment.Center => _textWidth / 2f, 
			_ => throw new Exception("Unreachable"), 
		};
	}

	public float GetVerticalOffset(TextVerticalAlignment verticalAlignment)
	{
		return verticalAlignment switch
		{
			TextVerticalAlignment.Bottom => 0f, 
			TextVerticalAlignment.Top => _textHeight, 
			TextVerticalAlignment.Middle => _textHeight / 2f, 
			_ => throw new Exception("Unreachable"), 
		};
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteVertexArray(VertexArray);
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteBuffer(_indicesBuffer);
	}

	public void Draw()
	{
		if (!(_text == string.Empty))
		{
			GLFunctions gL = _graphics.GL;
			_graphics.GPUProgramStore.TextProgram.AssertInUse();
			gL.AssertTextureBound(GL.TEXTURE0, _font.TextureAtlas.GLTexture);
			gL.BindVertexArray(VertexArray);
			gL.DrawElements(GL.TRIANGLES, _indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
		}
	}
}
