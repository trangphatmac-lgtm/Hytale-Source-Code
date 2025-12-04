#version 330 core

#include "Reconstruction_inc.glsl"
#include "Deferred_inc.glsl"
#include "Noise_inc.glsl"

// Force the LINEAR_Z usage, cause it's a lot faster
#define USE_LINEAR_Z 1

#ifndef USE_TEMPORAL_FILTERING
#define USE_TEMPORAL_FILTERING 1
#endif

#ifndef INPUT_NORMALS_IN_WS
#define INPUT_NORMALS_IN_WS 1
#endif

// Possible choices : A, B, C, D
#define AO_EQUATION_C 1
// Possible choices : SAMPLING_PATTERN_VOGEL_DISK, SAMPLING_PATTERN_ALCHEMY
#define SAMPLING_PATTERN_VOGEL_DISK 1
// Possible choices : DISTANT_FADE_OUT_METHOD_A, DISTANT_FADE_OUT_METHOD_B
#define DISTANT_FADE_OUT_METHOD_A 1

// TODO : Additionnal improvement to try strategy
// - Use a HiZ pyramid instead of flat Z buffer

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;
uniform sampler2D uTapsSourceTexture;
uniform sampler2D uGBufferTexture;
uniform sampler2D uSSAOCacheTexture;
uniform sampler2D uShadowTexture;

uniform vec2 uViewportSize;
uniform mat4 uReprojectMatrix;
uniform float uTemporalSampleOffset;
uniform vec2 uSamplesData[16];

uniform vec4 uPackedParameters = vec4(0.2, 2.0, 0.95, 500.0 * 0.95);

#define PARAM_OCCLUSION_MAX uPackedParameters.x
#define PARAM_OCCLUSION_STRENGTH uPackedParameters.y
#define PARAM_RADIUS uPackedParameters.z
#define PARAM_RADIUS_PROJECTED uPackedParameters.w

// Makes the PARAM_RADIUS projected scale independent from the rendering resolution.
// NB : the calibration was done at 1920x1080, hence the 1920 here.
//float radiusProjScale = projScale * uViewportSize.x / 1920.0f;

#if INPUT_NORMALS_IN_WS
uniform mat4 uViewMatrix;
#endif

#if USE_LINEAR_Z
uniform mat4 uProjectionMatrix;
#else
uniform mat4 uInvProjectionMatrix;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;
in vec3 fragFrustumRay;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//---------------------------------------------------------------------------------------------------------

const float uFarClip = 1024.0f;

// Bias to avoid AO in smooth corners, e.g., 0.01m
const float biasNear = 0.075;
const float biasFar = 0.25;
const float SSAO_FAR_Z = 90.0;

float radius2 = PARAM_RADIUS * PARAM_RADIUS;

vec4 ProjInfo = vec4(    -2.0f / uProjectionMatrix[0][0],
                          -2.0f / uProjectionMatrix[1][1],
                          ( 1.0f - uProjectionMatrix[0][2]) / uProjectionMatrix[0][0],
                          ( 1.0f + uProjectionMatrix[1][2]) / uProjectionMatrix[1][1]);

// Read the camera-space position of the point at screen-space pixel ssP + unitOffset * ssR.  Assumes length(unitOffset) == 1
vec3 getOffsetPosition(ivec2 ssC, vec2 unitOffset, float ssR)
{
    vec3 P;

    // TODO : minor opti - use uInvViewportSize instead
    vec2 pixelSize = 1.0 / uViewportSize;
    vec2 screenUV = (ssR * unitOffset) * pixelSize + fragTexCoords;

    // TODO : use hiZ buffer as input, and select the right MIP manually
    float linearDepth = -texture(uTapsSourceTexture, screenUV).r * uFarClip;

    // Offset to pixel center
    P = PositionFromLinearDepth(screenUV, linearDepth, ProjInfo);

    return P;
}

#define PI              3.1415f
#define TWO_PI          2.0f * PI

// Compute the occlusion due to sample with index \a i about the pixel at \a ssC that corresponds
// to camera-space point \a C with unit normal \a n_C, using maximum screen-space sampling PARAM_RADIUS \a ssDiskRadius
float sampleAO(in ivec2 ssC, in vec3 positionVS, in vec3 normalVS, in float ssDiskRadius, in int tapIndex, float bias)
{
    // Offset on the unit disk, spun for this pixel
    float ssR = 1.0f;

#ifdef SAMPLING_PATTERN_VOGEL_DISK
    float noise = InterleavedGradientNoise(gl_FragCoord.xy);
#if USE_TEMPORAL_FILTERING
    noise += uTemporalSampleOffset;
#endif
    vec2 unitOffset = VogelDiskOffset(tapIndex, uSamplesData, TWO_PI * noise, ssR);
#else // SAMPLING_PATTERN_VOGEL_DISK
    float noise = AlchemyNoise(ivec2(gl_FragCoord.xy));
    vec2 unitOffset = AlchemySpiralOffset(tapIndex, noise);
#endif // SAMPLING_PATTERN_VOGEL_DISK

    // Samples taken vertically are slightly more important, so shrink the .x
    unitOffset.x *= 0.75;

    // return unitOffset.y;
    ssR *= ssDiskRadius;

    // The occluding point in camera space
    vec3 samplePositionVS = getOffsetPosition(ssC, unitOffset, ssR);
    vec3 v = samplePositionVS - positionVS;
    // return length(v);
    float vv = dot(v, v);
    float vn = dot(v, normalVS);

    float ao;

#ifdef AO_EQUATION_A
    // A:
     const float epsilon = 0.01;
     ao = float(vv < radius2) * max((vn - bias) / (epsilon + vv), 0.0) * radius2 * 0.6;
#endif
#ifdef AO_EQUATION_B
    // B: Smoother transition to zero (lowers contrast, smoothing out corners). [Recommended]
     const float epsilon = 0.0001;
     float f = max(radius2 - vv, 0.0);
     ao = f * max((vn - bias) / (epsilon + vv), 0.0);
    //  ao = f * f * f * max((vn - bias) / (epsilon + vv), 0.0);

#endif
#ifdef AO_EQUATION_C
    // C : No division
    float invRadius2 = 1.0 / radius2;
    ao = 4.0 * max(1.0 - vv * invRadius2, 0.0) * max(vn - bias, 0.0);
#endif
#ifdef AO_EQUATION_D
    // D : No division either
     ao = 2.0 * float(vv < PARAM_RADIUS * PARAM_RADIUS) * max(vn - bias, 0.0);
#endif

    // Tweak
    //float difference = v.z;
    // ao = step(0, difference) * (1.0 - smoothstep(0.0, 1.0, vv)) * 1.0/sqrt(vv + 0.001) * max(vn, 0.15);

    return ao;
}

vec2 ReprojectToPreviousFrame(vec3 currentPosition, mat4 reprojectMatrix)
{
    // Optimize me : we don't need the Z.
    // Back to previous frame screen space position
    vec4 vProjectedPos = vec4(currentPosition, 1.0f);
    vec4 prevFrameCoords = reprojectMatrix * vProjectedPos;
    prevFrameCoords.xy /= prevFrameCoords.w;
    prevFrameCoords.xy = prevFrameCoords.xy * 0.5 + 0.5;

    return prevFrameCoords.xy;
}

vec2 TemporalFiltering(vec2 currentValue, vec3 positionVS, float linear01Depth, vec2 screenUV, mat4 reprojectMatrix, sampler2D cacheTexture)
{
    // 1. Reproject  current position (view space) into the previous frame (screen space)
    vec2 prevFrameCoords = ReprojectToPreviousFrame(positionVS, reprojectMatrix);

    // 2. Fetch the previous frame data, and unpack the linear01Depth that was stored in the .GB channels
    vec4 cachedData = texture(cacheTexture, prevFrameCoords.xy).rgba;
    float cachedLinear01Depth = unpackDepth(cachedData.gb);

    // 3. Try to compute some similarity between the previous pos & the current one
    // - we base the similarity on depth & velocity

    // FIXME : improving the reprojection matrix precision would help a lot here !
    const float DEPTH_MIN_SIMILARITY = -0.1;
    float depthForwardVariation = cachedLinear01Depth / linear01Depth;
    float depthInverseVariation = linear01Depth / cachedLinear01Depth;
    float depthVariation = depthForwardVariation;//(depthForwardVariation < depthInverseVariation) ? depthForwardVariation : depthInverseVariation;
    float depthSimilarity = clamp(pow(depthVariation, 4.0) + DEPTH_MIN_SIMILARITY, 0.0, 1.0);

    const float VELOCITY_SCALAR = 250.0;
    float veclocity = length(prevFrameCoords.xy - screenUV.xy) * VELOCITY_SCALAR;
    float velocitySimilarity = clamp(1.0 / veclocity, 0.0, 1.0);

    // Avoid accumulating temporal samples over too many frames
    const float ssaoSimilarityDecayFactor = 0.95;
    const float shadowSimilarityDecayFactor = 0.8;
    float similarity = depthSimilarity * velocitySimilarity;

    //similarity = velocitySimilarity * similarityDecayFactor;// * velocitySimilarity;

    // Discard if prev was out of screen - should be automatically done with velocitySimilarity
    // similarity *= any(lessThan(prevFrameCoords.xy, vec2(0.0))) || any(greaterThan(prevFrameCoords.xy, vec2(1.0))) ? 0.0 : 1.0;

    return mix(currentValue, cachedData.ra, vec2(ssaoSimilarityDecayFactor, shadowSimilarityDecayFactor) * similarity);
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    float ao = 1.0;

    float depth = texture(uDepthTexture, fragTexCoords).r;
    float deferredShadow = texture(uShadowTexture, fragTexCoords).r;

#if USE_LINEAR_Z
    float linear01Depth = depth;
    vec3 positionVS = fragFrustumRay * linear01Depth;
#else
    float depthHW = depth;
    vec3 positionVS = PositionFromDepth(depthHW, fragTexCoords, uInvProjectionMatrix);
    // positionVS = PositionFromDepthHW(depthHW, fragTexCoords, uProjectionMatrix);
    float linear01Depth = -positionVS.z / uFarClip;
#endif // USE_LINEAR_Z

    // ao is unstable in the distance, so avoid it
    float distance = length(positionVS);
    float ssaoDistanceScale = distance / SSAO_FAR_Z;

    ivec2 ssC = ivec2(gl_FragCoord.xy);

    if (ssaoDistanceScale < 1.0)
    {
        vec2 compressedNormal = texture(uGBufferTexture, fragTexCoords).ba;

        // WARNING : it's a looot better to use a compile time constant for performance, cause it'll allow loop unrolling by the compiler

        // NB : This comes from a CPU side define
        const int numSamples = SAMPLES_COUNT;

        // Choose the screen-space sample PARAM_RADIUS
        float ssDiskRadius = PARAM_RADIUS_PROJECTED / max(-positionVS.z, 0.1f);
        // ssDiskRadius = min(150, ssDiskRadius);

        float occlusion = 0.0;

        float bias = mix(biasNear, biasFar, ssaoDistanceScale);

        vec3 normalVS = decodeNormal(compressedNormal);

#if INPUT_NORMALS_IN_WS
        normalVS = mat3(uViewMatrix) * normalVS;
#endif

        for (int i = 0; i < numSamples; i++)
        {
            occlusion += sampleAO(ssC, positionVS, normalVS, ssDiskRadius, i, bias);
        }

        // Basic method
        occlusion = occlusion / float(numSamples);
        ao = 1.0 - clamp(PARAM_OCCLUSION_STRENGTH * occlusion, 0.0, PARAM_OCCLUSION_MAX);

        // // Alternate method used instead of "occlusion / float(numSamples);"
        // const float temp = radius2 * PARAM_RADIUS;
        // occlusion /= temp * temp;
        // ao = 1.0f - clamp(occlusion * (PARAM_OCCLUSION_STRENGTH / numSamples), 0.0, PARAM_OCCLUSION_MAX);

        // Fade out near : this algorithm has problems with near surfaces... lerp it out smoothly
        ao = mix(ao, 1.0f, 1.0f - clamp(-0.3f * positionVS.z, 0.0, 1.0));

        // Fade out far : and lerp out the as well in the distance
        float fadeOutFactor = (ssaoDistanceScale );

#ifdef DISTANT_FADE_OUT_METHOD_A
        ao = mix(ao, 1.0f, clamp(fadeOutFactor, 0.0, 1.0));
#endif
#ifdef DISTANT_FADE_OUT_METHOD_B
        // this second version fades out on the 2nd half of Z
        ao = mix(ao, 1.0f, clamp(1.5 * (ssaoDistanceScale)-0.5, 0.0, 1.0));
#endif

#if USE_TEMPORAL_FILTERING
        // Refine the result with data from the previous frame
        vec2 filteredValues = TemporalFiltering(vec2(ao, deferredShadow), positionVS, linear01Depth, fragTexCoords, uReprojectMatrix, uSSAOCacheTexture);
        
        ao = filteredValues.x;
        deferredShadow = filteredValues.y;
    }
    // Also use temporal refinement for shadows beyond the ssao far z limit.
    else if (linear01Depth < 500)
    {
        // Refine the result with data from the previous frame
        vec2 filteredValues = TemporalFiltering(vec2(ao, deferredShadow), positionVS, linear01Depth, fragTexCoords, uReprojectMatrix, uSSAOCacheTexture);
        
        deferredShadow = filteredValues.y;    
#endif
    }

    // Bilateral box-filter over a quad for free, respecting depth edges
    // (the difference that this makes is subtle)
    if (abs(dFdx(positionVS.z)) < 0.2)
    {
        ao -= dFdx(ao) * ((ssC.x & 1) - 0.5);
    }
    if (abs(dFdy(positionVS.z)) < 0.2)
    {
        ao -= dFdy(ao) * ((ssC.y & 1) - 0.5);
    }

    outColor.r = ao;
    outColor.gb = packDepth(linear01Depth);
    outColor.a = deferredShadow;
}
