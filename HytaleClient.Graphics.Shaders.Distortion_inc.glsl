#ifndef DISTORTION_INCLUDE
#define DISTORTION_INCLUDE

vec2 computeDistortion(vec2 invViewportSize, vec3 positionVS, vec4 texel, float strength)
{
    vec2 distortion = texel.rg * vec2(2.0) - vec2(1.0);
    float refractionStrength = 75.0 / positionVS.z;
    distortion *= invViewportSize * refractionStrength;

    distortion *= texel.a * strength;

    return distortion;
}

#endif //DISTORTION_INCLUDE
