#version 330 core

#if SKY_DITHERING
#include "Dithering_inc.glsl"
#endif
#if USE_MOOD_FOG && FOG_DITHERING
#include "Fog_inc.glsl"
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uStarsTexture;

uniform float uStarsOpacity;
uniform vec3 uSunPosition;
uniform float uSunScale;
uniform vec4 uSunGlowColor;
uniform float uMoonOpacity;
uniform float uMoonScale;
uniform vec4 uMoonGlowColor;
uniform vec3 uFogBackColor;
uniform vec4 uDrawSkySunMoonStars;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec3 fragSpherePosition;
in vec2 fragTexCoords;
in vec4 fragBackgroundSunsetColor;

#if USE_MOOD_FOG
 in vec4 fragFogInfo;
#endif // USE_MOOD_FOG

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout (location = 0) out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    const float PiOver2 = 3.14159265 * 0.5;

    float nightLevel = clamp(-uSunPosition.y * 1.5, 0.0, 1.0);

    // Since background & sunset are a nice gradient, and our sky sphere has a decent amount of vertices,
    // it's fine to compute both background & sunset color in the VS for more speed.
    // The HW interpolators will do the job just fine to make sure we have the right color here.
    vec4 color = fragBackgroundSunsetColor;
    //vec4 color = vec4(0, 0, 0, 1);

    if (0.0 == uDrawSkySunMoonStars.x)
    {
        outColor = vec4(uFogBackColor, 1);
        return;
    }

    // Stars 
    // Warning : do not try some other test like if ( nightLevel < 0.05 ) cause it would have no positive impact
    if (1.0 == uDrawSkySunMoonStars.w)
    {
        vec4 starsTexel = texture(uStarsTexture, vec2(fragTexCoords.x * 2.0 - uSunPosition.x * 0.1, fragTexCoords.y * 2.0));
        float starsHeight = clamp((fragSpherePosition.y) * 5, 0.0, 1.0);
        float starsVisibility = nightLevel * starsHeight * uStarsOpacity * starsTexel.a;
        starsVisibility *= pow(abs(fragSpherePosition.y * 1.5), 1);
        color.rgb = mix(color.rgb, starsTexel.rgb, starsVisibility);
    }

    vec3 noise = vec3(0);
#if (USE_MOOD_FOG && FOG_DITHERING) || SKY_DITHERING
    noise = vec3(n2rand_faster(fragTexCoords));
#endif

#if USE_MOOD_FOG
    vec3 smoothMoodFogColor = fragFogInfo.rgb;

#if FOG_DITHERING
    smoothMoodFogColor.rgb = ditherRGB(smoothMoodFogColor.rgb, noise, 255.0);
#endif // FOG_DITHERING

    float moodFogThickness = fragFogInfo.a;

    color.rgb = mix(color.rgb, smoothMoodFogColor, moodFogThickness);
#endif // USE_MOOD_FOG

    // Sun glow
    if (1.0 == uDrawSkySunMoonStars.y)
    {
        float distanceToSun = min(distance(fragSpherePosition, uSunPosition) / (uSunScale * 0.4), 0.99);
        float glowMixFactor = 1 - clamp(sin(distanceToSun * PiOver2), 0.0, 1.0);
        color = mix(color, uSunGlowColor, clamp(uSunGlowColor.a * glowMixFactor * (1 - nightLevel * 2), 0.0, 1.0));
    }
   
    // Moon glow
    if (1.0 == uDrawSkySunMoonStars.z)
    {
        float distanceToMoon = min(distance(fragSpherePosition, -uSunPosition) / (uMoonScale * 0.4), 0.99);
        float moonGlowMixFactor = 1 - clamp(sin(distanceToMoon * PiOver2), 0.0, 1.0);
        color = mix(color, uMoonGlowColor, clamp(uMoonGlowColor.a * moonGlowMixFactor * uMoonOpacity, 0.0, 1.0));
    }

    #if SKY_DITHERING
    color.rgb = ditherRGB(color.rgb, noise, 255.0);
    #endif

    outColor = color;
}
