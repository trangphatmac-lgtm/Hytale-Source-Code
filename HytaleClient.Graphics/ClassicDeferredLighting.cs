using System;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class ClassicDeferredLighting
{
	private struct LightDrawTask
	{
		public Matrix ModelMatrix;

		public BoundingSphere Sphere;

		public Vector3 Color;
	}

	private struct LightGroupDrawTask
	{
		public Matrix ModelMatrix;

		public BoundingSphere Sphere;

		public ushort LightIndexStart;

		public ushort LightCount;
	}

	public int LightDataTransferMethod = 0;

	public bool UseLightGroups = true;

	public bool UseStencilForOuterLights = true;

	private LightDrawTask[] _outerLightDrawTasks = new LightDrawTask[1024];

	private LightDrawTask[] _innerLightDrawTasks = new LightDrawTask[1024];

	private ushort _outerLightDrawTasksCount;

	private ushort _innerLightDrawTasksCount;

	private Vector4[] _globalLightPositionSizes = new Vector4[1024];

	private Vector3[] _globalLightColors = new Vector3[1024];

	private int _globalLightDataCount;

	private LightGroupDrawTask[] _outerLightGroupDrawTasks = new LightGroupDrawTask[1024];

	private LightGroupDrawTask[] _innerLightGroupDrawTasks = new LightGroupDrawTask[1024];

	private ushort _outerLightGroupDrawTasksCount;

	private ushort _innerLightGroupDrawTasksCount;

	private Mesh _sphereLightMesh;

	private Mesh _boxLightMesh;

	private Matrix _boxLightModelMatrix;

	private BoundingBox _globalLightBoundingBox;

	private readonly GraphicsDevice _graphics;

	private readonly GPUProgramStore _gpuProgramStore;

	private readonly RenderTargetStore _renderTargetStore;

	private readonly GLFunctions _gl;

	public int LightCount => _globalLightDataCount;

	public ClassicDeferredLighting(GraphicsDevice graphics, RenderTargetStore renderTargetStore)
	{
		_graphics = graphics;
		_gpuProgramStore = graphics.GPUProgramStore;
		_renderTargetStore = renderTargetStore;
		_gl = _graphics.GL;
	}

	public void Init()
	{
		MeshProcessor.CreateSphere(ref _sphereLightMesh, 5, 8, 1f, 0);
		MeshProcessor.CreateSimpleBox(ref _boxLightMesh);
	}

	public void Dispose()
	{
		_sphereLightMesh.Dispose();
		_boxLightMesh.Dispose();
	}

	public void PrepareLightsForDraw(LightData[] lightData, int lightCount, Vector3 cameraPosition, ref Matrix viewRotationMatrix, ref Matrix invViewRotationMatrix, bool completeFullSetup)
	{
		_outerLightDrawTasksCount = 0;
		_innerLightDrawTasksCount = 0;
		BoundingSphere boundingSphere = default(BoundingSphere);
		for (int i = 0; i < lightCount; i++)
		{
			float radius = lightData[i].Sphere.Radius;
			Vector3 center = lightData[i].Sphere.Center;
			Vector3 color = lightData[i].Color;
			boundingSphere.Center = center;
			boundingSphere.Radius = radius;
			center -= cameraPosition;
			Matrix.CreateScale(radius, out var result);
			Matrix.AddTranslation(ref result, center.X, center.Y, center.Z);
			center = Vector3.Transform(center, viewRotationMatrix);
			if (boundingSphere.Contains(cameraPosition) != 0)
			{
				if (_innerLightDrawTasksCount < _innerLightDrawTasks.Length)
				{
					_innerLightDrawTasks[_innerLightDrawTasksCount].ModelMatrix = result;
					_innerLightDrawTasks[_innerLightDrawTasksCount].Sphere = new BoundingSphere(center, radius);
					_innerLightDrawTasks[_innerLightDrawTasksCount].Color = color;
					_innerLightDrawTasksCount++;
				}
			}
			else if (_outerLightDrawTasksCount < _outerLightDrawTasks.Length)
			{
				_outerLightDrawTasks[_outerLightDrawTasksCount].ModelMatrix = result;
				_outerLightDrawTasks[_outerLightDrawTasksCount].Sphere = new BoundingSphere(center, radius);
				_outerLightDrawTasks[_outerLightDrawTasksCount].Color = color;
				_outerLightDrawTasksCount++;
			}
		}
		_globalLightDataCount = _innerLightDrawTasksCount + _outerLightDrawTasksCount;
		if (!completeFullSetup)
		{
			return;
		}
		if (UseStencilForOuterLights && _outerLightDrawTasksCount > 0)
		{
			BoundingSphere sphere = default(BoundingSphere);
			sphere.Center = Vector3.Transform(_outerLightDrawTasks[0].Sphere.Center, invViewRotationMatrix);
			sphere.Radius = _outerLightDrawTasks[0].Sphere.Radius;
			_globalLightBoundingBox = BoundingBox.CreateFromSphere(sphere);
			for (int j = 1; j < _outerLightDrawTasksCount; j++)
			{
				sphere.Center = Vector3.Transform(_outerLightDrawTasks[j].Sphere.Center, invViewRotationMatrix);
				sphere.Radius = _outerLightDrawTasks[j].Sphere.Radius;
				BoundingBox additional = BoundingBox.CreateFromSphere(sphere);
				_globalLightBoundingBox = BoundingBox.CreateMerged(_globalLightBoundingBox, additional);
			}
			Vector3 scales = _globalLightBoundingBox.Max - _globalLightBoundingBox.Min;
			Vector3 center2 = _globalLightBoundingBox.GetCenter();
			Matrix.CreateScale(ref scales, out _boxLightModelMatrix);
			Matrix.AddTranslation(ref _boxLightModelMatrix, center2.X, center2.Y, center2.Z);
		}
		if (!UseLightGroups)
		{
			return;
		}
		PrepareLightGroupDrawTasks(12f, ref invViewRotationMatrix, ref _innerLightDrawTasks, _innerLightDrawTasksCount, ref _globalLightPositionSizes, ref _globalLightColors, 0, ref _innerLightGroupDrawTasks, out _innerLightGroupDrawTasksCount);
		PrepareLightGroupDrawTasks(7.5f, ref invViewRotationMatrix, ref _outerLightDrawTasks, _outerLightDrawTasksCount, ref _globalLightPositionSizes, ref _globalLightColors, _innerLightDrawTasksCount, ref _outerLightGroupDrawTasks, out _outerLightGroupDrawTasksCount);
		for (int k = 0; k < _outerLightGroupDrawTasksCount; k++)
		{
			if (_outerLightGroupDrawTasks[k].Sphere.Contains(new Vector3(0f)) != 0)
			{
				_innerLightGroupDrawTasks[_innerLightGroupDrawTasksCount] = _outerLightGroupDrawTasks[k];
				_innerLightGroupDrawTasksCount++;
				_outerLightGroupDrawTasks[k] = _outerLightGroupDrawTasks[_outerLightGroupDrawTasksCount - 1];
				_outerLightGroupDrawTasksCount--;
				k--;
			}
		}
	}

	private void PrepareLightGroupDrawTasks(float maxGroupSphereRadius, ref Matrix invViewRotationMatrix, ref LightDrawTask[] inputLightDrawTasks, ushort inputLightDrawTaskCount, ref Vector4[] outputLightPositionSizes, ref Vector3[] outputLightColors, int outputLightStart, ref LightGroupDrawTask[] outputLightGroupDrawTasks, out ushort outputLightGroupDrawTasksCount)
	{
		float num = maxGroupSphereRadius * maxGroupSphereRadius;
		outputLightGroupDrawTasksCount = 0;
		if (inputLightDrawTaskCount <= 0)
		{
			return;
		}
		int num2 = outputLightStart;
		ushort num3 = 0;
		ushort num4 = 0;
		ushort[] array = new ushort[inputLightDrawTaskCount];
		for (int i = 0; i < inputLightDrawTaskCount; i++)
		{
			array[i] = (ushort)i;
		}
		int num5 = array.Length;
		while (num5 > 0)
		{
			int num6 = 0;
			ushort num7 = array[num6];
			num3 = outputLightGroupDrawTasksCount;
			BoundingSphere original = inputLightDrawTasks[num7].Sphere;
			num4 = 1;
			outputLightGroupDrawTasks[num3].LightIndexStart = (ushort)num2;
			outputLightGroupDrawTasksCount++;
			outputLightColors[num2] = inputLightDrawTasks[num7].Color;
			outputLightPositionSizes[num2] = new Vector4(inputLightDrawTasks[num7].Sphere.Center.X, inputLightDrawTasks[num7].Sphere.Center.Y, inputLightDrawTasks[num7].Sphere.Center.Z, inputLightDrawTasks[num7].Sphere.Radius);
			num2++;
			array[num6] = array[num5 - 1];
			num5--;
			for (int j = 0; j < num5; j++)
			{
				num7 = array[j];
				Vector3.DistanceSquared(ref original.Center, ref inputLightDrawTasks[num7].Sphere.Center, out var result);
				if (result < num)
				{
					BoundingSphere.CreateMerged(ref original, ref inputLightDrawTasks[num7].Sphere, out var result2);
					if (result2.Radius < maxGroupSphereRadius)
					{
						original = result2;
						num4++;
						outputLightColors[num2] = inputLightDrawTasks[num7].Color;
						outputLightPositionSizes[num2] = new Vector4(inputLightDrawTasks[num7].Sphere.Center.X, inputLightDrawTasks[num7].Sphere.Center.Y, inputLightDrawTasks[num7].Sphere.Center.Z, inputLightDrawTasks[num7].Sphere.Radius);
						num2++;
						array[j] = array[num5 - 1];
						num5--;
						j--;
					}
				}
			}
			Vector3 vector = Vector3.Transform(original.Center, invViewRotationMatrix);
			Matrix.CreateScale(original.Radius, out var result3);
			Matrix.AddTranslation(ref result3, vector.X, vector.Y, vector.Z);
			outputLightGroupDrawTasks[num3].ModelMatrix = result3;
			outputLightGroupDrawTasks[num3].Sphere = original;
			outputLightGroupDrawTasks[num3].LightCount = num4;
		}
	}

	public void TagStencil(uint stencilMask, ref Matrix viewRotationProjectionMatrix)
	{
		_gl.StencilMask(stencilMask);
		_gl.Enable(GL.DEPTH_TEST);
		_gl.Disable(GL.CULL_FACE);
		_gl.ColorMask(red: false, green: false, blue: false, alpha: false);
		_gl.StencilFunc(GL.ALWAYS, 0, stencilMask);
		_gl.StencilOp(GL.KEEP, GL.INVERT, GL.KEEP);
		ZOnlyProgram zOnlyProgram = _gpuProgramStore.ZOnlyProgram;
		_gl.UseProgram(zOnlyProgram);
		zOnlyProgram.ViewProjectionMatrix.SetValue(ref viewRotationProjectionMatrix);
		zOnlyProgram.ModelMatrix.SetValue(ref _boxLightModelMatrix);
		_gl.BindVertexArray(_boxLightMesh.VertexArray);
		_gl.DrawElements(GL.TRIANGLES, _boxLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
		_gl.ColorMask(red: true, green: true, blue: true, alpha: true);
		_gl.StencilMask(0u);
		_gl.Enable(GL.CULL_FACE);
		_gl.StencilOp(GL.KEEP, GL.KEEP, GL.REPLACE);
	}

	public void DrawDeferredLights(bool fullResolution, bool useStencilForOuterLights, ref Matrix viewRotationMatrix, ref Matrix projectionMatrix, float farClip)
	{
		_gl.AssertDepthMask(write: false);
		_gl.AssertBlendFunc(GL.SRC_ALPHA, GL.ONE);
		_gl.AssertEnabled(GL.BLEND);
		_gl.AssertActiveTexture(GL.TEXTURE0);
		_gl.Enable(GL.DEPTH_TEST);
		_gl.Enable(GL.CULL_FACE);
		LightProgram lightProgram = (fullResolution ? _gpuProgramStore.LightProgram : _gpuProgramStore.LightLowResProgram);
		_gl.UseProgram(lightProgram);
		lightProgram.Debug.SetValue(0);
		lightProgram.ProjectionMatrix.SetValue(ref projectionMatrix);
		lightProgram.ViewMatrix.SetValue(ref viewRotationMatrix);
		if (fullResolution)
		{
			lightProgram.InvScreenSize.SetValue(_renderTargetStore.LightBufferFullRes.InvWidth, _renderTargetStore.LightBufferFullRes.InvHeight);
		}
		else
		{
			lightProgram.InvScreenSize.SetValue(_renderTargetStore.LightBufferHalfRes.InvWidth, _renderTargetStore.LightBufferHalfRes.InvHeight);
		}
		if (UseLightGroups && LightDataTransferMethod == 1)
		{
			lightProgram.GlobalLightColors.SetValue(_globalLightColors, _globalLightDataCount);
			lightProgram.GlobalLightPositionSizes.SetValue(_globalLightPositionSizes, _globalLightDataCount);
		}
		lightProgram.UseLightGroup.SetValue(UseLightGroups ? 1 : 0);
		lightProgram.TransferMethod.SetValue(LightDataTransferMethod);
		if (_graphics.UseLinearZForLight)
		{
			_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.LinearZ.GetTexture(RenderTarget.Target.Color0));
			lightProgram.FarClip.SetValue(farClip);
		}
		else
		{
			_gl.BindTexture(GL.TEXTURE_2D, _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Depth));
		}
		_gl.BindVertexArray(_sphereLightMesh.VertexArray);
		_gl.CullFace(GL.FRONT);
		_gl.DepthFunc(GL.GREATER);
		if (UseLightGroups)
		{
			if (LightDataTransferMethod == 0)
			{
				for (int i = 0; i < _innerLightGroupDrawTasksCount; i++)
				{
					lightProgram.ModelMatrix.SetValue(ref _innerLightGroupDrawTasks[i].ModelMatrix);
					lightProgram.GlobalLightColors.SetValue(_globalLightColors, _innerLightGroupDrawTasks[i].LightIndexStart, _innerLightGroupDrawTasks[i].LightCount);
					lightProgram.GlobalLightPositionSizes.SetValue(_globalLightPositionSizes, _innerLightGroupDrawTasks[i].LightIndexStart, _innerLightGroupDrawTasks[i].LightCount);
					lightProgram.LightGroup.SetValue(0, _innerLightGroupDrawTasks[i].LightCount);
					_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
			else
			{
				for (int j = 0; j < _innerLightGroupDrawTasksCount; j++)
				{
					lightProgram.ModelMatrix.SetValue(ref _innerLightGroupDrawTasks[j].ModelMatrix);
					lightProgram.LightGroup.SetValue(_innerLightGroupDrawTasks[j].LightIndexStart, _innerLightGroupDrawTasks[j].LightCount);
					_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
		}
		else
		{
			for (int k = 0; k < _innerLightDrawTasksCount; k++)
			{
				lightProgram.ModelMatrix.SetValue(ref _innerLightDrawTasks[k].ModelMatrix);
				lightProgram.Color.SetValue(_innerLightDrawTasks[k].Color);
				lightProgram.PositionSize.SetValue(_innerLightDrawTasks[k].Sphere.Center.X, _innerLightDrawTasks[k].Sphere.Center.Y, _innerLightDrawTasks[k].Sphere.Center.Z, _innerLightDrawTasks[k].Sphere.Radius);
				_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
		_gl.CullFace(GL.BACK);
		_gl.DepthFunc(GL.LEQUAL);
		if (useStencilForOuterLights)
		{
			_gl.StencilFunc(GL.NOTEQUAL, 0, 32u);
		}
		if (UseLightGroups)
		{
			if (LightDataTransferMethod == 0)
			{
				for (int l = 0; l < _outerLightGroupDrawTasksCount; l++)
				{
					lightProgram.ModelMatrix.SetValue(ref _outerLightGroupDrawTasks[l].ModelMatrix);
					lightProgram.GlobalLightColors.SetValue(_globalLightColors, _outerLightGroupDrawTasks[l].LightIndexStart, _outerLightGroupDrawTasks[l].LightCount);
					lightProgram.GlobalLightPositionSizes.SetValue(_globalLightPositionSizes, _outerLightGroupDrawTasks[l].LightIndexStart, _outerLightGroupDrawTasks[l].LightCount);
					lightProgram.LightGroup.SetValue(0, _outerLightGroupDrawTasks[l].LightCount);
					_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
			else
			{
				for (int m = 0; m < _outerLightGroupDrawTasksCount; m++)
				{
					lightProgram.ModelMatrix.SetValue(ref _outerLightGroupDrawTasks[m].ModelMatrix);
					lightProgram.LightGroup.SetValue(_outerLightGroupDrawTasks[m].LightIndexStart, _outerLightGroupDrawTasks[m].LightCount);
					_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				}
			}
		}
		else
		{
			for (int n = 0; n < _outerLightDrawTasksCount; n++)
			{
				lightProgram.ModelMatrix.SetValue(ref _outerLightDrawTasks[n].ModelMatrix);
				lightProgram.Color.SetValue(_outerLightDrawTasks[n].Color);
				lightProgram.PositionSize.SetValue(_outerLightDrawTasks[n].Sphere.Center.X, _outerLightDrawTasks[n].Sphere.Center.Y, _outerLightDrawTasks[n].Sphere.Center.Z, _outerLightDrawTasks[n].Sphere.Radius);
				_gl.DrawElements(GL.TRIANGLES, _sphereLightMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
		_gl.Disable(GL.CULL_FACE);
		_gl.AssertCullFace(GL.BACK);
		_gl.AssertDepthFunc(GL.LEQUAL);
		_gl.AssertDepthMask(write: false);
		_gl.AssertBlendFunc(GL.SRC_ALPHA, GL.ONE);
		_gl.AssertEnabled(GL.BLEND);
	}
}
