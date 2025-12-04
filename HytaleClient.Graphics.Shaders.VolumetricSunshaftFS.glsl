#version 330 core

// Global scene data
#include "SceneData_inc.glsl"

#include "Deferred_inc.glsl"
#include "Random_inc.glsl"

// Requires data from SceneData_inc.glsl.
#include "ShadowMap_inc.glsl"

#define NUM_SAMPLES 100
#define NUM_SAMPLE_RCP (1.0 / NUM_SAMPLES)
#define TAU 0.0001
#define PHI 10000000.0
#define PI_RCP 0.31820988618379067153776752674503
#define PI 3.14159265358979323846
#define G_SCATTERING 0.3

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;
uniform vec3 uSunDirection;
uniform vec4 uSunColor;
uniform sampler2D uGBuffer0Texture;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec3 fragFrustumRay;
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

// Mie-scattering phase function approximated by the Henyey-Greenstein phase function
float ComputeScattering(float lightDotView)
{
    float result = 1.0f - G_SCATTERING * G_SCATTERING;
    result /= (4.0f * PI * pow(1.0f + G_SCATTERING * G_SCATTERING - (2.0f * G_SCATTERING) * lightDotView, 1.5f));
    return result;
}

// Volumetric sunshafts algorithm from "Volumetric Lighting for Many Lights in Lords of the Fallen"
// Slides : https://www.slideshare.net/BenjaminGlatzel/volumetric-lighting-for-many-lights-in-lords-of-the-fallen
// Other reference : https://www.alexandre-pestana.com/volumetric-lights/
void main()
{
    float depth = texture(uDepthTexture, fragTexCoords).r;
    float linearDepth = depth;
    vec3 positionFromCameraWS = fragFrustumRay * depth;

    vec2 compressedNormal = texture(uGBuffer0Texture, fragTexCoords).ba;
    vec3 normalWS;
    normalWS = decodeNormal(compressedNormal);

    vec4 positionLS;
    int cascadeId = getCascadeIdNew(positionFromCameraWS, normalWS, positionLS);

    float raymarchDistanceLimit = 1024.0;
    float raymarchDistance = (clamp(length(positionFromCameraWS.xyz), 0.0, raymarchDistanceLimit));
    float stepSize = raymarchDistance * NUM_SAMPLE_RCP;
    vec3 rayPositionWS = positionFromCameraWS.xyz;

    vec3 VLI = vec3(0.0);
    for (float l = raymarchDistance; l > stepSize; l -= stepSize)
    {
        vec3 rayDir = normalize(positionFromCameraWS);
        rayPositionWS -=  stepSize * rayDir;

//        float shadow = computeShadowIntensity(rayPositionWS, linearDepth, normalWS, false) ;

        cascadeId = getCascadeIdNew(rayPositionWS, normalWS, positionLS);
        vec3 positionTS  = positionLS.xyz;

        positionTS.xyz = positionTS.xyz * vec3(0.5) + vec3(0.5);

        // Get ready to sample the right cascade.
        const float CascadeScale = 1.0f / CASCADE_COUNT;
        positionTS.x = positionTS.x * CascadeScale + CascadeScale * cascadeId;
        vec3 shadowTerm =  vec3(texture(uShadowMap, positionTS));

        float v = TAU *(pow(shadowTerm.x,1.0))* (PHI * 0.25 * PI_RCP) * 0.01;
        float lightDotView = dot(rayDir, normalize(-uSunDirection.xyz));
        VLI += vec3(ComputeScattering(lightDotView) * 2.0) * v;
    }

    VLI *= NUM_SAMPLE_RCP;

    // adds noise for a less visible banding effect
    VLI += (n2rand_faster(VLI.xy * Time)* 2.0 - 1.0) * 0.025;

    vec3 color= VLI * uSunColor.rgb * uSunColor.a * (1.2 - pow(SceneBrightness.r, 3.0));
    outColor.rgba = vec4(color.rgb, 1.0);
}

