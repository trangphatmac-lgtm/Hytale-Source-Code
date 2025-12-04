#ifndef LIGHT_CLUSTER_INCLUDE
#define LIGHT_CLUSTER_INCLUDE

#ifndef USE_DIRECT_ACCESS
#define USE_DIRECT_ACCESS 1
#endif

#ifndef USE_CUSTOM_Z_DISTRIBUTION
#define USE_CUSTOM_Z_DISTRIBUTION 1
#endif

// WARNING:
// Make sure you have included this file after having declared the following uniforms:
//
// uniform vec3 uLightGridResolution;
// uniform vec3 uZSlicesParams;

#if USE_DIRECT_ACCESS
#define LIGHT_SAMPLER_TYPE samplerBuffer
#define uLightBufferTexture uLightIndicesOrDataBufferTexture
#else
#define LIGHT_SAMPLER_TYPE isamplerBuffer
#define uLightIndicesBufferTexture uLightIndicesOrDataBufferTexture
#endif
uniform isampler3D uLightGridTexture;
uniform LIGHT_SAMPLER_TYPE uLightIndicesOrDataBufferTexture;

#define MAX_LIGHTS 1024
layout(std140) uniform uboPointLightBlock
{
    vec4 PointLights[MAX_LIGHTS];
};

float getLightGridDepthSlice(float z, float lightGridResZ, float paramX, float paramZ)
{
#if USE_CUSTOM_Z_DISTRIBUTION
    float slicesCount = lightGridResZ - 1;
#else
    float slicesCount = lightGridResZ;
#endif

    float slice = float(slicesCount * log(z / paramX) * paramZ);

#if USE_CUSTOM_Z_DISTRIBUTION
    slice = z < paramX ? 0 : slice + 1;
#endif

    return slice;
}

void fetchClusterData(vec2 screenUV, float depthVS, isampler3D lightGridTexture, float lightGridResZ, float paramX, float paramZ, out int lightIndex, out int pointLightCount)
{
    float zSlice = getLightGridDepthSlice(depthVS, lightGridResZ, paramX, paramZ);

    // Fetch light list
    ivec2 lightClusterData = texture(lightGridTexture, vec3(screenUV, zSlice / lightGridResZ)).xy;       

    // Extract parameters
    lightIndex = lightClusterData.x << 3 | (lightClusterData.y & 7); 
    pointLightCount = (lightClusterData.y >> 3);
}

// Local specialized version of the one available in the include file.
void fetchClusterData(vec2 screenUV, float depthVS, out int lightIndex, out int pointLightCount)
{
    fetchClusterData(screenUV, depthVS, uLightGridTexture, uLightGridResolution.z, uZSlicesParams.x, uZSlicesParams.z, lightIndex, pointLightCount);
}

void takeNextPointLight(inout int lightIndex, out vec4 lightPosSize, out vec3 lightColor)
{
#if USE_DIRECT_ACCESS
    lightPosSize = texelFetch(uLightBufferTexture, lightIndex).xyzw;
    lightColor = texelFetch(uLightBufferTexture, lightIndex + 1).xyz;
    lightIndex += 2;
#else
    uint index = uint(texelFetch(uLightIndicesBufferTexture, lightIndex));
    lightPosSize = PointLights[index].xyzw;
    lightColor = PointLights[index + uint(1)].rgb;
    lightIndex++;
#endif //USE_DIRECT_ACCESS
}


float getChannelLightNEW(float distanceToLight, float maxChannelLight) 
{
    return pow(max(0.0, 1.0 - distanceToLight / maxChannelLight), 1.5f) * maxChannelLight * 0.8; 
}

vec3 computeClusteredLighting(vec3 positionVS, int pointLightCount, int lightIndex, float earlyOutThreshold)
{
    vec3 dynamicLightColor = vec3(0,0,0);

    for (int pl = 0; pl < pointLightCount; pl++) 
    {
        vec4 LightPosSize;
        vec3 Color;
        takeNextPointLight(lightIndex, LightPosSize, Color);

        // Compute pointlight here ...
        float distanceToLight = distance(LightPosSize.xyz, positionVS.xyz);

        if (distanceToLight > LightPosSize.w) continue;

        // for compatibility with Hytale current lighting code
        float fixedDistanceToLight = distanceToLight  * 0.1f;

        vec3 currentDynamicLightColor = vec3(0.0);
        currentDynamicLightColor.r = getChannelLightNEW(fixedDistanceToLight, Color.r);
        currentDynamicLightColor.g = getChannelLightNEW(fixedDistanceToLight, Color.g);
        currentDynamicLightColor.b = getChannelLightNEW(fixedDistanceToLight, Color.b);

        dynamicLightColor = max (dynamicLightColor, currentDynamicLightColor);

        float lightAccumulated = min(dynamicLightColor.r, min(dynamicLightColor.g, dynamicLightColor.b));
        //float lightAccumulated = dot(vec3(1), dynamicLightColor);

        if (lightAccumulated > earlyOutThreshold) break;
    }

    return dynamicLightColor;
}

#endif //LIGHT_CLUSTER_INCLUDE
