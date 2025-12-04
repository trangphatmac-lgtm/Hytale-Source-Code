#ifndef SAMPLING_INCLUDE
#define SAMPLING_INCLUDE

vec2 computeAtlasUV(vec2 normalizedUV, vec2 atlasUVOffset, vec2 atlasUVSize)
{
   return normalizedUV * atlasUVSize + atlasUVOffset;
}

vec2 manualWrapNormalizedUV(vec2 normalizedUV)
{
    // Wrap the texture UV (using repeat)
    return mod(normalizedUV, vec2(1.0));
}

vec2 manualWrapAtlasUV(vec2 uv, vec2 atlasUVOffset, vec2 atlasUVSize)
{
    // Wrap the texture UV (using repeat)
    return mod(uv.xy - atlasUVOffset, atlasUVSize) + atlasUVOffset;
}

void computeLinearFilteringTaps(vec2 uv, vec2 texelSize, out vec2 uv0, out vec2 uv1, out vec2 uv2, out vec2 uv3, out vec2 f)
{
    // Shift the uv half a texel backwards to get the right weight f.
    uv -= 0.5 * texelSize;

    f = fract(uv / texelSize);
    
    // Wrap manually the texture UV (using repeat)
    uv0 = manualWrapNormalizedUV(uv);
    uv1 = manualWrapNormalizedUV(uv + vec2(texelSize.x, 0));
    uv2 = manualWrapNormalizedUV(uv + vec2(0, texelSize.y));
    uv3 = manualWrapNormalizedUV(uv + vec2(texelSize.x, texelSize.y));
}

vec4 customLinearFilteringWithNormalizedUV(sampler2D tex, vec2 normalizedUV, vec2 texelSize, vec2 atlasUVOffset, vec2 atlasUVSize)
{
    vec2 uv0, uv1, uv2, uv3, f;
    computeLinearFilteringTaps(normalizedUV, texelSize, uv0, uv1, uv2, uv3, f);

    uv0 = computeAtlasUV(uv0, atlasUVOffset, atlasUVSize);
    uv1 = computeAtlasUV(uv1, atlasUVOffset, atlasUVSize);
    uv2 = computeAtlasUV(uv2, atlasUVOffset, atlasUVSize);
    uv3 = computeAtlasUV(uv3, atlasUVOffset, atlasUVSize);

    vec4 color0 = texture(tex, uv0);
    vec4 color1 = texture(tex, uv1);
    vec4 color2 = texture(tex, uv2);
    vec4 color3 = texture(tex, uv3);
    
    return mix(mix(color0, color1, f.x), mix(color2, color3, f.x), f.y);
}


vec4 customLinearFilteringWithAtlasUV(sampler2D tex, vec2 uv, vec2 texelSize)
{
    vec2 uv0, uv1, uv2, uv3, f;
    computeLinearFilteringTaps(uv, texelSize, uv0, uv1, uv2, uv3, f);

    vec4 color0 = texture(tex, uv0);
    vec4 color1 = texture(tex, uv1);
    vec4 color2 = texture(tex, uv2);
    vec4 color3 = texture(tex, uv3);
    
    return mix(mix(color0, color1, f.x), mix(color2, color3, f.x), f.y);
}

#endif //SAMPLING_INCLUDE
