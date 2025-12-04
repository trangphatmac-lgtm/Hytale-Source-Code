#version 330 core

// Global scene data
#include "SceneData_inc.glsl"
#include "ModelVFX_inc.glsl"
#include "Deferred_inc.glsl"

#if USE_DISTORTION_RT
#include "Distortion_inc.glsl"
#endif

// FIXME(blocky-model-shader-workaround):
// When COMPLETE_VERSION is false, a simpler version of the shader, without fog or dynamic lights
// is used as a workaround for some Macs with NVIDIA hardware where we seem to be hitting
// some uniform-related threshold where performance drops significantly
#if !DEFERRED && COMPLETE_VERSION
#if USE_CLUSTERED_LIGHTING
#include "LightCluster_inc.glsl"
#endif //USE_CLUSTERED_LIGHTING

#include "Fog_inc.glsl"
#endif //!DEFERRED && COMPLETE_VERSION

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture0;
uniform sampler2D uTexture1;
uniform sampler2D uTexture2;
uniform sampler2D uGradientAtlasTexture;
uniform sampler2D uNoiseTexture;

uniform vec2 uAtlasSizeFactor0;
uniform vec2 uAtlasSizeFactor1;
uniform vec2 uAtlasSizeFactor2;
uniform float uNearScreendoorThreshold;

#if USE_SCENE_DATA_OVERRIDE
uniform mat4 uViewMatrix;
#define ViewMatrix uViewMatrix
#undef InvViewportSize
#define InvViewportSize vec2(1)
#endif //USE_SCENE_DATA_OVERRIDE

#if USE_DISTORTION_RT
uniform vec2 uCurrentInvViewportSize;
#endif //USE_DISTORTION_RT

#if USE_ENTITY_DATA_BUFFER
#if USE_DISTORTION_RT
uniform vec4 uStaticLightColor = vec4(1.0);
#define staticLightColor uStaticLightColor
#endif
uniform samplerBuffer uModelVFXDataBuffer;
const int MODELVFX_DATA_SIZE = 4;
// MODELVFX_HIGHLIGHT = 0;
// MODELVFX_NOISEPARAMS = 1;
// MODELVFX_POSTCOLOR = 2;
// MODEVFX_PACKEDPARAMS_INVMODELHEIGHT = 3;
#else //USE_ENTITY_DATA_BUFFER

uniform vec4 uStaticLightColor = vec4(1.0);
uniform float uModelVFXAnimationProgress;
uniform vec4 uModelVFXHighlightColorAndThickness;
uniform int uModelVFXPackedParams;
uniform vec4 uModelVFXNoiseParams;
uniform vec4 uModelVFXPostColor;
uniform float uInvModelHeight = 1.0 / (1.80 * 64); // Player Height * DrawScale -- FIXME!
uniform float uUseDithering;
#define staticLightColor uStaticLightColor
#define modelVFXAnimationProgress uModelVFXAnimationProgress
#define modelVFXHighlightColor (uModelVFXHighlightColorAndThickness.rgb)
#define modelVFXHighlightThickness (uModelVFXHighlightColorAndThickness.a)
#define modelVFXNoiseScale (uModelVFXNoiseParams.xy)
#define modelVFXNoiseScrollSpeed (uModelVFXNoiseParams.zw)
#define modelVFXPostColor (uModelVFXPostColor)
#define invModelHeight uInvModelHeight
#define useDithering uUseDithering

#endif //USE_ENTITY_DATA_BUFFER

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec3 fragPositionWS;
in vec2 fragTexCoords;
in vec4 fragGradientColor;
#define HeightFactor fragGradientColor.a

flat in int fragAtlasIndexAndShadingModeAndGradientId; // layout : atlasIndex [8], shadingMode[2], gradientId[8], padding [14]

#if USE_DISTORTION_RT
in vec3 fragPositionVS;
#endif //USE_DISTORTION_RT

#if USE_ENTITY_DATA_BUFFER
#if USE_DISTORTION_RT
flat in vec3 fragInvModelHeightAnimationProgressId;
#define invModelHeight (fragInvModelHeightAnimationProgressId.x)
#define modelVFXAnimationProgress (fragInvModelHeightAnimationProgressId.y)
#define modelVFXId (fragInvModelHeightAnimationProgressId.z)

#else //USE_DISTORTION_RT

flat in vec4 fragStaticLightColor;
#define staticLightColor (fragStaticLightColor)

flat in vec4 fragInvModelHeightAnimationProgressId;
#define invModelHeight (fragInvModelHeightAnimationProgressId.x)
#define modelVFXAnimationProgress (fragInvModelHeightAnimationProgressId.y)
#define modelVFXId (fragInvModelHeightAnimationProgressId.z)
#define useDithering (fragInvModelHeightAnimationProgressId.w)

#endif //USE_DISTORTION_RT
#endif // USE_ENTITY_DATA_BUFFER

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
#if DEFERRED
layout (location = 0) out vec4 outColor0;
layout (location = 1) out vec4 outColor1;
#else //DEFERRED
// Forward or Distortion
layout (location = 0) out vec4 outColor0;
#endif //DEFERRED

//-------------------------------------------------------------------------------------------------------------------------

// Unpack data from the VertexShader
void unpackData(int packedData, out int atlasIndex, out int shadingMode, out int gradientId)
{
    const int maskAtlasIndex = ((1 << 8) - 1);
    const int maskShadingMode = ((1 << 2) - 1);
    const int maskGradientId = ((1 << 8) - 1);

    atlasIndex = packedData & maskAtlasIndex;
    shadingMode = (packedData >> 8) & maskShadingMode;
    gradientId = (packedData >> (8 + 2)) & maskGradientId;
}

#if !USE_DISTORTION_RT
vec3 computeSimpleLightColor(vec3 normalWS, int shadingMode);
vec3 computeFullLightColor(vec3 normalWS, int shadingMode, vec3 positionVS, int pointLightCount, int lightIndex);
#endif //!USE_DISTORTION_RT

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
#if USE_CLUSTERED_LIGHTING
    int lightIndex; 
    int pointLightCount;
    vec4 positionVS = ViewMatrix * vec4(fragPositionWS, 1);
    fetchClusterData(gl_FragCoord.xy * InvViewportSize, -positionVS.z, lightIndex, pointLightCount);
#endif

#if !USE_DISTORTION_RT
    if (useDithering == 1.0 && 0 == mod((gl_FragCoord.x + gl_FragCoord.y), 2.0)) discard;
#endif
    int atlasIndex, shadingMode, gradientId;
    unpackData(fragAtlasIndexAndShadingModeAndGradientId, atlasIndex, shadingMode, gradientId);

    // We can't use a ternary conditional operator inside the texture(...) call here
    // because some drivers will fail to link the program with it
    vec4 texel;
    if (atlasIndex== 0) texel = texture(uTexture0, fragTexCoords);
    else if (atlasIndex== 1) texel = texture(uTexture1, fragTexCoords);
    else texel = texture(uTexture2, fragTexCoords);

    // Discard for fragment too close to the camera or discard if texel is fully transparent 
    // NB : unlike in MapChunkFS.glsl, here we get the threshold from a uniform to get more flexibility, 
    // because this shader is also used for character / item previews, with an ortho projection.
    if (gl_FragCoord.z < uNearScreendoorThreshold || (texel.a == 0.0)) discard;

#if USE_DISTORTION_RT
    // Model VFX
    bool vfxDiscard = false;
    bool hasHighlight = false;
    int modelVFXDirection, modelVFXSwitchTo, modelVFXUseBloomOnHighlight, modelVFXUseProgressiveHighlight;

    if(modelVFXAnimationProgress != 0.0)
    {
#if USE_ENTITY_DATA_BUFFER
    // Get modelVFX data
    vec2 modelVFXNoiseScale, modelVFXNoiseScrollSpeed;

    if(modelVFXId != -1)
    {
        int modelVFXAddress = MODELVFX_DATA_SIZE * int(modelVFXId);

        vec4 noiseParams = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 1);
        float modelVFXPackedParams = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 3).x;

        unpackModelVFXData(int(modelVFXPackedParams), modelVFXDirection, modelVFXSwitchTo, modelVFXUseBloomOnHighlight, modelVFXUseProgressiveHighlight);

         modelVFXNoiseScale = noiseParams.rg;
         modelVFXNoiseScrollSpeed = noiseParams.ba;
    }
#else
        unpackModelVFXData(uModelVFXPackedParams, modelVFXDirection, modelVFXSwitchTo, modelVFXUseBloomOnHighlight, modelVFXUseProgressiveHighlight);
#endif // USE_ENTITY_DATA_BUFFER

        bool useDiscard = true;

        vfxDiscard = ComputeModelVFX(fragTexCoords, uNoiseTexture, invModelHeight, HeightFactor, modelVFXAnimationProgress, int(modelVFXDirection), vec3(1), false, false, 1.0, modelVFXNoiseScale, modelVFXNoiseScrollSpeed, vec4(0), outColor0, hasHighlight, Time);
        if (!vfxDiscard || hasHighlight) discard;

        vec2 distortionScale = vec2(50);
#if FIRST_PERSON_VIEW
        // We need custom parameters for distortion in first person view.
        vec2 uvFactor = distortionScale;
        vec2 vfxUV = fragTexCoords * uvFactor;
        vec2 noise = texture(uNoiseTexture, vfxUV).xy;
        vec2 distortion = noise * 2 - 1;
        distortion = distortion * 0.07;
#else
        vec2 uvFactor = distortionScale * 100 * invModelHeight;
        vec2 vfxUV = fragTexCoords * uvFactor;
        vec2 noise = texture(uNoiseTexture, vfxUV).xy;
        vec2 distortion = noise * 2 - 1;
        distortion = computeDistortion(uCurrentInvViewportSize, fragPositionVS, vec4(distortion, 0, 1), 0.2);
#endif // FIRST_PERSON_VIEW

        outColor0.rg = distortion;
        outColor0.ba = vec2(0,1);
    }
#else //USE_DISTORTION_RT

    outColor0.rgb = texel.rgb;
    outColor0.a = texel.a;

    vec3 gradientColor = fragGradientColor.rgb;

    // Use a gradient to apply color on a greyscale texture.
    bool isGreyscale = (texel.r == texel.g) && (texel.g == texel.b);
    if(gradientId > 0 && isGreyscale)
    {
        ivec2 coord = ivec2(texel.r * 255, gradientId - 1);
        vec3 finalHaircutColor = texelFetch(uGradientAtlasTexture, coord, 0).rgb;
        outColor0.rgb =  finalHaircutColor.rgb;
    }

    vec3 normalWS = fastNormalFromPosition(fragPositionWS);

    // Model VFX
    bool vfxDiscard = false;
    bool hasHighlight = false;
    int modelVFXDirection, modelVFXSwitchTo, modelVFXUseBloomOnHighlight, modelVFXUseProgressiveHighlight;

    if(modelVFXAnimationProgress != 0.0)
    {
#if USE_ENTITY_DATA_BUFFER
        // Get modelVFX data
        vec3 modelVFXHighlightColor;
        float modelVFXHighlightThickness;
        vec2 modelVFXNoiseScale;
        vec2 modelVFXNoiseScrollSpeed;
        vec4 modelVFXPostColor;
        if(modelVFXId != -1)
        {
            int modelVFXAddress = MODELVFX_DATA_SIZE * int(modelVFXId);

            vec4 highlightColorAndThickness = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 0);
            vec4 noiseParams = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 1);
            vec4 postColor = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 2);
            float modelVFXPackedParams = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 3).x;

            unpackModelVFXData(int(modelVFXPackedParams), modelVFXDirection, modelVFXSwitchTo, modelVFXUseBloomOnHighlight, modelVFXUseProgressiveHighlight);

             modelVFXHighlightColor = highlightColorAndThickness.rgb;
             modelVFXHighlightThickness = highlightColorAndThickness.a;
             modelVFXNoiseScale = noiseParams.rg;
             modelVFXNoiseScrollSpeed = noiseParams.ba;
             modelVFXPostColor = postColor;
        }
#else
        unpackModelVFXData(uModelVFXPackedParams, modelVFXDirection, modelVFXSwitchTo, modelVFXUseBloomOnHighlight, modelVFXUseProgressiveHighlight);
#endif // USE_ENTITY_DATA_BUFFER

        const int SWITCH_TO_DISAPPEAR = 0;
        const int SWITCH_TO_POST_COLOR = 1;
        const int SWITCH_TO_DISTORTION = 2;

        bool useDiscard = modelVFXSwitchTo == SWITCH_TO_DISAPPEAR || modelVFXSwitchTo == SWITCH_TO_DISTORTION;
        bool usePostDisappearColor = modelVFXSwitchTo == SWITCH_TO_POST_COLOR;
        bool useBloom = modelVFXUseBloomOnHighlight == 1;
        bool useProgressiveHighlight = modelVFXUseProgressiveHighlight == 1;
        float postColorOpacity = !usePostDisappearColor ? -1.0 : modelVFXPostColor.a;

        vec2 atlasSizeFactor;
        switch (atlasIndex)
        {
            case 0 : atlasSizeFactor = uAtlasSizeFactor0; break;
            case 1 : atlasSizeFactor = uAtlasSizeFactor1; break;
            case 2 : atlasSizeFactor = uAtlasSizeFactor2; break;
        }

        vec2 fxUV = fragTexCoords * atlasSizeFactor;
        vfxDiscard = ComputeModelVFX(fxUV, uNoiseTexture, invModelHeight, HeightFactor, modelVFXAnimationProgress, int(modelVFXDirection), modelVFXHighlightColor, useBloom, useProgressiveHighlight, modelVFXHighlightThickness, modelVFXNoiseScale, modelVFXNoiseScrollSpeed, vec4(modelVFXPostColor.rgb, postColorOpacity), outColor0, hasHighlight, Time);

        if (useDiscard && vfxDiscard) discard;

        // Apply bloom on the highlight.
        shadingMode = hasHighlight ? SHADING_FULLBRIGHT : shadingMode;
    }

#if DEFERRED
    gradientColor.xyz = vec3(1.0) - ((vec3(1.0) - outColor0.rgb) * (vec3(1.0) - gradientColor.xyz));
    outColor0.rgb = gradientColor * 0.5 + outColor0.rgb * (1.0 - 0.5);

    // Write surface similarity 
    //outColor0.a = 25000.0 * fwidth(gl_FragCoord.z);

    // Use whatever RGB compression was selected inside this function
    outColor0.rg = encodeAlbedoCompressed(outColor0.rgb, ivec2(gl_FragCoord.xy));

    // Encode the normal in the .BA channels
    outColor0.ba = encodeNormal(normalWS);

    outColor1.rgb = computeSimpleLightColor(normalWS, shadingMode);

    // This prevents ssao to be used on this pixel
    bool hasSSAO = shadingMode != SHADING_FULLBRIGHT;
    bool hasBloom = shadingMode == SHADING_FULLBRIGHT;
    bool useCleanShadowBackfaces = false;
    outColor1.a = packFragBits(hasSSAO, hasBloom, useCleanShadowBackfaces);

#if USE_LBUFFER_COMPRESSION
    // Write surface similarity
    //float similarity = 25000.0 * fwidth(gl_FragCoord.z);
    outColor1.rgba = vec4(encodeLightCompressed(min(vec3(1), outColor1.rgb), ivec2(gl_FragCoord.xy)), staticLightColor.a, outColor1.a);
#endif

#else // DEFERRED

#if COMPLETE_VERSION
    if (shadingMode != SHADING_FULLBRIGHT)
    {
        vec3 light = computeFullLightColor(normalWS, shadingMode, positionVS.xyz, pointLightCount, lightIndex);

        gradientColor.xyz = vec3(1.0) - ((vec3(1.0) - outColor0.rgb) * (vec3(1.0) - gradientColor.xyz));

        outColor0.rgb *= light;

        gradientColor = gradientColor * light * 0.9 + gradientColor * (1.0 - 0.9);
        outColor0.rgb = gradientColor * 0.5 + outColor0.rgb * (1.0 - 0.5);
    }

    outColor0.rgb = applyDistantFog(fragPositionWS, outColor0.rgb, FogBackColor.rgb, FogParams);
#else
    vec3 light = computeSimpleLightColor(normalWS, shadingMode);
    outColor0.rgb *= light;

    // NOTE: The check on shadingMode makes no sense and is not needed, it's just to make the shader compile with this workaround version
    if (texel.a == 0.0 && shadingMode >= 0) discard;
#endif // COMPLETE_VERSION

#endif // DEFERRED
#endif //USE_DISTORTION_RT

    // Screendoor Transparency
    //if( 1 == mod(gl_FragCoord.x + gl_FragCoord.y,2.0))discard;
}

#if !USE_DISTORTION_RT
vec3 computeSimpleLightColor(vec3 normalWS, int shadingMode)
{
    vec3 lightColor;

    // shadingMode can go from 0 to 3. The values are defined in "HytaleClient/Graphics/ShadingMode.cs"
    switch (shadingMode)
    {
        case SHADING_FLAT:
            lightColor = staticLightColor.rgb;
            break;

        case SHADING_FULLBRIGHT:
            lightColor = vec3(1.0f);
            break;

        case SHADING_REFLECTIVE:
        default:
            float lightExpositionVertical = 0.4 * clamp(normalWS.y, 0.0, 1.0);
            float lightExpositionHorizontal = 0.2 * abs(normalWS.z);
            lightColor = staticLightColor.rgb * (0.6 + max(lightExpositionVertical, lightExpositionHorizontal));
            break;
    }

    return lightColor;
}

vec3 computeFullLightColor(vec3 normalWS, int shadingMode, vec3 positionVS, int pointLightCount, int lightIndex)
{
    vec3 lightColor = computeSimpleLightColor(normalWS, shadingMode);

#if USE_CLUSTERED_LIGHTING
    const float EarlyOutThreshold = 1.0;
    vec3 dynamicLightColor = computeClusteredLighting(positionVS.xyz, pointLightCount, lightIndex, EarlyOutThreshold);

    lightColor = max(dynamicLightColor, lightColor.rgb);
#endif //USE_CLUSTERED_LIGHTING

    return lightColor;
}
#endif //!USE_DISTORTION_RT

