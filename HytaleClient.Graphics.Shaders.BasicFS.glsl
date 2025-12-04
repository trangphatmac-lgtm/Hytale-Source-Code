#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture;
uniform int uMipLevel = 0;

#if USE_COLOR_AND_OPACITY
uniform vec3 uColor;
uniform float uOpacity = 1.0;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
#if WRITE_ALPHA
layout (location = 0) out vec4 outColor;
#else
layout(location = 0) out vec3 outColor;
#endif

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec4 texel = textureLod(uTexture, fragTexCoords, uMipLevel);

#if USE_DISCARD
    if (texel.a == 0.0) discard;
#endif

#if USE_COLOR_AND_OPACITY
    vec4 color = vec4(texel.rgb * uColor.rgb, texel.a * uOpacity);
#else
    vec4 color = texel.rgba;
#endif

    outColor.rgb = color.rgb;

#if WRITE_ALPHA
    outColor.a = color.a;
#endif
}
