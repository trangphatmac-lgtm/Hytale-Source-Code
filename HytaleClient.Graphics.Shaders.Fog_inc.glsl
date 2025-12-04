#ifndef FOG_INCLUDE
#define FOG_INCLUDE

#include "Dithering_inc.glsl"

#ifndef USE_FOG
#define USE_FOG 0
#endif

#ifndef USE_MOOD_FOG
#define USE_MOOD_FOG 0
#endif

#ifndef USE_FOG_DITHERING
#define USE_FOG_DITHERING 0
#endif

// fogParams :
#define fogDistanceStart fogParams.x
#define fogDistanceEnd fogParams.y
#define fogDepthStart fogParams.z
#define fogDepthFalloff fogParams.w


float pow2(float x) {return x*x;}

float computeSunProximityFactor(vec3 cameraToFragment, vec3 normalizedSunPosition)
{
    // ASSERT : cameraToFragment must be a normalized vector
    return dot(cameraToFragment, normalizedSunPosition) * 0.5 + 0.5;
}

vec3 getFogColorHorizon(vec3 fogFrontColor, vec3 fogBackColor, vec3 cameraToFragment, vec3 normalizedSunPosition)
{
    // Use a blend of front & back color for fog, according to proximity to the sun
    float sunProximityFactor = computeSunProximityFactor(cameraToFragment, normalizedSunPosition);

    return mix(fogBackColor, fogFrontColor, sunProximityFactor);
}

vec3 getFogColor(vec3 fogTopColor, vec3 fogFrontColor, vec3 fogBackColor, vec3 cameraToFragment, vec3 normalizedSunPosition)
{
// Alternate version, kept for future reference - does the vertical mix before the horizontal mix
/*/
    float sunProximityFactor = computeSunProximityFactor(cameraToFragment, normalizedSunPosition);

    float gradientHeight = clamp(cameraToFragment.y * 1.25, 0.0, 1.0);

    vec3 verticalColor = mix(fogBackColor, fogTopColor, gradientHeight);// *(1.0 - sunProximityFactor * sunProximityFactor) );
    vec3 horizontalColor = mix(verticalColor, mix(fogFrontColor, fogBackColor, 0.1f), sunProximityFactor);

    return horizontalColor;

/*/
    float sunProximityFactor = computeSunProximityFactor(cameraToFragment, normalizedSunPosition);

    vec3 bottomColor = mix(fogBackColor, fogFrontColor, sunProximityFactor);

    float gradientHeight = clamp(cameraToFragment.y * 1.25, 0.0, 1.0);
    return mix(bottomColor, fogTopColor, gradientHeight);// *(1.0 - sunProximityFactor * sunProximityFactor) );
//*/
}

float computeDistantFogFactor(vec3 positionFromCamera, vec4 fogParams)
{
    float fogFactor = 0.0;

    float dist = length(positionFromCamera.xz);
    fogFactor = clamp((fogDistanceEnd - dist) / (fogDistanceEnd - fogDistanceStart), 0.0, 1.0);
    fogFactor = clamp(exp(-pow2(4.0 * fogFactor)) * 1.02, 0.0, 1.0);

    return fogFactor;
}

float computeDepthFogFactor(vec3 positionFromCamera, vec4 fogParams)
{
    float fogFactor = 0.0;

    float depthDist = length(positionFromCamera.xyz);
    float depthDistMax = fogDepthStart;
    fogFactor = clamp(1.0 - ((depthDistMax - depthDist) / depthDistMax), 0.0, 1.0);
    fogFactor = pow(fogFactor, fogDepthFalloff);

    return fogFactor;
}

float computeDistantDepthFogFactor(vec3 positionFromCamera, vec4 fogParams)
{
    float fogFactor = 0.0;

    if (fogDistanceEnd > 0.0)
    {
        fogFactor = computeDistantFogFactor(positionFromCamera, fogParams);

        if (fogDepthStart > 0.0)
        {
            float fogDepthFactor = computeDepthFogFactor(positionFromCamera, fogParams);
            fogFactor = max(fogDepthFactor, fogFactor);
        }
    }

    return fogFactor;
}

void computeMoodFog(vec3 positionFromCamera, vec3 cameraPosition, float heightFalloff, float density, vec3 fogBackColor,
                    vec3 ditherNoise, bool useSmoothNearMoodColor, float fogHeightDensityAtViewer, float noise, float densityVariationScale, float sceneBrightness,
                    inout vec3 fogColor, inout float fogThicknessFactor)
{
    // Based on CryTek height falloff fog, presented in GDC 2007 presentation.

    float cHeightFalloff = heightFalloff;
    float cGlobalDensity = density;

    cGlobalDensity = density * (noise * densityVariationScale + 1.0);

    vec3 cameraToWorldPos = positionFromCamera;

    float fogInt = length(cameraToWorldPos) * fogHeightDensityAtViewer;

    const float cSlopeThreshold = 0.01;
    if (abs(cameraToWorldPos.y) > cSlopeThreshold)
    {
        float t = cHeightFalloff * cameraToWorldPos.y;
        fogInt *= ( 1.0 - exp(-t) ) / t;
    }

    const float maxThickness = 0.01;
    fogThicknessFactor = clamp(1.0 - exp(-cGlobalDensity * fogInt), maxThickness, 1.0);

    // modifies fog thickness according to scene global brightness.
    sceneBrightness = mix(-0.2, 1.2, sceneBrightness + 0.1);
    fogThicknessFactor *= clamp(sceneBrightness, 0.0, 1.0);

    if (useSmoothNearMoodColor)
    {
        float dist = clamp(length(positionFromCamera.xz) * 0.01, 0.0, 1.0);
        fogColor = mix(fogBackColor, fogColor, dist);
    }

#if USE_FOG_DITHERING
    fogColor = ditherRGB( fogColor, ditherNoise, 255.0);
#endif
}

float fetchSceneBrightness(sampler2D sceneBrightnessTexture)
{
    // Scene Brightness is a temporally stabilized average of the sun occlusion on screen.
    return textureLod(sceneBrightnessTexture, vec2(0.5),9).r;
}

vec4 computeFog(vec3 positionFromCameraWS, vec3 cameraPosition, vec3 sunDirection,
                vec3 fogTopColor, vec3 fogFrontColor, vec3 fogBackColor, vec4 fogParams, vec4 fogMoodParams, float fogHeightDensityAtViewer,
                vec2 ditherNoiseSeed,sampler2D noiseTexture, float sceneBrightness)
{

#define fogHeightFalloff (fogMoodParams.x)
#define fogGlobalDensity (fogMoodParams.y)
#define fogSpeed (fogMoodParams.z)
#define fogDensityVariationScale (fogMoodParams.w)

    vec4 fog = vec4(0);

#if USE_FOG
    // First compute distant fog (and depth fog)
    vec3 cameraToFragment = normalize(positionFromCameraWS);
    vec3 smoothFogColor = getFogColor(fogTopColor, fogFrontColor, fogBackColor, cameraToFragment, sunDirection);

    float fogFactor = computeDistantDepthFogFactor(positionFromCameraWS, fogParams);

    fog = vec4(smoothFogColor.rgb, fogFactor);

#if USE_MOOD_FOG
    float noise = 0.0f;

    if (fogGlobalDensity > 0)
    {
        const vec2 projectionUVScale = vec2(0.005f);
        vec2 projectionUVFog = (positionFromCameraWS.xz + cameraPosition.xz) * projectionUVScale + vec2(fogSpeed);
        noise = texture(noiseTexture, projectionUVFog).r;
        noise = noise * 2 - 1;

        vec3 smoothMoodFogColor = getFogColor(fogBackColor, fogFrontColor, fogBackColor, cameraToFragment, sunDirection);

        // Custom modif for bloom
//        smoothMoodFogColor.rgb = mix(color.rgb, smoothMoodFogColor.rgb, hasBloom ? 0.7 : 1);

        vec3 ditherNoise = vec3(0);
#if USE_FOG_DITHERING
        ditherNoise = vec3(n2rand_faster_animated(ditherNoiseSeed, fogSpeed));
#endif // USE_FOG_DITHERING

        const bool useSmoothNearMoodColor = USE_SMOOTH_NEAR_MOOD_FOG_COLOR == 1;

        float moodFogThickness = 0.0;
        computeMoodFog(positionFromCameraWS, cameraPosition, fogHeightFalloff, fogGlobalDensity, fogBackColor.rgb, ditherNoise, useSmoothNearMoodColor,
        fogHeightDensityAtViewer, noise, fogDensityVariationScale, sceneBrightness, smoothMoodFogColor, moodFogThickness);

        fog.rgb = mix(fog.rgb, smoothMoodFogColor, moodFogThickness);
        fog.a = max(moodFogThickness, fog.a);
    }
#endif // USE_MOOD_FOG
#endif // USE_FOG

#undef fogHeightFalloff
#undef fogGlobalDensity
#undef fogSpeed
#undef fogDensityVariationScale
    return fog;
}

vec4 computeFogForSky(vec3 positionFromCameraWS, vec3 cameraPosition, vec3 sunDirection,
                vec3 fogFrontColor, vec3 fogBackColor, vec3 fogMoodParams, float fogHeightDensityAtViewer,
                sampler2D sceneBrightnessTexture)
{

#define fogHeightFalloff (fogMoodParams.x)
#define fogGlobalDensity (fogMoodParams.y)
#define fogSpeed (fogMoodParams.z)

    vec4 fog = vec4(0);

#if USE_MOOD_FOG
    if (fogGlobalDensity > 0)
    {
        // Scene Brightness is a temporally stabilized average of the sun occlusion on screen.
        float sceneBrightness = fetchSceneBrightness(sceneBrightnessTexture);

        vec3 cameraToFragment = normalize(positionFromCameraWS);
        vec3 smoothMoodFogColor = getFogColor(fogBackColor, fogFrontColor, fogBackColor, cameraToFragment, sunDirection);

        // Custom modif for bloom
//        smoothMoodFogColor.rgb = mix(color.rgb, smoothMoodFogColor.rgb, hasBloom ? 0.7 : 1);

        vec3 ditherNoise = vec3(0);

        const bool useSmoothNearMoodColor = false;//USE_SMOOTH_NEAR_MOOD_FOG_COLOR == 1;
        const float fogDensityVariationScale = 0.0f;
        const float noise = 0.0f;

        float moodFogThickness = 0.0;
        computeMoodFog(positionFromCameraWS, cameraPosition, fogHeightFalloff, fogGlobalDensity, fogBackColor.rgb, ditherNoise, useSmoothNearMoodColor,
                        fogHeightDensityAtViewer, noise, fogDensityVariationScale, sceneBrightness, smoothMoodFogColor, moodFogThickness);

        fog.rgb = smoothMoodFogColor;
        fog.a = moodFogThickness;
    }
#endif // USE_MOOD_FOG

#undef fogHeightFalloff
#undef fogGlobalDensity
#undef fogSpeed
    return fog;
}

vec3 applyDistantFog(vec3 positionFromCamera, vec3 fragmentColor, vec3 fogColor, vec4 fogParams)
{
    float fogFactor = 0.0;

    if (fogDistanceEnd > 0.0)
    {
        fogFactor = computeDistantFogFactor(positionFromCamera, fogParams);
    }

    return mix(fragmentColor, fogColor, fogFactor);
}

#undef fogDistanceStart
#undef fogDistanceEnd
#undef fogDepthStart
#undef fogDepthFalloff


#endif //FOG_INCLUDE
