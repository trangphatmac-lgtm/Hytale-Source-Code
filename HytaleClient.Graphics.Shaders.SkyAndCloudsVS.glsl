#version 330 core

#include "Fog_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform mat4 uMVPMatrix;

#if USE_MOOD_FOG
uniform vec3 uCameraPosition;
uniform sampler2D uSunOcclusionHistory;
uniform vec3 uFogMoodParams;

#define uFogHeightFalloff uFogMoodParams.x
#define uFogGlobalDensity uFogMoodParams.y
#define uFogHeightDensityAtViewer uFogMoodParams.z
#endif

#if USE_MOOD_FOG || SKY_VERSION
uniform vec3 uSunPosition;
#endif

#if SKY_VERSION
uniform vec4 uDrawSkySunMoonStars;
uniform vec4 uTopGradientColor;
uniform vec4 uSunsetColor;
vec4 sunsetColor = vec4(uSunsetColor.rgb, 1.0);

uniform vec3 uFogFrontColor;
uniform vec3 uFogBackColor;
#endif

#if !SKY_VERSION && USE_MOOD_FOG
uniform vec3 uFogFrontColor;
uniform vec3 uFogBackColor;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertPosition;
in vec2 vertTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec3 fragSpherePosition;
out vec2 fragTexCoords;

#if SKY_VERSION
out vec4 fragBackgroundSunsetColor;
#endif

#if USE_MOOD_FOG
out vec4 fragFogInfo;
#endif

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    fragSpherePosition = normalize(vertPosition.xyz);

    gl_Position = uMVPMatrix * vec4(vertPosition, 1.0);

    fragTexCoords = vertTexCoords;

#if USE_MOOD_FOG
    const float sphereRadius = 1000.0;

#if SKY_VERSION
    vec3 positionWS = fragSpherePosition.xyz * vec3(sphereRadius);
#else // SKY_VERSION
    const vec3 cloudsTranslation = vec3(0, -sphereRadius * 0.5, 0);
    vec3 positionWS = fragSpherePosition.xyz * vec3(sphereRadius) + cloudsTranslation;
#endif // SKY_VERSION
    
    fragFogInfo = computeFogForSky(positionWS, uCameraPosition, uSunPosition,
                                uFogFrontColor.rgb, uFogBackColor.rgb, uFogMoodParams, uFogHeightDensityAtViewer,
                                uSunOcclusionHistory);

#endif // USE_MOOD_FOG

#if SKY_VERSION

    vec4 color = vec4(0,0,0,1);
    
    if (1.0 == uDrawSkySunMoonStars.x)
    {
        // Use a blend of front & back color for fog, according to proximity to the sun	
        vec3 cameraToFragment = fragSpherePosition;
        color.rgb = getFogColor(uTopGradientColor.rgb, uFogFrontColor.rgb, uFogBackColor.rgb, cameraToFragment, uSunPosition);

        // Sunset color
        vec3 offsetFromSun = fragSpherePosition - uSunPosition;
        offsetFromSun.y *= 6.0;
        offsetFromSun.x *= 2.0;
        float sunsetFactor = 1.0 - length(offsetFromSun) * 0.5;

        float sunsetMixFactor = clamp(sunsetFactor * uSunsetColor.a, 0.0, 1.0);
        color = mix(color, sunsetColor, sunsetMixFactor);	
    }

    fragBackgroundSunsetColor = color;

#endif
}
