#version 330 core

#ifndef USE_DRAW_INSTANCED
#define USE_DRAW_INSTANCED 0
#endif

#ifndef MAX_NODES_COUNT
#define MAX_NODES_COUNT 256
#endif

#include "ModelVFX_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
layout(std140) uniform uboNodeBlock
{
    mat4 nodeData[MAX_NODES_COUNT];
};

uniform samplerBuffer uEntityShadowMapDataBuffer;
const int ENTITY_SHADOWMAP_DATA_SIZE = 6;
// ENTITY_WORLDMATRIX_OFFSET = 0;
// ENTITY_TARGETCASCADES_INVMODELHEIGHT_ANIMATIONPROGRESS = 4;
// ENTITY_MODELVFXID = 5;

uniform int uDrawId;
uniform mat4 uModelMatrix;

#define MAX_CASCADES 4
uniform mat4 uViewProjectionMatrix[MAX_CASCADES];

#if USE_BIAS_METHOD_2
uniform mat4 uViewMatrix[MAX_CASCADES];
#endif

#if USE_DRAW_INSTANCED
uniform vec2 uViewportInfos[MAX_CASCADES];
#endif

#if USE_MODEL_VFX
uniform float uInvModelHeight = 1.0 / (1.80 * 64); // Player Height * DrawScale -- FIXME!
uniform int uModelVFXId;
uniform samplerBuffer uModelVFXDataBuffer;
uniform float uModelVFXAnimationProgress;
const int MODELVFX_DATA_SIZE = 4;
// MODELVFX_HIGHLIGHT = 0;
// MODELVFX_NOISEPARAMS = 1;
// MODELVFX_POSTCOLOR = 2;
// MODEVFX_PACKEDPARAMS_INVMODELHEIGHT = 3;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertPosition;
in vec2 vertTexCoords;
in int vertNodeIndex;
in int vertAtlasIndexAndShadingModeAndGradientId;   // layout : atlasIndex [8], shadingMode[2], gradientId[8], padding [14]

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
flat out int fragAtlasIndex;

#if USE_BIAS_METHOD_2
out vec3 fragPositionVS;
#endif

#if USE_DRAW_INSTANCED
flat out int fragCascadeId;
#endif

#if USE_MODEL_VFX
out vec3 fragTexCoords;
flat out ivec2 fragModelVFXUnpackedData;
flat out vec4 fragModelVFXNoiseParams;
flat out vec2 fragInvModelHeightAnimationProgress;
#else
out vec2 fragTexCoords;
#endif

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    mat4 nodeMatrix = nodeData[vertNodeIndex];

    const int maskAtlasIndex = ((1 << 8) - 1);
    int atlasIndex = vertAtlasIndexAndShadingModeAndGradientId & maskAtlasIndex;

    int address = ENTITY_SHADOWMAP_DATA_SIZE * uDrawId;

    mat4 modelMatrix;
    modelMatrix[0] = texelFetch(uEntityShadowMapDataBuffer, address + 0);
    modelMatrix[1] = texelFetch(uEntityShadowMapDataBuffer, address + 1);
    modelMatrix[2] = texelFetch(uEntityShadowMapDataBuffer, address + 2);
    modelMatrix[3] = texelFetch(uEntityShadowMapDataBuffer, address + 3);

    modelMatrix = uDrawId == -1 ? uModelMatrix : modelMatrix;
    #if USE_DRAW_INSTANCED
    vec2 targetCascades = texelFetch(uEntityShadowMapDataBuffer, address + 4).xy;
    #endif // USE_DRAW_INSTANCED

    #if USE_MODEL_VFX
    vec3 modelVFXIdInvModelHeightAnimationProgress = texelFetch(uEntityShadowMapDataBuffer, address + 5).xyz;

    int modelVFXId = uDrawId == -1 ? uModelVFXId : int(modelVFXIdInvModelHeightAnimationProgress.x);
    float invModelHeight = uDrawId == -1 ? uInvModelHeight : modelVFXIdInvModelHeightAnimationProgress.y;
    float animationProgress = uDrawId == -1 ? uModelVFXAnimationProgress : modelVFXIdInvModelHeightAnimationProgress.z;

    fragInvModelHeightAnimationProgress = vec2(invModelHeight, animationProgress);
    #endif // USE_MODEL_VFX

    // Extract animated UV offset and clear it from the matrix
    vec2 uvOffset = vec2(nodeMatrix[0][3], nodeMatrix[1][3]);
    nodeMatrix[0][3] = 0;
    nodeMatrix[1][3] = 0;

    vec4 positionOS = nodeMatrix * vec4(vertPosition, 1.0);
    vec4 positionWS = modelMatrix * positionOS;
    
#if USE_BIAS_METHOD_2
    vec4 positionVS = uViewMatrix * positionWS;
    fragPositionVS = positionVS.xyz;
#endif

#if USE_DRAW_INSTANCED
    int cascadeId = targetCascades.x + gl_InstanceID;
    fragCascadeId = cascadeId;

    mat4 viewProjectionMatrix = uViewProjectionMatrix[cascadeId];
    gl_Position = viewProjectionMatrix * positionWS;
    float scale = uViewportInfos[0].x;

    gl_Position.x = gl_Position.x * 0.5 + 0.5;
    gl_Position.x = gl_Position.x * scale + cascadeId * scale;
    gl_Position.x = gl_Position.x * 2 - 1;
#else
    mat4 viewProjectionMatrix = uViewProjectionMatrix[0];
    gl_Position = viewProjectionMatrix * positionWS;
#endif

    fragTexCoords.xy = vertTexCoords + uvOffset;

#if USE_MODEL_VFX
    // Height factor
    fragTexCoords.z = clamp(positionOS.y * invModelHeight, 0.0, 1.0);

    // Get modelVFX data
    fragModelVFXUnpackedData = ivec2(0);
    fragModelVFXNoiseParams = vec4(0);
    if(modelVFXId != -1)
    {
        int modelVFXAddress = MODELVFX_DATA_SIZE * modelVFXId;

        vec4 noiseParams = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 1);
        float modelVFXPackedParams = texelFetch(uModelVFXDataBuffer, modelVFXAddress + 3).x;

        int direction, switchTo, useBloom, progressiveHighlight;

        unpackModelVFXData(int(modelVFXPackedParams), direction, switchTo, useBloom, progressiveHighlight);

        // for shadows we only need to send direction and switchTo.
        fragModelVFXUnpackedData = ivec2(direction, switchTo);
        fragModelVFXNoiseParams = noiseParams;
    }
#endif

    fragAtlasIndex = atlasIndex;
}
