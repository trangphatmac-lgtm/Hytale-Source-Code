#version 330 core
#pragma optionNV(strict on)

#include "Deferred_inc.glsl"
#include "Dithering_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
#if USE_DITHERING
uniform float uTime;
#endif // USE_DITHERING

#if FULLBRIGHT || POW
uniform sampler2D uSceneColorTexture;
uniform float uPower;
#endif

#if POW
uniform vec2 uPowerOptions;
#define uPowIntensity uPowerOptions.x
#define uPowPower uPowerOptions.y
#endif

#if SUN_OR_MOON
uniform sampler2D uSunMoonTexture;
uniform float uSunMoonIntensity;
uniform int uUseSunOrMoon;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec3 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
    outColor.rgb = vec3(0);

    #if POW || FULLBRIGHT
    vec4 color = texture(uSceneColorTexture, fragTexCoords.xy).rgba;
    #endif

    #if SUN_OR_MOON
    vec3 sun = vec3(0);
    if(uUseSunOrMoon == 1) 
    {
        sun = texture(uSunMoonTexture, fragTexCoords.xy).rgb;
    }
    #endif

    #if FULLBRIGHT
    // read the renderConfig bits from SceneColor.a
    float renderConfig = color.a;
    bool applyBloom;
    unpackFragBit1(renderConfig, applyBloom);
    outColor.rgb = applyBloom ? pow(color.rgb, vec3(uPower)) : vec3(0);
    #endif

    #if POW
    outColor.rgb += pow(color.rgb, vec3(uPowPower)) * uPowIntensity;
    #endif

    #if SUN_OR_MOON
    outColor.rgb += sun * uSunMoonIntensity;
    #endif

    #if USE_DITHERING
    vec3 noise = vec3(n2rand_faster_animated(fragTexCoords, uTime));

    outColor.rgb = ditherRGB(outColor.rgb, noise, 1000.0);
    #endif // USE_DITHERING
}
