#ifndef RECONSTRUCTION_INCLUDE
#define RECONSTRUCTION_INCLUDE

float GetDepthNDCFromDepthHW(float depthHW)
{
    // Tested. Valid.
    return depthHW * 2.0 - 1.0;
}

float GetDepthHWFromDepthNDC(float depthNDC)
{
    // Tested. Valid.
    return depthNDC * 0.5 + 0.5;
}

float GetLinearDepthFromDepthHW(float depthHW, mat4 projectionMatrix)
{
    // Tested. Valid.
    float depthNDC = GetDepthNDCFromDepthHW(depthHW);
    return projectionMatrix[3][2] / ( depthNDC + projectionMatrix[2][2]);
}

float GetDepthNDCFromLinearDepth(float linearDepth, mat4 projectionMatrix)
{
    // Tested. Valid.
    return (projectionMatrix[3][2] - linearDepth * projectionMatrix[2][2]) / linearDepth;
}

float GetDepthHWFromLinearDepth(float linearDepth, mat4 projectionMatrix)
{
    // Tested. Valid.
    float depthNDC = GetDepthNDCFromLinearDepth(linearDepth, projectionMatrix);
    return GetDepthHWFromDepthNDC(depthNDC);
}

// For information : 
// vec4 ProjInfo = vec4(    -2.0f / uProjectionMatrix[0][0],
//                           -2.0f / uProjectionMatrix[1][1],
//                           ( 1.0f - uProjectionMatrix[0][2]) / uProjectionMatrix[0][0],
//                           ( 1.0f + uProjectionMatrix[1][2]) / uProjectionMatrix[1][1]);
vec3 PositionFromLinearDepth(vec2 screenUV, float linearDepth, vec4 projInfo)
{
    return vec3( (screenUV * projInfo.xy + projInfo.zw) * linearDepth, linearDepth);
}

vec3 PositionFromDepthHW(float depthHW, vec2 screenUV, mat4 projectionMatrix, vec4 projInfo)
{
    float linearDepth = -GetLinearDepthFromDepthHW(depthHW, projectionMatrix);
    return PositionFromLinearDepth(screenUV, linearDepth, projInfo);
}

vec3 PositionFromLinearDepth(vec2 texCoords, vec3 frustumRay, sampler2D linearDepthTexture)
{
    // Uses a trick from Crytek.
    // Original link is unavailable, since it's an old thing. 
    // Some info here https://mynameismjp.wordpress.com/2009/03/10/reconstructing-position-from-depth/
    return frustumRay * texture(linearDepthTexture, texCoords).r;
}

vec3 PositionFromDepth(float depthHW, vec2 texCoords, mat4 reconstructMatrix)
{
    // Back to NDC space
    vec3 posNDC = vec3(texCoords.xy, depthHW) * 2.0 - 1.0;

    // Back to World space (if reconstruct is the inverse of ViewProjection)
    // - minus perspective divide
    vec4 vProjectedPos = vec4(posNDC, 1.0f);
    vec4 vPosition = reconstructMatrix * vProjectedPos;

    // Perspective divide - now we are in requested space!
    return vPosition.xyz / vPosition.w;
}

#endif //RECONSTRUCTION_INCLUDE
