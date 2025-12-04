#ifndef SHADOW_MAP_INCLUDE
#define SHADOW_MAP_INCLUDE

// WARNING:
// Make sure you have included
// #include "SceneData_inc.glsl"
//
// as the following data is required
//  ViewMatrix
//  SunShadowMatrix
//  SunShadowCascadeStats
//  SunShadowCascadeCachedTranslations
//  
// NB:  SunShadowCascadeStats[...].x = split distance
//      SunShadowCascadeStats[...].y = texel scale

// Max allowed amount of cascades.
#define MAX_CASCADES 4

#ifndef USE_NOISE
#define USE_NOISE 1
#endif

#define SAMPLES_COUNT 8
#include "Noise_inc.glsl"

// Number of cascade used - fed by the CPU.
#ifndef CASCADE_COUNT
#define CASCADE_COUNT 4
#endif

#ifndef USE_CAMERA_BIAS
#define USE_CAMERA_BIAS 0
#endif

#ifndef USE_NORMAL_BIAS
#define USE_NORMAL_BIAS 1
#endif

#ifndef USE_CLEAN_BACKFACES
#define USE_CLEAN_BACKFACES 1
#endif

#ifndef USE_FADING
#define USE_FADING 1
#endif

#ifndef USE_MANUAL_MODE
#define USE_MANUAL_MODE 0
#endif

#ifndef USE_SINGLE_SAMPLE
#define USE_SINGLE_SAMPLE 1
#endif

#ifndef INPUT_NORMALS_IN_WS
#define INPUT_NORMALS_IN_WS 1
#endif


#if USE_MANUAL_MODE
// To get the actual z value from the shadow map we need a common 2D sampler 
uniform sampler2D uShadowMap;
#else
uniform sampler2DShadow uShadowMap;
#endif

//// TODO : move this to the CPU
//// It will move the coords from [-1;1] to [0;1]
//const mat4 BiasMatrix  = mat4   ( vec4( 0.5f, 0.0f, 0.0f, 0.0f )
//                                , vec4( 0.0f, 0.5f, 0.0f, 0.0f )
//                                , vec4( 0.0f, 0.0f, 0.5f, 0.0f )
//                                , vec4( 0.5f, 0.5f, 0.5f, 1.0f )
//                                );

vec2 ComputeNoiseFactor()
{
#if USE_NOISE    
    vec2 seed = gl_FragCoord.xy;
    float noise = InterleavedGradientNoise(seed);
#if 1
    noise = noise * 2 - 1;

    return vec2(noise);
#else
    // Offset on the unit disk, spun for this pixel
    float ssR = 0.000001f;
    int tapIndex = int(gl_FragCoord.x + gl_FragCoord.y) % SAMPLES_COUNT;
    return VogelDiskOffset(tapIndex, TWO_PI * noise, ssR);
#endif
#else // USE_NOISE
    return vec2(0);
#endif
}

#define EPSILON         -0.0001f

float computeShadowFactor(vec3 uvw, float linearDepth, float nDotL, int cascadeId)
{
    float shadow = 0.0;

#if USE_SINGLE_SAMPLE 
    vec2 noiseFactor = ComputeNoiseFactor();
    float noiseScale = 0.00075;

    // using the 2.25 factor produces nice results in the distance, but not on close to camera.
    noiseFactor *= noiseScale;// * 2.25;

    noiseFactor *= SunShadowCascadeStats[cascadeId].y;
//    noiseFactor *= linearDepth*50;
//    noiseFactor *= clamp(nDotL, 0.25, 1) * 15;
 
#if USE_MANUAL_MODE
    float sampleZ = texture( uShadowMap, uvw.xy + noiseFactor).x;
    float distanceToOccluder = (sampleZ - EPSILON) - uvw.z;

    if (distanceToOccluder < 0)
    {
        shadow += clamp(1 - smoothstep(0, 1, abs(distanceToOccluder) * 1.5), 0, 1);
    }
 
    shadow = 1 - shadow;
#else
    shadow += texture( uShadowMap, uvw + vec3(noiseFactor, EPSILON));
#endif

    return (SunShadowIntensity + ( shadow / (1.0f - SunShadowIntensity)));

#else // USE_SINGLE_SAMPLE

    ivec2 vTexSize = textureSize(uShadowMap, 0);
    vec2 vOffset = 1.0f / vTexSize;

    vOffset *= 2; // take samples further away !

    const int N = 2;

//    float noise = InterleavedGradientNoise(seed);
//    noise = noise * 2 - 1;
    float noise = 0;
    for( int y = -N ; y < N ; y++ )
    {
        for( int x = -N ; x < N ; x++ )
        {
            vec2 offsets = vec2( x * vOffset.x, y * vOffset.y ) + vec2(noise*0.0005);

            vec3 coords = vec3( uvw.xy + offsets, uvw.z);

#if USE_MANUAL_MODE
            shadow += (texture( uShadowMap, uvw.xy).x - EPSILON < uvw.z)? 1 : 0;
#else
            shadow += texture( uShadowMap, coords);
#endif
        }
    }

    const int total = (N)*(N);
    float averageShadow = shadow / total;

    return (SunShadowIntensity + (averageShadow / (1.0f - SunShadowIntensity)));

#endif // USE_SINGLE_SAMPLE

}

int getCascadeIdNew(vec3 positionFromCameraWS, vec3 normalWS, out vec4 positionLS)
{
    // Find in which cascade we are
    // Slow but simple.
    int cascadeId = 0;

    float normalBias = clamp(0.0175 / SunShadowCascadeStats[cascadeId].y, 0.075, 0.35);
    positionLS = SunShadowMatrix[cascadeId] * vec4(normalWS * normalBias + (positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz) , 1);

    // Instead of checking against a [-1;1] cube (in projected space),
    // we check against a slightly smaller one,
    // which avoids cascade transition issues.
    const vec3 limit = vec3(0.99);

#if CASCADE_COUNT > 1
    if (any(greaterThan(abs(positionLS.xyz), limit)))
    {
        cascadeId = 1;
        normalBias = clamp(0.0175 / SunShadowCascadeStats[cascadeId].y, 0.075, 0.35);
        positionLS = SunShadowMatrix[cascadeId]  * vec4(normalWS * normalBias + (positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz) , 1);

#if CASCADE_COUNT > 2
        if (any(greaterThan(abs(positionLS.xyz), limit)))
        {
            cascadeId = 2;
            normalBias = clamp(0.0175 / SunShadowCascadeStats[cascadeId].y, 0.075, 0.35);
            positionLS = SunShadowMatrix[cascadeId]  * vec4(normalWS * normalBias + (positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz) , 1);

#if CASCADE_COUNT > 3
            if (any(greaterThan(abs(positionLS.xyz), limit)))
            {
                cascadeId = 3;
                normalBias = clamp(0.0175 / SunShadowCascadeStats[cascadeId].y, 0.075, 0.35);
                positionLS = SunShadowMatrix[cascadeId]  * vec4(normalWS * normalBias + (positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz) , 1);

                if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
            }
#else // CASCADE_COUT > 3
            if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
#endif // CASCADE_COUNT > 3

            if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
        }
#else // CASCADE_COUT > 2
        if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
#endif // CASCADE_COUNT > 2
    }
#else // CASCADE_COUNT > 1
    if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
#endif // CASCADE_COUNT > 1

    return cascadeId;
}


int getCascadeId(vec3 positionFromCameraWS, out vec4 positionLS)
{
    // Find in which cascade we are
    // Slow but simple.
    int cascadeId = 0;

    positionLS = SunShadowMatrix[cascadeId] * vec4(positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz, 1);

    // Instead of checking against a [-1;1] cube (in projected space),
    // we check against a slightly smaller one,
    // which avoids cascade transition issues.
    const vec3 limit = vec3(0.99);

#if CASCADE_COUNT > 1
    if (any(greaterThan(abs(positionLS.xyz), limit)))
    {
        cascadeId = 1;
        positionLS = SunShadowMatrix[cascadeId] * vec4(positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz, 1);

#if CASCADE_COUNT > 2
        if (any(greaterThan(abs(positionLS.xyz), limit)))
        {
            cascadeId = 2;
            positionLS = SunShadowMatrix[cascadeId] * vec4(positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz, 1);

#if CASCADE_COUNT > 3
            if (any(greaterThan(abs(positionLS.xyz), limit)))
            {
                cascadeId = 3;
                positionLS = SunShadowMatrix[cascadeId] * vec4(positionFromCameraWS + SunShadowCascadeCachedTranslations[cascadeId].xyz, 1);

                if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
            }
#else // CASCADE_COUT > 3
            if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
#endif // CASCADE_COUNT > 3

            if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
        }
#else // CASCADE_COUT > 2
        if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
#endif // CASCADE_COUNT > 2
    }
#else // CASCADE_COUNT > 1
    if (any(greaterThan(abs(positionLS.xyz), limit))) return -1;
#endif // CASCADE_COUNT > 1

    return cascadeId;
}


float computeShadowIntensity(vec3 positionFromCameraWS, float linearDepth, vec3 normalWS, bool useCleanShadowBackfaces)
{
#if USE_CAMERA_BIAS
    // offset a little in the camera direction, to avoid acne.
    vec3 offset = -normalize(positionFromCameraWS);
    positionFromCameraWS.xyz += offset * 0.1;
#endif

    // NB: Shadow direction can be different from SunLightDirection.xyz, since we may use a shift when we use "safe angles".
    vec3 lightDir = normalize(vec3(SunShadowMatrix[0][0][2], SunShadowMatrix[0][1][2], SunShadowMatrix[0][2][2]));
    
    // Light Depth Bias - connects better the upper part of the shadow
    positionFromCameraWS.xyz += lightDir * 0.1;

#if USE_NORMAL_BIAS
    // Bias that works for distant shadows.
    // It should be smaller for closer cascades.
    float normalOffset = 0.35;
#endif //USE_NORMAL_BIAS

    // Find in which cascade we are
    int cascadeId = 0;
    vec4 positionLS;
//    cascadeId = getCascadeId(positionFromCameraWS, positionLS);
    cascadeId = getCascadeIdNew(positionFromCameraWS, normalWS, positionLS);

    if (cascadeId < 0) return 1.0;

    // To Texture (atlas) Space.
    vec3 positionTS  = positionLS.xyz;
    float borderDistance = length(positionTS.xy);
    positionTS.xyz = positionTS.xyz * vec3(0.5) + vec3(0.5);

    // Get ready to sample the right cascade.
    const float CascadeScale = 1.0f / CASCADE_COUNT;
    positionTS.x = positionTS.x * CascadeScale + CascadeScale * cascadeId;
    
    float isFrontFacing = 1;
    float nDotL = 1;

#if USE_CLEAN_BACKFACES
    nDotL = dot(normalWS, lightDir);
    isFrontFacing = (!useCleanShadowBackfaces || nDotL < 0) ? 1 : SunShadowIntensity;
#endif // USE_CLEAN_BACKFACES

    float shadow = computeShadowFactor(positionTS.xyz, linearDepth, nDotL, cascadeId);

    shadow = min(shadow, isFrontFacing);

#if USE_FADING
    // 1. Fade out (in circle) when we reach the borders of the last cascade.
    float cascadeBorderFadeOutFactor = (CASCADE_COUNT - 1) != cascadeId ? 0 : pow(borderDistance, 20);

    // 2. Fade out w/ camera distance.
    float farCascade = SunShadowCascadeStats[CASCADE_COUNT-1].x;
    float distanceToCamera = length(positionFromCameraWS.xyz);

    // Use different fading power according to the number of cascades, to hide different issues.
    const float fadeOutPower = CASCADE_COUNT <= 2 ? 0.15 : 0.75;
    float cameraDistanceFadeOutFactor = pow(clamp(distanceToCamera / farCascade, 0, 1), fadeOutPower);

    float fadeOutFactor = max(cascadeBorderFadeOutFactor, cameraDistanceFadeOutFactor);
    shadow = max(shadow, fadeOutFactor);
#endif

    return clamp(shadow, 0, 1);
}

#endif //SHADOW_MAP_INCLUDE
