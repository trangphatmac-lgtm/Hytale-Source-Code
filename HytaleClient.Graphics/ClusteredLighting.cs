#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Graphics;

internal class ClusteredLighting
{
	private struct ZSliceLightData
	{
		public const uint MaxLights = 1024u;

		public const uint UintPerCluster = 32u;

		public uint[] Bitfields;

		public ushort[] LightCounts;

		public uint LightRefCountInPreviousSlices;

		private uint _lightRefCount;

		private uint _width;

		private uint _height;

		public uint ActiveLightRefCount => _lightRefCount;

		public ZSliceLightData(uint width, uint height)
		{
			_width = width;
			_height = height;
			_lightRefCount = 0u;
			LightRefCountInPreviousSlices = 0u;
			Bitfields = new uint[width * height * 32];
			LightCounts = new ushort[width * height];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint GetClusterIndex(uint x, uint y)
		{
			return y * _width + x;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterLight(uint x, uint y, ushort lightId)
		{
			uint clusterIndex = GetClusterIndex(x, y);
			int num = lightId / 32;
			int bitId = lightId % 32;
			BitUtils.SwitchOnBit(bitId, ref Bitfields[clusterIndex * 32 + num]);
			LightCounts[clusterIndex]++;
			_lightRefCount++;
		}

		public void ClearData()
		{
			if (_lightRefCount != 0)
			{
				_lightRefCount = 0u;
				Array.Clear(Bitfields, 0, Bitfields.Length);
				Array.Clear(LightCounts, 0, LightCounts.Length);
			}
			LightRefCountInPreviousSlices = 0u;
		}
	}

	private struct ClusterBBox
	{
		public byte minZ;

		public byte maxZ;

		public byte minY;

		public byte maxY;

		public byte minX;

		public byte maxX;
	}

	private struct LightRefinementInfo
	{
		public uint CenterY;

		public uint CenterZ;
	}

	private struct TestData
	{
		public float Input;

		public float Result;

		public float ExpectedResult;

		public int ResultInt;

		public int ExpectedResultInt;

		public bool CheckResult()
		{
			return MathHelper.Distance(Result, ExpectedResult) < 0.01f;
		}

		public bool CheckResultInt()
		{
			return ResultInt == ExpectedResultInt;
		}
	}

	private bool _useRefinedVoxelization;

	private bool _useMappedGPUBuffers;

	private bool _useLightDirectAccess = true;

	private bool _usePBO;

	private bool _useDoubleBuffering = true;

	private bool _useParallelExecution = true;

	private uint _lightGridWidth;

	private uint _lightGridHeight;

	private uint _lightGridDepth;

	private uint _lightGridDataCount;

	private ushort[] _lightGridData;

	private GPUBuffer _lightGridPBO;

	private Texture _lightGridTexture;

	private ushort[] _lightIndices;

	private GPUBufferTexture _lightIndicesBufferTexture;

	private uint _lightIndicesCount;

	private uint _directPointLightCount;

	private GPUBufferTexture _directPointLightBufferTexture;

	private Vector4[] _directPointLightData;

	private Vector4[] _pointLightData = new Vector4[2048];

	private GPUBuffer _pointLightsBuffer;

	private int _globalLightDataCount;

	private ClusterBBox[] _lightClusterBBox = new ClusterBBox[1024];

	private LightRefinementInfo[] _lightRefinementInfo = new LightRefinementInfo[1024];

	private ZSliceLightData[] _zSliceLightData;

	private Plane[] _planesZ;

	private Plane[] _planesY;

	private Plane[] _planesX;

	private bool _useCustomZDistribution = true;

	private const float GridNearZCustom = 5f;

	private const float GridNearZDefault = 0.1f;

	private float _gridNearZ = 5f;

	private float _gridFarZ = 500f;

	private float _coef;

	private int _zSlicesCount;

	private float[] _depthSlices;

	private Matrix _projectionMatrix;

	private Matrix _invProjectionMatrix;

	private int _renderingProfileLightClusterClear;

	private int _renderingProfileLightClustering;

	private int _renderingProfileLightClusteringRefine;

	private int _renderingProfileLightFillGridData;

	private int _renderingProfileLightSendDataToGPU;

	private readonly Profiling _profiling;

	private readonly RenderTargetStore _renderTargetStore;

	private readonly GraphicsDevice _graphics;

	private readonly GLFunctions _gl;

	public uint GridWidth => _lightGridWidth;

	public uint GridHeight => _lightGridHeight;

	public uint GridDepth => _lightGridDepth;

	public float GridNearZ => _gridNearZ;

	public float GridFarZ => _gridFarZ;

	public float GridRangeCoef => _coef;

	public int LightCount => _globalLightDataCount;

	public GLTexture LightGridTexture => _lightGridTexture.GLTexture;

	public GLTexture DirectPointLightGpuBufferTexture => _directPointLightBufferTexture.CurrentTexture;

	public GLTexture LightIndicesGpuBufferTexture => _lightIndicesBufferTexture.CurrentTexture;

	public GLBuffer PointLightsGpuBuffer => _pointLightsBuffer.Current;

	public void UseRefinedVoxelization(bool enable)
	{
		_useRefinedVoxelization = enable;
	}

	public void UseMappedGPUBuffers(bool enable)
	{
		_useMappedGPUBuffers = enable;
	}

	public void UseLightDirectAccess(bool enable)
	{
		_useLightDirectAccess = enable;
	}

	public void UsePBO(bool enable)
	{
		_usePBO = enable;
	}

	public void UseDoubleBuffering(bool enable)
	{
		_useDoubleBuffering = enable;
	}

	public void UseParallelExecution(bool enable)
	{
		_useParallelExecution = enable;
	}

	public ClusteredLighting(GraphicsDevice graphics, RenderTargetStore renderTargetStore, Profiling profiling)
	{
		_graphics = graphics;
		_gl = _graphics.GL;
		_renderTargetStore = renderTargetStore;
		_profiling = profiling;
	}

	public void Init()
	{
		_lightGridTexture = new Texture(Texture.TextureTypes.Texture3D);
		_lightGridTexture.CreateTexture3D(1, 1, 1, IntPtr.Zero, GL.NEAREST, GL.NEAREST, GL.CLAMP_TO_EDGE, GL.CLAMP_TO_EDGE, GL.CLAMP_TO_EDGE, GL.UNSIGNED_SHORT, GL.RG16UI, GL.RG_INTEGER);
		_directPointLightCount = 0u;
		_pointLightsBuffer.CreateStorage(GL.UNIFORM_BUFFER, GL.STREAM_DRAW, useDoubleBuffering: true, (uint)(_pointLightData.Length * 4 * 4 * 2), 0u, GPUBuffer.GrowthPolicy.Never);
		SetGridResolution(16u, 8u, 24u);
	}

	public void Dispose()
	{
		_pointLightsBuffer.DestroyStorage();
		_lightIndicesBufferTexture.DestroyStorage();
		_directPointLightBufferTexture.DestroyStorage();
		_lightGridPBO.DestroyStorage();
		_lightGridTexture.Dispose();
	}

	public void SetupRenderingProfiles(int profileLightClusterClear, int profileLightClustering, int profileLightClusteringRefine, int profileLightFillGridData, int profileLightSendDataToGPU)
	{
		_renderingProfileLightClusterClear = profileLightClusterClear;
		_renderingProfileLightClustering = profileLightClustering;
		_renderingProfileLightClusteringRefine = profileLightClusteringRefine;
		_renderingProfileLightFillGridData = profileLightFillGridData;
		_renderingProfileLightSendDataToGPU = profileLightSendDataToGPU;
	}

	public void UseCustomZDistribution(bool custom)
	{
		_useCustomZDistribution = custom;
		_gridNearZ = (_useCustomZDistribution ? 5f : 0.1f);
		SetupGrid();
	}

	public void ChangeGridResolution(uint width, uint height, uint depth)
	{
		_lightIndicesBufferTexture.DestroyStorage();
		_directPointLightBufferTexture.DestroyStorage();
		_lightGridPBO.DestroyStorage();
		SetGridResolution(width, height, depth);
	}

	public void SetGridResolution(uint width, uint height, uint depth)
	{
		_lightGridWidth = width;
		_lightGridHeight = height;
		_lightGridDepth = depth;
		_lightGridDataCount = _lightGridWidth * _lightGridHeight * _lightGridDepth;
		_lightGridData = new ushort[_lightGridDataCount * 2];
		uint num = 256 * _lightGridDataCount;
		_lightIndices = new ushort[num];
		_directPointLightData = new Vector4[_lightGridDataCount * 256 * 2];
		_lightGridPBO.CreateStorage(GL.PIXEL_UNPACK_BUFFER, GL.STREAM_DRAW, useDoubleBuffering: true, (uint)(_lightGridData.Length * 2), 0u, GPUBuffer.GrowthPolicy.Never);
		_lightGridTexture.UpdateTexture3D(width, height, depth, null);
		_directPointLightBufferTexture.CreateStorage(GL.RGBA32F, GL.STREAM_DRAW, useDoubleBuffering: true, (uint)(_directPointLightData.Length * 4 * 4), 0u, GPUBuffer.GrowthPolicy.Never);
		_lightIndicesBufferTexture.CreateStorage(GL.R16UI, GL.STREAM_DRAW, useDoubleBuffering: true, (uint)(_lightIndices.Length * 2), 0u, GPUBuffer.GrowthPolicy.Never);
		_zSliceLightData = new ZSliceLightData[_lightGridDepth];
		for (int i = 0; i < _zSliceLightData.Length; i++)
		{
			_zSliceLightData[i] = new ZSliceLightData(_lightGridWidth, _lightGridHeight);
		}
		_zSlicesCount = (int)_lightGridDepth;
		_depthSlices = new float[_zSlicesCount + 1];
		SetupGrid();
	}

	public void BuildFrustumGridPlanes()
	{
		uint lightGridWidth = _lightGridWidth;
		uint lightGridHeight = _lightGridHeight;
		uint lightGridDepth = _lightGridDepth;
		if (_planesZ == null || _planesZ.Length != lightGridDepth + 1)
		{
			_planesZ = new Plane[lightGridDepth + 1];
		}
		if (_planesY == null || _planesY.Length != lightGridHeight + 1)
		{
			_planesY = new Plane[lightGridHeight + 1];
		}
		if (_planesX == null || _planesX.Length != lightGridWidth + 1)
		{
			_planesX = new Plane[lightGridWidth + 1];
		}
		for (int i = 0; i <= lightGridWidth; i++)
		{
			float x = (float)i / (float)lightGridWidth * 2f - 1f;
			Vector3 pointB = ConvertProjectionSpaceToViewSpace(new Vector3(x, 1f, -1f));
			Vector3 pointA = ConvertProjectionSpaceToViewSpace(new Vector3(x, -1f, -1f));
			_planesX[i] = CreatePlaneAtOrigin(pointA, pointB);
		}
		for (int j = 0; j <= lightGridHeight; j++)
		{
			float y = (float)j / (float)lightGridHeight * 2f - 1f;
			Vector3 pointB2 = ConvertProjectionSpaceToViewSpace(new Vector3(-1f, y, -1f));
			Vector3 pointA2 = ConvertProjectionSpaceToViewSpace(new Vector3(1f, y, -1f));
			_planesY[j] = CreatePlaneAtOrigin(pointA2, pointB2);
		}
		for (int k = 0; k <= lightGridDepth; k++)
		{
			_planesZ[k] = new Plane(Vector3.Forward, _depthSlices[k]);
		}
	}

	private void SpheresVoxelizationPerSlice(uint z)
	{
		for (int i = 0; i < _globalLightDataCount; i++)
		{
			if (_lightClusterBBox[i].minZ > z || z > _lightClusterBBox[i].maxZ)
			{
				continue;
			}
			ref Vector4 reference = ref _pointLightData[2 * i];
			for (byte b = _lightClusterBBox[i].minY; b <= _lightClusterBBox[i].maxY; b++)
			{
				for (byte b2 = _lightClusterBBox[i].minX; b2 <= _lightClusterBBox[i].maxX; b2++)
				{
					RegisterLightInCluster((ushort)i, b2, b, z);
				}
			}
		}
	}

	public void Prepare(LightData[] lightData, int lightCount, float WorldFieldOfView, Vector3 cameraPosition, ref Matrix viewRotationMatrix, ref Matrix projectionMatrix)
	{
		if (_useDoubleBuffering)
		{
			PingPongBuffers();
		}
		_globalLightDataCount = System.Math.Min(1024, lightCount);
		if (projectionMatrix != _projectionMatrix)
		{
			_projectionMatrix = projectionMatrix;
			_invProjectionMatrix = Matrix.Invert(_projectionMatrix);
			BuildFrustumGridPlanes();
		}
		_profiling.StartMeasure(_renderingProfileLightClusterClear);
		ClearLightGridData();
		_profiling.StopMeasure(_renderingProfileLightClusterClear);
		_profiling.StartMeasure(_renderingProfileLightClustering);
		float num = 1f / (float)System.Math.Tan(WorldFieldOfView / 2f);
		Vector3 projectionMatrixColumn = projectionMatrix.Column0;
		Vector3 projectionMatrixColumn2 = projectionMatrix.Column1;
		Vector3 projectionMatrixColumn3 = projectionMatrix.Column3;
		for (int i = 0; i < _globalLightDataCount; i++)
		{
			ref Vector3 color = ref lightData[i].Color;
			ref BoundingSphere sphere = ref lightData[i].Sphere;
			float radius = lightData[i].Sphere.Radius;
			Vector4 vector = new Vector4(sphere.Center.X, sphere.Center.Y, sphere.Center.Z, 1f);
			vector.X -= cameraPosition.X;
			vector.Y -= cameraPosition.Y;
			vector.Z -= cameraPosition.Z;
			vector = Vector4.Transform(vector, viewRotationMatrix);
			_pointLightData[2 * i] = new Vector4(vector.X, vector.Y, vector.Z, radius);
			_pointLightData[2 * i + 1] = new Vector4(color.X, color.Y, color.Z, 1f);
			float num2 = vector.Z + radius;
			float num3 = vector.Z - radius;
			_lightClusterBBox[i].minZ = (byte)GetLightGridDepthSlice(0f - num2);
			_lightClusterBBox[i].maxZ = (byte)GetLightGridDepthSlice(0f - num3);
			Vector2 min;
			Vector2 max;
			if (lightData[i].Sphere.Contains(cameraPosition) == ContainmentType.Contains)
			{
				min = Vector2.Zero;
				max = Vector2.One;
			}
			else
			{
				Vector3 center = new Vector3(vector.X, vector.Y, vector.Z);
				GetBoundingBox(ref center, radius, -0.1f, ref projectionMatrixColumn, ref projectionMatrixColumn2, ref projectionMatrixColumn3, out min, out max);
			}
			_lightClusterBBox[i].minX = (byte)MathHelper.Clamp(min.X * (float)_lightGridWidth, 0f, _lightGridWidth - 1);
			_lightClusterBBox[i].maxX = (byte)MathHelper.Clamp(max.X * (float)_lightGridWidth, 0f, _lightGridWidth - 1);
			_lightClusterBBox[i].minY = (byte)MathHelper.Clamp(min.Y * (float)_lightGridHeight, 0f, _lightGridHeight - 1);
			_lightClusterBBox[i].maxY = (byte)MathHelper.Clamp(max.Y * (float)_lightGridHeight, 0f, _lightGridHeight - 1);
			if (_useRefinedVoxelization)
			{
				Vector4.Transform(ref vector, ref projectionMatrix, out var result);
				result /= result.W;
				result.X = result.X * 0.5f + 0.5f;
				result.Y = result.Y * 0.5f + 0.5f;
				result.Z = result.Z * 0.5f + 0.5f;
				_lightRefinementInfo[i].CenterZ = GetLightGridDepthSlice(0f - vector.Z);
				_lightRefinementInfo[i].CenterY = (uint)System.Math.Floor(result.Y * (float)_lightGridHeight);
			}
			Debug.Assert(_lightClusterBBox[i].minZ < _lightGridDepth && _lightClusterBBox[i].maxZ < _lightGridDepth, "Error in the light cluster bounding box computation on axis Z");
			Debug.Assert(_lightClusterBBox[i].minY < _lightGridHeight && _lightClusterBBox[i].maxY < _lightGridHeight, "Error in the light cluster bounding box computation on axis Y");
			Debug.Assert(_lightClusterBBox[i].minX < _lightGridWidth && _lightClusterBBox[i].maxX < _lightGridWidth, "Error in the light cluster bounding box computation on axis X");
		}
		_profiling.StopMeasure(_renderingProfileLightClustering);
		_profiling.StartMeasure(_renderingProfileLightClusteringRefine);
		if (!_useRefinedVoxelization)
		{
			if (_useParallelExecution)
			{
				Parallel.For(0L, _lightGridDepth, delegate(long z)
				{
					SpheresVoxelizationPerSlice((uint)z);
				});
			}
			else
			{
				for (int j = 0; j < _lightGridDepth; j++)
				{
					SpheresVoxelizationPerSlice((uint)j);
				}
			}
		}
		else if (_useParallelExecution)
		{
			Parallel.For(0L, _lightGridDepth, delegate(long z)
			{
				RefineSpheresVoxelizationPerSlice((uint)z);
			});
		}
		else
		{
			for (int k = 0; k < _lightGridDepth; k++)
			{
				RefineSpheresVoxelizationPerSlice((uint)k);
			}
		}
		_profiling.StopMeasure(_renderingProfileLightClusteringRefine);
		_profiling.StartMeasure(_renderingProfileLightFillGridData);
		FillClusteredLightingBuffers();
		_profiling.StopMeasure(_renderingProfileLightFillGridData);
	}

	public unsafe void SendDataToGPU()
	{
		_profiling.StartMeasure(_renderingProfileLightSendDataToGPU);
		int xoffset = 0;
		int yoffset = 0;
		int zoffset = 0;
		if (_usePBO)
		{
			_lightGridPBO.UnpackToTexture3D(_lightGridTexture.GLTexture, 0, xoffset, yoffset, zoffset, (int)_lightGridWidth, (int)_lightGridHeight, (int)_lightGridDepth, GL.RG_INTEGER, GL.UNSIGNED_SHORT);
		}
		else
		{
			_lightGridTexture.UpdateTexture3D(_lightGridWidth, _lightGridHeight, _lightGridDepth, _lightGridData);
		}
		if (!_useLightDirectAccess)
		{
			fixed (Vector4* ptr = _pointLightData)
			{
				_pointLightsBuffer.TransferCopy((IntPtr)ptr, (uint)(_globalLightDataCount * 4 * 8));
			}
			if (!_useMappedGPUBuffers)
			{
				fixed (ushort* ptr2 = _lightIndices)
				{
					_lightIndicesBufferTexture.TransferCopy((IntPtr)ptr2, _lightIndicesCount * 2);
				}
			}
		}
		else if (!_useMappedGPUBuffers)
		{
			fixed (Vector4* ptr3 = _directPointLightData)
			{
				_directPointLightBufferTexture.TransferCopy((IntPtr)ptr3, _directPointLightCount * 2 * 4 * 4);
			}
		}
		_profiling.StopMeasure(_renderingProfileLightSendDataToGPU);
	}

	public void SkipMeasures()
	{
		_profiling.SkipMeasure(_renderingProfileLightClusterClear);
		_profiling.SkipMeasure(_renderingProfileLightClustering);
		_profiling.SkipMeasure(_renderingProfileLightClusteringRefine);
		_profiling.SkipMeasure(_renderingProfileLightFillGridData);
		_profiling.SkipMeasure(_renderingProfileLightSendDataToGPU);
	}

	public void SetupLightDataTextures(uint gridTextureUnit, uint indicesOrDataTextureUnit)
	{
		GLFunctions gL = _graphics.GL;
		GLTexture texture = (_useLightDirectAccess ? DirectPointLightGpuBufferTexture : LightIndicesGpuBufferTexture);
		gL.ActiveTexture((GL)(33984 + indicesOrDataTextureUnit));
		gL.BindTexture(GL.TEXTURE_BUFFER, texture);
		gL.ActiveTexture((GL)(33984 + gridTextureUnit));
		gL.BindTexture(GL.TEXTURE_3D, LightGridTexture);
	}

	public void DrawDeferredLights(Vector3[] frustumFarCornersVS, ref Matrix projectionMatrix, bool fullResolution, bool secondPass)
	{
		_gl.AssertDepthMask(write: false);
		_gl.AssertBlendFunc(GL.SRC_ALPHA, GL.ONE);
		_gl.AssertEnabled(GL.BLEND);
		GLFunctions gL = _graphics.GL;
		LightClusteredProgram lightClusteredProgram = _graphics.GPUProgramStore.LightClusteredProgram;
		gL.UseProgram(lightClusteredProgram);
		if (!secondPass)
		{
			SetupLightDataTextures(1u, 2u);
			_gl.ActiveTexture(GL.TEXTURE0);
			if (_graphics.UseLinearZForLight)
			{
				lightClusteredProgram.FarCorners.SetValue(frustumFarCornersVS);
			}
			else
			{
				lightClusteredProgram.ProjectionMatrix.SetValue(ref projectionMatrix);
			}
			if (!_useLightDirectAccess)
			{
				lightClusteredProgram.PointLightBlock.SetBuffer(PointLightsGpuBuffer);
			}
			lightClusteredProgram.FarClip.SetValue(1024f);
			lightClusteredProgram.LightGridResolution.SetValue(GridWidth, GridHeight, GridDepth);
			lightClusteredProgram.ZSlicesParams.SetValue(GridNearZ, GridFarZ, GridRangeCoef);
		}
		GLTexture texture = (_graphics.UseLinearZForLight ? _renderTargetStore.LinearZ.GetTexture(RenderTarget.Target.Color0) : _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Depth));
		_gl.BindTexture(GL.TEXTURE_2D, texture);
		float value = (fullResolution ? 1f : 0f);
		lightClusteredProgram.UseLBufferCompression.SetValue(value);
		_graphics.ScreenTriangleRenderer.Draw();
		_gl.AssertDepthMask(write: false);
		_gl.AssertBlendFunc(GL.SRC_ALPHA, GL.ONE);
		_gl.AssertEnabled(GL.BLEND);
	}

	private void PingPongBuffers()
	{
		_lightGridPBO.Swap();
		_lightIndicesBufferTexture.Swap();
		_directPointLightBufferTexture.Swap();
		_pointLightsBuffer.Swap();
	}

	private void SetupGrid()
	{
		ComputeGridDepthSlices(_useCustomZDistribution, _zSlicesCount, _gridNearZ, _gridFarZ, ref _coef, ref _depthSlices);
		BuildFrustumGridPlanes();
	}

	private static void ComputeGridDepthSlices(bool customNearZ, int zSliceCount, float nearZ, float farZ, ref float distributionCoef, ref float[] depthSlices)
	{
		int num = (customNearZ ? 1 : 0);
		int num2 = (customNearZ ? (zSliceCount - 1) : zSliceCount);
		float num3 = farZ / nearZ;
		distributionCoef = 1f / (float)System.Math.Log(num3);
		depthSlices[0] = 0.1f;
		for (int i = 0; i <= num2; i++)
		{
			depthSlices[i + num] = (float)((double)nearZ * System.Math.Pow(num3, (float)i / (float)num2));
		}
	}

	private static uint GetLightGridDepthSlice(bool customNearZ, int zSliceCount, float nearZ, float distributionCoef, float z)
	{
		int num = (customNearZ ? (zSliceCount - 1) : zSliceCount);
		double num2 = System.Math.Log(z / nearZ) * (double)distributionCoef;
		uint num3 = (uint)System.Math.Max(0.0, (double)num * num2);
		return (!customNearZ) ? num3 : ((!(z < nearZ)) ? (num3 + 1) : 0u);
	}

	private uint GetLightGridDepthSlice(float z)
	{
		return GetLightGridDepthSlice(_useCustomZDistribution, _zSlicesCount, _gridNearZ, _coef, z);
	}

	private void ProjectSphereToPlane(ref Plane plane, ref BoundingSphere sphere, out BoundingSphere projectedSphere)
	{
		Vector3.Dot(ref plane.Normal, ref sphere.Center, out var result);
		result -= plane.D;
		projectedSphere.Center = sphere.Center - plane.Normal * result;
		projectedSphere.Radius = (float)System.Math.Sqrt(System.Math.Max(0.0, sphere.Radius * sphere.Radius - result * result));
	}

	private Vector3 ConvertProjectionSpaceToViewSpace(Vector3 posPS)
	{
		Vector4 vector = Vector4.Transform(posPS, _invProjectionMatrix);
		return new Vector3(vector.X / vector.W, vector.Y / vector.W, vector.Z / vector.W);
	}

	private void RefineSpheresVoxelizationPerSlice(uint z)
	{
		for (int i = 0; i < _globalLightDataCount; i++)
		{
			if (_lightClusterBBox[i].minZ > z || z > _lightClusterBBox[i].maxZ)
			{
				continue;
			}
			uint minZ = _lightClusterBBox[i].minZ;
			uint maxZ = _lightClusterBBox[i].maxZ;
			uint minY = _lightClusterBBox[i].minY;
			uint maxY = _lightClusterBBox[i].maxY;
			uint minX = _lightClusterBBox[i].minX;
			uint maxX = _lightClusterBBox[i].maxX;
			ref Vector4 reference = ref _pointLightData[2 * i];
			uint centerZ = _lightRefinementInfo[i].CenterZ;
			uint centerY = _lightRefinementInfo[i].CenterY;
			BoundingSphere sphere = new BoundingSphere(new Vector3(reference.X, reference.Y, reference.Z), reference.W);
			if (z != centerZ)
			{
				Plane plane = ((z < centerZ) ? _planesZ[z + 1] : new Plane(-_planesZ[z].Normal, 0f - _planesZ[z].D));
				ProjectSphereToPlane(ref plane, ref sphere, out sphere);
			}
			for (uint num = minY; num <= maxY; num++)
			{
				BoundingSphere sphere2 = sphere;
				if (num != centerY)
				{
					Plane plane2 = ((num < centerY) ? _planesY[num + 1] : new Plane(-_planesY[num].Normal, 0f - _planesY[num].D));
					ProjectSphereToPlane(ref plane2, ref sphere2, out sphere2);
				}
				int num2 = (int)minX;
				do
				{
					num2++;
				}
				while (num2 <= maxX && ComputeSignedDistanceFromPlane(ref sphere2.Center, ref _planesX[num2]) >= sphere2.Radius);
				int num3 = (int)(maxX + 1);
				do
				{
					num3--;
				}
				while (num3 >= num2 && 0f - ComputeSignedDistanceFromPlane(ref sphere2.Center, ref _planesX[num3]) >= sphere2.Radius);
				num2--;
				for (num3++; num2 < num3; num2++)
				{
					RegisterLightInCluster((ushort)i, (uint)num2, num, z);
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float ComputeSignedDistanceFromPlane(ref Vector3 point, ref Plane plane)
	{
		return Vector3.Dot(plane.Normal, point);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Plane CreatePlaneAtOrigin(Vector3 pointA, Vector3 pointB)
	{
		return new Plane(Vector3.Normalize(Vector3.Cross(pointA, pointB)), 0f);
	}

	private void GetBoundsForAxis(bool xAxis, ref Vector3 center, float radius, float nearZ, out Vector3 U, out Vector3 L)
	{
		bool flag = center.Z + radius < nearZ;
		Vector3 vector = (xAxis ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 1f, 0f));
		Vector2 vector2 = new Vector2(Vector3.Dot(vector, center), center.Z);
		Vector2 vector3 = new Vector2(0f);
		Vector2 vector4 = new Vector2(0f);
		float num = Vector2.Dot(vector2, vector2) - MathHelper.Square(radius);
		float num2 = 0f;
		float num3 = 0f;
		if (num > 0f)
		{
			float num4 = (float)System.Math.Sqrt(num);
			float num5 = vector2.Length();
			num2 = num4 / num5;
			num3 = radius / num5;
		}
		float num6 = 0f;
		if (!flag)
		{
			num6 = (float)System.Math.Sqrt(MathHelper.Square(radius) - MathHelper.Square(nearZ - vector2.Y));
		}
		if (num > 0f)
		{
			Vector2 value = new Vector2(num2, 0f - num3);
			vector3 = new Vector2(y: Vector2.Dot(new Vector2(num3, num2), vector2), x: Vector2.Dot(value, vector2)) * num2;
		}
		if (!flag && (num <= 0f || vector3.Y > nearZ))
		{
			vector3.X = vector2.X + num6;
			vector3.Y = nearZ;
		}
		num3 *= -1f;
		num6 *= -1f;
		if (num > 0f)
		{
			Vector2 value2 = new Vector2(num2, 0f - num3);
			vector4 = new Vector2(y: Vector2.Dot(new Vector2(num3, num2), vector2), x: Vector2.Dot(value2, vector2)) * num2;
		}
		if (!flag && (num <= 0f || vector4.Y > nearZ))
		{
			vector4.X = vector2.X + num6;
			vector4.Y = nearZ;
		}
		num3 *= -1f;
		num6 *= -1f;
		U = vector * vector3.X;
		U.Z = vector3.Y;
		L = vector * vector4.X;
		L.Z = vector4.Y;
	}

	private void GetBoundingBox(ref Vector3 center, float radius, float nearZ, ref Vector3 projectionMatrixColumn0, ref Vector3 projectionMatrixColumn1, ref Vector3 projectionMatrixColumn3, out Vector2 min, out Vector2 max)
	{
		GetBoundsForAxis(xAxis: true, ref center, radius, nearZ, out var U, out var L);
		GetBoundsForAxis(xAxis: false, ref center, radius, nearZ, out var U2, out var L2);
		max.X = Vector3.Dot(U, projectionMatrixColumn0) / Vector3.Dot(U, projectionMatrixColumn3);
		min.X = Vector3.Dot(L, projectionMatrixColumn0) / Vector3.Dot(L, projectionMatrixColumn3);
		max.Y = Vector3.Dot(U2, projectionMatrixColumn1) / Vector3.Dot(U2, projectionMatrixColumn3);
		min.Y = Vector3.Dot(L2, projectionMatrixColumn1) / Vector3.Dot(L2, projectionMatrixColumn3);
		max.X = 0.5f + 0.5f * max.X;
		min.X = 0.5f + 0.5f * min.X;
		max.Y = 0.5f + 0.5f * max.Y;
		min.Y = 0.5f + 0.5f * min.Y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RegisterLightInCluster(ushort lightId, uint clusterX, uint clusterY, uint clusterZ)
	{
		if (lightId >= 1024)
		{
			throw new Exception($"Light Id too high - we support light rendering only up to {1024}.");
		}
		if (clusterX >= _lightGridWidth || clusterY >= _lightGridHeight || clusterZ >= _lightGridDepth)
		{
			throw new Exception($"Cluster XYZ ({clusterX}, {clusterY}, {clusterZ}) out of bounds ({_lightGridWidth}, {_lightGridHeight}, {_lightGridDepth}).");
		}
		_zSliceLightData[clusterZ].RegisterLight(clusterX, clusterY, lightId);
	}

	private void ClearLightGridData()
	{
		if (!_usePBO)
		{
			Array.Clear(_lightGridData, 0, _lightGridData.Length);
		}
		for (int i = 0; i < _lightGridDepth; i++)
		{
			_zSliceLightData[i].ClearData();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint GetLightGridAccess(uint x, uint y, uint z)
	{
		return z * (_lightGridWidth * _lightGridHeight) + y * _lightGridWidth + x;
	}

	private unsafe void FillClusteredLightingBuffersSlice(uint z, IntPtr lightGridPtr, IntPtr directPointLightPtr, IntPtr lightIndicesPtr)
	{
		if (_zSliceLightData[z].ActiveLightRefCount == 0 && !_usePBO)
		{
			return;
		}
		uint num = 0u;
		uint num2 = _zSliceLightData[z].LightRefCountInPreviousSlices * 2;
		uint num3 = _zSliceLightData[z].LightRefCountInPreviousSlices;
		for (uint num4 = 0u; num4 < _lightGridHeight; num4++)
		{
			for (uint num5 = 0u; num5 < _lightGridWidth; num5++)
			{
				uint num6 = _zSliceLightData[z].LightCounts[num];
				if (num6 != 0)
				{
					uint lightGridAccess = GetLightGridAccess(num5, num4, z);
					uint num7 = (_useLightDirectAccess ? num2 : num3);
					uint num8 = num6;
					uint num9 = num7 >> 3;
					uint num10 = (num8 << 3) | (num7 & 7u);
					if (_usePBO)
					{
						ushort* ptr = (ushort*)lightGridPtr.ToPointer();
						ptr[2 * lightGridAccess] = (ushort)num9;
						ptr[2 * lightGridAccess + 1] = (ushort)num10;
					}
					else
					{
						_lightGridData[2 * lightGridAccess] = (ushort)num9;
						_lightGridData[2 * lightGridAccess + 1] = (ushort)num10;
					}
					int num11 = 0;
					ushort num12 = 0;
					for (int i = 0; (long)i < 32L; i++)
					{
						uint num13 = _zSliceLightData[z].Bitfields[num * 32 + i];
						if (num13 != 0)
						{
							for (int j = 0; j < 32; j++)
							{
								if (!BitUtils.IsBitOn(j, num13))
								{
									continue;
								}
								num12 = (ushort)(i * 32 + j);
								if (!_useMappedGPUBuffers)
								{
									if (!_useLightDirectAccess)
									{
										ushort num14 = (ushort)(num12 * 2);
										_lightIndices[num3] = num14;
										num3++;
									}
									else
									{
										_directPointLightData[num2] = _pointLightData[2 * num12];
										_directPointLightData[num2 + 1] = _pointLightData[2 * num12 + 1];
										num2 += 2;
									}
								}
								else if (!_useLightDirectAccess)
								{
									ushort num15 = (ushort)(num12 * 2);
									ushort* ptr2 = (ushort*)lightIndicesPtr.ToPointer();
									ptr2[num3] = num15;
									num3++;
								}
								else
								{
									Vector4* ptr3 = (Vector4*)directPointLightPtr.ToPointer();
									ptr3[num2] = _pointLightData[2 * num12];
									ptr3[num2 + 1] = _pointLightData[2 * num12 + 1];
									num2 += 2;
								}
								num11++;
								if (num11 == num6)
								{
									break;
								}
							}
						}
						if (num11 == num6)
						{
							break;
						}
					}
				}
				else if (_usePBO)
				{
					uint lightGridAccess2 = GetLightGridAccess(num5, num4, z);
					ushort* ptr4 = (ushort*)lightGridPtr.ToPointer();
					ptr4[2 * lightGridAccess2] = 0;
					ptr4[2 * lightGridAccess2 + 1] = 0;
				}
				num++;
			}
		}
	}

	private void FillClusteredLightingBuffers()
	{
		IntPtr lightGridPtr = IntPtr.Zero;
		IntPtr directPointLightPtr = IntPtr.Zero;
		IntPtr lightIndicesPtr = IntPtr.Zero;
		if (_usePBO)
		{
			lightGridPtr = _lightGridPBO.BeginTransfer((uint)(_lightGridData.Length * 2));
		}
		if (_useMappedGPUBuffers)
		{
			if (!_useLightDirectAccess)
			{
				lightIndicesPtr = _lightIndicesBufferTexture.BeginTransfer((uint)(_lightIndices.Length * 2));
			}
			else
			{
				directPointLightPtr = _directPointLightBufferTexture.BeginTransfer((uint)(_directPointLightData.Length * 4 * 4));
			}
		}
		for (uint num = 1u; num < _lightGridDepth; num++)
		{
			_zSliceLightData[num].LightRefCountInPreviousSlices = _zSliceLightData[num - 1].LightRefCountInPreviousSlices + _zSliceLightData[num - 1].ActiveLightRefCount;
		}
		_lightIndicesCount = _zSliceLightData[_lightGridDepth - 1].LightRefCountInPreviousSlices + _zSliceLightData[_lightGridDepth - 1].ActiveLightRefCount;
		_directPointLightCount = _zSliceLightData[_lightGridDepth - 1].LightRefCountInPreviousSlices + _zSliceLightData[_lightGridDepth - 1].ActiveLightRefCount;
		if (_useParallelExecution)
		{
			Parallel.For(0L, _lightGridDepth, delegate(long z)
			{
				FillClusteredLightingBuffersSlice((uint)z, lightGridPtr, directPointLightPtr, lightIndicesPtr);
			});
		}
		else
		{
			for (int i = 0; i < _lightGridDepth; i++)
			{
				FillClusteredLightingBuffersSlice((uint)i, lightGridPtr, directPointLightPtr, lightIndicesPtr);
			}
		}
		if (_useMappedGPUBuffers)
		{
			if (!_useLightDirectAccess)
			{
				_lightIndicesBufferTexture.EndTransfer();
			}
			else
			{
				_directPointLightBufferTexture.EndTransfer();
			}
		}
		if (_usePBO)
		{
			_lightGridPBO.EndTransfer();
		}
	}

	public static void UnitTest()
	{
		float nearZ = 0.1f;
		float farZ = 500f;
		float distributionCoef = 1f;
		float[] depthSlices = new float[17];
		int num = depthSlices.Length;
		TestData[] array = new TestData[num];
		ComputeGridDepthSlices(customNearZ: false, 16, nearZ, farZ, ref distributionCoef, ref depthSlices);
		float[] array2 = new float[17]
		{
			0.1f, 0.17f, 0.29f, 0.49f, 0.84f, 1.43f, 2.44f, 4.15f, 7.07f, 12.04f,
			20.5f, 34.91f, 59.46f, 101.25f, 172.42f, 293.62f, 500f
		};
		for (int i = 0; i < num; i++)
		{
			array[i].ExpectedResult = array2[i];
			array[i].Result = depthSlices[i];
		}
		for (int j = 0; j < num; j++)
		{
			Debug.Assert(array[j].CheckResult(), $"Error in test data {j}.");
		}
		num = 10;
		ComputeGridDepthSlices(customNearZ: false, 16, nearZ, farZ, ref distributionCoef, ref depthSlices);
		float[] array3 = new float[10] { 0.1f, 0.3f, 1f, 2f, 5f, 10f, 30f, 50f, 100f, 200f };
		int[] array4 = new int[10] { 0, 2, 4, 5, 7, 8, 10, 11, 12, 14 };
		for (int k = 0; k < num; k++)
		{
			array[k].Input = array3[k];
			array[k].ExpectedResultInt = array4[k];
			array[k].ResultInt = (int)GetLightGridDepthSlice(customNearZ: false, 16, nearZ, distributionCoef, array3[k]);
		}
		for (int l = 0; l < num; l++)
		{
			Debug.Assert(array[l].CheckResultInt(), $"Error in test data {l}.");
		}
		num = depthSlices.Length;
		bool customNearZ = true;
		nearZ = 5f;
		ComputeGridDepthSlices(customNearZ, 16, nearZ, farZ, ref distributionCoef, ref depthSlices);
		float[] array5 = new float[17]
		{
			0.1f, 5f, 6.79f, 9.24f, 12.56f, 17.07f, 23.21f, 31.55f, 42.88f, 58.29f,
			79.24f, 107.72f, 146.43f, 199.05f, 270.58f, 367.82f, 500f
		};
		for (int m = 0; m < num; m++)
		{
			array[m].ExpectedResult = array5[m];
			array[m].Result = depthSlices[m];
		}
		for (int n = 0; n < num; n++)
		{
			Debug.Assert(array[n].CheckResult(), $"Error in test data {n}.");
		}
		num = 10;
		bool customNearZ2 = true;
		nearZ = 5f;
		ComputeGridDepthSlices(customNearZ2, 16, nearZ, farZ, ref distributionCoef, ref depthSlices);
		float[] array6 = new float[10] { 0.1f, 0.3f, 1f, 2f, 5.1f, 10f, 30f, 50f, 100f, 200f };
		int[] array7 = new int[10] { 0, 0, 0, 0, 1, 3, 6, 8, 10, 13 };
		for (int num2 = 0; num2 < num; num2++)
		{
			array[num2].Input = array6[num2];
			array[num2].ExpectedResultInt = array7[num2];
			array[num2].ResultInt = (int)GetLightGridDepthSlice(customNearZ2, 16, nearZ, distributionCoef, array6[num2]);
		}
		for (int num3 = 0; num3 < num; num3++)
		{
			Debug.Assert(array[num3].CheckResultInt(), $"Error in test data {num3}.");
		}
	}
}
