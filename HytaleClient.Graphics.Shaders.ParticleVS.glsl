#version 330 core

// Global scene data
#include "SceneData_inc.glsl"
#include "Quaternion_inc.glsl"
#include "Fog_inc.glsl"
#include "Random_inc.glsl"
#include "CurveType_inc.glsl"

#ifndef USE_SUNLIGHT_SHADING
#define USE_SUNLIGHT_SHADING 0
#endif

#if USE_CLUSTERED_LIGHTING
#include "LightCluster_inc.glsl"
#endif //USE_CLUSTERED_LIGHTING

#ifndef USE_SUN_SHADOWS
#define USE_SUN_SHADOWS 0
#endif

#if USE_SUN_SHADOWS
#include "ShadowMap_inc.glsl"
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform vec2 uInvTextureAtlasSize;

// Buffer containing all spawners data
uniform samplerBuffer uSpawnerDataBuffer;

const int SPAWNER_DATA_SIZE = 12;
// SPAWNER_WORLDMATRIX_OFFSET = 0;
// SPAWNER_STATICLIGHTCOLOR_INFLUENCE_OFFSET = 4;
// SPAWNER_INVROTATION_OFFSET = 5;
// SPAWNER_TEXIMAGELOCATION_OFFSET = 6;
// SPAWNER_TEXFRAMESIZE_OFFSET = 7;
// SPAWNER_UVMOTION_OFFSET = 8;
// SPAWNER_INTERSECTION_HIGHLIGHT = 9
// SPAWNER_CAMERA_OFFSET_VELOCITY_STRETCH_SOFT_FACTOR = 10
// SPAWNER_UV_MOTION_STRENGTH_CURVE_TYPE = 11

const int STRENGTH_CURVE_TYPE_CONST = 0;
const int STRENGTH_CURVE_TYPE_INCR_LINEAR = 1;
const int STRENGTH_CURVE_TYPE_INCR_QUARTIN = 2;
const int STRENGTH_CURVE_TYPE_INCR_QUARTINOUT = 3;
const int STRENGTH_CURVE_TYPE_INCR_QUARTOUT = 4;
const int STRENGTH_CURVE_TYPE_DECR_LINEAR = 5;
const int STRENGTH_CURVE_TYPE_DECR_QUARTIN = 6;
const int STRENGTH_CURVE_TYPE_DECR_QUARTINOUT = 7;
const int STRENGTH_CURVE_TYPE_DECR_QUARTOUT = 8;

#if USE_FOG
uniform sampler2D uFogNoiseTexture;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec4 vertData1;
in vec4 vertData2;
in vec4 vertData3;
in vec4 vertData4;

// Common
#define vertConfig              floatBitsToUint(vertData1.x)

// Particles
#define vertTextureInfo         floatBitsToUint(vertData1.y)
#define vertColor               floatBitsToUint(vertData1.z)
#define vertPosition            vec3(vertData1.w, vertData2.xy)
#define vertScale               (vertData2.zw)
#define vertVelocity            (vertData3.xyz)
#define vertRotation            vec4(vertData3.w, vertData4.xyz)
#define vertSeedAndLifeRatio    floatBitsToUint(vertData4.w)

// Trails
#define vertTopLeftPosition     (vertData1.yzw)
#define vertBottomLeftPosition  (vertData2.xyz)
#define vertTopRightPosition    vec3(vertData2.w, vertData3.xy)
#define vertBottomRightPosition vec3(vertData3.zw, vertData4.x)
#define vertLength              (vertData4.y)

// Bit shifts to extract data from vertConfig
const int CONFIG_BIT_SHIFT_QUAD_TYPE = 0;
const int CONFIG_BIT_SHIFT_LINEAR_FILTERING = 3;
const int CONFIG_BIT_SHIFT_SOFT_PARTICLE = 4;
const int CONFIG_BIT_SHIFT_INVERT_UTEXTURE = 5;
const int CONFIG_BIT_SHIFT_INVERT_VTEXTURE = 6;
const int CONFIG_BIT_SHIFT_BLEND_MODE = 7;
const int CONFIG_BIT_SHIFT_FIRST_PERSON = 9;
const int CONFIG_BIT_SHIFT_DRAW_ID = 16;

const int CONFIG_BIT_MASK_QUAD_TYPE = 1 << 2 | 1 << 1 | 1 << 0;
const int QUAD_TYPE_ORIENTED = 0;
const int QUAD_TYPE_BILLBOARD = 1;
const int QUAD_TYPE_BILLBOARD_Y = 2;
const int QUAD_TYPE_BILLBOARD_VELOCITY = 3;
const int QUAD_TYPE_VELOCITY = 4;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
flat out int fragConfig;
flat out int fragFXType;

out vec4 fragTexCoords;
out float fragDepthVS;
out vec4 fragColor;

#if USE_DISTORTION_RT
out vec3 fragPositionVS;
#endif

#if USE_FOG
out vec4 fragFogInfo;
#endif // USE_FOG

// Particles
flat out vec4 fragAtlasUVOffsetSize;
flat out vec4 fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2;
flat out vec4 fragIntersectionHighlight;
flat out float fragUVMotionStrength;

// Trails
out vec3 fragK;
out vec4 fragPlaneCoords;
out vec4 fragPlaneCoordsu;

flat out float fragUVMotionOffset;
//-------------------------------------------------------------------------------------------------------------------------

void particle(mat4 worldMatrix, int address, int config, out vec4 positionWS, out vec4 positionVS, out float size, out int lightStartIndex, out int lightCount);
void trail(mat4 worldMatrix, int address, int config, out vec4 positionWS, out vec4 positionVS, out float size, out int lightStartIndex, out int lightCount);

#if USE_CLUSTERED_LIGHTING
vec4 computeFXClusteredLighting(vec3 positionVS, int pointLightCount, int lightIndex, vec4 fragmentColor, vec4 staticLightColorInfluence, vec4 sunLight);
#endif //USE_CLUSTERED_LIGHTING

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    uint uconfig = vertConfig;
    int config = int(uconfig);
    fragConfig = config;

    int drawId = config >> CONFIG_BIT_SHIFT_DRAW_ID;
    int address = SPAWNER_DATA_SIZE * drawId;

    mat4 worldMatrix;
    worldMatrix[0] = texelFetch(uSpawnerDataBuffer, address + 0);
    worldMatrix[1] = texelFetch(uSpawnerDataBuffer, address + 1);
    worldMatrix[2] = texelFetch(uSpawnerDataBuffer, address + 2);
    worldMatrix[3] = texelFetch(uSpawnerDataBuffer, address + 3);

    vec4 staticLightColorInfluence = texelFetch(uSpawnerDataBuffer, address + 4);

    const int TYPE_PARTICLE = 0;
    const int TYPE_TRAIL = 1;
    fragFXType = int(texelFetch(uSpawnerDataBuffer, address + 7).w);

    // Get the position in WS from the relevant function.
    vec4 positionWS, positionVS;
    float size;
    int lightStartIndex, lightCount;

    if (fragFXType == TYPE_PARTICLE)
    {
        particle(worldMatrix, address, config, positionWS, positionVS, size, lightStartIndex, lightCount);
    }
    else
    {
        trail(worldMatrix, address, config, positionWS, positionVS, size, lightStartIndex, lightCount);
    }

    // Debug : uncomment this to specifically measure vertex shader perf.
    //gl_Position = gl_Position * vec4(0) + vec4(-1);

    bool isFullbright = staticLightColorInfluence.w == 0;    

#if USE_CLUSTERED_LIGHTING
    if (!isFullbright)
    {
        // Cap the light count to a reasonable number
        lightCount = min(8, lightCount);
        fragColor = computeFXClusteredLighting(positionVS.xyz, lightCount, lightStartIndex, fragColor, staticLightColorInfluence, SunLightColor);
    }
#endif //USE_CLUSTERED_LIGHTING

#if USE_SUN_SHADOWS
    // Avoid shadow on emissive material.
    // And for performance, skip this also if the particle (rather vertex here) is too far away or if it's small on screen.
    // We use custom heuristics for Particles, but this cannot work for Trails, since we can have super tiny triangles at the beginning/end of a Trail motion.
    float sizeOnScreenFactor = size / abs(positionVS.z);
    bool isSmallOnScreen = (fragFXType == TYPE_PARTICLE) && sizeOnScreenFactor < 0.02;
    bool isInSunShadowRange = abs(positionVS.z) < SunShadowCascadeStats[CASCADE_COUNT-1].x;

    if (!isFullbright && isInSunShadowRange && !isSmallOnScreen)
    {
        // We sample the shadowmap using the vertex position rather than the particle position.
        vec3 positionFromCameraWS = positionWS.xyz;
//        vec3 positionFromCameraWS = vec3(worldMatrix * vec4(particlePosition,1)).xyz;

        const vec3 normalWS = vec3(0,1,0);
        const bool useCleanShadowBackfaces = false;
        float shadow = computeShadowIntensity(positionFromCameraWS, positionVS.z, normalWS, useCleanShadowBackfaces);

        // If the particle is somewhat big and if it's inside an unevenly shadowed area, it will flicker as it moves (or as the sun moves).
        // So we want to stabilize the shadow.
        // The easiest way is to take many shadow samples around the vertex and average.
        bool isParticleBigOnScreen = sizeOnScreenFactor > 0.04;

        // We restrict this to Particles only. Trails should not suffer too much from flickering as they are really short lived fx.
        if (fragFXType == TYPE_PARTICLE && isParticleBigOnScreen)
        {
            shadow = 0;

            // We take 3*3*3 = 27 samples - this sounds like a lot, but since we do it per vertex, it's still really cheap.
            const float offsetDistance = 0.5;
            const float n = 1;
            const float sampleCount = (2*n+1)*(2*n+1)*(2*n+1);

            for (float x = -n; x <= n; x++)
            for (float y = -n; y <= n; y++)
            for (float z = -n; z <= n; z++)
            {
                vec3 offset = vec3(x, y, z) * offsetDistance;
                shadow += computeShadowIntensity(positionFromCameraWS + offset, positionVS.z, normalWS, useCleanShadowBackfaces);
            }
            shadow /= sampleCount;
        }

        fragColor.rgb *= shadow;

//        // Debug info "isParticleBigOnScreen" or "shadow"
//        if(isParticleBigOnScreen) fragColor.rgb = vec3(0,0,1);
//        fragColor.rgb = vec3(shadow, 1-shadow, 0);
    }
    
//    // Debug info "isInSunShadowRange" or "isSmallOnScreen"
//    if (!isInSunShadowRange) fragColor.rgb = vec3(1,0,0);
//    if (isSmallOnScreen) fragColor.rgb = vec3(0,1,0);

#endif // USE_SUN_SHADOWS

#if USE_FOG
    // Send needed information to fragment shader to blend smoothFogColor with pixelColor
    vec2 ditherNoiseSeed = vec2(0);
    fragFogInfo = computeFog(positionWS.xyz, CameraPosition, SunPositionWS,
                            FogTopColor.rgb, FogFrontColor.rgb, FogBackColor.rgb, FogParams, FogMoodParamsA, FogHeightDensityAtViewer,
                            ditherNoiseSeed, uFogNoiseTexture, SceneBrightness.r);
    //fragFogInfo.a = 1;
#endif // USE_FOG
}

//-------------------------------------------------------------------------------------------------------------------------
// Particles
void computeQuad(int quadType, int quadVertexId, vec2 quadScale, vec3 particlePosition, vec3 particleVelocity, vec4 particleRotation, float velocityStretch, vec4 spawnerInvRotation, mat4 worldMatrix, mat3 viewMatrix, uint textureInfo, vec4 texImageLocation, vec2 texFrameSize, out vec3 positionOS, out vec3 offsetVS, out vec2 quadUV, out vec2 atlasUVSize, out vec4 atlasUVOffsets, out float spriteBlendProgress);

void particle(mat4 worldMatrix, int address, int config, out vec4 positionWS, out vec4 positionVS, out float size, out int lightStartIndex, out int lightCount)
{
    // Decompress particle data
    //const float positionDecompressor = 511.984375f;// 32767.0f * vec3(64.0f);
    //const float positionDecompressor = 255.28125f;// 32767.0f * vec3(128.0f);
    vec3 particlePosition = vertPosition;
    vec2 particleScale = vertScale;
    vec4 particleRotation = vertRotation;
    vec3 particleVelocity = vertVelocity;
    vec4 spawnerInvRotation = texelFetch(uSpawnerDataBuffer, address + 5);
    vec4 texImageLocation = texelFetch(uSpawnerDataBuffer, address + 6);
    vec3 texFrameSizeAndUVMotionTextureId = texelFetch(uSpawnerDataBuffer, address + 7).xyz;
    float uvMotionSpawnerStrength = texelFetch(uSpawnerDataBuffer, address + 8).z;
    vec4 texIntersectionHighlight = texelFetch(uSpawnerDataBuffer, address + 9);
    vec3 cameraOffsetVelocityStretchAndAddRandomOffset = texelFetch(uSpawnerDataBuffer, address + 10).xyw;
    int uvMotionStrengthCurveType = int(texelFetch(uSpawnerDataBuffer, address + 11).x);

    vec2 texFrameSize = texFrameSizeAndUVMotionTextureId.xy;
    float texUVMotionTextureId = texFrameSizeAndUVMotionTextureId.z;

    fragIntersectionHighlight = texIntersectionHighlight;

    bool addRandomOffset = cameraOffsetVelocityStretchAndAddRandomOffset.z == 1;

    float lifeRatio  = float((vertSeedAndLifeRatio) & 65535u) / 65535.0;
    uint seed = vertSeedAndLifeRatio >> 16u;

    fragUVMotionOffset = addRandomOffset ? randomui(seed) * 2.0 - 1.0 : 0.0;

    // UVMotion TextureId in X, SpriteBlending params in (Y,ZW)
    fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2.x = texUVMotionTextureId;

    float uvMotionStrength = 0.0;
    switch(uvMotionStrengthCurveType)
    {
        case STRENGTH_CURVE_TYPE_CONST: uvMotionStrength = uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_INCR_LINEAR: uvMotionStrength = lifeRatio * uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_DECR_LINEAR: uvMotionStrength = (1.0 - lifeRatio) * uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_INCR_QUARTIN: uvMotionStrength = QuartIn(lifeRatio) * uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_DECR_QUARTIN: uvMotionStrength = (1.0 - QuartIn(lifeRatio)) * uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_INCR_QUARTINOUT: uvMotionStrength = QuartInOut(lifeRatio) * uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_DECR_QUARTINOUT: uvMotionStrength = (1.0 - QuartInOut(lifeRatio)) * uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_INCR_QUARTOUT: uvMotionStrength = QuartOut(lifeRatio) * uvMotionSpawnerStrength; break;
        case STRENGTH_CURVE_TYPE_DECR_QUARTOUT: uvMotionStrength = (1.0 - QuartOut(lifeRatio)) * uvMotionSpawnerStrength; break;
    }

    fragUVMotionStrength = uvMotionStrength;

    int quadVertexId = gl_VertexID % 4;
    int quadType = (config >> CONFIG_BIT_SHIFT_QUAD_TYPE) & CONFIG_BIT_MASK_QUAD_TYPE;

    int isFirstPerson = (config >> CONFIG_BIT_SHIFT_FIRST_PERSON) & 1;
    mat4 viewMatrix = isFirstPerson == 1 ? FirstPersonViewMatrix : ViewMatrix;
    mat4 projectionMatrix = isFirstPerson == 1 ? FirstPersonProjectionMatrix : ProjectionMatrix;

    vec3 positionOS, offsetVS;
    vec2 quadUV;
    vec2 atlasUVSize;
    vec4 atlasUVOffsets;
    float spriteBlendProgress;

    // Compute the quad corners info
    computeQuad(quadType, quadVertexId, particleScale, particlePosition, particleVelocity, particleRotation, cameraOffsetVelocityStretchAndAddRandomOffset.y, spawnerInvRotation, worldMatrix, mat3(viewMatrix), vertTextureInfo, texImageLocation, texFrameSize, positionOS, offsetVS, quadUV, atlasUVSize, atlasUVOffsets, spriteBlendProgress);
    fragTexCoords.zw = quadUV;
    fragAtlasUVOffsetSize.xy = atlasUVOffsets.xy;
    fragAtlasUVOffsetSize.zw = atlasUVSize;
    fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2.zw = atlasUVOffsets.zw;
    fragUVMotionTextureIdAndSpriteBlendAndAtlasUVOffset2.y = spriteBlendProgress;
    
    // func out
    positionWS = worldMatrix * vec4(positionOS, 1.0);
    positionVS = viewMatrix * positionWS;
    positionVS.xyz += offsetVS;

    // Camera offset: cannot be done on the .z only, otherwise it would be unaligned when drawn on the side of the screen.
    positionVS.xyz += normalize(positionVS.xyz) * cameraOffsetVelocityStretchAndAddRandomOffset.x;
    
    gl_Position = projectionMatrix * positionVS;

    // To avoid first person weapon to intersect w/ world geometry and to have it match the Particles rendered in first person,
    // we modify the Z in the vertex shader (rather than in the fragment shader, which would disable the early-Z hw optimization).
    gl_Position.z *= (isFirstPerson == 1) ? 0.55 : 1.0;
    
    // func out
    size = length(particleScale * texFrameSize);

#if USE_CLUSTERED_LIGHTING
    vec2 screenUV = (gl_Position.xy/gl_Position.ww) * vec2(0.5) + vec2(0.5);
    fetchClusterData(screenUV, -positionVS.z, lightStartIndex, lightCount);
#else
    lightStartIndex = 0; 
    lightCount = 0;
#endif

    // To have minimal visual issues (w/ OIT or other part of the code), we do something similar to what is done w/ gl_Position.z in first person,
    // but using a different factor as it's a different space though.
    fragDepthVS = (isFirstPerson == 1) ? positionVS.z * 0.2 : positionVS.z;

    #if USE_DISTORTION_RT
    fragPositionVS = positionVS.xyz;
    #endif

    uint color = vertColor;
    fragColor = vec4(
        float(color & uint(0xff)) / 255.0,
        float((color >> 8) & uint(0xff)) / 255.0,
        float((color >> 16) & uint(0xff)) / 255.0,
        float((color >> 24) & uint(0xff)) / 255.0);

#if USE_SUNLIGHT_SHADING
    // This is a quick demo of proper light shading.
    // To keep things simple, we only shade this w/ the sun light direction,
    // but we could actually do it for dynamic lights too.

    // Debug helpers
    // fragColor.rgb = vec3(1);
    // fragColor.a += 0.5;
    
    vec3 particleNormal = normalize(vec3(0,0,1) + normalize(positionVS.xyz - (viewMatrix * uModelMatrix * vec4(particlePosition,1)).xyz));
    vec3 sunLightDir = mat3(viewMatrix) * SunLightDirection.xyz;
    float NdotL = (dot(sunLightDir, particleNormal) * 0.5 + 0.5);

    // Specify a potential light shading remapping. Normal would be (1,0);
    const vec2 lightRemap = vec2(1.0, 0.0);
    //const vec2 lightRemap = vec2(0.7, 0.3);
    fragColor.rgb *= NdotL * lightRemap.x + lightRemap.y;
    
    // Debug normal
    //fragColor.rgb = particleNormal;
#endif
}

void computeQuad(int quadType, int quadVertexId, vec2 quadScale, vec3 particlePosition, vec3 particleVelocity, vec4 particleRotation, float velocityStretch, vec4 spawnerInvRotation, mat4 worldMatrix, mat3 viewMatrix, uint textureInfo, vec4 texImageLocation, vec2 texFrameSize, out vec3 positionOS, out vec3 offsetVS, out vec2 quadUV, out vec2 atlasUVSize, out vec4 atlasUVOffsets, out float spriteBlendProgress)
{
    const vec3 upOS = vec3(0,1,0);
    const vec3 rightOS = vec3(1,0,0);
    const vec3 cameraRightVS = vec3(1,0,0);
    const vec3 cameraUpVS = vec3(0,1,0);
    const vec3 cameraFrontVS = vec3(0,0,-1);
    vec3 worldUpVS = vec3(viewMatrix[1]);

    const vec2 QuadCornersSign[4] = vec2[]( vec2( 1, 1),
                                            vec2(-1, 1),
                                            vec2(-1,-1),
                                            vec2( 1,-1));

    const vec2 QuadUVs[4] = vec2[]( vec2(1, 0),
                                    vec2(0, 0),
                                    vec2(0, 1),
                                    vec2(1, 1));

    // a) common 0,1 uv
    quadUV = QuadUVs[quadVertexId];

    // b) compute the texture area offset & size in the atlas
    int tilesPerRow = int(texImageLocation.z) / int(texFrameSize.x);

    int textureId = int(textureInfo) & 0xFF;
    int textureId2 = int(textureInfo >> 8u) & 0xFF;
    spriteBlendProgress = float(textureInfo >> 16u) / 65535.0;

    ivec4 tile1and2 = ivec4(ivec2(textureId % tilesPerRow, textureId / tilesPerRow), ivec2(textureId2 % tilesPerRow, textureId2 / tilesPerRow));
    vec4 particleCoordinates = vec4(tile1and2) * texFrameSize.xyxy + texImageLocation.xyxy;

    atlasUVSize.xy = texFrameSize * uInvTextureAtlasSize.xy;
    atlasUVOffsets = particleCoordinates * uInvTextureAtlasSize.xyxy;

    // 1. Scale the quad corner
    vec2 sign = QuadCornersSign[quadVertexId];
    vec2 quadHalfSize = quadScale * texFrameSize * 0.5;
    vec3 scaledOffset = vec3(sign.x * quadHalfSize.x, sign.y * quadHalfSize.y, 0);

    // 2. Init the quad corner in the most efficient space :
    // - billboard : View Space
    // - billboardY : View Space
    // - billboard velocity : View Space
    // - oriented : Object Space
    // - velocity : Object Space

    // Quad corner offsets
    vec3 offsetRight;
    vec3 offsetUp;

    vec3 velocityVS = viewMatrix * particleVelocity;

    switch(quadType)
    {
        case QUAD_TYPE_BILLBOARD:
            offsetRight = cameraRightVS;
            offsetUp = cameraUpVS;
            break;

        case QUAD_TYPE_BILLBOARD_Y:
            // Z rotation applied before any other rotation
            // Since the rotation on the Y and X axis is constrained, only the Z axis rotation has interest here.
            // So we can extract it by nulling the x & y, and renormalizing the resulting quaternion.
            // But this is already done during settings sanitization, so we can ignore it.
            // ...normalize(vec4(0,0,particleRotation.z,particleRotation.w));
            scaledOffset = rotateVector(scaledOffset, particleRotation);

            offsetRight = cameraRightVS;
            offsetUp = worldUpVS;
            break;

        case QUAD_TYPE_BILLBOARD_VELOCITY:
            // Same remark as above for QUAD_TYPE_BILLBOARD_Y.
            scaledOffset = rotateVector(scaledOffset, particleRotation);

            offsetUp = computeDirectionFromVelocity(velocityVS);

            vec3 vToCamera = viewMatrix * (worldMatrix * vec4(particlePosition, 1)).xyz;
            offsetRight = cross(offsetUp, normalize(vToCamera));
            break;

        case QUAD_TYPE_ORIENTED:
        case QUAD_TYPE_VELOCITY:
            offsetRight = rightOS;
            offsetUp = upOS;
            break;
    }

    vec3 offset = (offsetRight * scaledOffset.x) + (offsetUp * scaledOffset.y);

    // 3. Rotate & write to the out parameters
    vec4 quaternion;
    switch(quadType)
    {
        case QUAD_TYPE_BILLBOARD:
            positionOS = particlePosition;
            offsetVS = rotateVector(offset, particleRotation);
            break;

        case QUAD_TYPE_BILLBOARD_Y:
            positionOS = particlePosition;
            offsetVS = offset;
            break;

        case QUAD_TYPE_BILLBOARD_VELOCITY:
            positionOS = particlePosition;
            offsetVS = offset;

            // stretch the billboard more according to its velocity
            offsetVS.xyz += dot(offsetVS.xyz, velocityVS) * velocityStretch * velocityVS;
            break;

        case QUAD_TYPE_ORIENTED:
            quaternion = quaternionMultiply(spawnerInvRotation, particleRotation);
            offset = rotateVector(offset, quaternion);
            positionOS = particlePosition + offset;
            offsetVS = vec3(0);
            break;

        case QUAD_TYPE_VELOCITY:
            quaternion = quaternionMultiply(spawnerInvRotation, particleRotation);
            quaternion = quaternionMultiply(quaternionFromVelocity(particleVelocity), quaternion);
            offset = rotateVector(offset, quaternion);

            // stretch the billboard more according to its velocity
            offset.xyz += dot(offset.xyz, particleVelocity) * velocityStretch * particleVelocity;

            positionOS = particlePosition + offset;
            offsetVS = vec3(0);
            break;
    }
}

//-------------------------------------------------------------------------------------------------------------------------
// Trails
float wedge2Dxz(vec3 v, vec3 w)
{
    return v.x * w.z - v.z * w.x;
}

float wedge2Dzy(vec3 v, vec3 w)
{
    return v.z * w.y - v.y * w.z;
}

float wedge2Dxy(vec3 v, vec3 w)
{
    return v.y * w.x - v.x * w.y;
}

void trail(mat4 worldMatrix, int address, int config, out vec4 positionWS, out vec4 positionVS, out float size, out int lightStartIndex, out int lightCount)
{
    vec4 startColor = texelFetch(uSpawnerDataBuffer, address + 5);
    vec4 textureCoords = texelFetch(uSpawnerDataBuffer, address + 6);
    vec4 endColor = texelFetch(uSpawnerDataBuffer, address + 8);
    vec4 intersectionHighlight = texelFetch(uSpawnerDataBuffer, address + 9);

    int isFirstPerson = (config >> CONFIG_BIT_SHIFT_FIRST_PERSON) & 1;
    mat4 viewMatrix = isFirstPerson == 1 ? FirstPersonViewMatrix : ViewMatrix;
    mat4 projectionMatrix = isFirstPerson == 1 ? FirstPersonProjectionMatrix : ProjectionMatrix;

    int groupPosition = gl_VertexID - 4 * int(gl_VertexID * 0.25);

    vec3 p; 

    switch(groupPosition)
    {
    case 0: p = vertTopLeftPosition;    break;
    case 1: p = vertBottomLeftPosition; break;
    case 2: p = vertBottomRightPosition;break;
    case 3: p = vertTopRightPosition;   break;
    }
    
    // func out
    positionWS = worldMatrix * vec4(p, 1.0);
    positionVS = viewMatrix * positionWS;
    
    gl_Position = projectionMatrix * positionVS;

    // func out
    size = vertLength;

    fragDepthVS = positionVS.z;
    fragColor = (endColor - startColor) * vertLength + startColor;
    fragIntersectionHighlight = intersectionHighlight;

    float x = textureCoords.x + (textureCoords.z - textureCoords.x) * (1 - vertLength);
    fragTexCoords = vec4(x, x, textureCoords.y, textureCoords.w);

#if USE_DISTORTION_RT
    fragPositionVS = positionVS.xyz;
#endif

#if USE_CLUSTERED_LIGHTING
    vec2 screenUV = (gl_Position.xy/gl_Position.ww) * vec2(0.5) + vec2(0.5);
    fetchClusterData(screenUV, -positionVS.z, lightStartIndex, lightCount);
#else
    lightStartIndex = 0; 
    lightCount = 0;
#endif

    vec3 p3 = vertTopLeftPosition;
    vec3 p0 = vertBottomLeftPosition;
    vec3 p2 = vertTopRightPosition;
    vec3 p1 = vertBottomRightPosition;

    vec3 e = p1 - p0;
    vec3 f = p3 - p0;
    vec3 g = p0 - p1 + p2 - p3;
    vec3 h = p - p0;

    float areaX = 0;
    float areaY = 0;
    float areaZ = 0;

    float x1 = 0;
    float x2 = 0;
    float y1 = 0;
    float y2 = 0;
    float z1 = 0;
    float z2 = 0;

    // X
    y1 = p1.y - p0.y;
    z1 = p1.z - p0.z;
    y2 = p2.y - p0.y;
    z2 = p2.z - p0.z;
    areaX += y1 * z2 - y2 * z1;

    y1 = p2.y - p0.y;
    z1 = p2.z - p0.z;
    y2 = p3.y - p0.y;
    z2 = p3.z - p0.z;
    areaX += y1 * z2 - y2 * z1;

    // Y
    x1 = p1.x - p0.x;
    z1 = p1.z - p0.z;
    x2 = p2.x - p0.x;
    z2 = p2.z - p0.z;
    areaY += x1 * z2 - x2 * z1;

    x1 = p2.x - p0.x;
    z1 = p2.z - p0.z;
    x2 = p3.x - p0.x;
    z2 = p3.z - p0.z;
    areaY += x1 * z2 - x2 * z1;

    // Z
    x1 = p1.x - p0.x;
    y1 = p1.y - p0.y;
    x2 = p2.x - p0.x;
    y2 = p2.y - p0.y;
    areaZ += x1 * y2 - x2 * y1;

    x1 = p2.x - p0.x;
    y1 = p2.y - p0.y;
    x2 = p3.x - p0.x;
    y2 = p3.y - p0.y;
    areaZ += x1 * y2 - x2 * y1;

    vec3 areas = vec3(abs(areaX * 0.5), abs(areaY * 0.5), abs(areaZ * 0.5));

    fragK = vec3(0);
    fragPlaneCoords = vec4(0);
    fragPlaneCoordsu = vec4(0);

    if (areas.y > areas.x && areas.y > areas.z)
    {
        fragK.z = wedge2Dxz( g, f );
        fragK.y = wedge2Dxz( e, f ) + wedge2Dxz( h, g );
        fragK.x = wedge2Dxz( h, e );

        fragPlaneCoords = vec4(h.z, f.z, e.z, g.z);
        fragPlaneCoordsu = vec4(h.x, f.x, e.x, g.x);
    }
    else if (areas.x > areas.y && areas.x > areas.z)
    {
        fragK.z = wedge2Dzy( g, f );
        fragK.y = wedge2Dzy( e, f ) + wedge2Dzy( h, g );
        fragK.x = wedge2Dzy( h, e );

        fragPlaneCoords = vec4(h.y, f.y, e.y, g.y);
        fragPlaneCoordsu = vec4(h.z, f.z, e.z, g.z);
    }
    else if (areas.z > areas.x && areas.z > areas.y)
    {
        fragK.z = wedge2Dxy( g, f );
        fragK.y = wedge2Dxy( e, f ) + wedge2Dxy( h, g );
        fragK.x = wedge2Dxy( h, e );

        fragPlaneCoords = vec4(h.y, f.y, e.y, g.y);
        fragPlaneCoordsu = vec4(h.x, f.x, e.x, g.x);
    }
}

//-------------------------------------------------------------------------------------------------------------------------
// Common

#if USE_CLUSTERED_LIGHTING
vec4 computeFXClusteredLighting(vec3 positionVS, int pointLightCount, int lightIndex, vec4 fragmentColor, vec4 staticLightColorInfluence, vec4 sunLight)
{
    const float LightMultiplier = 2.0;

    // Lighting code
    const float EarlyOutThreshold = 1.0f / LightMultiplier;
    vec3 dynamicLightColor = computeClusteredLighting(positionVS, pointLightCount, lightIndex, EarlyOutThreshold);

    // Same as BlockyModelFS
    dynamicLightColor = min(dynamicLightColor * LightMultiplier, vec3(1.0, 1.0, 1.0));

    // Different light color blend methods... hacks
    //fragmentColor.rgb *= uSunLight.a * dynamicLightColor;
    //fragmentColor.rgb += dynamicLightColor;
    //fragmentColor.rgb *= (min(uSunLight.a * 1.5, 0.65) + dynamicLightColor);
    //fragmentColor.rgb += (min(uSunLight.a * 1.5, 0.65) + dynamicLightColor);    
    //fragmentColor.rgb = mix(min(uSunLight.a * 1.5, 1.0) * fragmentColor.rgb, dynamicLightColor, 0.5 );
    fragmentColor.rgb *= max(staticLightColorInfluence.rgb, min(sunLight.a * 1.5, 0.65) + dynamicLightColor.rgb);

    return fragmentColor;
}
#endif //USE_CLUSTERED_LIGHTING
