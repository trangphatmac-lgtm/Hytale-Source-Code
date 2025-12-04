#version 330 core

#include "Reconstruction_inc.glsl"
#include "Deferred_inc.glsl"

// NDAL : define is still required for compilation
#define MAX_LIGHTS 8
#include "Light_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;

uniform mat4 uProjectionMatrix;
uniform float uFarClip;
uniform vec2 uInvScreenSize;
uniform vec3 uColor;
uniform vec4 uPositionSize;

uniform int uUseLightGroup;
uniform int uTransferMethod;
uniform vec4 uGlobalLightPositionSizes[MAX_DEFERRED_LIGHTS];
uniform vec3 uGlobalLightColors[MAX_DEFERRED_LIGHTS];
uniform ivec2 uLightGroup;

// NB : kept here for tests
//struct LightData
//{
//vec4 uPositionSizeXXX;
//vec3 uColorXXX;
//}
//uniform LightData uGlobalLightData[MAX_DEFERRED_LIGHTS];

uniform int uDebug = 0;

#define uPosition uPositionSize.xyz
#define uSize uPositionSize.w

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------

#if USE_LINEAR_Z
in vec3 fragPositionVS;
#endif // USE_LINEAR_Z

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
#if USE_LBUFFER_COMPRESSION
layout(location = 0) out vec2 outColor;
#else
layout(location = 0) out vec3 outColor;
#endif

//-------------------------------------------------------------------------------------------------------------------------

// If blend mode = GL.MAX, 3.0f gives the best results ( = closest to what we had with forward rendering)
const float LightMultiplier = 3.0f;

vec4 ProjInfo = vec4(    -2.0f / uProjectionMatrix[0][0],
                          -2.0f / uProjectionMatrix[1][1],
                          ( 1.0f - uProjectionMatrix[0][2]) / uProjectionMatrix[0][0],
                          ( 1.0f + uProjectionMatrix[1][2]) / uProjectionMatrix[1][1]);

float getChannelLightNEW(float distanceToLight, float maxChannelLight, float invMaxChannelLight) 
{
    return pow(max(0.0, 1.0 - distanceToLight * invMaxChannelLight), 1.5f) * maxChannelLight * 0.8; 
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    if (uDebug == 1)
	{
#if USE_LBUFFER_COMPRESSION
        outColor.rg = encodeLightCompressed(uColor.rgb, ivec2(gl_FragCoord.xy));
#else
        outColor.rgb = uColor.rgb;
#endif
        return;
    }

    vec2 texCoords =  gl_FragCoord.xy * uInvScreenSize;

    float depth = texture(uDepthTexture, texCoords).r;

    // Optimization : abort if "far away" (~sky)
    // NB : this is depending on texture read obviously, and will cause latency... 
    // maybe try to do this later in the code to make instruction unit busy instead of waiting idle here ?
    if (depth > 0.9999999) discard;

#if USE_LINEAR_Z
        vec3 viewRay = vec3(fragPositionVS.xy * (uFarClip / -fragPositionVS.z), -uFarClip);
        vec3 positionFromCameraVS = viewRay * depth;
#else
        vec3 positionFromCameraVS = PositionFromDepthHW(depth, texCoords, uProjectionMatrix, ProjInfo);
#endif // USE_LINEAR_Z

    vec3 dynamicLightColor = vec3(0.0);

    if (uUseLightGroup == 1)
    {
        if (uTransferMethod == 1 || uTransferMethod == 0)
        {
            // Optimization : 
            // When there are "enough lights" in the same group, 
            // if the dynamic color is already close to its max (i.e. vec3(1) )
            // all future work will be useless, so try to avoid it
            if (uLightGroup.y > 6)
            {
                for( int i = 0; i < uLightGroup.y; i++)
                {
                    int lightID = uLightGroup.x + i;
                    vec4 lightPosSize = uGlobalLightPositionSizes[lightID];

                    float distanceToLight = distance(lightPosSize.xyz, positionFromCameraVS);

                    if (distanceToLight > lightPosSize.w) continue;
            
                    // for compatibility with Hytale current lighting code
                    float fixedDistanceToLight = distanceToLight  * 0.1f;
            
                    vec3 lightColor = uGlobalLightColors[lightID];
            
                    vec3 currentDynamicLightColor = vec3(0.0);

                    currentDynamicLightColor.r = getChannelLight(fixedDistanceToLight, lightColor.r);
                    currentDynamicLightColor.g = getChannelLight(fixedDistanceToLight, lightColor.g);
                    currentDynamicLightColor.b = getChannelLight(fixedDistanceToLight, lightColor.b);

                    dynamicLightColor = max (dynamicLightColor, currentDynamicLightColor);

                    float lightAccumulated = min(dynamicLightColor.r, min(dynamicLightColor.g, dynamicLightColor.b));
                    //float lightAccumulated = dot(vec3(1), dynamicLightColor);

                    // Warning : take into account the fact that there will be a LightMultiplier later in this shader
                    const float EarlyOutThreshold = 1.0 / LightMultiplier;
                    if (lightAccumulated > EarlyOutThreshold) break;
                }

            }
            else
            {
                for( int i = 0; i < uLightGroup.y; i++)
                {
                    int lightID = uLightGroup.x + i;
                    vec4 lightPosSize = uGlobalLightPositionSizes[lightID];

                    float distanceToLight = distance(lightPosSize.xyz, positionFromCameraVS);

                    if (distanceToLight > lightPosSize.w) continue;
            
                    // for compatibility with Hytale current lighting code
                    float fixedDistanceToLight = distanceToLight  * 0.1f;
            
                    vec3 lightColor = uGlobalLightColors[lightID];
            
                    vec3 currentDynamicLightColor = vec3(0.0);

                    currentDynamicLightColor.r = getChannelLight(fixedDistanceToLight, lightColor.r);
                    currentDynamicLightColor.g = getChannelLight(fixedDistanceToLight, lightColor.g);
                    currentDynamicLightColor.b = getChannelLight(fixedDistanceToLight, lightColor.b);

                    dynamicLightColor = max (dynamicLightColor, currentDynamicLightColor);
                }
            }
        }        
    }
    else
    {
        float distanceToLight = distance(uPosition, positionFromCameraVS);

        if (distanceToLight > uSize) discard;

        // for compatibility with Hytale current lighting code
        float fixedDistanceToLight = distanceToLight  * 0.1f;

        dynamicLightColor = vec3(0.0);

        dynamicLightColor.r += getChannelLight(fixedDistanceToLight, uColor.r);
        dynamicLightColor.g += getChannelLight(fixedDistanceToLight, uColor.g);
        dynamicLightColor.b += getChannelLight(fixedDistanceToLight, uColor.b);
        dynamicLightColor = min(dynamicLightColor, vec3(1.0));

        // NB : The code below is kept for the next PR !

        // TODO : use such an attenuation factor in light equation !
        float atten = 0.8 - ((distanceToLight * distanceToLight) / (uSize * uSize));
        //float atten = pow(1 - distanceToLight / uSize, 2);
        //float atten = 0.8 - ((distanceToLight * distanceToLight) * fragLightEquationFactor);
        //outColor.rgb = uColor.rgb * 1.5 * atten * atten * atten * LightMultiplier;// * 0.65;
        //outColor.rgb = vec3(1) * 1.5 * atten * atten * atten * LightMultiplier;// * 0.65;
        //outColor.rgb = uColor.rgb * atten * LightMultiplier * 0.65;

        //float atten = 3 - 3 / dist;
        //outColor.rgb = vec3(dist / 10000);

        //outColor.a = dot(vec3(1.0),dynamicLightColor) * 0.3333333 ;
        //outColor.a = max(dynamicLightColor.r, max(dynamicLightColor.g, dynamicLightColor.b));//, 1.0f;
    } 

    // Blending is NOT free, so discard if there is nothing to add !
    if ( dot( dynamicLightColor, vec3(1)) == 0 ) discard;

	// outColor.a = 1.0f;
#if USE_LBUFFER_COMPRESSION
    outColor.rg = encodeLightCompressed(min(vec3(1), dynamicLightColor * LightMultiplier), ivec2(gl_FragCoord.xy));
#else
    outColor.rgb = dynamicLightColor * LightMultiplier ;
#endif
}
