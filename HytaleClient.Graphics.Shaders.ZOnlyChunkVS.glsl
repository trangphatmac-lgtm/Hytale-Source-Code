#version 330 core

#include "VertexCompression_inc.glsl"

#ifndef USE_DRAW_INSTANCED
#define USE_DRAW_INSTANCED 0
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
#define MAX_CASCADES 4
uniform mat4 uModelMatrix;
uniform mat4 uViewProjectionMatrix[MAX_CASCADES];
uniform float uTime;

// Sun shadow related
uniform vec3 uLightPositions[MAX_CASCADES];
uniform ivec2 uTargetCascades;

#if USE_DRAW_INSTANCED
uniform vec2 uViewportInfos[MAX_CASCADES];
#endif

#if ANIMATED
layout(std140) uniform uboNodeBlock
{
    mat4 nodeData[MAX_NODES_COUNT];
};
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
// (from 4xS16) XYZ: Position packed, W: DoubleSided [15] and BlockId [14-0]
in ivec4 vertPositionAndDoubleSidedAndBlockId;

// (from 4xU32) X: Normal packed [23-0] and NodeId [31-24], Y: Tint [31-8] and Shading [7-6] and Effect [5-0], Z: Glow [23-0] & Sunlight [31-24], W: Billboard [0]
in uvec4 vertDataPacked;

#if ALPHA_TEST
// (from 4xS16) XY: TexCoords, ZW: MaskTexCoords
in vec4 vertTexCoords;
#endif

#define vertPositionPacked                  (vertPositionAndDoubleSidedAndBlockId.xyz)
#define vertDoubleSidedAndBlockId           (vertPositionAndDoubleSidedAndBlockId.w)
#define vertNormalAndNodeId                 (vertDataPacked.x)
#define vertTintColorAndShadingAndEffect    (vertDataPacked.y)
#define vertGlowColorAndSunlight            (vertDataPacked.z)
#define vertBillboard                       (vertDataPacked.w)

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
#if ALPHA_TEST
out vec2 fragTexCoords;
#endif

#if USE_DRAW_INSTANCED
flat out int fragCascadeId;
#endif

//-------------------------------------------------------------------------------------------------------------------------

const int EFFECT_WIND = 15;

bool processBackfaceCulling(vec3 positionWS, vec3 normalWS);

//#define USE_BACKFACE_CULLING 1
//#define USE_DISTANT_BACKFACE_CULLING 0

//-------------------------------------------------------------------------------------------------------------------------


void main()
{
#if USE_COMPRESSED_POSITION
    vec3 fxPosition = vec3(vertPositionPacked) / 511.984375f; // 32767.0f * vec3(64.0f);
#else
    vec3 fxPosition = vec3(vertPositionPacked);
#endif

#if ALPHA_TEST

    fragTexCoords = vertTexCoords.xy;
    
    ivec4 tintColorAndEffectAndShadingMode = unpackIVec4FromUInt(vertTintColorAndShadingAndEffect);

    // Same as MapChunkVS.glsl
    // Extract the id and the custom param (stored in the last bit)
    int effectID = tintColorAndEffectAndShadingMode.w & 31;
    int effectParam = (tintColorAndEffectAndShadingMode.w & 32) >> 5;
    float effectValue = effectID / 255.0;

#if USE_FOLIAGE_CULLING
    if (effectID < EFFECT_WIND)
    {
        // Force Triangle Culling by injecting NaN in a vertex pos.
        const float NaN = sqrt(-1.0);
        gl_Position.w = NaN;
        return;
    }
#endif

    mat4 nodeMatrix = uModelMatrix;

    vec3 normal = unpackNormal(vertNormalAndNodeId);

#if ANIMATED
    int nodeId = int(vertNormalAndNodeId >> 24);
    mat4 animationMatrix = nodeData[nodeId];

    // Extract animated UV offset and clear it from the matrix
    vec2 uvOffset = vec2(animationMatrix[0][3], animationMatrix[1][3]);
    fragTexCoords += uvOffset;
    
    animationMatrix[0][3] = 0;
    animationMatrix[1][3] = 0;
    nodeMatrix *= animationMatrix;

    normal = mat3(nodeMatrix) * normal;
    normal = normalize(normal);

#endif //ANIMATED

    vec4 positionWS = nodeMatrix * vec4(fxPosition, 1.0);

    float distanceToCamera = length(positionWS.xyz);
    

    // Mimick the behaviour from MapChunkVS.glsl
    if (distanceToCamera < 64 && effectID <= EFFECT_WIND)
    {
        // 0. Animation
        const float Pi = 3.14159265;

        // It's important to take the seed out of the computation below and store it in a var, otherwise we will have numeric instabilities
        vec2 seed = floor(vec2(vertPositionPacked.xz));
        float dist = effectValue * 8.0;
        positionWS.x += (cos(seed.x * Pi + uTime * 1.2) + sin(seed.y * Pi + uTime * 1.8) + cos(uTime * 0.6)) * 0.15 * dist;
        positionWS.z += (cos(seed.x * Pi + uTime * 1.6) + sin(seed.y * Pi + uTime * 1.2) + sin(uTime * 0.6)) * 0.10 * dist;
    }

// If we use foliage culling, this can't happen, cause the culling took place a few lines above.
// So keep the code only when we don't use it.
#if !USE_FOLIAGE_CULLING
    // Mimick the behaviour from MapChunkVS.glsl
    if (effectID < EFFECT_WIND)
    {
        // 1. Foliage fading
        // The influence of the wind is entangled w/ the effectID.
        // For the wind attached effect,  "wind influence" is transformed from 0-14 to [0, 2-15], in order to have the same max wind influence as the wind effect.
        float windInfluence = (effectID > 0) ? float(effectID + 1) : float(effectID);
        windInfluence /= 255.0f;

        // This not only brings a smooth apparition of grass, 
        // it also improves the perf considerably by scaling down the number of fragment with their distance to camera.
        // This means less interpolator power, less fragment power, less overdraw, less writing into the GBuffer.
        if (effectParam > 0 && effectID < EFFECT_WIND) positionWS.y -= clamp(windInfluence * length(positionWS) * 0.25, 0.0, 1.0);
    }
#endif // !USE_FOLIAGE_CULLING

#else // ALPHA_TESTED
    vec4 positionWS = uModelMatrix * vec4(fxPosition, 1.0);
#endif // ALPHA_TEST

#if USE_DRAW_INSTANCED
    int cascadeId = uTargetCascades.x + gl_InstanceID;
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

#if USE_BACKFACE_CULLING

#if USE_DRAW_INSTANCED
    vec3 RenderOrigin = uLightPositions[cascadeId];
#else
    vec3 RenderOrigin = uLightPositions[0];
#endif

    // The info "DoubleSided" is encoded in sign of that field.
    bool doubleSided = vertDoubleSidedAndBlockId < 0;

#if USE_DISTANT_BACKFACE_CULLING
    // Force backface culling when geometry is not close (or not in the first cascade).
    
    // We draw shadow casters w/ position relative to the camera (i.e. camera pos = 0), so positionWS.xyz is = (positionWS.xyz - uCameraPosition).
    bool isFarAway = distanceToCamera > DISTANT_BACKFACE_CULLING_DISTANCE && uTargetCascades.x > 0;
    bool useCulling = isFarAway || !doubleSided;
#else // USE_DISTANT_BACKFACE_CULLING
    bool useCulling = !doubleSided;
#endif // USE_DISTANT_BACKFACE_CULLING

    if (useCulling && processBackfaceCulling(positionWS.xyz - RenderOrigin, normal))
    {
        // Force Triangle Culling by injecting NaN in a vertex pos.
        const float NaN = sqrt(-1.0);
        gl_Position.w = NaN;
    }
#endif
}


bool processBackfaceCulling(vec3 positionWS, vec3 normalWS)
{
#if USE_BACKFACE_CULLING
#if SHADOW_VERSION
    // Use an epsilon instead of 0 to account for precision issues that would cull "too early" side facing triangles.
    const float epsilon = 0.1;
#else
    // Use the same epsilon used as in MapChunkVS.glsl.
    const float epsilon = 0.01;
#endif // SHADOW_VERSION
    return dot(normalWS, normalize(positionWS)) > epsilon;
#else 
    return false;
#endif //USE_BACKFACE_CULLING
}
