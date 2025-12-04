#version 330 core
#include "Fog_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture;
uniform vec3 uColor;
uniform float uOpacity = 1.0;

uniform vec3 uCameraPosition;
uniform vec3 uFogColor;
uniform vec4 uFogParams;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec3 fragPosition;
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout (location = 0) out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec4 texel = texture(uTexture, fragTexCoords);
    if (texel.a == 0.0) discard;

    outColor = texel;
    outColor.rgb *= uColor;
    outColor.a *= uOpacity;

    outColor.rgb = applyDistantFog(fragPosition - uCameraPosition, outColor.rgb, uFogColor, uFogParams);
}
