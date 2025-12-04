#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HytaleClient.Core;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class OcclusionCulling : Disposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
	public struct OccludeeData
	{
		public static readonly int Size = Marshal.SizeOf(typeof(OccludeeData));

		public Vector3 BoxMin;

		public Vector3 BoxMax;

		public uint Padding1;

		public uint Padding2;
	}

	public bool IsEnabled = true;

	private readonly GraphicsDevice _graphics;

	private readonly Profiling _profiling;

	private RenderTarget _occlusionRenderTarget;

	private RenderTarget _occlusionRenderTargetB;

	private GLVertexArray _reprojectedPointsVertexArray;

	private Vector4[] _previousFrameInvalidScreenAreas;

	private GLVertexArray _occludeesVAO;

	private GLBuffer _occludeesPositionsVBO;

	private GLBuffer _visibleOccludeesTFBO;

	private int MaxOccludees = 2000;

	private const int OccludeesGrowth = 500;

	private OccludeeData[] _occludeesData;

	private int _occludeesCount;

	private int[] _visibleOccludees;

	private const float MinSkipDuration = 2000f;

	private float _skipRemainingTime;

	private Mesh _debugOccludeeMesh;

	private int _renderingProfileOcclusionBuildMap;

	private int _renderingProfileOcclusionRenderOccluders;

	private int _renderingProfileOcclusionReproject;

	private int _renderingProfileOcclusionCreateHiZ;

	private int _renderingProfileOcclusionPrepareOccludees;

	private int _renderingProfileOcclusionTestOccludees;

	private int _renderingProfileOcclusionFetchResults;

	public bool IsActive { get; private set; }

	public int MaxInvalidScreenAreasForReprojection => _graphics.GPUProgramStore.HiZReprojectProgram.MaxInvalidScreenAreas;

	public OcclusionCulling(GraphicsDevice graphics, Profiling profiling)
	{
		_graphics = graphics;
		_profiling = profiling;
		_visibleOccludees = new int[MaxOccludees];
		_previousFrameInvalidScreenAreas = new Vector4[32];
		CreateGPUData();
	}

	protected override void DoDispose()
	{
		DestroyGPUData();
		_visibleOccludees = null;
	}

	private void CreateGPUData()
	{
		GLFunctions gL = _graphics.GL;
		int num = MaxOccludees * 4;
		_visibleOccludeesTFBO = gL.GenBuffer();
		gL.BindBuffer(GL.ARRAY_BUFFER, _visibleOccludeesTFBO);
		gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)num, IntPtr.Zero, GL.STATIC_READ);
		int num2 = MaxOccludees * OccludeeData.Size;
		_occludeesPositionsVBO = gL.GenBuffer();
		gL.BindBuffer(GL.ARRAY_BUFFER, _occludeesPositionsVBO);
		gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)num2, IntPtr.Zero, GL.DYNAMIC_DRAW);
		_occludeesVAO = gL.GenVertexArray();
		gL.BindVertexArray(_occludeesVAO);
		HiZCullProgram hiZCullProgram = _graphics.GPUProgramStore.HiZCullProgram;
		IntPtr zero = IntPtr.Zero;
		gL.EnableVertexAttribArray(hiZCullProgram.AttribBoxMin.Index);
		gL.VertexAttribPointer(hiZCullProgram.AttribBoxMin.Index, 3, GL.FLOAT, normalized: false, OccludeeData.Size, zero);
		zero += 12;
		gL.EnableVertexAttribArray(hiZCullProgram.AttribBoxMax.Index);
		gL.VertexAttribPointer(hiZCullProgram.AttribBoxMax.Index, 3, GL.FLOAT, normalized: false, OccludeeData.Size, zero);
		zero += 12;
		_reprojectedPointsVertexArray = gL.GenVertexArray();
		_occlusionRenderTarget = new RenderTarget(_graphics.OcclusionMapWidth, _graphics.OcclusionMapHeight, "_occlusionRenderTarget");
		_occlusionRenderTarget.AddTexture(RenderTarget.Target.Depth, GL.DEPTH_COMPONENT32, GL.DEPTH_COMPONENT, GL.UNSIGNED_INT, GL.NEAREST, GL.NEAREST, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: true);
		_occlusionRenderTarget.FinalizeSetup();
		_occlusionRenderTargetB = new RenderTarget(_graphics.OcclusionMapWidth, _graphics.OcclusionMapHeight, "_occlusionRenderTargetB");
		_occlusionRenderTargetB.AddTexture(RenderTarget.Target.Depth, GL.DEPTH_COMPONENT32, GL.DEPTH_COMPONENT, GL.UNSIGNED_INT, GL.NEAREST, GL.NEAREST, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: true);
		_occlusionRenderTargetB.FinalizeSetup();
		MeshProcessor.CreateSimpleBox(ref _debugOccludeeMesh);
	}

	private void DestroyGPUData()
	{
		GLFunctions gL = _graphics.GL;
		_debugOccludeeMesh.Dispose();
		_occlusionRenderTargetB.Dispose();
		_occlusionRenderTarget.Dispose();
		gL.DeleteVertexArray(_reprojectedPointsVertexArray);
		gL.DeleteVertexArray(_occludeesVAO);
		gL.DeleteBuffer(_occludeesPositionsVBO);
		gL.DeleteBuffer(_visibleOccludeesTFBO);
	}

	public void SetupRenderingProfiles(int renderingProfileOcclusionBuildMap, int renderingProfileOcclusionRenderOccluders, int renderingProfileOcclusionReproject, int renderingProfileOcclusionCreateHiZ, int renderingProfileOcclusionPrepareOccludees, int renderingProfileOcclusionTestOccludees, int renderingProfileOcclusionFetchResults)
	{
		_renderingProfileOcclusionBuildMap = renderingProfileOcclusionBuildMap;
		_renderingProfileOcclusionRenderOccluders = renderingProfileOcclusionRenderOccluders;
		_renderingProfileOcclusionReproject = renderingProfileOcclusionReproject;
		_renderingProfileOcclusionCreateHiZ = renderingProfileOcclusionCreateHiZ;
		_renderingProfileOcclusionPrepareOccludees = renderingProfileOcclusionPrepareOccludees;
		_renderingProfileOcclusionTestOccludees = renderingProfileOcclusionTestOccludees;
		_renderingProfileOcclusionFetchResults = renderingProfileOcclusionFetchResults;
	}

	private void GrowOccludeesBuffersIfNecessary(int occludeesCount, int growth)
	{
		if (occludeesCount >= MaxOccludees)
		{
			GLFunctions gL = _graphics.GL;
			MaxOccludees += growth;
			int num = MaxOccludees * 4;
			gL.BindBuffer(GL.ARRAY_BUFFER, _visibleOccludeesTFBO);
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)num, IntPtr.Zero, GL.STATIC_READ);
			int num2 = MaxOccludees * OccludeeData.Size;
			gL.BindBuffer(GL.ARRAY_BUFFER, _occludeesPositionsVBO);
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)num2, IntPtr.Zero, GL.DYNAMIC_DRAW);
			Array.Resize(ref _visibleOccludees, MaxOccludees);
		}
	}

	public void Update(ref Matrix viewRotationProjectionMatrix, float frameTime, bool isSpatialContinuityLost, Action drawOccluders, ref Matrix reprojectionMatrix, ref Matrix previousProjectionMatrix, RenderTarget previousZBuffer, RenderTarget.Target previousZBufferTarget, Vector4[] previousFrameInvalidScreenAreas, int previousFrameInvalidScreenAreaCount, bool fillReprojectionHoles, ref OccludeeData[] candidateOccludees, int candidateOccludeesCount, ref int[] visibleOccludees)
	{
		Profiling profiling = _profiling;
		if (isSpatialContinuityLost)
		{
			_skipRemainingTime = 2000f;
		}
		else if (_skipRemainingTime > 0f)
		{
			_skipRemainingTime -= frameTime;
		}
		IsActive = IsEnabled && _skipRemainingTime <= 0f;
		if (IsActive)
		{
			BuildOcclusionMap(drawOccluders, ref reprojectionMatrix, ref previousProjectionMatrix, previousZBuffer, previousZBufferTarget, previousFrameInvalidScreenAreas, previousFrameInvalidScreenAreaCount, fillReprojectionHoles);
			PrepareOccludees(ref candidateOccludees, candidateOccludeesCount);
			TestOccludees(ref viewRotationProjectionMatrix);
			return;
		}
		for (int i = 0; i < _visibleOccludees.Length; i++)
		{
			_visibleOccludees[i] = 1;
		}
		visibleOccludees = _visibleOccludees;
		profiling.SkipMeasure(_renderingProfileOcclusionBuildMap);
		profiling.SkipMeasure(_renderingProfileOcclusionRenderOccluders);
		profiling.SkipMeasure(_renderingProfileOcclusionReproject);
		profiling.SkipMeasure(_renderingProfileOcclusionCreateHiZ);
		profiling.SkipMeasure(_renderingProfileOcclusionPrepareOccludees);
		profiling.SkipMeasure(_renderingProfileOcclusionTestOccludees);
	}

	private void BuildOcclusionMap(Action drawOccluders, ref Matrix reprojectionMatrix, ref Matrix previousProjectionMatrix, RenderTarget previousZBuffer, RenderTarget.Target previousZBufferTarget, Vector4[] previousFrameInvalidScreenAreas, int previousFrameInvalidScreenAreaCount, bool fillReprojectionHoles)
	{
		Profiling profiling = _profiling;
		GraphicsDevice graphics = _graphics;
		GLFunctions gL = graphics.GL;
		profiling.StartMeasure(_renderingProfileOcclusionBuildMap);
		gL.ColorMask(red: false, green: false, blue: false, alpha: false);
		gL.Enable(GL.DEPTH_TEST);
		gL.DepthMask(write: true);
		gL.Enable(GL.CULL_FACE);
		bool flag = previousZBuffer != null;
		bool flag2 = flag && fillReprojectionHoles;
		if (flag2)
		{
			_occlusionRenderTargetB.Bind(clear: true, setupViewport: true);
		}
		else
		{
			_occlusionRenderTarget.Bind(clear: true, setupViewport: true);
		}
		profiling.StartMeasure(_renderingProfileOcclusionRenderOccluders);
		drawOccluders();
		profiling.StopMeasure(_renderingProfileOcclusionRenderOccluders);
		if (flag)
		{
			profiling.StartMeasure(_renderingProfileOcclusionReproject);
			ReprojectPreviousZBuffer(ref reprojectionMatrix, ref previousProjectionMatrix, previousZBuffer, previousZBufferTarget, previousFrameInvalidScreenAreas, previousFrameInvalidScreenAreaCount);
			if (flag2)
			{
				_occlusionRenderTargetB.Unbind();
				_occlusionRenderTarget.Bind(clear: true, setupViewport: false);
				FillHoles();
			}
			profiling.StopMeasure(_renderingProfileOcclusionReproject);
		}
		else
		{
			profiling.SkipMeasure(_renderingProfileOcclusionReproject);
		}
		profiling.StartMeasure(_renderingProfileOcclusionCreateHiZ);
		CreateHiZOcclusionCullingMap();
		profiling.StopMeasure(_renderingProfileOcclusionCreateHiZ);
		_occlusionRenderTarget.Unbind();
		RenderTarget.BindHardwareFramebuffer();
		gL.ColorMask(red: true, green: true, blue: true, alpha: true);
		profiling.StopMeasure(_renderingProfileOcclusionBuildMap);
	}

	private void ReprojectPreviousZBuffer(ref Matrix reprojectionMatrix, ref Matrix previousProjectionMatrix, RenderTarget previousZBuffer, RenderTarget.Target previousZBufferTarget, Vector4[] previousFrameInvalidScreenAreas, int previousFrameInvalidScreenAreaCount)
	{
		GraphicsDevice graphics = _graphics;
		GLFunctions gL = graphics.GL;
		gL.AssertActiveTexture(GL.TEXTURE0);
		Vector2 vector = new Vector2(512f, 256f);
		gL.PointSize(1f);
		HiZReprojectProgram hiZReprojectProgram = _graphics.GPUProgramStore.HiZReprojectProgram;
		gL.BindTexture(GL.TEXTURE_2D, previousZBuffer.GetTexture(previousZBufferTarget));
		gL.UseProgram(hiZReprojectProgram);
		int num = System.Math.Min(hiZReprojectProgram.MaxInvalidScreenAreas, previousFrameInvalidScreenAreaCount);
		if (previousFrameInvalidScreenAreas != null || previousFrameInvalidScreenAreaCount > num)
		{
			Array.Copy(previousFrameInvalidScreenAreas, _previousFrameInvalidScreenAreas, num);
		}
		Array.Clear(_previousFrameInvalidScreenAreas, num, hiZReprojectProgram.MaxInvalidScreenAreas - num);
		hiZReprojectProgram.InvalidScreenAreas.SetValue(_previousFrameInvalidScreenAreas, hiZReprojectProgram.MaxInvalidScreenAreas);
		hiZReprojectProgram.Resolutions.SetValue(vector.X, vector.Y, previousZBuffer.Width, previousZBuffer.Height);
		hiZReprojectProgram.ReprojectMatrix.SetValue(ref reprojectionMatrix);
		hiZReprojectProgram.ProjectionMatrix.SetValue(ref previousProjectionMatrix);
		gL.BindVertexArray(_reprojectedPointsVertexArray);
		gL.DrawArrays(GL.NO_ERROR, 0, (int)(vector.X * vector.Y));
	}

	private void FillHoles()
	{
		GraphicsDevice graphics = _graphics;
		GLFunctions gL = graphics.GL;
		gL.AssertActiveTexture(GL.TEXTURE0);
		HiZFillHoleProgram hiZFillHoleProgram = _graphics.GPUProgramStore.HiZFillHoleProgram;
		gL.BindTexture(GL.TEXTURE_2D, _occlusionRenderTargetB.GetTexture(RenderTarget.Target.Depth));
		gL.UseProgram(hiZFillHoleProgram);
		graphics.ScreenTriangleRenderer.Draw();
	}

	private void CreateHiZOcclusionCullingMap()
	{
		GraphicsDevice graphics = _graphics;
		GLFunctions gL = graphics.GL;
		HiZBuildProgram hiZBuildProgram = graphics.GPUProgramStore.HiZBuildProgram;
		gL.UseProgram(hiZBuildProgram);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, _occlusionRenderTarget.GetTexture(RenderTarget.Target.Depth));
		gL.DepthFunc(GL.ALWAYS);
		int textureMipLevelCount = _occlusionRenderTarget.GetTextureMipLevelCount(RenderTarget.Target.Depth);
		int num = _occlusionRenderTarget.Width;
		int num2 = _occlusionRenderTarget.Height;
		graphics.ScreenTriangleRenderer.BindVertexArray();
		for (int i = 1; i < textureMipLevelCount; i++)
		{
			num /= 2;
			num2 /= 2;
			num = ((num <= 0) ? 1 : num);
			num2 = ((num2 <= 0) ? 1 : num2);
			gL.Viewport(0, 0, num, num2);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_BASE_LEVEL, i - 1);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAX_LEVEL, i - 1);
			gL.FramebufferTexture2D(GL.FRAMEBUFFER, GL.DEPTH_ATTACHMENT, GL.TEXTURE_2D, _occlusionRenderTarget.GetTexture(RenderTarget.Target.Depth), i);
			graphics.ScreenTriangleRenderer.DrawRaw();
		}
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_BASE_LEVEL, 0);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAX_LEVEL, textureMipLevelCount - 1);
		gL.FramebufferTexture2D(GL.FRAMEBUFFER, GL.DEPTH_ATTACHMENT, GL.TEXTURE_2D, _occlusionRenderTarget.GetTexture(RenderTarget.Target.Depth), 0);
		gL.DepthFunc(GL.LEQUAL);
	}

	private unsafe void PrepareOccludees(ref OccludeeData[] candidateOccludees, int candidateOccludeesCount)
	{
		Profiling profiling = _profiling;
		GraphicsDevice graphics = _graphics;
		GLFunctions gL = graphics.GL;
		profiling.StartMeasure(_renderingProfileOcclusionPrepareOccludees);
		_occludeesData = candidateOccludees;
		_occludeesCount = candidateOccludeesCount;
		int growth = System.Math.Max(500, 2 * (_occludeesCount - MaxOccludees));
		GrowOccludeesBuffersIfNecessary(_occludeesCount, growth);
		gL.BindBuffer(GL.ARRAY_BUFFER, _occludeesPositionsVBO);
		fixed (OccludeeData* ptr = candidateOccludees)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_occludeesCount * OccludeeData.Size), (IntPtr)ptr, GL.DYNAMIC_DRAW);
		}
		profiling.StopMeasure(_renderingProfileOcclusionPrepareOccludees);
	}

	private void TestOccludees(ref Matrix viewRotationProjectionMatrix)
	{
		Profiling profiling = _profiling;
		GraphicsDevice graphics = _graphics;
		GLFunctions gL = graphics.GL;
		profiling.StartMeasure(_renderingProfileOcclusionTestOccludees);
		HiZCullProgram hiZCullProgram = graphics.GPUProgramStore.HiZCullProgram;
		gL.UseProgram(hiZCullProgram);
		hiZCullProgram.ViewportSize.SetValue((float)graphics.OcclusionMapWidth, (float)graphics.OcclusionMapHeight);
		hiZCullProgram.ViewProjectionMatrix.SetValue(ref viewRotationProjectionMatrix);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, _occlusionRenderTarget.GetTexture(RenderTarget.Target.Depth));
		gL.Enable(GL.RASTERIZER_DISCARD);
		gL.BindBufferBase(GL.TRANSFORM_FEEDBACK_BUFFER, 0u, _visibleOccludeesTFBO.InternalId);
		gL.BeginTransformFeedback(GL.NO_ERROR);
		gL.BindVertexArray(_occludeesVAO);
		gL.DrawArrays(GL.NO_ERROR, 0, _occludeesCount);
		gL.EndTransformFeedback();
		gL.Disable(GL.RASTERIZER_DISCARD);
		profiling.StopMeasure(_renderingProfileOcclusionTestOccludees);
	}

	public void FetchVisibleOccludeesFromGPU(ref int[] visibleOccludees)
	{
		Profiling profiling = _profiling;
		GLFunctions gL = _graphics.GL;
		if (IsActive)
		{
			profiling.StartMeasure(_renderingProfileOcclusionFetchResults);
			gL.Flush();
			if (_occludeesCount > 0)
			{
				gL.BindBufferBase(GL.TRANSFORM_FEEDBACK_BUFFER, 0u, _visibleOccludeesTFBO.InternalId);
				IntPtr source = gL.MapBufferRange(GL.TRANSFORM_FEEDBACK_BUFFER, (IntPtr)0, (IntPtr)(4 * _occludeesCount), GL.ONE);
				Marshal.Copy(source, _visibleOccludees, 0, _occludeesCount);
				gL.UnmapBuffer(GL.TRANSFORM_FEEDBACK_BUFFER);
			}
			visibleOccludees = _visibleOccludees;
			profiling.StopMeasure(_renderingProfileOcclusionFetchResults);
		}
		else
		{
			profiling.SkipMeasure(_renderingProfileOcclusionFetchResults);
		}
	}

	public void DebugDrawOcclusionMap(float opacity, int mipLevel)
	{
		_graphics.RTStore.DebugDrawMap(debugAsZMap: true, debugAsLinearZ: false, debugAsTexture2DArray: false, 0, useNormalQuantization: false, RenderTargetStore.DebugMapParam.ChromaSubsamplingMode.None, 0, _occlusionRenderTarget.Width, _occlusionRenderTarget.Height, _occlusionRenderTarget.GetTexture(RenderTarget.Target.Depth), opacity, mipLevel, 0, 1f, 0f, Vector4.One);
	}

	public void DebugDrawOccludees(int occludeeStartIndex, int occludeesCount, ref Matrix viewRotationProjectionMatrix, bool drawCulledOnly = false)
	{
		GLFunctions gL = _graphics.GL;
		gL.AssertActiveTexture(GL.TEXTURE0);
		Debug.Assert(_occludeesCount <= _occludeesData.Length, "OccludeeData array was modified, and is not valid for debugging anymore.");
		if (!IsActive || occludeesCount == 0 || _occludeesCount == 0)
		{
			return;
		}
		Debug.Assert(occludeeStartIndex < _occludeesCount);
		Debug.Assert(occludeeStartIndex + occludeesCount <= _occludeesCount);
		gL.BindTexture(GL.TEXTURE_2D, _graphics.WhitePixelTexture.GLTexture);
		gL.Disable(GL.DEPTH_TEST);
		gL.BindVertexArray(_debugOccludeeMesh.VertexArray);
		BasicProgram basicProgram = _graphics.GPUProgramStore.BasicProgram;
		gL.UseProgram(basicProgram);
		basicProgram.Opacity.SetValue(1f);
		Vector3 vector = new Vector3(1f, 1f, 1f);
		Vector3 vector2 = new Vector3(1f, 0f, 0f);
		for (int i = 0; i < occludeesCount; i++)
		{
			int num = i + occludeeStartIndex;
			bool flag = _visibleOccludees[num] == 1;
			if (!flag || !drawCulledOnly)
			{
				ref OccludeeData reference = ref _occludeesData[num];
				Vector3 scale = reference.BoxMax - reference.BoxMin;
				Vector3 translation = (reference.BoxMax + reference.BoxMin) * 0.5f;
				Matrix.Compose(scale, Quaternion.Identity, translation, out var result);
				Matrix.Multiply(ref result, ref viewRotationProjectionMatrix, out result);
				Vector3 value = (flag ? vector : vector2);
				basicProgram.Color.SetValue(value);
				basicProgram.MVPMatrix.SetValue(ref result);
				gL.DrawElements(GL.TRIANGLES, _debugOccludeeMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
			}
		}
		gL.Enable(GL.DEPTH_TEST);
	}
}
