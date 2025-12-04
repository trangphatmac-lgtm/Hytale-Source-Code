#version 330 core

// Global scene data
#include "SceneData_inc.glsl"

#include "VertexCompression_inc.glsl"
#include "Fog_inc.glsl"
#include "Shading_inc.glsl"

#if USE_FOG
uniform sampler2D uFogNoiseTexture;
#endif

#if USE_SCENE_DATA_OVERRIDE
uniform mat4 uViewProjectionMatrix;
#define ViewProjectionMatrix uViewProjectionMatrix
#undef Time
#define Time 0
#undef SunLightColor
#define SunLightColor vec4(1)
#define SceneBrightness vec4(1)
#endif //USE_SCENE_DATA_OVERRIDE

#ifndef DEBUG_BOUNDARIES
#define DEBUG_BOUNDARIES 0
#endif

uniform mat4 uModelMatrix;

uniform vec3 uWaterTintColor;

#if NEAR && ALPHA_TEST
const int MAX_INTERACTION_POSITIONS = 20;
uniform vec3 uFoliageInteractionPositions[MAX_INTERACTION_POSITIONS];
uniform vec3 uFoliageInteractionParams;
#endif
#if ANIMATED
layout(std140) uniform uboNodeBlock
{
    mat4 nodeData[MAX_NODES_COUNT];
};
#endif

// (from 4xS16) XYZ: Position packed, W: DoubleSided [15] and BlockId [14-0]
in ivec4 vertPositionAndDoubleSidedAndBlockId;
// (from 4xS16) XY: TexCoords, ZW: MaskTexCoords
in vec4 vertTexCoords;
// (from 4xU32) X: Normal packed [23-0] and NodeId [31-24], Y: Tint [31-8] and Shading [7-6] and Effect [5-0], Z: Glow [23-0] & Sunlight [31-24], W: Billboard [0]
in uvec4 vertDataPacked;

#define vertPositionPacked                  (vertPositionAndDoubleSidedAndBlockId.xyz)
#define vertDoubleSidedAndBlockId           (vertPositionAndDoubleSidedAndBlockId.w)
#define vertNormalAndNodeId                 (vertDataPacked.x)
#define vertTintColorAndShadingAndEffect    (vertDataPacked.y)
#define vertGlowColorAndSunlight            (vertDataPacked.z)
#define vertBillboard                       (vertDataPacked.w)

// FIXME : it is recommended (by AMD, 2014) to stay at max 4 output attributes between shader stages, on all GPUs

// Packed data layout : {effectID [6], shadingMode[2], renderConfig[8], padding[16]}
flat out int fragPackedData;
flat out vec3 fragNormalWS;

out vec3 fragTintColor;
out vec2 fragTexCoords;
out vec4 fragStaticLight;

#if ANIMATED || (!ALPHA_BLEND && !ALPHA_TEST) // OPAQUE
out vec2 fragMaskTexCoords;
#endif // OPAQUE

#if !DEFERRED
out vec3 fragPositionWS;
out vec3 fragPositionVS;
out vec4 fragFog;
#if ALPHA_BLEND
out float fragTexOffsetY;
out vec2 fragLargeTexCoords;
#endif // ALPHA_BLEND
#endif // !DEFERRED

const float Pi = 3.14159265;

const float StaticLightMultiplier = 2.0;
const float FluidOffsetAmplitude = 0.04;

// Ripple variables
const float RippleLowerBound = 1.0;
const float RippleAmplitude = 0.12;
const float RippleSpeed = 2.0;
const float RippleFrequency = 15;

const int DOUBLESIDED_MASK = (1<<15);
const int BLOCK_ID_MASK = ~(1<<15);

// Pack data for the FragmentShader
int packData(int effectID, int shadingMode, bool hasSSAO, bool hasBloom);

vec4 computeFog(vec3 positionWS);

// Use backface culling for opaque (i.e. ALPHA_BLEND == 0 && ALPHA_TEST == 0) & for some alphatested (ALPHA_TEST == 1 + other condition?)
#if DEFERRED && !ALPHA_BLEND
#define USE_BACKFACE_CULLING 1
#else
#define USE_BACKFACE_CULLING 0
#endif

bool processBackfaceCulling(vec3 positionWS, vec3 normalWS);

void main()
{
    ivec4 tintColorAndEffectAndShadingMode = unpackIVec4FromUInt(vertTintColorAndShadingAndEffect);
    // To have a stable seed, for the wind effect, it's important to have the vertPosition NOT normalized by the hardware
    vec3 fxPosition = vec3(vertPositionPacked) / 511.984375f;// 32767.0f * vec3(64.0f);
//    vec3 fxPosition = vertPosition / 511.984375f;// 32767.0f * vec3(64.0f);
    int shadingMode = tintColorAndEffectAndShadingMode.w >> 6;

    // Extract the id and the custom param (stored in the last bit)
    int effectID = tintColorAndEffectAndShadingMode.w & 31;
    int effectParam = (tintColorAndEffectAndShadingMode.w & 32) >> 5;

    // The influence of the wind is entangled w/ the effectID.
    // For the wind attached effect,  "wind influence" is transformed from 0-14 to [0, 2-15], in order to have the same max wind influence as the wind effect.
    float windInfluence = (effectID > 0 && effectID < EFFECT_WIND) ? float(effectID + 1) : float(effectID);
    windInfluence /= 255.0f;
    
    fragTexCoords = vertTexCoords.xy;

    vec3 tintColor = vec3(tintColorAndEffectAndShadingMode.xyz) / 255.0;
    
    vec3 normal = unpackNormal(vertNormalAndNodeId);

#if NEAR && ALPHA_TEST
    // Superpowers block effects
    // Wind (0 - 15)
    if (effectID <= EFFECT_WIND)
    {
        // It's important to take the seed out of the computation below and store it in a var, otherwise we will have numeric instabilities
        vec2 seed = floor(vec2(vertPositionPacked.xz));
        float dist = windInfluence * 8.0;
        fxPosition.x += (cos(seed.x * Pi + Time * 1.2) + sin(seed.y * Pi + Time * 1.8) + cos(Time * 0.6)) * 0.15 * dist;
        fxPosition.z += (cos(seed.x * Pi + Time * 1.6) + sin(seed.y * Pi + Time * 1.2) + sin(Time * 0.6)) * 0.10 * dist;
    }
    // Ripple
    else if (effectID == EFFECT_RIPPLE)
    {
        float taper = clamp(fxPosition.y, RippleLowerBound, 1.0);
        vec3 offset = normalize(-fxPosition) * (RippleAmplitude * sin(RippleSpeed * Time + fxPosition.y * RippleFrequency)) * taper;
        fxPosition.x += offset.x;
        fxPosition.z += offset.z;
    }
#endif //NEAR && ALPHA_TEST

    mat4 nodeMatrix = uModelMatrix;

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

    fragNormalWS = normal;

    vec4 positionWS = nodeMatrix * vec4(fxPosition, 1.0);
    
#if ALPHA_BLEND
    fragTexOffsetY = 0.0;

    // effectId can go from 0 to 31. The values are defined in "HytaleClient/Data/Map/ClientBlockType.cs" in the "ClientShaderEffect" enum
    switch (effectID)
    {
        // Cube block effects
        case EFFECT_ICE:
            // TODO: Implement me ishraam sensei 
            break;
        case EFFECT_WATER:
        case EFFECT_WATERENVIRONMENTCOLOR:
        case EFFECT_WATERENVIRONMENTTRANSITION:
        case EFFECT_LAVA:
        case EFFECT_SLIME:
        {
            // Fade out the waves when using OIT to avoid glitches caused by overdraw.
            float distToCamera = length(positionWS.xyz);

            float maxDistanceForWaves = 64.0;
#if USE_OIT
            maxDistanceForWaves = IsCameraUnderwater > 0 ? 8.0 : 16.0;
#endif // USE_OIT

            if (distToCamera < maxDistanceForWaves)
            {
                float waveFadeOut = 1.0;
                waveFadeOut = 1.0 - (clamp(distToCamera / maxDistanceForWaves, 0.0, 1.0));

                float fluidOffset = (cos((fxPosition.x + fxPosition.z) * Pi / 2 + Time * 1.2) + sin(fxPosition.z * Pi / 2 + Time * 1.5)) * FluidOffsetAmplitude;
                fluidOffset = (FluidOffsetAmplitude * -2.0 + fluidOffset) * effectParam;
                
            //    positionWS.y += - FluidOffsetAmplitude;
                positionWS.y += fluidOffset * waveFadeOut;
            }
            
            // Water tint from weather, not applied when there is already an environment water tint
            if (effectID == EFFECT_WATER) tintColor = mix(tintColor, tintColor * uWaterTintColor.rgb, 0.8);
            else if (effectID == EFFECT_WATERENVIRONMENTTRANSITION) tintColor = mix(tintColor, uWaterTintColor.rgb, 0.8);

            // Color modifier
//            tintColor += clamp(yOffset - FluidOffsetAmplitude, 0.01, 1.0);

            float scrollSpeed = (effectID == EFFECT_WATER || effectID == EFFECT_WATERENVIRONMENTCOLOR || effectID == EFFECT_WATERENVIRONMENTTRANSITION) ? 0.025f : 0.005f;

            // Texture shift
            fragTexOffsetY = fragTexCoords.y + cos(fxPosition.x + fxPosition.z + Time) / 320.0;
            float fragTexOffsetY2 = fragTexCoords.y - Time * scrollSpeed;
            float faceUpValue = step(0.9, normal.y);
            fragTexOffsetY = mix(fragTexOffsetY2, fragTexOffsetY, faceUpValue);
            break;
        }
    }

#endif // ALPHA_BLEND

#if ALPHA_TEST
#if NEAR 
    // The normal is only needed on the fragment shader for near chunks because of the shine effect
    // fragNormal = normal;

    // Computations with foliage / grass depends on the world position, 
    // and for smooth results we need to do this after the object space position is modified.
    if (effectID > 0 && effectID <= EFFECT_WIND)
    {
        // WARNING : the GLSL compiler must unroll this loop for max performance.
        // It should be done automatically, provided the number of iterations is 
        // - a compile time constant (it is!)
        // - below 32 !
        for (int i = 0; i < MAX_INTERACTION_POSITIONS; i++)
        {
            // For a better result, we can use slightly different params for first person view & third person view.
            // The effect can be stronger in FP view.
            float radiusInfluence = uFoliageInteractionParams.x;
        
            // Since entity pos has y = 0 in object space (= feet level),
            // we take into consideration a position slightly above (hence the vec3(0,1,0))
            vec3 diff = positionWS.xyz - (vec3(0,1,0) + uFoliageInteractionPositions[i].xyz);
        
            if( length(diff) < radiusInfluence)
            {
                float len = length(diff.xyz);
                // float len = length(diff.xz);
                vec2 direction = normalize(diff.xz);

                float strengthFactor = windInfluence * uFoliageInteractionParams.y * smoothstep( 0, radiusInfluence, 1/pow(len, 5));

                // Modulate the strength for worst case scenario, so that we can minimize issues
                strengthFactor *= mix(uFoliageInteractionParams.z, 1.0f, effectParam);

                positionWS.xz += direction * strengthFactor;
            }
        }
    }
#endif // NEAR

#ifndef USE_FOLIAGE_FADING
#define USE_FOLIAGE_FADING 1
#endif

#if USE_FOLIAGE_FADING
    // This not only brings a smooth apparition of grass, 
    // it also improves the perf considerably by scaling down the number of fragment with their distance to camera.
    // This means less interpolator power, less fragment power, less overdraw, less writing into the GBuffer.
    if (effectParam > 0 && effectID < EFFECT_WIND) positionWS.y -= clamp(windInfluence * length(positionWS) * 0.25, 0.0, 1.0);
#endif
#endif // ALPHA_TEST

#if !ANIMATED
    // Below is sample code, kept for future tests.

    // Extract the block pos in the chunk
    ivec3 blockPosition = ivec3((vertDoubleSidedAndBlockId & 31), ((vertDoubleSidedAndBlockId >> 5) & 31), ((vertDoubleSidedAndBlockId >> 10) & 31));
    vec3 blockCenter = vec3(0.5) + blockPosition;

    // Older version, not working all the time - see explanations below
    //int blockId = BLOCK_ID_MASK & vertDoubleSidedAndBlockId;

    // Warning: because vertDoubleSidedAndBlockId comes from a signed short, which uses 2s complement,
    // we need to create the blockId from the extracted block position, to have the blockId == X test to work at all times,
    // otherwise when the sign bit is 1 in vertDoubleSidedAndBlockId (for DoubleSided) it won't work.
    int blockId = blockPosition.x | blockPosition.y << 5 | blockPosition.z << 10;

    // Fastest block detection
    const ivec3 selectedBlockCoords = ivec3(0,21,0);
    const int selectedBlockId = selectedBlockCoords.x | selectedBlockCoords.y << 5 | selectedBlockCoords.z << 10;
    bool isSelectedBlock = blockId == selectedBlockId;

    // alternate block detection
    //vec3 selectedBlockCoordinates = vec3(0.0, 26.0, 0.0);
    //bool isSelectedBlock = blockPosition.x >= selectedBlockCoordinates.x && blockPosition.x < (selectedBlockCoordinates.x + 1.0) && blockPosition.z >= selectedBlockCoordinates.z && blockPosition.z < (selectedBlockCoordinates.z + 1.0) && blockPosition.y >= selectedBlockCoordinates.y && blockPosition.y < (selectedBlockCoordinates.y + 1.0);


#if USE_LOD
    // LOD Billboard : draw some quads as billboards, when they are far enough.    
    if (vertBillboard == uint(1) && length(positionWS) > (LOD_DISTANCE - 20))
    {
        vec3 offset = fxPosition - blockCenter;
        float size = length(offset);
//        positionWS.xyz += (offset/size) * abs(SinTime);
//        positionWS.xyz += (offset/size) * length(positionWS.xyz) * 0.01;

        // create Billboard 
        vec3 up = vec3(ViewMatrix[0][1], ViewMatrix[1][1], ViewMatrix[2][1]);
        vec3 right = vec3(ViewMatrix[0][0], ViewMatrix[1][0], ViewMatrix[2][0]);
        vec3 billboardOffset;

        switch(gl_VertexID % 4)
        {
        case 0: billboardOffset = up - right; break;
        case 1: billboardOffset = up + right; break;
        case 2: billboardOffset = -up + right; break;
        case 3: billboardOffset = -up - right; break;
        }

        // Bigger or original size?
//        positionWS = nodeMatrix * vec4((blockCenter + billboardOffset * size), 1);
        positionWS = nodeMatrix * vec4((blockCenter + normalize(billboardOffset) * size), 1);
    }
#endif
#endif // !ANIMATED

#if ALPHA_BLEND
    // To prevent Z-Fight between a transparent (e.g. water) face & some half immerged geometry (e.g. stair),
    // we need to push the geometry a bit. We cannot use glPolygonOffset for that, as it produces other artifacts on water
    // (black lines at grazing angles on the surface). So we do it manually here in the vertex shader.
    gl_Position = ViewProjectionMatrix * vec4(positionWS + vec4(normalize(positionWS.xyz), 0) * 0.01);
#else
    gl_Position = ViewProjectionMatrix * positionWS;
#endif // ALPHA_BLEND

    float sunlight = 1.0;
    float lightExposition;

    // shadingMode can go from 0 to 3. The values are defined in "HytaleClient/Graphics/ShadingMode.cs"
    switch (shadingMode)
    {
        case SHADING_FLAT:
        case SHADING_FULLBRIGHT:
            lightExposition = 1.0;
            break;

        case SHADING_REFLECTIVE:
        default:
            float lightExpositionVertical = 0.4 * clamp(normal.y, 0.0, 1.0);
            float lightExpositionHorizontal = 0.2 * abs(normal.z);
            lightExposition = 0.6 + max(lightExpositionVertical, lightExpositionHorizontal);
            break;
    }

    vec4 glowColorAndSunlight = unpackVec4FromUInt(vertGlowColorAndSunlight) / 255.0;
    vec4 staticLight = glowColorAndSunlight.rgba * lightExposition;
    fragStaticLight.rgb = max(SunLightColor.rgb * SunLightColor.a * staticLight.a, staticLight.rgb * StaticLightMultiplier);
    fragStaticLight.rgb = (shadingMode != SHADING_FULLBRIGHT) ? fragStaticLight.rgb : vec3(1.0f);
    fragStaticLight.a = glowColorAndSunlight.a;

    // Same computation as in MapModule.GetLightColorAtBlockPosition
    vec3 tintWithColoredLights = glowColorAndSunlight.rgb * StaticLightMultiplier;
    vec3 tintWithSunlight = vec3(sunlight);
    vec3 tintLight = max(tintWithColoredLights, tintWithSunlight);

    // Avoids overexposure for FULLBRIGHT & REFLECTIVE, caused by the StaticLightMultiplier making the values go beyond 1.0.
    // Note that if we wanted to allow some overexposure, we could control it by clamping to something greater than 1.0f.
    tintLight = shadingMode != SHADING_FULLBRIGHT && shadingMode != SHADING_REFLECTIVE ? tintLight : clamp(tintLight, vec3(0.0), vec3(1.0));

    // NB: this is the last place we need to fix, where some color is mixed w/ some light... it's wrong.
    fragTintColor = tintColor * tintLight;

#if !DEFERRED
    fragPositionWS = positionWS.xyz;
    fragPositionVS = mat3(ViewMatrix) * positionWS.xyz;
    fragFog = computeFog(positionWS.xyz);

#if ALPHA_BLEND
    fragLargeTexCoords = fxPosition.xz / vec2(32.0);
#endif // ALPHA_BLEND

#endif // DEFERRED

#if ANIMATED || (!ALPHA_BLEND && !ALPHA_TEST)
    fragMaskTexCoords = vertTexCoords.zw;
#endif

    bool hasSSAO = effectID > EFFECT_WIND && shadingMode != SHADING_FULLBRIGHT;
    bool hasBloom = shadingMode == SHADING_FULLBRIGHT;
    fragPackedData = packData(effectID, shadingMode, hasSSAO, hasBloom);
       

#if DEBUG_BOUNDARIES && !ANIMATED
    if (vertPositionPacked.x <= 0.1 || vertPositionPacked.y <= 0.1 || vertPositionPacked.z <= 0.1)
    {
        fragTintColor = vec3(1, 0, 0);
        fragStaticLight.a = 1;
    }
#endif

//    // Debug "isSelectedBlock"
//#if !ANIMATED
//    if(isSelectedBlock) fragTintColor = vec3(0,0,1);
//#endif // !ANIMATED
//
//    // Debug "doubleSided"
//    if(vertPositionAndDoubleSidedAndBlockId.w < 0) fragTintColor = vec3(1,0,0);
//
//    // Debug billboard usage
//    if (vertBillboard == uint(1) && length(positionWS) > (LOD_DISTANCE - 20)) fragTintColor = vec3(1,0,0);

#if USE_BACKFACE_CULLING
    // The info "DoubleSided" is encoded in sign of that field.
    bool doubleSided = vertDoubleSidedAndBlockId < 0;

#define USE_DISTANT_BACKFACE_CULLING 0
#if USE_DISTANT_BACKFACE_CULLING
    // Force backface culling when geometry is not close.
    bool isFarAway = length(positionWS) > 256.0;
    bool useCulling = isFarAway || !doubleSided;
#else // USE_DISTANT_BACKFACE_CULLING
    bool useCulling = !doubleSided;
#endif // USE_DISTANT_BACKFACE_CULLING

    if (useCulling && processBackfaceCulling(positionWS.xyz, fragNormalWS))
    {
        // Force Triangle Culling by injecting NaN in a vertex pos.
        const float NaN = sqrt(-1.0);
        gl_Position.w = NaN;
    }
#endif // USE_BACKFACE_CULLING
}

// Pack data for the FragmentShader
int packData(int effectID, int shadingMode, bool hasSSAO, bool hasBloom)
{
    // Have clean shadow backfaces only for "hard geometry" (i.e., not for foliage).
    bool useCleanShadowBackfaces = effectID > EFFECT_WIND;

    int value = effectID;
    value += shadingMode << 6;
    value += hasSSAO ? (1 << 8) : 0;
    value += hasBloom ? (1 << 9) : 0;
    value += useCleanShadowBackfaces ? (1 << 15) : 0;

    return value;
}

vec4 computeFog(vec3 positionFromCameraWS)
{
    vec4 fog = vec4(0);

#if USE_FOG
    vec2 ditherNoiseSeed = vec2(0);
    fog = computeFog(positionFromCameraWS, CameraPosition, SunPositionWS,
                        FogTopColor.rgb, FogFrontColor.rgb, FogBackColor.rgb, FogParams, FogMoodParamsA, FogHeightDensityAtViewer,
                        ditherNoiseSeed, uFogNoiseTexture, SceneBrightness.r);
#endif // USE_FOG

    return fog;
}


bool processBackfaceCulling(vec3 positionWS, vec3 normalWS)
{
#if USE_BACKFACE_CULLING
    // Use an epsilon instead of 0 to account for precision issues that would cull "too early" side facing triangles.
    const float epsilon = 0.01;
    return dot(normalWS, normalize(positionWS)) > epsilon;
#else
    return false;
#endif
}
