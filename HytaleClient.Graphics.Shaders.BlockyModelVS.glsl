#version 330 core

// Global scene data
#include "SceneData_inc.glsl"
#include "ModelVFX_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
layout(std140) uniform uboNodeBlock
{
    mat4 nodeData[MAX_NODES_COUNT];
};


#if USE_SCENE_DATA_OVERRIDE
uniform mat4 uViewProjectionMatrix;
#define ViewProjectionMatrix uViewProjectionMatrix
#endif //USE_SCENE_DATA_OVERRIDE

#if USE_ENTITY_DATA_BUFFER
// x = Offset to the first (distortion) data
// y = draw data id
uniform ivec2 uDrawId;

uniform samplerBuffer uEntityDataBuffer;
const int ENTITY_DATA_SIZE = 8;
// ENTITY_WORLDMATRIX_OFFSET = 0;
// ENTITY_BLOCKLIGHTCOLOR = 4;
// ENTITY_BOTTOMTINT_MODELVFXANIMATIONPROGRESS = 5;
// ENTITY_TOPTINT_MODELVFXID = 6;
// ENTITY_INVMODELHEIGHT = 7;

const int ENTITY_DISTORTION_DATA_SIZE = 5;
// ENTITY_WORLDMATRIX_OFFSET = 0;
// ENTITY_INVMODELHEIGHT_FXPACKEDPARAM_ANIMPROGRESS = 4;

uniform samplerBuffer uModelVFXDataBuffer;
const int MODELVFX_DATA_SIZE = 4;
// MODELVFX_HIGHLIGHT = 0;
// MODELVFX_NOISEPARAMS = 1;
// MODELVFX_POSTCOLOR = 2;
// MODEVFX_PACKEDPARAMS_INVMODELHEIGHT = 3;
#else // !USE_ENTITY_DATA_BUFFER

// Per instance data
uniform mat4 uModelMatrix;
uniform vec3 uBottomTint = vec3(0);
uniform vec3 uTopTint = vec3(0);
uniform float uInvModelHeight = 1.0 / (1.80 * 64); // Player Height * DrawScale -- FIXME!

#define modelMatrix uModelMatrix
#define bottomTint uBottomTint
#define topTint uTopTint
#define invModelHeight uInvModelHeight

#endif // USE_ENTITY_DATA_BUFFER

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in int vertNodeIndex;
in int vertAtlasIndexAndShadingModeAndGradientId; // layout : atlasIndex [8], shadingMode[2], gradientId[8], padding [14]
in vec3 vertPosition;
in vec2 vertTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec3 fragPositionWS;
out vec2 fragTexCoords;
out vec4 fragGradientColor;
flat out int fragAtlasIndexAndShadingModeAndGradientId;

#if USE_ENTITY_DATA_BUFFER
#if USE_DISTORTION_RT
out vec3 fragPositionVS;
flat out vec3 fragInvModelHeightAnimationProgressId;
#else // !USE_DISTORTION_RT
flat out vec4 fragStaticLightColor;
flat out vec4 fragInvModelHeightAnimationProgressId;
#endif //USE_DISTORTION_RT
#endif //USE_ENTITY_DATA_BUFFER

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    mat4 nodeMatrix = nodeData[vertNodeIndex];

    // Extract animated UV offset and clear it from the matrix
    vec2 uvOffset = vec2(nodeMatrix[0][3], nodeMatrix[1][3]);
    nodeMatrix[0][3] = 0;
    nodeMatrix[1][3] = 0;

    vec4 positionOS = nodeMatrix * vec4(vertPosition, 1.0);

#if USE_ENTITY_DATA_BUFFER
#if USE_DISTORTION_RT

    int offsetToDistortionData = ENTITY_DATA_SIZE * uDrawId.x;
    int address = offsetToDistortionData + ENTITY_DISTORTION_DATA_SIZE * uDrawId.y;

    mat4 modelMatrix;
    modelMatrix[0] = texelFetch(uEntityDataBuffer, address + 0);
    modelMatrix[1] = texelFetch(uEntityDataBuffer, address + 1);
    modelMatrix[2] = texelFetch(uEntityDataBuffer, address + 2);
    modelMatrix[3] = texelFetch(uEntityDataBuffer, address + 3);

    vec3 modelVFXAnimationProgressModelVFXIdAndInvModelHeight = texelFetch(uEntityDataBuffer, address + 4).xyz;
    float invModelHeight = modelVFXAnimationProgressModelVFXIdAndInvModelHeight.z;
    float modelVFXId = modelVFXAnimationProgressModelVFXIdAndInvModelHeight.y;

    float modelVFXAnimationProgress = modelVFXId == -1 ? 0.0 :modelVFXAnimationProgressModelVFXIdAndInvModelHeight.x;
    vec3 bottomTint = vec3(0);
    vec3 topTint = vec3(0);
    fragInvModelHeightAnimationProgressId = vec3(invModelHeight, modelVFXAnimationProgress, modelVFXId);
#else // !USE_DISTORTION_RT

    int address = ENTITY_DATA_SIZE * uDrawId.y;

    mat4 modelMatrix;
    modelMatrix[0] = texelFetch(uEntityDataBuffer, address + 0);
    modelMatrix[1] = texelFetch(uEntityDataBuffer, address + 1);
    modelMatrix[2] = texelFetch(uEntityDataBuffer, address + 2);
    modelMatrix[3] = texelFetch(uEntityDataBuffer, address + 3);
    
    vec4 blockLightColor = texelFetch(uEntityDataBuffer, address + 4);
    vec4 bottomTintAndAnimationProgress = texelFetch(uEntityDataBuffer, address + 5);
    vec4 topTintAndModelVFXId = texelFetch(uEntityDataBuffer, address + 6);
    vec2 invModelHeightUseDithering = texelFetch(uEntityDataBuffer, address + 7).xy;
    vec3 bottomTint = bottomTintAndAnimationProgress.rgb;
    vec3 topTint = topTintAndModelVFXId.rgb;
    float modelVFXId = topTintAndModelVFXId.a;
    float modelVFXAnimationProgress = modelVFXId == -1 ? 0.0 :bottomTintAndAnimationProgress.w;
    float invModelHeight = invModelHeightUseDithering.x;
    float useDithering = invModelHeightUseDithering.y;

    fragInvModelHeightAnimationProgressId = vec4(invModelHeight, modelVFXAnimationProgress, modelVFXId, useDithering);
    fragStaticLightColor = blockLightColor;

#endif // USE_DISTORTION_RT
#endif // USE_ENTITY_DATA_BUFFER


    vec4 positionWS = modelMatrix * positionOS;
    fragPositionWS = positionWS.xyz;

    fragTexCoords = vertTexCoords + uvOffset;
    fragAtlasIndexAndShadingModeAndGradientId = vertAtlasIndexAndShadingModeAndGradientId;
    fragGradientColor.rgb = mix(bottomTint, topTint, clamp(positionOS.y * invModelHeight, 0.0, 1.0));

    // Height factor
    fragGradientColor.a = clamp(positionOS.y * invModelHeight, 0.0, 1.0);

#if FIRST_PERSON_VIEW
    // in first person the view matrix is the identity,
    // so we can just multiply by the projection matrix
    gl_Position = FirstPersonProjectionMatrix * positionWS;

    // To avoid first person weapon to intersect w/ world geometry and to have it match the Particles rendered in first person,
    // we modify the Z in the vertex shader (rather than in the fragment shader, which would disable the early-Z hw optimization).
    gl_Position.z *= 0.55;
#else // FIRST_PERSON_VIEW
#if USE_DISTORTION_RT
    fragPositionVS = (ViewMatrix * positionWS).xyz;
#endif
    gl_Position = ViewProjectionMatrix * positionWS;
#endif // FIRST_PERSON_VIEW

}
