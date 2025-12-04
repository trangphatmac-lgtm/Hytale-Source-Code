using System.IO;
using System.Reflection;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics.Programs;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Graphics;

public class GPUProgramStore
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	internal readonly BasicProgram BasicProgram;

	internal readonly BasicFogProgram BasicFogProgram;

	internal readonly BlockyModelProgram BlockyModelForwardProgram;

	internal readonly BlockyModelProgram BlockyModelDitheringProgram;

	internal readonly BlockyModelProgram BlockyModelProgram;

	internal readonly BlockyModelProgram FirstPersonBlockyModelProgram;

	internal readonly BlockyModelProgram BlockyModelDistortionProgram;

	internal readonly BlockyModelProgram FirstPersonDistortionBlockyModelProgram;

	internal readonly BlockyModelProgram FirstPersonClippingBlockyModelProgram;

	internal readonly MapChunkNearProgram MapChunkNearOpaqueProgram;

	internal readonly MapChunkNearProgram MapChunkNearAlphaTestedProgram;

	internal readonly MapChunkFarProgram MapChunkFarOpaqueProgram;

	internal readonly MapChunkFarProgram MapChunkFarAlphaTestedProgram;

	internal readonly MapChunkAlphaBlendedProgram MapChunkAlphaBlendedProgram;

	internal readonly MapBlockAnimatedProgram MapBlockAnimatedProgram;

	internal readonly MapBlockAnimatedProgram MapBlockAnimatedForwardProgram;

	internal readonly SkyProgram SkyProgram;

	internal readonly CloudsProgram CloudsProgram;

	internal readonly TextProgram TextProgram;

	internal readonly ParticleProgram ParticleProgram;

	internal readonly ParticleProgram ParticleErosionProgram;

	internal readonly ParticleProgram ParticleDistortionProgram;

	internal readonly ForceFieldProgram ForceFieldProgram;

	internal readonly ForceFieldProgram BuilderToolProgram;

	internal readonly WorldMapProgram WorldMapProgram;

	internal readonly PostEffectProgram PostEffectProgram;

	internal readonly PostEffectProgram InventoryPostEffectProgram;

	internal readonly PostEffectProgram MainMenuPostEffectProgram;

	internal readonly TemporalAAProgram TemporalAAProgram;

	internal readonly OITCompositeProgram OITCompositeProgram;

	internal readonly CubemapProgram CubemapProgram;

	internal readonly ZDownsampleProgram ZDownsampleProgram;

	internal readonly ZDownsampleProgram LinearZDownsampleProgram;

	internal readonly EdgeDetectionProgram EdgeDetectionProgram;

	internal readonly LinearZProgram LinearZProgram;

	internal readonly LightMixProgram LightMixProgram;

	internal readonly LightProgram LightProgram;

	internal readonly LightProgram LightLowResProgram;

	internal readonly LightClusteredProgram LightClusteredProgram;

	internal readonly ZOnlyChunkProgram MapChunkShadowMapProgram;

	internal readonly ZOnlyChunkProgram MapBlockAnimatedShadowMapProgram;

	internal readonly ZOnlyBlockyModelProgram BlockyModelShadowMapProgram;

	internal readonly ZOnlyBlockyModelProgram BlockyModelOcclusionMapProgram;

	internal readonly DeferredShadowProgram DeferredShadowProgram;

	internal readonly VolumetricSunshaftProgram VolumetricSunshaftProgram;

	internal readonly SSAOProgram SSAOProgram;

	internal readonly BlurProgram BlurSSAOAndShadowProgram;

	internal readonly DeferredProgram DeferredProgram;

	internal readonly ScreenBlitProgram ScreenBlitProgram;

	internal readonly BlurProgram BlurProgram;

	internal readonly DoFBlurProgram DoFBlurProgram;

	internal readonly DoFCircleOfConfusionProgram DoFCircleOfConfusionProgram;

	internal readonly DoFDownsampleProgram DoFDownsampleProgram;

	internal readonly MaxProgram DoFNearCoCMaxProgram;

	internal readonly DoFNearCoCBlurProgram DoFNearCoCBlurProgram;

	internal readonly DepthOfFieldAdvancedProgram DepthOfFieldAdvancedProgram;

	internal readonly DoFFillProgram DoFFillProgram;

	internal readonly BloomSelectProgram BloomSelectProgram;

	internal readonly BloomDownsampleBlurProgram BloomDownsampleBlurProgram;

	internal readonly BloomUpsampleBlurProgram BloomUpsampleBlurProgram;

	internal readonly MaxProgram BloomMaxProgram;

	internal readonly BloomCompositeProgram BloomCompositeProgram;

	internal readonly RadialGlowMaskProgram RadialGlowMaskProgram;

	internal readonly RadialGlowLuminanceProgram RadialGlowLuminanceProgram;

	internal readonly SunOcclusionDownsampleProgram SunOcclusionDownsampleProgram;

	internal readonly SceneBrightnessPackProgram SceneBrightnessPackProgram;

	internal readonly ZOnlyChunkProgram ZOnlyMapChunkProgram;

	internal readonly ZOnlyChunkPlanesProgram ZOnlyMapChunkPlanesProgram;

	internal readonly ZOnlyProgram ZOnlyProgram;

	internal readonly HiZReprojectProgram HiZReprojectProgram;

	internal readonly HiZFillHoleProgram HiZFillHoleProgram;

	internal readonly HiZBuildProgram HiZBuildProgram;

	internal readonly HiZCullProgram HiZCullProgram;

	internal readonly DebugDrawMapProgram DebugDrawMapProgram;

	internal readonly Batcher2DProgram Batcher2DProgram;

	private GraphicsDevice _graphicsDevice;

	public GPUProgramStore(GraphicsDevice graphicsDevice)
	{
		_graphicsDevice = graphicsDevice;
		GPUProgram.CreateFallbacks();
		GPUProgram.SetShaderCodeDumpPolicy(GPUProgram.ShaderCodeDumpPolicy.OnError);
		GPUProgram.SetResourcePaths("HytaleClient.Graphics.Shaders", Path.Combine(Paths.App, "..", "..", "..", "..", "HytaleClient", "Graphics", "Shaders"), Path.Combine(Paths.UserData, "Shaders"));
		int maxNodeCount = BlockyModel.MaxNodeCount;
		BasicProgram = new BasicProgram(writeAlphaChannel: true, "BasicProgram");
		BasicFogProgram = new BasicFogProgram();
		BlockyModelForwardProgram = new BlockyModelProgram(useDeferred: false, useSceneDataOverride: true, useCompleteForwardVersion: false, firstPersonView: false, useEntityDataBuffer: false, useDistortionRT: false, "BlockyModelForwardProgram");
		BlockyModelDitheringProgram = new BlockyModelProgram(useDeferred: false, useSceneDataOverride: false, !_graphicsDevice.UseDeferredLight, firstPersonView: false, useEntityDataBuffer: true, useDistortionRT: false, "BlockyModelDitheringProgram");
		BlockyModelProgram = new BlockyModelProgram(_graphicsDevice.UseDeferredLight, useSceneDataOverride: false, !_graphicsDevice.UseDeferredLight, firstPersonView: false, useEntityDataBuffer: true, useDistortionRT: false, "BlockyModelProgram");
		FirstPersonBlockyModelProgram = new BlockyModelProgram(_graphicsDevice.UseDeferredLight, useSceneDataOverride: false, !_graphicsDevice.UseDeferredLight, firstPersonView: true, useEntityDataBuffer: false, useDistortionRT: false, "FirstPersonBlockyModelProgram");
		FirstPersonClippingBlockyModelProgram = new BlockyModelProgram(_graphicsDevice.UseDeferredLight, useSceneDataOverride: false, !_graphicsDevice.UseDeferredLight, firstPersonView: false, useEntityDataBuffer: false, useDistortionRT: false, "FirstPersonClippingBlockyModelProgram");
		BlockyModelDistortionProgram = new BlockyModelProgram(useDeferred: false, useSceneDataOverride: false, !_graphicsDevice.UseDeferredLight, firstPersonView: false, useEntityDataBuffer: true, useDistortionRT: true, "BlockyModelDistortionProgram");
		FirstPersonDistortionBlockyModelProgram = new BlockyModelProgram(useDeferred: false, useSceneDataOverride: false, !_graphicsDevice.UseDeferredLight, firstPersonView: true, useEntityDataBuffer: false, useDistortionRT: true, "FirstPersonDistortionBlockyModelProgram");
		MapChunkNearOpaqueProgram = new MapChunkNearProgram(alphaTest: false, _graphicsDevice.UseDeferredLight, useLOD: false, "MapChunkNearOpaqueProgram");
		MapChunkNearAlphaTestedProgram = new MapChunkNearProgram(alphaTest: true, _graphicsDevice.UseDeferredLight, useLOD: true, "MapChunkNearAlphaTestedProgram");
		MapChunkFarOpaqueProgram = new MapChunkFarProgram(alphaTest: false, _graphicsDevice.UseDeferredLight, useLOD: false, "MapChunkFarOpaqueProgram");
		MapChunkFarAlphaTestedProgram = new MapChunkFarProgram(alphaTest: true, _graphicsDevice.UseDeferredLight, useLOD: true, "MapChunkFarAlphaTestedProgram");
		MapChunkAlphaBlendedProgram = new MapChunkAlphaBlendedProgram(useForwardSunShadows: false, "MapChunkAlphaBlendedProgram");
		MapBlockAnimatedProgram = new MapBlockAnimatedProgram(maxNodeCount, _graphicsDevice.UseDeferredLight, useSceneDataOverride: false, writeRenderConfigBitsInAlpha: true, "MapBlockAnimatedProgram");
		MapBlockAnimatedForwardProgram = new MapBlockAnimatedProgram(maxNodeCount, useDeferred: false, useSceneDataOverride: true, writeRenderConfigBitsInAlpha: false, "MapBlockAnimatedForwardProgram");
		SkyProgram = new SkyProgram();
		CloudsProgram = new CloudsProgram();
		TextProgram = new TextProgram();
		ParticleProgram = new ParticleProgram();
		ParticleErosionProgram = new ParticleProgram(useForwardClusteredLighting: true, useLightDirectAccess: true, useCustomZDistribution: true, useSunShadows: true, useDistortionRT: false, useErosion: true, "ParticleErosionProgram");
		ParticleDistortionProgram = new ParticleProgram(useForwardClusteredLighting: false, useLightDirectAccess: false, useCustomZDistribution: false, useSunShadows: false, useDistortionRT: true, useErosion: false, "ParticleDistortionProgram");
		ForceFieldProgram = new ForceFieldProgram();
		BuilderToolProgram = new ForceFieldProgram(discardFaceHighlight: true, useUndergroundColor: true, "BuilderToolProgram");
		WorldMapProgram = new WorldMapProgram();
		PostEffectProgram = new PostEffectProgram(_graphicsDevice.UseReverseZ, useAntiAliasingHighQuality: false, discardDark: false, "PostEffectProgram");
		InventoryPostEffectProgram = new PostEffectProgram(_graphicsDevice.UseReverseZ, useAntiAliasingHighQuality: true, discardDark: true, "InventoryPostEffectProgram");
		MainMenuPostEffectProgram = new PostEffectProgram(_graphicsDevice.UseReverseZ, useAntiAliasingHighQuality: true, discardDark: false, "MainMenuPostEffectProgram");
		TemporalAAProgram = new TemporalAAProgram();
		OITCompositeProgram = new OITCompositeProgram();
		CubemapProgram = new CubemapProgram();
		ZDownsampleProgram = new ZDownsampleProgram(writeToColor: false, writeToDepth: true, useLinearZ: false, "ZDownsampleProgram");
		LinearZDownsampleProgram = new ZDownsampleProgram(writeToColor: true, writeToDepth: false, useLinearZ: true, "LinearZDownsampleProgram");
		EdgeDetectionProgram = new EdgeDetectionProgram();
		LinearZProgram = new LinearZProgram();
		LightMixProgram = new LightMixProgram();
		LightProgram = new LightProgram(256);
		LightLowResProgram = new LightProgram(256);
		LightClusteredProgram = new LightClusteredProgram();
		MapChunkShadowMapProgram = new ZOnlyChunkProgram(buildShadowMaps: true, animated: false, 0, alphaTest: true, useCompressedPosition: true, useFoliageCulling: true, 0f, "MapChunkShadowMapProgram");
		MapBlockAnimatedShadowMapProgram = new ZOnlyChunkProgram(buildShadowMaps: true, animated: true, maxNodeCount, alphaTest: true, useCompressedPosition: true, useFoliageCulling: true, 0f, "MapBlockAnimatedShadowMapProgram");
		BlockyModelShadowMapProgram = new ZOnlyBlockyModelProgram(BlockyModelProgram, useModelVFX: true);
		BlockyModelOcclusionMapProgram = new ZOnlyBlockyModelProgram(BlockyModelProgram, useModelVFX: true);
		DeferredShadowProgram = new DeferredShadowProgram();
		VolumetricSunshaftProgram = new VolumetricSunshaftProgram();
		SSAOProgram = new SSAOProgram();
		BlurSSAOAndShadowProgram = new BlurProgram(blurCustomChannels: true, "ra", "gb", "BlurSSAOAndShadowProgram");
		DeferredProgram = new DeferredProgram(_graphicsDevice.UseReverseZ, _graphicsDevice.UseDownsampledZ, useDeferredFog: true, _graphicsDevice.UseDeferredLight, _graphicsDevice.UseLowResDeferredLighting, useSSAO: true);
		ScreenBlitProgram = new ScreenBlitProgram();
		BlurProgram = new BlurProgram(blurCustomChannels: false, "rgba", "", "BlurProgram");
		BlurProgram.UseEdgeAwareness = false;
		DoFBlurProgram = new DoFBlurProgram();
		DoFCircleOfConfusionProgram = new DoFCircleOfConfusionProgram();
		DoFDownsampleProgram = new DoFDownsampleProgram();
		DoFNearCoCMaxProgram = new MaxProgram(useVec3: false, 6, "DoFNearCoCMaxProgram");
		DoFNearCoCBlurProgram = new DoFNearCoCBlurProgram();
		DepthOfFieldAdvancedProgram = new DepthOfFieldAdvancedProgram();
		DoFFillProgram = new DoFFillProgram();
		BloomSelectProgram = new BloomSelectProgram();
		BloomDownsampleBlurProgram = new BloomDownsampleBlurProgram();
		BloomUpsampleBlurProgram = new BloomUpsampleBlurProgram();
		BloomMaxProgram = new MaxProgram(useVec3: true, 1, "BloomMaxProgram");
		BloomCompositeProgram = new BloomCompositeProgram();
		RadialGlowMaskProgram = new RadialGlowMaskProgram();
		RadialGlowLuminanceProgram = new RadialGlowLuminanceProgram(8);
		SunOcclusionDownsampleProgram = new SunOcclusionDownsampleProgram();
		SceneBrightnessPackProgram = new SceneBrightnessPackProgram();
		ZOnlyMapChunkProgram = new ZOnlyChunkProgram(buildShadowMaps: false, animated: false, 0, alphaTest: true, useCompressedPosition: true, useFoliageCulling: false, -2f, "ZOnlyMapChunkProgram");
		ZOnlyMapChunkPlanesProgram = new ZOnlyChunkPlanesProgram();
		ZOnlyProgram = new ZOnlyProgram(alphaTest: false, "ZOnlyProgram");
		HiZReprojectProgram = new HiZReprojectProgram();
		HiZFillHoleProgram = new HiZFillHoleProgram();
		HiZBuildProgram = new HiZBuildProgram();
		HiZCullProgram = new HiZCullProgram();
		DebugDrawMapProgram = new DebugDrawMapProgram();
		Batcher2DProgram = new Batcher2DProgram();
		InitializeAllPrograms();
	}

	public void Release()
	{
		ReleaseAllPrograms();
		GPUProgram.DestroyFallbacks();
	}

	private void InitializeAllPrograms(bool releaseFirst = false, bool forceReset = false)
	{
		string text = "";
		FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType.IsSubclassOf(typeof(GPUProgram)))
			{
				GPUProgram gPUProgram = fieldInfo.GetValue(this) as GPUProgram;
				if (!((!releaseFirst) ? gPUProgram.Initialize() : gPUProgram.Reset(forceReset)))
				{
					text = text + fieldInfo.Name + "\n";
				}
			}
		}
		if (text.Length != 0)
		{
			string text2 = "Summary : Errors encountered during the building of GPU Programs :\n" + text + "...(see details above).";
			Logger.Error(text2);
		}
		else
		{
			Logger.Info("Summary : all GPU Programs were built successfully!");
		}
	}

	private void ReleaseAllPrograms(bool releaseFirst = false)
	{
		FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType.IsSubclassOf(typeof(GPUProgram)))
			{
				GPUProgram gPUProgram = fieldInfo.GetValue(this) as GPUProgram;
				gPUProgram.Release();
			}
		}
	}

	public void ResetPrograms(bool forceReset)
	{
		PostEffectProgram.ReverseZ = _graphicsDevice.UseReverseZ;
		MainMenuPostEffectProgram.ReverseZ = _graphicsDevice.UseReverseZ;
		BlockyModelProgram.Deferred = _graphicsDevice.UseDeferredLight;
		MapChunkNearOpaqueProgram.Deferred = _graphicsDevice.UseDeferredLight;
		MapChunkFarOpaqueProgram.Deferred = _graphicsDevice.UseDeferredLight;
		MapBlockAnimatedProgram.Deferred = _graphicsDevice.UseDeferredLight;
		DeferredProgram.UseLight = _graphicsDevice.UseDeferredLight;
		DeferredProgram.UseDownsampledZ = _graphicsDevice.UseDownsampledZ;
		DeferredProgram.UseLowResLighting = _graphicsDevice.UseLowResDeferredLighting;
		DeferredProgram.UseLinearZ = _graphicsDevice.UseLinearZ;
		LightProgram.UseLinearZ = _graphicsDevice.UseLinearZForLight;
		LightLowResProgram.UseLinearZ = _graphicsDevice.UseLinearZForLight;
		LightClusteredProgram.UseLinearZ = _graphicsDevice.UseLinearZForLight;
		InitializeAllPrograms(releaseFirst: true, forceReset);
	}

	public void ResetProgramUniforms()
	{
		FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType.IsSubclassOf(typeof(GPUProgram)))
			{
				GPUProgram gPUProgram = fieldInfo.GetValue(this) as GPUProgram;
				gPUProgram.ResetUniforms();
			}
		}
	}
}
