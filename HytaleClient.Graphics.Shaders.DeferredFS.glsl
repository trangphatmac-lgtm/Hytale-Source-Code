#version 330 core

// Global scene data
#include "SceneData_inc.glsl"
#include "Deferred_inc.glsl"
#include "Reconstruction_inc.glsl"
#include "Fog_inc.glsl"
#include "Shading_inc.glsl"

#if DEBUG_SHADOW_CASCADES
// Requires data from SceneData_inc.glsl.
#include "ShadowMap_inc.glsl"           
#endif

// Careful : w/ those GLSL generated defines, we must use #ifdef and not #if !
#if USE_UNDERWATER_CAUSTICS || USE_CLOUDS_SHADOWS
#define USE_PROJECTED_ENV_TEXTURE 1
#endif

#ifndef INPUT_NORMALS_IN_WS
#define INPUT_NORMALS_IN_WS 1
#endif

#define USE_CHROMA_SUBSAMPLING 1

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;
uniform sampler2D uDepthTexture;
////uniform sampler2D uLowResDepthTexture;
uniform sampler2D uSSAOTexture;
uniform sampler2D uLightTexture;
////uniform sampler2D uPointLightTexture;
uniform sampler2D uTopDownProjectionTexture;
uniform sampler2D uShadowTexture;

#if USE_FOG
uniform sampler2D uFogNoiseTexture;
#endif

#if DEBUG_SHADOW_CASCADES
// MAX_CASCADES is defined in "ShadowMap_inc.glsl"
uniform mat4 uDebugShadowMatrix[MAX_CASCADES];
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec3 fragFrustumRay;
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void getLightColor(vec2 texCoords, out vec3 light, out float sunOcclusion, out float configBits);
float getShadowFactor(vec2 texCoords);
float getSSAOValue(vec2 texCoords);
vec3 getAlbedo(vec3 gbufferContent, vec2 texCoords);
vec3 computePositionFromCameraWS(float depth, vec2 texCoords);
vec4 debugPixelData(bool hasSSAO, bool hasBloom, bool useCleanShadowBackfaces, float finalAo, vec3 finalAmbientColor, vec3 finalLightColor);

float remap(float start, float end, float x);

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec4 color;
    vec2 texCoords = fragTexCoords;

    float depth = texture(uDepthTexture, texCoords).r;

    const float DEPTH_FAR_THRESHOLD = 1.0;

   // Avoid to apply fog on background objects like sky, clouds, sun & moon    
    if (depth < DEPTH_FAR_THRESHOLD)
    {
        //===================================================================================================================
        // Section 1. Texture reads are done as early as possible
        // ... and using the read values postponed to as late as possible.

        vec4 gbufferContent = texture(uColorTexture, texCoords);

        vec3 light;
        float sunOcclusion;
        float configBits;
        getLightColor(texCoords, light, sunOcclusion, configBits);
        float ao = getSSAOValue(texCoords);
        float shadow = getShadowFactor(texCoords);

        //===================================================================================================================
        // Section 2. Compute stuff w/ the data already available (i.e. not the read values)
        vec3 positionFromCameraWS = computePositionFromCameraWS(depth, texCoords);
        vec3 positionWS = positionFromCameraWS + CameraPosition;

        //===================================================================================================================
        // Section 3. Use the read values (and read some more if needed)
        vec3 albedo = getAlbedo(gbufferContent.rgb, texCoords);

        bool hasSSAO, hasBloom, useCleanShadowBackfaces;
        unpackFragBits(configBits, hasSSAO, hasBloom, useCleanShadowBackfaces);

#if USE_SKY_AMBIENT
        vec3 normal = decodeNormal(gbufferContent.ba);

#if INPUT_NORMALS_IN_WS
        vec3 ambientColor = computeSkyAmbient(normal, SunPositionWS, AmbientBackColor, AmbientFrontColor, AmbientIntensity, sunOcclusion);
#else
        vec3 ambientColor = computeSkyAmbient(normal, SunPositionVS, AmbientBackColor, AmbientFrontColor, AmbientIntensity, sunOcclusion);
#endif

        //bool caveHint = length(light.rgb) < 0.01;
        light.rgb += ambientColor.rgb;
#endif //USE_SKY_AMBIENT

#ifdef USE_PROJECTED_ENV_TEXTURE

        const int PROJECT_NOTHING = 0;
        const int PROJECT_CAUSTICS = 1;
        const int PROJECT_CLOUDS_SHADOWS = 2;

    #if USE_UNDERWATER_CAUSTICS && USE_CLOUDS_SHADOWS
        int projectionType = (IsCameraUnderwater == 1.0) ? PROJECT_CAUSTICS : PROJECT_CLOUDS_SHADOWS;
    #endif // CAUSTICS & CLOUDS SHADOWS
    #if USE_UNDERWATER_CAUSTICS && !USE_CLOUDS_SHADOWS
        int projectionType = (IsCameraUnderwater == 1.0) ? PROJECT_CAUSTICS : PROJECT_NOTHING;
    #endif // CAUSTICS
    #if !USE_UNDERWATER_CAUSTICS && USE_CLOUDS_SHADOWS
        int projectionType = (IsCameraUnderwater == 1.0) ? PROJECT_NOTHING : PROJECT_CLOUDS_SHADOWS;
    #endif // CLOUDS SHADOWS

        switch (projectionType)
        {
        case PROJECT_CAUSTICS:

            const float CausticsFalloff = 0.015;
            light.rgb += vec3(computeCaustics(uTopDownProjectionTexture, WaterCausticsIntensity, WaterCausticsScale, WaterCausticsDistortion,
                                                    positionWS.xz * WaterCausticsScale, vec2(WaterCausticsAnimTime), positionWS, positionFromCameraWS,
                                                    length(positionFromCameraWS), CausticsFalloff) * sunOcclusion);
            break;
        case PROJECT_CLOUDS_SHADOWS:
            // We can either use the same uv offset as the CloudsFS.glsl to have some kind of sync, or use our own

            // Moves the UV so that the speed & direction of the shadows match the clouds in the sky
            float projected = computeCloudsShadows(uTopDownProjectionTexture, positionWS.xz * CloudsShadowScale, vec2(CloudsShadowAnimTime), CloudsShadowIntensity, CloudsShadowBlurriness);
            projected = hasBloom ? 0 : projected * sunOcclusion;

            // Removes shadows darkness
            // line below is <=> but faster (1 MADD) to light.rgb = light.rgb * (1.0 - projected);
            light.rgb = -light.rgb * projected + light.rgb;
            break;

        default: break;
        }
#endif

#if USE_SSAO
#if USE_CHROMA_SUBSAMPLING
        // Since the GBuffer.RG channels contain the Y{Co/Cg}, use the luminance Y as is !
        float brightness = gbufferContent.r;
#else
        const vec3 BrightnessCoefficients = vec3(0.299, 0.587, 0.114);
        float brightness = dot(BrightnessCoefficients, albedo.rgb);
#endif

        // avoids clamping down the brightness to avoid having ~no AO visible on white surfaces
        brightness = min(0.5, brightness);

        // Ambient occlusion is following these rules :
        // 1. no AO on surfaces tagged "no AO" (e.g. grass)
        // 2. make AO lighter on brighter surces (hence the "brightness" in the equation)
        // 3. make AO stronger in indoors (hence the "sunOcclusion" in the equation)
        //     - this is meant to add some shadow from AO under the entities,
        //       as shadowmap won't produce visible shadows for entities indoor because it's merged with the world geometry shadow.
        ao = !hasSSAO ? 1.0 : pow(ao, (2.5 - sunOcclusion) * (1 - brightness));
//        ao = !hasSSAO ? 1.0 : pow(ao, 1.0 - (brightness * brightness * brightness));

        light.rgb *= ao;
#endif //USE_SSAO

#if USE_DEFERRED_SHADOW_INDOOR_FADING
        // Fade out the shadow in caves / indoor...
        // - w/o this, when chunks cast shadows, caves become super dark
        // - w/ this, when the chunks do not cast shadows, we lose the entities shadow.
        const float minSunOcclusion = 0.1;
        float sunRatio = minSunOcclusion/sunOcclusion;
        shadow = clamp(mix(shadow, 1.0, sunRatio), 0, 1);
#endif //USE_DEFERRED_SHADOW_INDOOR_FADING

        // Avoid shadow on emissive material.
        shadow = hasBloom ? 1.0 : shadow;
        color.rgb = albedo.rgb * light.rgb * shadow;

#if USE_FOG
        vec2 ditherNoiseSeed = fragTexCoords;
        vec4 fog = computeFog(positionFromCameraWS, CameraPosition, SunPositionWS,
                                FogTopColor.rgb, FogFrontColor.rgb, FogBackColor.rgb, FogParams, FogMoodParamsA, FogHeightDensityAtViewer,
                                ditherNoiseSeed, uFogNoiseTexture, SceneBrightness.r);
        
        color.rgb = mix(color.rgb, fog.rgb, fog.a);
#endif // USE_FOG

        // Transfer configbits in channel A - make sure this channel is protected for writing in the next render passes, or just be careful.
        color.a = configBits;
        
        outColor = color;
//        outColor.rgb = light;

#if DEBUG_PIXELS
        outColor = debugPixelData(hasSSAO, hasBloom, useCleanShadowBackfaces, ao, ambientColor.rgb, light.rgb);
#endif //DEBUG_PIXELS

#if DEBUG_SHADOW_CASCADES
        vec4 positionLS;
        int cascadeId = getCascadeId(positionFromCameraWS, positionLS);
        
        vec3 debugCascadeColors[4] = vec3[4](vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1), vec3(1, 1, 0));
        outColor.rgb = mix(outColor.rgb, debugCascadeColors[cascadeId], 0.5);
#endif //DEBUG_SHADOW_CASCADES
    }
    else
    {
        discard;
    }
}

void getLightColor(vec2 texCoords, out vec3 light, out float sunOcclusion, out float configBits)
{
#if USE_LIGHT
    vec4 color = texture(uLightTexture, texCoords);

#if USE_LBUFFER_COMPRESSION
    // Use whatever RGB decompression was selected inside this function
    light.rgb = decodeCompressedLight(color.rg, uLightTexture, texCoords, ivec2(gl_FragCoord.xy));
#else
    light.rgb = color.rgb;
#endif
    sunOcclusion = color.b;
    configBits = color.a;

#else
    light = vec3(1.0);
    sunOcclusion = 1;
    configBits = 1;
#endif
}

float getShadowFactor(vec2 texCoords)
{
#if USE_DEFERRED_SHADOW
#if USE_DEFERRED_SHADOW_BLURRED && USE_SSAO
    float shadow = texture(uSSAOTexture, texCoords).a;
#else
    float shadow = texture(uShadowTexture, texCoords).r;
#endif // USE_DEFERRED_SHADOW_BLURRED
#else
    float shadow = 1;
#endif

    return shadow;
}

float getSSAOValue(vec2 texCoords)
{
#if USE_SSAO
#if USE_EDGE_AWARE_UPSAMPLING
    float totalWeight = 0;
    float resultValue = 0;
    const float depth_epsilon = 0.0001;

    for (int x = 0; x <= 1; x++)
    {
        for (int y = 0; y <= 1; y++)
        {
            vec4 value = textureOffset(uSSAOTexture, texCoords, ivec2(x, y));

            float lowResDepth = unpackDepth(value.gb);
            float weight = 1.0f / (depth_epsilon + abs (depth - lowResDepth) );

            resultValue += value.r * weight;
            totalWeight += weight;
        }
    }
    float ao = resultValue / totalWeight;
#else
    float ao = texture(uSSAOTexture, texCoords).r;
#endif // USE_EDGE_AWARE_UPSAMPLING
#else
    float ao = 1.0f;
#endif // SSAO

    return ao;
}

vec3 getAlbedo(vec3 gbufferContent, vec2 texCoords)
{
#if USE_CHROMA_SUBSAMPLING
    // Use whatever RGB decompression was selected inside this function
    vec3 albedo = decodeCompressedAlbedo(gbufferContent.rg, uColorTexture, texCoords, ivec2(gl_FragCoord.xy));
#else
    vec3 albedo = gbufferContent.rgb;
#endif // USE_CHROMA_SUBSAMPLING

    return albedo;
}

vec3 computePositionFromCameraWS(float depth, vec2 texCoords)
{
#if USE_LINEAR_Z
    vec3 positionFromCameraWS = fragFrustumRay * depth;
#else
    vec3 positionFromCameraWS = PositionFromDepth(depth, texCoords, InvViewProjectionMatrix);
#endif // USE_LINEAR_Z

    return positionFromCameraWS;
}

vec4 debugPixelData(bool hasSSAO, bool hasBloom, bool useCleanShadowBackfaces, float finalAo, vec3 finalAmbientColor, vec3 finalLightColor)
{
#if DEBUG_PIXELS
#if DEBUG_PIXELS_USE_CLEAN_SHADOW_BACKFACES
    return useCleanShadowBackfaces ? vec4(0) : vec4(1);
#endif
#if DEBUG_PIXELS_HAS_BLOOM
    return hasBloom ? vec4(1) : vec4(0);
#endif
#if DEBUG_PIXELS_HAS_SSAO
    return hasSSAO ? vec4(1) : vec4(0);
#endif
#if DEBUG_PIXELS_FINAL_SSAO
    return vec4(finalAo);
#endif
#if DEBUG_PIXELS_FINAL_AMBIENT
    return vec4(finalAmbientColor, 1.0);
#endif
#if DEBUG_PIXELS_FINAL_LIGHT
    return vec4(finalLightColor, 1.0);
#endif
#else // DEBUG_PIXELS
    return vec4(1.0);
#endif // DEBUG_PIXELS
}

float remap(float start, float end, float x)
{
    // optimized !
    // see : http://www.humus.name/Articles/Persson_LowLevelThinking.pdf
    float range = end - start;
    return x * (1.0f / range) + (-start / range);
}

