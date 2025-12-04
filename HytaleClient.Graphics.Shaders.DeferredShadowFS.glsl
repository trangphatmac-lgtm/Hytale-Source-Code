#version 330 core

// Global scene data
#include "SceneData_inc.glsl"           
#include "Reconstruction_inc.glsl"
#include "Deferred_inc.glsl"

// Requires data from SceneData_inc.glsl.
#include "ShadowMap_inc.glsl"           

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;

#if USE_NORMAL_BIAS || USE_CLEAN_BACKFACES
uniform sampler2D uGBuffer0Texture;
#endif

#if USE_CLEAN_BACKFACES
uniform sampler2D uGBuffer1Texture;
#endif

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

void main()
{
    // The output RenderTexture is cleared to 1.0.
    // We will write the amount of shadow per pixel.

    vec4 color;

    float depth = texture(uDepthTexture, fragTexCoords).r;

#if USE_NORMAL_BIAS || USE_CLEAN_BACKFACES
    vec2 compressedNormal = texture(uGBuffer0Texture, fragTexCoords).ba;
#endif

#if USE_CLEAN_BACKFACES
    float configBits = texture(uGBuffer1Texture, fragTexCoords).a;
#endif

    const float DEPTH_FAR_THRESHOLD = 1.0;

    if (depth < DEPTH_FAR_THRESHOLD)
    {
#if USE_LINEAR_Z
        float linearDepth = depth;
        vec3 positionFromCameraWS = fragFrustumRay * depth;
#else
        float linearDepth = GetLinearDepthFromDepthHW(depth, ProjectionMatrix);
        vec3 positionFromCameraWS = PositionFromDepth(depth, fragTexCoords, InvViewProjectionMatrix);
#endif

        bool useCleanShadowBackfaces;
        vec3 normalWS;

#if USE_NORMAL_BIAS || USE_CLEAN_BACKFACES
        normalWS = decodeNormal(compressedNormal);

#if !INPUT_NORMALS_IN_WS
        normalWS = mat3(InvViewMatrix) * normalWS + CameraPosition;
#endif
#endif // USE_NORMAL_BIAS || USE_CLEAN_BACKFACES

#if USE_CLEAN_BACKFACES
        unpackFragBit7(configBits, useCleanShadowBackfaces);
#endif

        float shadow = computeShadowIntensity(positionFromCameraWS, linearDepth, normalWS, useCleanShadowBackfaces);

        const float SHADOW_DISCARD_THRESHOLD = 0.99f;

        if (shadow >= SHADOW_DISCARD_THRESHOLD) discard;

        outColor.rgba = vec4(shadow);

    }
    else
    {
        discard;
    }
}

