using System;
using HytaleClient.Core;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Gizmos;

internal class SelectionToolRenderer : Disposable
{
	public enum SelectionDrawMode
	{
		Normal,
		Combine,
		Subtract
	}

	private readonly GraphicsDevice _graphics;

	private readonly Font _font;

	private float[] _vertices;

	private ushort[] _indices;

	private readonly GLVertexArray _vertexArray;

	private readonly GLBuffer _verticesBuffer;

	private readonly GLBuffer _indicesBuffer;

	private readonly QuadRenderer _faceHighlightRenderer;

	private readonly BoxRenderer _boxRenderer;

	private Mesh _meshBox;

	private readonly BoundingBox _originBox = new BoundingBox(new Vector3(-0.02f, -0.02f, -0.02f), new Vector3(1.02f, 1.02f, 1.02f));

	private BoundingBox _selectionBox;

	private readonly TextRenderer _textRenderer;

	private Vector3 _centerPos;

	private Vector3 _selectionSize;

	private Vector3 _pos1;

	private Vector3 _pos2;

	private Matrix _tempMatrix;

	private Matrix _matrix;

	private const float GizmoOffset = 0.01f;

	public SelectionToolRenderer(GraphicsDevice graphics, Font font)
	{
		_graphics = graphics;
		_font = font;
		GLFunctions gL = _graphics.GL;
		_vertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_vertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.EnableVertexAttribArray(basicProgram.AttribPosition.Index);
		gL.VertexAttribPointer(basicProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(basicProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(basicProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
		_faceHighlightRenderer = new QuadRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram.AttribPosition, _graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
		_boxRenderer = new BoxRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_textRenderer = new TextRenderer(_graphics, _font, "Entity");
		ForceFieldProgram builderToolProgram = _graphics.GPUProgramStore.BuilderToolProgram;
		MeshProcessor.CreateBox(ref _meshBox, 2f, (int)builderToolProgram.AttribPosition.Index, (int)builderToolProgram.AttribTexCoords.Index, (int)builderToolProgram.AttribNormal.Index);
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _graphics.GL;
		gL.DeleteVertexArray(_vertexArray);
		gL.DeleteBuffer(_verticesBuffer);
		gL.DeleteBuffer(_indicesBuffer);
		_faceHighlightRenderer.Dispose();
		_boxRenderer.Dispose();
		_textRenderer.Dispose();
		_meshBox.Dispose();
	}

	public unsafe void UpdateSelection(Vector3 pos1, Vector3 pos2)
	{
		_pos1 = pos1;
		_pos2 = pos2;
		int num = (int)MathHelper.Min(pos1.X, pos2.X);
		int num2 = (int)MathHelper.Min(pos1.Y, pos2.Y);
		int num3 = (int)MathHelper.Min(pos1.Z, pos2.Z);
		int num4 = (int)MathHelper.Max(pos1.X, pos2.X) + 1;
		int num5 = (int)MathHelper.Max(pos1.Y, pos2.Y) + 1;
		int num6 = (int)MathHelper.Max(pos1.Z, pos2.Z) + 1;
		int num7 = num4 - num;
		int num8 = num5 - num2;
		int num9 = num6 - num3;
		_centerPos = new Vector3((float)num + (float)num7 / 2f, (float)num2 + (float)num8 / 2f, (float)num3 + (float)num9 / 2f);
		_selectionSize = new Vector3(num7, num8, num9);
		int num10 = (num7 + 1) * 4;
		int num11 = (num8 + 1) * 4;
		int num12 = (num9 + 1) * 4;
		int num13 = num10 + num11 + num12;
		_vertices = new float[num13 * 8];
		_indices = new ushort[num13 * 2];
		Vector3 zero = Vector3.Zero;
		int vertexInc = 0;
		ushort indexInc = 0;
		for (int i = 0; i <= num7; i++)
		{
			BuildLineLoop(ref vertexInc, ref indexInc, new Vector3(i, zero.Y, zero.Z), new Vector3(i, num8, zero.Z), new Vector3(i, num8, num9), new Vector3(i, zero.Y, num9));
		}
		for (int j = 0; j <= num9; j++)
		{
			BuildLineLoop(ref vertexInc, ref indexInc, new Vector3(zero.X, zero.Y, j), new Vector3(zero.X, num8, j), new Vector3(num7, num8, j), new Vector3(num7, zero.Y, j));
		}
		for (int k = 0; k <= num8; k++)
		{
			BuildLineLoop(ref vertexInc, ref indexInc, new Vector3(zero.X, k, zero.Z), new Vector3(zero.X, k, num9), new Vector3(num7, k, num9), new Vector3(num7, k, zero.Z));
		}
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		gL.BindBuffer(_vertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (float* ptr = _vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertices.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		gL.BindBuffer(_vertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (ushort* ptr2 = _indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_indices.Length * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
		_selectionBox = new BoundingBox(Vector3.Min(_pos1, _pos2), Vector3.Max(_pos1, _pos2) + Vector3.One);
	}

	private void BuildLineLoop(ref int vertexInc, ref ushort indexInc, Vector3 vec0, Vector3 vec1, Vector3 vec2, Vector3 vec3)
	{
		int num = vertexInc / 8;
		AddLineVertex(ref vertexInc, vec0);
		AddLineVertex(ref vertexInc, vec1);
		AddLineVertex(ref vertexInc, vec2);
		AddLineVertex(ref vertexInc, vec3);
		_indices[indexInc++] = (ushort)num;
		_indices[indexInc++] = (ushort)(num + 1);
		_indices[indexInc++] = (ushort)(num + 1);
		_indices[indexInc++] = (ushort)(num + 2);
		_indices[indexInc++] = (ushort)(num + 2);
		_indices[indexInc++] = (ushort)(num + 3);
		_indices[indexInc++] = (ushort)(num + 3);
		_indices[indexInc++] = (ushort)num;
	}

	private void AddLineVertex(ref int vertexInc, Vector3 pos)
	{
		_vertices[vertexInc++] = pos.X;
		_vertices[vertexInc++] = pos.Y;
		_vertices[vertexInc++] = pos.Z;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
		_vertices[vertexInc++] = 0f;
	}

	public void DrawGrid(ref Matrix viewProjectionMatrix, Vector3 positionOffset, Vector3 color, float opacity, SelectionDrawMode drawMode)
	{
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		basicProgram.Color.SetValue(color);
		Vector3 vector = _centerPos - _selectionSize * new Vector3(0.5f);
		Vector3 position = vector + positionOffset;
		Matrix.CreateTranslation(ref position, out _matrix);
		Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
		basicProgram.MVPMatrix.SetValue(ref _matrix);
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(_vertexArray);
		basicProgram.Opacity.SetValue(opacity);
		if (drawMode == SelectionDrawMode.Normal)
		{
			basicProgram.Color.SetValue(_graphics.WhiteColor);
		}
		else
		{
			basicProgram.Color.SetValue(_graphics.CyanColor);
		}
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
		gL.DrawElements(GL.ONE, _indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
		if (drawMode != 0)
		{
			basicProgram.Opacity.SetValue(opacity);
			basicProgram.Color.SetValue(_graphics.WhiteColor);
			gL.DrawElements(GL.ONE, _indices.Length, GL.UNSIGNED_SHORT, (IntPtr)0);
		}
	}

	public void DrawOutlineBox(ref Matrix viewProjectionMatrix, ref Matrix viewMatrix, Vector3 positionOffset, Vector2 viewportSize, Vector3 lineColor, Vector3 quadColor, float lineOpacity, float quadOpacity, bool drawIntersectionHighlight = true)
	{
		_ = _selectionBox;
		if (true)
		{
			ForceFieldProgram builderToolProgram = _graphics.GPUProgramStore.BuilderToolProgram;
			GLFunctions gL = _graphics.GL;
			gL.UseProgram(builderToolProgram);
			Vector3 scales = _selectionBox.GetSize() * new Vector3(0.5f, 0.5f, 0.5f);
			Vector3 position = _selectionBox.Min + scales;
			position += positionOffset;
			Matrix.CreateScale(ref scales, out _matrix);
			Matrix.CreateTranslation(ref position, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			Matrix matrix = Matrix.Transpose(Matrix.Invert(_matrix));
			builderToolProgram.ModelMatrix.SetValue(ref _matrix);
			builderToolProgram.ColorOpacity.SetValue(quadColor.X, quadColor.Y, quadColor.Z, quadOpacity);
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
			gL.BindVertexArray(_meshBox.VertexArray);
			gL.DrawArrays(GL.TRIANGLES, 0, _meshBox.Count);
			gL.UseProgram(_graphics.GPUProgramStore.BasicProgram);
			_boxRenderer.Draw(positionOffset, _selectionBox, viewProjectionMatrix, lineColor, lineOpacity, quadColor, 0f);
		}
	}

	public void DrawCornerBoxes(ref Matrix viewProjectionMatrix, Vector3 positionOffset, Vector3 pos1Color, Vector3 pos2Color, float pos1Opacity = 0.05f, float pos2Opacity = 0.05f)
	{
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		_graphics.GL.AssertTextureBound(GL.TEXTURE0, _graphics.WhitePixelTexture.GLTexture);
		GLFunctions gL = _graphics.GL;
		gL.DepthFunc(GL.ALWAYS);
		_boxRenderer.Draw(_pos1 + positionOffset, _originBox, viewProjectionMatrix, pos1Color, 0.4f, _graphics.GreenColor, pos1Opacity);
		_boxRenderer.Draw(_pos2 + positionOffset, _originBox, viewProjectionMatrix, pos2Color, 0.4f, _graphics.RedColor, pos2Opacity);
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}

	public void DrawResizeGizmoForFace(Vector3 playerPosition, ref Matrix viewProjectionMatrix, Vector3 selectionNormal, Vector3 color, float minGizmoSize, float maxGizmoSize, float percentageOfSelectionLengthGizmoShouldRender)
	{
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		basicProgram.Color.SetValue(color);
		float num = MathHelper.Clamp(_selectionSize.X * percentageOfSelectionLengthGizmoShouldRender, minGizmoSize, maxGizmoSize);
		float num2 = MathHelper.Clamp(_selectionSize.Y * percentageOfSelectionLengthGizmoShouldRender, minGizmoSize, maxGizmoSize);
		float num3 = MathHelper.Clamp(_selectionSize.Z * percentageOfSelectionLengthGizmoShouldRender, minGizmoSize, maxGizmoSize);
		Vector3 vector = _centerPos - playerPosition;
		bool flag = true;
		if (selectionNormal.Y != 0f)
		{
			if ((double)selectionNormal.Y < -0.1 && playerPosition.Y < _centerPos.Y - _selectionSize.Y / 2f)
			{
				flag = false;
			}
			else if ((double)selectionNormal.Y > 0.1 && playerPosition.Y > _centerPos.Y + _selectionSize.Y / 2f)
			{
				flag = false;
			}
			Matrix.CreateScale(num, num3, num2, out _matrix);
			Matrix.CreateRotationX((float)System.Math.PI / 2f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			Matrix.CreateTranslation(vector.X - num * 0.5f, 0.01f * selectionNormal.Y + vector.Y + _selectionSize.Y / 2f * selectionNormal.Y, vector.Z - num3 * 0.5f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		}
		else if (selectionNormal.X != 0f)
		{
			if ((double)selectionNormal.X < -0.1 && playerPosition.X < _centerPos.X - _selectionSize.X / 2f)
			{
				flag = false;
			}
			else if ((double)selectionNormal.X > 0.1 && playerPosition.X > _centerPos.X + _selectionSize.X / 2f)
			{
				flag = false;
			}
			Matrix.CreateScale(num3, num2, num, out _matrix);
			Matrix.CreateRotationY(-(float)System.Math.PI / 2f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			Matrix.CreateTranslation(0.01f * selectionNormal.X + vector.X + _selectionSize.X / 2f * selectionNormal.X, vector.Y - num2 * 0.5f, vector.Z - num3 * 0.5f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		}
		else if (selectionNormal.Z != 0f)
		{
			if ((double)selectionNormal.Z < -0.1 && playerPosition.Z < _centerPos.Z - _selectionSize.Z / 2f)
			{
				flag = false;
			}
			else if ((double)selectionNormal.Z > 0.1 && playerPosition.Z > _centerPos.Z + _selectionSize.Z / 2f)
			{
				flag = false;
			}
			Matrix.CreateScale(num, num2, num3, out _matrix);
			Matrix.CreateTranslation(vector.X - num * 0.5f, vector.Y - num2 * 0.5f, 0.01f * selectionNormal.Z + vector.Z + _selectionSize.Z / 2f * selectionNormal.Z, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		}
		Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
		basicProgram.MVPMatrix.SetValue(ref _matrix);
		GLFunctions gL = _graphics.GL;
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.GEQUAL : GL.LEQUAL);
		basicProgram.Opacity.SetValue(0.15f);
		_faceHighlightRenderer.Draw();
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
		basicProgram.Opacity.SetValue(0.3f);
		_faceHighlightRenderer.Draw();
	}

	public void DrawFaceHighlight(ref Matrix viewProjectionMatrix, Vector3 selectionNormal, Vector3 color, Vector3 positionOffset)
	{
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		basicProgram.Color.SetValue(color);
		Vector3 vector = _centerPos + positionOffset;
		if (selectionNormal.Y != 0f)
		{
			Matrix.CreateScale(_selectionSize.X, _selectionSize.Z, 1f, out _matrix);
			Matrix.CreateRotationX((float)System.Math.PI / 2f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			Matrix.CreateTranslation(vector.X - _selectionSize.X / 2f, vector.Y + (_selectionSize.Y / 2f + 0.005f) * selectionNormal.Y, vector.Z - _selectionSize.Z / 2f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		}
		else if (selectionNormal.X != 0f)
		{
			Matrix.CreateScale(_selectionSize.Z, _selectionSize.Y, 1f, out _matrix);
			Matrix.CreateRotationY(-(float)System.Math.PI / 2f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			Matrix.CreateTranslation(vector.X + (_selectionSize.X / 2f + 0.005f) * selectionNormal.X, vector.Y - _selectionSize.Y / 2f, vector.Z - _selectionSize.Z / 2f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		}
		else if (selectionNormal.Z != 0f)
		{
			Matrix.CreateScale(_selectionSize.X, _selectionSize.Y, 1f, out _matrix);
			Matrix.CreateTranslation(vector.X - _selectionSize.X / 2f, vector.Y - _selectionSize.Y / 2f, vector.Z + (_selectionSize.Z / 2f + 0.005f) * selectionNormal.Z, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
		}
		Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
		basicProgram.MVPMatrix.SetValue(ref _matrix);
		GLFunctions gL = _graphics.GL;
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.GEQUAL : GL.LEQUAL);
		basicProgram.Opacity.SetValue(0.15f);
		_faceHighlightRenderer.Draw();
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
		basicProgram.Opacity.SetValue(0.3f);
		_faceHighlightRenderer.Draw();
	}

	public void DrawText(ref Matrix viewProjectionMatrix, ICameraController cameraController)
	{
		GLFunctions gL = _graphics.GL;
		TextProgram textProgram = _graphics.GPUProgramStore.TextProgram;
		textProgram.AssertInUse();
		float scale = 0.4f / (float)_font.BaseSize;
		int spread = _font.Spread;
		float num = 1f / (float)spread;
		Vector3 position = cameraController.Position;
		gL.DepthFunc(GL.ALWAYS);
		Vector3[] array = new Vector3[3];
		Vector3 vector = (_pos1 - _pos2 + Vector3.One) * 0.5f;
		Vector3 vector2 = new Vector3((vector.X < 0f) ? 1f : 0f, (vector.Y < 0f) ? 1f : 0f, (vector.Z < 0f) ? 1f : 0f);
		float y = System.Math.Max(_pos1.Y, _pos2.Y) - _pos2.Y + 1f - num / 2f;
		array[0] = _pos2 + new Vector3(vector.X, y, vector2.Z);
		array[1] = _pos2 + new Vector3(vector2.X, vector.Y - num / 2f, vector2.Z);
		array[2] = _pos2 + new Vector3(vector2.X, y, vector.Z);
		string[] array2 = new string[3]
		{
			_selectionSize.X.ToString(),
			_selectionSize.Y.ToString(),
			_selectionSize.Z.ToString()
		};
		for (int i = 0; i < 3; i++)
		{
			Vector3 value = array[i] - position;
			float num2 = Vector3.Distance(array[i], position);
			float value2 = MathHelper.Clamp(2f * num2 * 0.1f, 1f, spread) * num;
			Matrix.CreateTranslation(0f - _textRenderer.GetHorizontalOffset(TextRenderer.TextAlignment.Center), 0f - _textRenderer.GetVerticalOffset(TextRenderer.TextVerticalAlignment.Middle), 0f, out _tempMatrix);
			Matrix.CreateScale(scale, out _matrix);
			Matrix.Multiply(ref _tempMatrix, ref _matrix, out _matrix);
			Vector3 rotation = cameraController.Rotation;
			Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f, out _tempMatrix);
			Matrix.Multiply(ref _matrix, ref _tempMatrix, out _matrix);
			Matrix.AddTranslation(ref _matrix, array[i].X, array[i].Y, array[i].Z);
			Matrix.Multiply(ref _matrix, ref viewProjectionMatrix, out _matrix);
			textProgram.Position.SetValue(value);
			textProgram.FillBlurThreshold.SetValue(value2);
			textProgram.MVPMatrix.SetValue(ref _matrix);
			_textRenderer.Text = array2[i];
			_textRenderer.Draw();
		}
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}
}
