using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Gizmos.Models;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Graphics.Gizmos;

internal class BrushToolRenderer : Disposable
{
	private readonly GraphicsDevice _graphics;

	private readonly GLVertexArray _vertexArray;

	private readonly GLBuffer _verticesBuffer;

	private readonly GLBuffer _indicesBuffer;

	private PrimitiveModelData _modelData;

	private BrushData _brushData;

	private Matrix _tempMatrix;

	private Matrix _matrix;

	public BrushToolRenderer(GraphicsDevice graphics)
	{
		_graphics = graphics;
		GLFunctions gL = _graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		ForceFieldProgram builderToolProgram = _graphics.GPUProgramStore.BuilderToolProgram;
		gL.EnableVertexAttribArray(builderToolProgram.AttribPosition.Index);
		gL.VertexAttribPointer(builderToolProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(builderToolProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(builderToolProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteVertexArray(_vertexArray);
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteBuffer(_indicesBuffer);
	}

	public unsafe void UpdateBrushData(BrushData brushData, PrimitiveModelData modelData)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected I4, but got Unknown
		if (modelData != null)
		{
			_modelData = modelData;
			_brushData = brushData;
		}
		else
		{
			if (brushData.Equals(_brushData))
			{
				return;
			}
			_brushData = brushData;
			BrushShape shape = brushData.Shape;
			BrushShape val = shape;
			switch (val - 1)
			{
			case 0:
				_modelData = SphereModel.BuildModelData((float)_brushData.Width / 2f, _brushData.Height, 16, 16);
				break;
			case 1:
				_modelData = CylinderModel.BuildModelData((float)_brushData.Width / 2f, _brushData.Height, 16);
				break;
			case 2:
			case 3:
				_modelData = ConeModel.BuildModelData((float)_brushData.Width / 2f, _brushData.Height, 16);
				break;
			case 4:
			case 5:
				_modelData = PyramidModel.BuildModelData((float)_brushData.Width / 2f, _brushData.Height, 5);
				break;
			default:
				_modelData = CubeModel.BuildModelData((float)_brushData.Width / 2f, (float)_brushData.Height / 2f);
				break;
			}
		}
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (float* ptr = _modelData.Vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_modelData.Vertices.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (ushort* ptr2 = _modelData.Indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_modelData.Indices.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
	}

	public void Draw(ref Matrix viewProjectionMatrix, ref Matrix viewMatrix, Vector2 viewportSize, Vector3 position, Vector3 color, float opacity, bool drawIntersectionHighlight = false)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Invalid comparison between Unknown and I4
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Invalid comparison between Unknown and I4
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Invalid comparison between Unknown and I4
		ForceFieldProgram builderToolProgram = _graphics.GPUProgramStore.BuilderToolProgram;
		GLFunctions gL = _graphics.GL;
		builderToolProgram.AssertInUse();
		if (_modelData == null)
		{
			return;
		}
		ushort[] indices = _modelData.Indices;
		if (indices != null && indices.Length == 0)
		{
			return;
		}
		float num = 0.5f;
		if (_brushData != null)
		{
			if ((int)_brushData.Shape == 4 || (int)_brushData.Shape == 6)
			{
				Matrix.CreateRotationX((float)System.Math.PI, out _matrix);
			}
			else
			{
				float num2 = ((float)_brushData.Width + 0.1f) / (float)_brushData.Width;
				float yScale = ((float)_brushData.Height + 0.1f) / (float)_brushData.Height;
				Matrix.CreateScale(num2, yScale, num2, out _matrix);
			}
			if ((int)_brushData.Origin == 2)
			{
				num = (float)(-_brushData.Height) / 2f + 1f;
			}
			else if ((int)_brushData.Origin == 1)
			{
				num = (float)_brushData.Height / 2f + 1f;
			}
		}
		else
		{
			Matrix.CreateScale(1f, 1f, 1f, out _matrix);
		}
		Matrix.CreateTranslation(position.X + 0.5f, position.Y + num, position.Z + 0.5f, out _tempMatrix);
		Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		Matrix matrix = Matrix.Transpose(Matrix.Invert(_matrix));
		builderToolProgram.ModelMatrix.SetValue(ref _matrix);
		builderToolProgram.ViewMatrix.SetValue(ref viewMatrix);
		builderToolProgram.ViewProjectionMatrix.SetValue(ref viewProjectionMatrix);
		builderToolProgram.CurrentInvViewportSize.SetValue(Vector2.One / viewportSize);
		builderToolProgram.NormalMatrix.SetValue(ref matrix);
		builderToolProgram.UVAnimationSpeed.SetValue(0f, 0f);
		builderToolProgram.OutlineMode.SetValue(builderToolProgram.OutlineModeNone);
		builderToolProgram.DrawAndBlendMode.SetValue(builderToolProgram.DrawModeColor, builderToolProgram.BlendModeLinear);
		Vector4 value = new Vector4(1f, 1f, 1f, 0.4f);
		float value2 = (drawIntersectionHighlight ? 0.5f : 0f);
		builderToolProgram.IntersectionHighlightColorOpacity.SetValue(value);
		builderToolProgram.IntersectionHighlightThickness.SetValue(value2);
		gL.BindVertexArray(_vertexArray);
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.GEQUAL : GL.LEQUAL);
		builderToolProgram.ColorOpacity.SetValue(color.X, color.Y, color.Z, opacity * 0.5f);
		gL.DrawElements(GL.TRIANGLES, _modelData.Indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
		builderToolProgram.ColorOpacity.SetValue(color.X, color.Y, color.Z, opacity);
		gL.DrawElements(GL.TRIANGLES, _modelData.Indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
		gL.DrawElements(GL.ONE, _modelData.Indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
	}
}
