using System;
using System.Runtime.CompilerServices;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics;

internal class ForceFieldFXSystem : Disposable
{
	public enum FXShape
	{
		Sphere,
		Box,
		Quad,
		Custom,
		MAX
	}

	private struct ColorDrawTask
	{
		public Matrix ModelMatrix;

		public Matrix NormalMatrix;

		public Vector4 ColorOpacity;

		public Vector4 IntersectionHighlightColorOpacity;

		public float IntersectionHighlightThickness;

		public Vector2 UVAnimationSpeed;

		public int OutlineMode;

		public FXShape Shape;
	}

	private struct DistortionDrawTask
	{
		public Matrix ModelMatrix;

		public Vector2 UVAnimationSpeed;

		public FXShape Shape;
	}

	public GLTexture NormalMap;

	private GraphicsDevice _graphics;

	private const int DrawTaskDefaultSize = 200;

	private const int DrawTaskGrowth = 50;

	private ColorDrawTask[] _colorDrawTasks = new ColorDrawTask[200];

	private DistortionDrawTask[] _distortionDrawTasks = new DistortionDrawTask[200];

	private int _colorDrawTaskCount;

	private int _distortionDrawTaskCount;

	private int _incomingColorDrawTaskCount;

	private int _incomingDistortionDrawTaskCount;

	private bool _wasSceneDataSent;

	private Matrix _viewMatrix;

	private Matrix _viewProjectionMatrix;

	private float _farClippingPlane;

	private Vector3 _timeSinCos;

	private Mesh _meshQuad;

	private Mesh _meshSphere;

	private Mesh _meshBox;

	public bool HasColorTasks => _colorDrawTaskCount > 0;

	public bool HasDistortionTasks => _distortionDrawTaskCount > 0;

	public ForceFieldFXSystem(GraphicsDevice graphics)
	{
		_graphics = graphics;
		Initialize();
	}

	private void Initialize()
	{
		ForceFieldProgram forceFieldProgram = _graphics.GPUProgramStore.ForceFieldProgram;
		MeshProcessor.CreateSphere(ref _meshSphere, 15, 16, 1f, (int)forceFieldProgram.AttribPosition.Index, (int)forceFieldProgram.AttribTexCoords.Index, (int)forceFieldProgram.AttribNormal.Index);
		MeshProcessor.CreateBox(ref _meshBox, 2f, (int)forceFieldProgram.AttribPosition.Index, (int)forceFieldProgram.AttribTexCoords.Index, (int)forceFieldProgram.AttribNormal.Index);
		MeshProcessor.CreateQuad(ref _meshQuad, 2f, (int)forceFieldProgram.AttribPosition.Index, (int)forceFieldProgram.AttribTexCoords.Index, (int)forceFieldProgram.AttribNormal.Index);
	}

	protected override void DoDispose()
	{
		_meshBox.Dispose();
		_meshSphere.Dispose();
		_meshQuad.Dispose();
	}

	public void BeginFrame()
	{
		_wasSceneDataSent = false;
		ResetCounters();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingColorTasks(int size)
	{
		_incomingColorDrawTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _colorDrawTasks, _colorDrawTaskCount + size, 200);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrepareForIncomingDistortionTasks(int size)
	{
		_incomingDistortionDrawTaskCount += size;
		ArrayUtils.GrowArrayIfNecessary(ref _distortionDrawTasks, _distortionDrawTaskCount + size, 200);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterColorTask(FXShape shape, ref Matrix modelMatrix, ref Matrix normalMatrix, Vector2 uvAnimationSpeed, int outlineMode, Vector4 color, Vector4 intersectionHighlightColorOpacity, float intersectionHighlightThickness)
	{
		int colorDrawTaskCount = _colorDrawTaskCount;
		_colorDrawTasks[colorDrawTaskCount].Shape = shape;
		_colorDrawTasks[colorDrawTaskCount].ModelMatrix = modelMatrix;
		_colorDrawTasks[colorDrawTaskCount].NormalMatrix = normalMatrix;
		_colorDrawTasks[colorDrawTaskCount].ColorOpacity = color;
		_colorDrawTasks[colorDrawTaskCount].IntersectionHighlightColorOpacity = intersectionHighlightColorOpacity;
		_colorDrawTasks[colorDrawTaskCount].IntersectionHighlightThickness = intersectionHighlightThickness;
		_colorDrawTasks[colorDrawTaskCount].UVAnimationSpeed.X = uvAnimationSpeed.X;
		_colorDrawTasks[colorDrawTaskCount].UVAnimationSpeed.Y = uvAnimationSpeed.Y;
		_colorDrawTasks[colorDrawTaskCount].OutlineMode = outlineMode;
		_colorDrawTaskCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RegisterDistortionTask(FXShape shape, ref Matrix modelMatrix, Vector2 uvAnimationSpeed)
	{
		int distortionDrawTaskCount = _distortionDrawTaskCount;
		_distortionDrawTasks[distortionDrawTaskCount].Shape = shape;
		_distortionDrawTasks[distortionDrawTaskCount].ModelMatrix = modelMatrix;
		_distortionDrawTasks[distortionDrawTaskCount].UVAnimationSpeed.X = uvAnimationSpeed.X;
		_distortionDrawTasks[distortionDrawTaskCount].UVAnimationSpeed.Y = uvAnimationSpeed.Y;
		_distortionDrawTaskCount++;
	}

	public void SetupSceneData(ref Matrix viewMatrix, ref Matrix viewProjectionMatrix)
	{
		_viewMatrix = viewMatrix;
		_viewProjectionMatrix = viewProjectionMatrix;
	}

	public void DrawColor(bool sendDataToGPU = true)
	{
		ForceFieldProgram forceFieldProgram = _graphics.GPUProgramStore.ForceFieldProgram;
		GLFunctions gL = _graphics.GL;
		if (sendDataToGPU && !_wasSceneDataSent)
		{
			SendSceneDataToGPU();
		}
		for (int i = 0; i < _colorDrawTaskCount; i++)
		{
			ref ColorDrawTask reference = ref _colorDrawTasks[i];
			forceFieldProgram.ModelMatrix.SetValue(ref reference.ModelMatrix);
			forceFieldProgram.NormalMatrix.SetValue(ref reference.NormalMatrix);
			forceFieldProgram.UVAnimationSpeed.SetValue(reference.UVAnimationSpeed);
			forceFieldProgram.OutlineMode.SetValue(reference.OutlineMode);
			forceFieldProgram.ColorOpacity.SetValue(reference.ColorOpacity);
			forceFieldProgram.IntersectionHighlightColorOpacity.SetValue(reference.IntersectionHighlightColorOpacity);
			forceFieldProgram.IntersectionHighlightThickness.SetValue(reference.IntersectionHighlightThickness);
			switch (reference.Shape)
			{
			case FXShape.Sphere:
				gL.BindVertexArray(_meshSphere.VertexArray);
				gL.DrawElements(GL.TRIANGLES, _meshSphere.Count, GL.UNSIGNED_SHORT, IntPtr.Zero);
				break;
			case FXShape.Box:
				gL.BindVertexArray(_meshBox.VertexArray);
				gL.DrawArrays(GL.TRIANGLES, 0, _meshBox.Count);
				break;
			case FXShape.Quad:
				gL.BindVertexArray(_meshQuad.VertexArray);
				gL.DrawArrays(GL.TRIANGLES, 0, _meshQuad.Count);
				break;
			}
		}
	}

	public void DrawDistortion()
	{
		ForceFieldProgram forceFieldProgram = _graphics.GPUProgramStore.ForceFieldProgram;
		GLFunctions gL = _graphics.GL;
		gL.AssertActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, NormalMap);
		gL.UseProgram(forceFieldProgram);
		if (!_wasSceneDataSent)
		{
			SendSceneDataToGPU();
		}
		forceFieldProgram.DrawAndBlendMode.SetValue(forceFieldProgram.DrawModeDistortion, forceFieldProgram.BlendModePremultLinear);
		for (int i = 0; i < _distortionDrawTaskCount; i++)
		{
			ref DistortionDrawTask reference = ref _distortionDrawTasks[i];
			forceFieldProgram.ModelMatrix.SetValue(ref reference.ModelMatrix);
			forceFieldProgram.UVAnimationSpeed.SetValue(reference.UVAnimationSpeed);
			switch (reference.Shape)
			{
			case FXShape.Sphere:
				gL.BindVertexArray(_meshSphere.VertexArray);
				gL.DrawElements(GL.TRIANGLES, _meshSphere.Count, GL.UNSIGNED_SHORT, IntPtr.Zero);
				break;
			case FXShape.Box:
				gL.BindVertexArray(_meshBox.VertexArray);
				gL.DrawArrays(GL.TRIANGLES, 0, _meshBox.Count);
				break;
			case FXShape.Quad:
				gL.BindVertexArray(_meshQuad.VertexArray);
				gL.DrawArrays(GL.TRIANGLES, 0, _meshQuad.Count);
				break;
			}
		}
	}

	private void ResetCounters()
	{
		_colorDrawTaskCount = 0;
		_distortionDrawTaskCount = 0;
		_incomingColorDrawTaskCount = 0;
		_incomingDistortionDrawTaskCount = 0;
	}

	private void SendSceneDataToGPU()
	{
		ForceFieldProgram forceFieldProgram = _graphics.GPUProgramStore.ForceFieldProgram;
		forceFieldProgram.AssertInUse();
		forceFieldProgram.ViewMatrix.SetValue(ref _viewMatrix);
		forceFieldProgram.ViewProjectionMatrix.SetValue(ref _viewProjectionMatrix);
		_wasSceneDataSent = true;
	}
}
