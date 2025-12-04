#version 330 core

#include "Fog_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture;

uniform vec3 uPosition;
uniform vec3 uFogColor;
uniform vec4 uFogParams;
uniform float uFillThreshold;
uniform float uFillBlurThreshold;
uniform float uOutlineThreshold;
uniform float uOutlineBlurThreshold;
uniform vec2 uOutlineOffset;
uniform float uOpacity = 1.0f;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;
in vec4 fillColor;
in vec4 outlineColor;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout (location = 0) out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    float fillDistance = (texture(uTexture, fragTexCoords).r - 0.5) * 2.0;
    float fillAlpha = 1.0 - smoothstep(uFillThreshold, uFillBlurThreshold, fillDistance);

    float outlineAlpha = 0.0;

    if (uOutlineThreshold > uFillBlurThreshold)
    {
        float outlineDistance = (texture(uTexture, fragTexCoords - uOutlineOffset).r - 0.5) * 2.0;
        outlineAlpha = (1.0 - smoothstep(uOutlineThreshold, uOutlineBlurThreshold, outlineDistance)) * outlineColor.a;
    }

    float overallAlpha = fillAlpha + (1.0 - fillAlpha) * outlineAlpha;
    if (overallAlpha * fillColor.a == 0.0) discard;

    vec3 overallColor = mix(outlineColor.rgb, fillColor.rgb, fillAlpha / overallAlpha);
    outColor = vec4(overallColor, overallAlpha * fillColor.a * uOpacity);
    
    outColor.rgb = applyDistantFog(uPosition, outColor.rgb, uFogColor, uFogParams);
}
