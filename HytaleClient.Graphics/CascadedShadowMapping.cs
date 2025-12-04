#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics;

internal class CascadedShadowMapping
{
	public struct RenderData
	{
		public const int MaxShadowMapCascades = 4;

		public float DynamicShadowIntensity;

		public BoundingFrustum VirtualSunViewFrustum;

		public KDop VirtualSunKDopFrustum;

		public Vector3 VirtualSunPosition;

		public Vector3 VirtualSunDirection;

		public Vector3[] VirtualSunPositions;

		public Matrix[] VirtualSunViewRotationMatrix;

		public Matrix[] VirtualSunProjectionMatrix;

		public Matrix[] VirtualSunViewRotationProjectionMatrix;

		public Vector2[] CascadeDistanceAndTexelScales;

		public Vector3[] CascadeCachedTranslations;
	}

	public struct InputParams
	{
		public Vector3 LightDirection;

		public float WorldFieldOfView;

		public float AspectRatio;

		public float NearClipDistance;

		public Vector3 CameraPosition;

		public Matrix ViewRotationMatrix;

		public Matrix ViewRotationProjectionMatrix;

		public bool IsSpatialContinuityLost;

		public Vector2 QuantifiedCameraMotion;

		public Vector3 CameraPositionDelta;

		public uint FrameId;
	}

	public struct CascadedShadowsBuildSettings
	{
		public int Count;

		public bool UseGlobalKDop;

		public bool UseCaching;

		public bool UseStableProjection;

		public bool UseSlopeScaleBias;

		public Vector2 SlopeScaleBias;
	}

	public struct DeferredShadowsSettings
	{
		public float ResolutionScale;

		public bool UseBlur;

		public bool UseNoise;

		public bool UseManualMode;

		public bool UseFading;

		public bool UseSingleSample;

		public bool UseCameraBias;

		public bool UseNormalBias;
	}

	public static Vector2 DefaultDeferredShadowResolutionScale = new Vector2(1f);

	private GraphicsDevice _graphics;

	private GPUProgramStore _gpuProgramStore;

	private RenderTargetStore _renderTargetStore;

	private CascadedShadowsBuildSettings _sunShadowCascades;

	private DeferredShadowsSettings _sunDeferredShadows;

	private InputParams _csmInputParams;

	private float _maxWorldHeight = 320f;

	private bool[] _cascadeNeedsUpdate = new bool[4];

	private BoundingFrustum[] _cascadeFrustums = new BoundingFrustum[4];

	private Vector2[] _cascadeDistanceAndTexelScales = new Vector2[4];

	private Vector3[] _cascadeCachedTranslations = new Vector3[4];

	private float[] _cascadeSizes = new float[4];

	private float[] _cascadeDistances = new float[5];

	private Vector3[] _tmpFrustumCorners = new Vector3[8];

	private Action _shadowCastersDrawFunc;

	private bool _isDebuggingFreeze;

	private bool _isDebuggingCameraFrustum = false;

	private bool _isDebuggingCameraFrustumSplits = false;

	private bool _isDebuggingShadowCascadeFrustums = false;

	private Vector3 _debugCameraPosition;

	private Mesh _debugCameraFrustumMesh;

	private Mesh[] _debugCameraFrustumSplitMeshes = new Mesh[4];

	private Mesh[] _debugShadowCascadeFrustumMeshes = new Mesh[4];

	private Vector3[] _debugCascadeColors = new Vector3[4];

	public ref CascadedShadowsBuildSettings CascadesSettings => ref _sunShadowCascades;

	public ref DeferredShadowsSettings DeferredShadowSettings => ref _sunDeferredShadows;

	public bool[] CascadeNeedsUpdate => _cascadeNeedsUpdate;

	public BoundingFrustum[] CascadeFrustums => _cascadeFrustums;

	public bool UseSunShadowsGlobalKDop => _sunShadowCascades.UseGlobalKDop;

	public bool UseDeferredShadowBlur => _sunDeferredShadows.UseBlur;

	public bool NeedsDebugDrawShadowRelated => _isDebuggingCameraFrustum || _isDebuggingCameraFrustumSplits || _isDebuggingShadowCascadeFrustums;

	private bool IsFrozenForDebug => _isDebuggingFreeze || _isDebuggingCameraFrustumSplits || _isDebuggingShadowCascadeFrustums;

	public CascadedShadowMapping(GraphicsDevice graphics)
	{
		_graphics = graphics;
		_gpuProgramStore = graphics.GPUProgramStore;
		_renderTargetStore = graphics.RTStore;
	}

	public void Dispose()
	{
		_renderTargetStore = null;
		_gpuProgramStore = null;
		_graphics = null;
	}

	public void SetSunShadowsMaxWorldHeight(float maxWorldHeight)
	{
		_maxWorldHeight = maxWorldHeight;
	}

	public void SetSunShadowsSlopeScaleBias(float factor, float units)
	{
		if (factor != float.NaN && units != float.NaN)
		{
			_sunShadowCascades.SlopeScaleBias.X = factor;
			_sunShadowCascades.SlopeScaleBias.Y = units;
		}
	}

	public void SetSunShadowsCascadeCount(int count)
	{
		int max = 4;
		int num = MathHelper.Clamp(count, 1, max);
		if (_sunShadowCascades.Count != num)
		{
			int width = _renderTargetStore.ShadowMap.Width / _sunShadowCascades.Count;
			_sunShadowCascades.Count = num;
			SetSunShadowMapResolution((uint)width, (uint)_renderTargetStore.ShadowMap.Height);
			_gpuProgramStore.DeferredShadowProgram.CascadeCount = (uint)num;
			_gpuProgramStore.DeferredShadowProgram.Reset();
			_gpuProgramStore.VolumetricSunshaftProgram.CascadeCount = (uint)num;
			_gpuProgramStore.VolumetricSunshaftProgram.Reset();
		}
	}

	public void SetSunShadowMapResolution(uint width, uint height = 0u)
	{
		RenderTargetStore rTStore = _graphics.RTStore;
		height = ((height == 0) ? width : height);
		width *= (uint)_sunShadowCascades.Count;
		if (width <= 8192 && height <= 8192 && (rTStore.ShadowMap.Width != width || rTStore.ShadowMap.Height != height))
		{
			rTStore.ShadowMap.Resize((int)width, (int)height);
		}
	}

	public void SetDeferredShadowResolutionScale(float scale)
	{
		RenderTargetStore rTStore = _graphics.RTStore;
		if (scale > 0f && scale <= 1f && _sunDeferredShadows.ResolutionScale != scale)
		{
			_sunDeferredShadows.ResolutionScale = scale;
			rTStore.SetDeferredShadowResolutionScale(scale);
		}
	}

	public void SetSunShadowsGlobalKDopEnabled(bool enable)
	{
		_sunShadowCascades.UseGlobalKDop = enable;
	}

	public void SetSunShadowMapCachingEnabled(bool enable)
	{
		_sunShadowCascades.UseCaching = enable;
	}

	public void SetSunShadowMappingStableProjectionEnabled(bool enable)
	{
		_sunShadowCascades.UseStableProjection = enable;
	}

	public void SetSunShadowMappingUseLinearZ(bool enable)
	{
		_gpuProgramStore.DeferredShadowProgram.UseLinearZ = enable;
		_gpuProgramStore.DeferredShadowProgram.Reset();
	}

	public void SetSunShadowsUseCleanBackfaces(bool enable)
	{
		_gpuProgramStore.DeferredShadowProgram.UseCleanBackfaces = enable;
		_gpuProgramStore.DeferredShadowProgram.Reset();
	}

	public void SetDeferredShadowsBlurEnabled(bool enable)
	{
		if (_sunDeferredShadows.UseBlur != enable)
		{
			_sunDeferredShadows.UseBlur = enable;
		}
	}

	public void SetDeferredShadowsNoiseEnabled(bool enable)
	{
		if (_sunDeferredShadows.UseNoise != enable)
		{
			_sunDeferredShadows.UseNoise = enable;
			_gpuProgramStore.DeferredShadowProgram.UseNoise = enable;
			_gpuProgramStore.DeferredShadowProgram.Reset();
		}
	}

	public void SetDeferredShadowsManualModeEnabled(bool enable)
	{
		if (_sunDeferredShadows.UseManualMode != enable)
		{
			_sunDeferredShadows.UseManualMode = enable;
			_gpuProgramStore.DeferredShadowProgram.UseManualMode = enable;
			_gpuProgramStore.DeferredShadowProgram.Reset();
			_gpuProgramStore.VolumetricSunshaftProgram.UseManualMode = enable;
			_gpuProgramStore.VolumetricSunshaftProgram.Reset();
		}
	}

	public void SetDeferredShadowsFadingEnabled(bool enable)
	{
		if (_sunDeferredShadows.UseFading != enable)
		{
			_sunDeferredShadows.UseFading = enable;
			_gpuProgramStore.DeferredShadowProgram.UseFading = enable;
			_gpuProgramStore.DeferredShadowProgram.Reset();
		}
	}

	public void SetDeferredShadowsWithSingleSampleEnabled(bool enable)
	{
		if (_sunDeferredShadows.UseSingleSample != enable)
		{
			_sunDeferredShadows.UseSingleSample = enable;
			_gpuProgramStore.DeferredShadowProgram.UseSingleSample = enable;
			_gpuProgramStore.DeferredShadowProgram.Reset();
		}
	}

	public void SetDeferredShadowsCameraBiasEnabled(bool enable)
	{
		if (_sunDeferredShadows.UseCameraBias != enable)
		{
			_sunDeferredShadows.UseCameraBias = enable;
			_gpuProgramStore.DeferredShadowProgram.UseCameraBias = enable;
			_gpuProgramStore.DeferredShadowProgram.Reset();
		}
	}

	public void SetDeferredShadowsNormalBiasEnabled(bool enable)
	{
		if (_sunDeferredShadows.UseNormalBias != enable)
		{
			_sunDeferredShadows.UseNormalBias = enable;
			_gpuProgramStore.DeferredShadowProgram.UseNormalBias = enable;
			_gpuProgramStore.DeferredShadowProgram.Reset();
		}
	}

	public void Init(Action shadowCastersDrawFunc)
	{
		RegisterShadowCastersDrawFunc(shadowCastersDrawFunc);
		InitShadowCascades();
		InitDeferredShadows();
		InitShadowCascadesDebug();
	}

	public void Release()
	{
		DisposeShadowCascadesDebug();
		UnregisterShadowCastersDrawFunc();
	}

	private void InitShadowCascades()
	{
		for (int i = 0; i < 4; i++)
		{
			_cascadeFrustums[i] = new BoundingFrustum(Matrix.Identity);
		}
		_sunShadowCascades.Count = 1;
		_sunShadowCascades.UseGlobalKDop = true;
		_sunShadowCascades.UseCaching = true;
		_sunShadowCascades.UseStableProjection = true;
		_sunShadowCascades.UseSlopeScaleBias = true;
		_sunShadowCascades.SlopeScaleBias = new Vector2(1f, 1f);
	}

	private void InitDeferredShadows()
	{
		_sunDeferredShadows.ResolutionScale = 1f;
		_sunDeferredShadows.UseNoise = true;
		_sunDeferredShadows.UseManualMode = false;
		_sunDeferredShadows.UseFading = false;
		_sunDeferredShadows.UseSingleSample = true;
		_sunDeferredShadows.UseCameraBias = false;
		_sunDeferredShadows.UseNormalBias = true;
		_sunDeferredShadows.UseBlur = true;
	}

	public void Update(ref InputParams csmInputParams, ref RenderData csmRenderParams)
	{
		_csmInputParams = csmInputParams;
		if (IsFrozenForDebug)
		{
			for (int i = 0; i < _sunShadowCascades.Count; i++)
			{
				_cascadeCachedTranslations[i] += csmInputParams.CameraPositionDelta;
				csmRenderParams.CascadeCachedTranslations[i] = _cascadeCachedTranslations[i];
			}
			return;
		}
		int size = 48;
		UpdateShadowCascadeDistances(size);
		bool flag = !_sunShadowCascades.UseCaching || _csmInputParams.IsSpatialContinuityLost;
		for (int j = 0; j < _sunShadowCascades.Count; j++)
		{
			_cascadeNeedsUpdate[j] = flag || ShouldUpdateCascade(j, _csmInputParams.FrameId, _csmInputParams.QuantifiedCameraMotion);
		}
		UpdateGlobalShadowFrustum(_csmInputParams.LightDirection, _csmInputParams.WorldFieldOfView, _csmInputParams.AspectRatio, _csmInputParams.CameraPosition, ref _csmInputParams.ViewRotationMatrix, out var virtualLightPosition, out var virtualSunViewFrustumMatrix);
		csmRenderParams.VirtualSunPosition = virtualLightPosition;
		csmRenderParams.VirtualSunViewFrustum.Matrix = virtualSunViewFrustumMatrix;
		UpdateShadowCascadeFrustums(_csmInputParams.LightDirection, _csmInputParams.WorldFieldOfView, _csmInputParams.AspectRatio, _csmInputParams.CameraPosition, ref _csmInputParams.ViewRotationMatrix, _csmInputParams.CameraPositionDelta, ref csmRenderParams);
		if (UseSunShadowsGlobalKDop)
		{
			float farPlaneDistance = _cascadeDistances[_sunShadowCascades.Count];
			Matrix matrix = Matrix.CreatePerspectiveFieldOfView(_csmInputParams.WorldFieldOfView, _csmInputParams.AspectRatio, _csmInputParams.NearClipDistance, farPlaneDistance);
			Matrix value = Matrix.Multiply(_csmInputParams.ViewRotationMatrix, matrix);
			BoundingFrustum frustum = new BoundingFrustum(value);
			csmRenderParams.VirtualSunKDopFrustum.BuildFrom(frustum, _csmInputParams.LightDirection);
		}
		csmRenderParams.VirtualSunDirection = _csmInputParams.LightDirection;
		for (int k = 0; k < 4; k++)
		{
			csmRenderParams.CascadeDistanceAndTexelScales[k] = _cascadeDistanceAndTexelScales[k];
			csmRenderParams.CascadeCachedTranslations[k] = _cascadeCachedTranslations[k];
		}
	}

	private void StabilizeProjection(int shadowMapWidth, int shadowMapHeight, ref Matrix shadowViewMatrix, ref Matrix shadowProjectionMatrix, ref Matrix shadowViewProjectionMatrix)
	{
		Vector3 position = new Vector3(0f, 0f, 0f);
		position = Vector3.Transform(position, shadowViewProjectionMatrix);
		position.X = position.X * (float)shadowMapWidth / 2f;
		position.Y = position.Y * (float)shadowMapWidth / 2f;
		position.Z = position.Z * (float)shadowMapWidth / 2f;
		Vector3 vector = default(Vector3);
		vector.X = MathHelper.Round(position.X);
		vector.Y = MathHelper.Round(position.Y);
		vector.Z = MathHelper.Round(position.Z);
		Vector3 vector2 = vector - position;
		vector2.X = vector2.X * 2f / (float)shadowMapWidth;
		vector2.Y = vector2.Y * 2f / (float)shadowMapWidth;
		vector2.Z = vector2.Z * 2f / (float)shadowMapWidth;
		shadowProjectionMatrix.M41 += vector2.X;
		shadowProjectionMatrix.M42 += vector2.Y;
		shadowProjectionMatrix.M43 += 0f;
		Matrix.Multiply(ref shadowViewMatrix, ref shadowProjectionMatrix, out shadowViewProjectionMatrix);
	}

	private void ComputeShadowCascadeData(Vector3 lightDirection, float nearDistance, float farDistance, float worldFoV, float aspectRatio, Vector3 cameraPosition, ref Matrix viewRotationMatrix, int shadowMapWidth, int shadowMapHeight, bool useStableProjection, out Vector3 virtualLightPosition, out Matrix shadowViewMatrix, out Matrix shadowProjectionMatrix, out Matrix shadowViewProjectionMatrix)
	{
		Matrix matrix = Matrix.CreatePerspectiveFieldOfView(worldFoV, aspectRatio, nearDistance, farDistance);
		Matrix value = Matrix.Multiply(viewRotationMatrix, matrix);
		BoundingFrustum boundingFrustum = new BoundingFrustum(value);
		boundingFrustum.GetCorners(_tmpFrustumCorners);
		BoundingSphere boundingSphere = default(BoundingSphere);
		boundingSphere.Center = (_tmpFrustumCorners[7] + _tmpFrustumCorners[6] + _tmpFrustumCorners[5] + _tmpFrustumCorners[4] + _tmpFrustumCorners[3] + _tmpFrustumCorners[2] + _tmpFrustumCorners[1] + _tmpFrustumCorners[0]) / 8f;
		boundingSphere.Radius = Vector3.Distance(_tmpFrustumCorners[0], boundingSphere.Center);
		Vector3 vector = new Vector3(0.1f, 0f, 0f);
		float num = System.Math.Min(_maxWorldHeight, 400f);
		num -= cameraPosition.Y;
		num = System.Math.Max(boundingSphere.Center.Y + 10f, num);
		Plane plane = new Plane(Vector3.Down, num);
		ComputeIntersection(plane, boundingSphere.Center, lightDirection, out var intersection);
		intersection += vector;
		Matrix shadowViewMatrix2 = Matrix.CreateLookAt(intersection, boundingSphere.Center, Vector3.Up);
		float value2 = Vector3.Distance(boundingSphere.Center, intersection) + boundingSphere.Radius;
		value2 = MathHelper.Clamp(value2, 0f, 720f);
		value2 = MathHelper.Round(value2);
		value2 = 720f;
		float num2 = (float)MathHelper.Round(boundingSphere.Radius) + 1f;
		float num3 = num2 * 2f;
		Matrix shadowProjectionMatrix2 = Matrix.CreateOrthographic(num3, num3, 1f, value2);
		Matrix shadowViewProjectionMatrix2 = Matrix.Multiply(shadowViewMatrix2, shadowProjectionMatrix2);
		if (useStableProjection)
		{
			StabilizeProjection(shadowMapWidth, shadowMapHeight, ref shadowViewMatrix2, ref shadowProjectionMatrix2, ref shadowViewProjectionMatrix2);
		}
		shadowViewMatrix = shadowViewMatrix2;
		shadowProjectionMatrix = shadowProjectionMatrix2;
		shadowViewProjectionMatrix = shadowViewProjectionMatrix2;
		virtualLightPosition = intersection;
	}

	private void UpdateShadowCascadeDistances(int size)
	{
		_cascadeDistances[0] = _csmInputParams.NearClipDistance;
		switch (_sunShadowCascades.Count)
		{
		case 1:
			_cascadeSizes[0] = size;
			_cascadeDistances[1] = size;
			_cascadeDistanceAndTexelScales[0].X = size;
			_cascadeDistanceAndTexelScales[0].Y = 0.25f;
			break;
		case 2:
			_cascadeSizes[0] = size / 4;
			_cascadeSizes[1] = size;
			_cascadeDistances[1] = (float)size / 4f;
			_cascadeDistances[2] = size;
			_cascadeDistanceAndTexelScales[0].X = (float)size / 4f;
			_cascadeDistanceAndTexelScales[1].X = size;
			_cascadeDistanceAndTexelScales[0].Y = 1f;
			_cascadeDistanceAndTexelScales[1].Y = 0.25f;
			break;
		case 3:
			_cascadeSizes[0] = size / 4;
			_cascadeSizes[1] = (float)size * 1f;
			_cascadeSizes[2] = (float)size * 2f;
			_cascadeDistances[1] = (float)size / 4f;
			_cascadeDistances[2] = (float)size * 1f;
			_cascadeDistances[3] = (float)size * 2f;
			_cascadeDistanceAndTexelScales[0].X = (float)size / 4f;
			_cascadeDistanceAndTexelScales[1].X = (float)size * 1f;
			_cascadeDistanceAndTexelScales[2].X = (float)size * 2f;
			_cascadeDistanceAndTexelScales[0].Y = 1f;
			_cascadeDistanceAndTexelScales[1].Y = 0.25f;
			_cascadeDistanceAndTexelScales[2].Y = 0.125f;
			break;
		case 4:
			_cascadeSizes[0] = size / 4;
			_cascadeSizes[1] = size;
			_cascadeSizes[2] = (int)((float)size * 2f);
			_cascadeSizes[3] = size * 5;
			_cascadeDistances[1] = (float)size / 4f;
			_cascadeDistances[2] = size;
			_cascadeDistances[3] = (float)size * 2f;
			_cascadeDistances[4] = (float)size * 5f;
			_cascadeDistanceAndTexelScales[0].X = (float)size / 4f;
			_cascadeDistanceAndTexelScales[1].X = size;
			_cascadeDistanceAndTexelScales[2].X = (float)size * 2f;
			_cascadeDistanceAndTexelScales[3].X = (float)size * 5f;
			_cascadeDistanceAndTexelScales[0].Y = 1f;
			_cascadeDistanceAndTexelScales[1].Y = 0.25f;
			_cascadeDistanceAndTexelScales[2].Y = 0.125f;
			_cascadeDistanceAndTexelScales[3].Y = 0.05f;
			break;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool ShouldUpdateCascade(int cascadeId, uint frameId, Vector2 quantifiedCameraMotion)
	{
		bool flag = false;
		bool flag2 = quantifiedCameraMotion.X >= 25f || quantifiedCameraMotion.Y >= (float)System.Math.PI / 12f;
		switch (_sunShadowCascades.Count)
		{
		case 4:
			if (flag2)
			{
				return cascadeId == 0 || (cascadeId == 1 && frameId % 2 == 1) || (cascadeId == 2 && frameId % 2 == 1) || (cascadeId == 3 && frameId % 2 == 0);
			}
			return cascadeId == 0 || (cascadeId == 1 && frameId % 4 != 0) || (cascadeId == 2 && frameId % 2 == 1) || (cascadeId == 3 && frameId % 4 == 0);
		case 3:
			return cascadeId == 0 || (cascadeId == 1 && frameId % 2 == 0) || (cascadeId == 2 && frameId % 2 == 1);
		default:
			return true;
		}
	}

	private void UpdateShadowCascadeFrustums(Vector3 lightDirection, float worldFoV, float aspectRatio, Vector3 cameraPosition, ref Matrix viewMatrix, Vector3 cameraPositionDelta, ref RenderData outRenderData)
	{
		for (int i = 0; i < _sunShadowCascades.Count; i++)
		{
			if (_cascadeNeedsUpdate[i])
			{
				ComputeShadowCascadeData(lightDirection, _cascadeDistances[i], _cascadeDistances[i + 1], worldFoV, aspectRatio, cameraPosition, ref viewMatrix, _renderTargetStore.ShadowMap.Width / _sunShadowCascades.Count, _renderTargetStore.ShadowMap.Height, _sunShadowCascades.UseStableProjection, out outRenderData.VirtualSunPositions[i], out outRenderData.VirtualSunViewRotationMatrix[i], out outRenderData.VirtualSunProjectionMatrix[i], out outRenderData.VirtualSunViewRotationProjectionMatrix[i]);
				_cascadeCachedTranslations[i] = new Vector3(0f);
				_cascadeFrustums[i].Matrix = outRenderData.VirtualSunViewRotationProjectionMatrix[i];
			}
			else
			{
				_cascadeCachedTranslations[i] += cameraPositionDelta;
			}
		}
	}

	private void UpdateGlobalShadowFrustum(Vector3 lightDirection, float worldFoV, float aspectRatio, Vector3 cameraPosition, ref Matrix viewRotationMatrix, out Vector3 virtualLightPosition, out Matrix virtualSunViewFrustumMatrix)
	{
		float farDistance = _cascadeDistances[_sunShadowCascades.Count];
		ComputeShadowCascadeData(lightDirection, _csmInputParams.NearClipDistance, farDistance, worldFoV, aspectRatio, cameraPosition, ref viewRotationMatrix, _renderTargetStore.ShadowMap.Width, _renderTargetStore.ShadowMap.Height, useStableProjection: false, out var virtualLightPosition2, out var _, out var _, out var shadowViewProjectionMatrix);
		virtualLightPosition = virtualLightPosition2;
		virtualSunViewFrustumMatrix = shadowViewProjectionMatrix;
	}

	private static float ComputeDistance(Plane plane, Vector3 point)
	{
		return Vector3.Dot(plane.Normal, point) + plane.D;
	}

	private static void ComputePointAlongRay(Vector3 rayOrigin, Vector3 rayDirection, float distance, out Vector3 point)
	{
		point = rayOrigin + rayDirection * distance;
	}

	public static bool ComputeIntersection(Plane plane, Vector3 rayOrigin, Vector3 rayDirection, out Vector3 intersection)
	{
		bool result = false;
		intersection = Vector3.Zero;
		float num = Vector3.Dot(plane.Normal, rayDirection);
		if (num != 0f)
		{
			float distance = (0f - ComputeDistance(plane, rayOrigin)) / num;
			ComputePointAlongRay(rayOrigin, rayDirection, distance, out intersection);
			result = true;
		}
		return result;
	}

	private void RegisterShadowCastersDrawFunc(Action shadowCastersDrawFunc)
	{
		_shadowCastersDrawFunc = shadowCastersDrawFunc;
	}

	private void UnregisterShadowCastersDrawFunc()
	{
		_shadowCastersDrawFunc = null;
	}

	public void BuildShadowMap()
	{
		GLFunctions gL = _graphics.GL;
		Debug.Assert(_shadowCastersDrawFunc != null, "Did you forget to call RegisterShadowCastersDrawFunc()?");
		gL.AssertEnabled(GL.DEPTH_TEST);
		gL.AssertEnabled(GL.CULL_FACE);
		gL.AssertDepthFunc(GL.LEQUAL);
		gL.AssertDepthMask(write: true);
		gL.DepthFunc(GL.LESS);
		gL.ColorMask(red: false, green: false, blue: false, alpha: false);
		bool flag = !_sunShadowCascades.UseCaching || _csmInputParams.IsSpatialContinuityLost;
		_renderTargetStore.ShadowMap.Bind(flag, setupViewport: false);
		int num = _renderTargetStore.ShadowMap.Width / _sunShadowCascades.Count;
		if (!flag)
		{
			gL.Enable(GL.SCISSOR_TEST);
			int num2 = 1;
			for (int i = 1; i < _sunShadowCascades.Count && _cascadeNeedsUpdate[i]; i++)
			{
				num2++;
			}
			gL.Scissor(0, 0, num * num2, _renderTargetStore.ShadowMap.Height);
			gL.Clear(GL.DEPTH_BUFFER_BIT);
			for (int j = num2 + 1; j < _sunShadowCascades.Count; j++)
			{
				if (_cascadeNeedsUpdate[j])
				{
					gL.Scissor(j * num, 0, num, _renderTargetStore.ShadowMap.Height);
					gL.Clear(GL.DEPTH_BUFFER_BIT);
				}
			}
			gL.Disable(GL.SCISSOR_TEST);
		}
		if (_sunShadowCascades.UseSlopeScaleBias)
		{
			gL.Enable(GL.POLYGON_OFFSET_FILL);
			gL.PolygonOffset(_sunShadowCascades.SlopeScaleBias.X, _sunShadowCascades.SlopeScaleBias.Y);
		}
		_shadowCastersDrawFunc();
		_renderTargetStore.ShadowMap.Unbind();
		gL.DepthFunc(GL.LEQUAL);
		gL.Disable(GL.POLYGON_OFFSET_FILL);
		gL.CullFace(GL.BACK);
		gL.ColorMask(red: true, green: true, blue: true, alpha: true);
		gL.AssertCullFace(GL.BACK);
		gL.AssertDepthFunc(GL.LEQUAL);
		gL.AssertEnabled(GL.DEPTH_TEST);
		gL.AssertDepthMask(write: true);
	}

	public void DrawDeferredShadow(GLBuffer sceneDataBuffer, Vector3[] frustumFarCornersWS)
	{
		GLFunctions gL = _graphics.GL;
		_renderTargetStore.DeferredShadow.Bind(clear: false, setupViewport: true);
		float[] data = new float[4] { 1f, 1f, 1f, 1f };
		gL.ClearBufferfv(GL.COLOR, 0, data);
		DeferredShadowProgram deferredShadowProgram = _gpuProgramStore.DeferredShadowProgram;
		deferredShadowProgram.SceneDataBlock.SetBuffer(sceneDataBuffer);
		gL.UseProgram(deferredShadowProgram);
		gL.ActiveTexture(GL.TEXTURE3);
		gL.BindTexture(GL.TEXTURE_2D, _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Color2));
		gL.ActiveTexture(GL.TEXTURE2);
		gL.BindTexture(GL.TEXTURE_2D, _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Color0));
		gL.ActiveTexture(GL.TEXTURE1);
		gL.BindTexture(GL.TEXTURE_2D, _renderTargetStore.ShadowMap.GetTexture(RenderTarget.Target.Depth));
		gL.ActiveTexture(GL.TEXTURE0);
		if (deferredShadowProgram.UseLinearZ)
		{
			gL.BindTexture(GL.TEXTURE_2D, _renderTargetStore.LinearZ.GetTexture(RenderTarget.Target.Color0));
			deferredShadowProgram.FarCorners.SetValue(frustumFarCornersWS);
		}
		else
		{
			gL.BindTexture(GL.TEXTURE_2D, _renderTargetStore.GBuffer.GetTexture(RenderTarget.Target.Depth));
		}
		_graphics.ScreenTriangleRenderer.Draw();
		_renderTargetStore.DeferredShadow.Unbind();
	}

	public string WriteShadowMappingStateToString()
	{
		string text = "CSM state :";
		text = text + "\n.Deferred res. scale: " + _sunDeferredShadows.ResolutionScale;
		text = text + "\n.Noise: " + _sunDeferredShadows.UseNoise;
		text = text + "\n.Blur: " + _sunDeferredShadows.UseBlur;
		text = text + "\n.Fade: " + _sunDeferredShadows.UseFading;
		text = text + "\n.Stable: " + _sunShadowCascades.UseStableProjection;
		text = text + "\n.Map resolution: " + _graphics.RTStore.ShadowMap.Width + "x" + _graphics.RTStore.ShadowMap.Height;
		string text2 = (_sunDeferredShadows.UseCameraBias ? "camera, " : "");
		text2 += (_sunDeferredShadows.UseNormalBias ? "normal, " : "");
		text2 += (_sunShadowCascades.UseSlopeScaleBias ? ("slope scale " + _sunShadowCascades.SlopeScaleBias.ToString() + ", ") : "");
		text2 += (_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod1 ? "model hack#1, " : "");
		text2 += (_gpuProgramStore.BlockyModelShadowMapProgram.UseBiasMethod2 ? "model hack#2, " : "");
		text = text + "\n.Bias methods: " + text2;
		string text3 = (_sunShadowCascades.UseGlobalKDop ? "global, " : "");
		text3 += ((text3 == "") ? "none" : "");
		text = text + "\n.K-Dop: " + text3;
		text = text + "\n.Caching: " + _sunShadowCascades.UseCaching;
		return text + "\n.Cascade count: " + _sunShadowCascades.Count;
	}

	public void ToggleFreeze()
	{
		_isDebuggingFreeze = !_isDebuggingFreeze;
	}

	public void ToggleCameraFrustumDebug(Vector3 cameraPosition)
	{
		_isDebuggingCameraFrustum = !_isDebuggingCameraFrustum;
		if (_isDebuggingCameraFrustum)
		{
			_debugCameraPosition = cameraPosition;
			BoundingFrustum frustum = new BoundingFrustum(Matrix.Identity);
			float farPlaneDistance = _cascadeDistances[_sunShadowCascades.Count];
			Matrix matrix = Matrix.CreatePerspectiveFieldOfView(_csmInputParams.WorldFieldOfView, _csmInputParams.AspectRatio, _csmInputParams.NearClipDistance, farPlaneDistance);
			Matrix matrix2 = Matrix.Multiply(_csmInputParams.ViewRotationMatrix, matrix);
			frustum.Matrix = matrix2;
			MeshProcessor.CreateFrustum(ref _debugCameraFrustumMesh, ref frustum);
		}
	}

	public void ToggleCameraFrustumSplitsDebug(Vector3 cameraPosition)
	{
		_isDebuggingCameraFrustumSplits = !_isDebuggingCameraFrustumSplits;
		if (_isDebuggingCameraFrustumSplits)
		{
			_debugCameraPosition = cameraPosition;
			BoundingFrustum boundingFrustum = new BoundingFrustum(Matrix.Identity);
			for (int i = 0; i < 4; i++)
			{
				Matrix matrix = Matrix.CreatePerspectiveFieldOfView(_csmInputParams.WorldFieldOfView, _csmInputParams.AspectRatio, _cascadeDistances[i], _cascadeDistances[i + 1]);
				Matrix value = Matrix.Multiply(_csmInputParams.ViewRotationMatrix, matrix);
				boundingFrustum = new BoundingFrustum(value);
				MeshProcessor.CreateFrustum(ref _debugCameraFrustumSplitMeshes[i], ref boundingFrustum);
			}
		}
	}

	public void ToggleShadowCascadeFrustumDebug(Vector3 cameraPosition)
	{
		_isDebuggingShadowCascadeFrustums = !_isDebuggingShadowCascadeFrustums;
		if (_isDebuggingShadowCascadeFrustums)
		{
			_debugCameraPosition = cameraPosition;
			for (int i = 0; i < 4; i++)
			{
				MeshProcessor.CreateFrustum(ref _debugShadowCascadeFrustumMeshes[i], ref _cascadeFrustums[i]);
			}
		}
	}

	public void DebugDrawShadowRelated(ref Matrix viewProjectionMatrix)
	{
		GLFunctions gL = _graphics.GL;
		gL.DepthMask(write: false);
		gL.Enable(GL.DEPTH_TEST);
		gL.Disable(GL.CULL_FACE);
		gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
		gL.BlendEquationSeparate(GL.FUNC_ADD, GL.FUNC_ADD);
		float num = 0.25f;
		BasicProgram basicProgram = _gpuProgramStore.BasicProgram;
		gL.UseProgram(basicProgram);
		Matrix matrix = Matrix.CreateTranslation(_debugCameraPosition);
		matrix = Matrix.Multiply(matrix, viewProjectionMatrix);
		if (_isDebuggingCameraFrustum)
		{
			basicProgram.MVPMatrix.SetValue(ref matrix);
			basicProgram.Opacity.SetValue(num);
			basicProgram.Color.SetValue(new Vector3(1f, 1f, 1f) * num);
			gL.BindVertexArray(_debugCameraFrustumMesh.VertexArray);
			gL.DrawElements(GL.TRIANGLES, _debugCameraFrustumMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
			gL.PolygonMode(GL.FRONT_AND_BACK, GL.LINE);
			basicProgram.Opacity.SetValue(1f);
			gL.DrawElements(GL.TRIANGLES, _debugCameraFrustumMesh.Count, GL.UNSIGNED_SHORT, (IntPtr)0);
			gL.PolygonMode(GL.FRONT_AND_BACK, GL.FILL);
		}
		if (_isDebuggingCameraFrustumSplits)
		{
			for (int i = 0; i < _sunShadowCascades.Count; i++)
			{
				basicProgram.MVPMatrix.SetValue(ref matrix);
				basicProgram.Opacity.SetValue(num);
				basicProgram.Color.SetValue(_debugCascadeColors[i] * num);
				gL.BindVertexArray(_debugCameraFrustumSplitMeshes[i].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _debugCameraFrustumSplitMeshes[i].Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				gL.PolygonMode(GL.FRONT_AND_BACK, GL.LINE);
				basicProgram.Opacity.SetValue(1f);
				basicProgram.Color.SetValue(_debugCascadeColors[i]);
				gL.DrawElements(GL.TRIANGLES, _debugCameraFrustumSplitMeshes[i].Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				gL.PolygonMode(GL.FRONT_AND_BACK, GL.FILL);
			}
		}
		if (_isDebuggingShadowCascadeFrustums)
		{
			for (int j = 0; j < _sunShadowCascades.Count; j++)
			{
				basicProgram.MVPMatrix.SetValue(ref matrix);
				basicProgram.Opacity.SetValue(num);
				basicProgram.Color.SetValue(_debugCascadeColors[j] * num);
				gL.BindVertexArray(_debugShadowCascadeFrustumMeshes[j].VertexArray);
				gL.DrawElements(GL.TRIANGLES, _debugShadowCascadeFrustumMeshes[j].Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				gL.PolygonMode(GL.FRONT_AND_BACK, GL.LINE);
				basicProgram.Opacity.SetValue(1f);
				basicProgram.Color.SetValue(_debugCascadeColors[j]);
				gL.DrawElements(GL.TRIANGLES, _debugShadowCascadeFrustumMeshes[j].Count, GL.UNSIGNED_SHORT, (IntPtr)0);
				gL.PolygonMode(GL.FRONT_AND_BACK, GL.FILL);
			}
		}
		gL.BlendEquationSeparate(GL.FUNC_ADD, GL.FUNC_ADD);
		gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
		gL.Disable(GL.DEPTH_TEST);
		gL.DepthMask(write: true);
	}

	private void InitShadowCascadesDebug()
	{
		_debugCascadeColors[0] = new Vector3(1f, 0f, 0f);
		_debugCascadeColors[1] = new Vector3(0f, 1f, 0f);
		_debugCascadeColors[2] = new Vector3(0f, 0f, 1f);
		_debugCascadeColors[3] = new Vector3(1f, 1f, 0f);
	}

	private void DisposeShadowCascadesDebug()
	{
		if (_isDebuggingCameraFrustum)
		{
			_debugCameraFrustumMesh.Dispose();
		}
		if (_isDebuggingCameraFrustumSplits)
		{
			for (int i = 0; i < 4; i++)
			{
				_debugCameraFrustumSplitMeshes[i].Dispose();
			}
		}
		if (_isDebuggingShadowCascadeFrustums)
		{
			for (int j = 0; j < 4; j++)
			{
				_debugShadowCascadeFrustumMeshes[j].Dispose();
			}
		}
	}
}
