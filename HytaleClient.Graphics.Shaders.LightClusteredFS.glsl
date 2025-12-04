#version 330

#include "Deferred_inc.glsl"
#include "Reconstruction_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;
uniform mat4 uProjectionMatrix;
uniform float uFarClip;
uniform float uUseLBufferCompression;

// Clustered Lighting (required by LightCluster_inc.glsl)
uniform vec3 uLightGridResolution;
uniform vec3 uZSlicesParams;

#include "LightCluster_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;
in vec3 fragFrustumRay;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out vec3 outColor;

//-------------------------------------------------------------------------------------------------------------------------

// If blend mode = GL.MAX, 3.0f gives the best results ( = closest to what we had with forward rendering)
const float DynamicLightMultiplier = 3.0f;

vec4 ProjInfo = vec4( 2.0f / uProjectionMatrix[0][0],
                      -2.0f / uProjectionMatrix[1][1],
                      ( 1.0f - uProjectionMatrix[0][2]) / uProjectionMatrix[0][0],
                      ( 1.0f + uProjectionMatrix[1][2]) / uProjectionMatrix[1][1]);

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec2 texCoords = fragTexCoords;//gl_FragCoord.xy * uInvScreenSize;
    
    float depth = texture(uDepthTexture, texCoords).r;
//    float depth = texelFetch(uDepthTexture, ivec2(gl_FragCoord.xy), 0).r;

    // Optimization : abort if "far away" (~sky)
    // NB : this is depending on texture read obviously, and will cause latency... 
    // maybe try to do this later in the code to make instruction unit busy instead of waiting idle here ?
    if (depth > 0.9999999) discard;

#if USE_LINEAR_Z
    vec3 positionFromCameraVS = fragFrustumRay * depth;
#else
    vec3 positionFromCameraVS = PositionFromDepthHW(depth, texCoords, uProjectionMatrix, ProjInfo);
#endif // USE_LINEAR_Z

    float depthVS = depth * uFarClip;
    int lightIndex; 
    int pointLightCount;
    fetchClusterData(texCoords, depthVS, lightIndex, pointLightCount);

    if (pointLightCount == 0) discard;

    vec3 dynamicLightColor = vec3(0);

    // Point lights
#if DEBUG
    for (int pl = 0; pl < pointLightCount; pl++) 
    {
        vec4 LightPosSize;
        vec3 Color;
        takeNextPointLight(lightIndex, LightPosSize, Color);
        dynamicLightColor = Color.rgb;
    }
#else //DEBUG

#if 0
    vec4 lightPosSize[4];
    vec3 lightColor[4];

    for (int pl = 0; pl < pointLightCount; pl+=4) 
    {
#if USE_DIRECT_ACCESS
        lightPosSize[0] = texelFetch(uLightBufferTexture, lightIndex).xyzw;
        lightColor[0] = texelFetch(uLightBufferTexture, lightIndex + 1).xyz;
        lightPosSize[1] = texelFetch(uLightBufferTexture, lightIndex + 2).xyzw;
        lightColor[1] = texelFetch(uLightBufferTexture, lightIndex + 3).xyz;
        lightPosSize[2] = texelFetch(uLightBufferTexture, lightIndex + 4).xyzw;
        lightColor[2] = texelFetch(uLightBufferTexture, lightIndex + 5).xyz;
        lightPosSize[3] = texelFetch(uLightBufferTexture, lightIndex + 6).xyzw;
        lightColor[3] = texelFetch(uLightBufferTexture, lightIndex + 7).xyz;
        lightIndex += 8;
#else
        ivec4 index;
        index.x = int(texelFetch(uLightIndicesBufferTexture, lightIndex));
        index.y = int(texelFetch(uLightIndicesBufferTexture, lightIndex + 1));
        index.z = int(texelFetch(uLightIndicesBufferTexture, lightIndex + 2));
        index.w = int(texelFetch(uLightIndicesBufferTexture, lightIndex + 3));
        lightIndex += 4;

        lightPosSize[0] = PointLights[index.x].xyzw;
        lightColor[0] = PointLights[index.x + 1].rgb;
        lightPosSize[1] = PointLights[index.y].xyzw;
        lightColor[1] = PointLights[index.y + 1].rgb;
        lightPosSize[2] = PointLights[index.z].xyzw;
        lightColor[2] = PointLights[index.z + 1].rgb;
        lightPosSize[3] = PointLights[index.w].xyzw;
        lightColor[3] = PointLights[index.w + 1].rgb;
#endif //USE_DIRECT_ACCESS

        // Compute pointlight here ...
        float distanceToLight[4];
        distanceToLight[0] = distance(lightPosSize[0].xyz, positionFromCameraVS);
        distanceToLight[1] = distance(lightPosSize[1].xyz, positionFromCameraVS);
        distanceToLight[2] = distance(lightPosSize[2].xyz, positionFromCameraVS);
        distanceToLight[3] = distance(lightPosSize[3].xyz, positionFromCameraVS);

        for (int i = 0; i < 4; i++)
        {
            if (distanceToLight[i] > lightPosSize[i].w) continue;

            // for compatibility with Hytale current lighting code
            float fixedDistanceToLight = distanceToLight[i] * 0.1f;

            vec3 currentDynamicLightColor = vec3(0.0);
            currentDynamicLightColor.r = getChannelLightNEW(fixedDistanceToLight, lightColor[i].r);
            currentDynamicLightColor.g = getChannelLightNEW(fixedDistanceToLight, lightColor[i].g);
            currentDynamicLightColor.b = getChannelLightNEW(fixedDistanceToLight, lightColor[i].b);

            dynamicLightColor = max (dynamicLightColor, currentDynamicLightColor);
        }

        float lightAccumulated = min(dynamicLightColor.r, min(dynamicLightColor.g, dynamicLightColor.b));
        //float lightAccumulated = dot(vec3(1), dynamicLightColor);

        // Warning : take into account the fact that there will be a DynamicLightMultiplier later in this shader
        const float EarlyOutThreshold = 1.0 / DynamicLightMultiplier;
        if (lightAccumulated > EarlyOutThreshold) break;
    }
#else //0
    // Warning : take into account the fact that there will be a DynamicLightMultiplier later in this shader
    const float EarlyOutThreshold = 1.0 / DynamicLightMultiplier;

    dynamicLightColor = computeClusteredLighting(positionFromCameraVS.xyz, pointLightCount, lightIndex, EarlyOutThreshold); 
#endif //0

#endif //DEBUG

    // Blending is NOT free, so discard if there is nothing to add !
    //if ( dot( dynamicLightColor, vec3(1)) <= 0.001 ) discard;

    if (uUseLBufferCompression > 0)
    {
        outColor.rg = encodeLightCompressed(min(vec3(1), dynamicLightColor * DynamicLightMultiplier), ivec2(gl_FragCoord.xy));
    }
    else
    {
        outColor.rgb = dynamicLightColor * DynamicLightMultiplier;
    }
}

