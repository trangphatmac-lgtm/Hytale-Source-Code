//#version 330

#ifndef SCENE_DATA_INCLUDE
#define SCENE_DATA_INCLUDE

// SceneRenderer.GLBuffer sceneData

// Warning: remeber to avoid vec3 in UBO, as some drivers have the layout wrong for them.
// https://www.khronos.org/opengl/wiki/Interface_Block_(GLSL)#Memory_layout

#define MAX_CASCADES 4

layout(std140) uniform uboSceneDataBlock
{
    // Camera
    mat4 FirstPersonViewMatrix;         // useless, always Identity
    mat4 FirstPersonProjectionMatrix;
    mat4 ViewMatrix;
    mat4 ProjectionMatrix;
    mat4 ViewProjectionMatrix;
    mat4 InvViewMatrix;
    mat4 InvViewProjectionMatrix;
    mat4 ReprojectionMatrix;
    
    vec4 ProjInfos;                     
    vec4 CameraPositionAndIsUnderwater; // .xyz position, .w isCameraUnderwater (0/1)
    vec4 CameraDirection;               // free .w
    vec4 ViewportInfo;                  // xy: size, zw: 1/size
    vec4 NearFarClip;                   // free .zw
    vec4 TimeSinCosDelta;

    // Screen pass
    // vec4 FrustumFarCornersWS[4];        // .w free
    // vec4 FrustumFarCornersVS[4];        // .w free

    // Sun
    vec4 SunLightDirection;             // .w free
    vec4 SunLightDirectionVS;           // .w free
    vec4 SunLightColor;
    vec4 SunColor;                      // .w free

    // Sky Ambient
    vec4 AmbientFrontColorAndIntensity; 
    vec4 AmbientColorBack;              // .rgb color, .w caustics anim time
    
    // Fog 
    vec4 FogTopColor;                   // .rgb color, .w caustics distortion
    vec4 FogFrontColor;                 // .rgb color, .w caustics scale
    vec4 FogBackColor;                  // .rgb color, .w caustics intensity
    vec4 FogParams;        
    vec4 FogMoodParamsA;
    vec4 FogMoodParamsB;                // .yz free, .w clouds shadow anim time

    // Clustered Lighting 
    vec4 LightGridResolution;
    vec4 ZSlicesParams;

    vec4 SunShadowIntensityParams;                          // .x sun shadow intensity, .yzw clouds shadow {blurriness, scale, intensity}
    vec4 SunShadowCascadeStats[MAX_CASCADES];               // .zw free
    vec4 SunShadowCascadeCachedTranslations[MAX_CASCADES];  // .w free
    mat4 SunShadowMatrix[MAX_CASCADES];

    // Written by a GPUProgram (from a texture data)
    vec4 SceneBrightness;               // 

//////// Order Independent Transparency
//////ivec2 uOITParams;
};

// Helpers
#define CameraPosition (CameraPositionAndIsUnderwater.xyz)
#define IsCameraUnderwater (CameraPositionAndIsUnderwater.w)

#define NearClip (NearFarClip.x)
#define FarClip (NearFarClip.y)
#define Time (TimeSinCosDelta.x)
#define SinTime (TimeSinCosDelta.y)
#define CosTime (TimeSinCosDelta.z)
#define DeltaTime (TimeSinCosDelta.w)

#define ViewportSize (ViewportInfo.xy)
#define InvViewportSize (ViewportInfo.zw)

#define SunPositionWS (SunLightDirection.xyz)
#define SunPositionVS (SunLightDirectionVS.xyz)

#define AmbientFrontColor (AmbientFrontColorAndIntensity.xyz)
#define AmbientIntensity (AmbientFrontColorAndIntensity.w)
#define AmbientBackColor (AmbientColorBack.xyz)

#define FogHeightFalloff (FogMoodParamsA.x)
#define FogGlobalDensity (FogMoodParamsA.y)
#define FogSpeed (FogMoodParamsA.z)
#define FogDensityVariationScale (FogMoodParamsA.w)
#define FogHeightDensityAtViewer (FogMoodParamsB.x)

#define WaterCausticsAnimTime       (AmbientColorBack.w)
#define WaterCausticsDistortion     (FogTopColor.w)
#define WaterCausticsScale          (FogFrontColor.w)
#define WaterCausticsIntensity      (FogBackColor.w)

#define CloudsShadowAnimTime       (FogMoodParamsB.w)
#define CloudsShadowBlurriness     (SunShadowIntensityParams.y)
#define CloudsShadowScale          (SunShadowIntensityParams.z)
#define CloudsShadowIntensity      (SunShadowIntensityParams.w)

// Compatibility
#define uLightGridResolution (LightGridResolution.xyz)
#define uZSlicesParams (ZSlicesParams.xyz)

#define SunShadowIntensity (SunShadowIntensityParams.x)

//////uniform vec3 uWaterTintColor;
//////uniform int uWaterQuality = 1;
//////uniform vec3 uFoliageInteractionPositions[MAX_INTERACTION_POSITIONS];
//////uniform vec3 uFoliageInteractionParams;


//
//layout(std140) uniform uboPointLightBlock
//{
//  vec3 uLightGridResolution;
//  vec3 uZSlicesParams;
//  vec4 PointLights[MAX_LIGHTS];
//};

// Prepare for some possible overriding of this scene data
#ifndef USE_SCENE_DATA_OVERRIDE
#define USE_SCENE_DATA_OVERRIDE 0
#endif

#endif

