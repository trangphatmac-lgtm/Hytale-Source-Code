#ifndef DITHERING_INCLUDE
#define DITHERING_INCLUDE

#include "Random_inc.glsl"

vec3 applyNoise(vec3 c, vec3 ditherNoise, float strength)
{
    return c + ditherNoise / strength;
}

vec3 srgb2lin(vec3 c)
{
    return vec3(c.rgb * c.rgb);
}

vec3 lin2srgb(vec3 c)
{
    return vec3(sqrt(c.rgb));
}

vec3 ditherRGB(vec3 color, vec3 noise, float strength)
{
    return srgb2lin(applyNoise(lin2srgb(color), noise, strength));
}

#endif //DITHERING_INCLUDE
