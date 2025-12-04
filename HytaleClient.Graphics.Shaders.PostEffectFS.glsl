#version 330 core
#extension GL_ARB_gpu_shader5 : enable

#define PIXELSIZE uPixelSize
#include "Fallback_inc.glsl"

// Additional information for fallback code within FXAA_inc.glsl
#define FXAA_PIXELSIZE uPixelSize
// Hytale FXAA config : Parameters are chosen based on the informations provided in FXAA_inc.glsl
#if USE_FXAA_HIGH_QUALITY
#define FXAA_QUALITY__PRESET 39
#else
#define FXAA_QUALITY__PRESET 20
#endif
#include "FXAA_inc.glsl"

uniform vec2 uPixelSize;
#include "DepthOfField_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;
uniform sampler2D uDistortionTexture;

uniform float uTime;
uniform float uDistortionAmplitude = 0.0;
uniform float uDistortionFrequency = 0.0;

#if DEBUG_TILES
uniform vec2 uDebugTileResolution;
#endif // DEBUG_TILES

uniform vec2 uColorBrightnessContrast;
uniform float uColorSaturation = 1.0;
uniform vec3 uColorFilter = vec3(1.0);
#define uColorBrightness uColorBrightnessContrast.x
#define uColorContrast   uColorBrightnessContrast.y

#if DOF_VERSION != 3
// dof version 1
uniform sampler2D uDepthTexture;

#if USE_LINEAR_Z
uniform float uFarClip = 1024.0f;
#else
uniform mat4 uProjectionMatrix;
#endif

uniform float uNearBlurry;
uniform float uNearSharp;
uniform float uFarSharp;
uniform float uFarBlurry;
#endif // DOF_VERSION == 1 || DOF_VERSION == 2 || DOF_VERSION == 3

#if DOF_VERSION == 0
uniform float uNearBlurMax;
uniform float uFarBlurMax;
#endif
#if DOF_VERSION == 1
// dof version 2
uniform sampler2D uBlurTexture;
#endif
#if DOF_VERSION == 2
// dof version 2bis
uniform sampler2D uNearBlurTexture;
uniform sampler2D uFarBlurTexture;
#endif
#if DOF_VERSION == 3
// dof version 3
uniform sampler2D uCoCTexture;
uniform sampler2D uCoCLowResTexture;
uniform sampler2D uNearCoCBlurredLowResTexture;
uniform sampler2D uNearFieldLowResTexture;
uniform sampler2D uFarFieldLowResTexture;
uniform sampler2D uSceneColorLowResTexturePoint;
#endif

#if USE_BLOOM
uniform sampler2D uBloomTexture;
uniform int uApplyBloom;
#endif

#if USE_VOL_SUNSHAFT
uniform sampler2D uVolumetricSunshaftTexture;
uniform float uVolumetricSunshaftStrength;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

// Used to calculate overall brightness
// See http://stackoverflow.com/a/596241 and https://www.w3.org/TR/AERT#color-contrast
const vec3 BrightnessCoefficients = vec3(0.299, 0.587, 0.114);

const float Pi = 3.14159265;

// Cost on Intel GPU :
// full screen & sanity check (aka worst case): 0.7ms
// full screen & no sanity check: 0.5ms
// common scene & sanity check : 0.4ms
#define USE_DISTORTION_SANITY_CHECK 1

vec4 FXAA(sampler2D texture, vec2 texCoords, vec2 pixelSize)
{
#if USE_FXAA_HIGH_QUALITY
    //const float fxaaQualityEdgeThreshold = 0.15;
    const float fxaaQualityEdgeThreshold = 0.05;
#else
    //const float fxaaQualityEdgeThreshold = 0.33;
    const float fxaaQualityEdgeThreshold = 0.15;
#endif

    // NB : when using Screendoor Transparency, fxaaQualitySubpix should be 1.0
    const float fxaaQualitySubpix = 0.0;//1.0;
    const float fxaaQualityEdgeThresholdMin = 0.0;

    return FxaaPixelShader(texture, texCoords, pixelSize, fxaaQualitySubpix, fxaaQualityEdgeThreshold, fxaaQualityEdgeThresholdMin);
}

vec3 sharpen(in sampler2D tex, in vec2 coords, vec3 pixelColor, float strength)
{
    float dx = uPixelSize.x;
    float dy = uPixelSize.y;
    vec3 sum = vec3(0.0);
    sum += -strength * texture(tex, coords + vec2( -dx , 0.0)).rgb;
    sum += -strength * texture(tex, coords + vec2( 0.0, -dy)).rgb;
    sum += (4.0 * strength + 1.0) * pixelColor;
    sum += -strength * texture(tex, coords + vec2( 0.0, dy)).rgb;
    sum += -strength * texture(tex, coords + vec2( dx , 0.0)).rgb;

    return sum;
}

vec3 computeDepthOfField(out bool isSharp)
{
    vec3 dofColor;

    #if DOF_VERSION != 3
    #if USE_LINEAR_Z 
        float depth = texture(uDepthTexture, fragTexCoords.xy).r;
        float linearDepth = depth * uFarClip;
    #else
        // TODO use a function that will be brought by ssao 
        float depth = texture(uDepthTexture, fragTexCoords.xy).r * 2.0 - 1.0;
        float linearDepth = getLinearDepthFromDepthNDC(depth, uProjectionMatrix);
    #endif // USE_LINEAR_Z
    #endif

    #if DOF_VERSION == 0
        dofColor = DepthOfFieldNaive(uColorTexture, fragTexCoords, uPixelSize, linearDepth, uNearBlurry, uNearSharp, uFarSharp, uFarBlurry, uNearBlurMax, uFarBlurMax, isSharp);
    #else 
    #if DOF_VERSION == 1
        dofColor = DepthOfFieldFast(uColorTexture, uBlurTexture, fragTexCoords, linearDepth, uNearBlurry, uNearSharp, uFarSharp, uFarBlurry, isSharp);
    #else
    #if DOF_VERSION == 2
        dofColor = DepthOfFieldFastBis(uColorTexture, uNearBlurTexture, uFarBlurTexture, fragTexCoords, linearDepth, uNearBlurry, uNearSharp, uFarSharp, uFarBlurry, isSharp);
    #else
    #if DOF_VERSION == 3
        dofColor = DepthOfFieldAdvanced(uSceneColorLowResTexturePoint, uCoCTexture, uCoCLowResTexture, uNearCoCBlurredLowResTexture, uNearFieldLowResTexture, uFarFieldLowResTexture, fragTexCoords, uPixelSize, isSharp);
    #endif
    #endif
    #endif
    #endif
    return dofColor;
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec4 color;
    vec2 texCoords = fragTexCoords;

#if USE_DISTORTION
    vec2 distortion = texture(uDistortionTexture, texCoords).rg;
#if USE_DISTORTION_SANITY_CHECK
    // sanity check - a bit expensive, but no choice!
    if (vec2(0.0) != texture(uDistortionTexture, texCoords + distortion).rg) texCoords += distortion;
#else
    texCoords += distortion;
#endif // USE_DISTORTION_SANITY_CHECK
#endif // USE_DISTORTION

    // Distortion effect
    // FIXME : later, this will be removed from here and integrated in a refraction / distortion rendering pass
    if (uDistortionAmplitude > 0.0)
    {
        vec2 distort = vec2(
            cos(texCoords.x * uDistortionFrequency * Pi + uTime),
            sin(texCoords.y * uDistortionFrequency * Pi + uTime));

        texCoords = clamp(texCoords + distort * uPixelSize * uDistortionAmplitude, 0.0, 1.0);
    }

    // used to disable FXAA on depth of field's blurry area
    bool isSharp = true;
    
#if USE_BLOOM  && (SUN_FB_POW || USE_SUNSHAFT)
    vec3 bloomColor = vec3(0);
    if(uApplyBloom == 1)
    {
        bloomColor = texture(uBloomTexture, texCoords.xy).rgb;
    }
#endif
// -------------------------------------------- DOF --------------------------------------------
    #if USE_DOF

        vec3 dofColor = computeDepthOfField(isSharp);

        color.rgb = isSharp ? color.rgb : dofColor;

    #endif // USE_DOF

// -------------------------------------------- FXAA --------------------------------------------
    #if USE_FXAA
        color.rgb = isSharp ? FXAA(uColorTexture, texCoords, uPixelSize).rgb : color.rgb;
    #if USE_SHARPEN
        // Especially useful after FXAA, to compensate the blur produced
        const float sharpenStrength = SHARPEN_STRENGTH;
        color.rgb = isSharp ? sharpen(uColorTexture, texCoords, color.rgb, sharpenStrength) : color.rgb;
    #endif // USE_SHARPEN
    #else
        color.rgb = isSharp ? texture(uColorTexture, texCoords).rgb : color.rgb;
    #endif  


    // Color filter
    float colorIntensity = dot(color.rgb, BrightnessCoefficients);
    vec3 colorGray = vec3(colorIntensity);

#if DISCARD_DARK
    // This is useful when applying FXAA on the 3D part of a Menu :
    // you render your 3D scene with a black clear color in a RT,
    // apply FXAA (what we are doing now !),
    // and then draw your RT on top of the Menu.
    if (colorIntensity == 0) discard;
#endif

    //outColor = color;
    outColor.rgb = mix(colorGray.rgb, color.rgb, uColorSaturation) * uColorFilter.rgb;
    
    // -------------------------------------------- BLOOM --------------------------------------------
    #if USE_BLOOM  && (SUN_FB_POW || USE_SUNSHAFT)
    outColor.rgb += bloomColor;
    #endif

    #if USE_VOL_SUNSHAFT
    vec3 sunshaft = texture(uVolumetricSunshaftTexture, texCoords.xy).rgb; 
    outColor.rgb += sunshaft * uVolumetricSunshaftStrength;
    #endif

    outColor.a = 1.0;

    float brightness = uColorBrightness; //0.05;//-0.02;//
    outColor.rgb = clamp(outColor.rgb + vec3(brightness), vec3(0), vec3(1));

    float contrast = uColorContrast; //0.9;//1.05;//
    outColor.rgb = (outColor.rgb - vec3(0.5)) * max(0, contrast) + vec3(0.5);

    // Debug view of potential Tiles

#if DEBUG_TILES
    //const ivec2 tileSize = ivec2(120, 125);
    ivec2 tileSize = ivec2(vec2(textureSize(uColorTexture, 0)) / uDebugTileResolution.xy);
    vec2 v = floor(mod(gl_FragCoord.xy, tileSize.xy));
    if(v.x == 0 || v.y == 0) outColor =  vec4(1);
#endif
}
