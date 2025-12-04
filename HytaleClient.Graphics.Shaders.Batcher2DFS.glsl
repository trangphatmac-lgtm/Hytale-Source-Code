#version 150 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture;
uniform sampler2D uMaskTexture;
uniform sampler2DArray uFontTexture;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
layout(origin_upper_left) in vec4 gl_FragCoord;
in vec2 fragTexCoords;
flat in vec4 fragScissor;
flat in vec4 fragMaskTextureArea;
flat in vec4 fragMaskBounds;
in vec4 fragFillColor;
in vec4 fragOutlineColor;
in vec4 fragSDFSettings;
flat in uint fragFontId;

#define scissorWidth (fragScissor.z)
#define scissorHeight (fragScissor.w)

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec4 maskTint = vec4(1,1,1,1);

    if (fragMaskBounds.w != 0.0)
    {
        vec2 maskCoords = vec2((gl_FragCoord.x + 0.5 - fragMaskBounds.x) / fragMaskBounds.z, (gl_FragCoord.y + 0.5 - fragMaskBounds.y) / fragMaskBounds.w);
        if (maskCoords.x < 0.0 || maskCoords.y < 0.0 || maskCoords.x >= 1.0 || maskCoords.y >= 1.0) discard;

        maskTint = texture(uMaskTexture, fragMaskTextureArea.xy + vec2(maskCoords.x, maskCoords.y) * fragMaskTextureArea.zw);
        if (maskTint.a == 0.0) discard;
    }

    if (gl_FragCoord.x < fragScissor.x || gl_FragCoord.x > fragScissor.x + scissorWidth || gl_FragCoord.y < fragScissor.y || gl_FragCoord.y > fragScissor.y + scissorHeight)
    {
        discard;
    }

    if (fragSDFSettings == vec4(0, 0, 0, 0))
    {
        vec4 texel = texture(uTexture, fragTexCoords);
        if (texel.a == 0.0) discard;
        
        outColor = texel * fragFillColor * maskTint;
    }
    else
    {
        float fillThreshold = fragSDFSettings.r;
        float fillBlurAmount = fragSDFSettings.g;
        float outlineThreshold = fragSDFSettings.b;
        float outlineBlurAmount = fragSDFSettings.a;

        float fillDistance = (texture(uFontTexture, vec3(fragTexCoords, fragFontId)).r - 0.5) * 2.0;
        float fillAlpha = 1.0 - smoothstep(fillThreshold - fillBlurAmount / 2.0, fillThreshold + fillBlurAmount / 2.0, fillDistance);

        float outlineAlpha = 0.0;

        if (outlineThreshold > fillThreshold)
        {
            float outlineDistance = (texture(uFontTexture, vec3(fragTexCoords /* - outlineOffset */, fragFontId)).r - 0.5) * 2.0;
            outlineAlpha = (1.0 - smoothstep(outlineThreshold - outlineBlurAmount / 2.0, outlineThreshold + outlineBlurAmount / 2.0, outlineDistance)) * fragOutlineColor.a;
        }

        float overallAlpha = fillAlpha + (1.0 - fillAlpha) * outlineAlpha;
        if (overallAlpha * fragFillColor.a == 0.0) discard;

        vec3 overallColor = mix(fragOutlineColor.rgb, fragFillColor.rgb, fillAlpha / overallAlpha);
        outColor = vec4(overallColor, overallAlpha * fragFillColor.a) * maskTint;
    }
}
