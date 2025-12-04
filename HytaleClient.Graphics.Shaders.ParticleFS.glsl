#version 330 core

// Global scene data
#include "SceneData_inc.glsl"
#include "Sampling_inc.glsl"
#include "Deferred_inc.glsl"
#include "Distortion_inc.glsl"
#include "FX_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
// Required before "OIT_inc.glsl"
const int BLEND_MODE_LINEAR = 0;
const int BLEND_MODE_ADD = 1;
const int BLEND_MODE_EROSION = 2;

#ifndef USE_OIT
#define USE_OIT 0
#endif
uniform ivec2 uOITParams;

// Moments OIT related
uniform sampler2D uMomentsTexture;
uniform sampler2D uTotalOpticalDepthTexture;

#include "OIT_inc.glsl"

uniform sampler2D uSmoothTexture;
uniform sampler2D uTexture;
uniform sampler2D uDepthTexture;
uniform sampler2DArray uUVMotionTexture;

// Can be differ from scene data during multi res passes.
uniform vec2 uCurrentInvViewportSize;
uniform vec2 uInvTextureAtlasSize;

// Buffer containing all spawners data
uniform samplerBuffer uSpawnerDataBuffer;
const int SPAWNER_DATA_SIZE = 12;
// SPAWNER_WORLDMATRIX_OFFSET = 0;
// SPAWNER_STATICLIGHTCOLOR_INFLUENCE_OFFSET = 4;
// SPAWNER_INVROTATION_OFFSET = 5;
// SPAWNER_TEXIMAGELOCATION_OFFSET = 6;
// SPAWNER_TEXFRAMESIZE_OFFSET = 7;
// SPAWNER_UVMOTION_OFFSET = 8;
// SPAWNER_INTERSECTION_HIGHLIGHT = 9
// SPAWNER_CAMERA_OFFSET_VELOCITY_STRETCH_SOFT_FACTOR = 10

#define uvMotionSpeed (uvMotionParams.xy)
#define uvMotionScale (uvMotionParams.w)

uniform int uDebugOverdraw;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
// Common
flat in int fragConfig;
flat in int fragFXType;

in vec4 fragTexCoords;
in vec4 fragColor;
in float fragDepthVS;

flat in float fragUVMotionOffset;
flat in float fragUVMotionStrength;
#define uvMotionStrength (fragUVMotionStrength)

#if USE_DISTORTION_RT
in vec3 fragPositionVS;
#endif

#if USE_FOG
in vec4 fragFogInfo;
#endif // USE_FOG

// Common
flat in vec4 fragIntersectionHighlight;
#define intersectionHighlightColor      (fragIntersectionHighlight.rgb)
#define intersectionHighlightThreshold  (fragIntersectionHighlight.a)

// Particle
flat in vec4 fragAtlasUVOffsetSize;
flat in vec4 fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2;
#define fragAtlasUVOffset               (fragAtlasUVOffsetSize.xy)
#define fragAtlasUVSize                 (fragAtlasUVOffsetSize.zw)
#define fragUVMotionTextureId           (fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2.x)
#define fragSpriteBlend                 (fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2.y)
#define fragAtlasUVOffset2              (fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2.zw)

// Trail
in vec3 fragK;
in vec4 fragPlaneCoords;
in vec4 fragPlaneCoordsu;

// Bit shifts to extract data from fragConfig
const int CONFIG_BIT_SHIFT_LINEAR_FILTERING = 3;
const int CONFIG_BIT_SHIFT_SOFT_PARTICLE = 4;
const int CONFIG_BIT_SHIFT_INVERT_TEXTURE_U = 5;
const int CONFIG_BIT_SHIFT_INVERT_TEXTURE_V = 6;
const int CONFIG_BIT_SHIFT_BLEND_MODE = 7;
const int CONFIG_BIT_SHIFT_FIRST_PERSON = 9;
const int CONFIG_BIT_SHIFT_DRAW_ID = 16;

const int CONFIG_BIT_MASK_BLEND_MODE = (1 << 2) - 1;

// Filtering modes include a custom one to deal w/ linear filtering issues in a texture atlas
const int FILTERING_HARDWARE_NEAREST = 0;
const int FILTERING_HARDWARE_LINEAR = 1;
const int FILTERING_CUSTOM_LINEAR = 2;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
#if USE_OIT
layout (location = 0) out vec4 outColor0;
layout (location = 1) out vec4 outColor1;
#else
layout (location = 0) out vec4 outColor;
#define outColor0 outColor
#endif

//-------------------------------------------------------------------------------------------------------------------------

void particle();
void trail();

vec4 customLinearFiltering(vec2 normalizedUV, vec2 atlasUVOffset)
{
    return customLinearFilteringWithNormalizedUV(uTexture, normalizedUV, uInvTextureAtlasSize / fragAtlasUVSize, atlasUVOffset, fragAtlasUVSize); 
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    if (fragFXType == 0)
    {
        particle();
    }
    else
    {
        trail();
    }
}

//-------------------------------------------------------------------------------------------------------------------------
// Particles

void particle()
{
#if DEBUG_OVERDRAW
    if (uDebugOverdraw == 1)
    {
        // Add 1 per fragment drawn, using additive blending w/ a R32F render target.
        outColor0 = vec4(1); return;
    }
#endif // DEBUG_OVERDRAW

    vec4 texel;
    vec4 color;

    int drawId = fragConfig >> CONFIG_BIT_SHIFT_DRAW_ID;
    int address = drawId * SPAWNER_DATA_SIZE;
    int filteringMode = (fragConfig >> CONFIG_BIT_SHIFT_LINEAR_FILTERING) & 1;

    float staticLightColorInfluenceW = texelFetch(uSpawnerDataBuffer, address + 4).w;
    vec4 uvMotionParams = texelFetch(uSpawnerDataBuffer, address + 8);
    float softParticlesFadeFactor = texelFetch(uSpawnerDataBuffer, address + 10).z;

    vec2 normalizedUV = fragTexCoords.zw;
    vec2 uvMotionUV = fragTexCoords.zw + vec2(fragUVMotionOffset);

    // Invert if needed.
    int flipU = (fragConfig >> CONFIG_BIT_SHIFT_INVERT_TEXTURE_U) & 1;
    int flipV = (fragConfig >> CONFIG_BIT_SHIFT_INVERT_TEXTURE_V) & 1;
    normalizedUV.x = (flipU != 0) ? 1 - normalizedUV.x : normalizedUV.x;
    normalizedUV.y = (flipV != 0) ? 1 - normalizedUV.y : normalizedUV.y;

    // Debug w/ these values.
//     vec2 motionSpeed = vec2(0,0.1);
//     float motionScale = 1.2;
//     float motionStrength = 0;//.25;

    vec2 flow = vec2(0);
    bool isEdge = false;

    if (uvMotionScale != 0 && fragUVMotionTextureId >= 0)
    {
        // Scale the strength by the normalized size of the tex, to keep the behaviour consistent across all tex size.
        float motionStrength = uvMotionStrength;// * 3;// * 1000;
        vec2 motionSpeed = uvMotionSpeed;
        float motionScale = uvMotionScale;

        // Get the motion vector in [0,1] and remap it to [-1,1]
        flow = texture(uUVMotionTexture, vec3(uvMotionUV * motionScale + motionSpeed * Time, fragUVMotionTextureId)).rg;
        flow = vec2(2.0) * flow - vec2(1.0);

        // Apply the motion to the uv.
        vec2 factorN = vec2(0.01) * vec2(1,4);
        normalizedUV = flow * (motionStrength * factorN) + normalizedUV;

        // There is a common problem when using wrapping w/ a texture atlas.
        // So we first must wrap manually.
        normalizedUV = manualWrapNormalizedUV(normalizedUV);

        // Then, if LINEAR filtering is wanted, we need to filter manually on the edge, because there would be bleeding otherwise.
        // And for better performance, we still use the hardware filtering in the general case, and will only do the manual filtering when it's required, 
        // i.e. when we are on the edge.
        vec2 halfTexelN = 0.5 * (uInvTextureAtlasSize / fragAtlasUVSize);
        isEdge = (normalizedUV.x > 1 - halfTexelN.x) || (normalizedUV.x < 0 + halfTexelN.x) || (normalizedUV.y > 1 - halfTexelN.y) || (normalizedUV.y < 0 + halfTexelN.y);

        filteringMode = (filteringMode == FILTERING_HARDWARE_LINEAR && isEdge) ? FILTERING_CUSTOM_LINEAR : filteringMode;
    }

#if USE_DISTORTION_RT

    // Build the uv in the atlas from the normalizedUV & the texture area info
    vec2 uv = computeAtlasUV(normalizedUV, fragAtlasUVOffset, fragAtlasUVSize);

    texel = texture(uTexture, uv);

    if (texel.a < 0.05) discard;

    color.rg = computeDistortion(uCurrentInvViewportSize, fragPositionVS, texel, fragColor.a);
    color.ba = vec2(0,1);
    
    outColor0 = color;
#else //USE_DISTORTION_RT

    // Build the uv in the atlas from the normalizedUV & the texture area info
    vec2 currentAtlasUVOffset = fragSpriteBlend == 0 ? fragAtlasUVOffset2 : fragAtlasUVOffset;
    vec2 uv = computeAtlasUV(normalizedUV, currentAtlasUVOffset, fragAtlasUVSize);
        
    switch(filteringMode)
    {
        case FILTERING_HARDWARE_NEAREST :   texel = texture(uTexture, uv); break;
        case FILTERING_HARDWARE_LINEAR :    texel = texture(uSmoothTexture, uv); break;
        case FILTERING_CUSTOM_LINEAR :      texel = customLinearFiltering(normalizedUV, currentAtlasUVOffset); break;
    }

    // Sprite blending
    if (fragSpriteBlend > 0 && fragAtlasUVOffset2 != fragAtlasUVOffset)
    {
        uv = computeAtlasUV(normalizedUV, fragAtlasUVOffset2, fragAtlasUVSize);

        vec4 texelPrev;
        switch(filteringMode)
        {
            case FILTERING_HARDWARE_NEAREST :   texelPrev = texture(uTexture, uv); break;
            case FILTERING_HARDWARE_LINEAR :    texelPrev = texture(uSmoothTexture, uv); break;
            case FILTERING_CUSTOM_LINEAR :      texelPrev = customLinearFiltering(normalizedUV, fragAtlasUVOffset2); break;
        }

        // We don't want to blend 2 pixels color if one of them is full transparent, cause its .rgb channels will contain an invalid color.
        texelPrev.rgb = texelPrev.a < 0.01 ? texel.rgb : texelPrev.rgb;
        texel.rgb = texel.a < 0.01 ? texelPrev.rgb : texel.rgb;

        texel = mix(texelPrev, texel, fragSpriteBlend);
    }

    int isSoft = (fragConfig >> CONFIG_BIT_SHIFT_SOFT_PARTICLE) & 1;
    int blendMode = (fragConfig >> CONFIG_BIT_SHIFT_BLEND_MODE) & CONFIG_BIT_MASK_BLEND_MODE;

    // Optimize : avoid having 2 'if', and since we only do a mul here, it's ok to comment out this condition
    //if (texel.a < 0.05) discard;

    color = texel * fragColor;

    // Debug the UV edge detection used to trigger the custom bilinear filtering. 
    // color.rgb = isEdge ? vec3(1,0,0) : color.rgb;

    // only apply the sunLightColor to particles if the color influence is not zero.
    color.rgb *= staticLightColorInfluenceW == 0 ? vec3(1.0) : SunLightColor.rgb;

    int useSoftParticles;

#if DEBUG_TEXTURE
    // Make the edge of the quad fully opaque.
    const float t = 0.05;
    color.a = (fragTexCoords.z > 1 - t) || (fragTexCoords.z < t) || (fragTexCoords.w > 1 - t) || (fragTexCoords.w < t) ? 0.75 : color.a;
    useSoftParticles = 0;
#else
    // Optimize : Avoid writing (and blending !) if the color is almost transparent
    if (color.a < 0.01) discard;
    useSoftParticles = isSoft;
#endif // DEBUG_TEXTURE

#if DEBUG_UVMOTION
    color = flow != vec2(0) ? vec4(flow, 0, 1) : color;
#endif //DEBUG_UVMOTION

    float sceneLinearDepth = 0;

    vec2 screenUV = gl_FragCoord.xy * uCurrentInvViewportSize;

#if !USE_EROSION
    // Soft particles
    if (useSoftParticles == 1 && softParticlesFadeFactor > 0)
    {
        sceneLinearDepth = -texture(uDepthTexture, screenUV).r * FarClip;
        float zDist = fragDepthVS - sceneLinearDepth;
        float zFade = clamp(zDist * softParticlesFadeFactor, 0.0, 1.0);

        color.a *= zFade;
    }

    // Optimize : Avoid writing (and blending !) if the color is almost transparent
    if (color.a < 0.01) discard;
#endif // !USE_EROSION

    if(intersectionHighlightThreshold != 0)
    {
        float depthVS = -texture(uDepthTexture, screenUV).r * FarClip;

        ComputeIntersectionHighlight(depthVS, fragDepthVS, intersectionHighlightThreshold, intersectionHighlightColor, color);
    }

#if USE_FOG
    // fog contribution
    color.rgb = fragFogInfo.a <= 0 ? color.rgb : mix(color.rgb, fragFogInfo.rgb, fragFogInfo.a);
#endif // USE_FOG

#if USE_OIT

    if (uOITMethod == OIT_POIT && isSoft == 0)
    {
        sceneLinearDepth = -texture(uDepthTexture, screenUV).r * FarClip;
    }

    processOIT(color, fragDepthVS, sceneLinearDepth, screenUV, blendMode, outColor0, outColor1);
#else //USE_OIT

    // Blending w/ Premultiplied Alpha :
    // - to get linear blend we can use linear blend w/ premultiplied alpha, i.e. glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA) and RGB = RGB * A
    // - to get additive blend we can use the same setup and simply force the alpha to 0 (cf proof below)
    switch (blendMode)
    {
    case BLEND_MODE_LINEAR:
        color.rgb *= color.a;
        break;
    case BLEND_MODE_ADD:
        color.rgb *= color.a;
        color.a = 0; 
        break;
    case BLEND_MODE_EROSION:
        if(texel.a < (1-fragColor.a)) discard;

        // apply bloom on eroded particle
        bool shadingModeFullBright = staticLightColorInfluenceW == 0;
        color.a = packFragBits(false, shadingModeFullBright, false);
        break;
    }

    outColor0 = color;
#endif //USE_OIT 

#endif //USE_DISTORTION_RT

}

//-------------------------------------------------------------------------------------------------------------------------
// Trails

void trail()
{
    int blendMode = (fragConfig >> CONFIG_BIT_SHIFT_BLEND_MODE) & CONFIG_BIT_MASK_BLEND_MODE;

    int drawId = fragConfig >> CONFIG_BIT_SHIFT_DRAW_ID;
    int address = drawId * SPAWNER_DATA_SIZE;
    float staticLightColorInfluenceW = texelFetch(uSpawnerDataBuffer, address + 4).w;

    vec2 uv;

    if (abs(fragK.z) < 0.001) // Non-quadratic
    {
		float v = -fragK.x / fragK.y;
	    float u;
        
        if ((fragPlaneCoords.z * fragK.y - fragPlaneCoords.w * fragK.x) != 0)
            u = (fragPlaneCoords.x * fragK.y + fragPlaneCoords.y * fragK.x) / (fragPlaneCoords.z * fragK.y - fragPlaneCoords.w * fragK.x);
        else
            u = (fragPlaneCoordsu.x * fragK.y + fragPlaneCoordsu.y * fragK.x) / (fragPlaneCoordsu.z * fragK.y - fragPlaneCoordsu.w * fragK.x);

        uv = vec2( u, v );
    }
	else
    {
        float w = fragK.y * fragK.y - 4.0 * fragK.x * fragK.z;
        if (w < 0.0)
        {
            uv = vec2(-1.0);
        }
        else
        {
            w = sqrt(w);

            float v1 = (-fragK.y - w) / (2.0 * fragK.z);
            float v2 = (-fragK.y + w) / (2.0 * fragK.z);
            float u1 = (fragPlaneCoords.x - fragPlaneCoords.y * v1) / (fragPlaneCoords.z + fragPlaneCoords.w * v1);
            float u2 = (fragPlaneCoords.x - fragPlaneCoords.y * v2) / (fragPlaneCoords.z + fragPlaneCoords.w * v2);

            bool  bool1 = v1 > 0.0 && v1 < 1.1 && u1 > 0.0 && u1 < 1.1;
            bool  bool2 = v2 > 0.0 && v2 < 1.1 && u2 > 0.0 && u2 < 1.1;

            if (!bool1 &&  bool2) uv = vec2(u2, v2);
            else uv = vec2(u1, v1);
        }
    }

    uv.x = fragTexCoords.x + (fragTexCoords.y - fragTexCoords.x) * uv.x;
    uv.y = fragTexCoords.z + (fragTexCoords.w - fragTexCoords.z) * uv.y;

    vec4 texel = texture(uTexture, uv.xy);

    vec4 color = texel * fragColor;

#if USE_DISTORTION_RT
    if (texel.a < 0.05) discard;

    color.rg = computeDistortion(uCurrentInvViewportSize, fragPositionVS, texel, texel.a);
    color.ba = vec2(0,1);

    outColor0 = color;
#else // USE_DISTORTION_RT

    // Optimize : Avoid writing (and blending !) if the color is almost transparent
    if (color.a < 0.01) discard;

    vec2 screenUV = gl_FragCoord.xy * uCurrentInvViewportSize;

    if(intersectionHighlightThreshold != 0)
    {
        float depthVS = -texture(uDepthTexture, screenUV).r * FarClip;

        ComputeIntersectionHighlight(depthVS, fragDepthVS, intersectionHighlightThreshold, intersectionHighlightColor, color);
    }
#if USE_FOG
    // fog contribution
    color.rgb = fragFogInfo.a <= 0 ? color.rgb : mix(color.rgb, fragFogInfo.rgb, fragFogInfo.a);
#endif // USE_FOG

#if USE_OIT
    // Hacky, for POIT only.
    float sceneLinearDepth = 100;
    processOIT(color, fragDepthVS, sceneLinearDepth, screenUV, blendMode, outColor0, outColor1);
#else
    // Blending w/ Premultiplied Alpha :
    // - to get linear blend we can use linear blend w/ premultiplied alpha, i.e. glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA) and RGB = RGB * A
    // - to get additive blend we can use the same setup and simply force the alpha to 0 (cf proof below)
    switch (blendMode)
    {
    case BLEND_MODE_LINEAR:
        color.rgb *= color.a; 
        break;
    case BLEND_MODE_ADD: 
        color.rgb *= color.a;
        color.a = 0; 
        break;
    case BLEND_MODE_EROSION:
        if(texel.a < (1-fragColor.a)) discard;

        // apply bloom on eroded trail
        bool shadingModeFullBright = staticLightColorInfluenceW == 0;
        color.a = packFragBits(false, shadingModeFullBright, false);
        break;
    }

    outColor = color;
#endif // USE_OIT
#endif // USE_DISTORTION_RT
}

//-------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------
// Proof :
// A ) usually, linear blend (w/ post multiplied alpha) :
//  1. do nothing special in the shader (or in the texture data)
//      so FragmentShader.rgb = color.rgb
//         FragmentShader.a = color.a
//  2. and set the hardware to do glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA) which means
//      RenderTarget.rgb = FragmentShader.rgb * FragmentShader.a + RenderTarget.rgb * (1 - FragmentShader.a)
//
// B) but linear blend w/ pre multiplied alpha works too (even better) :
//  1. in the shader you premultiply the color by the alpha (it could be done in the texture instead)
//      so FragmentShader.rgb = color.rgb * color.a 
//         FragmentShader.a = color.a
//  2. and set the hardware to do glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA), which means
//      RenderTarget.rgb = FragmentShader.rgb * 1.0 + RenderTarget.rgb * (1 - FragmentShader.a)
//       but since we modified FragmentShader to be
//          FragmentShader.rgb = color.rgb * color.a,
//              that's exactly the same!
// 
// C) usually, additive blend is done like this
//  1. do nothing special in the shader (or in the texture data)
//      so FragmentShader.rgb = color.rgb
//         FragmentShader.a = color.a
//  2. and set the hardware to do glBlendFunc(GL_SRC_ALPHA, GL_ONE), which means
//      RenderTarget.rgb = FragmentShader.rgb * FragmentShader.a + RenderTarget.rgb * 1.0
// 
// D) but additive blend is also achieved w/ premultiplied alpha like this
//  1. in the shader you premultiply the color by the alpha (it could be done in the texture instead), and then set the alpha to 0
//      so FragmentShader.rgb = color.rgb * color.a 
//         FragmentShader.a = 0
//  2. and set the hardware to do glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA), which means
//      RenderTarget.rgb = FragmentShader.rgb * 1.0 + RenderTarget.rgb * (1 - FragmentShader.a)
//       but since we modified  FragmentShader to be
//          FragmentShader.rgb = color.rgb * color.a
//          FragmentShader.a = 0
//              it's exactly the same !
//      => and that means we can get additive blending using the same glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA) than linear blending !
