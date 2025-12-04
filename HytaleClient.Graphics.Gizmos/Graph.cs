using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HytaleClient.Core;
using HytaleClient.Graphics.Batcher2D;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos;

internal class Graph : Disposable
{
	public struct DataSet
	{
		public int FirstValueIndex { get; private set; }

		public int NextValueIndex { get; private set; }

		public int MaxDataCount { get; private set; }

		public float[] History { get; private set; }

		public float AverageValue { get; private set; }

		public float MaxValue { get; private set; }

		public DataSet(int maxData)
		{
			FirstValueIndex = 0;
			NextValueIndex = 0;
			MaxDataCount = maxData;
			History = new float[maxData];
			AverageValue = 0f;
			MaxValue = 0f;
		}

		public int GetValuesCount()
		{
			return (FirstValueIndex < NextValueIndex) ? NextValueIndex : MaxDataCount;
		}

		public void RecordValue(float value)
		{
			float num = History[NextValueIndex];
			History[NextValueIndex] = value;
			AverageValue = AverageValue - AverageValue / (float)MaxDataCount + value / (float)MaxDataCount;
			if (num != 0f)
			{
				AverageValue = (AverageValue - num / (float)MaxDataCount) / (1f - 1f / (float)MaxDataCount);
			}
			MaxValue = ((MaxValue < value) ? value : MaxValue);
			NextValueIndex = (NextValueIndex + 1) % MaxDataCount;
			if (NextValueIndex == FirstValueIndex)
			{
				FirstValueIndex = (FirstValueIndex + 1) % MaxDataCount;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Axis
	{
		public float Value;

		public string Label;

		public float Margin;
	}

	private const float LabelMargin = 10f;

	private const float TitleMargin = 6f;

	private const int VerticesByPoint = 32;

	private readonly GraphicsDevice _graphics;

	private readonly Font _font;

	public Vector3 Position = Vector3.Zero;

	public Vector3 LabelPosition = Vector3.Zero;

	public Vector3 Scale = Vector3.One;

	public Vector3 Color = Vector3.Zero;

	private float _textHeight = 1f;

	private int _historyDuration;

	private readonly ushort[] _indices;

	private readonly float[] _vertices;

	private readonly List<Axis> _axes = new List<Axis>();

	public string AxisUnit;

	private float _axisScale = 1f;

	private Matrix _dataDrawMatrix;

	private readonly GLBuffer _indicesBuffer;

	private readonly GLVertexArray _vertexArray;

	private readonly GLBuffer _verticesBuffer;

	private readonly string _title;

	private HytaleClient.Graphics.Batcher2D.Batcher2D _batcher2D;

	public unsafe Graph(GraphicsDevice graphics, HytaleClient.Graphics.Batcher2D.Batcher2D batcher2d, Font font, int historyDuration, string title = "")
	{
		_graphics = graphics;
		_batcher2D = batcher2d;
		_font = font;
		_historyDuration = historyDuration;
		_indices = new ushort[historyDuration * 6];
		_vertices = new float[historyDuration * 32];
		GLFunctions gL = _graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		for (int i = 0; i < historyDuration; i++)
		{
			_vertices[i * 32] = 1f + (float)i;
			_vertices[i * 32 + 1] = 0f;
			_vertices[i * 32 + 2] = 0f;
			_vertices[i * 32 + 3] = 0f;
			_vertices[i * 32 + 4] = 0f;
			_vertices[i * 32 + 5] = 0f;
			_vertices[i * 32 + 6] = 0f;
			_vertices[i * 32 + 7] = 0f;
			_vertices[i * 32 + 8] = 0f + (float)i;
			_vertices[i * 32 + 9] = 0f;
			_vertices[i * 32 + 10] = 0f;
			_vertices[i * 32 + 11] = 0f;
			_vertices[i * 32 + 12] = 0f;
			_vertices[i * 32 + 13] = 0f;
			_vertices[i * 32 + 14] = 0f;
			_vertices[i * 32 + 15] = 0f;
			_vertices[i * 32 + 16] = 0f + (float)i;
			_vertices[i * 32 + 17] = 1f;
			_vertices[i * 32 + 18] = 0f;
			_vertices[i * 32 + 19] = 0f;
			_vertices[i * 32 + 20] = 0f;
			_vertices[i * 32 + 21] = 0f;
			_vertices[i * 32 + 22] = 0f;
			_vertices[i * 32 + 23] = 0f;
			_vertices[i * 32 + 24] = 1f + (float)i;
			_vertices[i * 32 + 25] = 1f;
			_vertices[i * 32 + 26] = 0f;
			_vertices[i * 32 + 27] = 0f;
			_vertices[i * 32 + 28] = 0f;
			_vertices[i * 32 + 29] = 0f;
			_vertices[i * 32 + 30] = 0f;
			_vertices[i * 32 + 31] = 0f;
		}
		for (int j = 0; j < historyDuration; j++)
		{
			_indices[j * 6] = (ushort)(j * 4);
			_indices[j * 6 + 1] = (ushort)(j * 4 + 1);
			_indices[j * 6 + 2] = (ushort)(j * 4 + 2);
			_indices[j * 6 + 3] = (ushort)(j * 4);
			_indices[j * 6 + 4] = (ushort)(j * 4 + 2);
			_indices[j * 6 + 5] = (ushort)(j * 4 + 3);
		}
		fixed (float* ptr = _vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertices.Length * 4), (IntPtr)ptr, GL.DYNAMIC_DRAW);
		}
		fixed (ushort* ptr2 = _indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_indices.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.EnableVertexAttribArray(basicProgram.AttribPosition.Index);
		gL.VertexAttribPointer(basicProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(basicProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(basicProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
		if (!string.IsNullOrEmpty(title))
		{
			_title = title;
		}
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteBuffer(_indicesBuffer);
		gL.DeleteVertexArray(_vertexArray);
	}

	public void AddAxis(string name, float value)
	{
		Axis axis = default(Axis);
		axis.Value = value;
		axis.Label = name;
		axis.Margin = _font.CalculateTextWidth(name) * _textHeight / (float)_font.BaseSize;
		Axis item = axis;
		_axes.Add(item);
	}

	public void UpdateTextHeight(float textHeight)
	{
		_textHeight = textHeight;
		for (int i = 0; i < _axes.Count; i++)
		{
			_axes[i] = new Axis
			{
				Value = _axes[i].Value,
				Label = _axes[i].Label,
				Margin = _font.CalculateTextWidth(_axes[i].Label) * _textHeight / (float)_font.BaseSize
			};
		}
	}

	public void UpdateHistory(DataSet data, float scale = 1f)
	{
		if (scale != 1f)
		{
			_axisScale = scale;
			for (int i = 0; i < data.GetValuesCount(); i++)
			{
				int num = (i + data.FirstValueIndex) % data.MaxDataCount;
				_vertices[i * 32 + 17] = data.History[num] / _axisScale;
				_vertices[i * 32 + 25] = data.History[num] / _axisScale;
			}
		}
		else
		{
			_axisScale = 1f;
			for (int j = 0; j < data.GetValuesCount(); j++)
			{
				int num2 = (j + data.FirstValueIndex) % data.MaxDataCount;
				_vertices[j * 32 + 17] = data.History[num2];
				_vertices[j * 32 + 25] = data.History[num2];
			}
		}
	}

	public void UpdateAxisData(ref Matrix projectionMatrix)
	{
		Matrix.CreateScale(ref Scale, out _dataDrawMatrix);
		Matrix.CreateTranslation(ref Position, out var result);
		Matrix.Multiply(ref _dataDrawMatrix, ref result, out _dataDrawMatrix);
		Matrix.Multiply(ref _dataDrawMatrix, ref projectionMatrix, out _dataDrawMatrix);
	}

	public void PrepareForDrawAxisAndLabels()
	{
		if (_title != null)
		{
			_batcher2D.RequestDrawText(_font, _textHeight, _title, new Vector3(LabelPosition.X, LabelPosition.Y, 0f), UInt32Color.White);
		}
		for (int i = 0; i < _axes.Count; i++)
		{
			float num = LabelPosition.Y - _axes[i].Value / _axisScale * Scale.Y;
			_batcher2D.RequestDrawText(_font, _textHeight, _axes[i].Label, new Vector3(LabelPosition.X - 10f - _axes[i].Margin, num, 0f), UInt32Color.White);
			_batcher2D.RequestDrawTexture(_graphics.WhitePixelTexture, new Rectangle(0, 0, 1, 1), new Vector3((int)LabelPosition.X, (int)num, 0f), (int)((float)_historyDuration * Scale.X), 1f, UInt32Color.White);
		}
	}

	public unsafe void DrawData()
	{
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		basicProgram.Opacity.AssertValue(0.8f);
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		basicProgram.Color.SetValue(Color);
		basicProgram.MVPMatrix.SetValue(ref _dataDrawMatrix);
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (float* ptr = _vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertices.Length * 4), (IntPtr)ptr, GL.DYNAMIC_DRAW);
		}
		gL.DrawElements(GL.TRIANGLES, _indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
	}
}
