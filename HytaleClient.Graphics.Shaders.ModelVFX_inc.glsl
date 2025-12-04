#ifndef ENTITY_VFX_INCLUDE
#define ENTITY_VFX_INCLUDE

// We want ComputeIntersectionHighlight() from that include.
#include "FX_inc.glsl"

const int FX_NONE = 0;
const int FX_ARMOR = 1;
const int FX_GHOST = 2;
const int FX_GHOST_BY_NIGHT = 3;

bool ComputeTestVFX(int vfxType, vec2 texCoords, vec3 posWS, float time, sampler2D noiseTexture, float heightFactor, float staticLuminosity, vec3 highlightColor, inout vec4 color, inout vec4 light, out bool hasHighlight)
{
    int visualFX = vfxType;
    vec2 vfxUV;

    float noiseValue = 0;
    float threshold = 0;
    float animationTime = time;

    bool tryDiscard = true;
    hasHighlight = false;

    if (visualFX != FX_NONE)
    {
        switch (visualFX)
        {
            case FX_ARMOR:
                vfxUV = posWS.xy * 0.15;
//              vfxUV = texCoords * vec2(32);
                tryDiscard = false;
                noiseValue = texture(noiseTexture, vfxUV).r;
                noiseValue += heightFactor * 0.25;
                threshold = clamp((sin(animationTime) * 0.5 + 0.5), 0, 1);
            break;

            case FX_GHOST :
            case FX_GHOST_BY_NIGHT:
                vfxUV = texCoords * vec2(16);
                noiseValue = texture(noiseTexture, vfxUV).r;
                noiseValue *= heightFactor;
                threshold = 0.2 + sin(animationTime * 2) * 0.02;

                if (visualFX == FX_GHOST_BY_NIGHT)
                {
                    threshold *= (1 - staticLuminosity);
                    color.rgb = mix(highlightColor.rgb, color.rgb, staticLuminosity);
                }
            break;
        }

        float highlightOffset = 0.01;
        hasHighlight = abs(noiseValue - threshold) < highlightOffset;
        color.rgb = hasHighlight ? highlightColor : color.rgb;
    }

    if (visualFX == FX_GHOST_BY_NIGHT)
    {
        light.rgb = mix(highlightColor * (1 - heightFactor) * 1.5, light.rgb, staticLuminosity);
    }

    // must discard ?
    return tryDiscard && noiseValue < threshold;
}

bool ComputeModelVFX(vec2 texCoords, sampler2D noiseTexture, float invModelHeight, float heightFactor, float disappearFactor, int direction, vec3 highlightColor, bool useBloom, bool useProgressiveHighlight, float highlightThickness, vec2 noiseScale, vec2 noiseScrollSpeed, vec4 postColor, inout vec4 color, out bool hasHighlight, float time)
{
    const int DisappearConst = 0; // default
    const int DisappearBottomUp = 1;
    const int DisappearTopDown = 2;
    const int DisappearToCenter = 3;
    const int DisappearFromCenter = 4;

    float mask = heightFactor;
    float highlightOffset = highlightThickness * 0.01;

    // use a similar noise on any entities ( big or small)
    vec2 uvFactor = noiseScale * 100 * invModelHeight;
    vec2 vfxUV = texCoords * uvFactor + noiseScrollSpeed * time;

    float noiseValue = texture(noiseTexture, vfxUV).r;

    float minNoiseValue = 0.23f;
    float maxNoiseValue = 0.71f;

    // remap the noise value between 0 and 1 because on the noise texture used we only go from 0.23 to 0.71 approximately.
    noiseValue = (noiseValue - minNoiseValue) / (maxNoiseValue - minNoiseValue);

    // and then between 0 and 0.5.
    noiseValue *= 0.5;

    switch (direction)
    {
        case DisappearConst:
            noiseValue *= 2;
            break;
        case DisappearBottomUp:
            noiseValue += mask * 0.5;
            break;
        case DisappearTopDown:
            noiseValue += (1 - mask) * 0.5;
            break;
        case DisappearToCenter:
            mask = 4 * (mask - 0.5) * (mask - 0.5);     // easier to read: 4 * (x - 0.5)^2
            noiseValue += -0.5 * mask + 0.5;            // faster than: noiseValue += (1 - mask) * 0.5;
            break;
        case DisappearFromCenter:
            mask = 4 * (mask - 0.5) * (mask - 0.5);     // easier to read: 4 * (x - 0.5)^2
            noiseValue += 0.5 * mask;
            break;
        default:
            break;
    }

    float threshold = disappearFactor;

    hasHighlight = abs(noiseValue - threshold) < highlightOffset;

    float ratio = hasHighlight ? 1: 0;
    ratio = useProgressiveHighlight ? abs(clamp(threshold / noiseValue, 0.0, 1.0)) : ratio;
    color.rgb = mix(color.rgb, highlightColor, ratio);

    bool vfxDiscard = noiseValue < threshold;

    bool usePostColor = postColor.a > 0.0f;
    vec3 finalPostColor = mix(color.rgb, postColor.rgb, postColor.a);
    color.rgb = (usePostColor && vfxDiscard && !hasHighlight) ? finalPostColor.rgb : color.rgb;


    hasHighlight = hasHighlight && useBloom;

    return vfxDiscard;
}

const int CONFIG_BIT_MASK_DIRECTION = ((1 << 3) - 1);
const int CONFIG_BIT_MASK_SWITCHTO = ((1 << 2) - 1);

const int CONFIG_BIT_SHIFT_SWITCHTO = 3;
const int CONFIG_BIT_SHIFT_BLOOM = 5;
const int CONFIG_BIT_SHIFT_PROGRESSIVE_HIGHLIGHT = 6;

// packedData = direction (3bits), switchTo (2bits), useBloom (1bit), useProgressiveHighlight (1bit)
void unpackModelVFXData(int packedData, out int direction, out int switchTo, out int useBloom, out int useProgressiveHighlight)
{
    direction = packedData & CONFIG_BIT_MASK_DIRECTION;
    switchTo = ((packedData >> CONFIG_BIT_SHIFT_SWITCHTO) & CONFIG_BIT_MASK_SWITCHTO);
    useBloom = ((packedData >> CONFIG_BIT_SHIFT_BLOOM) & 1);
    useProgressiveHighlight = ((packedData >> CONFIG_BIT_SHIFT_PROGRESSIVE_HIGHLIGHT) & 1);
}

#endif //ENTITY_VFX_INCLUDE
